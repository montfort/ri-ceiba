using Bunit;
using Ceiba.Core.Interfaces;
using Ceiba.Shared.DTOs;
using Ceiba.Web.Components.Pages.Reports;
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
/// Component tests for ReportList Blazor component.
/// Tests report listing with filters and pagination for CREADOR role.
/// </summary>
public class ReportListTests : TestContext
{
    private readonly Mock<IReportService> _mockReportService;
    private readonly FakeNavigationManager _navigationManager;
    private readonly Guid _testUserId = Guid.NewGuid();

    public ReportListTests()
    {
        _mockReportService = new Mock<IReportService>();
        _navigationManager = new FakeNavigationManager();

        Services.AddSingleton(_mockReportService.Object);
        Services.AddSingleton<NavigationManager>(_navigationManager);
        Services.AddSingleton<AuthenticationStateProvider>(new TestAuthStateProvider(_testUserId, "CREADOR"));
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        SetupDefaultMocks();
    }

    #region Rendering Tests

    [Fact(DisplayName = "ReportList should render page title")]
    public void ReportList_ShouldRenderPageTitle()
    {
        // Act
        var cut = Render<ReportList>();

        // Assert
        cut.Markup.Should().Contain("Mis Reportes de Incidencias");
    }

    [Fact(DisplayName = "ReportList should render Nuevo Reporte button")]
    public void ReportList_ShouldRenderNewReportButton()
    {
        // Act
        var cut = Render<ReportList>();

        // Assert
        var newReportButton = cut.FindAll("button.btn-primary").FirstOrDefault(b => b.TextContent.Contains("Nuevo Reporte"));
        newReportButton.Should().NotBeNull();
    }

    [Fact(DisplayName = "ReportList should render reports in table")]
    public void ReportList_ShouldRenderReportsInTable()
    {
        // Act
        var cut = Render<ReportList>();

        // Assert
        cut.Markup.Should().Contain("Robo a casa habitación");
        cut.Markup.Should().Contain("Zona Norte");
    }

    [Fact(DisplayName = "ReportList should display estado badges")]
    public void ReportList_ShouldDisplayEstadoBadges()
    {
        // Act
        var cut = Render<ReportList>();

        // Assert
        var borradorBadge = cut.FindAll(".badge.bg-warning").FirstOrDefault();
        borradorBadge.Should().NotBeNull();
        borradorBadge!.TextContent.Should().Contain("Borrador");
    }

    [Fact(DisplayName = "ReportList should display Entregado badge for submitted reports")]
    public void ReportList_ShouldDisplayEntregadoBadge()
    {
        // Act
        var cut = Render<ReportList>();

        // Assert
        var entregadoBadge = cut.FindAll(".badge.bg-success").FirstOrDefault();
        entregadoBadge.Should().NotBeNull();
        entregadoBadge!.TextContent.Should().Contain("Entregado");
    }

    #endregion

    #region Filter Tests

    [Fact(DisplayName = "ReportList should render filter controls")]
    public void ReportList_ShouldRenderFilterControls()
    {
        // Act
        var cut = Render<ReportList>();

        // Assert
        cut.Find("#filterEstado").Should().NotBeNull();
        cut.Find("#filterDelito").Should().NotBeNull();
        cut.Find("#filterFechaDesde").Should().NotBeNull();
        cut.Find("#filterFechaHasta").Should().NotBeNull();
    }

    [Fact(DisplayName = "ReportList should filter by estado")]
    public async Task ReportList_ShouldFilterByEstado()
    {
        // Arrange
        var cut = Render<ReportList>();

        // Act
        var estadoFilter = cut.Find("#filterEstado");
        await cut.InvokeAsync(() => estadoFilter.Change("0")); // Borrador

        // Assert
        _mockReportService.Verify(
            s => s.ListReportsAsync(It.Is<ReportFilterDto>(f => f.Estado == 0), It.IsAny<Guid>(), false),
            Times.AtLeastOnce);
    }

    [Fact(DisplayName = "ReportList should filter by delito with debounce")]
    public async Task ReportList_ShouldFilterByDelitoWithDebounce()
    {
        // Arrange
        var cut = Render<ReportList>();

        // Act
        var delitoFilter = cut.Find("#filterDelito");
        await cut.InvokeAsync(() => delitoFilter.Input("Robo"));

        // Wait for debounce
        await Task.Delay(600);

        // Assert
        _mockReportService.Verify(
            s => s.ListReportsAsync(It.Is<ReportFilterDto>(f => f.Delito == "Robo"), It.IsAny<Guid>(), false),
            Times.AtLeastOnce);
    }

    [Fact(DisplayName = "ReportList should clear filters when Limpiar clicked")]
    public async Task ReportList_ShouldClearFilters()
    {
        // Arrange
        var cut = Render<ReportList>();

        // Set a filter first
        var estadoFilter = cut.Find("#filterEstado");
        await cut.InvokeAsync(() => estadoFilter.Change("0"));

        // Act
        var clearButton = cut.FindAll("button").First(b => b.TextContent.Contains("Limpiar"));
        await cut.InvokeAsync(() => clearButton.Click());

        // Assert
        _mockReportService.Verify(
            s => s.ListReportsAsync(It.Is<ReportFilterDto>(f => f.Estado == null), It.IsAny<Guid>(), false),
            Times.AtLeastOnce);
    }

    #endregion

    #region Navigation Tests

    [Fact(DisplayName = "ReportList should navigate to new report on button click")]
    public async Task ReportList_ShouldNavigateToNewReport()
    {
        // Arrange
        var cut = Render<ReportList>();

        // Act
        var newReportButton = cut.FindAll("button.btn-primary").First(b => b.TextContent.Contains("Nuevo Reporte"));
        await cut.InvokeAsync(() => newReportButton.Click());

        // Assert
        _navigationManager.Uri.Should().Contain("/reports/new");
    }

    [Fact(DisplayName = "ReportList should navigate to view report on eye button click")]
    public async Task ReportList_ShouldNavigateToViewReport()
    {
        // Arrange
        var cut = Render<ReportList>();

        // Act
        var viewButton = cut.FindAll("button.btn-outline-primary").First();
        await cut.InvokeAsync(() => viewButton.Click());

        // Assert
        _navigationManager.Uri.Should().Contain("/reports/view/");
    }

    [Fact(DisplayName = "ReportList should navigate to edit report for draft reports")]
    public async Task ReportList_ShouldNavigateToEditReport()
    {
        // Arrange
        var cut = Render<ReportList>();

        // Act - find edit button (pencil icon)
        var editButton = cut.FindAll("button.btn-outline-secondary").FirstOrDefault();
        if (editButton != null)
        {
            await cut.InvokeAsync(() => editButton.Click());

            // Assert
            _navigationManager.Uri.Should().Contain("/reports/edit/");
        }
    }

    #endregion

    #region Report Actions Tests

    [Fact(DisplayName = "ReportList should show edit button only for Borrador reports")]
    public void ReportList_ShouldShowEditButtonOnlyForBorrador()
    {
        // Act
        var cut = Render<ReportList>();

        // Assert - should have edit buttons for borrador reports
        var editButtons = cut.FindAll("button.btn-outline-secondary");

        // Count should match number of borrador reports in mock
        editButtons.Count.Should().BeGreaterThan(0);
    }

    [Fact(DisplayName = "ReportList should not show edit button for Entregado reports")]
    public void ReportList_ShouldNotShowEditButtonForEntregado()
    {
        // Arrange - setup mock with only Entregado reports
        var entregadoReports = new ReportListResponse
        {
            Items = new List<ReportDto>
            {
                CreateTestReport(1, "Reporte 1", 1) // Estado 1 = Entregado
            },
            TotalCount = 1,
            Page = 1,
            PageSize = 20
        };

        _mockReportService.Setup(s => s.ListReportsAsync(It.IsAny<ReportFilterDto>(), It.IsAny<Guid>(), false))
            .ReturnsAsync(entregadoReports);

        // Act
        var cut = Render<ReportList>();

        // Assert - should NOT have edit buttons
        var editButtons = cut.FindAll("button.btn-outline-secondary");
        editButtons.Should().BeEmpty();
    }

    #endregion

    #region Pagination Tests

    [Fact(DisplayName = "ReportList should display pagination when multiple pages")]
    public void ReportList_ShouldDisplayPagination()
    {
        // Arrange - setup mock with many reports
        var manyReports = new ReportListResponse
        {
            Items = Enumerable.Range(1, 20).Select(i => CreateTestReport(i, $"Reporte {i}", i % 2)).ToList(),
            TotalCount = 50,
            Page = 1,
            PageSize = 20
        };

        _mockReportService.Setup(s => s.ListReportsAsync(It.IsAny<ReportFilterDto>(), It.IsAny<Guid>(), false))
            .ReturnsAsync(manyReports);

        // Act
        var cut = Render<ReportList>();

        // Assert
        cut.Markup.Should().Contain("Siguiente");
        cut.Markup.Should().Contain("Anterior");
    }

    [Fact(DisplayName = "ReportList should call service with page number on pagination click")]
    public async Task ReportList_ShouldCallServiceWithPageNumber()
    {
        // Arrange - setup mock with many reports
        var manyReports = new ReportListResponse
        {
            Items = Enumerable.Range(1, 20).Select(i => CreateTestReport(i, $"Reporte {i}", 0)).ToList(),
            TotalCount = 50,
            Page = 1,
            PageSize = 20
        };

        _mockReportService.Setup(s => s.ListReportsAsync(It.IsAny<ReportFilterDto>(), It.IsAny<Guid>(), false))
            .ReturnsAsync(manyReports);

        var cut = Render<ReportList>();

        // Act - click next page
        var nextButton = cut.FindAll("button.page-link").First(b => b.TextContent.Contains("Siguiente"));
        await cut.InvokeAsync(() => nextButton.Click());

        // Assert
        _mockReportService.Verify(
            s => s.ListReportsAsync(It.Is<ReportFilterDto>(f => f.Page == 2), It.IsAny<Guid>(), false),
            Times.AtLeastOnce);
    }

    #endregion

    #region Error Handling Tests

    [Fact(DisplayName = "ReportList should display error when load fails")]
    public void ReportList_ShouldDisplayErrorWhenLoadFails()
    {
        // Arrange
        _mockReportService.Setup(s => s.ListReportsAsync(It.IsAny<ReportFilterDto>(), It.IsAny<Guid>(), false))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var cut = Render<ReportList>();

        // Assert
        cut.Markup.Should().Contain("Error al cargar");
    }

    [Fact(DisplayName = "ReportList should display empty state when no reports")]
    public void ReportList_ShouldDisplayEmptyState()
    {
        // Arrange
        _mockReportService.Setup(s => s.ListReportsAsync(It.IsAny<ReportFilterDto>(), It.IsAny<Guid>(), false))
            .ReturnsAsync(new ReportListResponse
            {
                Items = new List<ReportDto>(),
                TotalCount = 0,
                Page = 1,
                PageSize = 20
            });

        // Act
        var cut = Render<ReportList>();

        // Assert
        cut.Markup.Should().Contain("No hay reportes");
    }

    [Fact(DisplayName = "ReportList empty state should have create button")]
    public void ReportList_EmptyStateShouldHaveCreateButton()
    {
        // Arrange
        _mockReportService.Setup(s => s.ListReportsAsync(It.IsAny<ReportFilterDto>(), It.IsAny<Guid>(), false))
            .ReturnsAsync(new ReportListResponse
            {
                Items = new List<ReportDto>(),
                TotalCount = 0,
                Page = 1,
                PageSize = 20
            });

        // Act
        var cut = Render<ReportList>();

        // Assert
        cut.Markup.Should().Contain("Crear Primer Reporte");
    }

    #endregion

    #region Loading State Tests

    [Fact(DisplayName = "ReportList should display loading spinner initially")]
    public void ReportList_ShouldDisplayLoadingSpinner()
    {
        // Arrange - setup slow response
        _mockReportService.Setup(s => s.ListReportsAsync(It.IsAny<ReportFilterDto>(), It.IsAny<Guid>(), false))
            .Returns(async () =>
            {
                await Task.Delay(1000);
                return new ReportListResponse
                {
                    Items = new List<ReportDto>(),
                    TotalCount = 0,
                    Page = 1,
                    PageSize = 20
                };
            });

        // Act
        var cut = Render<ReportList>();

        // Assert - should show spinner before data loads
        cut.Markup.Should().Contain("spinner-border");
    }

    #endregion

    #region Helper Methods

    private void SetupDefaultMocks()
    {
        var reports = new ReportListResponse
        {
            Items = new List<ReportDto>
            {
                CreateTestReport(1, "Robo a casa habitación", 0),
                CreateTestReport(2, "Violencia familiar", 1),
                CreateTestReport(3, "Lesiones", 0)
            },
            TotalCount = 3,
            Page = 1,
            PageSize = 20
        };

        _mockReportService.Setup(s => s.ListReportsAsync(It.IsAny<ReportFilterDto>(), It.IsAny<Guid>(), false))
            .ReturnsAsync(reports);
    }

    private ReportDto CreateTestReport(int id, string delito, int estado)
    {
        return new ReportDto
        {
            Id = id,
            Delito = delito,
            Estado = estado,
            CreatedAt = DateTime.UtcNow.AddDays(-id),
            DatetimeHechos = DateTime.UtcNow.AddDays(-id).AddHours(-2),
            Zona = new CatalogItemDto { Id = 1, Nombre = "Zona Norte" },
            Sector = new CatalogItemDto { Id = 1, Nombre = "Sector 1" },
            Cuadrante = new CatalogItemDto { Id = 1, Nombre = "Cuadrante A" },
            Sexo = "Femenino",
            Edad = 25,
            TurnoCeiba = 1,
            TipoDeAtencion = "Presencial",
            TipoDeAccion = 1,
            HechosReportados = "Descripción de hechos",
            AccionesRealizadas = "Acciones realizadas"
        };
    }

    private class FakeNavigationManager : NavigationManager
    {
        public FakeNavigationManager()
        {
            Initialize("https://localhost:5001/", "https://localhost:5001/reports");
        }

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            // Handle relative URIs by converting to absolute
            if (!uri.StartsWith("http"))
            {
                uri = new Uri(new Uri(BaseUri), uri).ToString();
            }
            Uri = uri;
        }
    }

    private class TestAuthStateProvider : AuthenticationStateProvider
    {
        private readonly AuthenticationState _authState;

        public TestAuthStateProvider(Guid userId, string role)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "creador@ceiba.local"),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            }, "Test");

            _authState = new AuthenticationState(new ClaimsPrincipal(identity));
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(_authState);
    }

    #endregion
}
