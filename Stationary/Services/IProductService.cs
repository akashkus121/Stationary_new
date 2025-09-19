using Stationary.Models;

namespace Stationary.Services
{
    public interface IProductService
    {
        Task<IEnumerable<Product>> GetAllProductsAsync();
        Task<IEnumerable<Product>> GetProductsByCategoryAsync(string category);
        Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm);
        Task<Product?> GetProductByIdAsync(int id);
        Task<Product> CreateProductAsync(Product product);
        Task<Product> UpdateProductAsync(Product product);
        Task DeleteProductAsync(int id);
        Task<IEnumerable<string>> GetCategoriesAsync();
        Task<bool> IsProductInStockAsync(int productId, int quantity);
        Task UpdateStockAsync(int productId, int quantity);
        Task<IEnumerable<Product>> GetAvailableProductsAsync(bool includeOutOfStock = false);
        Task<IEnumerable<Product>> GetOutOfStockProductsAsync();
        Task<IEnumerable<Product>> GetLowStockProductsAsync();
        Task<StockAlertSummary> GetStockAlertSummaryAsync();
        Task ToggleProductVisibilityAsync(int productId, bool isVisible);
        Task<IEnumerable<Product>> GetVisibleProductsAsync();
    }
}