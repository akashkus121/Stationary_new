using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Stationary.Services
{
    public class RedisCacheService : IRedisCacheService
    {
        private readonly IConnectionMultiplexer? _redis;
        private readonly IDatabase? _db;
        private readonly ILogger<RedisCacheService> _logger;
        private readonly HttpClient _httpClient;
        private readonly string? _upstashRestUrl;
        private readonly string? _upstashRestToken;
        private const string KeyPrefix = "stationary:";
        
        // In-memory fallback cache in case Redis is temporarily unreachable
        private static readonly ConcurrentDictionary<string, (object Value, DateTime Expiry)> _memoryFallback = new();

        public RedisCacheService(IConfiguration configuration, ILogger<RedisCacheService> logger, IConnectionMultiplexer? redis = null)
        {
            _logger = logger;
            _httpClient = new HttpClient();

            _upstashRestUrl = configuration["Upstash:RestUrl"]?.TrimEnd('/');
            _upstashRestToken = configuration["Upstash:RestToken"];

            if (!string.IsNullOrEmpty(_upstashRestToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _upstashRestToken);
            }

            try
            {
                _redis = redis;
                if (_redis != null && _redis.IsConnected)
                {
                    _db = _redis.GetDatabase();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis TCP connection failed at startup. Will use Upstash REST / in-memory fallback.");
            }
        }

        public bool IsConnected => (_redis != null && _redis.IsConnected) || !string.IsNullOrEmpty(_upstashRestUrl);

        private static string FormatKey(string key) => key.StartsWith(KeyPrefix) ? key : $"{KeyPrefix}{key}";

        public async Task<T?> GetAsync<T>(string key)
        {
            var fullKey = FormatKey(key);

            // 1. Try TCP StackExchange.Redis
            try
            {
                if (_redis != null && _redis.IsConnected && _db != null)
                {
                    var val = await _db.StringGetAsync(fullKey);
                    if (val.HasValue)
                    {
                        return JsonSerializer.Deserialize<T>(val.ToString()!);
                    }
                    return default;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis TCP GET error for key {Key}, falling back to REST/memory", fullKey);
            }

            // 2. Try Upstash REST API
            if (!string.IsNullOrEmpty(_upstashRestUrl) && !string.IsNullOrEmpty(_upstashRestToken))
            {
                try
                {
                    var res = await _httpClient.GetAsync($"{_upstashRestUrl}/get/{Uri.EscapeDataString(fullKey)}");
                    if (res.IsSuccessStatusCode)
                    {
                        using var stream = await res.Content.ReadAsStreamAsync();
                        using var doc = await JsonDocument.ParseAsync(stream);
                        if (doc.RootElement.TryGetProperty("result", out var resultEl) && resultEl.ValueKind == JsonValueKind.String)
                        {
                            var jsonString = resultEl.GetString();
                            if (!string.IsNullOrEmpty(jsonString))
                            {
                                return JsonSerializer.Deserialize<T>(jsonString);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Upstash REST GET error for key {Key}", fullKey);
                }
            }

            // 3. In-memory fallback check
            if (_memoryFallback.TryGetValue(fullKey, out var cached))
            {
                if (cached.Expiry > DateTime.UtcNow)
                {
                    if (cached.Value is T directVal) return directVal;
                    if (cached.Value is string jsonStr)
                    {
                        return JsonSerializer.Deserialize<T>(jsonStr);
                    }
                }
                else
                {
                    _memoryFallback.TryRemove(fullKey, out _);
                }
            }

            return default;
        }

        public async Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            var fullKey = FormatKey(key);
            var expiration = expiry ?? TimeSpan.FromMinutes(30);
            var serialized = JsonSerializer.Serialize(value);

            // Always update memory fallback for ultra-resilience
            _memoryFallback[fullKey] = (serialized, DateTime.UtcNow.Add(expiration));

            // 1. Try TCP StackExchange.Redis
            try
            {
                if (_redis != null && _redis.IsConnected && _db != null)
                {
                    return await _db.StringSetAsync(fullKey, serialized, expiration);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis TCP SET error for key {Key}, falling back to REST", fullKey);
            }

            // 2. Try Upstash REST API
            if (!string.IsNullOrEmpty(_upstashRestUrl) && !string.IsNullOrEmpty(_upstashRestToken))
            {
                try
                {
                    var seconds = (int)expiration.TotalSeconds;
                    var res = await _httpClient.PostAsync(
                        $"{_upstashRestUrl}/set/{Uri.EscapeDataString(fullKey)}/{Uri.EscapeDataString(serialized)}?EX={seconds}",
                        null);

                    if (res.IsSuccessStatusCode) return true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Upstash REST SET error for key {Key}", fullKey);
                }
            }

            return true;
        }

        public async Task<bool> RemoveAsync(string key)
        {
            var fullKey = FormatKey(key);
            _memoryFallback.TryRemove(fullKey, out _);

            try
            {
                if (_redis != null && _redis.IsConnected && _db != null)
                {
                    await _db.KeyDeleteAsync(fullKey);
                }
            }
            catch { }

            if (!string.IsNullOrEmpty(_upstashRestUrl) && !string.IsNullOrEmpty(_upstashRestToken))
            {
                try
                {
                    await _httpClient.GetAsync($"{_upstashRestUrl}/del/{Uri.EscapeDataString(fullKey)}");
                }
                catch { }
            }

            return true;
        }

        public async Task<bool> RemoveByPatternAsync(string pattern)
        {
            var fullPattern = FormatKey(pattern);
            var prefix = fullPattern.Replace("*", "");
            
            foreach (var key in _memoryFallback.Keys.Where(k => k.StartsWith(prefix)))
            {
                _memoryFallback.TryRemove(key, out _);
            }

            try
            {
                if (_redis != null && _redis.IsConnected && _db != null)
                {
                    var endpoints = _redis.GetEndPoints();
                    foreach (var endpoint in endpoints)
                    {
                        var server = _redis.GetServer(endpoint);
                        var keys = server.Keys(pattern: fullPattern).ToArray();
                        if (keys.Length > 0)
                        {
                            await _db.KeyDeleteAsync(keys);
                        }
                    }
                }
            }
            catch { }

            if (!string.IsNullOrEmpty(_upstashRestUrl) && !string.IsNullOrEmpty(_upstashRestToken))
            {
                try
                {
                    // Search keys via REST
                    var res = await _httpClient.GetAsync($"{_upstashRestUrl}/keys/{Uri.EscapeDataString(fullPattern)}");
                    if (res.IsSuccessStatusCode)
                    {
                        using var stream = await res.Content.ReadAsStreamAsync();
                        using var doc = await JsonDocument.ParseAsync(stream);
                        if (doc.RootElement.TryGetProperty("result", out var resultEl) && resultEl.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var keyEl in resultEl.EnumerateArray())
                            {
                                var k = keyEl.GetString();
                                if (!string.IsNullOrEmpty(k))
                                {
                                    await _httpClient.GetAsync($"{_upstashRestUrl}/del/{Uri.EscapeDataString(k)}");
                                }
                            }
                        }
                    }
                }
                catch { }
            }

            return true;
        }

        public async Task StoreRefreshTokenAsync(int userId, string token, DateTime expiry)
        {
            var key = $"refreshtoken:{userId}";
            var ttl = expiry - DateTime.UtcNow;
            if (ttl <= TimeSpan.Zero) return;

            await SetAsync(key, token, ttl);
        }

        public async Task<bool> ValidateRefreshTokenAsync(int userId, string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;

            var key = $"refreshtoken:{userId}";
            var storedToken = await GetAsync<string>(key);
            
            return !string.IsNullOrEmpty(storedToken) && storedToken == token;
        }

        public async Task RevokeRefreshTokenAsync(int userId)
        {
            var key = $"refreshtoken:{userId}";
            await RemoveAsync(key);
        }

        // =====================================================================
        // Message Queue Implementation (Upstash Redis + In-Memory Fallback)
        // With BRPOP & Reliable Queue Pattern (BRPOPLPUSH / Processing List)
        // =====================================================================
        private static readonly ConcurrentDictionary<string, List<string>> _memoryQueueFallback = new();
        private static readonly object _queueLock = new();

        public async Task<long> EnqueueAsync<T>(string queueName, T item)
        {
            var fullKey = FormatKey(queueName);
            var serialized = JsonSerializer.Serialize(item);

            // 1. In-memory queue fallback (LPUSH: insert at index 0)
            lock (_queueLock)
            {
                var list = _memoryQueueFallback.GetOrAdd(fullKey, _ => new List<string>());
                list.Insert(0, serialized);
            }

            // 2. Try TCP Redis LPUSH
            try
            {
                if (_redis != null && _redis.IsConnected && _db != null)
                {
                    return await _db.ListLeftPushAsync(fullKey, serialized);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis TCP LPUSH error for queue {QueueName}, falling back to REST/memory", fullKey);
            }

            // 3. Try Upstash REST LPUSH
            if (!string.IsNullOrEmpty(_upstashRestUrl) && !string.IsNullOrEmpty(_upstashRestToken))
            {
                try
                {
                    var res = await _httpClient.PostAsync(
                        $"{_upstashRestUrl}/lpush/{Uri.EscapeDataString(fullKey)}/{Uri.EscapeDataString(serialized)}",
                        null);

                    if (res.IsSuccessStatusCode)
                    {
                        using var stream = await res.Content.ReadAsStreamAsync();
                        using var doc = await JsonDocument.ParseAsync(stream);
                        if (doc.RootElement.TryGetProperty("result", out var resultEl) && resultEl.TryGetInt64(out var len))
                        {
                            return len;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Upstash REST LPUSH error for queue {QueueName}", fullKey);
                }
            }

            lock (_queueLock)
            {
                return _memoryQueueFallback.TryGetValue(fullKey, out var list) ? list.Count : 0;
            }
        }

        public async Task<T?> DequeueAsync<T>(string queueName)
        {
            // Default Dequeue now delegates to BlockingDequeueAsync with a 1-second timeout (BRPOP)
            return await BlockingDequeueAsync<T>(queueName, TimeSpan.FromSeconds(1));
        }

        public async Task<T?> BlockingDequeueAsync<T>(string queueName, TimeSpan? timeout = null)
        {
            var fullKey = FormatKey(queueName);
            var timeoutSec = Math.Max(1, (int)(timeout?.TotalSeconds ?? 2));

            // 1. Try TCP Redis BRPOP
            try
            {
                if (_redis != null && _redis.IsConnected && _db != null)
                {
                    var result = await _db.ExecuteAsync("BRPOP", fullKey, timeoutSec);
                    if (!result.IsNull)
                    {
                        string? jsonString = null;
                        if (result.Type == ResultType.MultiBulk || result.Type == ResultType.Array)
                        {
                            var arr = (RedisResult[])result!;
                            if (arr.Length >= 2)
                            {
                                jsonString = arr[1].ToString();
                            }
                        }
                        else
                        {
                            jsonString = result.ToString();
                        }

                        if (!string.IsNullOrEmpty(jsonString))
                        {
                            // Remove from memory fallback if present
                            lock (_queueLock)
                            {
                                if (_memoryQueueFallback.TryGetValue(fullKey, out var memList) && memList.Count > 0)
                                {
                                    memList.RemoveAt(memList.Count - 1);
                                }
                            }
                            return JsonSerializer.Deserialize<T>(jsonString);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis TCP BRPOP error for queue {QueueName}, falling back to REST/memory", fullKey);
            }

            // 2. Try Upstash REST BRPOP / RPOP
            if (!string.IsNullOrEmpty(_upstashRestUrl) && !string.IsNullOrEmpty(_upstashRestToken))
            {
                try
                {
                    var res = await _httpClient.PostAsync(
                        $"{_upstashRestUrl}/brpop/{Uri.EscapeDataString(fullKey)}/{timeoutSec}",
                        null);

                    if (res.IsSuccessStatusCode)
                    {
                        using var stream = await res.Content.ReadAsStreamAsync();
                        using var doc = await JsonDocument.ParseAsync(stream);
                        if (doc.RootElement.TryGetProperty("result", out var resultEl))
                        {
                            string? json = null;
                            if (resultEl.ValueKind == JsonValueKind.Array && resultEl.GetArrayLength() >= 2)
                            {
                                json = resultEl[1].GetString();
                            }
                            else if (resultEl.ValueKind == JsonValueKind.String)
                            {
                                json = resultEl.GetString();
                            }

                            if (!string.IsNullOrEmpty(json))
                            {
                                lock (_queueLock)
                                {
                                    if (_memoryQueueFallback.TryGetValue(fullKey, out var memList) && memList.Count > 0)
                                    {
                                        memList.RemoveAt(memList.Count - 1);
                                    }
                                }
                                return JsonSerializer.Deserialize<T>(json);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Upstash REST BRPOP error for queue {QueueName}", fullKey);
                }
            }

            // 3. Fallback to in-memory queue (pop right/tail)
            lock (_queueLock)
            {
                if (_memoryQueueFallback.TryGetValue(fullKey, out var memList) && memList.Count > 0)
                {
                    var item = memList[memList.Count - 1];
                    memList.RemoveAt(memList.Count - 1);
                    return JsonSerializer.Deserialize<T>(item);
                }
            }

            return default;
        }

        /// <summary>
        /// Reliable Queue Pattern: Atomically moves item from sourceQueue to processingQueue via BRPOPLPUSH / RPOPLPUSH.
        /// This ensures ZERO DATA LOSS: if the worker crashes before finishing database operations,
        /// the item remains safe inside the processingQueue and can be recovered.
        /// </summary>
        public async Task<T?> DequeueWithReliableProcessingAsync<T>(string sourceQueue, string processingQueue, TimeSpan? timeout = null)
        {
            var fullSourceKey = FormatKey(sourceQueue);
            var fullProcKey = FormatKey(processingQueue);
            var timeoutSec = Math.Max(1, (int)(timeout?.TotalSeconds ?? 2));

            // 1. Try TCP StackExchange.Redis (BRPOPLPUSH / ListRightPopLeftPushAsync)
            try
            {
                if (_redis != null && _redis.IsConnected && _db != null)
                {
                    // Execute BRPOPLPUSH
                    var result = await _db.ExecuteAsync("BRPOPLPUSH", fullSourceKey, fullProcKey, timeoutSec);
                    if (!result.IsNull && !string.IsNullOrEmpty(result.ToString()))
                    {
                        var json = result.ToString()!;
                        lock (_queueLock)
                        {
                            // Sync in-memory representation
                            if (_memoryQueueFallback.TryGetValue(fullSourceKey, out var srcList) && srcList.Count > 0)
                            {
                                srcList.RemoveAt(srcList.Count - 1);
                            }
                            var procList = _memoryQueueFallback.GetOrAdd(fullProcKey, _ => new List<string>());
                            procList.Insert(0, json);
                        }
                        return JsonSerializer.Deserialize<T>(json);
                    }

                    // Fallback to ListRightPopLeftPushAsync if BRPOPLPUSH command returned null or empty
                    var nonBlockingVal = await _db.ListRightPopLeftPushAsync(fullSourceKey, fullProcKey);
                    if (nonBlockingVal.HasValue && !string.IsNullOrEmpty(nonBlockingVal.ToString()))
                    {
                        var json = nonBlockingVal.ToString()!;
                        lock (_queueLock)
                        {
                            if (_memoryQueueFallback.TryGetValue(fullSourceKey, out var srcList) && srcList.Count > 0)
                            {
                                srcList.RemoveAt(srcList.Count - 1);
                            }
                            var procList = _memoryQueueFallback.GetOrAdd(fullProcKey, _ => new List<string>());
                            procList.Insert(0, json);
                        }
                        return JsonSerializer.Deserialize<T>(json);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis TCP BRPOPLPUSH error between {Source} and {Proc}, falling back to REST/memory", fullSourceKey, fullProcKey);
            }

            // 2. Try Upstash REST BRPOPLPUSH / RPOPLPUSH
            if (!string.IsNullOrEmpty(_upstashRestUrl) && !string.IsNullOrEmpty(_upstashRestToken))
            {
                try
                {
                    // Try BRPOPLPUSH / RPOPLPUSH
                    var res = await _httpClient.PostAsync(
                        $"{_upstashRestUrl}/brpoplpush/{Uri.EscapeDataString(fullSourceKey)}/{Uri.EscapeDataString(fullProcKey)}/{timeoutSec}",
                        null);

                    if (!res.IsSuccessStatusCode)
                    {
                        // Fallback to REST rpoplpush
                        res = await _httpClient.PostAsync(
                            $"{_upstashRestUrl}/rpoplpush/{Uri.EscapeDataString(fullSourceKey)}/{Uri.EscapeDataString(fullProcKey)}",
                            null);
                    }

                    if (res.IsSuccessStatusCode)
                    {
                        using var stream = await res.Content.ReadAsStreamAsync();
                        using var doc = await JsonDocument.ParseAsync(stream);
                        if (doc.RootElement.TryGetProperty("result", out var resultEl) && resultEl.ValueKind == JsonValueKind.String)
                        {
                            var json = resultEl.GetString();
                            if (!string.IsNullOrEmpty(json))
                            {
                                lock (_queueLock)
                                {
                                    if (_memoryQueueFallback.TryGetValue(fullSourceKey, out var srcList) && srcList.Count > 0)
                                    {
                                        srcList.RemoveAt(srcList.Count - 1);
                                    }
                                    var procList = _memoryQueueFallback.GetOrAdd(fullProcKey, _ => new List<string>());
                                    procList.Insert(0, json);
                                }
                                return JsonSerializer.Deserialize<T>(json);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Upstash REST RPOPLPUSH error between {Source} and {Proc}", fullSourceKey, fullProcKey);
                }
            }

            // 3. Fallback to in-memory reliable transfer
            lock (_queueLock)
            {
                if (_memoryQueueFallback.TryGetValue(fullSourceKey, out var srcList) && srcList.Count > 0)
                {
                    var item = srcList[srcList.Count - 1];
                    srcList.RemoveAt(srcList.Count - 1);

                    var procList = _memoryQueueFallback.GetOrAdd(fullProcKey, _ => new List<string>());
                    procList.Insert(0, item);

                    return JsonSerializer.Deserialize<T>(item);
                }
            }

            return default;
        }

        /// <summary>
        /// Acknowledges completion of an item by removing it from the processing queue via LREM.
        /// Call this ONLY AFTER database SaveChangesAsync() has succeeded.
        /// </summary>
        public async Task<bool> AcknowledgeAsync<T>(string processingQueue, T item)
        {
            var fullProcKey = FormatKey(processingQueue);
            var serialized = JsonSerializer.Serialize(item);

            // 1. Remove from in-memory fallback
            lock (_queueLock)
            {
                if (_memoryQueueFallback.TryGetValue(fullProcKey, out var procList))
                {
                    // Find and remove matching element (or first matching serialization)
                    var index = procList.FindIndex(s => s == serialized);
                    if (index >= 0)
                    {
                        procList.RemoveAt(index);
                    }
                    else if (procList.Count > 0)
                    {
                        // Match on QueueId if available via JSON
                        try
                        {
                            using var docItem = JsonDocument.Parse(serialized);
                            if (docItem.RootElement.TryGetProperty("QueueId", out var qIdProp) ||
                                docItem.RootElement.TryGetProperty("queueId", out qIdProp))
                            {
                                var qIdStr = qIdProp.GetString();
                                procList.RemoveAll(s => s.Contains(qIdStr ?? "___nonexistent___"));
                            }
                        }
                        catch { }
                    }
                }
            }

            // 2. Try TCP Redis LREM
            try
            {
                if (_redis != null && _redis.IsConnected && _db != null)
                {
                    await _db.ListRemoveAsync(fullProcKey, serialized, 1);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis TCP LREM error for processing queue {Proc}", fullProcKey);
            }

            // 3. Try Upstash REST LREM
            if (!string.IsNullOrEmpty(_upstashRestUrl) && !string.IsNullOrEmpty(_upstashRestToken))
            {
                try
                {
                    await _httpClient.PostAsync(
                        $"{_upstashRestUrl}/lrem/{Uri.EscapeDataString(fullProcKey)}/1/{Uri.EscapeDataString(serialized)}",
                        null);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Upstash REST LREM error for processing queue {Proc}", fullProcKey);
                }
            }

            return true;
        }

        /// <summary>
        /// Moves an uncommitted item from processingQueue back to sourceQueue on error.
        /// </summary>
        public async Task<long> RequeueFailedAsync<T>(string processingQueue, string sourceQueue, T item)
        {
            await AcknowledgeAsync(processingQueue, item);
            return await EnqueueAsync(sourceQueue, item);
        }

        /// <summary>
        /// On startup or retry cycle, recovers all stranded items in processingQueue back into sourceQueue (RPOPLPUSH).
        /// </summary>
        public async Task<long> RecoverProcessingQueueAsync<T>(string processingQueue, string sourceQueue)
        {
            var fullProcKey = FormatKey(processingQueue);
            var fullSourceKey = FormatKey(sourceQueue);
            long recovered = 0;

            // 1. Recover in TCP Redis
            try
            {
                if (_redis != null && _redis.IsConnected && _db != null)
                {
                    while (true)
                    {
                        var val = await _db.ListRightPopLeftPushAsync(fullProcKey, fullSourceKey);
                        if (!val.HasValue || string.IsNullOrEmpty(val.ToString())) break;
                        recovered++;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis TCP queue recovery error between {Proc} and {Source}", fullProcKey, fullSourceKey);
            }

            // 2. Recover in Upstash REST
            if (!string.IsNullOrEmpty(_upstashRestUrl) && !string.IsNullOrEmpty(_upstashRestToken))
            {
                try
                {
                    while (true)
                    {
                        var res = await _httpClient.PostAsync(
                            $"{_upstashRestUrl}/rpoplpush/{Uri.EscapeDataString(fullProcKey)}/{Uri.EscapeDataString(fullSourceKey)}",
                            null);

                        if (!res.IsSuccessStatusCode) break;
                        using var stream = await res.Content.ReadAsStreamAsync();
                        using var doc = await JsonDocument.ParseAsync(stream);
                        if (doc.RootElement.TryGetProperty("result", out var resultEl) && resultEl.ValueKind == JsonValueKind.String)
                        {
                            recovered++;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Upstash REST queue recovery error between {Proc} and {Source}", fullProcKey, fullSourceKey);
                }
            }

            // 3. Recover in-memory fallback
            lock (_queueLock)
            {
                if (_memoryQueueFallback.TryGetValue(fullProcKey, out var procList) && procList.Count > 0)
                {
                    var srcList = _memoryQueueFallback.GetOrAdd(fullSourceKey, _ => new List<string>());
                    while (procList.Count > 0)
                    {
                        var item = procList[procList.Count - 1];
                        procList.RemoveAt(procList.Count - 1);
                        srcList.Insert(0, item);
                        recovered++;
                    }
                }
            }

            return recovered;
        }

        public async Task<List<T>> GetQueueItemsAsync<T>(string queueName, int start = 0, int stop = -1)
        {
            var fullKey = FormatKey(queueName);
            var result = new List<T>();

            // 1. Try TCP Redis LRANGE
            try
            {
                if (_redis != null && _redis.IsConnected && _db != null)
                {
                    var items = await _db.ListRangeAsync(fullKey, start, stop);
                    foreach (var item in items)
                    {
                        if (item.HasValue && !string.IsNullOrEmpty(item.ToString()))
                        {
                            var obj = JsonSerializer.Deserialize<T>(item.ToString()!);
                            if (obj != null) result.Add(obj);
                        }
                    }
                    if (result.Any()) return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis TCP LRANGE error for queue {QueueName}", fullKey);
            }

            // 2. Try Upstash REST LRANGE
            if (!string.IsNullOrEmpty(_upstashRestUrl) && !string.IsNullOrEmpty(_upstashRestToken))
            {
                try
                {
                    var res = await _httpClient.GetAsync($"{_upstashRestUrl}/lrange/{Uri.EscapeDataString(fullKey)}/{start}/{stop}");
                    if (res.IsSuccessStatusCode)
                    {
                        using var stream = await res.Content.ReadAsStreamAsync();
                        using var doc = await JsonDocument.ParseAsync(stream);
                        if (doc.RootElement.TryGetProperty("result", out var resultEl) && resultEl.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var el in resultEl.EnumerateArray())
                            {
                                var jsonStr = el.GetString();
                                if (!string.IsNullOrEmpty(jsonStr))
                                {
                                    var obj = JsonSerializer.Deserialize<T>(jsonStr);
                                    if (obj != null) result.Add(obj);
                                }
                            }
                            if (result.Any()) return result;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Upstash REST LRANGE error for queue {QueueName}", fullKey);
                }
            }

            // 3. Fallback to memory
            lock (_queueLock)
            {
                if (_memoryQueueFallback.TryGetValue(fullKey, out var memList))
                {
                    var items = (stop == -1 || stop >= memList.Count)
                        ? memList.Skip(start)
                        : memList.Skip(start).Take(stop - start + 1);

                    foreach (var json in items)
                    {
                        var obj = JsonSerializer.Deserialize<T>(json);
                        if (obj != null) result.Add(obj);
                    }
                }
            }

            return result;
        }

        public async Task<long> GetQueueLengthAsync(string queueName)
        {
            var fullKey = FormatKey(queueName);

            try
            {
                if (_redis != null && _redis.IsConnected && _db != null)
                {
                    return await _db.ListLengthAsync(fullKey);
                }
            }
            catch { }

            if (!string.IsNullOrEmpty(_upstashRestUrl) && !string.IsNullOrEmpty(_upstashRestToken))
            {
                try
                {
                    var res = await _httpClient.GetAsync($"{_upstashRestUrl}/llen/{Uri.EscapeDataString(fullKey)}");
                    if (res.IsSuccessStatusCode)
                    {
                        using var stream = await res.Content.ReadAsStreamAsync();
                        using var doc = await JsonDocument.ParseAsync(stream);
                        if (doc.RootElement.TryGetProperty("result", out var resultEl) && resultEl.TryGetInt64(out var len))
                        {
                            return len;
                        }
                    }
                }
                catch { }
            }

            lock (_queueLock)
            {
                if (_memoryQueueFallback.TryGetValue(fullKey, out var memList))
                {
                    return memList.Count;
                }
            }

            return 0;
        }
    }
}
