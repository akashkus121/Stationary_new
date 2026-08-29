using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Stationary.Data;
using Stationary.Models;

namespace Stationary.Services
{
    public class PendingQueueProcessorService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PendingQueueProcessorService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(15);

        public PendingQueueProcessorService(IServiceProvider serviceProvider, ILogger<PendingQueueProcessorService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PendingQueueProcessorService started monitoring SQL connection and offline fallback queue.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessQueueIfSqlAvailableAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error occurred during pending queue sync loop.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        private async Task ProcessQueueIfSqlAvailableAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var fallbackQueue = scope.ServiceProvider.GetRequiredService<IOfflineFallbackQueueService>();
            var redisCache = scope.ServiceProvider.GetRequiredService<IRedisCacheService>();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var eventStream = scope.ServiceProvider.GetRequiredService<IEventStreamService>();

            var pendingActions = await fallbackQueue.GetPendingActionsAsync();
            var pendingOrderCount = await redisCache.GetQueueLengthAsync("orders:pending");

            if (!pendingActions.Any() && pendingOrderCount <= 0)
            {
                return;
            }

            bool canConnect = false;
            try
            {
                canConnect = await db.Database.CanConnectAsync(stoppingToken);
            }
            catch
            {
                canConnect = false;
            }

            if (!canConnect)
            {
                _logger.LogWarning("Master database is offline. {ActionCount} actions and {OrderCount} orders waiting in Upstash Redis fallback queue.", pendingActions.Count, pendingOrderCount);
                return;
            }

            // =====================================================================
            // 1. Process Queued Orders from Upstash Redis Message Queue
            // =====================================================================
            if (pendingOrderCount > 0)
            {
                _logger.LogInformation("Master database online. Synchronizing {Count} queued orders from Upstash Redis...", pendingOrderCount);

                int syncedOrders = 0;
                while (pendingOrderCount > 0 && !stoppingToken.IsCancellationRequested)
                {
                    var queuedOrder = await redisCache.DequeueAsync<Stationary.Controllers.OrdersController.QueuedOrderDto>("orders:pending");
                    if (queuedOrder == null) break;

                    try
                    {
                        var order = new Order
                        {
                            UserId = queuedOrder.UserId,
                            Subtotal = queuedOrder.Subtotal,
                            TaxAmount = queuedOrder.TaxAmount,
                            TotalAmount = queuedOrder.TotalAmount,
                            Date = queuedOrder.Date,
                            PaymentMethod = queuedOrder.PaymentMethod,
                            OrderStatus = "Completed",
                            OrderItems = queuedOrder.Items.Select(i => new OrderItem
                            {
                                ProductId = i.ProductId,
                                AdminId = i.AdminId,
                                Quantity = i.Quantity,
                                ProductName = i.ProductName,
                                Price = i.Price,
                                TotalPrice = i.Price * i.Quantity
                            }).ToList()
                        };

                        // Deduct product stock in DB
                        foreach (var item in queuedOrder.Items)
                        {
                            var product = await db.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId, stoppingToken);
                            if (product != null)
                            {
                                product.StockQuantity = Math.Max(0, product.StockQuantity - item.Quantity);
                            }
                        }

                        db.Orders.Add(order);
                        await db.SaveChangesAsync(stoppingToken);

                        // Remove from user-level Redis queue
                        var userOrdersKey = $"orders:user:{queuedOrder.UserId}";
                        var userOrders = await redisCache.GetAsync<List<Stationary.Controllers.OrdersController.QueuedOrderDto>>(userOrdersKey);
                        if (userOrders != null)
                        {
                            userOrders.RemoveAll(o => o.QueueId == queuedOrder.QueueId);
                            if (userOrders.Any())
                            {
                                await redisCache.SetAsync(userOrdersKey, userOrders, TimeSpan.FromDays(7));
                            }
                            else
                            {
                                await redisCache.RemoveAsync(userOrdersKey);
                            }
                        }

                        syncedOrders++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to persist queued order {QueueId}. Re-enqueueing to Redis.", queuedOrder.QueueId);
                        await redisCache.EnqueueAsync("orders:pending", queuedOrder);
                        break;
                    }
                }

                if (syncedOrders > 0)
                {
                    _logger.LogInformation("Successfully synced {Count} orders from Upstash Redis to PostgreSQL database.", syncedOrders);
                    await redisCache.RemoveByPatternAsync("products:*");

                    eventStream.BroadcastEvent("stock_update", new
                    {
                        action = "orders_synced",
                        count = syncedOrders,
                        timestamp = DateTime.UtcNow
                    });
                }
            }

            // =====================================================================
            // 2. Process Admin Offline Fallback Queue Actions
            // =====================================================================
            if (pendingActions.Any())
            {
                _logger.LogInformation("Processing {Count} queued admin actions...", pendingActions.Count);

                int processedCount = 0;
                foreach (var item in pendingActions)
                {
                    if (stoppingToken.IsCancellationRequested) break;

                    try
                    {
                        if (item.ActionType == "bulk_stock_update")
                        {
                            var updates = JsonSerializer.Deserialize<List<StockUpdateModel>>(item.PayloadJson);
                            if (updates != null && updates.Any())
                            {
                                foreach (var u in updates)
                                {
                                    var product = await db.Products.FirstOrDefaultAsync(p => p.Id == u.ProductId, stoppingToken);
                                    if (product != null)
                                    {
                                        product.StockQuantity = Math.Max(0, u.NewStockQuantity);
                                        product.LowStockThreshold = Math.Max(0, u.NewLowStockThreshold);
                                    }
                                }
                                await db.SaveChangesAsync(stoppingToken);
                            }
                        }
                        else if (item.ActionType == "bulk_create")
                        {
                            var products = JsonSerializer.Deserialize<List<BulkProductModel>>(item.PayloadJson);
                            if (products != null && products.Any())
                            {
                                foreach (var p in products)
                                {
                                    db.Products.Add(new Product
                                    {
                                        Name = p.Name.Trim(),
                                        Category = string.IsNullOrWhiteSpace(p.Category) ? "Uncategorized" : p.Category.Trim(),
                                        Price = p.Price,
                                        StockQuantity = Math.Max(0, p.StockQuantity),
                                        LowStockThreshold = p.LowStockThreshold <= 0 ? 5 : p.LowStockThreshold,
                                        IsVisible = p.IsVisible,
                                        ImagePath = p.ImageUrl ?? p.ImagePath ?? string.Empty
                                    });
                                }
                                await db.SaveChangesAsync(stoppingToken);
                            }
                        }

                        await fallbackQueue.RemovePendingActionAsync(item.ActionId);
                        processedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process queued action {ActionId} ({ActionType}). Will retry next loop.", item.ActionId, item.ActionType);
                    }
                }

                if (processedCount > 0)
                {
                    _logger.LogInformation("Successfully synced {Count} queued offline actions to database.", processedCount);

                    var freshProducts = await db.Products.AsNoTracking().ToListAsync(stoppingToken);
                    await fallbackQueue.SaveProductCacheAsync(freshProducts);

                    eventStream.BroadcastEvent("stock_update", new
                    {
                        action = "queue_synced",
                        syncedCount = processedCount,
                        timestamp = DateTime.UtcNow
                    });
                }
            }
        }
    }
}
