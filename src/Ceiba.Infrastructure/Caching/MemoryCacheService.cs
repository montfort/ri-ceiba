using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Ceiba.Infrastructure.Caching;

/// <summary>
/// In-memory cache service implementation using IMemoryCache.
/// T117b: Query caching strategy
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<MemoryCacheService> _logger;
    private readonly ConcurrentDictionary<string, byte> _keys = new();

    // Default cache durations
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan CatalogExpiration = TimeSpan.FromHours(1);
    private static readonly TimeSpan ShortExpiration = TimeSpan.FromMinutes(1);

    public MemoryCacheService(IMemoryCache cache, ILogger<MemoryCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
    {
        if (_cache.TryGetValue(key, out T? cachedValue))
        {
            _logger.LogDebug("Cache hit for key: {Key}", key);
            return cachedValue;
        }

        _logger.LogDebug("Cache miss for key: {Key}", key);

        var value = await factory();

        // Use EqualityComparer to properly handle both reference and value types (SonarQube S2955)
        if (!EqualityComparer<T>.Default.Equals(value, default))
        {
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? GetDefaultExpiration(key),
                SlidingExpiration = TimeSpan.FromMinutes(2)
            };

            options.RegisterPostEvictionCallback((evictedKey, evictedValue, reason, state) =>
            {
                _keys.TryRemove(evictedKey.ToString()!, out _);
                _logger.LogDebug("Cache evicted key: {Key}, Reason: {Reason}", evictedKey, reason);
            });

            _cache.Set(key, value, options);
            _keys.TryAdd(key, 0);
        }

        return value;
    }

    public T? Get<T>(string key)
    {
        if (_cache.TryGetValue(key, out T? value))
        {
            _logger.LogDebug("Cache hit for key: {Key}", key);
            return value;
        }

        _logger.LogDebug("Cache miss for key: {Key}", key);
        return default;
    }

    public void Set<T>(string key, T value, TimeSpan? expiration = null)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? GetDefaultExpiration(key),
            SlidingExpiration = TimeSpan.FromMinutes(2)
        };

        options.RegisterPostEvictionCallback((evictedKey, evictedValue, reason, state) =>
        {
            _keys.TryRemove(evictedKey.ToString()!, out _);
        });

        _cache.Set(key, value, options);
        _keys.TryAdd(key, 0);

        _logger.LogDebug("Cache set for key: {Key}", key);
    }

    public void Remove(string key)
    {
        _cache.Remove(key);
        _keys.TryRemove(key, out _);
        _logger.LogDebug("Cache removed key: {Key}", key);
    }

    public void RemoveByPrefix(string prefix)
    {
        var keysToRemove = _keys.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();

        foreach (var key in keysToRemove)
        {
            _cache.Remove(key);
            _keys.TryRemove(key, out _);
        }

        _logger.LogDebug("Cache removed {Count} keys with prefix: {Prefix}", keysToRemove.Count, prefix);
    }

    public bool Exists(string key)
    {
        return _cache.TryGetValue(key, out _);
    }

    private static TimeSpan GetDefaultExpiration(string key)
    {
        // Catalog data can be cached longer
        if (key.StartsWith("catalog:", StringComparison.OrdinalIgnoreCase))
        {
            return CatalogExpiration;
        }

        // Stats can be cached moderately
        if (key.StartsWith("stats:", StringComparison.OrdinalIgnoreCase))
        {
            return TimeSpan.FromMinutes(15);
        }

        // Report data should have shorter cache
        if (key.StartsWith("report:", StringComparison.OrdinalIgnoreCase))
        {
            return ShortExpiration;
        }

        return DefaultExpiration;
    }
}
