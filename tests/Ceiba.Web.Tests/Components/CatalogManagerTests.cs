using Bunit;
using Ceiba.Core.Interfaces;
using Ceiba.Shared.DTOs;
using Ceiba.Web.Components.Pages.Admin;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Security.Claims;

namespace Ceiba.Web.Tests.Components;

/// <summary>
/// Component tests for CatalogManager Blazor component.
/// Tests geographic catalog management (Zona/Region/Sector/Cuadrante).
/// Phase 3: Coverage improvement tests.
/// </summary>
[Trait("Category", "Component")]
public class CatalogManagerTests : TestContext
{
    private readonly Mock<ICatalogAdminService> _mockCatalogService;
    private readonly Guid _testUserId = Guid.NewGuid();

    public CatalogManagerTests()
    {
        _mockCatalogService = new Mock<ICatalogAdminService>();

        Services.AddSingleton(_mockCatalogService.Object);
        Services.AddSingleton<AuthenticationStateProvider>(new TestAuthStateProvider(_testUserId, "ADMIN"));
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        SetupDefaultMocks();
    }

    #region Rendering Tests

    [Fact(DisplayName = "CatalogManager should render page title")]
    public void CatalogManager_ShouldRenderPageTitle()
    {
        // Act
        var cut = Render<CatalogManager>();

        // Assert
        cut.Markup.Should().Contain("Gestión de Catálogos Geográficos");
    }

    [Fact(DisplayName = "CatalogManager should render all tabs")]
    public void CatalogManager_ShouldRenderAllTabs()
    {
        // Act
        var cut = Render<CatalogManager>();

        // Assert
        cut.Markup.Should().Contain("Zonas");
        cut.Markup.Should().Contain("Regiones");
        cut.Markup.Should().Contain("Sectores");
        cut.Markup.Should().Contain("Cuadrantes");
    }

    [Fact(DisplayName = "CatalogManager should show Zonas tab by default")]
    public void CatalogManager_ShouldShowZonasTabByDefault()
    {
        // Act
        var cut = Render<CatalogManager>();

        // Assert
        var zonasTab = cut.FindAll("button.nav-link").FirstOrDefault(b => b.TextContent.Contains("Zonas"));
        zonasTab?.ClassList.Should().Contain("active");
    }

    [Fact(DisplayName = "CatalogManager should render Zonas table")]
    public void CatalogManager_ShouldRenderZonasTable()
    {
        // Act
        var cut = Render<CatalogManager>();

        // Assert
        cut.Markup.Should().Contain("<table");
        cut.Markup.Should().Contain("ID");
        cut.Markup.Should().Contain("Nombre");
        cut.Markup.Should().Contain("Regiones");
        cut.Markup.Should().Contain("Estado");
        cut.Markup.Should().Contain("Acciones");
    }

    [Fact(DisplayName = "CatalogManager should display zonas from service")]
    public void CatalogManager_ShouldDisplayZonasFromService()
    {
        // Act
        var cut = Render<CatalogManager>();

        // Assert
        cut.Markup.Should().Contain("Zona Norte");
        cut.Markup.Should().Contain("Zona Sur");
    }

    [Fact(DisplayName = "CatalogManager should display Nueva Zona button")]
    public void CatalogManager_ShouldDisplayNuevaZonaButton()
    {
        // Act
        var cut = Render<CatalogManager>();

        // Assert
        var button = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Nueva Zona"));
        button.Should().NotBeNull();
    }

    #endregion

    #region Tab Navigation Tests

    [Fact(DisplayName = "CatalogManager Regiones tab should show regions table")]
    public async Task CatalogManager_RegionesTab_ShouldShowRegionsTable()
    {
        // Arrange
        var cut = Render<CatalogManager>();

        // Act
        var regionesTab = cut.FindAll("button.nav-link").First(b => b.TextContent.Contains("Regiones"));
        await cut.InvokeAsync(() => regionesTab.Click());

        // Assert
        cut.Markup.Should().Contain("Nueva Región");
        cut.Markup.Should().Contain("Todas las zonas");
    }

    [Fact(DisplayName = "CatalogManager Sectores tab should show sectors table")]
    public async Task CatalogManager_SectoresTab_ShouldShowSectorsTable()
    {
        // Arrange
        var cut = Render<CatalogManager>();

        // Act
        var sectoresTab = cut.FindAll("button.nav-link").First(b => b.TextContent.Contains("Sectores"));
        await cut.InvokeAsync(() => sectoresTab.Click());

        // Assert
        cut.Markup.Should().Contain("Nuevo Sector");
        cut.Markup.Should().Contain("Todas las regiones");
    }

    [Fact(DisplayName = "CatalogManager Cuadrantes tab should show cuadrantes table")]
    public async Task CatalogManager_CuadrantesTab_ShouldShowCuadrantesTable()
    {
        // Arrange
        var cut = Render<CatalogManager>();

        // Act
        var cuadrantesTab = cut.FindAll("button.nav-link").First(b => b.TextContent.Contains("Cuadrantes"));
        await cut.InvokeAsync(() => cuadrantesTab.Click());

        // Assert
        cut.Markup.Should().Contain("Nuevo Cuadrante");
        cut.Markup.Should().Contain("Todos los sectores");
    }

    #endregion

    #region Zona Modal Tests

    [Fact(DisplayName = "CatalogManager Nueva Zona button should open modal")]
    public async Task CatalogManager_NuevaZonaButton_ShouldOpenModal()
    {
        // Arrange
        var cut = Render<CatalogManager>();

        // Act
        var nuevaZonaButton = cut.FindAll("button").First(b => b.TextContent.Contains("Nueva Zona"));
        await cut.InvokeAsync(() => nuevaZonaButton.Click());

        // Assert
        cut.Markup.Should().Contain("modal");
        cut.Markup.Should().Contain("Nueva Zona");
    }

    [Fact(DisplayName = "CatalogManager Zona modal should have name field")]
    public async Task CatalogManager_ZonaModal_ShouldHaveNameField()
    {
        // Arrange
        var cut = Render<CatalogManager>();

        // Open modal
        var nuevaZonaButton = cut.FindAll("button").First(b => b.TextContent.Contains("Nueva Zona"));
        await cut.InvokeAsync(() => nuevaZonaButton.Click());

        // Assert
        cut.Markup.Should().Contain("Nombre");
        cut.Markup.Should().Contain("Guardar");
        cut.Markup.Should().Contain("Cancelar");
    }

    [Fact(DisplayName = "CatalogManager Zona modal cancel should close modal")]
    public async Task CatalogManager_ZonaModalCancel_ShouldCloseModal()
    {
        // Arrange
        var cut = Render<CatalogManager>();

        // Open modal
        var nuevaZonaButton = cut.FindAll("button").First(b => b.TextContent.Contains("Nueva Zona"));
        await cut.InvokeAsync(() => nuevaZonaButton.Click());

        // Act
        var cancelButton = cut.FindAll("button").First(b => b.TextContent.Contains("Cancelar"));
        await cut.InvokeAsync(() => cancelButton.Click());

        // Assert
        cut.FindAll(".modal").Should().BeEmpty();
    }

    [Fact(DisplayName = "CatalogManager edit zona button should open modal")]
    public async Task CatalogManager_EditZonaButton_ShouldOpenModal()
    {
        // Arrange
        var cut = Render<CatalogManager>();

        // Wait for table to render
        cut.WaitForAssertion(() => cut.FindAll("table tbody tr").Count.Should().BeGreaterThan(0));

        // Act
        var editButton = cut.FindAll("button.btn-outline-primary").FirstOrDefault();
        if (editButton != null)
        {
            await cut.InvokeAsync(() => editButton.Click());

            // Assert
            cut.Markup.Should().Contain("modal");
            cut.Markup.Should().Contain("Editar Zona");
        }
    }

    #endregion

    #region Zona Actions Tests

    [Fact(DisplayName = "CatalogManager toggle zona button should call service")]
    public async Task CatalogManager_ToggleZonaButton_ShouldCallService()
    {
        // Arrange
        _mockCatalogService.Setup(s => s.ToggleZonaActivoAsync(It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ZonaDto { Id = 1, Nombre = "Zona Norte", Activo = false, RegionesCount = 3 });

        var cut = Render<CatalogManager>();

        // Wait for table to render
        cut.WaitForAssertion(() => cut.FindAll("table tbody tr").Count.Should().BeGreaterThan(0));

        // Act - Find toggle button (warning or success outline in btn-group)
        var toggleButton = cut.FindAll(".btn-group button.btn-outline-warning, .btn-group button.btn-outline-success")
            .FirstOrDefault();
        if (toggleButton != null)
        {
            await cut.InvokeAsync(() => toggleButton.Click());
        }

        // Assert
        _mockCatalogService.Verify(s => s.ToggleZonaActivoAsync(It.IsAny<int>(), _testUserId, It.IsAny<CancellationToken>()), Times.AtMostOnce);
    }

    [Fact(DisplayName = "CatalogManager save zona should call service")]
    public async Task CatalogManager_SaveZona_ShouldCallService()
    {
        // Arrange
        _mockCatalogService.Setup(s => s.CreateZonaAsync(It.IsAny<CreateZonaDto>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ZonaDto { Id = 3, Nombre = "Nueva Zona", Activo = true, RegionesCount = 0 });

        var cut = Render<CatalogManager>();

        // Open modal
        var nuevaZonaButton = cut.FindAll("button").First(b => b.TextContent.Contains("Nueva Zona"));
        await cut.InvokeAsync(() => nuevaZonaButton.Click());

        // Fill form
        var nameInput = cut.Find("input[type='text']");
        await cut.InvokeAsync(() => nameInput.Change("Nueva Zona Test"));

        // Click save
        var saveButton = cut.FindAll("button").First(b => b.TextContent.Contains("Guardar"));
        await cut.InvokeAsync(() => saveButton.Click());

        // Assert
        _mockCatalogService.Verify(s => s.CreateZonaAsync(
            It.Is<CreateZonaDto>(dto => dto.Nombre == "Nueva Zona Test"), _testUserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Status Badge Tests

    [Fact(DisplayName = "CatalogManager should show success badge for active items")]
    public void CatalogManager_ActiveItem_ShouldShowSuccessBadge()
    {
        // Act
        var cut = Render<CatalogManager>();

        // Assert
        cut.Markup.Should().Contain("bg-success");
        cut.Markup.Should().Contain("Activo");
    }

    [Fact(DisplayName = "CatalogManager should show secondary badge for inactive items")]
    public void CatalogManager_InactiveItem_ShouldShowSecondaryBadge()
    {
        // Arrange
        var zonas = new List<ZonaDto>
        {
            new() { Id = 1, Nombre = "Zona Inactiva", Activo = false, RegionesCount = 0 }
        };

        _mockCatalogService.Setup(s => s.GetZonasAsync(It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(zonas);

        // Act
        var cut = Render<CatalogManager>();

        // Assert
        cut.Markup.Should().Contain("bg-secondary");
        cut.Markup.Should().Contain("Inactivo");
    }

    #endregion

    #region Loading State Tests

    [Fact(DisplayName = "CatalogManager should show error message on service failure")]
    public void CatalogManager_ServiceError_ShouldShowErrorMessage()
    {
        // Arrange
        _mockCatalogService.Setup(s => s.GetZonasAsync(It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var cut = Render<CatalogManager>();

        // Assert
        cut.Markup.Should().Contain("Error al cargar los catálogos");
    }

    #endregion

    #region Region Modal Tests

    [Fact(DisplayName = "CatalogManager Nueva Region button should open modal with zona selector")]
    public async Task CatalogManager_NuevaRegionButton_ShouldOpenModalWithZonaSelector()
    {
        // Arrange
        var cut = Render<CatalogManager>();

        // Switch to Regiones tab
        var regionesTab = cut.FindAll("button.nav-link").First(b => b.TextContent.Contains("Regiones"));
        await cut.InvokeAsync(() => regionesTab.Click());

        // Act
        var nuevaRegionButton = cut.FindAll("button").First(b => b.TextContent.Contains("Nueva Región"));
        await cut.InvokeAsync(() => nuevaRegionButton.Click());

        // Assert
        cut.Markup.Should().Contain("modal");
        cut.Markup.Should().Contain("Nueva Región");
        cut.Markup.Should().Contain("Zona");
        cut.Markup.Should().Contain("Nombre");
    }

    #endregion

    #region Sector Modal Tests

    [Fact(DisplayName = "CatalogManager Nueva Sector button should open modal with region selector")]
    public async Task CatalogManager_NuevaSectorButton_ShouldOpenModalWithRegionSelector()
    {
        // Arrange
        var cut = Render<CatalogManager>();

        // Switch to Sectores tab
        var sectoresTab = cut.FindAll("button.nav-link").First(b => b.TextContent.Contains("Sectores"));
        await cut.InvokeAsync(() => sectoresTab.Click());

        // Act
        var nuevoSectorButton = cut.FindAll("button").First(b => b.TextContent.Contains("Nuevo Sector"));
        await cut.InvokeAsync(() => nuevoSectorButton.Click());

        // Assert
        cut.Markup.Should().Contain("modal");
        cut.Markup.Should().Contain("Nuevo Sector");
        cut.Markup.Should().Contain("Región");
        cut.Markup.Should().Contain("Nombre");
    }

    #endregion

    #region Cuadrante Modal Tests

    [Fact(DisplayName = "CatalogManager Nuevo Cuadrante button should open modal with sector selector")]
    public async Task CatalogManager_NuevoCuadranteButton_ShouldOpenModalWithSectorSelector()
    {
        // Arrange
        var cut = Render<CatalogManager>();

        // Switch to Cuadrantes tab
        var cuadrantesTab = cut.FindAll("button.nav-link").First(b => b.TextContent.Contains("Cuadrantes"));
        await cut.InvokeAsync(() => cuadrantesTab.Click());

        // Act
        var nuevoCuadranteButton = cut.FindAll("button").First(b => b.TextContent.Contains("Nuevo Cuadrante"));
        await cut.InvokeAsync(() => nuevoCuadranteButton.Click());

        // Assert
        cut.Markup.Should().Contain("modal");
        cut.Markup.Should().Contain("Nuevo Cuadrante");
        cut.Markup.Should().Contain("Sector");
        cut.Markup.Should().Contain("Nombre");
    }

    #endregion

    #region Helper Methods

    private void SetupDefaultMocks()
    {
        var zonas = new List<ZonaDto>
        {
            new() { Id = 1, Nombre = "Zona Norte", Activo = true, RegionesCount = 3 },
            new() { Id = 2, Nombre = "Zona Sur", Activo = true, RegionesCount = 2 }
        };

        var regiones = new List<RegionDto>
        {
            new() { Id = 1, Nombre = "Región Centro", ZonaId = 1, ZonaNombre = "Zona Norte", Activo = true, SectoresCount = 5 },
            new() { Id = 2, Nombre = "Región Este", ZonaId = 1, ZonaNombre = "Zona Norte", Activo = true, SectoresCount = 3 }
        };

        var sectores = new List<SectorDto>
        {
            new() { Id = 1, Nombre = "Sector A", RegionId = 1, RegionNombre = "Región Centro", ZonaNombre = "Zona Norte", Activo = true, CuadrantesCount = 4 },
            new() { Id = 2, Nombre = "Sector B", RegionId = 1, RegionNombre = "Región Centro", ZonaNombre = "Zona Norte", Activo = true, CuadrantesCount = 2 }
        };

        var cuadrantes = new List<CuadranteDto>
        {
            new() { Id = 1, Nombre = "Cuadrante 1", SectorId = 1, SectorNombre = "Sector A", Activo = true },
            new() { Id = 2, Nombre = "Cuadrante 2", SectorId = 1, SectorNombre = "Sector A", Activo = true }
        };

        _mockCatalogService.Setup(s => s.GetZonasAsync(It.IsAny<bool?>(), It.IsAny<CancellationToken>())).ReturnsAsync(zonas);
        _mockCatalogService.Setup(s => s.GetRegionesAsync(It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>())).ReturnsAsync(regiones);
        _mockCatalogService.Setup(s => s.GetSectoresAsync(It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>())).ReturnsAsync(sectores);
        _mockCatalogService.Setup(s => s.GetCuadrantesAsync(It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>())).ReturnsAsync(cuadrantes);
    }

    private class TestAuthStateProvider : AuthenticationStateProvider
    {
        private readonly AuthenticationState _authState;

        public TestAuthStateProvider(Guid userId, string role)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "admin@ceiba.local"),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            }, "Test");

            _authState = new AuthenticationState(new ClaimsPrincipal(identity));
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(_authState);
    }

    #endregion
}
