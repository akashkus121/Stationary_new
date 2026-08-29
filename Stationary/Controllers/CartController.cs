using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stationary.Data;
using Stationary.Models;
using System.Security.Claims;

namespace Stationary.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Route("api/cartapi")]
    public class CartController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly Services.IProductLockService _lockService;

        public CartController(ApplicationDbContext db, Services.IProductLockService lockService)
        {
            _db = db;
            _lockService = lockService;
        }

        private async Task<User?> GetCurrentAuthUserAsync()
        {
            int? userId = null;

            if (User.Identity?.IsAuthenticated == true)
            {
                var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? User.FindFirst("id")?.Value
                           ?? User.FindFirst("nameid")?.Value
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

        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var user = await GetCurrentAuthUserAsync();
            if (user == null)
                return Unauthorized(new { message = "Please login first." });

            var cartItems = await _db.Carts
                .Include(c => c.Product)
                .Where(c => c.UserId == user.Id)
                .ToListAsync();

            var items = cartItems.Select(c => new
            {
                id = c.Id,
                productId = c.ProductId,
                productName = c.Product?.Name ?? "Unknown Product",
                category = c.Product?.Category ?? "",
                price = c.Product?.Price ?? 0m,
                imagePath = c.Product?.ImagePath ?? "",
                stockQuantity = c.Product?.StockQuantity ?? 0,
                isOutOfStock = c.Product?.IsOutOfStock ?? true,
                quantity = c.Quantity,
                subtotal = (c.Product?.Price ?? 0m) * c.Quantity
            });

            var subtotal = items.Sum(i => i.subtotal);
            var tax = subtotal * 0.10m;
            var total = subtotal + tax;

            return Ok(new
            {
                items,
                itemCount = items.Sum(i => i.quantity),
                subtotal,
                tax,
                total
            });
        }

        [HttpGet("count")]
        public async Task<IActionResult> GetCartCount()
        {
            var user = await GetCurrentAuthUserAsync();
            if (user == null)
                return Ok(new { count = 0 });

            var count = await _db.Carts
                .Where(c => c.UserId == user.Id)
                .SumAsync(c => (int?)c.Quantity) ?? 0;

            return Ok(new { count });
        }

        public class AddToCartDto
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; } = 1;
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto)
        {
            var user = await GetCurrentAuthUserAsync();
            if (user == null)
                return Unauthorized(new { message = "Please login first." });

            if (dto.Quantity <= 0)
                return BadRequest(new { message = "Quantity must be greater than 0." });

            try
            {
                return await _lockService.ExecuteWithLockAsync(dto.ProductId, async () =>
                {
                    var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == dto.ProductId);
                    if (product == null)
                        return (IActionResult)NotFound(new { message = "Product not found." });

                    if (product.IsOutOfStock || product.StockQuantity <= 0)
                        return BadRequest(new { message = "This product is currently out of stock." });

                    var existingCartItem = await _db.Carts.FirstOrDefaultAsync(c => c.ProductId == dto.ProductId && c.UserId == user.Id);
                    var currentCartQuantity = existingCartItem?.Quantity ?? 0;
                    var availableStock = product.StockQuantity - currentCartQuantity;

                    if (dto.Quantity > availableStock)
                    {
                        return BadRequest(new { message = $"Only {availableStock} more items available in stock. (You already have {currentCartQuantity} in your cart)" });
                    }

                    if (existingCartItem == null)
                    {
                        _db.Carts.Add(new Cart
                        {
                            UserId = user.Id,
                            ProductId = dto.ProductId,
                            Quantity = dto.Quantity,
                            AddedDate = DateTime.UtcNow
                        });
                    }
                    else
                    {
                        existingCartItem.Quantity += dto.Quantity;
                        existingCartItem.UpdatedDate = DateTime.UtcNow;
                    }

                    await _db.SaveChangesAsync();

                    var totalCount = await _db.Carts.Where(c => c.UserId == user.Id).SumAsync(c => c.Quantity);

                    return Ok(new { success = true, count = totalCount, message = "Product added to cart successfully!" });
                });
            }
            catch (TimeoutException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        public class UpdateCartDto
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateCart([FromBody] UpdateCartDto dto)
        {
            var user = await GetCurrentAuthUserAsync();
            if (user == null)
                return Unauthorized(new { message = "Please login first." });

            var cartItem = await _db.Carts.FirstOrDefaultAsync(c => c.ProductId == dto.ProductId && c.UserId == user.Id);
            if (cartItem == null)
                return NotFound(new { message = "Item not found in cart." });

            if (dto.Quantity <= 0)
            {
                _db.Carts.Remove(cartItem);
                await _db.SaveChangesAsync();
                var count = await _db.Carts.Where(c => c.UserId == user.Id).SumAsync(c => c.Quantity);
                return Ok(new { success = true, count, message = "Item removed from cart." });
            }

            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == dto.ProductId);
            if (product == null)
                return NotFound(new { message = "Product not found." });

            if (product.StockQuantity < dto.Quantity)
            {
                return BadRequest(new { message = $"Only {product.StockQuantity} items available in stock." });
            }

            cartItem.Quantity = dto.Quantity;
            cartItem.UpdatedDate = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            var updatedCount = await _db.Carts.Where(c => c.UserId == user.Id).SumAsync(c => c.Quantity);
            return Ok(new { success = true, count = updatedCount, message = "Cart updated successfully." });
        }

        [HttpDelete("remove/{productId}")]
        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            var user = await GetCurrentAuthUserAsync();
            if (user == null)
                return Unauthorized(new { message = "Please login first." });

            var cartItem = await _db.Carts.FirstOrDefaultAsync(c => c.ProductId == productId && c.UserId == user.Id);
            if (cartItem != null)
            {
                _db.Carts.Remove(cartItem);
                await _db.SaveChangesAsync();
            }

            var count = await _db.Carts.Where(c => c.UserId == user.Id).SumAsync(c => c.Quantity);
            return Ok(new { success = true, count, message = "Item removed from cart successfully." });
        }

        [HttpDelete("clear")]
        public async Task<IActionResult> ClearCart()
        {
            var user = await GetCurrentAuthUserAsync();
            if (user == null)
                return Unauthorized(new { message = "Please login first." });

            var cartItems = await _db.Carts.Where(c => c.UserId == user.Id).ToListAsync();
            if (cartItems.Any())
            {
                _db.Carts.RemoveRange(cartItems);
                await _db.SaveChangesAsync();
            }

            return Ok(new { success = true, count = 0, message = "Cart cleared." });
        }
    }
}
