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
/// Component tests for ReportListRevisor Blazor component.
/// Tests report supervision view for REVISOR role.
/// </summary>
[Trait("Category", "Component")]
public class ReportListRevisorTests : TestContext
{
    private readonly Mock<IReportService> _mockReportService;
    private readonly Mock<IExportService> _mockExportService;
    private readonly Mock<IJSRuntime> _mockJsRuntime;
    private readonly Guid _testUserId = Guid.NewGuid();

    public ReportListRevisorTests()
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

    [Fact(DisplayName = "ReportListRevisor should render page title")]
    public void ReportListRevisor_ShouldRenderPageTitle()
    {
        // Act
        var cut = Render<Ceiba.Web.Components.Pages.Reports.ReportListRevisor>();

        // Assert
        cut.Markup.Should().Contain("Supervisar Reportes");
    }

    [Fact(DisplayName = "ReportListRevisor should render filter section")]
    public void ReportListRevisor_ShouldRenderFilterSection()
    {
        // Act
        var cut = Render<Ceiba.Web.Components.Pages.Reports.ReportListRevisor>();

        // Assert
        cut.Markup.Should().Contain("Estado");
        cut.Markup.Should().Contain("Delito");
        cut.Markup.Should().Contain("Zona");
        cut.Markup.Should().Contain("Desde");
        cut.Markup.Should().Contain("Hasta");
    }

    [Fact(DisplayName = "ReportListRevisor should render estado filter options")]
    public void ReportListRevisor_ShouldRenderEstadoFilterOptions()
    {
        // Act
        var cut = Render<Ceiba.Web.Components.Pages.Reports.ReportListRevisor>();

        // Assert
        cut.Markup.Should().Contain("Todos");
        cut.Markup.Should().Contain("Borrador");
        cut.Markup.Should().Contain("Entregado");
    }

    #endregion

    #region Loading State Tests

    [Fact(DisplayName = "ReportListRevisor should show loading state initially")]
    public void ReportListRevisor_ShouldShowLoadingStateInitially()
    {
        // Arrange - Make the service never complete
        _mockReportService.Setup(x => x.ListReportsAsync(It.IsAny<ReportFilterDto>(), It.IsAny<Guid>(), true))
            .Returns(new TaskCompletionSource<ReportListResponse>().Task);

        // Act
        var cut = Render<Ceiba.Web.Components.Pages.Reports.ReportListRevisor>();

        // Assert
        cut.Markup.Should().Contain("Cargando reportes...");
    }

    #endregion

    #region Report List Tests

    [Fact(DisplayName = "ReportListRevisor should display reports in table")]
    public void ReportListRevisor_ShouldDisplayReportsInTable()
    {
        // Arrange
        var reports = CreateTestReports(3);
        _mockReportService.Setup(x => x.ListReportsAsync(It.IsAny<ReportFilterDto>(), It.IsAny<Guid>(), true))
            .ReturnsAsync(new ReportListResponse { Items = reports, TotalCount = 3 });

        // Act
        var cut = Render<Ceiba.Web.Components.Pages.Reports.ReportListRevisor>();

        // Assert
        cut.Markup.Should().Contain("<table");
        cut.Markup.Should().Contain("Delito 1");
        cut.Markup.Should().Contain("Delito 2");
        cut.Markup.Should().Contain("Delito 3");
    }

    [Fact(DisplayName = "ReportListRevisor should show table headers")]
    public void ReportListRevisor_ShouldShowTableHeaders()
    {
        // Arrange
        var reports = CreateTestReports(1);
        _mockReportService.Setup(x => x.ListReportsAsync(It.IsAny<ReportFilterDto>(), It.IsAny<Guid>(), true))
            .ReturnsAsync(new ReportListResponse { Items = reports, TotalCount = 1 });

        // Act
        var cut = Render<Ceiba.Web.Components.Pages.Reports.ReportListRevisor>();

        // Assert
        cut.Markup.Should().Contain("<th");
        cut.Markup.Should().Contain("ID");
        cut.Markup.Should().Contain("Fecha");
        cut.Markup.Should().Contain("Estado");
        cut.Markup.Should().Contain("Delito");
        cut.Markup.Should().Contain("Zona");
        cut.Markup.Should().Contain("Usuario");
        cut.Markup.Should().Contain("Acciones");
    }

    [Fact(DisplayName = "ReportListRevisor should show empty message when no reports")]
    public void ReportListRevisor_ShouldShowEmptyMessageWhenNoReports()
    {
        // Arrange
        _mockReportService.Setup(x => x.ListReportsAsync(It.IsAny<ReportFilterDto>(), It.IsAny<Guid>(), true))
            .ReturnsAsync(new ReportListResponse { Items = new List<ReportDto>(), TotalCount = 0 });

        // Act
        var cut = Render<Ceiba.Web.Components.Pages.Reports.ReportListRevisor>();

        // Assert
        cut.Markup.Should().Contain("No hay reportes que coincidan con los filtros");
    }

    [Fact(DisplayName = "ReportListRevisor should display estado badges correctly")]
    public void ReportListRevisor_ShouldDisplayEstadoBadgesCorrectly()
    {
        // Arrange
        var reports = new List<ReportDto>
        {
            CreateTestReport(1, 0),
            CreateTestReport(2, 1)
        };
        _mockReportService.Setup(x => x.ListReportsAsync(It.IsAny<ReportFilterDto>(), It.IsAny<Guid>(), true))
            .ReturnsAsync(new ReportListResponse { Items = reports, TotalCount = 2 });

        // Act
        var cut = Render<Ceiba.Web.Components.Pages.Reports.ReportListRevisor>();

        // Assert
        cut.Markup.Should().Contain("bg-warning"); // Borrador
        cut.Markup.Should().Contain("bg-success"); // Entregado
    }

    #endregion

    #region Action Buttons Tests

    [Fact(DisplayName = "ReportListRevisor should show action buttons for each report")]
    public void ReportListRevisor_ShouldShowActionButtonsForEachReport()
    {
        // Arrange
        var reports = CreateTestReports(1);
        _mockReportService.Setup(x => x.ListReportsAsync(It.IsAny<ReportFilterDto>(), It.IsAny<Guid>(), true))
            .ReturnsAsync(new ReportListResponse { Items = reports, TotalCount = 1 });

        // Act
        var cut = Render<Ceiba.Web.Components.Pages.Reports.ReportListRevisor>();

        // Assert
        cut.Markup.Should().Contain("bi-eye"); // View button
        cut.Markup.Should().Contain("bi-pencil"); // Edit button
        cut.Markup.Should().Contain("bi-file-pdf"); // PDF export
        cut.Markup.Should().Contain("bi-file-code"); // JSON export
    }

    #endregion

    #region Selection Tests

    [Fact(DisplayName = "ReportListRevisor should allow selecting reports")]
    public void ReportListRevisor_ShouldAllowSelectingReports()
    {
        // Arrange
        var reports = CreateTestReports(3);
        _mockReportService.Setup(x => x.ListReportsAsync(It.IsAny<ReportFilterDto>(), It.IsAny<Guid>(), true))
            .ReturnsAsync(new ReportListResponse { Items = reports, TotalCount = 3 });

        // Act
        var cut = Render<Ceiba.Web.Components.Pages.Reports.ReportListRevisor>();

        // Assert
        var checkboxes = cut.FindAll("input[type='checkbox']");
        checkboxes.Should().HaveCountGreaterThan(0);
    }

    [Fact(DisplayName = "ReportListRevisor should select all reports")]
    public async Task ReportListRevisor_ShouldSelectAllReports()
    {
        // Arrange
        var reports = CreateTestReports(3);
        _mockReportService.Setup(x => x.ListReportsAsync(It.IsAny<ReportFilterDto>(), It.IsAny<Guid>(), true))
            .ReturnsAsync(new ReportListResponse { Items = reports, TotalCount = 3 });

        var cut = Render<Ceiba.Web.Components.Pages.Reports.ReportListRevisor>();

        // Act - Click select all checkbox in header
        var selectAllCheckbox = cut.Find("thead input[type='checkbox']");
        await cut.InvokeAsync(() => selectAllCheckbox.Change(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = true }));

        // Assert - Should show count in export buttons
        cut.Markup.Should().Contain("(3)");
    }

    #endregion

    #region Export Tests

    [Fact(DisplayName = "ReportListRevisor should export single report to PDF")]
    public async Task ReportListRevisor_ShouldExportSingleReportToPdf()
    {
        // Arrange
        var reports = CreateTestReports(1);
        _mockReportService.Setup(x => x.ListReportsAsync(It.IsAny<ReportFilterDto>(), It.IsAny<Guid>(), true))
            .ReturnsAsync(new ReportListResponse { Items = reports, TotalCount = 1 });

        _mockExportService.Setup(x => x.ExportSingleReportAsync(It.IsAny<int>(), ExportFormat.PDF, It.IsAny<Guid>(), true))
            .ReturnsAsync(new ExportResultDto
            {
                Data = new byte[] { 1 },
                FileName = "reporte-1.pdf",
                ContentType = "application/pdf",
                ReportCount = 1
            });

        var cut = Render<Ceiba.Web.Components.Pages.Reports.ReportListRevisor>();

        // Act
        var pdfButton = cut.Find("button.btn-outline-danger[title='Exportar PDF']");
        await cut.InvokeAsync(() => pdfButton.Click());

        // Assert
        _mockExportService.Verify(x => x.ExportSingleReportAsync(1, ExportFormat.PDF, It.IsAny<Guid>(), true), Times.Once);
    }

    #endregion

    #region Pagination Tests

    [Fact(DisplayName = "ReportListRevisor should show pagination when multiple pages")]
    public void ReportListRevisor_ShouldShowPaginationWhenMultiplePages()
    {
        // Arrange
        var reports = CreateTestReports(20);
        _mockReportService.Setup(x => x.ListReportsAsync(It.IsAny<ReportFilterDto>(), It.IsAny<Guid>(), true))
            .ReturnsAsync(new ReportListResponse { Items = reports, TotalCount = 50 });

        // Act
        var cut = Render<Ceiba.Web.Components.Pages.Reports.ReportListRevisor>();

        // Assert
        cut.Markup.Should().Contain("Anterior");
        cut.Markup.Should().Contain("Siguiente");
        cut.Markup.Should().Contain("Mostrando");
    }

    #endregion

    #region Filter Tests

    [Fact(DisplayName = "ReportListRevisor should filter by estado")]
    public async Task ReportListRevisor_ShouldFilterByEstado()
    {
        // Arrange
        var reports = CreateTestReports(5);
        _mockReportService.Setup(x => x.ListReportsAsync(It.IsAny<ReportFilterDto>(), It.IsAny<Guid>(), true))
            .ReturnsAsync(new ReportListResponse { Items = reports, TotalCount = 5 });

        var cut = Render<Ceiba.Web.Components.Pages.Reports.ReportListRevisor>();

        // Act
        var estadoSelect = cut.Find("#filterEstado");
        await cut.InvokeAsync(() => estadoSelect.Change(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = "1" }));

        // Assert
        _mockReportService.Verify(x => x.ListReportsAsync(
            It.Is<ReportFilterDto>(f => f.Estado == 1),
            It.IsAny<Guid>(),
            true), Times.AtLeastOnce);
    }

    #endregion

    #region Error Handling Tests

    [Fact(DisplayName = "ReportListRevisor should show error on load failure")]
    public void ReportListRevisor_ShouldShowErrorOnLoadFailure()
    {
        // Arrange
        _mockReportService.Setup(x => x.ListReportsAsync(It.IsAny<ReportFilterDto>(), It.IsAny<Guid>(), true))
            .ThrowsAsync(new InvalidOperationException("Error de prueba"));

        // Act
        var cut = Render<Ceiba.Web.Components.Pages.Reports.ReportListRevisor>();

        // Assert
        cut.Markup.Should().Contain("Error al cargar los reportes");
    }

    #endregion

    #region Mobile View Tests

    [Fact(DisplayName = "ReportListRevisor should have mobile card view")]
    public void ReportListRevisor_ShouldHaveMobileCardView()
    {
        // Arrange
        var reports = CreateTestReports(1);
        _mockReportService.Setup(x => x.ListReportsAsync(It.IsAny<ReportFilterDto>(), It.IsAny<Guid>(), true))
            .ReturnsAsync(new ReportListResponse { Items = reports, TotalCount = 1 });

        // Act
        var cut = Render<Ceiba.Web.Components.Pages.Reports.ReportListRevisor>();

        // Assert
        cut.Markup.Should().Contain("d-md-none"); // Mobile view class
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

    private static ReportDto CreateTestReport(int id, int estado)
    {
        return new ReportDto
        {
            Id = id,
            Estado = estado,
            Delito = $"Delito {id}",
            Zona = new CatalogItemDto { Id = 1, Nombre = $"Zona {id}" },
            Region = new CatalogItemDto { Id = 1, Nombre = $"Region {id}" },
            Sector = new CatalogItemDto { Id = 1, Nombre = $"Sector {id}" },
            Cuadrante = new CatalogItemDto { Id = 1, Nombre = $"Cuadrante {id}" },
            UsuarioId = Guid.NewGuid(),
            UsuarioEmail = $"user{id}@test.com",
            CreatedAt = DateTime.UtcNow
        };
    }

    private static List<ReportDto> CreateTestReports(int count)
    {
        return Enumerable.Range(1, count).Select(i => CreateTestReport(i, i % 2)).ToList();
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
