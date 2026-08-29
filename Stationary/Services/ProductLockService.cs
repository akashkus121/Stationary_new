using System.Collections.Concurrent;

namespace Stationary.Services
{
    public interface IProductLockService
    {
        Task<T> ExecuteWithLockAsync<T>(int productId, Func<Task<T>> action, int timeoutMs = 3000);
        Task ExecuteWithLockAsync(int productId, Func<Task> action, int timeoutMs = 3000);
    }

    public class ProductLockService : IProductLockService
    {
        private static readonly ConcurrentDictionary<int, SemaphoreSlim> _productLocks = new ConcurrentDictionary<int, SemaphoreSlim>();

        private SemaphoreSlim GetLockForProduct(int productId)
        {
            return _productLocks.GetOrAdd(productId, _ => new SemaphoreSlim(1, 1));
        }

        public async Task<T> ExecuteWithLockAsync<T>(int productId, Func<Task<T>> action, int timeoutMs = 3000)
        {
            var semaphore = GetLockForProduct(productId);
            bool acquired = await semaphore.WaitAsync(timeoutMs);

            if (!acquired)
            {
                throw new TimeoutException($"Concurrency Lock: Another user is currently reserving or modifying product #{productId}. Please try again.");
            }

            try
            {
                return await action();
            }
            finally
            {
                semaphore.Release();
            }
        }

        public async Task ExecuteWithLockAsync(int productId, Func<Task> action, int timeoutMs = 3000)
        {
            var semaphore = GetLockForProduct(productId);
            bool acquired = await semaphore.WaitAsync(timeoutMs);

            if (!acquired)
            {
                throw new TimeoutException($"Concurrency Lock: Another user is currently reserving or modifying product #{productId}. Please try again.");
            }

            try
            {
                await action();
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}
