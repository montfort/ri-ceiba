using System.Security.Claims;
using Ceiba.Core.Interfaces;
using Ceiba.Infrastructure.Data;
using Ceiba.Infrastructure.Data.Seeding;
using Ceiba.Shared.DTOs;
using Ceiba.Web.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Ceiba.Web.Tests.Controllers;

/// <summary>
/// Unit tests for AdminController.
/// Tests user management and catalog administration operations.
/// </summary>
public class AdminControllerTests
{
    private readonly Mock<IUserManagementService> _userServiceMock;
    private readonly Mock<ICatalogAdminService> _catalogServiceMock;
    private readonly Mock<ISeedOrchestrator> _seedOrchestratorMock;
    private readonly Mock<IRegionDataLoader> _regionDataLoaderMock;
    private readonly Mock<ILogger<AdminController>> _loggerMock;
    private readonly AdminController _controller;
    private readonly Guid _adminUserId = Guid.NewGuid();

    public AdminControllerTests()
    {
        _userServiceMock = new Mock<IUserManagementService>();
        _catalogServiceMock = new Mock<ICatalogAdminService>();
        _seedOrchestratorMock = new Mock<ISeedOrchestrator>();
        _regionDataLoaderMock = new Mock<IRegionDataLoader>();
        _loggerMock = new Mock<ILogger<AdminController>>();

        _controller = new AdminController(
            _userServiceMock.Object,
            _catalogServiceMock.Object,
            _seedOrchestratorMock.Object,
            _regionDataLoaderMock.Object,
            _loggerMock.Object);

        SetupAuthenticatedAdmin();
    }

    private void SetupAuthenticatedAdmin()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, _adminUserId.ToString()),
            new(ClaimTypes.Role, "ADMIN")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    #region User Management Tests

    [Fact]
    public async Task ListUsers_Success_ReturnsOkWithUsers()
    {
        // Arrange
        var filter = new UserFilterDto { Page = 1, PageSize = 10 };
        var response = new UserListResponse
        {
            Items = new List<UserDto>
            {
                new() { Id = Guid.NewGuid(), Email = "test@test.com" }
            },
            TotalCount = 1
        };

        _userServiceMock.Setup(x => x.ListUsersAsync(filter))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.ListUsers(filter);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedResponse = Assert.IsType<UserListResponse>(okResult.Value);
        Assert.Equal(1, returnedResponse.TotalCount);
    }

    [Fact]
    public async Task ListUsers_Exception_Returns500()
    {
        // Arrange
        var filter = new UserFilterDto();
        _userServiceMock.Setup(x => x.ListUsersAsync(filter))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.ListUsers(filter);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task GetUser_ExistingUser_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserDto { Id = userId, Email = "test@test.com" };
        _userServiceMock.Setup(x => x.GetUserByIdAsync(userId))
            .ReturnsAsync(user);

        // Act
        var result = await _controller.GetUser(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedUser = Assert.IsType<UserDto>(okResult.Value);
        Assert.Equal(userId, returnedUser.Id);
    }

    [Fact]
    public async Task GetUser_NotFound_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userServiceMock.Setup(x => x.GetUserByIdAsync(userId))
            .ReturnsAsync((UserDto?)null);

        // Act
        var result = await _controller.GetUser(userId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetUser_Exception_Returns500()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userServiceMock.Setup(x => x.GetUserByIdAsync(userId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetUser(userId);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task CreateUser_ValidData_ReturnsCreated()
    {
        // Arrange
        var createDto = new CreateUserDto
        {
            Email = "new@test.com",
            Password = "Password123!",
            Roles = new List<string> { "CREADOR" }
        };
        var createdUser = new UserDto { Id = Guid.NewGuid(), Email = "new@test.com" };

        _userServiceMock.Setup(x => x.CreateUserAsync(createDto, _adminUserId))
            .ReturnsAsync(createdUser);

        // Act
        var result = await _controller.CreateUser(createDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(201, createdResult.StatusCode);
    }

    [Fact]
    public async Task CreateUser_InvalidOperation_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateUserDto { Email = "existing@test.com" };
        _userServiceMock.Setup(x => x.CreateUserAsync(createDto, _adminUserId))
            .ThrowsAsync(new InvalidOperationException("Email already exists"));

        // Act
        var result = await _controller.CreateUser(createDto);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task CreateUser_Exception_Returns500()
    {
        // Arrange
        var createDto = new CreateUserDto();
        _userServiceMock.Setup(x => x.CreateUserAsync(createDto, _adminUserId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.CreateUser(createDto);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_ValidData_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateDto = new UpdateUserDto { Nombre = "Updated Name" };
        var updatedUser = new UserDto { Id = userId, Nombre = "Updated Name" };

        _userServiceMock.Setup(x => x.UpdateUserAsync(userId, updateDto, _adminUserId))
            .ReturnsAsync(updatedUser);

        // Act
        var result = await _controller.UpdateUser(userId, updateDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedUser = Assert.IsType<UserDto>(okResult.Value);
        Assert.Equal("Updated Name", returnedUser.Nombre);
    }

    [Fact]
    public async Task UpdateUser_NotFound_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateDto = new UpdateUserDto();
        _userServiceMock.Setup(x => x.UpdateUserAsync(userId, updateDto, _adminUserId))
            .ThrowsAsync(new KeyNotFoundException("User not found"));

        // Act
        var result = await _controller.UpdateUser(userId, updateDto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateUser_InvalidOperation_ReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateDto = new UpdateUserDto();
        _userServiceMock.Setup(x => x.UpdateUserAsync(userId, updateDto, _adminUserId))
            .ThrowsAsync(new InvalidOperationException("Cannot update"));

        // Act
        var result = await _controller.UpdateUser(userId, updateDto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task SuspendUser_Success_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var suspendedUser = new UserDto { Id = userId, Activo = false };

        _userServiceMock.Setup(x => x.SuspendUserAsync(userId, _adminUserId))
            .ReturnsAsync(suspendedUser);

        // Act
        var result = await _controller.SuspendUser(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedUser = Assert.IsType<UserDto>(okResult.Value);
        Assert.False(returnedUser.Activo);
    }

    [Fact]
    public async Task SuspendUser_NotFound_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userServiceMock.Setup(x => x.SuspendUserAsync(userId, _adminUserId))
            .ThrowsAsync(new KeyNotFoundException("User not found"));

        // Act
        var result = await _controller.SuspendUser(userId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task ActivateUser_Success_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var activatedUser = new UserDto { Id = userId, Activo = true };

        _userServiceMock.Setup(x => x.ActivateUserAsync(userId, _adminUserId))
            .ReturnsAsync(activatedUser);

        // Act
        var result = await _controller.ActivateUser(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedUser = Assert.IsType<UserDto>(okResult.Value);
        Assert.True(returnedUser.Activo);
    }

    [Fact]
    public async Task DeleteUser_Success_ReturnsNoContent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userServiceMock.Setup(x => x.DeleteUserAsync(userId, _adminUserId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteUser(userId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteUser_NotFound_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userServiceMock.Setup(x => x.DeleteUserAsync(userId, _adminUserId))
            .ThrowsAsync(new KeyNotFoundException("User not found"));

        // Act
        var result = await _controller.DeleteUser(userId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetRoles_ReturnsRolesList()
    {
        // Arrange
        var roles = new List<string> { "CREADOR", "REVISOR", "ADMIN" };
        _userServiceMock.Setup(x => x.GetAvailableRolesAsync())
            .ReturnsAsync(roles);

        // Act
        var result = await _controller.GetRoles();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedRoles = Assert.IsType<List<string>>(okResult.Value);
        Assert.Equal(3, returnedRoles.Count);
    }

    #endregion

    #region Zona Catalog Tests

    [Fact]
    public async Task GetZonas_ReturnsZonasList()
    {
        // Arrange
        var zonas = new List<ZonaDto>
        {
            new() { Id = 1, Nombre = "Zona 1" },
            new() { Id = 2, Nombre = "Zona 2" }
        };
        _catalogServiceMock.Setup(x => x.GetZonasAsync(null))
            .ReturnsAsync(zonas);

        // Act
        var result = await _controller.GetZonas();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedZonas = Assert.IsType<List<ZonaDto>>(okResult.Value);
        Assert.Equal(2, returnedZonas.Count);
    }

    [Fact]
    public async Task GetZona_ExistingZona_ReturnsOk()
    {
        // Arrange
        var zona = new ZonaDto { Id = 1, Nombre = "Test Zona" };
        _catalogServiceMock.Setup(x => x.GetZonaByIdAsync(1))
            .ReturnsAsync(zona);

        // Act
        var result = await _controller.GetZona(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedZona = Assert.IsType<ZonaDto>(okResult.Value);
        Assert.Equal("Test Zona", returnedZona.Nombre);
    }

    [Fact]
    public async Task GetZona_NotFound_ReturnsNotFound()
    {
        // Arrange
        _catalogServiceMock.Setup(x => x.GetZonaByIdAsync(999))
            .ReturnsAsync((ZonaDto?)null);

        // Act
        var result = await _controller.GetZona(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateZona_ValidData_ReturnsCreated()
    {
        // Arrange
        var createDto = new CreateZonaDto { Nombre = "New Zona" };
        var createdZona = new ZonaDto { Id = 1, Nombre = "New Zona" };

        _catalogServiceMock.Setup(x => x.CreateZonaAsync(createDto, _adminUserId))
            .ReturnsAsync(createdZona);

        // Act
        var result = await _controller.CreateZona(createDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(201, createdResult.StatusCode);
    }

    [Fact]
    public async Task UpdateZona_Success_ReturnsOk()
    {
        // Arrange
        var updateDto = new CreateZonaDto { Nombre = "Updated Zona" };
        var updatedZona = new ZonaDto { Id = 1, Nombre = "Updated Zona" };

        _catalogServiceMock.Setup(x => x.UpdateZonaAsync(1, updateDto, _adminUserId))
            .ReturnsAsync(updatedZona);

        // Act
        var result = await _controller.UpdateZona(1, updateDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedZona = Assert.IsType<ZonaDto>(okResult.Value);
        Assert.Equal("Updated Zona", returnedZona.Nombre);
    }

    [Fact]
    public async Task UpdateZona_NotFound_ReturnsNotFound()
    {
        // Arrange
        var updateDto = new CreateZonaDto { Nombre = "Updated" };
        _catalogServiceMock.Setup(x => x.UpdateZonaAsync(999, updateDto, _adminUserId))
            .ThrowsAsync(new KeyNotFoundException("Zona not found"));

        // Act
        var result = await _controller.UpdateZona(999, updateDto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task ToggleZona_Success_ReturnsOk()
    {
        // Arrange
        var zona = new ZonaDto { Id = 1, Activo = false };
        _catalogServiceMock.Setup(x => x.ToggleZonaActivoAsync(1, _adminUserId))
            .ReturnsAsync(zona);

        // Act
        var result = await _controller.ToggleZona(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region Sector Catalog Tests

    [Fact]
    public async Task GetSectores_ReturnsSectoresList()
    {
        // Arrange
        var sectores = new List<SectorDto>
        {
            new() { Id = 1, Nombre = "Sector 1" }
        };
        _catalogServiceMock.Setup(x => x.GetSectoresAsync(null, null))
            .ReturnsAsync(sectores);

        // Act
        var result = await _controller.GetSectores();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedSectores = Assert.IsType<List<SectorDto>>(okResult.Value);
        Assert.Single(returnedSectores);
    }

    [Fact]
    public async Task GetSector_ExistingSector_ReturnsOk()
    {
        // Arrange
        var sector = new SectorDto { Id = 1, Nombre = "Test Sector" };
        _catalogServiceMock.Setup(x => x.GetSectorByIdAsync(1))
            .ReturnsAsync(sector);

        // Act
        var result = await _controller.GetSector(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task CreateSector_ValidData_ReturnsCreated()
    {
        // Arrange
        var createDto = new CreateSectorDto { Nombre = "New Sector", RegionId = 1 };
        var createdSector = new SectorDto { Id = 1, Nombre = "New Sector" };

        _catalogServiceMock.Setup(x => x.CreateSectorAsync(createDto, _adminUserId))
            .ReturnsAsync(createdSector);

        // Act
        var result = await _controller.CreateSector(createDto);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result.Result);
    }

    [Fact]
    public async Task CreateSector_RegionNotFound_ReturnsNotFound()
    {
        // Arrange
        var createDto = new CreateSectorDto { Nombre = "New Sector", RegionId = 999 };
        _catalogServiceMock.Setup(x => x.CreateSectorAsync(createDto, _adminUserId))
            .ThrowsAsync(new KeyNotFoundException("Region not found"));

        // Act
        var result = await _controller.CreateSector(createDto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    #endregion

    #region Cuadrante Catalog Tests

    [Fact]
    public async Task GetCuadrantes_ReturnsCuadrantesList()
    {
        // Arrange
        var cuadrantes = new List<CuadranteDto>
        {
            new() { Id = 1, Nombre = "Cuadrante 1" }
        };
        _catalogServiceMock.Setup(x => x.GetCuadrantesAsync(null, null))
            .ReturnsAsync(cuadrantes);

        // Act
        var result = await _controller.GetCuadrantes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task CreateCuadrante_ValidData_ReturnsCreated()
    {
        // Arrange
        var createDto = new CreateCuadranteDto { Nombre = "New Cuadrante", SectorId = 1 };
        var createdCuadrante = new CuadranteDto { Id = 1, Nombre = "New Cuadrante" };

        _catalogServiceMock.Setup(x => x.CreateCuadranteAsync(createDto, _adminUserId))
            .ReturnsAsync(createdCuadrante);

        // Act
        var result = await _controller.CreateCuadrante(createDto);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result.Result);
    }

    #endregion

    #region Sugerencias Tests

    [Fact]
    public async Task GetSugerencias_ReturnsList()
    {
        // Arrange
        var sugerencias = new List<SugerenciaDto>
        {
            new() { Id = 1, Campo = "Delito", Valor = "Robo" }
        };
        _catalogServiceMock.Setup(x => x.GetSugerenciasAsync(null, null))
            .ReturnsAsync(sugerencias);

        // Act
        var result = await _controller.GetSugerencias();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public void GetSugerenciaCampos_ReturnsCamposList()
    {
        // Act
        var result = _controller.GetSugerenciaCampos();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task CreateSugerencia_ValidData_ReturnsCreated()
    {
        // Arrange
        var createDto = new CreateSugerenciaDto { Campo = "Delito", Valor = "Robo" };
        var created = new SugerenciaDto { Id = 1, Campo = "Delito", Valor = "Robo" };

        _catalogServiceMock.Setup(x => x.CreateSugerenciaAsync(createDto, _adminUserId))
            .ReturnsAsync(created);

        // Act
        var result = await _controller.CreateSugerencia(createDto);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result.Result);
    }

    [Fact]
    public async Task CreateSugerencia_InvalidCampo_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateSugerenciaDto { Campo = "InvalidCampo", Valor = "Test" };
        _catalogServiceMock.Setup(x => x.CreateSugerenciaAsync(createDto, _adminUserId))
            .ThrowsAsync(new ArgumentException("Invalid campo"));

        // Act
        var result = await _controller.CreateSugerencia(createDto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task DeleteSugerencia_Success_ReturnsNoContent()
    {
        // Arrange
        _catalogServiceMock.Setup(x => x.DeleteSugerenciaAsync(1, _adminUserId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteSugerencia(1);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteSugerencia_NotFound_ReturnsNotFound()
    {
        // Arrange
        _catalogServiceMock.Setup(x => x.DeleteSugerenciaAsync(999, _adminUserId))
            .ThrowsAsync(new KeyNotFoundException("Not found"));

        // Act
        var result = await _controller.DeleteSugerencia(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion

    #region Geographic Catalog Stats and Reload Tests

    [Fact]
    public async Task GetGeographicCatalogStats_Success_ReturnsOkWithStats()
    {
        // Arrange
        var mockStats = new RegionDataLoader.SeedingStats
        {
            Zonas = 5,
            Regiones = 16,
            Sectores = 72,
            Cuadrantes = 1015
        };

        _regionDataLoaderMock.Setup(x => x.GetCurrentStatsAsync())
            .ReturnsAsync(mockStats);

        // Act
        var result = await _controller.GetGeographicCatalogStats();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var stats = Assert.IsType<GeographicCatalogStatsDto>(okResult.Value);
        Assert.Equal(5, stats.ZonasCount);
        Assert.Equal(16, stats.RegionesCount);
        Assert.Equal(72, stats.SectoresCount);
        Assert.Equal(1015, stats.CuadrantesCount);
    }

    [Fact]
    public async Task GetGeographicCatalogStats_Exception_Returns500()
    {
        // Arrange
        _regionDataLoaderMock.Setup(x => x.GetCurrentStatsAsync())
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetGeographicCatalogStats();

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task ReloadGeographicCatalogs_Success_ReturnsOkWithStats()
    {
        // Arrange
        _seedOrchestratorMock.Setup(x => x.ReloadGeographicCatalogsAsync())
            .Returns(Task.CompletedTask);

        var mockStats = new RegionDataLoader.SeedingStats
        {
            Zonas = 5,
            Regiones = 16,
            Sectores = 72,
            Cuadrantes = 1015
        };

        _regionDataLoaderMock.Setup(x => x.GetCurrentStatsAsync())
            .ReturnsAsync(mockStats);

        // Act
        var result = await _controller.ReloadGeographicCatalogs();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var stats = Assert.IsType<GeographicCatalogStatsDto>(okResult.Value);
        Assert.Contains("recargados exitosamente", stats.Message);
        Assert.Equal(5, stats.ZonasCount);
    }

    [Fact]
    public async Task ReloadGeographicCatalogs_FileNotFound_Returns500()
    {
        // Arrange
        _seedOrchestratorMock.Setup(x => x.ReloadGeographicCatalogsAsync())
            .ThrowsAsync(new FileNotFoundException("regiones.json not found"));

        // Act
        var result = await _controller.ReloadGeographicCatalogs();

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task ReloadGeographicCatalogs_Exception_Returns500()
    {
        // Arrange
        _seedOrchestratorMock.Setup(x => x.ReloadGeographicCatalogsAsync())
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.ReloadGeographicCatalogs();

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    #endregion
}
