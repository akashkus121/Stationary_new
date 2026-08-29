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
    }
}
