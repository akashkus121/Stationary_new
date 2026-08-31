using Microsoft.EntityFrameworkCore;
using Stationary.Data;
using Stationary.Models;

namespace Stationary.Services
{
    public class ProductServiceWithSP : IProductService
    {
        private readonly ApplicationDbContext _db;

        public ProductServiceWithSP(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            return await _db.Products.AsNoTracking().ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetAvailableProductsAsync(bool includeOutOfStock = false, bool includeHidden = false)
        {
            if (includeHidden)
                return includeOutOfStock 
                    ? await _db.Products.AsNoTracking().ToListAsync()
                    : await _db.Products.AsNoTracking().Where(p => p.StockQuantity > 0).ToListAsync();

            if (includeOutOfStock)
                return await _db.Products.AsNoTracking().Where(p => p.IsVisible).ToListAsync();
            
            return await _db.Products
                .AsNoTracking()
                .Where(p => p.StockQuantity > 0 && p.IsVisible)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetOutOfStockProductsAsync()
        {
            return await _db.Products
                .AsNoTracking()
                .Where(p => p.StockQuantity <= 0 && p.IsVisible)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetLowStockProductsAsync()
        {
            return await _db.Products
                .AsNoTracking()
                .Where(p => p.StockQuantity > 0 && p.StockQuantity <= p.LowStockThreshold && p.IsVisible)
                .ToListAsync();
        }

        public async Task<StockAlertSummary> GetStockAlertSummaryAsync()
        {
            var products = await _db.Products.AsNoTracking().ToListAsync();
            
            return new StockAlertSummary
            {
                TotalProducts = products.Count,
                InStockProducts = products.Count(p => p.StockQuantity > p.LowStockThreshold),
                LowStockProducts = products.Count(p => p.StockQuantity > 0 && p.StockQuantity <= p.LowStockThreshold),
                OutOfStockProducts = products.Count(p => p.StockQuantity <= 0),
                CriticalStockProducts = products.Count(p => p.StockQuantity == 1)
            };
        }

        public async Task<IEnumerable<string>> GetCategoriesAsync()
        {
            return await _db.Products
                .Select(p => p.Category)
                .Distinct()
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(string category)
        {
            return await _db.Products
                .AsNoTracking()
                .Where(p => p.Category == category && p.IsVisible)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllProductsAsync();

            var term = searchTerm.ToLower();
            return await _db.Products
                .AsNoTracking()
                .Where(p => (p.Name.ToLower().Contains(term) || p.Category.ToLower().Contains(term)) && p.IsVisible)
                .ToListAsync();
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            return await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Product> CreateProductAsync(Product product)
        {
            _db.Products.Add(product);
            await _db.SaveChangesAsync();
            return product;
        }

        public async Task<Product> UpdateProductAsync(Product product)
        {
            var existingProduct = await _db.Products.FirstOrDefaultAsync(p => p.Id == product.Id);
            if (existingProduct == null)
                throw new InvalidOperationException("Product not found");

            existingProduct.Name = product.Name;
            existingProduct.Category = product.Category;
            existingProduct.Price = product.Price;
            existingProduct.StockQuantity = product.StockQuantity;
            existingProduct.LowStockThreshold = product.LowStockThreshold;
            existingProduct.ImagePath = product.ImagePath;
            existingProduct.IsVisible = product.IsVisible;

            await _db.SaveChangesAsync();
            return existingProduct;
        }

        public async Task DeleteProductAsync(int id)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product != null)
            {
                _db.Products.Remove(product);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<bool> IsProductInStockAsync(int productId, int quantity)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId);
            return product != null && product.StockQuantity >= quantity;
        }

        public async Task UpdateStockAsync(int productId, int quantity)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId);
            if (product != null)
            {
                product.StockQuantity -= quantity;
                await _db.SaveChangesAsync();
            }
        }

        public async Task ToggleProductVisibilityAsync(int productId, bool isVisible)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId);
            if (product != null)
            {
                product.IsVisible = isVisible;
                await _db.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Product>> GetTopProductsAsync(int count = 5)
        {
            var topProducts = new List<Product>();
            try
            {
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

                    topProducts.AddRange(topOrderedIds
                        .Select(id => orderedProducts.FirstOrDefault(p => p.Id == id))
                        .Where(p => p != null)!);
                }
            }
            catch { }

            if (topProducts.Count < count)
            {
                var existingIds = topProducts.Select(p => p.Id).ToList();
                var remainingCount = count - topProducts.Count;
                var supplemental = await _db.Products
                    .AsNoTracking()
                    .Where(p => !existingIds.Contains(p.Id) && p.IsVisible && p.StockQuantity > 0)
                    .OrderByDescending(p => p.StockQuantity)
                    .Take(remainingCount)
                    .ToListAsync();
                topProducts.AddRange(supplemental);
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