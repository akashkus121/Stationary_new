using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stationary.Data;
using Stationary.Models;
using Stationary.Services;
using System.Security.Claims;

namespace Stationary.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Route("api/ordersapi")]
    public class OrdersController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IEventStreamService _eventStream;
        private readonly IRedisCacheService _cache;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(
            ApplicationDbContext db,
            IEventStreamService eventStream,
            IRedisCacheService cache,
            ILogger<OrdersController> logger)
        {
            _db = db;
            _eventStream = eventStream;
            _cache = cache;
            _logger = logger;
        }

        private async Task<User?> GetCurrentAuthUserAsync()
        {
            int? userId = null;

            if (User.Identity?.IsAuthenticated == true)
            {
                var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? User.FindFirst("id")?.Value
                           ?? User.FindFirst("sub")?.Value;
                if (!string.IsNullOrEmpty(idClaim) && int.TryParse(idClaim, out var parsedId))
                {
                    userId = parsedId;
                }
            }

            if (userId == null && Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                var headerStr = authHeader.ToString();
                if (headerStr.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    var tokenStr = headerStr.Substring("Bearer ".Length).Trim();
                    try
                    {
                        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                        if (handler.CanReadToken(tokenStr))
                        {
                            var jwt = handler.ReadJwtToken(tokenStr);
                            var claim = jwt.Claims.FirstOrDefault(c =>
                                c.Type == ClaimTypes.NameIdentifier ||
                                c.Type == "id" ||
                                c.Type == "nameid" ||
                                c.Type == "sub")?.Value;
                            if (int.TryParse(claim, out var jwtId))
                            {
                                userId = jwtId;
                            }
                        }
                    }
                    catch { }
                }
            }

            if (userId == null)
            {
                try
                {
                    userId = HttpContext.Session.GetInt32("UserId");
                }
                catch { }
            }

            if (userId == null) return null;

            try
            {
                return await _db.Users.FindAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to query user from DB. Falling back to JWT/cache info for user {UserId}", userId);
                return new User { Id = userId.Value, Username = $"User_{userId.Value}", Role = "User" };
            }
        }

        public class CheckoutDto
        {
            public string PaymentMethod { get; set; } = "cash";
        }

        public class QueuedOrderDto
        {
            public string QueueId { get; set; } = Guid.NewGuid().ToString();
            public int UserId { get; set; }
            public decimal Subtotal { get; set; }
            public decimal TaxAmount { get; set; }
            public decimal TotalAmount { get; set; }
            public DateTime Date { get; set; } = DateTime.UtcNow;
            public string PaymentMethod { get; set; } = "cash";
            public string OrderStatus { get; set; } = "Processing (Queued)";
            public List<QueuedOrderItemDto> Items { get; set; } = new();
        }

        public class QueuedOrderItemDto
        {
            public int ProductId { get; set; }
            public int? AdminId { get; set; }
            public string ProductName { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public decimal Price { get; set; }
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutDto dto)
        {
            var user = await GetCurrentAuthUserAsync();
            if (user == null)
                return Unauthorized(new { message = "Please login to place an order." });

            List<Cart> cart = new();
            bool dbOnline = true;

            try
            {
                cart = await _db.Carts
                    .Include(c => c.Product)
                    .Where(c => c.UserId == user.Id)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Main database connection failed during cart retrieval. Checking Redis cached cart.");
                dbOnline = false;
                var cachedCart = await _cache.GetAsync<List<Cart>>($"cart:{user.Id}");
                if (cachedCart != null) cart = cachedCart;
            }

            if (!cart.Any())
            {
                return BadRequest(new { message = "Your cart is empty." });
            }

            var subtotal = cart.Sum(c => (c.Product?.Price ?? 0) * c.Quantity);
            var tax = subtotal * 0.10m;
            var total = subtotal + tax;
            var paymentMethod = string.Equals(dto.PaymentMethod, "upi", StringComparison.OrdinalIgnoreCase) ? "upi" : "cash";

            var queuedOrder = new QueuedOrderDto
            {
                UserId = user.Id,
                Subtotal = subtotal,
                TaxAmount = tax,
                TotalAmount = total,
                Date = DateTime.UtcNow,
                PaymentMethod = paymentMethod,
                OrderStatus = dbOnline ? "Completed" : "Queued",
                Items = cart.Select(c => new QueuedOrderItemDto
                {
                    ProductId = c.ProductId,
                    AdminId = c.Product?.AdminId,
                    ProductName = c.Product?.Name ?? "Item",
                    Quantity = c.Quantity,
                    Price = c.Product?.Price ?? 0
                }).ToList()
            };

            // Attempt Master DB Persistence
            if (dbOnline)
            {
                try
                {
                    foreach (var item in cart)
                    {
                        if (item.Product != null)
                        {
                            item.Product.StockQuantity = Math.Max(0, item.Product.StockQuantity - item.Quantity);
                            _db.Products.Update(item.Product);
                        }
                    }

                    var order = new Order
                    {
                        UserId = user.Id,
                        Subtotal = subtotal,
                        TaxAmount = tax,
                        TotalAmount = total,
                        Date = DateTime.UtcNow,
                        PaymentMethod = paymentMethod,
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

                    _db.Orders.Add(order);
                    _db.Carts.RemoveRange(cart);
                    await _db.SaveChangesAsync();

                    // Invalidate caches
                    await _cache.RemoveAsync($"cart:{user.Id}");
                    await _cache.RemoveByPatternAsync("products:*");

                    _eventStream.BroadcastEvent("stock_update", new
                    {
                        action = "order_checkout",
                        orderId = order.Id,
                        userId = user.Id,
                        totalAmount = order.TotalAmount,
                        timestamp = DateTime.UtcNow
                    });

                    var successMsg = paymentMethod == "upi"
                        ? "Order placed successfully! Please complete your UPI payment to confirm."
                        : "Order placed successfully! Cash on delivery expected upon arrival.";

                    return Ok(new
                    {
                        success = true,
                        isQueued = false,
                        message = successMsg,
                        order = new
                        {
                            id = order.Id,
                            date = order.Date,
                            totalAmount = order.TotalAmount,
                            paymentMethod = order.PaymentMethod,
                            items = order.OrderItems.Select(i => new
                            {
                                productId = i.ProductId,
                                adminId = i.AdminId,
                                productName = i.ProductName,
                                quantity = i.Quantity,
                                price = i.Price,
                                itemTotal = i.Price * i.Quantity
                            })
                        }
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to commit order to primary SQL database. Redirecting to Upstash Redis Message Queue.");
                    dbOnline = false;
                }
            }

            // =====================================================================
            // Failover: Enqueue to Upstash Redis Message Queue (Zero Downtime)
            // =====================================================================
            try
            {
                // 1. Push order to Redis Message Queue for background sync
                await _cache.EnqueueAsync("orders:pending", queuedOrder);

                // 2. Save user-level order copy in Redis so user sees it in My Orders immediately
                var userOrdersKey = $"orders:user:{user.Id}";
                var existingUserOrders = await _cache.GetAsync<List<QueuedOrderDto>>(userOrdersKey) ?? new List<QueuedOrderDto>();
                existingUserOrders.Insert(0, queuedOrder);
                await _cache.SetAsync(userOrdersKey, existingUserOrders, TimeSpan.FromDays(7));

                // 3. Clear cached cart
                await _cache.RemoveAsync($"cart:{user.Id}");

                _logger.LogInformation("Order {QueueId} for User {UserId} successfully enqueued to Upstash Redis Message Queue.", queuedOrder.QueueId, user.Id);

                _eventStream.BroadcastEvent("stock_update", new
                {
                    action = "order_queued",
                    queueId = queuedOrder.QueueId,
                    userId = user.Id,
                    totalAmount = queuedOrder.TotalAmount,
                    timestamp = DateTime.UtcNow
                });

                return Ok(new
                {
                    success = true,
                    isQueued = true,
                    message = "Database is currently synchronizing. Your order has been securely saved to the Upstash Redis Message Queue and will be finalized automatically!",
                    order = new
                    {
                        id = queuedOrder.QueueId,
                        date = queuedOrder.Date,
                        totalAmount = queuedOrder.TotalAmount,
                        paymentMethod = queuedOrder.PaymentMethod,
                        status = "Queued in Upstash Redis",
                        items = queuedOrder.Items.Select(i => new
                        {
                            productId = i.ProductId,
                            adminId = i.AdminId,
                            productName = i.ProductName,
                            quantity = i.Quantity,
                            price = i.Price,
                            itemTotal = i.Price * i.Quantity
                        })
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Critical failure saving order to Upstash Redis message queue.");
                return StatusCode(500, new { message = "Order processing temporarily unavailable. Please retry in a few moments." });
            }
        }

        [HttpGet("my-orders")]
        public async Task<IActionResult> GetMyOrders()
        {
            var user = await GetCurrentAuthUserAsync();
            if (user == null)
                return Unauthorized(new { message = "Please login first." });

            var result = new List<object>();

            // 1. Fetch any pending queued orders from Redis
            try
            {
                var queuedOrders = await _cache.GetAsync<List<QueuedOrderDto>>($"orders:user:{user.Id}");
                if (queuedOrders != null && queuedOrders.Any())
                {
                    foreach (var qo in queuedOrders)
                    {
                        result.Add(new
                        {
                            id = qo.QueueId,
                            date = qo.Date,
                            totalAmount = qo.TotalAmount,
                            paymentMethod = qo.PaymentMethod,
                            status = "Queued in Upstash Redis (Syncing)",
                            itemCount = qo.Items?.Sum(i => i.Quantity) ?? 0,
                            items = qo.Items?.Select(i => new
                            {
                                id = 0,
                                productId = i.ProductId,
                                productName = i.ProductName,
                                quantity = i.Quantity,
                                price = i.Price
                            })
                        });
                    }
                }
            }
            catch { }

            // 2. Fetch database orders
            try
            {
                var dbOrders = await _db.Orders
                    .Include(o => o.OrderItems)
                    .Where(o => o.UserId == user.Id)
                    .OrderByDescending(o => o.Date)
                    .ToListAsync();

                foreach (var o in dbOrders)
                {
                    result.Add(new
                    {
                        id = o.Id.ToString(),
                        date = o.Date,
                        totalAmount = o.TotalAmount,
                        paymentMethod = o.PaymentMethod,
                        status = o.OrderStatus,
                        itemCount = o.OrderItems?.Sum(i => i.Quantity) ?? 0,
                        items = o.OrderItems?.Select(i => new
                        {
                            id = i.Id,
                            productId = i.ProductId,
                            productName = i.ProductName,
                            quantity = i.Quantity,
                            price = i.Price
                        })
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "DB unavailable during GetMyOrders. Returning Redis cached/queued orders.");
            }

            return Ok(result);
        }

        [HttpGet("all-orders")]
        public async Task<IActionResult> GetAllOrders()
        {
            var user = await GetCurrentAuthUserAsync();
            if (user == null || !string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase))
                return Unauthorized(new { message = "Admin authorization required." });

            var query = _db.Orders.Include(o => o.OrderItems).AsQueryable();

            bool isSuperAdmin = string.Equals(user.Username, "admin", StringComparison.OrdinalIgnoreCase);
            if (!isSuperAdmin)
            {
                query = query.Where(o => o.OrderItems.Any(i => i.AdminId == user.Id || i.AdminId == null));
            }

            var orders = await query.OrderByDescending(o => o.Date).ToListAsync();

            var userIds = orders.Select(o => o.UserId).Distinct().ToList();
            var usersDict = await _db.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Username);

            var result = orders.Select(o =>
            {
                var itemsToShow = isSuperAdmin
                    ? o.OrderItems
                    : o.OrderItems.Where(i => i.AdminId == user.Id || i.AdminId == null).ToList();

                return new
                {
                    id = o.Id,
                    userId = o.UserId,
                    username = usersDict.TryGetValue(o.UserId, out var uname) ? uname : $"User #{o.UserId}",
                    date = o.Date,
                    totalAmount = o.TotalAmount,
                    paymentMethod = o.PaymentMethod,
                    itemCount = itemsToShow?.Sum(i => i.Quantity) ?? 0,
                    items = itemsToShow?.Select(i => new
                    {
                        id = i.Id,
                        productId = i.ProductId,
                        adminId = i.AdminId,
                        productName = i.ProductName,
                        quantity = i.Quantity,
                        price = i.Price
                    })
                };
            });

            return Ok(result);
        }
    }
}
