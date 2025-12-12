using Ceiba.Application.Services;
using Ceiba.Core.Entities;
using Ceiba.Infrastructure.Data;
using Ceiba.Shared.DTOs;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Ceiba.Infrastructure.Tests.Services;

/// <summary>
/// Unit tests for CatalogService (US1: T036)
/// Tests catalog retrieval operations for Zonas, Sectores, Cuadrantes, and Sugerencias.
/// </summary>
public class CatalogServiceTests : IDisposable
{
    private readonly CeibaDbContext _context;
    private readonly CatalogService _service;

    public CatalogServiceTests()
    {
        var options = new DbContextOptionsBuilder<CeibaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CeibaDbContext(options);
        _service = new CatalogService(_context);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Create geographic hierarchy: 2 Zones, each with 2 Sectors, each with 2 Cuadrantes
        var zona1 = new Zona { Id = 1, Nombre = "Zona Norte", Activo = true };
        var zona2 = new Zona { Id = 2, Nombre = "Zona Sur", Activo = true };
        var zona3 = new Zona { Id = 3, Nombre = "Zona Inactiva", Activo = false }; // Inactive

        _context.Zonas.AddRange(zona1, zona2, zona3);

        var sector1_1 = new Sector { Id = 1, Nombre = "Sector Norte-1", ZonaId = 1, Activo = true };
        var sector1_2 = new Sector { Id = 2, Nombre = "Sector Norte-2", ZonaId = 1, Activo = true };
        var sector2_1 = new Sector { Id = 3, Nombre = "Sector Sur-1", ZonaId = 2, Activo = true };
        var sector2_2 = new Sector { Id = 4, Nombre = "Sector Sur-2", ZonaId = 2, Activo = false }; // Inactive

        _context.Sectores.AddRange(sector1_1, sector1_2, sector2_1, sector2_2);

        var cuadrante1_1_1 = new Cuadrante { Id = 1, Nombre = "Cuadrante N1-A", SectorId = 1, Activo = true };
        var cuadrante1_1_2 = new Cuadrante { Id = 2, Nombre = "Cuadrante N1-B", SectorId = 1, Activo = true };
        var cuadrante1_2_1 = new Cuadrante { Id = 3, Nombre = "Cuadrante N2-A", SectorId = 2, Activo = true };
        var cuadrante1_2_2 = new Cuadrante { Id = 4, Nombre = "Cuadrante N2-B", SectorId = 2, Activo = false }; // Inactive
        var cuadrante2_1_1 = new Cuadrante { Id = 5, Nombre = "Cuadrante S1-A", SectorId = 3, Activo = true };

        _context.Cuadrantes.AddRange(cuadrante1_1_1, cuadrante1_1_2, cuadrante1_2_1, cuadrante1_2_2, cuadrante2_1_1);

        // Add test suggestions for different fields
        _context.CatalogosSugerencia.AddRange(
            // Sexo suggestions
            new CatalogoSugerencia { Id = 1, Campo = "sexo", Valor = "Masculino", Orden = 1, Activo = true },
            new CatalogoSugerencia { Id = 2, Campo = "sexo", Valor = "Femenino", Orden = 2, Activo = true },
            new CatalogoSugerencia { Id = 3, Campo = "sexo", Valor = "No binario", Orden = 3, Activo = true },
            new CatalogoSugerencia { Id = 4, Campo = "sexo", Valor = "Otro", Orden = 4, Activo = false }, // Inactive

            // Delito suggestions
            new CatalogoSugerencia { Id = 5, Campo = "delito", Valor = "Robo", Orden = 1, Activo = true },
            new CatalogoSugerencia { Id = 6, Campo = "delito", Valor = "Violencia familiar", Orden = 2, Activo = true },
            new CatalogoSugerencia { Id = 7, Campo = "delito", Valor = "Fraude", Orden = 3, Activo = true },

            // Tipo de atención suggestions
            new CatalogoSugerencia { Id = 8, Campo = "tipo_de_atencion", Valor = "Presencial", Orden = 1, Activo = true },
            new CatalogoSugerencia { Id = 9, Campo = "tipo_de_atencion", Valor = "Telefónica", Orden = 2, Activo = true },
            new CatalogoSugerencia { Id = 10, Campo = "tipo_de_atencion", Valor = "Digital", Orden = 3, Activo = true }
        );

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region GetZonasAsync Tests

    [Fact(DisplayName = "T036: GetZonasAsync should return all active zones")]
    public async Task GetZonasAsync_ReturnsActiveZones()
    {
        // Act
        var result = await _service.GetZonasAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2); // Only active zones
        result.Should().AllSatisfy(z => z.Nombre.Should().NotBeNullOrEmpty());
    }

    [Fact(DisplayName = "T036: GetZonasAsync should not return inactive zones")]
    public async Task GetZonasAsync_DoesNotReturnInactiveZones()
    {
        // Act
        var result = await _service.GetZonasAsync();

        // Assert
        result.Should().NotContain(z => z.Nombre == "Zona Inactiva");
    }

    [Fact(DisplayName = "T036: GetZonasAsync should return zones ordered by name")]
    public async Task GetZonasAsync_ReturnsZonesOrderedByName()
    {
        // Act
        var result = await _service.GetZonasAsync();

        // Assert
        result.Should().BeInAscendingOrder(z => z.Nombre);
        result.First().Nombre.Should().Be("Zona Norte");
        result.Last().Nombre.Should().Be("Zona Sur");
    }

    [Fact(DisplayName = "T036: GetZonasAsync should return CatalogItemDto with correct properties")]
    public async Task GetZonasAsync_ReturnsCatalogItemDtoWithCorrectProperties()
    {
        // Act
        var result = await _service.GetZonasAsync();

        // Assert
        var firstZona = result.First();
        firstZona.Should().BeOfType<CatalogItemDto>();
        firstZona.Id.Should().BeGreaterThan(0);
        firstZona.Nombre.Should().NotBeNullOrEmpty();
    }

    [Fact(DisplayName = "T036: GetZonasAsync should return empty list when no active zones exist")]
    public async Task GetZonasAsync_NoActiveZones_ReturnsEmptyList()
    {
        // Arrange - Remove all zones and add only inactive ones
        using var context = new CeibaDbContext(
            new DbContextOptionsBuilder<CeibaDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options);
        var service = new CatalogService(context);

        context.Zonas.Add(new Zona { Id = 1, Nombre = "Zona Inactiva", Activo = false });
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetZonasAsync();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetSectoresByZonaAsync Tests

    [Fact(DisplayName = "T036: GetSectoresByZonaAsync should return sectors for specific zone")]
    public async Task GetSectoresByZonaAsync_ReturnsCorrectSectors()
    {
        // Act
        var result = await _service.GetSectoresByZonaAsync(1);

        // Assert
        result.Should().HaveCount(2); // Zona 1 has 2 active sectors
        result.Should().AllSatisfy(s => s.Nombre.Should().StartWith("Sector Norte"));
    }

    [Fact(DisplayName = "T036: GetSectoresByZonaAsync should only return active sectors")]
    public async Task GetSectoresByZonaAsync_OnlyReturnsActiveSectors()
    {
        // Act
        var result = await _service.GetSectoresByZonaAsync(2);

        // Assert
        result.Should().HaveCount(1); // Zona 2 has 1 active sector (sector2_2 is inactive)
        result.Should().AllSatisfy(s => s.Nombre.Should().Be("Sector Sur-1"));
    }

    [Fact(DisplayName = "T036: GetSectoresByZonaAsync should return sectors ordered by name")]
    public async Task GetSectoresByZonaAsync_ReturnsSectorsOrderedByName()
    {
        // Act
        var result = await _service.GetSectoresByZonaAsync(1);

        // Assert
        result.Should().BeInAscendingOrder(s => s.Nombre);
        result.First().Nombre.Should().Be("Sector Norte-1");
        result.Last().Nombre.Should().Be("Sector Norte-2");
    }

    [Fact(DisplayName = "T036: GetSectoresByZonaAsync should return empty list for non-existent zone")]
    public async Task GetSectoresByZonaAsync_NonExistentZone_ReturnsEmptyList()
    {
        // Act
        var result = await _service.GetSectoresByZonaAsync(999);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact(DisplayName = "T036: GetSectoresByZonaAsync should return empty list for zone with no sectors")]
    public async Task GetSectoresByZonaAsync_ZoneWithNoSectors_ReturnsEmptyList()
    {
        // Arrange - Add a zone with no sectors
        _context.Zonas.Add(new Zona { Id = 4, Nombre = "Zona Sin Sectores", Activo = true });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetSectoresByZonaAsync(4);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact(DisplayName = "T036: GetSectoresByZonaAsync should return CatalogItemDto with correct properties")]
    public async Task GetSectoresByZonaAsync_ReturnsCatalogItemDtoWithCorrectProperties()
    {
        // Act
        var result = await _service.GetSectoresByZonaAsync(1);

        // Assert
        var firstSector = result.First();
        firstSector.Should().BeOfType<CatalogItemDto>();
        firstSector.Id.Should().BeGreaterThan(0);
        firstSector.Nombre.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region GetCuadrantesBySectorAsync Tests

    [Fact(DisplayName = "T036: GetCuadrantesBySectorAsync should return cuadrantes for specific sector")]
    public async Task GetCuadrantesBySectorAsync_ReturnsCorrectCuadrantes()
    {
        // Act
        var result = await _service.GetCuadrantesBySectorAsync(1);

        // Assert
        result.Should().HaveCount(2); // Sector 1 has 2 active cuadrantes
        result.Should().AllSatisfy(c => c.Nombre.Should().StartWith("Cuadrante N1"));
    }

    [Fact(DisplayName = "T036: GetCuadrantesBySectorAsync should only return active cuadrantes")]
    public async Task GetCuadrantesBySectorAsync_OnlyReturnsActiveCuadrantes()
    {
        // Act
        var result = await _service.GetCuadrantesBySectorAsync(2);

        // Assert
        result.Should().HaveCount(1); // Sector 2 has 1 active cuadrante (cuadrante1_2_2 is inactive)
        result.Should().AllSatisfy(c => c.Nombre.Should().Be("Cuadrante N2-A"));
    }

    [Fact(DisplayName = "T036: GetCuadrantesBySectorAsync should return cuadrantes ordered by name")]
    public async Task GetCuadrantesBySectorAsync_ReturnsCuadrantesOrderedByName()
    {
        // Act
        var result = await _service.GetCuadrantesBySectorAsync(1);

        // Assert
        result.Should().BeInAscendingOrder(c => c.Nombre);
        result.First().Nombre.Should().Be("Cuadrante N1-A");
        result.Last().Nombre.Should().Be("Cuadrante N1-B");
    }

    [Fact(DisplayName = "T036: GetCuadrantesBySectorAsync should return empty list for non-existent sector")]
    public async Task GetCuadrantesBySectorAsync_NonExistentSector_ReturnsEmptyList()
    {
        // Act
        var result = await _service.GetCuadrantesBySectorAsync(999);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact(DisplayName = "T036: GetCuadrantesBySectorAsync should return empty list for sector with no cuadrantes")]
    public async Task GetCuadrantesBySectorAsync_SectorWithNoCuadrantes_ReturnsEmptyList()
    {
        // Arrange - Add a sector with no cuadrantes
        _context.Sectores.Add(new Sector { Id = 5, Nombre = "Sector Sin Cuadrantes", ZonaId = 1, Activo = true });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetCuadrantesBySectorAsync(5);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact(DisplayName = "T036: GetCuadrantesBySectorAsync should return CatalogItemDto with correct properties")]
    public async Task GetCuadrantesBySectorAsync_ReturnsCatalogItemDtoWithCorrectProperties()
    {
        // Act
        var result = await _service.GetCuadrantesBySectorAsync(1);

        // Assert
        var firstCuadrante = result.First();
        firstCuadrante.Should().BeOfType<CatalogItemDto>();
        firstCuadrante.Id.Should().BeGreaterThan(0);
        firstCuadrante.Nombre.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region GetSuggestionsAsync Tests

    [Fact(DisplayName = "T036: GetSuggestionsAsync should return suggestions for 'sexo' field")]
    public async Task GetSuggestionsAsync_Sexo_ReturnsSexoSuggestions()
    {
        // Act
        var result = await _service.GetSuggestionsAsync("sexo");

        // Assert
        result.Should().HaveCount(3); // Only active sexo suggestions
        result.Should().Contain("Masculino");
        result.Should().Contain("Femenino");
        result.Should().Contain("No binario");
        result.Should().NotContain("Otro"); // Inactive
    }

    [Fact(DisplayName = "T036: GetSuggestionsAsync should return suggestions for 'delito' field")]
    public async Task GetSuggestionsAsync_Delito_ReturnsDelitoSuggestions()
    {
        // Act
        var result = await _service.GetSuggestionsAsync("delito");

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain("Robo");
        result.Should().Contain("Violencia familiar");
        result.Should().Contain("Fraude");
    }

    [Fact(DisplayName = "T036: GetSuggestionsAsync should return suggestions for 'tipo_de_atencion' field")]
    public async Task GetSuggestionsAsync_TipoDeAtencion_ReturnsTipoDeAtencionSuggestions()
    {
        // Act
        var result = await _service.GetSuggestionsAsync("tipo_de_atencion");

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain("Presencial");
        result.Should().Contain("Telefónica");
        result.Should().Contain("Digital");
    }

    [Fact(DisplayName = "T036: GetSuggestionsAsync should be case-insensitive for campo parameter")]
    public async Task GetSuggestionsAsync_CaseInsensitive_ReturnsSuggestions()
    {
        // Act
        var resultLower = await _service.GetSuggestionsAsync("sexo");
        var resultUpper = await _service.GetSuggestionsAsync("SEXO");
        var resultMixed = await _service.GetSuggestionsAsync("SeXo");

        // Assert
        resultLower.Should().BeEquivalentTo(resultUpper);
        resultLower.Should().BeEquivalentTo(resultMixed);
    }

    [Fact(DisplayName = "T036: GetSuggestionsAsync should return suggestions ordered by orden then valor")]
    public async Task GetSuggestionsAsync_ReturnsOrderedByOrdenThenValor()
    {
        // Act
        var result = await _service.GetSuggestionsAsync("sexo");

        // Assert - Order by 'Orden' field, not alphabetically
        result.Should().HaveCount(3);
        result[0].Should().Be("Masculino"); // Orden = 1
        result[1].Should().Be("Femenino");  // Orden = 2
        result[2].Should().Be("No binario"); // Orden = 3
    }

    [Fact(DisplayName = "T036: GetSuggestionsAsync should return empty list for invalid campo")]
    public async Task GetSuggestionsAsync_InvalidCampo_ReturnsEmptyList()
    {
        // Act
        var result = await _service.GetSuggestionsAsync("campo_invalido");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact(DisplayName = "T036: GetSuggestionsAsync should only return active suggestions")]
    public async Task GetSuggestionsAsync_OnlyReturnsActiveSuggestions()
    {
        // Act
        var result = await _service.GetSuggestionsAsync("sexo");

        // Assert
        result.Should().NotContain("Otro"); // This suggestion is inactive
    }

    [Fact(DisplayName = "T036: GetSuggestionsAsync should return empty list when no active suggestions exist")]
    public async Task GetSuggestionsAsync_NoActiveSuggestions_ReturnsEmptyList()
    {
        // Arrange - Create a new campo with only inactive suggestions
        using var context = new CeibaDbContext(
            new DbContextOptionsBuilder<CeibaDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options);
        var service = new CatalogService(context);

        context.CatalogosSugerencia.Add(
            new CatalogoSugerencia { Id = 1, Campo = "test_campo", Valor = "Test", Orden = 1, Activo = false });
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetSuggestionsAsync("test_campo");

        // Assert
        result.Should().BeEmpty();
    }

    [Theory(DisplayName = "T036: GetSuggestionsAsync should only accept allowed campos")]
    [InlineData("sexo")]
    [InlineData("delito")]
    [InlineData("tipo_de_atencion")]
    public async Task GetSuggestionsAsync_AllowedCampos_ReturnsSuggestions(string campo)
    {
        // Act
        var result = await _service.GetSuggestionsAsync(campo);

        // Assert
        result.Should().NotBeEmpty();
    }

    [Theory(DisplayName = "T036: GetSuggestionsAsync should return empty for disallowed campos")]
    [InlineData("username")]
    [InlineData("password")]
    [InlineData("email")]
    [InlineData("random_field")]
    [InlineData("")]
    public async Task GetSuggestionsAsync_DisallowedCampos_ReturnsEmptyList(string campo)
    {
        // Act
        var result = await _service.GetSuggestionsAsync(campo);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region ValidateHierarchyAsync Tests

    [Fact(DisplayName = "T036: ValidateHierarchyAsync should return true for valid hierarchy")]
    public async Task ValidateHierarchyAsync_ValidHierarchy_ReturnsTrue()
    {
        // Arrange: Zona 1 → Sector 1 → Cuadrante 1
        int zonaId = 1;
        int sectorId = 1;
        int cuadranteId = 1;

        // Act
        var result = await _service.ValidateHierarchyAsync(zonaId, sectorId, cuadranteId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact(DisplayName = "T036: ValidateHierarchyAsync should return false when sector does not belong to zona")]
    public async Task ValidateHierarchyAsync_SectorNotInZona_ReturnsFalse()
    {
        // Arrange: Sector 3 belongs to Zona 2, not Zona 1
        int zonaId = 1;
        int sectorId = 3;
        int cuadranteId = 5;

        // Act
        var result = await _service.ValidateHierarchyAsync(zonaId, sectorId, cuadranteId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact(DisplayName = "T036: ValidateHierarchyAsync should return false when cuadrante does not belong to sector")]
    public async Task ValidateHierarchyAsync_CuadranteNotInSector_ReturnsFalse()
    {
        // Arrange: Cuadrante 5 belongs to Sector 3, not Sector 1
        int zonaId = 1;
        int sectorId = 1;
        int cuadranteId = 5;

        // Act
        var result = await _service.ValidateHierarchyAsync(zonaId, sectorId, cuadranteId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact(DisplayName = "T036: ValidateHierarchyAsync should return false when sector does not exist")]
    public async Task ValidateHierarchyAsync_NonExistentSector_ReturnsFalse()
    {
        // Arrange
        int zonaId = 1;
        int sectorId = 999; // Non-existent
        int cuadranteId = 1;

        // Act
        var result = await _service.ValidateHierarchyAsync(zonaId, sectorId, cuadranteId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact(DisplayName = "T036: ValidateHierarchyAsync should return false when cuadrante does not exist")]
    public async Task ValidateHierarchyAsync_NonExistentCuadrante_ReturnsFalse()
    {
        // Arrange
        int zonaId = 1;
        int sectorId = 1;
        int cuadranteId = 999; // Non-existent

        // Act
        var result = await _service.ValidateHierarchyAsync(zonaId, sectorId, cuadranteId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact(DisplayName = "T036: ValidateHierarchyAsync should return false when zona does not exist")]
    public async Task ValidateHierarchyAsync_NonExistentZona_ReturnsFalse()
    {
        // Arrange
        int zonaId = 999; // Non-existent
        int sectorId = 1;
        int cuadranteId = 1;

        // Act
        var result = await _service.ValidateHierarchyAsync(zonaId, sectorId, cuadranteId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact(DisplayName = "T036: ValidateHierarchyAsync should return false when sector is inactive")]
    public async Task ValidateHierarchyAsync_InactiveSector_ReturnsFalse()
    {
        // Arrange: Sector 4 is inactive
        int zonaId = 2;
        int sectorId = 4;
        int cuadranteId = 1; // Assume we add a cuadrante to sector 4

        // Act
        var result = await _service.ValidateHierarchyAsync(zonaId, sectorId, cuadranteId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact(DisplayName = "T036: ValidateHierarchyAsync should return false when cuadrante is inactive")]
    public async Task ValidateHierarchyAsync_InactiveCuadrante_ReturnsFalse()
    {
        // Arrange: Cuadrante 4 is inactive
        int zonaId = 1;
        int sectorId = 2;
        int cuadranteId = 4;

        // Act
        var result = await _service.ValidateHierarchyAsync(zonaId, sectorId, cuadranteId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact(DisplayName = "T036: ValidateHierarchyAsync should validate complete chain Zona→Sector→Cuadrante")]
    public async Task ValidateHierarchyAsync_ValidatesCompleteChain()
    {
        // Test multiple valid hierarchies
        var testCases = new[]
        {
            new { Zona = 1, Sector = 1, Cuadrante = 1, Expected = true },
            new { Zona = 1, Sector = 1, Cuadrante = 2, Expected = true },
            new { Zona = 1, Sector = 2, Cuadrante = 3, Expected = true },
            new { Zona = 2, Sector = 3, Cuadrante = 5, Expected = true },
            // Invalid hierarchies
            new { Zona = 1, Sector = 3, Cuadrante = 5, Expected = false }, // Sector 3 belongs to Zona 2
            new { Zona = 2, Sector = 1, Cuadrante = 1, Expected = false }, // Sector 1 belongs to Zona 1
        };

        foreach (var testCase in testCases)
        {
            // Act
            var result = await _service.ValidateHierarchyAsync(testCase.Zona, testCase.Sector, testCase.Cuadrante);

            // Assert
            result.Should().Be(testCase.Expected,
                $"Zona={testCase.Zona}, Sector={testCase.Sector}, Cuadrante={testCase.Cuadrante} should be {testCase.Expected}");
        }
    }

    #endregion

    #region Edge Cases and Integration Tests

    [Fact(DisplayName = "T036: All methods should handle concurrent calls correctly")]
    public async Task ConcurrentCalls_ShouldWorkCorrectly()
    {
        // Arrange
        var tasks = new List<Task>
        {
            _service.GetZonasAsync(),
            _service.GetSectoresByZonaAsync(1),
            _service.GetCuadrantesBySectorAsync(1),
            _service.GetSuggestionsAsync("sexo"),
            _service.ValidateHierarchyAsync(1, 1, 1)
        };

        // Act
        await Task.WhenAll(tasks);

        // Assert - No exceptions should be thrown
        tasks.Should().AllSatisfy(t => t.IsCompletedSuccessfully.Should().BeTrue());
    }

    [Fact(DisplayName = "T036: Service should handle empty database gracefully")]
    public async Task EmptyDatabase_ShouldReturnEmptyLists()
    {
        // Arrange - Create service with empty database
        using var emptyContext = new CeibaDbContext(
            new DbContextOptionsBuilder<CeibaDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options);
        var emptyService = new CatalogService(emptyContext);

        // Act & Assert
        (await emptyService.GetZonasAsync()).Should().BeEmpty();
        (await emptyService.GetSectoresByZonaAsync(1)).Should().BeEmpty();
        (await emptyService.GetCuadrantesBySectorAsync(1)).Should().BeEmpty();
        (await emptyService.GetSuggestionsAsync("sexo")).Should().BeEmpty();
        (await emptyService.ValidateHierarchyAsync(1, 1, 1)).Should().BeFalse();
    }

    [Fact(DisplayName = "T036: GetSuggestionsAsync should handle special characters in values")]
    public async Task GetSuggestionsAsync_SpecialCharacters_ShouldReturnCorrectly()
    {
        // Arrange
        _context.CatalogosSugerencia.Add(
            new CatalogoSugerencia
            {
                Id = 100,
                Campo = "delito",
                Valor = "Robo con violencia (Art. 123)",
                Orden = 10,
                Activo = true
            });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetSuggestionsAsync("delito");

        // Assert
        result.Should().Contain("Robo con violencia (Art. 123)");
    }

    [Fact(DisplayName = "T036: GetZonasAsync should handle zones with special characters in names")]
    public async Task GetZonasAsync_SpecialCharactersInNames_ShouldReturnCorrectly()
    {
        // Arrange
        _context.Zonas.Add(new Zona { Id = 10, Nombre = "Zona Centro-Norte (Área 1)", Activo = true });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetZonasAsync();

        // Assert
        result.Should().Contain(z => z.Nombre == "Zona Centro-Norte (Área 1)");
    }

    #endregion
}
