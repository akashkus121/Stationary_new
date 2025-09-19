using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Data;
using Stationary.Data;
using Stationary.Models;

namespace Stationary.Services
{
    public class ProductServiceWithSP : IProductService
    {
        private readonly ApplicationDbContext _db;
        private readonly bool _useStoredProcedures;

        public ProductServiceWithSP(ApplicationDbContext db, bool useStoredProcedures = false)
        {
            _db = db;
            _useStoredProcedures = useStoredProcedures;
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            if (_useStoredProcedures)
                return await GetProductsWithSPAsync("all");
            
            return await _db.Products.ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetAvailableProductsAsync(bool includeOutOfStock = false)
        {
            if (_useStoredProcedures)
            {
                var status = includeOutOfStock ? "all" : "available";
                return await GetProductsWithSPAsync(status);
            }

            if (includeOutOfStock)
                return await _db.Products.Where(p => p.IsVisible).ToListAsync();
            
            return await _db.Products
                .Where(p => p.StockQuantity > 0 && p.IsVisible)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetOutOfStockProductsAsync()
        {
            if (_useStoredProcedures)
                return await GetProductsWithSPAsync("outofstock");
            
            return await _db.Products
                .Where(p => p.StockQuantity <= 0 && p.IsVisible)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetLowStockProductsAsync()
        {
            if (_useStoredProcedures)
                return await GetProductsWithSPAsync("lowstock");
            
            return await _db.Products
                .Where(p => p.StockQuantity > 0 && p.StockQuantity <= p.LowStockThreshold && p.IsVisible)
                .ToListAsync();
        }

        public async Task<StockAlertSummary> GetStockAlertSummaryAsync()
        {
            if (_useStoredProcedures)
                return await GetStockAlertSummaryWithSPAsync();
            
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

        public async Task<IEnumerable<string>> GetCategoriesAsync()
        {
            return await _db.Products
                .Select(p => p.Category)
                .Distinct()
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(string category)
        {
            if (_useStoredProcedures)
                return await GetProductsWithSPAsync("all", category);
            
            return await _db.Products
                .Where(p => p.Category == category && p.IsVisible)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm)
        {
            if (_useStoredProcedures)
                return await GetProductsWithSPAsync("all", searchTerm: searchTerm);
            
            return await _db.Products
                .Where(p => p.Name.Contains(searchTerm) && p.IsVisible)
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

        public async Task<IEnumerable<Product>> GetVisibleProductsAsync()
        {
            return await _db.Products
                .Where(p => p.IsEffectivelyVisible)
                .ToListAsync();
        }

        // Stored Procedure Methods
        private async Task<IEnumerable<Product>> GetProductsWithSPAsync(string stockStatus, string? category = null, string? searchTerm = null, int page = 1, int pageSize = 20)
        {
            var products = new List<Product>();
            
            try
            {
                using var connection = new SqlConnection(_db.Database.GetConnectionString());
                using var command = new SqlCommand("usp_GetProductsByStockStatus", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                command.Parameters.AddWithValue("@StockStatus", stockStatus);
                command.Parameters.AddWithValue("@Category", category ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@SearchTerm", searchTerm ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Page", page);
                command.Parameters.AddWithValue("@PageSize", pageSize);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    products.Add(new Product
                    {
                        Id = reader.GetInt32("Id"),
                        Name = reader.GetString("Name"),
                        Category = reader.GetString("Category"),
                        Price = reader.GetDecimal("Price"),
                        ImagePath = reader.IsDBNull("ImagePath") ? null : reader.GetString("ImagePath"),
                        StockQuantity = reader.GetInt32("StockQuantity"),
                        LowStockThreshold = reader.GetInt32("LowStockThreshold"),
                        IsVisible = reader.GetBoolean("IsVisible")
                    });
                }
            }
            catch (Exception ex)
            {
                // Fallback to EF Core if SP fails
                return await GetAvailableProductsAsync(stockStatus == "all");
            }

            return products;
        }

        private async Task<StockAlertSummary> GetStockAlertSummaryWithSPAsync()
        {
            try
            {
                using var connection = new SqlConnection(_db.Database.GetConnectionString());
                using var command = new SqlCommand("usp_GetStockAlertSummary", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    return new StockAlertSummary
                    {
                        TotalProducts = reader.GetInt32("TotalProducts"),
                        InStockProducts = reader.GetInt32("InStockProducts"),
                        LowStockProducts = reader.GetInt32("LowStockProducts"),
                        OutOfStockProducts = reader.GetInt32("OutOfStockProducts"),
                        CriticalStockProducts = reader.GetInt32("CriticalStockProducts")
                    };
                }
            }
            catch (Exception ex)
            {
                // Fallback to EF Core if SP fails
                return await GetStockAlertSummaryAsync();
            }

            return new StockAlertSummary();
        }
    }
}