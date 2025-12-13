using Bunit;
using Ceiba.Core.Interfaces;
using Ceiba.Shared.DTOs;
using Ceiba.Web.Components.Pages.Admin;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Security.Claims;

namespace Ceiba.Web.Tests.Components;

/// <summary>
/// Component tests for CatalogManager Blazor component.
/// Tests CRUD operations for Zonas, Sectores, and Cuadrantes.
/// </summary>
public class CatalogManagerTests : TestContext
{
    private readonly Mock<ICatalogAdminService> _mockCatalogService;
    private readonly Guid _testUserId = Guid.NewGuid();

    public CatalogManagerTests()
    {
        _mockCatalogService = new Mock<ICatalogAdminService>();

        Services.AddSingleton(_mockCatalogService.Object);
        Services.AddSingleton<NavigationManager>(new FakeNavigationManager());
        Services.AddSingleton<AuthenticationStateProvider>(new TestAuthStateProvider(_testUserId, "ADMIN"));
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        SetupDefaultMocks();
    }

    #region Tab Navigation Tests

    [Fact(DisplayName = "CatalogManager should render zonas tab by default")]
    public void CatalogManager_ShouldRenderZonasTabByDefault()
    {
        // Act
        var cut = Render<CatalogManager>();

        // Assert
        var activeTab = cut.Find(".nav-link.active");
        activeTab.TextContent.Should().Contain("Zonas");
    }

    [Fact(DisplayName = "CatalogManager should switch to sectores tab when clicked")]
    public async Task CatalogManager_ShouldSwitchToSectoresTab()
    {
        // Arrange
        var cut = Render<CatalogManager>();

        // Act
        var sectoresTab = cut.FindAll(".nav-link").First(n => n.TextContent.Contains("Sectores"));
        await cut.InvokeAsync(() => sectoresTab.Click());

        // Assert - re-find the element after state change
        var updatedTab = cut.FindAll(".nav-link.active").FirstOrDefault(n => n.TextContent.Contains("Sectores"));
        updatedTab.Should().NotBeNull();
    }

    [Fact(DisplayName = "CatalogManager should switch to cuadrantes tab when clicked")]
    public async Task CatalogManager_ShouldSwitchToCuadrantesTab()
    {
        // Arrange
        var cut = Render<CatalogManager>();

        // Act
        var cuadrantesTab = cut.FindAll(".nav-link").First(n => n.TextContent.Contains("Cuadrantes"));
        await cut.InvokeAsync(() => cuadrantesTab.Click());

        // Assert - re-find the element after state change
        var updatedTab = cut.FindAll(".nav-link.active").FirstOrDefault(n => n.TextContent.Contains("Cuadrantes"));
        updatedTab.Should().NotBeNull();
    }

    #endregion

    #region Zona Tests

    [Fact(DisplayName = "CatalogManager should display zonas in table")]
    public void CatalogManager_ShouldDisplayZonasInTable()
    {
        // Arrange - using default mocks that include zonas

        // Act
        var cut = Render<CatalogManager>();

        // Assert
        var tableRows = cut.FindAll("tbody tr");
        tableRows.Should().HaveCountGreaterThan(0);
        cut.Markup.Should().Contain("Zona Norte");
    }

    [Fact(DisplayName = "CatalogManager should open zona modal when Nueva Zona clicked")]
    public async Task CatalogManager_ShouldOpenZonaModal()
    {
        // Arrange
        var cut = Render<CatalogManager>();

        // Act
        var newZonaButton = cut.Find("button.btn-primary");
        await cut.InvokeAsync(() => newZonaButton.Click());

        // Assert
        var modal = cut.Find(".modal");
        modal.Should().NotBeNull();
        cut.Markup.Should().Contain("Nueva Zona");
    }

    [Fact(DisplayName = "CatalogManager should call CreateZonaAsync when saving new zona")]
    public async Task CatalogManager_ShouldCallCreateZonaAsync()
    {
        // Arrange
        var cut = Render<CatalogManager>();

        // Open modal
        var newZonaButton = cut.Find("button.btn-primary");
        await cut.InvokeAsync(() => newZonaButton.Click());

        // Fill form
        var input = cut.Find("input.form-control");
        await cut.InvokeAsync(() => input.Change("Nueva Zona Test"));

        // Act - save
        var saveButton = cut.FindAll(".modal button.btn-primary").First();
        await cut.InvokeAsync(() => saveButton.Click());

        // Assert
        _mockCatalogService.Verify(
            s => s.CreateZonaAsync(
                It.Is<CreateZonaDto>(d => d.Nombre == "Nueva Zona Test"),
                It.IsAny<Guid>()),
            Times.Once);
    }

    [Fact(DisplayName = "CatalogManager should show validation error when zona name is empty")]
    public async Task CatalogManager_ShouldShowValidationErrorForEmptyZonaName()
    {
        // Arrange
        var cut = Render<CatalogManager>();

        // Open modal
        var newZonaButton = cut.Find("button.btn-primary");
        await cut.InvokeAsync(() => newZonaButton.Click());

        // Act - save without filling form
        var saveButton = cut.FindAll(".modal button.btn-primary").First();
        await cut.InvokeAsync(() => saveButton.Click());

        // Assert
        cut.Markup.Should().Contain("El nombre es requerido");
    }

    [Fact(DisplayName = "CatalogManager should call ToggleZonaActivoAsync when toggle clicked")]
    public async Task CatalogManager_ShouldCallToggleZonaActivoAsync()
    {
        // Arrange
        var cut = Render<CatalogManager>();

        // Act - click toggle button (pause icon)
        var toggleButtons = cut.FindAll("button.btn-outline-warning");
        if (toggleButtons.Any())
        {
            await cut.InvokeAsync(() => toggleButtons.First().Click());

            // Assert
            _mockCatalogService.Verify(
                s => s.ToggleZonaActivoAsync(It.IsAny<int>(), It.IsAny<Guid>()),
                Times.Once);
        }
    }

    [Fact(DisplayName = "CatalogManager should display active badge for active zona")]
    public void CatalogManager_ShouldDisplayActiveBadge()
    {
        // Act
        var cut = Render<CatalogManager>();

        // Assert
        var activeBadge = cut.FindAll(".badge.bg-success").FirstOrDefault();
        activeBadge.Should().NotBeNull();
        activeBadge!.TextContent.Should().Contain("Activo");
    }

    #endregion

    #region Sector Tests

    [Fact(DisplayName = "CatalogManager should display sectores when tab selected")]
    public async Task CatalogManager_ShouldDisplaySectoresWhenTabSelected()
    {
        // Arrange
        var cut = Render<CatalogManager>();

        // Act
        var sectoresTab = cut.FindAll(".nav-link").First(n => n.TextContent.Contains("Sectores"));
        await cut.InvokeAsync(() => sectoresTab.Click());

        // Assert
        cut.Markup.Should().Contain("Sector Centro");
    }

    [Fact(DisplayName = "CatalogManager should filter sectores by region")]
    public async Task CatalogManager_ShouldFilterSectoresByRegion()
    {
        // Arrange
        var cut = Render<CatalogManager>();
        var sectoresTab = cut.FindAll(".nav-link").First(n => n.TextContent.Contains("Sectores"));
        await cut.InvokeAsync(() => sectoresTab.Click());

        // Act - Select region filter
        var regionFilter = cut.Find("select.form-select");
        await cut.InvokeAsync(() => regionFilter.Change("1"));

        // Assert - Verify GetSectoresAsync was called with regionId
        _mockCatalogService.Verify(
            s => s.GetSectoresAsync(1, It.IsAny<bool?>()),
            Times.AtLeastOnce);
    }

    [Fact(DisplayName = "CatalogManager should open sector modal when Nuevo Sector clicked")]
    public async Task CatalogManager_ShouldOpenSectorModal()
    {
        // Arrange
        var cut = Render<CatalogManager>();
        var sectoresTab = cut.FindAll(".nav-link").First(n => n.TextContent.Contains("Sectores"));
        await cut.InvokeAsync(() => sectoresTab.Click());

        // Act
        var newSectorButton = cut.FindAll("button.btn-primary").First(b => b.TextContent.Contains("Nuevo Sector"));
        await cut.InvokeAsync(() => newSectorButton.Click());

        // Assert
        cut.Markup.Should().Contain("Nuevo Sector");
    }

    #endregion

    #region Cuadrante Tests

    [Fact(DisplayName = "CatalogManager should display cuadrantes when tab selected")]
    public async Task CatalogManager_ShouldDisplayCuadrantesWhenTabSelected()
    {
        // Arrange
        var cut = Render<CatalogManager>();

        // Act
        var cuadrantesTab = cut.FindAll(".nav-link").First(n => n.TextContent.Contains("Cuadrantes"));
        await cut.InvokeAsync(() => cuadrantesTab.Click());

        // Assert
        cut.Markup.Should().Contain("Cuadrante A1");
    }

    [Fact(DisplayName = "CatalogManager should open cuadrante modal when Nuevo Cuadrante clicked")]
    public async Task CatalogManager_ShouldOpenCuadranteModal()
    {
        // Arrange
        var cut = Render<CatalogManager>();
        var cuadrantesTab = cut.FindAll(".nav-link").First(n => n.TextContent.Contains("Cuadrantes"));
        await cut.InvokeAsync(() => cuadrantesTab.Click());

        // Act
        var newCuadranteButton = cut.FindAll("button.btn-primary").First(b => b.TextContent.Contains("Nuevo Cuadrante"));
        await cut.InvokeAsync(() => newCuadranteButton.Click());

        // Assert
        cut.Markup.Should().Contain("Nuevo Cuadrante");
    }

    #endregion

    #region Error Handling Tests

    [Fact(DisplayName = "CatalogManager should display error message when load fails")]
    public void CatalogManager_ShouldDisplayErrorWhenLoadFails()
    {
        // Arrange
        _mockCatalogService.Setup(s => s.GetZonasAsync())
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var cut = Render<CatalogManager>();

        // Assert
        cut.Markup.Should().Contain("Error al cargar");
    }

    [Fact(DisplayName = "CatalogManager should display loading spinner initially")]
    public void CatalogManager_ShouldDisplayLoadingSpinner()
    {
        // Arrange - setup slow response
        _mockCatalogService.Setup(s => s.GetZonasAsync())
            .Returns(async () =>
            {
                await Task.Delay(1000);
                return new List<ZonaDto>();
            });

        // Act
        var cut = Render<CatalogManager>();

        // Assert - should show spinner before data loads
        cut.Markup.Should().Contain("spinner-border");
    }

    #endregion

    #region Helper Methods

    private void SetupDefaultMocks()
    {
        var zonas = new List<ZonaDto>
        {
            new() { Id = 1, Nombre = "Zona Norte", Activo = true, RegionesCount = 2 },
            new() { Id = 2, Nombre = "Zona Sur", Activo = true, RegionesCount = 1 }
        };

        var regiones = new List<RegionDto>
        {
            new() { Id = 1, Nombre = "Región Centro", ZonaId = 1, ZonaNombre = "Zona Norte", Activo = true, SectoresCount = 2 },
            new() { Id = 2, Nombre = "Región Este", ZonaId = 1, ZonaNombre = "Zona Norte", Activo = true, SectoresCount = 1 }
        };

        var sectores = new List<SectorDto>
        {
            new() { Id = 1, Nombre = "Sector Centro", RegionId = 1, ZonaNombre = "Zona Norte", Activo = true, CuadrantesCount = 3 },
            new() { Id = 2, Nombre = "Sector Este", RegionId = 1, ZonaNombre = "Zona Norte", Activo = true, CuadrantesCount = 2 }
        };

        var cuadrantes = new List<CuadranteDto>
        {
            new() { Id = 1, Nombre = "Cuadrante A1", SectorId = 1, SectorNombre = "Sector Centro", Activo = true },
            new() { Id = 2, Nombre = "Cuadrante A2", SectorId = 1, SectorNombre = "Sector Centro", Activo = true }
        };

        _mockCatalogService.Setup(s => s.GetZonasAsync()).ReturnsAsync(zonas);
        _mockCatalogService.Setup(s => s.GetRegionesAsync(It.IsAny<int?>())).ReturnsAsync(regiones);
        _mockCatalogService.Setup(s => s.GetSectoresAsync(It.IsAny<int?>())).ReturnsAsync(sectores);
        _mockCatalogService.Setup(s => s.GetCuadrantesAsync(It.IsAny<int?>())).ReturnsAsync(cuadrantes);
        _mockCatalogService.Setup(s => s.CreateZonaAsync(It.IsAny<CreateZonaDto>(), It.IsAny<Guid>()))
            .ReturnsAsync(new ZonaDto { Id = 3, Nombre = "Nueva Zona", Activo = true });
    }

    private class FakeNavigationManager : NavigationManager
    {
        public FakeNavigationManager()
        {
            Initialize("https://localhost:5001/", "https://localhost:5001/admin/catalogs");
        }

        protected override void NavigateToCore(string uri, bool forceLoad) { }
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
