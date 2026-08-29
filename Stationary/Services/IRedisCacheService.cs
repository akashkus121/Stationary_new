namespace Stationary.Services
{
    public interface IRedisCacheService
    {
        bool IsConnected { get; }
        Task<T?> GetAsync<T>(string key);
        Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null);
        Task<bool> RemoveAsync(string key);
        Task<bool> RemoveByPatternAsync(string pattern);
        // Refresh token operations
        Task StoreRefreshTokenAsync(int userId, string token, DateTime expiry);
        Task<bool> ValidateRefreshTokenAsync(int userId, string token);
        Task RevokeRefreshTokenAsync(int userId);

        // Message Queue Operations (Upstash Redis + Fallback)
        Task<long> EnqueueAsync<T>(string queueName, T item);
        Task<T?> DequeueAsync<T>(string queueName);
        Task<List<T>> GetQueueItemsAsync<T>(string queueName, int start = 0, int stop = -1);
        Task<long> GetQueueLengthAsync(string queueName);
    }
}
