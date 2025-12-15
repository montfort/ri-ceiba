using Ceiba.Core.Interfaces;
using Ceiba.Shared.DTOs;
using Ceiba.Web.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Ceiba.Web.Tests.Controllers;

/// <summary>
/// Unit tests for CatalogsController.
/// Tests catalog operations (Zona, Región, Sector, Cuadrante, Suggestions).
/// US1: T040
/// </summary>
[Trait("Category", "Unit")]
public class CatalogsControllerTests
{
    private readonly Mock<ICatalogService> _catalogServiceMock;
    private readonly Mock<ILogger<CatalogsController>> _loggerMock;
    private readonly CatalogsController _controller;

    public CatalogsControllerTests()
    {
        _catalogServiceMock = new Mock<ICatalogService>();
        _loggerMock = new Mock<ILogger<CatalogsController>>();

        _controller = new CatalogsController(
            _catalogServiceMock.Object,
            _loggerMock.Object);
    }

    #region GetZonas Tests

    [Fact(DisplayName = "GetZonas should return OK with list of zones")]
    public async Task GetZonas_Success_ReturnsOkWithZones()
    {
        // Arrange
        var zonas = new List<CatalogItemDto>
        {
            new() { Id = 1, Nombre = "Zona Norte" },
            new() { Id = 2, Nombre = "Zona Sur" },
            new() { Id = 3, Nombre = "Zona Centro" }
        };

        _catalogServiceMock.Setup(x => x.GetZonasAsync())
            .ReturnsAsync(zonas);

        // Act
        var result = await _controller.GetZonas();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedZonas = okResult.Value.Should().BeAssignableTo<List<CatalogItemDto>>().Subject;
        returnedZonas.Should().HaveCount(3);
        returnedZonas[0].Nombre.Should().Be("Zona Norte");
    }

    [Fact(DisplayName = "GetZonas should return empty list when no zones")]
    public async Task GetZonas_NoZones_ReturnsEmptyList()
    {
        // Arrange
        _catalogServiceMock.Setup(x => x.GetZonasAsync())
            .ReturnsAsync(new List<CatalogItemDto>());

        // Act
        var result = await _controller.GetZonas();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedZonas = okResult.Value.Should().BeAssignableTo<List<CatalogItemDto>>().Subject;
        returnedZonas.Should().BeEmpty();
    }

    [Fact(DisplayName = "GetZonas should return 500 when service throws exception")]
    public async Task GetZonas_ServiceThrows_Returns500()
    {
        // Arrange
        _catalogServiceMock.Setup(x => x.GetZonasAsync())
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetZonas();

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetRegiones Tests

    [Fact(DisplayName = "GetRegiones should return OK with list of regions")]
    public async Task GetRegiones_Success_ReturnsOkWithRegions()
    {
        // Arrange
        var regiones = new List<CatalogItemDto>
        {
            new() { Id = 1, Nombre = "Región Centro" },
            new() { Id = 2, Nombre = "Región Este" }
        };

        _catalogServiceMock.Setup(x => x.GetRegionesByZonaAsync(1))
            .ReturnsAsync(regiones);

        // Act
        var result = await _controller.GetRegiones(1);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedRegiones = okResult.Value.Should().BeAssignableTo<List<CatalogItemDto>>().Subject;
        returnedRegiones.Should().HaveCount(2);
    }

    [Fact(DisplayName = "GetRegiones should return BadRequest when zonaId is zero")]
    public async Task GetRegiones_ZonaIdZero_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetRegiones(0);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "GetRegiones should return BadRequest when zonaId is negative")]
    public async Task GetRegiones_ZonaIdNegative_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetRegiones(-1);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "GetRegiones should return 500 when service throws exception")]
    public async Task GetRegiones_ServiceThrows_Returns500()
    {
        // Arrange
        _catalogServiceMock.Setup(x => x.GetRegionesByZonaAsync(1))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetRegiones(1);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetSectores Tests

    [Fact(DisplayName = "GetSectores should return OK with list of sectors")]
    public async Task GetSectores_Success_ReturnsOkWithSectors()
    {
        // Arrange
        var sectores = new List<CatalogItemDto>
        {
            new() { Id = 1, Nombre = "Sector A" },
            new() { Id = 2, Nombre = "Sector B" }
        };

        _catalogServiceMock.Setup(x => x.GetSectoresByRegionAsync(1))
            .ReturnsAsync(sectores);

        // Act
        var result = await _controller.GetSectores(1);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedSectores = okResult.Value.Should().BeAssignableTo<List<CatalogItemDto>>().Subject;
        returnedSectores.Should().HaveCount(2);
    }

    [Fact(DisplayName = "GetSectores should return BadRequest when regionId is zero")]
    public async Task GetSectores_RegionIdZero_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetSectores(0);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "GetSectores should return BadRequest when regionId is negative")]
    public async Task GetSectores_RegionIdNegative_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetSectores(-5);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "GetSectores should return 500 when service throws exception")]
    public async Task GetSectores_ServiceThrows_Returns500()
    {
        // Arrange
        _catalogServiceMock.Setup(x => x.GetSectoresByRegionAsync(1))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetSectores(1);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetCuadrantes Tests

    [Fact(DisplayName = "GetCuadrantes should return OK with list of quadrants")]
    public async Task GetCuadrantes_Success_ReturnsOkWithQuadrants()
    {
        // Arrange
        var cuadrantes = new List<CatalogItemDto>
        {
            new() { Id = 1, Nombre = "Cuadrante 1-A" },
            new() { Id = 2, Nombre = "Cuadrante 1-B" }
        };

        _catalogServiceMock.Setup(x => x.GetCuadrantesBySectorAsync(1))
            .ReturnsAsync(cuadrantes);

        // Act
        var result = await _controller.GetCuadrantes(1);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedCuadrantes = okResult.Value.Should().BeAssignableTo<List<CatalogItemDto>>().Subject;
        returnedCuadrantes.Should().HaveCount(2);
    }

    [Fact(DisplayName = "GetCuadrantes should return BadRequest when sectorId is zero")]
    public async Task GetCuadrantes_SectorIdZero_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetCuadrantes(0);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "GetCuadrantes should return BadRequest when sectorId is negative")]
    public async Task GetCuadrantes_SectorIdNegative_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetCuadrantes(-10);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "GetCuadrantes should return 500 when service throws exception")]
    public async Task GetCuadrantes_ServiceThrows_Returns500()
    {
        // Arrange
        _catalogServiceMock.Setup(x => x.GetCuadrantesBySectorAsync(1))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetCuadrantes(1);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetSuggestions Tests

    [Fact(DisplayName = "GetSuggestions should return OK with suggestions for sexo")]
    public async Task GetSuggestions_Sexo_ReturnsOkWithSuggestions()
    {
        // Arrange
        var suggestions = new List<string> { "Masculino", "Femenino", "No binario" };

        _catalogServiceMock.Setup(x => x.GetSuggestionsAsync("sexo"))
            .ReturnsAsync(suggestions);

        // Act
        var result = await _controller.GetSuggestions("sexo");

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedSuggestions = okResult.Value.Should().BeAssignableTo<List<string>>().Subject;
        returnedSuggestions.Should().HaveCount(3);
        returnedSuggestions.Should().Contain("Masculino");
    }

    [Fact(DisplayName = "GetSuggestions should return OK with suggestions for delito")]
    public async Task GetSuggestions_Delito_ReturnsOkWithSuggestions()
    {
        // Arrange
        var suggestions = new List<string> { "Robo", "Asalto", "Vandalismo" };

        _catalogServiceMock.Setup(x => x.GetSuggestionsAsync("delito"))
            .ReturnsAsync(suggestions);

        // Act
        var result = await _controller.GetSuggestions("delito");

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedSuggestions = okResult.Value.Should().BeAssignableTo<List<string>>().Subject;
        returnedSuggestions.Should().HaveCount(3);
    }

    [Fact(DisplayName = "GetSuggestions should return OK with suggestions for tipo_de_atencion")]
    public async Task GetSuggestions_TipoDeAtencion_ReturnsOkWithSuggestions()
    {
        // Arrange
        var suggestions = new List<string> { "Orientación", "Canalización", "Apoyo directo" };

        _catalogServiceMock.Setup(x => x.GetSuggestionsAsync("tipo_de_atencion"))
            .ReturnsAsync(suggestions);

        // Act
        var result = await _controller.GetSuggestions("tipo_de_atencion");

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedSuggestions = okResult.Value.Should().BeAssignableTo<List<string>>().Subject;
        returnedSuggestions.Should().HaveCount(3);
    }

    [Fact(DisplayName = "GetSuggestions should be case insensitive for campo")]
    public async Task GetSuggestions_CaseInsensitive_ReturnsOk()
    {
        // Arrange
        var suggestions = new List<string> { "Masculino", "Femenino" };

        _catalogServiceMock.Setup(x => x.GetSuggestionsAsync("SEXO"))
            .ReturnsAsync(suggestions);

        // Act
        var result = await _controller.GetSuggestions("SEXO");

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact(DisplayName = "GetSuggestions should return BadRequest when campo is null")]
    public async Task GetSuggestions_CampoNull_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetSuggestions(null!);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "GetSuggestions should return BadRequest when campo is empty")]
    public async Task GetSuggestions_CampoEmpty_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetSuggestions("");

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "GetSuggestions should return BadRequest when campo is whitespace")]
    public async Task GetSuggestions_CampoWhitespace_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetSuggestions("   ");

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "GetSuggestions should return BadRequest for invalid campo")]
    public async Task GetSuggestions_InvalidCampo_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetSuggestions("invalid_field");

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
    }

    [Fact(DisplayName = "GetSuggestions should return BadRequest for unauthorized campo")]
    public async Task GetSuggestions_UnauthorizedCampo_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetSuggestions("password");

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "GetSuggestions should return 500 when service throws exception")]
    public async Task GetSuggestions_ServiceThrows_Returns500()
    {
        // Arrange
        _catalogServiceMock.Setup(x => x.GetSuggestionsAsync("sexo"))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetSuggestions("sexo");

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region ValidateHierarchy Tests

    [Fact(DisplayName = "ValidateHierarchy should return OK with isValid true")]
    public async Task ValidateHierarchy_Valid_ReturnsOkWithTrue()
    {
        // Arrange
        _catalogServiceMock.Setup(x => x.ValidateHierarchyAsync(1, 2, 3, 4))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ValidateHierarchy(1, 2, 3, 4);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact(DisplayName = "ValidateHierarchy should return OK with isValid false")]
    public async Task ValidateHierarchy_Invalid_ReturnsOkWithFalse()
    {
        // Arrange
        _catalogServiceMock.Setup(x => x.ValidateHierarchyAsync(1, 2, 3, 4))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.ValidateHierarchy(1, 2, 3, 4);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact(DisplayName = "ValidateHierarchy should return BadRequest when zonaId is zero")]
    public async Task ValidateHierarchy_ZonaIdZero_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.ValidateHierarchy(0, 2, 3, 4);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "ValidateHierarchy should return BadRequest when regionId is zero")]
    public async Task ValidateHierarchy_RegionIdZero_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.ValidateHierarchy(1, 0, 3, 4);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "ValidateHierarchy should return BadRequest when sectorId is zero")]
    public async Task ValidateHierarchy_SectorIdZero_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.ValidateHierarchy(1, 2, 0, 4);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "ValidateHierarchy should return BadRequest when cuadranteId is zero")]
    public async Task ValidateHierarchy_CuadranteIdZero_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.ValidateHierarchy(1, 2, 3, 0);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "ValidateHierarchy should return BadRequest when any ID is negative")]
    public async Task ValidateHierarchy_NegativeIds_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.ValidateHierarchy(-1, 2, 3, 4);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "ValidateHierarchy should return 500 when service throws exception")]
    public async Task ValidateHierarchy_ServiceThrows_Returns500()
    {
        // Arrange
        _catalogServiceMock.Setup(x => x.ValidateHierarchyAsync(1, 2, 3, 4))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.ValidateHierarchy(1, 2, 3, 4);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region Constructor Tests

    [Fact(DisplayName = "Constructor should create instance with valid dependencies")]
    public void Constructor_ValidDependencies_CreatesInstance()
    {
        // Arrange & Act
        var controller = new CatalogsController(
            _catalogServiceMock.Object,
            _loggerMock.Object);

        // Assert
        controller.Should().NotBeNull();
    }

    #endregion
}
