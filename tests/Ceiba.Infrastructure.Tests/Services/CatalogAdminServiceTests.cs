using Ceiba.Core.Entities;
using Ceiba.Infrastructure.Data;
using Ceiba.Infrastructure.Services;
using Ceiba.Shared.DTOs;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ceiba.Infrastructure.Tests.Services;

/// <summary>
/// Unit tests for CatalogAdminService (US5: T105)
/// Tests suggestion management operations.
/// </summary>
public class CatalogAdminServiceTests : IDisposable
{
    private readonly CeibaDbContext _context;
    private readonly ILogger<CatalogAdminService> _logger;
    private readonly CatalogAdminService _service;
    private readonly Guid _adminUserId = Guid.NewGuid();

    public CatalogAdminServiceTests()
    {
        var options = new DbContextOptionsBuilder<CeibaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CeibaDbContext(options);
        _logger = NullLogger<CatalogAdminService>.Instance;
        _service = new CatalogAdminService(_context, _logger);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Add zones for testing
        var zona = new Zona { Id = 1, Nombre = "Zona Test", Activo = true };
        _context.Zonas.Add(zona);

        // Region (Zona → Región → Sector → Cuadrante)
        var region = new Region { Id = 1, Nombre = "Región Test", ZonaId = 1, Activo = true };
        _context.Regiones.Add(region);

        var sector = new Sector { Id = 1, Nombre = "Sector Test", RegionId = 1, Activo = true };
        _context.Sectores.Add(sector);

        var cuadrante = new Cuadrante { Id = 1, Nombre = "Cuadrante Test", SectorId = 1, Activo = true };
        _context.Cuadrantes.Add(cuadrante);

        // Add test suggestions
        _context.CatalogosSugerencia.AddRange(
            new CatalogoSugerencia { Id = 1, Campo = "sexo", Valor = "Masculino", Orden = 1, Activo = true },
            new CatalogoSugerencia { Id = 2, Campo = "sexo", Valor = "Femenino", Orden = 2, Activo = true },
            new CatalogoSugerencia { Id = 3, Campo = "delito", Valor = "Robo", Orden = 1, Activo = true },
            new CatalogoSugerencia { Id = 4, Campo = "delito", Valor = "Fraude", Orden = 2, Activo = false }
        );

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region GetSugerenciasAsync Tests

    [Fact(DisplayName = "T105: GetSugerenciasAsync should return all suggestions when no filter")]
    public async Task GetSugerenciasAsync_NoFilter_ReturnsAll()
    {
        // Act
        var result = await _service.GetSugerenciasAsync();

        // Assert
        result.Should().HaveCount(4);
    }

    [Fact(DisplayName = "T105: GetSugerenciasAsync should filter by campo")]
    public async Task GetSugerenciasAsync_FilterByCampo_ReturnsFiltered()
    {
        // Act
        var result = await _service.GetSugerenciasAsync(campo: "sexo");

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(s => s.Campo == "sexo");
    }

    [Fact(DisplayName = "T105: GetSugerenciasAsync should filter by activo")]
    public async Task GetSugerenciasAsync_FilterByActivo_ReturnsFiltered()
    {
        // Act
        var result = await _service.GetSugerenciasAsync(activo: true);

        // Assert
        result.Should().HaveCount(3);
        result.Should().OnlyContain(s => s.Activo);
    }

    [Fact(DisplayName = "T105: GetSugerenciasAsync should filter by both campo and activo")]
    public async Task GetSugerenciasAsync_FilterByCampoAndActivo_ReturnsFiltered()
    {
        // Act
        var result = await _service.GetSugerenciasAsync(campo: "delito", activo: true);

        // Assert
        result.Should().HaveCount(1);
        result.First().Valor.Should().Be("Robo");
    }

    [Fact(DisplayName = "T105: GetSugerenciasAsync should return ordered by campo then orden then valor")]
    public async Task GetSugerenciasAsync_ShouldReturnOrdered()
    {
        // Act
        var result = await _service.GetSugerenciasAsync();

        // Assert
        result.Should().BeInAscendingOrder(s => s.Campo)
            .And.ThenBeInAscendingOrder(s => s.Orden);
    }

    #endregion

    #region GetSugerenciaByIdAsync Tests

    [Fact(DisplayName = "T105: GetSugerenciaByIdAsync should return suggestion when exists")]
    public async Task GetSugerenciaByIdAsync_Exists_ReturnsSugerencia()
    {
        // Act
        var result = await _service.GetSugerenciaByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Campo.Should().Be("sexo");
        result.Valor.Should().Be("Masculino");
    }

    [Fact(DisplayName = "T105: GetSugerenciaByIdAsync should return null when not exists")]
    public async Task GetSugerenciaByIdAsync_NotExists_ReturnsNull()
    {
        // Act
        var result = await _service.GetSugerenciaByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CreateSugerenciaAsync Tests

    [Fact(DisplayName = "T105: CreateSugerenciaAsync should create new suggestion")]
    public async Task CreateSugerenciaAsync_ValidData_CreatesSugerencia()
    {
        // Arrange
        var createDto = new CreateSugerenciaDto
        {
            Campo = "sexo",
            Valor = "No binario",
            Orden = 3,
            Activo = true
        };

        // Act
        var result = await _service.CreateSugerenciaAsync(createDto, _adminUserId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Campo.Should().Be("sexo");
        result.Valor.Should().Be("No binario");
        result.Orden.Should().Be(3);
        result.Activo.Should().BeTrue();

        // Verify in database
        var inDb = await _context.CatalogosSugerencia.FindAsync(result.Id);
        inDb.Should().NotBeNull();
    }

    [Fact(DisplayName = "T105: CreateSugerenciaAsync should throw for invalid campo")]
    public async Task CreateSugerenciaAsync_InvalidCampo_ThrowsArgumentException()
    {
        // Arrange
        var createDto = new CreateSugerenciaDto
        {
            Campo = "campo_invalido",
            Valor = "Test",
            Orden = 1,
            Activo = true
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateSugerenciaAsync(createDto, _adminUserId));
    }

    [Fact(DisplayName = "T105: CreateSugerenciaAsync should throw for duplicate campo+valor")]
    public async Task CreateSugerenciaAsync_DuplicateCampoValor_ThrowsInvalidOperationException()
    {
        // Arrange
        var createDto = new CreateSugerenciaDto
        {
            Campo = "sexo",
            Valor = "Masculino", // Already exists
            Orden = 5,
            Activo = true
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateSugerenciaAsync(createDto, _adminUserId));
    }

    [Theory(DisplayName = "T105: CreateSugerenciaAsync should accept all valid campos")]
    [InlineData("sexo")]
    [InlineData("delito")]
    [InlineData("tipo_de_atencion")]
    [InlineData("turno_ceiba")]
    [InlineData("tipo_de_accion")]
    [InlineData("traslados")]
    public async Task CreateSugerenciaAsync_ValidCampos_Succeeds(string campo)
    {
        // Arrange
        var createDto = new CreateSugerenciaDto
        {
            Campo = campo,
            Valor = $"Test_{Guid.NewGuid()}",
            Orden = 99,
            Activo = true
        };

        // Act
        var result = await _service.CreateSugerenciaAsync(createDto, _adminUserId);

        // Assert
        result.Should().NotBeNull();
        result.Campo.Should().Be(campo);
    }

    #endregion

    #region UpdateSugerenciaAsync Tests

    [Fact(DisplayName = "T105: UpdateSugerenciaAsync should update existing suggestion")]
    public async Task UpdateSugerenciaAsync_ValidData_UpdatesSugerencia()
    {
        // Arrange
        var updateDto = new CreateSugerenciaDto
        {
            Campo = "sexo",
            Valor = "Masculino Actualizado",
            Orden = 10,
            Activo = false
        };

        // Act
        var result = await _service.UpdateSugerenciaAsync(1, updateDto, _adminUserId);

        // Assert
        result.Should().NotBeNull();
        result.Valor.Should().Be("Masculino Actualizado");
        result.Orden.Should().Be(10);
        result.Activo.Should().BeFalse();
    }

    [Fact(DisplayName = "T105: UpdateSugerenciaAsync should throw for non-existent ID")]
    public async Task UpdateSugerenciaAsync_NotExists_ThrowsKeyNotFoundException()
    {
        // Arrange
        var updateDto = new CreateSugerenciaDto
        {
            Campo = "sexo",
            Valor = "Test",
            Orden = 1,
            Activo = true
        };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.UpdateSugerenciaAsync(999, updateDto, _adminUserId));
    }

    [Fact(DisplayName = "T105: UpdateSugerenciaAsync should throw for invalid campo")]
    public async Task UpdateSugerenciaAsync_InvalidCampo_ThrowsArgumentException()
    {
        // Arrange
        var updateDto = new CreateSugerenciaDto
        {
            Campo = "campo_invalido",
            Valor = "Test",
            Orden = 1,
            Activo = true
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdateSugerenciaAsync(1, updateDto, _adminUserId));
    }

    #endregion

    #region DeleteSugerenciaAsync Tests

    [Fact(DisplayName = "T105: DeleteSugerenciaAsync should delete existing suggestion")]
    public async Task DeleteSugerenciaAsync_Exists_DeletesSugerencia()
    {
        // Arrange
        var countBefore = await _context.CatalogosSugerencia.CountAsync();

        // Act
        await _service.DeleteSugerenciaAsync(1, _adminUserId);

        // Assert
        var countAfter = await _context.CatalogosSugerencia.CountAsync();
        countAfter.Should().Be(countBefore - 1);

        var deleted = await _context.CatalogosSugerencia.FindAsync(1);
        deleted.Should().BeNull();
    }

    [Fact(DisplayName = "T105: DeleteSugerenciaAsync should throw for non-existent ID")]
    public async Task DeleteSugerenciaAsync_NotExists_ThrowsKeyNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.DeleteSugerenciaAsync(999, _adminUserId));
    }

    #endregion

    #region ReorderSugerenciasAsync Tests

    [Fact(DisplayName = "T105: ReorderSugerenciasAsync should update orden values")]
    public async Task ReorderSugerenciasAsync_ValidOrder_UpdatesOrden()
    {
        // Arrange - Swap order of "sexo" suggestions
        var newOrder = new[] { 2, 1 }; // Femenino first, Masculino second

        // Act
        await _service.ReorderSugerenciasAsync("sexo", newOrder, _adminUserId);

        // Assert
        var sugerencias = await _context.CatalogosSugerencia
            .Where(s => s.Campo == "sexo")
            .OrderBy(s => s.Orden)
            .ToListAsync();

        sugerencias[0].Id.Should().Be(2); // Femenino now first
        sugerencias[0].Orden.Should().Be(0);
        sugerencias[1].Id.Should().Be(1); // Masculino now second
        sugerencias[1].Orden.Should().Be(1);
    }

    [Fact(DisplayName = "T105: ReorderSugerenciasAsync should only affect specified campo")]
    public async Task ReorderSugerenciasAsync_OnlyAffectsSpecifiedCampo()
    {
        // Arrange
        var delitosBefore = await _context.CatalogosSugerencia
            .Where(s => s.Campo == "delito")
            .Select(s => new { s.Id, s.Orden })
            .ToListAsync();

        // Act - Reorder "sexo" only
        await _service.ReorderSugerenciasAsync("sexo", new[] { 2, 1 }, _adminUserId);

        // Assert - Delitos should be unchanged
        var delitosAfter = await _context.CatalogosSugerencia
            .Where(s => s.Campo == "delito")
            .ToListAsync();

        foreach (var d in delitosAfter)
        {
            var original = delitosBefore.First(b => b.Id == d.Id);
            d.Orden.Should().Be(original.Orden);
        }
    }

    #endregion

    #region Zona CRUD Tests

    [Fact(DisplayName = "T105: GetZonasAsync should return all zones")]
    public async Task GetZonasAsync_ReturnsAll()
    {
        // Act
        var result = await _service.GetZonasAsync();

        // Assert
        result.Should().NotBeEmpty();
        result.First().Nombre.Should().Be("Zona Test");
    }

    [Fact(DisplayName = "T105: CreateZonaAsync should create new zona")]
    public async Task CreateZonaAsync_ValidData_CreatesZona()
    {
        // Arrange
        var createDto = new CreateZonaDto { Nombre = "Nueva Zona", Activo = true };

        // Act
        var result = await _service.CreateZonaAsync(createDto, _adminUserId);

        // Assert
        result.Should().NotBeNull();
        result.Nombre.Should().Be("Nueva Zona");
    }

    [Fact(DisplayName = "T105: ToggleZonaActivoAsync should toggle activo status")]
    public async Task ToggleZonaActivoAsync_TogglesStatus()
    {
        // Arrange
        var zonaBefore = await _context.Zonas.FindAsync(1);
        var activoBefore = zonaBefore!.Activo;

        // Act
        var result = await _service.ToggleZonaActivoAsync(1, _adminUserId);

        // Assert
        result.Activo.Should().Be(!activoBefore);
    }

    #endregion

    #region Sector CRUD Tests

    [Fact(DisplayName = "T105: GetSectoresAsync should return sectors filtered by region")]
    public async Task GetSectoresAsync_FilterByRegion_ReturnsFiltered()
    {
        // Act
        var result = await _service.GetSectoresAsync(regionId: 1);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().OnlyContain(s => s.RegionId == 1);
    }

    [Fact(DisplayName = "T105: CreateSectorAsync should create new sector")]
    public async Task CreateSectorAsync_ValidData_CreatesSector()
    {
        // Arrange
        var createDto = new CreateSectorDto { Nombre = "Nuevo Sector", RegionId = 1 };

        // Act
        var result = await _service.CreateSectorAsync(createDto, _adminUserId);

        // Assert
        result.Should().NotBeNull();
        result.Nombre.Should().Be("Nuevo Sector");
        result.ZonaId.Should().Be(1);
    }

    [Fact(DisplayName = "T105: CreateSectorAsync should throw for invalid zona")]
    public async Task CreateSectorAsync_InvalidZona_ThrowsKeyNotFoundException()
    {
        // Arrange
        var createDto = new CreateSectorDto { Nombre = "Sector Invalid", RegionId = 999 };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.CreateSectorAsync(createDto, _adminUserId));
    }

    #endregion

    #region Cuadrante CRUD Tests

    [Fact(DisplayName = "T105: GetCuadrantesAsync should return cuadrantes filtered by sector")]
    public async Task GetCuadrantesAsync_FilterBySector_ReturnsFiltered()
    {
        // Act
        var result = await _service.GetCuadrantesAsync(sectorId: 1);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().OnlyContain(c => c.SectorId == 1);
    }

    [Fact(DisplayName = "T105: CreateCuadranteAsync should create new cuadrante")]
    public async Task CreateCuadranteAsync_ValidData_CreatesCuadrante()
    {
        // Arrange
        var createDto = new CreateCuadranteDto { Nombre = "Nuevo Cuadrante", SectorId = 1, Activo = true };

        // Act
        var result = await _service.CreateCuadranteAsync(createDto, _adminUserId);

        // Assert
        result.Should().NotBeNull();
        result.Nombre.Should().Be("Nuevo Cuadrante");
        result.SectorId.Should().Be(1);
    }

    [Fact(DisplayName = "T105: CreateCuadranteAsync should throw for invalid sector")]
    public async Task CreateCuadranteAsync_InvalidSector_ThrowsKeyNotFoundException()
    {
        // Arrange
        var createDto = new CreateCuadranteDto { Nombre = "Test", SectorId = 999, Activo = true };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.CreateCuadranteAsync(createDto, _adminUserId));
    }

    #endregion
}
