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
        Task<T?> BlockingDequeueAsync<T>(string queueName, TimeSpan? timeout = null);
        Task<T?> DequeueWithReliableProcessingAsync<T>(string sourceQueue, string processingQueue, TimeSpan? timeout = null);
        Task<bool> AcknowledgeAsync<T>(string processingQueue, T item);
        Task<long> RequeueFailedAsync<T>(string processingQueue, string sourceQueue, T item);
        Task<long> RecoverProcessingQueueAsync<T>(string processingQueue, string sourceQueue);
        Task<List<T>> GetQueueItemsAsync<T>(string queueName, int start = 0, int stop = -1);
        Task<long> GetQueueLengthAsync(string queueName);
    }
}
