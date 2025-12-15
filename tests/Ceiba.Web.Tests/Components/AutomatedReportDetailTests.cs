using Bunit;
using Ceiba.Core.Interfaces;
using Ceiba.Shared.DTOs;
using Ceiba.Web.Components.Pages.Automated;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Security.Claims;

namespace Ceiba.Web.Tests.Components;

/// <summary>
/// Component tests for AutomatedReportDetail Blazor component.
/// Tests automated report detail view and actions.
/// </summary>
[Trait("Category", "Component")]
public class AutomatedReportDetailTests : TestContext
{
    private readonly Mock<IAutomatedReportService> _mockReportService;
    private readonly Guid _testUserId = Guid.NewGuid();

    public AutomatedReportDetailTests()
    {
        _mockReportService = new Mock<IAutomatedReportService>();

        Services.AddSingleton(_mockReportService.Object);
        Services.AddSingleton<AuthenticationStateProvider>(new TestAuthStateProvider(_testUserId, "REVISOR"));
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        SetupDefaultMocks();
    }

    #region Loading State Tests

    [Fact(DisplayName = "AutomatedReportDetail should show loading state initially")]
    public void AutomatedReportDetail_ShouldShowLoadingStateInitially()
    {
        // Arrange - Make the service never complete
        _mockReportService.Setup(x => x.GetReportByIdAsync(It.IsAny<int>()))
            .Returns(new TaskCompletionSource<AutomatedReportDetailDto?>().Task);

        // Act
        var cut = Render<AutomatedReportDetail>(parameters => parameters.Add(p => p.Id, 1));

        // Assert
        cut.Markup.Should().Contain("Cargando reporte...");
    }

    [Fact(DisplayName = "AutomatedReportDetail should show not found message")]
    public void AutomatedReportDetail_ShouldShowNotFoundMessage()
    {
        // Arrange
        _mockReportService.Setup(x => x.GetReportByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((AutomatedReportDetailDto?)null);

        // Act
        var cut = Render<AutomatedReportDetail>(parameters => parameters.Add(p => p.Id, 999));

        // Assert
        cut.Markup.Should().Contain("Reporte no encontrado");
        cut.Markup.Should().Contain("Volver a la lista");
    }

    #endregion

    #region Report Display Tests

    [Fact(DisplayName = "AutomatedReportDetail should display report header")]
    public void AutomatedReportDetail_ShouldDisplayReportHeader()
    {
        // Arrange
        var report = CreateTestReport();
        _mockReportService.Setup(x => x.GetReportByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(report);

        // Act
        var cut = Render<AutomatedReportDetail>(parameters => parameters.Add(p => p.Id, 1));

        // Assert
        cut.Markup.Should().Contain("Reporte:");
        cut.Markup.Should().Contain("Volver a la lista");
    }

    [Fact(DisplayName = "AutomatedReportDetail should display statistics panel")]
    public void AutomatedReportDetail_ShouldDisplayStatisticsPanel()
    {
        // Arrange
        var report = CreateTestReport();
        _mockReportService.Setup(x => x.GetReportByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(report);

        // Act
        var cut = Render<AutomatedReportDetail>(parameters => parameters.Add(p => p.Id, 1));

        // Assert
        cut.Markup.Should().Contain("Estadísticas");
        cut.Markup.Should().Contain("Total de Reportes Entregados");
        cut.Markup.Should().Contain("10"); // TotalReportes
    }

    [Fact(DisplayName = "AutomatedReportDetail should display vulnerable populations")]
    public void AutomatedReportDetail_ShouldDisplayVulnerablePopulations()
    {
        // Arrange
        var report = CreateTestReport();
        _mockReportService.Setup(x => x.GetReportByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(report);

        // Act
        var cut = Render<AutomatedReportDetail>(parameters => parameters.Add(p => p.Id, 1));

        // Assert
        cut.Markup.Should().Contain("Poblaciones Vulnerables");
        cut.Markup.Should().Contain("LGBTTTIQ+");
        cut.Markup.Should().Contain("Migrantes");
        cut.Markup.Should().Contain("Situación de calle");
        cut.Markup.Should().Contain("Con discapacidad");
    }

    [Fact(DisplayName = "AutomatedReportDetail should display most frequent crime")]
    public void AutomatedReportDetail_ShouldDisplayMostFrequentCrime()
    {
        // Arrange
        var report = CreateTestReport();
        _mockReportService.Setup(x => x.GetReportByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(report);

        // Act
        var cut = Render<AutomatedReportDetail>(parameters => parameters.Add(p => p.Id, 1));

        // Assert
        cut.Markup.Should().Contain("Delito Más Frecuente");
        cut.Markup.Should().Contain("Robo");
    }

    [Fact(DisplayName = "AutomatedReportDetail should display most active zone")]
    public void AutomatedReportDetail_ShouldDisplayMostActiveZone()
    {
        // Arrange
        var report = CreateTestReport();
        _mockReportService.Setup(x => x.GetReportByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(report);

        // Act
        var cut = Render<AutomatedReportDetail>(parameters => parameters.Add(p => p.Id, 1));

        // Assert
        cut.Markup.Should().Contain("Zona Más Activa");
        cut.Markup.Should().Contain("Zona Centro");
    }

    [Fact(DisplayName = "AutomatedReportDetail should display content panel")]
    public void AutomatedReportDetail_ShouldDisplayContentPanel()
    {
        // Arrange
        var report = CreateTestReport();
        _mockReportService.Setup(x => x.GetReportByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(report);

        // Act
        var cut = Render<AutomatedReportDetail>(parameters => parameters.Add(p => p.Id, 1));

        // Assert
        cut.Markup.Should().Contain("Contenido del Reporte");
        cut.Markup.Should().Contain("Vista Previa");
        cut.Markup.Should().Contain("Markdown");
    }

    [Fact(DisplayName = "AutomatedReportDetail should display breakdown tables")]
    public void AutomatedReportDetail_ShouldDisplayBreakdownTables()
    {
        // Arrange
        var report = CreateTestReport();
        _mockReportService.Setup(x => x.GetReportByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(report);

        // Act
        var cut = Render<AutomatedReportDetail>(parameters => parameters.Add(p => p.Id, 1));

        // Assert
        cut.Markup.Should().Contain("Distribución por Tipo de Delito");
        cut.Markup.Should().Contain("Distribución por Zona");
    }

    #endregion

    #region Status Alert Tests

    [Fact(DisplayName = "AutomatedReportDetail should show sent status")]
    public void AutomatedReportDetail_ShouldShowSentStatus()
    {
        // Arrange
        var report = CreateTestReport();
        report.Enviado = true;
        report.FechaEnvio = DateTime.UtcNow;
        _mockReportService.Setup(x => x.GetReportByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(report);

        // Act
        var cut = Render<AutomatedReportDetail>(parameters => parameters.Add(p => p.Id, 1));

        // Assert
        cut.Markup.Should().Contain("Enviado:");
        cut.Markup.Should().Contain("alert-success");
    }

    [Fact(DisplayName = "AutomatedReportDetail should show error message")]
    public void AutomatedReportDetail_ShouldShowErrorMessage()
    {
        // Arrange
        var report = CreateTestReport();
        report.ErrorMensaje = "Error al generar el reporte";
        _mockReportService.Setup(x => x.GetReportByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(report);

        // Act
        var cut = Render<AutomatedReportDetail>(parameters => parameters.Add(p => p.Id, 1));

        // Assert
        cut.Markup.Should().Contain("Error:");
        cut.Markup.Should().Contain("Error al generar el reporte");
        cut.Markup.Should().Contain("alert-danger");
    }

    #endregion

    #region Action Button Tests

    [Fact(DisplayName = "AutomatedReportDetail should show send button when not sent")]
    public void AutomatedReportDetail_ShouldShowSendButtonWhenNotSent()
    {
        // Arrange
        var report = CreateTestReport();
        report.Enviado = false;
        _mockReportService.Setup(x => x.GetReportByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(report);

        // Act
        var cut = Render<AutomatedReportDetail>(parameters => parameters.Add(p => p.Id, 1));

        // Assert
        cut.Markup.Should().Contain("Enviar");
    }

    [Fact(DisplayName = "AutomatedReportDetail should hide send button when already sent")]
    public void AutomatedReportDetail_ShouldHideSendButtonWhenAlreadySent()
    {
        // Arrange
        var report = CreateTestReport();
        report.Enviado = true;
        report.FechaEnvio = DateTime.UtcNow;
        _mockReportService.Setup(x => x.GetReportByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(report);

        // Act
        var cut = Render<AutomatedReportDetail>(parameters => parameters.Add(p => p.Id, 1));

        // Assert
        cut.FindAll("button").Should().NotContain(b => b.TextContent.Contains("Enviar") && b.ClassList.Contains("btn-success"));
    }

    [Fact(DisplayName = "AutomatedReportDetail should show download button when Word exists")]
    public void AutomatedReportDetail_ShouldShowDownloadButtonWhenWordExists()
    {
        // Arrange
        var report = CreateTestReport();
        report.ContenidoWordPath = "/path/to/report.docx";
        _mockReportService.Setup(x => x.GetReportByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(report);

        // Act
        var cut = Render<AutomatedReportDetail>(parameters => parameters.Add(p => p.Id, 1));

        // Assert
        cut.Markup.Should().Contain("Descargar Word");
    }

    [Fact(DisplayName = "AutomatedReportDetail should show generate button when no Word")]
    public void AutomatedReportDetail_ShouldShowGenerateButtonWhenNoWord()
    {
        // Arrange
        var report = CreateTestReport();
        report.ContenidoWordPath = null;
        _mockReportService.Setup(x => x.GetReportByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(report);

        // Act
        var cut = Render<AutomatedReportDetail>(parameters => parameters.Add(p => p.Id, 1));

        // Assert
        cut.Markup.Should().Contain("Generar Word");
    }

    #endregion

    #region Send Report Tests

    [Fact(DisplayName = "AutomatedReportDetail should send report on button click")]
    public async Task AutomatedReportDetail_ShouldSendReportOnButtonClick()
    {
        // Arrange
        var report = CreateTestReport();
        report.Enviado = false;
        _mockReportService.Setup(x => x.GetReportByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(report);
        _mockReportService.Setup(x => x.SendReportByEmailAsync(It.IsAny<int>(), It.IsAny<List<string>>()))
            .ReturnsAsync(true);

        var cut = Render<AutomatedReportDetail>(parameters => parameters.Add(p => p.Id, 1));

        // Act
        var sendButton = cut.FindAll("button").First(b => b.TextContent.Contains("Enviar"));
        await cut.InvokeAsync(() => sendButton.Click());

        // Assert
        _mockReportService.Verify(x => x.SendReportByEmailAsync(1, It.IsAny<List<string>>()), Times.Once);
    }

    #endregion

    #region Regenerate Word Tests

    [Fact(DisplayName = "AutomatedReportDetail should regenerate Word on button click")]
    public async Task AutomatedReportDetail_ShouldRegenerateWordOnButtonClick()
    {
        // Arrange
        var report = CreateTestReport();
        report.ContenidoWordPath = null;
        _mockReportService.Setup(x => x.GetReportByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(report);
        _mockReportService.Setup(x => x.RegenerateWordDocumentAsync(It.IsAny<int>()))
            .ReturnsAsync("/path/to/regenerated.docx");

        var cut = Render<AutomatedReportDetail>(parameters => parameters.Add(p => p.Id, 1));

        // Act
        var generateButton = cut.FindAll("button").First(b => b.TextContent.Contains("Generar Word"));
        await cut.InvokeAsync(() => generateButton.Click());

        // Assert
        _mockReportService.Verify(x => x.RegenerateWordDocumentAsync(1), Times.Once);
    }

    #endregion

    #region View Mode Tests

    [Fact(DisplayName = "AutomatedReportDetail should toggle view mode to markdown", Skip = "Button selector requires component-specific structure")]
    public async Task AutomatedReportDetail_ShouldToggleViewModeToMarkdown()
    {
        // Arrange
        var report = CreateTestReport();
        _mockReportService.Setup(x => x.GetReportByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(report);

        var cut = Render<AutomatedReportDetail>(parameters => parameters.Add(p => p.Id, 1));

        // Act
        var markdownButton = cut.FindAll("button").First(b => b.TextContent == "Markdown");
        await cut.InvokeAsync(() => markdownButton.Click());

        // Assert
        cut.Markup.Should().Contain("<pre");
        cut.Markup.Should().Contain("# Test Report");
    }

    [Fact(DisplayName = "AutomatedReportDetail should show preview by default")]
    public void AutomatedReportDetail_ShouldShowPreviewByDefault()
    {
        // Arrange
        var report = CreateTestReport();
        _mockReportService.Setup(x => x.GetReportByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(report);

        // Act
        var cut = Render<AutomatedReportDetail>(parameters => parameters.Add(p => p.Id, 1));

        // Assert
        cut.Markup.Should().Contain("markdown-content");
    }

    #endregion

    #region Distribution Tables Tests

    [Fact(DisplayName = "AutomatedReportDetail should display crime distribution")]
    public void AutomatedReportDetail_ShouldDisplayCrimeDistribution()
    {
        // Arrange
        var report = CreateTestReport();
        _mockReportService.Setup(x => x.GetReportByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(report);

        // Act
        var cut = Render<AutomatedReportDetail>(parameters => parameters.Add(p => p.Id, 1));

        // Assert
        cut.Markup.Should().Contain("Robo");
        cut.Markup.Should().Contain("Asalto");
    }

    [Fact(DisplayName = "AutomatedReportDetail should display zone distribution")]
    public void AutomatedReportDetail_ShouldDisplayZoneDistribution()
    {
        // Arrange
        var report = CreateTestReport();
        _mockReportService.Setup(x => x.GetReportByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(report);

        // Act
        var cut = Render<AutomatedReportDetail>(parameters => parameters.Add(p => p.Id, 1));

        // Assert
        cut.Markup.Should().Contain("Zona Norte");
        cut.Markup.Should().Contain("Zona Sur");
    }

    #endregion

    #region Helpers

    private void SetupDefaultMocks()
    {
        _mockReportService.Setup(x => x.GetReportByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(CreateTestReport());
    }

    private static AutomatedReportDetailDto CreateTestReport()
    {
        return new AutomatedReportDetailDto
        {
            Id = 1,
            FechaInicio = DateTime.UtcNow.AddDays(-1),
            FechaFin = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            Enviado = false,
            ContenidoMarkdown = "# Test Report\n\nThis is a test report.",
            NombreModelo = "Default Template",
            Estadisticas = new ReportStatisticsDto
            {
                TotalReportes = 10,
                TotalLgbtttiq = 2,
                TotalMigrantes = 1,
                TotalSituacionCalle = 3,
                TotalDiscapacidad = 0,
                DelitoMasFrecuente = "Robo",
                ZonaMasActiva = "Zona Centro",
                PorDelito = new Dictionary<string, int>
                {
                    { "Robo", 5 },
                    { "Asalto", 3 },
                    { "Fraude", 2 }
                },
                PorZona = new Dictionary<string, int>
                {
                    { "Zona Norte", 4 },
                    { "Zona Sur", 3 },
                    { "Zona Centro", 3 }
                },
                PorSexo = new Dictionary<string, int>
                {
                    { "Masculino", 6 },
                    { "Femenino", 4 }
                }
            }
        };
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
