using Ceiba.Core.Entities;
using Ceiba.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace Ceiba.Infrastructure.Tests.Data;

/// <summary>
/// Unit tests for RegionDataLoader service.
/// Tests JSON parsing and database seeding for geographic catalogs.
/// </summary>
public class RegionDataLoaderTests : IDisposable
{
    private readonly CeibaDbContext _context;
    private readonly RegionDataLoader _loader;
    private readonly Mock<ILogger<RegionDataLoader>> _loggerMock;
    private readonly string _tempJsonPath;

    public RegionDataLoaderTests()
    {
        var options = new DbContextOptionsBuilder<CeibaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CeibaDbContext(options);
        _loggerMock = new Mock<ILogger<RegionDataLoader>>();
        _loader = new RegionDataLoader(_context, _loggerMock.Object);

        // Create temp JSON file for tests
        _tempJsonPath = Path.Combine(Path.GetTempPath(), $"regiones_test_{Guid.NewGuid()}.json");
    }

    public void Dispose()
    {
        _context.Dispose();
        if (File.Exists(_tempJsonPath))
            File.Delete(_tempJsonPath);
    }

    #region LoadFromJsonAsync Tests

    [Fact]
    public async Task LoadFromJsonAsync_WithValidJson_ReturnsZonaData()
    {
        // Arrange
        var jsonContent = CreateValidTestJson();
        await File.WriteAllTextAsync(_tempJsonPath, jsonContent);

        // Act
        var result = await _loader.LoadFromJsonAsync(_tempJsonPath);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].NombreZona.Should().Be("Norte");
        result[0].Regiones.Should().HaveCount(1);
        result[0].Regiones[0].NumeroRegion.Should().Be(1);
        result[0].Regiones[0].Sectores.Should().HaveCount(2);
    }

    [Fact]
    public async Task LoadFromJsonAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = "/path/that/does/not/exist/regiones.json";

        // Act
        var act = () => _loader.LoadFromJsonAsync(nonExistentPath);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task LoadFromJsonAsync_WithInvalidJson_ThrowsJsonException()
    {
        // Arrange
        await File.WriteAllTextAsync(_tempJsonPath, "{ invalid json }");

        // Act
        var act = () => _loader.LoadFromJsonAsync(_tempJsonPath);

        // Assert
        await act.Should().ThrowAsync<JsonException>();
    }

    [Fact]
    public async Task LoadFromJsonAsync_WithEmptyArray_ReturnsEmptyList()
    {
        // Arrange
        await File.WriteAllTextAsync(_tempJsonPath, "[]");

        // Act
        var result = await _loader.LoadFromJsonAsync(_tempJsonPath);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region SeedGeographicCatalogsAsync Tests

    [Fact]
    public async Task SeedGeographicCatalogsAsync_WithValidData_CreatesAllEntities()
    {
        // Arrange
        var zonaData = CreateTestZonaData();
        var adminUserId = Guid.NewGuid();

        // Act
        await _loader.SeedGeographicCatalogsAsync(zonaData, adminUserId, clearExisting: false);

        // Assert
        var zonas = await _context.Zonas.ToListAsync();
        var regiones = await _context.Regiones.ToListAsync();
        var sectores = await _context.Sectores.ToListAsync();
        var cuadrantes = await _context.Cuadrantes.ToListAsync();

        zonas.Should().HaveCount(2);
        regiones.Should().HaveCount(2);
        sectores.Should().HaveCount(4);
        cuadrantes.Should().HaveCount(8); // 2 zonas * 1 region * 2 sectores * 2 cuadrantes
    }

    [Fact]
    public async Task SeedGeographicCatalogsAsync_WhenDataExists_SkipsWithoutClearFlag()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        _context.Zonas.Add(new Zona { Nombre = "Existing", Activo = true, UsuarioId = adminUserId });
        await _context.SaveChangesAsync();

        var zonaData = CreateTestZonaData();

        // Act
        await _loader.SeedGeographicCatalogsAsync(zonaData, adminUserId, clearExisting: false);

        // Assert - should still have only 1 zona (the existing one)
        var zonas = await _context.Zonas.ToListAsync();
        zonas.Should().HaveCount(1);
        zonas[0].Nombre.Should().Be("Existing");
    }

    [Fact(Skip = "ExecuteDeleteAsync not supported by InMemory provider - see integration tests")]
    public async Task SeedGeographicCatalogsAsync_WithClearFlag_ReplacesExistingData()
    {
        // This test requires a real database due to clearExisting using ExecuteDeleteAsync
        await Task.CompletedTask;
    }

    [Fact]
    public async Task SeedGeographicCatalogsAsync_SetsCorrectHierarchy()
    {
        // Arrange
        var zonaData = CreateTestZonaData();
        var adminUserId = Guid.NewGuid();

        // Act
        await _loader.SeedGeographicCatalogsAsync(zonaData, adminUserId, clearExisting: false);

        // Assert - verify hierarchy relationships
        var zona = await _context.Zonas
            .Include(z => z.Regiones)
            .ThenInclude(r => r.Sectores)
            .ThenInclude(s => s.Cuadrantes)
            .FirstAsync(z => z.Nombre == "Norte");

        zona.Regiones.Should().HaveCount(1);
        zona.Regiones.First().Nombre.Should().Be("Región 1");
        zona.Regiones.First().Sectores.Should().HaveCount(2);
        zona.Regiones.First().Sectores.First().Cuadrantes.Should().HaveCount(2);
    }

    [Fact]
    public async Task SeedGeographicCatalogsAsync_SetsAllEntitiesAsActive()
    {
        // Arrange
        var zonaData = CreateTestZonaData();
        var adminUserId = Guid.NewGuid();

        // Act
        await _loader.SeedGeographicCatalogsAsync(zonaData, adminUserId, clearExisting: false);

        // Assert
        var allZonas = await _context.Zonas.ToListAsync();
        var allRegiones = await _context.Regiones.ToListAsync();
        var allSectores = await _context.Sectores.ToListAsync();
        var allCuadrantes = await _context.Cuadrantes.ToListAsync();

        allZonas.Should().OnlyContain(z => z.Activo);
        allRegiones.Should().OnlyContain(r => r.Activo);
        allSectores.Should().OnlyContain(s => s.Activo);
        allCuadrantes.Should().OnlyContain(c => c.Activo);
    }

    [Fact]
    public async Task SeedGeographicCatalogsAsync_SetsCorrectUsuarioId()
    {
        // Arrange
        var zonaData = CreateTestZonaData();
        var adminUserId = Guid.NewGuid();

        // Act
        await _loader.SeedGeographicCatalogsAsync(zonaData, adminUserId, clearExisting: false);

        // Assert
        var allZonas = await _context.Zonas.ToListAsync();
        allZonas.Should().OnlyContain(z => z.UsuarioId == adminUserId);
    }

    #endregion

    #region ClearGeographicCatalogsAsync Tests

    // Note: ClearGeographicCatalogsAsync uses ExecuteDeleteAsync which is not supported
    // by InMemory provider. These tests require a real database (integration tests).
    // See Ceiba.Integration.Tests for integration tests of this functionality.

    [Fact(Skip = "ExecuteDeleteAsync not supported by InMemory provider - see integration tests")]
    public async Task ClearGeographicCatalogsAsync_RemovesAllGeographicData()
    {
        // This test requires a real database due to ExecuteDeleteAsync usage
        await Task.CompletedTask;
    }

    [Fact(Skip = "ExecuteDeleteAsync not supported by InMemory provider - see integration tests")]
    public async Task ClearGeographicCatalogsAsync_WithNoReports_ClearsAllData()
    {
        // This test requires a real database due to ExecuteDeleteAsync usage
        await Task.CompletedTask;
    }

    #endregion

    #region GetCurrentStatsAsync Tests

    [Fact]
    public async Task GetCurrentStatsAsync_ReturnsCorrectCounts()
    {
        // Arrange
        await SeedInitialData();

        // Act
        var stats = await _loader.GetCurrentStatsAsync();

        // Assert
        stats.Zonas.Should().Be(2);
        stats.Regiones.Should().Be(2);
        stats.Sectores.Should().Be(4);
        stats.Cuadrantes.Should().Be(8);
    }

    [Fact]
    public async Task GetCurrentStatsAsync_WithEmptyDatabase_ReturnsZeros()
    {
        // Act
        var stats = await _loader.GetCurrentStatsAsync();

        // Assert
        stats.Zonas.Should().Be(0);
        stats.Regiones.Should().Be(0);
        stats.Sectores.Should().Be(0);
        stats.Cuadrantes.Should().Be(0);
    }

    #endregion

    #region Helper Methods

    private string CreateValidTestJson()
    {
        return """
        [
          {
            "nombre_zona": "Norte",
            "regiones": [
              {
                "numero_region": 1,
                "sectores": [
                  { "nombre_sector": "Sector A", "cuadrantes": [1, 2] },
                  { "nombre_sector": "Sector B", "cuadrantes": [1, 2] }
                ]
              }
            ]
          },
          {
            "nombre_zona": "Sur",
            "regiones": [
              {
                "numero_region": 1,
                "sectores": [
                  { "nombre_sector": "Sector C", "cuadrantes": [1, 2] },
                  { "nombre_sector": "Sector D", "cuadrantes": [1, 2] }
                ]
              }
            ]
          }
        ]
        """;
    }

    private List<RegionJsonZona> CreateTestZonaData()
    {
        return new List<RegionJsonZona>
        {
            new RegionJsonZona("Norte", new List<RegionJsonRegion>
            {
                new RegionJsonRegion(1, new List<RegionJsonSector>
                {
                    new RegionJsonSector("Sector A", new List<int> { 1, 2 }),
                    new RegionJsonSector("Sector B", new List<int> { 1, 2 })
                })
            }),
            new RegionJsonZona("Sur", new List<RegionJsonRegion>
            {
                new RegionJsonRegion(1, new List<RegionJsonSector>
                {
                    new RegionJsonSector("Sector C", new List<int> { 1, 2 }),
                    new RegionJsonSector("Sector D", new List<int> { 1, 2 })
                })
            })
        };
    }

    private async Task SeedInitialData()
    {
        var adminUserId = Guid.NewGuid();

        var zona1 = new Zona { Nombre = "Norte", Activo = true, UsuarioId = adminUserId };
        var zona2 = new Zona { Nombre = "Sur", Activo = true, UsuarioId = adminUserId };
        _context.Zonas.AddRange(zona1, zona2);
        await _context.SaveChangesAsync();

        var region1 = new Region { Nombre = "Región 1", ZonaId = zona1.Id, Activo = true, UsuarioId = adminUserId };
        var region2 = new Region { Nombre = "Región 1", ZonaId = zona2.Id, Activo = true, UsuarioId = adminUserId };
        _context.Regiones.AddRange(region1, region2);
        await _context.SaveChangesAsync();

        var sector1 = new Sector { Nombre = "Sector A", RegionId = region1.Id, Activo = true, UsuarioId = adminUserId };
        var sector2 = new Sector { Nombre = "Sector B", RegionId = region1.Id, Activo = true, UsuarioId = adminUserId };
        var sector3 = new Sector { Nombre = "Sector C", RegionId = region2.Id, Activo = true, UsuarioId = adminUserId };
        var sector4 = new Sector { Nombre = "Sector D", RegionId = region2.Id, Activo = true, UsuarioId = adminUserId };
        _context.Sectores.AddRange(sector1, sector2, sector3, sector4);
        await _context.SaveChangesAsync();

        _context.Cuadrantes.AddRange(
            new Cuadrante { Nombre = "1", SectorId = sector1.Id, Activo = true, UsuarioId = adminUserId },
            new Cuadrante { Nombre = "2", SectorId = sector1.Id, Activo = true, UsuarioId = adminUserId },
            new Cuadrante { Nombre = "1", SectorId = sector2.Id, Activo = true, UsuarioId = adminUserId },
            new Cuadrante { Nombre = "2", SectorId = sector2.Id, Activo = true, UsuarioId = adminUserId },
            new Cuadrante { Nombre = "1", SectorId = sector3.Id, Activo = true, UsuarioId = adminUserId },
            new Cuadrante { Nombre = "2", SectorId = sector3.Id, Activo = true, UsuarioId = adminUserId },
            new Cuadrante { Nombre = "1", SectorId = sector4.Id, Activo = true, UsuarioId = adminUserId },
            new Cuadrante { Nombre = "2", SectorId = sector4.Id, Activo = true, UsuarioId = adminUserId }
        );
        await _context.SaveChangesAsync();
    }

    #endregion
}
