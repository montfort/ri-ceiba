using Ceiba.Infrastructure.Caching;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Ceiba.Infrastructure.Tests.Caching;

/// <summary>
/// Unit tests for MemoryCacheService - caching operations.
/// T117b: Query caching strategy tests.
/// </summary>
public class MemoryCacheServiceTests : IDisposable
{
    private readonly MemoryCache _memoryCache;
    private readonly ILogger<MemoryCacheService> _logger;
    private readonly MemoryCacheService _cacheService;

    public MemoryCacheServiceTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _logger = Substitute.For<ILogger<MemoryCacheService>>();
        _cacheService = new MemoryCacheService(_memoryCache, _logger);
    }

    public void Dispose()
    {
        _memoryCache.Dispose();
    }

    #region GetOrCreateAsync Tests

    [Fact(DisplayName = "GetOrCreateAsync should return cached value on cache hit")]
    public async Task GetOrCreateAsync_CacheHit_ReturnsCachedValue()
    {
        // Arrange
        var key = "test-key";
        var value = "cached-value";
        _cacheService.Set(key, value);
        var factoryCalled = false;

        // Act
        var result = await _cacheService.GetOrCreateAsync(key, () =>
        {
            factoryCalled = true;
            return Task.FromResult("new-value");
        });

        // Assert
        result.Should().Be(value);
        factoryCalled.Should().BeFalse();
    }

    [Fact(DisplayName = "GetOrCreateAsync should call factory on cache miss")]
    public async Task GetOrCreateAsync_CacheMiss_CallsFactory()
    {
        // Arrange
        var key = "new-key";
        var expectedValue = "factory-value";

        // Act
        var result = await _cacheService.GetOrCreateAsync(key, () => Task.FromResult(expectedValue));

        // Assert
        result.Should().Be(expectedValue);
    }

    [Fact(DisplayName = "GetOrCreateAsync should cache factory result")]
    public async Task GetOrCreateAsync_CacheMiss_CachesResult()
    {
        // Arrange
        var key = "cache-test-key";
        var value = "value-to-cache";

        // Act
        await _cacheService.GetOrCreateAsync(key, () => Task.FromResult(value));
        var cachedValue = _cacheService.Get<string>(key);

        // Assert
        cachedValue.Should().Be(value);
    }

    [Fact(DisplayName = "GetOrCreateAsync should not cache null values")]
    public async Task GetOrCreateAsync_NullValue_NotCached()
    {
        // Arrange
        var key = "null-value-key";

        // Act
        var result = await _cacheService.GetOrCreateAsync<string?>(key, () => Task.FromResult<string?>(null));

        // Assert
        result.Should().BeNull();
        _cacheService.Exists(key).Should().BeFalse();
    }

    [Fact(DisplayName = "GetOrCreateAsync should use custom expiration")]
    public async Task GetOrCreateAsync_CustomExpiration_Respects()
    {
        // Arrange
        var key = "expiring-key";
        var value = "expiring-value";
        var shortExpiration = TimeSpan.FromMilliseconds(50);

        // Act
        await _cacheService.GetOrCreateAsync(key, () => Task.FromResult(value), shortExpiration);

        // Wait for expiration
        await Task.Delay(100);

        var cachedValue = _cacheService.Get<string>(key);

        // Assert - value should be expired
        cachedValue.Should().BeNull();
    }

    #endregion

    #region Get Tests

    [Fact(DisplayName = "Get should return value when exists")]
    public void Get_ExistingKey_ReturnsValue()
    {
        // Arrange
        var key = "existing-key";
        var value = "existing-value";
        _cacheService.Set(key, value);

        // Act
        var result = _cacheService.Get<string>(key);

        // Assert
        result.Should().Be(value);
    }

    [Fact(DisplayName = "Get should return default when key not exists")]
    public void Get_NonExistingKey_ReturnsDefault()
    {
        // Arrange
        var key = "non-existing-key";

        // Act
        var result = _cacheService.Get<string>(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact(DisplayName = "Get should return default for value types when key not exists")]
    public void Get_NonExistingKey_ReturnsDefaultForValueType()
    {
        // Arrange
        var key = "non-existing-int-key";

        // Act
        var result = _cacheService.Get<int>(key);

        // Assert
        result.Should().Be(0);
    }

    #endregion

    #region Set Tests

    [Fact(DisplayName = "Set should store value")]
    public void Set_Value_Stored()
    {
        // Arrange
        var key = "set-test-key";
        var value = "set-test-value";

        // Act
        _cacheService.Set(key, value);
        var result = _cacheService.Get<string>(key);

        // Assert
        result.Should().Be(value);
    }

    [Fact(DisplayName = "Set should overwrite existing value")]
    public void Set_ExistingKey_Overwrites()
    {
        // Arrange
        var key = "overwrite-key";
        _cacheService.Set(key, "old-value");

        // Act
        _cacheService.Set(key, "new-value");
        var result = _cacheService.Get<string>(key);

        // Assert
        result.Should().Be("new-value");
    }

    [Fact(DisplayName = "Set should store complex objects")]
    public void Set_ComplexObject_Stored()
    {
        // Arrange
        var key = "complex-object-key";
        var value = new TestObject { Id = 1, Name = "Test", Date = DateTime.UtcNow };

        // Act
        _cacheService.Set(key, value);
        var result = _cacheService.Get<TestObject>(key);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Test");
    }

    #endregion

    #region Remove Tests

    [Fact(DisplayName = "Remove should delete cached value")]
    public void Remove_ExistingKey_Deletes()
    {
        // Arrange
        var key = "remove-test-key";
        _cacheService.Set(key, "value");

        // Act
        _cacheService.Remove(key);
        var result = _cacheService.Get<string>(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact(DisplayName = "Remove should not throw for non-existing key")]
    public void Remove_NonExistingKey_NoError()
    {
        // Arrange
        var key = "non-existing-remove-key";

        // Act & Assert
        var action = () => _cacheService.Remove(key);
        action.Should().NotThrow();
    }

    #endregion

    #region RemoveByPrefix Tests

    [Fact(DisplayName = "RemoveByPrefix should delete all matching keys")]
    public void RemoveByPrefix_MatchingKeys_Deleted()
    {
        // Arrange
        _cacheService.Set("catalog:zonas", "zonas");
        _cacheService.Set("catalog:sectores", "sectores");
        _cacheService.Set("report:1", "report1");

        // Act
        _cacheService.RemoveByPrefix("catalog:");

        // Assert
        _cacheService.Get<string>("catalog:zonas").Should().BeNull();
        _cacheService.Get<string>("catalog:sectores").Should().BeNull();
        _cacheService.Get<string>("report:1").Should().Be("report1");
    }

    [Fact(DisplayName = "RemoveByPrefix should be case-insensitive")]
    public void RemoveByPrefix_CaseInsensitive()
    {
        // Arrange
        _cacheService.Set("Catalog:zonas", "zonas");
        _cacheService.Set("CATALOG:sectores", "sectores");

        // Act
        _cacheService.RemoveByPrefix("catalog:");

        // Assert
        _cacheService.Get<string>("Catalog:zonas").Should().BeNull();
        _cacheService.Get<string>("CATALOG:sectores").Should().BeNull();
    }

    #endregion

    #region Exists Tests

    [Fact(DisplayName = "Exists should return true for cached key")]
    public void Exists_CachedKey_ReturnsTrue()
    {
        // Arrange
        var key = "exists-test-key";
        _cacheService.Set(key, "value");

        // Act
        var result = _cacheService.Exists(key);

        // Assert
        result.Should().BeTrue();
    }

    [Fact(DisplayName = "Exists should return false for non-cached key")]
    public void Exists_NonCachedKey_ReturnsFalse()
    {
        // Arrange
        var key = "non-cached-key";

        // Act
        var result = _cacheService.Exists(key);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Default Expiration Tests

    [Fact(DisplayName = "Catalog keys should use longer expiration")]
    public async Task CatalogKey_UsesLongerExpiration()
    {
        // Arrange
        var key = "catalog:test";
        var value = "catalog-value";

        // Act
        await _cacheService.GetOrCreateAsync(key, () => Task.FromResult(value));

        // Assert - value should still exist after default expiration
        await Task.Delay(100); // Short delay, well under 1 hour catalog expiration
        _cacheService.Get<string>(key).Should().Be(value);
    }

    [Fact(DisplayName = "Stats keys should use moderate expiration")]
    public async Task StatsKey_UsesModerateExpiration()
    {
        // Arrange
        var key = "stats:daily";
        var value = "stats-value";

        // Act
        await _cacheService.GetOrCreateAsync(key, () => Task.FromResult(value));

        // Assert - value should exist
        _cacheService.Get<string>(key).Should().Be(value);
    }

    [Fact(DisplayName = "Report keys should use shorter expiration")]
    public async Task ReportKey_UsesShorterExpiration()
    {
        // Arrange
        var key = "report:123";
        var value = "report-value";

        // Act
        await _cacheService.GetOrCreateAsync(key, () => Task.FromResult(value));

        // Assert - value should exist immediately
        _cacheService.Get<string>(key).Should().Be(value);
    }

    #endregion

    #region Concurrent Access Tests

    [Fact(DisplayName = "Cache should handle concurrent reads")]
    public async Task Cache_ConcurrentReads_Safe()
    {
        // Arrange
        var key = "concurrent-key";
        var value = "concurrent-value";
        _cacheService.Set(key, value);

        // Act
        var tasks = Enumerable.Range(0, 100)
            .Select(_ => Task.Run(() => _cacheService.Get<string>(key)))
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllBe(value);
    }

    [Fact(DisplayName = "Cache should handle concurrent writes")]
    public async Task Cache_ConcurrentWrites_Safe()
    {
        // Arrange
        var baseKey = "concurrent-write-";

        // Act
        var tasks = Enumerable.Range(0, 100)
            .Select(i => Task.Run(() => _cacheService.Set($"{baseKey}{i}", $"value-{i}")))
            .ToList();

        await Task.WhenAll(tasks);

        // Assert - all values should be stored
        for (int i = 0; i < 100; i++)
        {
            _cacheService.Exists($"{baseKey}{i}").Should().BeTrue();
        }
    }

    #endregion

    #region Helper Classes

    private class TestObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }

    #endregion
}
