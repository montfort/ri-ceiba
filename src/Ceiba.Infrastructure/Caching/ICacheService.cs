namespace Ceiba.Infrastructure.Caching;

/// <summary>
/// Cache service interface for application-level caching.
/// T117b: Query caching strategy
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Get a value from cache, or execute the factory function and cache the result.
    /// </summary>
    Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);

    /// <summary>
    /// Get a value from cache.
    /// </summary>
    T? Get<T>(string key);

    /// <summary>
    /// Set a value in cache.
    /// </summary>
    void Set<T>(string key, T value, TimeSpan? expiration = null);

    /// <summary>
    /// Remove a specific key from cache.
    /// </summary>
    void Remove(string key);

    /// <summary>
    /// Remove all keys matching a pattern (e.g., "catalog:*").
    /// </summary>
    void RemoveByPrefix(string prefix);

    /// <summary>
    /// Check if a key exists in cache.
    /// </summary>
    bool Exists(string key);
}
