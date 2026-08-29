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
            var pendingActions = await fallbackQueue.GetPendingActionsAsync();

            if (!pendingActions.Any())
            {
                return;
            }

            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var eventStream = scope.ServiceProvider.GetRequiredService<IEventStreamService>();

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
                _logger.LogWarning("SQL Server remains offline. {Count} operations waiting in fallback queue.", pendingActions.Count);
                return;
            }

            _logger.LogInformation("SQL Server connection active. Processing {Count} queued offline operations...", pendingActions.Count);

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
                _logger.LogInformation("Successfully synced {Count} queued offline actions to SQL Server database.", processedCount);

                // Update product cache snapshot
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
