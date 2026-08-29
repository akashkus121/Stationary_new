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

        public OrdersController(ApplicationDbContext db, IEventStreamService eventStream)
        {
            _db = db;
            _eventStream = eventStream;
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

            return await _db.Users.FindAsync(userId);
        }

        public class CheckoutDto
        {
            public string PaymentMethod { get; set; } = "cash";
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutDto dto)
        {
            var user = await GetCurrentAuthUserAsync();
            if (user == null)
                return Unauthorized(new { message = "Please login to place an order." });

            var cart = await _db.Carts
                .Include(c => c.Product)
                .Where(c => c.UserId == user.Id)
                .ToListAsync();

            if (!cart.Any())
            {
                return BadRequest(new { message = "Your cart is empty." });
            }

            foreach (var item in cart)
            {
                if (item.Product == null || item.Product.StockQuantity < item.Quantity)
                {
                    return BadRequest(new { message = $"Insufficient stock for {item.Product?.Name ?? "Unknown Product"}." });
                }
            }

            // Deduct stock directly from the specific product owned by the corresponding admin
            foreach (var item in cart)
            {
                item.Product!.StockQuantity -= item.Quantity;
                _db.Products.Update(item.Product);
            }

            var subtotal = cart.Sum(c => c.Product!.Price * c.Quantity);
            var tax = subtotal * 0.10m;
            var total = subtotal + tax;

            var paymentMethod = string.Equals(dto.PaymentMethod, "upi", StringComparison.OrdinalIgnoreCase) ? "upi" : "cash";

            var order = new Order
            {
                UserId = user.Id,
                Subtotal = subtotal,
                TaxAmount = tax,
                TotalAmount = total,
                Date = DateTime.UtcNow,
                PaymentMethod = paymentMethod,
                OrderStatus = "Completed",
                OrderItems = cart.Select(c => new OrderItem
                {
                    ProductId = c.ProductId,
                    AdminId = c.Product?.AdminId,
                    Quantity = c.Quantity,
                    ProductName = c.Product!.Name,
                    Price = c.Product!.Price,
                    TotalPrice = c.Product!.Price * c.Quantity
                }).ToList()
            };

            _db.Orders.Add(order);
            _db.Carts.RemoveRange(cart);

            await _db.SaveChangesAsync();

            _eventStream.BroadcastEvent("stock_update", new
            {
                action = "order_checkout",
                orderId = order.Id,
                userId = user.Id,
                totalAmount = order.TotalAmount,
                timestamp = DateTime.UtcNow
            });

            var message = paymentMethod == "upi"
                ? "Order placed successfully! Please complete your UPI payment to confirm."
                : "Order placed successfully! Cash on delivery expected upon arrival.";

            return Ok(new
            {
                success = true,
                message,
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

        [HttpGet("my-orders")]
        public async Task<IActionResult> GetMyOrders()
        {
            var user = await GetCurrentAuthUserAsync();
            if (user == null)
                return Unauthorized(new { message = "Please login first." });

            var orders = await _db.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.Date)
                .ToListAsync();

            var result = orders.Select(o => new
            {
                id = o.Id,
                date = o.Date,
                totalAmount = o.TotalAmount,
                paymentMethod = o.PaymentMethod,
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
