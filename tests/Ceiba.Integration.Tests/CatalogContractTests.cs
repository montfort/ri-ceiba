using System.Net;
using System.Net.Http.Json;
using Ceiba.Shared.DTOs;
using FluentAssertions;
using Xunit;

namespace Ceiba.Integration.Tests;

/// <summary>
/// Contract tests for Catalog Management API endpoints (US3)
/// T062: Validates catalog endpoints (Zonas, Sectores, Cuadrantes, Sugerencias) require ADMIN role
/// </summary>
[Collection("Integration Tests")]
public class CatalogContractTests : IClassFixture<CeibaWebApplicationFactory>
{
    private readonly CeibaWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CatalogContractTests(CeibaWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    #region Zonas Endpoints - Authentication Required

    [Fact(DisplayName = "T062: GET /api/admin/catalogs/zonas without authentication should return 401")]
    public async Task GetZonas_WithoutAuth_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/catalogs/zonas");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "T062: GET /api/admin/catalogs/zonas/{id} without authentication should return 401")]
    public async Task GetZonaById_WithoutAuth_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/catalogs/zonas/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "T062: POST /api/admin/catalogs/zonas without authentication should return 401")]
    public async Task CreateZona_WithoutAuth_Returns401()
    {
        // Arrange
        var createDto = new CreateZonaDto
        {
            Nombre = "Test Zona",
            Activo = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/admin/catalogs/zonas", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "T062: PUT /api/admin/catalogs/zonas/{id} without authentication should return 401")]
    public async Task UpdateZona_WithoutAuth_Returns401()
    {
        // Arrange
        var updateDto = new CreateZonaDto
        {
            Nombre = "Updated Zona",
            Activo = true
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/admin/catalogs/zonas/1", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "T062: POST /api/admin/catalogs/zonas/{id}/toggle without authentication should return 401")]
    public async Task ToggleZona_WithoutAuth_Returns401()
    {
        // Act
        var response = await _client.PostAsync("/api/admin/catalogs/zonas/1/toggle", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Sectores Endpoints - Authentication Required

    [Fact(DisplayName = "T062: GET /api/admin/catalogs/sectores without authentication should return 401")]
    public async Task GetSectores_WithoutAuth_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/catalogs/sectores");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "T062: GET /api/admin/catalogs/sectores/{id} without authentication should return 401")]
    public async Task GetSectorById_WithoutAuth_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/catalogs/sectores/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "T062: POST /api/admin/catalogs/sectores without authentication should return 401")]
    public async Task CreateSector_WithoutAuth_Returns401()
    {
        // Arrange
        var createDto = new CreateSectorDto
        {
            Nombre = "Test Sector",
            ZonaId = 1,
            Activo = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/admin/catalogs/sectores", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "T062: PUT /api/admin/catalogs/sectores/{id} without authentication should return 401")]
    public async Task UpdateSector_WithoutAuth_Returns401()
    {
        // Arrange
        var updateDto = new CreateSectorDto
        {
            Nombre = "Updated Sector",
            ZonaId = 1,
            Activo = true
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/admin/catalogs/sectores/1", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "T062: POST /api/admin/catalogs/sectores/{id}/toggle without authentication should return 401")]
    public async Task ToggleSector_WithoutAuth_Returns401()
    {
        // Act
        var response = await _client.PostAsync("/api/admin/catalogs/sectores/1/toggle", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Cuadrantes Endpoints - Authentication Required

    [Fact(DisplayName = "T062: GET /api/admin/catalogs/cuadrantes without authentication should return 401")]
    public async Task GetCuadrantes_WithoutAuth_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/catalogs/cuadrantes");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "T062: GET /api/admin/catalogs/cuadrantes/{id} without authentication should return 401")]
    public async Task GetCuadranteById_WithoutAuth_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/catalogs/cuadrantes/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "T062: POST /api/admin/catalogs/cuadrantes without authentication should return 401")]
    public async Task CreateCuadrante_WithoutAuth_Returns401()
    {
        // Arrange
        var createDto = new CreateCuadranteDto
        {
            Nombre = "Test Cuadrante",
            SectorId = 1,
            Activo = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/admin/catalogs/cuadrantes", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "T062: PUT /api/admin/catalogs/cuadrantes/{id} without authentication should return 401")]
    public async Task UpdateCuadrante_WithoutAuth_Returns401()
    {
        // Arrange
        var updateDto = new CreateCuadranteDto
        {
            Nombre = "Updated Cuadrante",
            SectorId = 1,
            Activo = true
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/admin/catalogs/cuadrantes/1", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "T062: POST /api/admin/catalogs/cuadrantes/{id}/toggle without authentication should return 401")]
    public async Task ToggleCuadrante_WithoutAuth_Returns401()
    {
        // Act
        var response = await _client.PostAsync("/api/admin/catalogs/cuadrantes/1/toggle", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Sugerencias Endpoints - Authentication Required

    [Fact(DisplayName = "T062: GET /api/admin/catalogs/sugerencias without authentication should return 401")]
    public async Task GetSugerencias_WithoutAuth_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/catalogs/sugerencias");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "T062: GET /api/admin/catalogs/sugerencias/campos without authentication should return 401")]
    public async Task GetSugerenciaCampos_WithoutAuth_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/catalogs/sugerencias/campos");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "T062: GET /api/admin/catalogs/sugerencias/{id} without authentication should return 401")]
    public async Task GetSugerenciaById_WithoutAuth_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/catalogs/sugerencias/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "T062: POST /api/admin/catalogs/sugerencias without authentication should return 401")]
    public async Task CreateSugerencia_WithoutAuth_Returns401()
    {
        // Arrange
        var createDto = new CreateSugerenciaDto
        {
            Campo = "sexo",
            Valor = "Test Value",
            Orden = 1,
            Activo = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/admin/catalogs/sugerencias", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "T062: PUT /api/admin/catalogs/sugerencias/{id} without authentication should return 401")]
    public async Task UpdateSugerencia_WithoutAuth_Returns401()
    {
        // Arrange
        var updateDto = new CreateSugerenciaDto
        {
            Campo = "sexo",
            Valor = "Updated Value",
            Orden = 1,
            Activo = true
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/admin/catalogs/sugerencias/1", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "T062: DELETE /api/admin/catalogs/sugerencias/{id} without authentication should return 401")]
    public async Task DeleteSugerencia_WithoutAuth_Returns401()
    {
        // Act
        var response = await _client.DeleteAsync("/api/admin/catalogs/sugerencias/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region DTO Validation Tests

    [Fact(DisplayName = "T062: ZonaDto should have expected properties")]
    public void ZonaDto_ShouldHaveExpectedProperties()
    {
        // Arrange
        var dto = new ZonaDto
        {
            Id = 1,
            Nombre = "Test Zona",
            Activo = true,
            SectoresCount = 5
        };

        // Assert
        dto.Id.Should().Be(1);
        dto.Nombre.Should().Be("Test Zona");
        dto.Activo.Should().BeTrue();
        dto.SectoresCount.Should().Be(5);
    }

    [Fact(DisplayName = "T062: SectorDto should include Zona reference")]
    public void SectorDto_ShouldIncludeZonaReference()
    {
        // Arrange
        var dto = new SectorDto
        {
            Id = 1,
            Nombre = "Test Sector",
            ZonaId = 1,
            ZonaNombre = "Test Zona",
            Activo = true,
            CuadrantesCount = 3
        };

        // Assert
        dto.ZonaId.Should().Be(1);
        dto.ZonaNombre.Should().Be("Test Zona");
        dto.CuadrantesCount.Should().Be(3);
    }

    [Fact(DisplayName = "T062: CuadranteDto should include Sector and Zona references")]
    public void CuadranteDto_ShouldIncludeSectorAndZonaReferences()
    {
        // Arrange
        var dto = new CuadranteDto
        {
            Id = 1,
            Nombre = "Test Cuadrante",
            SectorId = 1,
            SectorNombre = "Test Sector",
            ZonaId = 1,
            ZonaNombre = "Test Zona",
            Activo = true
        };

        // Assert
        dto.SectorId.Should().Be(1);
        dto.SectorNombre.Should().Be("Test Sector");
        dto.ZonaId.Should().Be(1);
        dto.ZonaNombre.Should().Be("Test Zona");
    }

    [Fact(DisplayName = "T062: SugerenciaDto should have all required fields")]
    public void SugerenciaDto_ShouldHaveRequiredFields()
    {
        // Arrange
        var dto = new SugerenciaDto
        {
            Id = 1,
            Campo = "sexo",
            Valor = "Masculino",
            Orden = 1,
            Activo = true
        };

        // Assert
        dto.Id.Should().Be(1);
        dto.Campo.Should().Be("sexo");
        dto.Valor.Should().Be("Masculino");
        dto.Orden.Should().Be(1);
        dto.Activo.Should().BeTrue();
    }

    [Fact(DisplayName = "T062: CreateSectorDto should require ZonaId")]
    public void CreateSectorDto_ShouldRequireZonaId()
    {
        // Arrange
        var zonaIdProperty = typeof(CreateSectorDto).GetProperty(nameof(CreateSectorDto.ZonaId));

        // Assert
        zonaIdProperty.Should().NotBeNull();
        zonaIdProperty!.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.RequiredAttribute), true)
            .Should().NotBeEmpty("ZonaId should be required");
        zonaIdProperty.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.RangeAttribute), true)
            .Should().NotBeEmpty("ZonaId should have Range validation");
    }

    [Fact(DisplayName = "T062: CreateCuadranteDto should require SectorId")]
    public void CreateCuadranteDto_ShouldRequireSectorId()
    {
        // Arrange
        var sectorIdProperty = typeof(CreateCuadranteDto).GetProperty(nameof(CreateCuadranteDto.SectorId));

        // Assert
        sectorIdProperty.Should().NotBeNull();
        sectorIdProperty!.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.RequiredAttribute), true)
            .Should().NotBeEmpty("SectorId should be required");
        sectorIdProperty.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.RangeAttribute), true)
            .Should().NotBeEmpty("SectorId should have Range validation");
    }

    [Fact(DisplayName = "T062: SugerenciaCampos should define all expected field types")]
    public void SugerenciaCampos_ShouldDefineExpectedFieldTypes()
    {
        // Assert
        SugerenciaCampos.All.Should().Contain("sexo");
        SugerenciaCampos.All.Should().Contain("delito");
        SugerenciaCampos.All.Should().Contain("tipo_de_atencion");
        SugerenciaCampos.All.Should().Contain("turno_ceiba");
        SugerenciaCampos.All.Should().Contain("tipo_de_accion");
        SugerenciaCampos.All.Should().Contain("traslados");
    }

    [Fact(DisplayName = "T062: SugerenciaCampos.GetDisplayName should return human-readable names")]
    public void SugerenciaCampos_GetDisplayName_ShouldReturnReadableNames()
    {
        // Assert
        SugerenciaCampos.GetDisplayName("sexo").Should().Be("Sexo");
        SugerenciaCampos.GetDisplayName("delito").Should().Be("Tipo de Delito");
        SugerenciaCampos.GetDisplayName("tipo_de_atencion").Should().Be("Tipo de Atención");
        SugerenciaCampos.GetDisplayName("turno_ceiba").Should().Be("Turno CEIBA");
    }

    #endregion

    #region Geographic Hierarchy Tests

    [Fact(DisplayName = "T062: Geographic hierarchy should be Zona > Sector > Cuadrante")]
    public void GeographicHierarchy_ShouldBeCorrect()
    {
        // Verify DTOs have correct parent references

        // Sector references Zona
        var sectorZonaId = typeof(SectorDto).GetProperty(nameof(SectorDto.ZonaId));
        sectorZonaId.Should().NotBeNull();

        // Cuadrante references Sector and Zona
        var cuadranteSectorId = typeof(CuadranteDto).GetProperty(nameof(CuadranteDto.SectorId));
        var cuadranteZonaId = typeof(CuadranteDto).GetProperty(nameof(CuadranteDto.ZonaId));
        cuadranteSectorId.Should().NotBeNull();
        cuadranteZonaId.Should().NotBeNull();
    }

    #endregion
}
