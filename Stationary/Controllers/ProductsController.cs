using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stationary.Data;
using Stationary.Models;
using Stationary.Services;

namespace Stationary.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Route("api/productsapi")]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IProductService _productService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IEventStreamService _eventStream;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IRedisCacheService _redisCache;

        public ProductsController(
            ApplicationDbContext db,
            IProductService productService,
            IWebHostEnvironment webHostEnvironment,
            IEventStreamService eventStream,
            ICloudinaryService cloudinaryService,
            IRedisCacheService redisCache)
        {
            _db = db;
            _productService = productService;
            _webHostEnvironment = webHostEnvironment;
            _eventStream = eventStream;
            _cloudinaryService = cloudinaryService;
            _redisCache = redisCache;
        }

        private async Task InvalidateProductCachesAsync()
        {
            try
            {
                await _redisCache.RemoveByPatternAsync("products:*");
                await _redisCache.RemoveAsync("products:all");
                await _redisCache.RemoveAsync("products:categories");
            }
            catch { }
        }

        private async Task<User?> GetCurrentAuthUserAsync()
        {
            int? userId = null;

            if (User.Identity?.IsAuthenticated == true)
            {
                var idClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
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
                                c.Type == System.Security.Claims.ClaimTypes.NameIdentifier ||
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
        public async Task<IActionResult> GetProducts(
            [FromQuery] string? search,
            [FromQuery] string? category,
            [FromQuery] string stockFilter = "available",
            [FromQuery] bool? includeHidden = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12)
        {
            var user = await GetCurrentAuthUserAsync();
            bool isAdmin = user != null && string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase);
            bool shouldIncludeHidden = (includeHidden == true) || (isAdmin && string.Equals(stockFilter, "all", StringComparison.OrdinalIgnoreCase)) || (string.Equals(stockFilter, "all", StringComparison.OrdinalIgnoreCase));

            IEnumerable<Product> products;

            switch (stockFilter?.ToLower())
            {
                case "all":
                    products = await _productService.GetAvailableProductsAsync(true, shouldIncludeHidden);
                    break;
                case "outofstock":
                    products = await _productService.GetOutOfStockProductsAsync();
                    break;
                case "lowstock":
                    products = await _productService.GetLowStockProductsAsync();
                    break;
                default:
                    products = await _productService.GetAvailableProductsAsync(false, false);
                    break;
            }

            if (!string.IsNullOrEmpty(search))
            {
                products = products.Where(p =>
                    (p.Name != null && p.Name.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (p.Category != null && p.Category.Contains(search, StringComparison.OrdinalIgnoreCase)));
            }

            if (!string.IsNullOrEmpty(category))
            {
                products = products.Where(p =>
                    p.Category != null && p.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
            }

            var totalProducts = products.Count();
            var totalPages = (int)Math.Ceiling((double)totalProducts / Math.Max(1, pageSize));

            var pagedProducts = products
                .Skip((Math.Max(1, page) - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Ok(new
            {
                products = pagedProducts,
                currentPage = page,
                totalPages,
                totalProducts,
                pageSize
            });
        }

        [HttpGet("top")]
        public async Task<IActionResult> GetTopProducts([FromQuery] int count = 5)
        {
            var topProducts = await _productService.GetTopProductsAsync(Math.Clamp(count, 1, 20));
            return Ok(topProducts);
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _productService.GetCategoriesAsync();
            return Ok(categories);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
                return NotFound(new { message = "Product not found." });

            return Ok(product);
        }

        public class CreateProductFormDto
        {
            public string Name { get; set; } = string.Empty;
            public string Category { get; set; } = "Uncategorized";
            public decimal Price { get; set; }
            public int StockQuantity { get; set; }
            public int LowStockThreshold { get; set; } = 5;
            public bool IsVisible { get; set; } = true;
            public IFormFile? Image { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromForm] CreateProductFormDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name) || dto.Price <= 0 || dto.StockQuantity < 0)
            {
                return BadRequest(new { message = "Please provide valid product information." });
            }

            var currentAdmin = await GetCurrentAuthUserAsync();
            string imagePath;

            if (dto.Image != null && dto.Image.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var fileExtension = Path.GetExtension(dto.Image.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new { message = "Please upload a valid image file (JPG, PNG, GIF, WEBP)." });
                }

                imagePath = await _cloudinaryService.UploadImageAsync(dto.Image);
            }
            else
            {
                imagePath = _cloudinaryService.GetFallbackImageUrl(dto.Category, dto.Name);
            }

            var product = new Product
            {
                Name = dto.Name.Trim(),
                Category = string.IsNullOrWhiteSpace(dto.Category) ? "Uncategorized" : dto.Category.Trim(),
                Price = dto.Price,
                StockQuantity = dto.StockQuantity,
                LowStockThreshold = dto.LowStockThreshold,
                IsVisible = dto.IsVisible,
                ImagePath = imagePath,
                AdminId = currentAdmin?.Id,
                AdminUsername = currentAdmin?.Username
            };

            _db.Products.Add(product);
            await _db.SaveChangesAsync();
            await InvalidateProductCachesAsync();

            _eventStream.BroadcastEvent("stock_update", new
            {
                action = "create",
                productId = product.Id,
                productName = product.Name,
                stockQuantity = product.StockQuantity,
                isVisible = product.IsVisible,
                timestamp = DateTime.UtcNow
            });

            return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, product);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> EditProduct(int id, [FromForm] CreateProductFormDto dto)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
                return NotFound(new { message = "Product not found." });

            if (string.IsNullOrWhiteSpace(dto.Name) || dto.Price <= 0 || dto.StockQuantity < 0)
            {
                return BadRequest(new { message = "Please provide valid product information." });
            }

            product.Name = dto.Name.Trim();
            product.Category = string.IsNullOrWhiteSpace(dto.Category) ? "Uncategorized" : dto.Category.Trim();
            product.Price = dto.Price;
            product.StockQuantity = dto.StockQuantity;
            product.LowStockThreshold = dto.LowStockThreshold;
            product.IsVisible = dto.IsVisible;

            if (dto.Image != null && dto.Image.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var fileExtension = Path.GetExtension(dto.Image.FileName).ToLowerInvariant();

                if (allowedExtensions.Contains(fileExtension))
                {
                    product.ImagePath = await _cloudinaryService.UploadImageAsync(dto.Image);
                }
            }

            await _db.SaveChangesAsync();
            await InvalidateProductCachesAsync();

            _eventStream.BroadcastEvent("stock_update", new
            {
                action = "update",
                productId = product.Id,
                productName = product.Name,
                stockQuantity = product.StockQuantity,
                isVisible = product.IsVisible,
                timestamp = DateTime.UtcNow
            });

            return Ok(product);
        }

        [HttpPatch("{id}/visibility")]
        public async Task<IActionResult> ToggleVisibility(int id)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
                return NotFound(new { message = "Product not found." });

            product.IsVisible = !product.IsVisible;
            await _db.SaveChangesAsync();
            await InvalidateProductCachesAsync();

            _eventStream.BroadcastEvent("stock_update", new
            {
                action = "toggle_visibility",
                productId = product.Id,
                productName = product.Name,
                stockQuantity = product.StockQuantity,
                isVisible = product.IsVisible,
                timestamp = DateTime.UtcNow
            });

            return Ok(new { success = true, isVisible = product.IsVisible, message = $"Product {(product.IsVisible ? "shown (Stock ON)" : "hidden (Stock OFF)")} successfully." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
                return NotFound(new { message = "Product not found." });

            bool usedInOrders = await _db.Orders.AnyAsync(o => o.OrderItems.Any(oi => oi.ProductId == id));
            if (usedInOrders)
            {
                return BadRequest(new { message = "Cannot delete product because it exists in past orders." });
            }

            var carts = await _db.Carts.Where(c => c.ProductId == id).ToListAsync();
            if (carts.Any())
                _db.Carts.RemoveRange(carts);

            _db.Products.Remove(product);
            await _db.SaveChangesAsync();
            await InvalidateProductCachesAsync();

            _eventStream.BroadcastEvent("stock_update", new
            {
                action = "delete",
                productId = id,
                timestamp = DateTime.UtcNow
            });

            return Ok(new { success = true, message = "Product deleted successfully." });
        }
    }
}
