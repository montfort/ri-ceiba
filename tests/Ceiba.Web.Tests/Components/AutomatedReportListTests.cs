using Bunit;
using Ceiba.Core.Interfaces;
using Ceiba.Shared.DTOs;
using Ceiba.Web.Components.Pages.Automated;
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
/// Component tests for AutomatedReportList Blazor component.
/// Tests automated report listing with filters and generation modal.
/// Phase 3: Coverage improvement tests.
/// </summary>
[Trait("Category", "Component")]
public class AutomatedReportListTests : TestContext
{
    private readonly Mock<IAutomatedReportService> _mockReportService;
    private readonly FakeNavigationManager _navigationManager;
    private readonly Guid _testUserId = Guid.NewGuid();

    public AutomatedReportListTests()
    {
        _mockReportService = new Mock<IAutomatedReportService>();
        _navigationManager = new FakeNavigationManager();

        Services.AddSingleton(_mockReportService.Object);
        Services.AddSingleton<NavigationManager>(_navigationManager);
        Services.AddSingleton<AuthenticationStateProvider>(new TestAuthStateProvider(_testUserId, "REVISOR"));
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        SetupDefaultMocks();
    }

    #region Rendering Tests

    [Fact(DisplayName = "AutomatedReportList should render page title")]
    public void AutomatedReportList_ShouldRenderPageTitle()
    {
        // Act
        var cut = Render<AutomatedReportList>();

        // Assert
        cut.Markup.Should().Contain("Reportes Automatizados");
    }

    [Fact(DisplayName = "AutomatedReportList should render Plantillas button")]
    public void AutomatedReportList_ShouldRenderPlantillasButton()
    {
        // Act
        var cut = Render<AutomatedReportList>();

        // Assert
        var button = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Plantillas"));
        button.Should().NotBeNull();
    }

    [Fact(DisplayName = "AutomatedReportList should render Generar Reporte button")]
    public void AutomatedReportList_ShouldRenderGenerateButton()
    {
        // Act
        var cut = Render<AutomatedReportList>();

        // Assert
        var button = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Generar Reporte"));
        button.Should().NotBeNull();
    }

    [Fact(DisplayName = "AutomatedReportList should render reports table")]
    public void AutomatedReportList_ShouldRenderReportsTable()
    {
        // Act
        var cut = Render<AutomatedReportList>();

        // Assert
        cut.Markup.Should().Contain("<table");
        cut.Markup.Should().Contain("Período");
        cut.Markup.Should().Contain("Fecha Generación");
        cut.Markup.Should().Contain("Total Reportes");
    }

    [Fact(DisplayName = "AutomatedReportList should display reports from service")]
    public void AutomatedReportList_ShouldDisplayReportsFromService()
    {
        // Act
        var cut = Render<AutomatedReportList>();

        // Assert
        cut.Markup.Should().Contain("badge bg-primary"); // Total reportes badge
    }

    [Fact(DisplayName = "AutomatedReportList should display Enviado badge for sent reports")]
    public void AutomatedReportList_ShouldDisplayEnviadoBadge()
    {
        // Arrange - Setup mock with sent report
        var sentReport = CreateTestReport(1, enviado: true);
        _mockReportService.Setup(s => s.GetReportsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<bool?>()))
            .ReturnsAsync(new List<AutomatedReportListDto> { sentReport });

        // Act
        var cut = Render<AutomatedReportList>();

        // Assert
        cut.Markup.Should().Contain("bg-success");
        cut.Markup.Should().Contain("Enviado");
    }

    [Fact(DisplayName = "AutomatedReportList should display Pendiente badge for unsent reports")]
    public void AutomatedReportList_ShouldDisplayPendienteBadge()
    {
        // Arrange - Setup mock with unsent report
        var unsentReport = CreateTestReport(1, enviado: false);
        _mockReportService.Setup(s => s.GetReportsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<bool?>()))
            .ReturnsAsync(new List<AutomatedReportListDto> { unsentReport });

        // Act
        var cut = Render<AutomatedReportList>();

        // Assert
        cut.Markup.Should().Contain("bg-warning");
        cut.Markup.Should().Contain("Pendiente");
    }

    [Fact(DisplayName = "AutomatedReportList should display Error badge for failed reports")]
    public void AutomatedReportList_ShouldDisplayErrorBadge()
    {
        // Arrange - Setup mock with error report
        var errorReport = CreateTestReport(1, tieneError: true);
        _mockReportService.Setup(s => s.GetReportsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<bool?>()))
            .ReturnsAsync(new List<AutomatedReportListDto> { errorReport });

        // Act
        var cut = Render<AutomatedReportList>();

        // Assert
        cut.Markup.Should().Contain("bg-danger");
        cut.Markup.Should().Contain("Error");
    }

    #endregion

    #region Loading State Tests

    [Fact(DisplayName = "AutomatedReportList should display loading spinner initially")]
    public void AutomatedReportList_ShouldDisplayLoadingSpinner()
    {
        // Arrange - Setup slow response
        _mockReportService.Setup(s => s.GetReportsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<bool?>()))
            .Returns(async () =>
            {
                await Task.Delay(5000);
                return new List<AutomatedReportListDto>();
            });

        // Act
        var cut = Render<AutomatedReportList>();

        // Assert
        cut.Markup.Should().Contain("spinner-border");
        cut.Markup.Should().Contain("Cargando reportes automatizados");
    }

    #endregion

    #region Empty State Tests

    [Fact(DisplayName = "AutomatedReportList should display empty state when no reports")]
    public void AutomatedReportList_ShouldDisplayEmptyState()
    {
        // Arrange
        _mockReportService.Setup(s => s.GetReportsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<bool?>()))
            .ReturnsAsync(new List<AutomatedReportListDto>());

        // Act
        var cut = Render<AutomatedReportList>();

        // Assert
        cut.Markup.Should().Contain("No hay reportes automatizados para mostrar");
    }

    [Fact(DisplayName = "AutomatedReportList empty state should have generate button")]
    public void AutomatedReportList_EmptyState_ShouldHaveGenerateButton()
    {
        // Arrange
        _mockReportService.Setup(s => s.GetReportsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<bool?>()))
            .ReturnsAsync(new List<AutomatedReportListDto>());

        // Act
        var cut = Render<AutomatedReportList>();

        // Assert
        var generateButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Generar Primer Reporte"));
        generateButton.Should().NotBeNull();
    }

    #endregion

    #region Error Handling Tests

    [Fact(DisplayName = "AutomatedReportList should display error when load fails")]
    public void AutomatedReportList_ShouldDisplayErrorOnLoadFailure()
    {
        // Arrange
        _mockReportService.Setup(s => s.GetReportsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<bool?>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var cut = Render<AutomatedReportList>();

        // Assert
        cut.Markup.Should().Contain("alert-danger");
        cut.Markup.Should().Contain("Error al cargar los reportes");
    }

    #endregion

    #region Filter Tests

    [Fact(DisplayName = "AutomatedReportList should render filter controls")]
    public void AutomatedReportList_ShouldRenderFilterControls()
    {
        // Act
        var cut = Render<AutomatedReportList>();

        // Assert
        cut.Markup.Should().Contain("Desde");
        cut.Markup.Should().Contain("Hasta");
        cut.Markup.Should().Contain("Estado de Envío");
        cut.Markup.Should().Contain("Limpiar");
    }

    [Fact(DisplayName = "AutomatedReportList clear button should reset filters")]
    public async Task AutomatedReportList_ClearButton_ShouldResetFilters()
    {
        // Arrange
        var cut = Render<AutomatedReportList>();

        // Act
        var clearButton = cut.FindAll("button").First(b => b.TextContent.Contains("Limpiar"));
        await cut.InvokeAsync(() => clearButton.Click());

        // Assert - Service should be called again with null filters
        _mockReportService.Verify(
            s => s.GetReportsAsync(0, 50, null, null, null),
            Times.AtLeast(2)); // Once on init, once on clear
    }

    #endregion

    #region Navigation Tests

    [Fact(DisplayName = "AutomatedReportList Plantillas button should navigate to templates")]
    public async Task AutomatedReportList_PlantillasButton_ShouldNavigate()
    {
        // Arrange
        var cut = Render<AutomatedReportList>();

        // Act
        var plantillasButton = cut.FindAll("button").First(b => b.TextContent.Contains("Plantillas"));
        await cut.InvokeAsync(() => plantillasButton.Click());

        // Assert
        _navigationManager.Uri.Should().Contain("/automated/templates");
    }

    [Fact(DisplayName = "AutomatedReportList view button should navigate to detail")]
    public async Task AutomatedReportList_ViewButton_ShouldNavigateToDetail()
    {
        // Arrange
        var cut = Render<AutomatedReportList>();

        // Wait for table to render with rows (reports loaded)
        cut.WaitForAssertion(() => cut.FindAll("table tbody tr").Count.Should().BeGreaterThan(0));

        // Debug: output the rows to understand what's rendered
        var rows = cut.FindAll("table tbody tr");
        var buttonsInTable = cut.FindAll("table tbody button");

        // Act - Find the first view button (it has title="Ver detalle" and icon bi-eye)
        var viewButtons = cut.FindAll("button[title='Ver detalle']");
        viewButtons.Should().NotBeEmpty("View buttons should exist in the table");

        var viewButton = viewButtons.First();

        // Record the initial URI
        var initialUri = _navigationManager.Uri;

        await cut.InvokeAsync(() => viewButton.Click());

        // Assert - the URI should change to contain /automated/reports/
        _navigationManager.Uri.Should().NotBe(initialUri, "Navigation should have occurred");
        _navigationManager.Uri.Should().Contain("/automated/reports/");
    }

    #endregion

    #region Generate Modal Tests

    [Fact(DisplayName = "AutomatedReportList generate button should open modal")]
    public async Task AutomatedReportList_GenerateButton_ShouldOpenModal()
    {
        // Arrange
        var cut = Render<AutomatedReportList>();

        // Act
        var generateButton = cut.FindAll("button.btn-primary").First(b => b.TextContent.Contains("Generar Reporte"));
        await cut.InvokeAsync(() => generateButton.Click());

        // Assert
        cut.Markup.Should().Contain("Generar Reporte Automatizado");
        cut.Markup.Should().Contain("modal");
    }

    [Fact(DisplayName = "AutomatedReportList modal cancel should close modal")]
    public async Task AutomatedReportList_ModalCancel_ShouldCloseModal()
    {
        // Arrange
        var cut = Render<AutomatedReportList>();

        // Open modal
        var generateButton = cut.FindAll("button.btn-primary").First(b => b.TextContent.Contains("Generar Reporte"));
        await cut.InvokeAsync(() => generateButton.Click());

        // Act
        var cancelButton = cut.FindAll("button.btn-secondary").First(b => b.TextContent.Contains("Cancelar"));
        await cut.InvokeAsync(() => cancelButton.Click());

        // Assert - Modal should be closed
        cut.Markup.Should().NotContain("Generar Reporte Automatizado");
    }

    [Fact(DisplayName = "AutomatedReportList modal should have date inputs")]
    public async Task AutomatedReportList_Modal_ShouldHaveDateInputs()
    {
        // Arrange
        var cut = Render<AutomatedReportList>();

        // Open modal
        var generateButton = cut.FindAll("button.btn-primary").First(b => b.TextContent.Contains("Generar Reporte"));
        await cut.InvokeAsync(() => generateButton.Click());

        // Assert
        cut.Markup.Should().Contain("Fecha Inicio");
        cut.Markup.Should().Contain("Fecha Fin");
        cut.Markup.Should().Contain("type=\"date\"");
    }

    [Fact(DisplayName = "AutomatedReportList modal should have send email checkbox")]
    public async Task AutomatedReportList_Modal_ShouldHaveSendEmailCheckbox()
    {
        // Arrange
        var cut = Render<AutomatedReportList>();

        // Open modal
        var generateButton = cut.FindAll("button.btn-primary").First(b => b.TextContent.Contains("Generar Reporte"));
        await cut.InvokeAsync(() => generateButton.Click());

        // Assert
        cut.Markup.Should().Contain("Enviar por email");
        cut.Markup.Should().Contain("type=\"checkbox\"");
    }

    #endregion

    #region Report Actions Tests

    [Fact(DisplayName = "AutomatedReportList should show send button for unsent reports")]
    public void AutomatedReportList_UnsentReport_ShouldShowSendButton()
    {
        // Arrange
        var unsentReport = CreateTestReport(1, enviado: false);
        _mockReportService.Setup(s => s.GetReportsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<bool?>()))
            .ReturnsAsync(new List<AutomatedReportListDto> { unsentReport });

        // Act
        var cut = Render<AutomatedReportList>();

        // Assert
        var sendButton = cut.FindAll("button.btn-outline-success").FirstOrDefault();
        sendButton.Should().NotBeNull();
    }

    [Fact(DisplayName = "AutomatedReportList should NOT show send button for sent reports")]
    public void AutomatedReportList_SentReport_ShouldNotShowSendButton()
    {
        // Arrange
        var sentReport = CreateTestReport(1, enviado: true);
        _mockReportService.Setup(s => s.GetReportsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<bool?>()))
            .ReturnsAsync(new List<AutomatedReportListDto> { sentReport });

        // Act
        var cut = Render<AutomatedReportList>();

        // Assert
        var sendButton = cut.FindAll("button.btn-outline-success").FirstOrDefault();
        sendButton.Should().BeNull();
    }

    [Fact(DisplayName = "AutomatedReportList delete button should call service")]
    public async Task AutomatedReportList_DeleteButton_ShouldCallService()
    {
        // Arrange
        var report = CreateTestReport(1);
        _mockReportService.Setup(s => s.GetReportsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<bool?>()))
            .ReturnsAsync(new List<AutomatedReportListDto> { report });
        _mockReportService.Setup(s => s.DeleteReportAsync(It.IsAny<int>()))
            .ReturnsAsync(true);

        var cut = Render<AutomatedReportList>();

        // Act
        var deleteButton = cut.FindAll("button.btn-outline-danger").FirstOrDefault();
        if (deleteButton != null)
        {
            await cut.InvokeAsync(() => deleteButton.Click());
        }

        // Assert
        _mockReportService.Verify(s => s.DeleteReportAsync(1), Times.Once);
    }

    #endregion

    #region Helper Methods

    private void SetupDefaultMocks()
    {
        var reports = new List<AutomatedReportListDto>
        {
            CreateTestReport(1, enviado: true),
            CreateTestReport(2, enviado: false)
        };

        var templates = new List<ReportTemplateListDto>
        {
            new() { Id = 1, Nombre = "Plantilla Default" }
        };

        _mockReportService.Setup(s => s.GetReportsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<bool?>()))
            .ReturnsAsync(reports);

        _mockReportService.Setup(s => s.GetTemplatesAsync())
            .ReturnsAsync(templates);
    }

    private static AutomatedReportListDto CreateTestReport(int id, bool enviado = false, bool tieneError = false)
    {
        return new AutomatedReportListDto
        {
            Id = id,
            FechaInicio = DateTime.UtcNow.AddDays(-1),
            FechaFin = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            TotalReportes = 15,
            Enviado = enviado,
            TieneError = tieneError,
            FechaEnvio = enviado ? DateTime.UtcNow : null,
            NombreModelo = "Default"
        };
    }

    private class FakeNavigationManager : NavigationManager
    {
        public FakeNavigationManager()
        {
            Initialize("https://localhost:5001/", "https://localhost:5001/automated");
        }

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
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
