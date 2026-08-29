using Microsoft.EntityFrameworkCore;
using Stationary.Data;
using Stationary.Models;

namespace Stationary.Services
{
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _db;
        private readonly IRedisCacheService _cache;
        private static readonly TimeSpan DefaultCacheTtl = TimeSpan.FromMinutes(30);

        public ProductService(ApplicationDbContext db, IRedisCacheService cache)
        {
            _db = db;
            _cache = cache;
        }

        private async Task InvalidateProductCacheAsync()
        {
            await _cache.RemoveAsync("products:all");
            await _cache.RemoveAsync("products:categories");
            await _cache.RemoveByPatternAsync("products:*");
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            const string cacheKey = "products:all";
            var cached = await _cache.GetAsync<List<Product>>(cacheKey);
            if (cached != null && cached.Count > 0)
            {
                return cached;
            }

            var products = await _db.Products.AsNoTracking().ToListAsync();
            await _cache.SetAsync(cacheKey, products, DefaultCacheTtl);
            return products;
        }

        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(string category)
        {
            var cacheKey = $"products:category:{category.ToLower()}";
            var cached = await _cache.GetAsync<List<Product>>(cacheKey);
            if (cached != null)
            {
                return cached;
            }

            var products = await _db.Products
                .AsNoTracking()
                .Where(p => p.Category == category)
                .ToListAsync();

            await _cache.SetAsync(cacheKey, products, DefaultCacheTtl);
            return products;
        }

        public async Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllProductsAsync();

            var searchLower = searchTerm.ToLower();
            return await _db.Products
                .AsNoTracking()
                .Where(p => p.Name.ToLower().Contains(searchLower) || p.Category.ToLower().Contains(searchLower))
                .ToListAsync();
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            var cacheKey = $"products:id:{id}";
            var cached = await _cache.GetAsync<Product>(cacheKey);
            if (cached != null)
            {
                return cached;
            }

            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product != null)
            {
                await _cache.SetAsync(cacheKey, product, DefaultCacheTtl);
            }
            return product;
        }

        public static string NormalizeCategory(string? cat)
        {
            if (string.IsNullOrWhiteSpace(cat)) return "Stationery";
            var c = cat.Trim();
            var lower = c.ToLowerInvariant();
            if (lower.Contains("writ") || lower.Contains("pen") || lower.Contains("ink") || lower.Contains("pencil") || lower.Contains("marker"))
                return "Writing";
            if (lower.Contains("note") || lower.Contains("journal") || lower.Contains("diary") || lower.Contains("pad") || lower.Contains("paper"))
                return "Notebooks";
            if (lower.Contains("desk") || lower.Contains("mat") || lower.Contains("organizer") || lower.Contains("sticky"))
                return "Desk Accessories";
            if (lower.Contains("art") || lower.Contains("paint") || lower.Contains("sketch") || lower.Contains("brush"))
                return "Art Supplies";
            if (lower.Contains("office") || lower.Contains("tape") || lower.Contains("stapler") || lower.Contains("clip"))
                return "Office Supplies";
            if (lower.Contains("school") || lower.Contains("draft") || lower.Contains("ruler") || lower.Contains("scissor"))
                return "School & Drafting";
            
            return char.ToUpper(c[0]) + (c.Length > 1 ? c.Substring(1).ToLower() : "");
        }

        public async Task<Product> CreateProductAsync(Product product)
        {
            product.Category = NormalizeCategory(product.Category);
            _db.Products.Add(product);
            await _db.SaveChangesAsync();
            await InvalidateProductCacheAsync();
            return product;
        }

        public async Task<Product> UpdateProductAsync(Product product)
        {
            var existingProduct = await _db.Products.FirstOrDefaultAsync(p => p.Id == product.Id);
            if (existingProduct == null)
                throw new InvalidOperationException("Product not found");

            existingProduct.Name = product.Name;
            existingProduct.Category = NormalizeCategory(product.Category);
            existingProduct.Price = product.Price;
            existingProduct.StockQuantity = product.StockQuantity;
            existingProduct.LowStockThreshold = product.LowStockThreshold;
            existingProduct.ImagePath = product.ImagePath;
            existingProduct.Description = product.Description;
            existingProduct.IsVisible = product.IsVisible;

            await _db.SaveChangesAsync();
            await InvalidateProductCacheAsync();
            return existingProduct;
        }

        public async Task DeleteProductAsync(int id)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product != null)
            {
                _db.Products.Remove(product);
                await _db.SaveChangesAsync();
                await InvalidateProductCacheAsync();
            }
        }

        public async Task<IEnumerable<string>> GetCategoriesAsync()
        {
            const string cacheKey = "products:categories";
            var cached = await _cache.GetAsync<List<string>>(cacheKey);
            if (cached != null && cached.Count > 0)
            {
                return cached;
            }

            var rawCategories = await _db.Products
                .Select(p => p.Category)
                .ToListAsync();

            var categories = rawCategories
                .Select(NormalizeCategory)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c)
                .ToList();

            if (categories.Count == 0)
            {
                categories = new List<string> { "Art Supplies", "Desk Accessories", "Notebooks", "Office Supplies", "School & Drafting", "Writing" };
            }

            await _cache.SetAsync(cacheKey, categories, DefaultCacheTtl);
            return categories;
        }

        public async Task<bool> IsProductInStockAsync(int productId, int quantity)
        {
            var product = await GetProductByIdAsync(productId);
            return product != null && product.StockQuantity >= quantity;
        }

        public async Task UpdateStockAsync(int productId, int quantity)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId);
            if (product != null)
            {
                product.StockQuantity -= quantity;
                await _db.SaveChangesAsync();
                await InvalidateProductCacheAsync();
            }
        }

        public async Task<IEnumerable<Product>> GetAvailableProductsAsync(bool includeOutOfStock = false)
        {
            if (includeOutOfStock)
                return await _db.Products.Where(p => p.IsVisible).ToListAsync();
            
            return await _db.Products
                .Where(p => p.StockQuantity > 0 && p.IsVisible)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetOutOfStockProductsAsync()
        {
            return await _db.Products
                .Where(p => p.StockQuantity <= 0 && p.IsVisible)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetLowStockProductsAsync()
        {
            return await _db.Products
                .Where(p => p.StockQuantity > 0 && p.StockQuantity <= p.LowStockThreshold && p.IsVisible)
                .ToListAsync();
        }

        public async Task<StockAlertSummary> GetStockAlertSummaryAsync()
        {
            var products = await _db.Products.ToListAsync();
            
            return new StockAlertSummary
            {
                TotalProducts = products.Count,
                InStockProducts = products.Count(p => p.StockQuantity > p.LowStockThreshold),
                LowStockProducts = products.Count(p => p.StockQuantity > 0 && p.StockQuantity <= p.LowStockThreshold),
                OutOfStockProducts = products.Count(p => p.StockQuantity <= 0),
                CriticalStockProducts = products.Count(p => p.StockQuantity == 1)
            };
        }

        public async Task ToggleProductVisibilityAsync(int productId, bool isVisible)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId);
            if (product != null)
            {
                product.IsVisible = isVisible;
                await _db.SaveChangesAsync();
                await InvalidateProductCacheAsync();
            }
        }

        public async Task<IEnumerable<Product>> GetTopProductsAsync(int count = 5)
        {
            var cacheKey = $"products:top:{count}";
            var cached = await _cache.GetAsync<List<Product>>(cacheKey);
            if (cached != null && cached.Count > 0)
            {
                return cached;
            }

            var topProducts = new List<Product>();

            try
            {
                // 1. Fetch top ordered product IDs based on sales order items
                var topOrderedIds = await _db.Orders
                    .SelectMany(o => o.OrderItems)
                    .GroupBy(oi => oi.ProductId)
                    .Select(g => new
                    {
                        ProductId = g.Key,
                        TotalQuantity = g.Sum(x => x.Quantity)
                    })
                    .OrderByDescending(x => x.TotalQuantity)
                    .Take(count)
                    .Select(x => x.ProductId)
                    .ToListAsync();

                if (topOrderedIds.Any())
                {
                    var orderedProducts = await _db.Products
                        .AsNoTracking()
                        .Where(p => topOrderedIds.Contains(p.Id) && p.IsVisible)
                        .ToListAsync();

                    // Preserve sales volume ranking order
                    topProducts.AddRange(topOrderedIds
                        .Select(id => orderedProducts.FirstOrDefault(p => p.Id == id))
                        .Where(p => p != null)!);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProductService] Error querying top ordered products: {ex.Message}");
            }

            // 2. If fewer than count products from orders, supplement with available visible products from DB
            if (topProducts.Count < count)
            {
                var existingIds = topProducts.Select(p => p.Id).ToList();
                var remainingCount = count - topProducts.Count;

                var supplementalProducts = await _db.Products
                    .AsNoTracking()
                    .Where(p => !existingIds.Contains(p.Id) && p.IsVisible && p.StockQuantity > 0)
                    .OrderByDescending(p => p.StockQuantity)
                    .ThenByDescending(p => p.Price)
                    .Take(remainingCount)
                    .ToListAsync();

                if (supplementalProducts.Count < remainingCount)
                {
                    var moreIds = existingIds.Concat(supplementalProducts.Select(p => p.Id)).ToList();
                    var extra = await _db.Products
                        .AsNoTracking()
                        .Where(p => !moreIds.Contains(p.Id) && p.IsVisible)
                        .OrderByDescending(p => p.Id)
                        .Take(remainingCount - supplementalProducts.Count)
                        .ToListAsync();
                    supplementalProducts.AddRange(extra);
                }

                topProducts.AddRange(supplementalProducts);
            }

            if (topProducts.Any())
            {
                await _cache.SetAsync(cacheKey, topProducts, TimeSpan.FromMinutes(15));
            }

            return topProducts;
        }

        public async Task<IEnumerable<Product>> GetVisibleProductsAsync()
        {
            return await _db.Products
                .Where(p => p.IsEffectivelyVisible)
                .ToListAsync();
        }
    }
}