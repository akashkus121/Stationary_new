using Stationary.Models;

namespace Stationary.Services
{
    public interface ICartService
    {
        Task<IEnumerable<Cart>> GetUserCartAsync(int userId);
        Task<Cart> AddToCartAsync(int userId, int productId, int quantity);
        Task<Cart> UpdateCartItemQuantityAsync(int userId, int productId, int quantity);
        Task RemoveFromCartAsync(int userId, int productId);
        Task ClearUserCartAsync(int userId);
        Task<int> GetCartItemCountAsync(int userId);
        Task<decimal> GetCartTotalAsync(int userId);
        Task<bool> ValidateCartStockAsync(int userId);
    }
}