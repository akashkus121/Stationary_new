using System.Text.Json;
using Stationary.Models;

namespace Stationary.Services
{
    public class PendingActionItem
    {
        public string ActionId { get; set; } = Guid.NewGuid().ToString();
        public string ActionType { get; set; } = string.Empty; // "bulk_stock_update", "bulk_create", "create_product", "update_product"
        public string PayloadJson { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public int RetryCount { get; set; } = 0;
    }

    public interface IOfflineFallbackQueueService
    {
        Task SaveProductCacheAsync(IEnumerable<Product> products);
        Task<List<Product>> GetProductCacheAsync();
        Task EnqueuePendingActionAsync(string actionType, object payload);
        Task<List<PendingActionItem>> GetPendingActionsAsync();
        Task RemovePendingActionAsync(string actionId);
        Task ClearPendingActionsAsync();
        Task<bool> HasPendingActionsAsync();
    }

    public class OfflineFallbackQueueService : IOfflineFallbackQueueService
    {
        private readonly string _cacheFilePath;
        private readonly string _queueFilePath;
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public OfflineFallbackQueueService(IWebHostEnvironment env)
        {
            var folder = Path.Combine(env.ContentRootPath, "wwwroot", "cache");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            _cacheFilePath = Path.Combine(folder, "products_cache.json");
            _queueFilePath = Path.Combine(folder, "pending_queue.json");
        }

        public async Task SaveProductCacheAsync(IEnumerable<Product> products)
        {
            await _semaphore.WaitAsync();
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(products, options);
                await File.WriteAllTextAsync(_cacheFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FallbackCache] Failed to save product cache: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<List<Product>> GetProductCacheAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                if (!File.Exists(_cacheFilePath))
                    return new List<Product>();

                var json = await File.ReadAllTextAsync(_cacheFilePath);
                var items = JsonSerializer.Deserialize<List<Product>>(json);
                return items ?? new List<Product>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FallbackCache] Failed to read product cache: {ex.Message}");
                return new List<Product>();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task EnqueuePendingActionAsync(string actionType, object payload)
        {
            await _semaphore.WaitAsync();
            try
            {
                var currentQueue = new List<PendingActionItem>();
                if (File.Exists(_queueFilePath))
                {
                    var existingJson = await File.ReadAllTextAsync(_queueFilePath);
                    currentQueue = JsonSerializer.Deserialize<List<PendingActionItem>>(existingJson) ?? new List<PendingActionItem>();
                }

                currentQueue.Add(new PendingActionItem
                {
                    ActionType = actionType,
                    PayloadJson = JsonSerializer.Serialize(payload),
                    Timestamp = DateTime.UtcNow
                });

                var options = new JsonSerializerOptions { WriteIndented = true };
                await File.WriteAllTextAsync(_queueFilePath, JsonSerializer.Serialize(currentQueue, options));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FallbackQueue] Failed to enqueue action: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<List<PendingActionItem>> GetPendingActionsAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                if (!File.Exists(_queueFilePath))
                    return new List<PendingActionItem>();

                var json = await File.ReadAllTextAsync(_queueFilePath);
                var items = JsonSerializer.Deserialize<List<PendingActionItem>>(json);
                return items ?? new List<PendingActionItem>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FallbackQueue] Failed to read pending queue: {ex.Message}");
                return new List<PendingActionItem>();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task RemovePendingActionAsync(string actionId)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (!File.Exists(_queueFilePath)) return;

                var json = await File.ReadAllTextAsync(_queueFilePath);
                var items = JsonSerializer.Deserialize<List<PendingActionItem>>(json) ?? new List<PendingActionItem>();
                items.RemoveAll(i => i.ActionId == actionId);

                var options = new JsonSerializerOptions { WriteIndented = true };
                await File.WriteAllTextAsync(_queueFilePath, JsonSerializer.Serialize(items, options));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FallbackQueue] Failed to remove action: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task ClearPendingActionsAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                if (File.Exists(_queueFilePath))
                {
                    File.Delete(_queueFilePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FallbackQueue] Failed to clear queue: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<bool> HasPendingActionsAsync()
        {
            var actions = await GetPendingActionsAsync();
            return actions.Any();
        }
    }
}
