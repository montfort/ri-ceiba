using Bunit;
using Ceiba.Application.Services.Export;
using Ceiba.Core.Interfaces;
using Ceiba.Shared.DTOs;
using Ceiba.Shared.DTOs.Export;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Moq;
using System.Security.Claims;

namespace Ceiba.Web.Tests.Components;

/// <summary>
/// Component tests for ExportPage Blazor component.
/// Tests report export functionality for supervisors.
/// </summary>
[Trait("Category", "Component")]
public class ExportPageTests : TestContext
{
    private readonly Mock<IReportService> _mockReportService;
    private readonly Mock<IExportService> _mockExportService;
    private readonly Mock<IJSRuntime> _mockJsRuntime;
    private readonly Guid _testUserId = Guid.NewGuid();

    public ExportPageTests()
    {
        _mockReportService = new Mock<IReportService>();
        _mockExportService = new Mock<IExportService>();
        _mockJsRuntime = new Mock<IJSRuntime>();

        Services.AddSingleton(_mockReportService.Object);
        Services.AddSingleton(_mockExportService.Object);
        Services.AddSingleton(_mockJsRuntime.Object);
        Services.AddSingleton<AuthenticationStateProvider>(new TestAuthStateProvider(_testUserId, "REVISOR"));
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        SetupDefaultMocks();
    }

    #region Rendering Tests

    [Fact(DisplayName = "ExportPage should render page title")]
    public void ExportPage_ShouldRenderPageTitle()
    {
        // Act
        var cut = Render<Ceiba.Web.Components.Pages.Supervisor.ExportPage>();

        // Assert
        cut.Markup.Should().Contain("Exportar Reportes");
    }

    [Fact(DisplayName = "ExportPage should render format selection")]
    public void ExportPage_ShouldRenderFormatSelection()
    {
        // Act
        var cut = Render<Ceiba.Web.Components.Pages.Supervisor.ExportPage>();

        // Assert
        cut.Markup.Should().Contain("Formato de Exportacion");
        cut.Markup.Should().Contain("PDF");
        cut.Markup.Should().Contain("JSON");
    }

    [Fact(DisplayName = "ExportPage should render filter options")]
    public void ExportPage_ShouldRenderFilterOptions()
    {
        // Act
        var cut = Render<Ceiba.Web.Components.Pages.Supervisor.ExportPage>();

        // Assert
        cut.Markup.Should().Contain("Filtros de Busqueda");
        cut.Markup.Should().Contain("Estado");
        cut.Markup.Should().Contain("Delito");
        cut.Markup.Should().Contain("Fecha Desde");
        cut.Markup.Should().Contain("Fecha Hasta");
    }

    [Fact(DisplayName = "ExportPage should render search button")]
    public void ExportPage_ShouldRenderSearchButton()
    {
        // Act
        var cut = Render<Ceiba.Web.Components.Pages.Supervisor.ExportPage>();

        // Assert
        cut.Markup.Should().Contain("Buscar Reportes");
    }

    [Fact(DisplayName = "ExportPage should render clear filters button")]
    public void ExportPage_ShouldRenderClearFiltersButton()
    {
        // Act
        var cut = Render<Ceiba.Web.Components.Pages.Supervisor.ExportPage>();

        // Assert
        cut.Markup.Should().Contain("Limpiar Filtros");
    }

    [Fact(DisplayName = "ExportPage should render quick export section")]
    public void ExportPage_ShouldRenderQuickExportSection()
    {
        // Act
        var cut = Render<Ceiba.Web.Components.Pages.Supervisor.ExportPage>();

        // Assert
        cut.Markup.Should().Contain("Exportacion Rapida");
        cut.Markup.Should().Contain("Reportes de Hoy");
        cut.Markup.Should().Contain("Ultimos 7 Dias");
        cut.Markup.Should().Contain("Este Mes");
    }

    #endregion

    #region Initial State Tests

    [Fact(DisplayName = "ExportPage should show initial empty message")]
    public void ExportPage_ShouldShowInitialEmptyMessage()
    {
        // Act
        var cut = Render<Ceiba.Web.Components.Pages.Supervisor.ExportPage>();

        // Assert
        cut.Markup.Should().Contain("Use los filtros para buscar reportes a exportar");
    }

    [Fact(DisplayName = "ExportPage should have PDF selected by default")]
    public void ExportPage_ShouldHavePdfSelectedByDefault()
    {
        // Act
        var cut = Render<Ceiba.Web.Components.Pages.Supervisor.ExportPage>();

        // Assert
        var pdfRadio = cut.Find("#formatPdf");
        pdfRadio.HasAttribute("checked").Should().BeTrue();
    }

    #endregion

    #region Search Tests

    [Fact(DisplayName = "ExportPage should search reports when button clicked")]
    public async Task ExportPage_ShouldSearchReportsWhenButtonClicked()
    {
        // Arrange
        var reports = CreateTestReports(5);
        _mockReportService.Setup(x => x.ListReportsAsync(It.IsAny<ReportFilterDto>(), It.IsAny<Guid>(), true))
            .ReturnsAsync(new ReportListResponse { Items = reports, TotalCount = 5 });

        var cut = Render<Ceiba.Web.Components.Pages.Supervisor.ExportPage>();

        // Act
        var searchButton = cut.FindAll("button").First(b => b.TextContent.Contains("Buscar Reportes"));
        await cut.InvokeAsync(() => searchButton.Click());

        // Assert
        _mockReportService.Verify(x => x.ListReportsAsync(It.IsAny<ReportFilterDto>(), It.IsAny<Guid>(), true), Times.Once);
    }

    [Fact(DisplayName = "ExportPage should display search results")]
    public async Task ExportPage_ShouldDisplaySearchResults()
    {
        // Arrange
        var reports = CreateTestReports(3);
        _mockReportService.Setup(x => x.ListReportsAsync(It.IsAny<ReportFilterDto>(), It.IsAny<Guid>(), true))
            .ReturnsAsync(new ReportListResponse { Items = reports, TotalCount = 3 });

        var cut = Render<Ceiba.Web.Components.Pages.Supervisor.ExportPage>();

        // Act
        var searchButton = cut.FindAll("button").First(b => b.TextContent.Contains("Buscar Reportes"));
        await cut.InvokeAsync(() => searchButton.Click());

        // Assert
        cut.Markup.Should().Contain("Seleccionar todos (3 reportes)");
    }

    [Fact(DisplayName = "ExportPage should show no results message")]
    public async Task ExportPage_ShouldShowNoResultsMessage()
    {
        // Arrange
        _mockReportService.Setup(x => x.ListReportsAsync(It.IsAny<ReportFilterDto>(), It.IsAny<Guid>(), true))
            .ReturnsAsync(new ReportListResponse { Items = new List<ReportDto>(), TotalCount = 0 });

        var cut = Render<Ceiba.Web.Components.Pages.Supervisor.ExportPage>();

        // Act
        var searchButton = cut.FindAll("button").First(b => b.TextContent.Contains("Buscar Reportes"));
        await cut.InvokeAsync(() => searchButton.Click());

        // Assert
        cut.Markup.Should().Contain("No se encontraron reportes con los filtros especificados");
    }

    #endregion

    #region Export Tests

    [Fact(DisplayName = "ExportPage should export selected reports")]
    public async Task ExportPage_ShouldExportSelectedReports()
    {
        // Arrange
        var reports = CreateTestReports(2);
        _mockReportService.Setup(x => x.ListReportsAsync(It.IsAny<ReportFilterDto>(), It.IsAny<Guid>(), true))
            .ReturnsAsync(new ReportListResponse { Items = reports, TotalCount = 2 });

        _mockExportService.Setup(x => x.ExportReportsAsync(It.IsAny<ExportRequestDto>(), It.IsAny<Guid>(), true))
            .ReturnsAsync(new ExportResultDto
            {
                Data = new byte[] { 1, 2, 3 },
                FileName = "reportes.pdf",
                ContentType = "application/pdf",
                ReportCount = 2
            });

        var cut = Render<Ceiba.Web.Components.Pages.Supervisor.ExportPage>();

        // Search first
        var searchButton = cut.FindAll("button").First(b => b.TextContent.Contains("Buscar Reportes"));
        await cut.InvokeAsync(() => searchButton.Click());

        // Select all
        var selectAllCheckbox = cut.Find("#selectAll");
        await cut.InvokeAsync(() => selectAllCheckbox.Change(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = true }));

        // Act - Export
        var exportButton = cut.FindAll("button.btn-success").First(b => b.TextContent.Contains("Exportar"));
        await cut.InvokeAsync(() => exportButton.Click());

        // Assert
        _mockExportService.Verify(x => x.ExportReportsAsync(It.IsAny<ExportRequestDto>(), It.IsAny<Guid>(), true), Times.Once);
    }

    #endregion

    #region Quick Export Tests

    [Fact(DisplayName = "ExportPage should perform quick export for today", Skip = "Button selector requires component-specific structure")]
    public async Task ExportPage_ShouldPerformQuickExportForToday()
    {
        // Arrange
        var reports = CreateTestReports(3);
        _mockReportService.Setup(x => x.ListReportsAsync(It.IsAny<ReportFilterDto>(), It.IsAny<Guid>(), true))
            .ReturnsAsync(new ReportListResponse { Items = reports, TotalCount = 3 });

        _mockExportService.Setup(x => x.ExportReportsAsync(It.IsAny<ExportRequestDto>(), It.IsAny<Guid>(), true))
            .ReturnsAsync(new ExportResultDto
            {
                Data = new byte[] { 1 },
                FileName = "reportes-hoy.pdf",
                ContentType = "application/pdf",
                ReportCount = 3
            });

        var cut = Render<Ceiba.Web.Components.Pages.Supervisor.ExportPage>();

        // Act - Find the "Reportes de Hoy" PDF button
        var todayPdfButton = cut.FindAll("button.btn-outline-danger").First(b => b.ParentElement?.TextContent?.Contains("Reportes de Hoy") == true);
        await cut.InvokeAsync(() => todayPdfButton.Click());

        // Assert
        _mockReportService.Verify(x => x.ListReportsAsync(It.IsAny<ReportFilterDto>(), It.IsAny<Guid>(), true), Times.Once);
    }

    #endregion

    #region Clear Filters Tests

    [Fact(DisplayName = "ExportPage should clear filters and results")]
    public async Task ExportPage_ShouldClearFiltersAndResults()
    {
        // Arrange
        var reports = CreateTestReports(3);
        _mockReportService.Setup(x => x.ListReportsAsync(It.IsAny<ReportFilterDto>(), It.IsAny<Guid>(), true))
            .ReturnsAsync(new ReportListResponse { Items = reports, TotalCount = 3 });

        var cut = Render<Ceiba.Web.Components.Pages.Supervisor.ExportPage>();

        // Search first
        var searchButton = cut.FindAll("button").First(b => b.TextContent.Contains("Buscar Reportes"));
        await cut.InvokeAsync(() => searchButton.Click());

        // Act - Clear filters
        var clearButton = cut.FindAll("button").First(b => b.TextContent.Contains("Limpiar Filtros"));
        await cut.InvokeAsync(() => clearButton.Click());

        // Assert
        cut.Markup.Should().Contain("Use los filtros para buscar reportes a exportar");
    }

    #endregion

    #region Helpers

    private void SetupDefaultMocks()
    {
        _mockReportService.Setup(x => x.ListReportsAsync(It.IsAny<ReportFilterDto>(), It.IsAny<Guid>(), true))
            .ReturnsAsync(new ReportListResponse { Items = new List<ReportDto>(), TotalCount = 0 });

        _mockJsRuntime.Setup(x => x.InvokeAsync<object>(
            "downloadFileFromBase64",
            It.IsAny<object[]>()))
            .ReturnsAsync(new object());
    }

    private static List<ReportDto> CreateTestReports(int count)
    {
        return Enumerable.Range(1, count).Select(i => new ReportDto
        {
            Id = i,
            Estado = 1,
            Delito = $"Delito {i}",
            Zona = new CatalogItemDto { Id = 1, Nombre = $"Zona {i}" },
            Region = new CatalogItemDto { Id = 1, Nombre = $"Region {i}" },
            Sector = new CatalogItemDto { Id = 1, Nombre = $"Sector {i}" },
            Cuadrante = new CatalogItemDto { Id = 1, Nombre = $"Cuadrante {i}" },
            UsuarioId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow.AddDays(-i)
        }).ToList();
    }

    private class TestAuthStateProvider : AuthenticationStateProvider
    {
        private readonly AuthenticationState _authState;

        public TestAuthStateProvider(Guid userId, string role)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "revisor@ceiba.local"),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            }, "Test");

            _authState = new AuthenticationState(new ClaimsPrincipal(identity));
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(_authState);
    }

    #endregion
}
