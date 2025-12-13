using Ceiba.Core.Entities;
using Ceiba.Infrastructure.Caching;
using Ceiba.Infrastructure.Data;
using Ceiba.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Ceiba.Infrastructure.Tests.Services;

/// <summary>
/// Unit tests for CachedCatalogService - cached catalog data access.
/// Tests caching behavior for Zonas, Sectores, Cuadrantes, and Sugerencias.
/// </summary>
public class CachedCatalogServiceTests : IDisposable
{
    private readonly CeibaDbContext _context;
    private readonly ICacheService _mockCache;
    private readonly ILogger<CachedCatalogService> _mockLogger;
    private readonly CachedCatalogService _service;

    public CachedCatalogServiceTests()
    {
        var options = new DbContextOptionsBuilder<CeibaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CeibaDbContext(options);
        _mockCache = Substitute.For<ICacheService>();
        _mockLogger = Substitute.For<ILogger<CachedCatalogService>>();

        _service = new CachedCatalogService(_context, _mockCache, _mockLogger);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var zona1 = new Zona { Id = 1, Nombre = "Zona Norte", Activo = true };
        var zona2 = new Zona { Id = 2, Nombre = "Zona Sur", Activo = true };
        var zonaInactiva = new Zona { Id = 3, Nombre = "Zona Inactiva", Activo = false };

        _context.Zonas.AddRange(zona1, zona2, zonaInactiva);

        // Regiones (Zona → Región → Sector → Cuadrante)
        var region1 = new Region { Id = 1, Nombre = "Región Norte", ZonaId = 1, Activo = true };
        var region2 = new Region { Id = 2, Nombre = "Región Sur", ZonaId = 2, Activo = true };

        _context.Regiones.AddRange(region1, region2);

        var sector1 = new Sector { Id = 1, Nombre = "Sector Centro", RegionId = 1, Activo = true };
        var sector2 = new Sector { Id = 2, Nombre = "Sector Este", RegionId = 1, Activo = true };
        var sector3 = new Sector { Id = 3, Nombre = "Sector Oeste", RegionId = 2, Activo = true };

        _context.Sectores.AddRange(sector1, sector2, sector3);

        var cuadrante1 = new Cuadrante { Id = 1, Nombre = "Cuadrante A1", SectorId = 1, Activo = true };
        var cuadrante2 = new Cuadrante { Id = 2, Nombre = "Cuadrante A2", SectorId = 1, Activo = true };
        var cuadrante3 = new Cuadrante { Id = 3, Nombre = "Cuadrante B1", SectorId = 2, Activo = true };

        _context.Cuadrantes.AddRange(cuadrante1, cuadrante2, cuadrante3);

        var sugerencia1 = new CatalogoSugerencia { Id = 1, Campo = "Sexo", Valor = "Masculino", Orden = 1, Activo = true };
        var sugerencia2 = new CatalogoSugerencia { Id = 2, Campo = "Sexo", Valor = "Femenino", Orden = 2, Activo = true };
        var sugerencia3 = new CatalogoSugerencia { Id = 3, Campo = "Delito", Valor = "Robo", Orden = 1, Activo = true };

        _context.CatalogosSugerencia.AddRange(sugerencia1, sugerencia2, sugerencia3);

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region GetAllZonasAsync Tests

    [Fact(DisplayName = "GetAllZonasAsync should return active zonas from cache")]
    public async Task GetAllZonasAsync_ShouldReturnActiveZonasFromCache()
    {
        // Arrange
        var cachedZonas = new List<Zona>
        {
            new() { Id = 1, Nombre = "Cached Zona", Activo = true }
        };

        _mockCache.GetOrCreateAsync(
            Arg.Any<string>(),
            Arg.Any<Func<Task<List<Zona>>>>(),
            Arg.Any<TimeSpan>())
            .Returns(cachedZonas);

        // Act
        var result = await _service.GetAllZonasAsync();

        // Assert
        result.Should().HaveCount(1);
        result.First().Nombre.Should().Be("Cached Zona");
    }

    [Fact(DisplayName = "GetAllZonasAsync should return empty list when cache returns null")]
    public async Task GetAllZonasAsync_CacheReturnsNull_ReturnsEmptyList()
    {
        // Arrange
        _mockCache.GetOrCreateAsync(
            Arg.Any<string>(),
            Arg.Any<Func<Task<List<Zona>>>>(),
            Arg.Any<TimeSpan>())
            .Returns((List<Zona>?)null);

        // Act
        var result = await _service.GetAllZonasAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact(DisplayName = "GetZonaByIdAsync should return zona from cached list")]
    public async Task GetZonaByIdAsync_ShouldReturnZonaFromCachedList()
    {
        // Arrange
        var cachedZonas = new List<Zona>
        {
            new() { Id = 1, Nombre = "Zona Norte", Activo = true },
            new() { Id = 2, Nombre = "Zona Sur", Activo = true }
        };

        _mockCache.GetOrCreateAsync(
            Arg.Any<string>(),
            Arg.Any<Func<Task<List<Zona>>>>(),
            Arg.Any<TimeSpan>())
            .Returns(cachedZonas);

        // Act
        var result = await _service.GetZonaByIdAsync(2);

        // Assert
        result.Should().NotBeNull();
        result!.Nombre.Should().Be("Zona Sur");
    }

    [Fact(DisplayName = "GetZonaByIdAsync should return null for non-existent id")]
    public async Task GetZonaByIdAsync_NonExistentId_ReturnsNull()
    {
        // Arrange
        var cachedZonas = new List<Zona>
        {
            new() { Id = 1, Nombre = "Zona Norte", Activo = true }
        };

        _mockCache.GetOrCreateAsync(
            Arg.Any<string>(),
            Arg.Any<Func<Task<List<Zona>>>>(),
            Arg.Any<TimeSpan>())
            .Returns(cachedZonas);

        // Act
        var result = await _service.GetZonaByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllSectoresAsync Tests

    [Fact(DisplayName = "GetAllSectoresAsync should return active sectores from cache")]
    public async Task GetAllSectoresAsync_ShouldReturnActiveSectoresFromCache()
    {
        // Arrange
        var cachedSectores = new List<Sector>
        {
            new() { Id = 1, Nombre = "Sector 1", RegionId = 1, Activo = true }
        };

        _mockCache.GetOrCreateAsync(
            Arg.Any<string>(),
            Arg.Any<Func<Task<List<Sector>>>>(),
            Arg.Any<TimeSpan>())
            .Returns(cachedSectores);

        // Act
        var result = await _service.GetAllSectoresAsync();

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact(DisplayName = "GetSectoresByRegionAsync should filter by region id")]
    public async Task GetSectoresByRegionAsync_ShouldFilterByRegionId()
    {
        // Arrange
        var cachedSectores = new List<Sector>
        {
            new() { Id = 1, Nombre = "Sector Centro", RegionId = 1, Activo = true },
            new() { Id = 2, Nombre = "Sector Este", RegionId = 1, Activo = true }
        };

        _mockCache.GetOrCreateAsync(
            Arg.Any<string>(),
            Arg.Any<Func<Task<List<Sector>>>>(),
            Arg.Any<TimeSpan>())
            .Returns(cachedSectores);

        // Act
        var result = await _service.GetSectoresByRegionAsync(1);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(s => s.RegionId.Should().Be(1));
    }

    #endregion

    #region GetAllCuadrantesAsync Tests

    [Fact(DisplayName = "GetAllCuadrantesAsync should return active cuadrantes from cache")]
    public async Task GetAllCuadrantesAsync_ShouldReturnActiveCuadrantesFromCache()
    {
        // Arrange
        var cachedCuadrantes = new List<Cuadrante>
        {
            new() { Id = 1, Nombre = "Cuadrante A1", SectorId = 1, Activo = true }
        };

        _mockCache.GetOrCreateAsync(
            Arg.Any<string>(),
            Arg.Any<Func<Task<List<Cuadrante>>>>(),
            Arg.Any<TimeSpan>())
            .Returns(cachedCuadrantes);

        // Act
        var result = await _service.GetAllCuadrantesAsync();

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact(DisplayName = "GetCuadrantesBySectorAsync should filter by sector id")]
    public async Task GetCuadrantesBySectorAsync_ShouldFilterBySectorId()
    {
        // Arrange
        var cachedCuadrantes = new List<Cuadrante>
        {
            new() { Id = 1, Nombre = "Cuadrante A1", SectorId = 1, Activo = true },
            new() { Id = 2, Nombre = "Cuadrante A2", SectorId = 1, Activo = true }
        };

        _mockCache.GetOrCreateAsync(
            Arg.Any<string>(),
            Arg.Any<Func<Task<List<Cuadrante>>>>(),
            Arg.Any<TimeSpan>())
            .Returns(cachedCuadrantes);

        // Act
        var result = await _service.GetCuadrantesBySectorAsync(1);

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion

    #region GetSugerenciasByCampoAsync Tests

    [Fact(DisplayName = "GetSugerenciasByCampoAsync should filter by campo")]
    public async Task GetSugerenciasByCampoAsync_ShouldFilterByCampo()
    {
        // Arrange
        var cachedSugerencias = new List<CatalogoSugerencia>
        {
            new() { Id = 1, Campo = "Sexo", Valor = "Masculino", Orden = 1, Activo = true },
            new() { Id = 2, Campo = "Sexo", Valor = "Femenino", Orden = 2, Activo = true }
        };

        _mockCache.GetOrCreateAsync(
            Arg.Any<string>(),
            Arg.Any<Func<Task<List<CatalogoSugerencia>>>>(),
            Arg.Any<TimeSpan>())
            .Returns(cachedSugerencias);

        // Act
        var result = await _service.GetSugerenciasByCampoAsync("Sexo");

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact(DisplayName = "GetAllSugerenciasAsync should return grouped dictionary")]
    public async Task GetAllSugerenciasAsync_ShouldReturnGroupedDictionary()
    {
        // Act - this method doesn't use cache, queries DB directly
        var result = await _service.GetAllSugerenciasAsync();

        // Assert
        result.Should().ContainKey("Sexo");
        result.Should().ContainKey("Delito");
        result["Sexo"].Should().Contain("Masculino");
        result["Sexo"].Should().Contain("Femenino");
        result["Delito"].Should().Contain("Robo");
    }

    #endregion

    #region Cache Invalidation Tests

    [Fact(DisplayName = "InvalidateAllCatalogs should remove all catalog caches")]
    public void InvalidateAllCatalogs_ShouldRemoveAllCatalogCaches()
    {
        // Act
        _service.InvalidateAllCatalogs();

        // Assert
        _mockCache.Received(1).RemoveByPrefix("catalog:");
    }

    [Fact(DisplayName = "InvalidateZonas should remove zona and related caches")]
    public void InvalidateZonas_ShouldRemoveZonaAndRelatedCaches()
    {
        // Act
        _service.InvalidateZonas();

        // Assert
        _mockCache.Received(1).Remove(CacheKeys.AllZonas);
        _mockCache.Received(1).RemoveByPrefix("catalog:regiones:");
    }

    [Fact(DisplayName = "InvalidateSectores should remove sector and related caches")]
    public void InvalidateSectores_ShouldRemoveSectorAndRelatedCaches()
    {
        // Act
        _service.InvalidateSectores();

        // Assert
        _mockCache.Received(1).Remove(CacheKeys.AllSectores);
        _mockCache.Received(1).RemoveByPrefix("catalog:sectores:region:");
        _mockCache.Received(1).RemoveByPrefix("catalog:cuadrantes:");
    }

    [Fact(DisplayName = "InvalidateCuadrantes should remove cuadrante caches")]
    public void InvalidateCuadrantes_ShouldRemoveCuadranteCaches()
    {
        // Act
        _service.InvalidateCuadrantes();

        // Assert
        _mockCache.Received(1).Remove(CacheKeys.AllCuadrantes);
        _mockCache.Received(1).RemoveByPrefix("catalog:cuadrantes:sector:");
    }

    [Fact(DisplayName = "InvalidateSugerencias with null campo should remove all sugerencia caches")]
    public void InvalidateSugerencias_NullCampo_ShouldRemoveAllSugerenciaCaches()
    {
        // Act
        _service.InvalidateSugerencias(null);

        // Assert
        _mockCache.Received(1).RemoveByPrefix("catalog:sugerencias:");
    }

    [Fact(DisplayName = "InvalidateSugerencias with specific campo should remove only that cache")]
    public void InvalidateSugerencias_SpecificCampo_ShouldRemoveOnlyThatCache()
    {
        // Act
        _service.InvalidateSugerencias("Sexo");

        // Assert
        _mockCache.Received(1).Remove(Arg.Is<string>(s => s.Contains("sexo")));
        _mockCache.DidNotReceive().RemoveByPrefix(Arg.Any<string>());
    }

    #endregion
}
