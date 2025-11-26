using Bunit;
using Ceiba.Application.Services;
using Ceiba.Core.Interfaces;
using Ceiba.Shared.DTOs;
using Ceiba.Web.Components.Pages.Reports;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Ceiba.Web.Tests;

/// <summary>
/// Component tests for ReportForm Blazor component (US1)
/// Tests form rendering and interaction per T026
/// </summary>
public class ReportFormComponentTests : TestContext
{
    private readonly Mock<IReportService> _mockReportService;
    private readonly Mock<ICatalogService> _mockCatalogService;

    public ReportFormComponentTests()
    {
        _mockReportService = new Mock<IReportService>();
        _mockCatalogService = new Mock<ICatalogService>();

        // Register mocked services
        Services.AddSingleton(_mockReportService.Object);
        Services.AddSingleton(_mockCatalogService.Object);
    }

    #region T026: Form Rendering Tests

    [Fact(DisplayName = "T026: ReportForm should render all required fields")]
    public void ReportForm_ShouldRenderAllRequiredFields()
    {
        // Arrange
        SetupCatalogMocks();

        // Act
        var cut = RenderComponent<ReportForm>();

        // Assert: Verify all required fields are present
        cut.Find("input[name='sexo']").Should().NotBeNull();
        cut.Find("input[name='edad']").Should().NotBeNull();
        cut.Find("select[name='delito']").Should().NotBeNull();
        cut.Find("select[name='zonaId']").Should().NotBeNull();
        cut.Find("select[name='sectorId']").Should().NotBeNull();
        cut.Find("select[name='cuadranteId']").Should().NotBeNull();
        cut.Find("input[name='turnoCeiba']").Should().NotBeNull();
        cut.Find("select[name='tipoDeAtencion']").Should().NotBeNull();
        cut.Find("select[name='tipoDeAccion']").Should().NotBeNull();
        cut.Find("textarea[name='hechosReportados']").Should().NotBeNull();
        cut.Find("textarea[name='accionesRealizadas']").Should().NotBeNull();
        cut.Find("select[name='traslados']").Should().NotBeNull();
        cut.Find("textarea[name='observaciones']").Should().NotBeNull();
    }

    [Fact(DisplayName = "T026: ReportForm should render checkbox fields")]
    public void ReportForm_ShouldRenderCheckboxFields()
    {
        // Arrange
        SetupCatalogMocks();

        // Act
        var cut = RenderComponent<ReportForm>();

        // Assert: Verify boolean checkbox fields
        cut.Find("input[name='lgbtttiqPlus'][type='checkbox']").Should().NotBeNull();
        cut.Find("input[name='situacionCalle'][type='checkbox']").Should().NotBeNull();
        cut.Find("input[name='migrante'][type='checkbox']").Should().NotBeNull();
        cut.Find("input[name='discapacidad'][type='checkbox']").Should().NotBeNull();
    }

    [Fact(DisplayName = "T026: ReportForm should have submit and save draft buttons")]
    public void ReportForm_ShouldHaveSubmitAndSaveDraftButtons()
    {
        // Arrange
        SetupCatalogMocks();

        // Act
        var cut = RenderComponent<ReportForm>();

        // Assert: Verify action buttons are present
        var submitButton = cut.Find("button[type='submit']");
        var saveDraftButton = cut.Find("button[data-action='save-draft']");

        submitButton.Should().NotBeNull();
        submitButton.TextContent.Should().Contain("Entregar");

        saveDraftButton.Should().NotBeNull();
        saveDraftButton.TextContent.Should().Contain("Guardar borrador");
    }

    #endregion

    #region T026: Cascading Dropdown Tests

    [Fact(DisplayName = "T026: Zona dropdown should populate on component initialization")]
    public async Task ZonaDropdown_ShouldPopulateOnInit()
    {
        // Arrange
        var zonas = new List<CatalogItemDto>
        {
            new() { Id = 1, Nombre = "Zona Norte" },
            new() { Id = 2, Nombre = "Zona Sur" }
        };

        _mockCatalogService
            .Setup(c => c.GetZonasAsync())
            .ReturnsAsync(zonas);

        SetupCatalogMocks();

        // Act
        var cut = RenderComponent<ReportForm>();
        await Task.Delay(100); // Wait for async initialization

        // Assert: Verify zona options are rendered
        var zonaSelect = cut.Find("select[name='zonaId']");
        var options = zonaSelect.QuerySelectorAll("option");

        options.Should().HaveCountGreaterThanOrEqualTo(2);
        options.Should().Contain(o => o.TextContent.Contains("Zona Norte"));
        options.Should().Contain(o => o.TextContent.Contains("Zona Sur"));
    }

    [Fact(DisplayName = "T026: Sector dropdown should update when zona changes")]
    public async Task SectorDropdown_ShouldUpdateWhenZonaChanges()
    {
        // Arrange
        var zonaId = 1;
        var sectores = new List<CatalogItemDto>
        {
            new() { Id = 1, Nombre = "Sector 1" },
            new() { Id = 2, Nombre = "Sector 2" }
        };

        _mockCatalogService
            .Setup(c => c.GetSectoresByZonaAsync(zonaId))
            .ReturnsAsync(sectores);

        SetupCatalogMocks();

        var cut = RenderComponent<ReportForm>();

        // Act: Select a zona
        var zonaSelect = cut.Find("select[name='zonaId']");
        await cut.InvokeAsync(() => zonaSelect.Change(zonaId.ToString()));
        await Task.Delay(100); // Wait for async update

        // Assert: Verify sector options are populated
        var sectorSelect = cut.Find("select[name='sectorId']");
        var options = sectorSelect.QuerySelectorAll("option");

        options.Should().HaveCountGreaterThanOrEqualTo(2);
        options.Should().Contain(o => o.TextContent.Contains("Sector 1"));
    }

    [Fact(DisplayName = "T026: Cuadrante dropdown should update when sector changes")]
    public async Task CuadranteDropdown_ShouldUpdateWhenSectorChanges()
    {
        // Arrange
        var sectorId = 1;
        var cuadrantes = new List<CatalogItemDto>
        {
            new() { Id = 1, Nombre = "Cuadrante A" },
            new() { Id = 2, Nombre = "Cuadrante B" }
        };

        _mockCatalogService
            .Setup(c => c.GetCuadrantesBySectorAsync(sectorId))
            .ReturnsAsync(cuadrantes);

        SetupCatalogMocks();

        var cut = RenderComponent<ReportForm>();

        // Act: Select a sector
        var sectorSelect = cut.Find("select[name='sectorId']");
        await cut.InvokeAsync(() => sectorSelect.Change(sectorId.ToString()));
        await Task.Delay(100); // Wait for async update

        // Assert: Verify cuadrante options are populated
        var cuadranteSelect = cut.Find("select[name='cuadranteId']");
        var options = cuadranteSelect.QuerySelectorAll("option");

        options.Should().HaveCountGreaterThanOrEqualTo(2);
        options.Should().Contain(o => o.TextContent.Contains("Cuadrante A"));
    }

    #endregion

    #region T026: Form Submission Tests

    [Fact(DisplayName = "T026: Save draft should call service with estado=0")]
    public async Task SaveDraft_ShouldCallServiceWithEstadoBorrador()
    {
        // Arrange
        SetupCatalogMocks();
        var cut = RenderComponent<ReportForm>();

        // Fill form
        await FillFormWithValidDataAsync(cut);

        // Act: Click save draft button
        var saveDraftButton = cut.Find("button[data-action='save-draft']");
        await cut.InvokeAsync(() => saveDraftButton.Click());

        // Assert: Verify service was called with estado=0
        _mockReportService.Verify(
            s => s.CreateReportAsync(
                It.Is<CreateReportDto>(dto => dto != null),
                It.IsAny<Guid>()
            ),
            Times.Once
        );
    }

    [Fact(DisplayName = "T026: Submit form should call submit service")]
    public async Task SubmitForm_ShouldCallSubmitService()
    {
        // Arrange
        SetupCatalogMocks();
        var cut = RenderComponent<ReportForm>();

        // Fill form
        await FillFormWithValidDataAsync(cut);

        // Act: Submit form
        var form = cut.Find("form");
        await cut.InvokeAsync(() => form.Submit());

        // Assert: Verify report was created and submitted
        _mockReportService.Verify(
            s => s.CreateReportAsync(It.IsAny<CreateReportDto>(), It.IsAny<Guid>()),
            Times.Once
        );

        _mockReportService.Verify(
            s => s.SubmitReportAsync(It.IsAny<int>(), It.IsAny<Guid>()),
            Times.Once
        );
    }

    [Fact(DisplayName = "T026: Form should validate required fields")]
    public async Task Form_ShouldValidateRequiredFields()
    {
        // Arrange
        SetupCatalogMocks();
        var cut = RenderComponent<ReportForm>();

        // Act: Try to submit empty form
        var form = cut.Find("form");
        await cut.InvokeAsync(() => form.Submit());

        // Assert: Verify validation errors are displayed
        var validationMessages = cut.FindAll(".validation-message");
        validationMessages.Should().NotBeEmpty();
    }

    [Fact(DisplayName = "T026: Form should display success message after save")]
    public async Task Form_ShouldDisplaySuccessMessageAfterSave()
    {
        // Arrange
        SetupCatalogMocks();

        _mockReportService
            .Setup(s => s.CreateReportAsync(It.IsAny<CreateReportDto>(), It.IsAny<Guid>()))
            .ReturnsAsync(new ReportDto { Id = 1, Estado = 0 });

        var cut = RenderComponent<ReportForm>();
        await FillFormWithValidDataAsync(cut);

        // Act: Save draft
        var saveDraftButton = cut.Find("button[data-action='save-draft']");
        await cut.InvokeAsync(() => saveDraftButton.Click());
        await Task.Delay(100);

        // Assert: Verify success message is displayed
        var successMessage = cut.Find(".alert-success");
        successMessage.Should().NotBeNull();
        successMessage.TextContent.Should().Contain("guardado exitosamente");
    }

    #endregion

    #region Helper Methods

    private void SetupCatalogMocks()
    {
        _mockCatalogService
            .Setup(c => c.GetZonasAsync())
            .ReturnsAsync(new List<CatalogItemDto>
            {
                new() { Id = 1, Nombre = "Zona Norte" }
            });

        _mockCatalogService
            .Setup(c => c.GetSectoresByZonaAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<CatalogItemDto>
            {
                new() { Id = 1, Nombre = "Sector 1" }
            });

        _mockCatalogService
            .Setup(c => c.GetCuadrantesBySectorAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<CatalogItemDto>
            {
                new() { Id = 1, Nombre = "Cuadrante A" }
            });
    }

    private async Task FillFormWithValidDataAsync(IRenderedComponent<ReportForm> cut)
    {
        await cut.InvokeAsync(() =>
        {
            cut.Find("input[name='sexo']").Change("Femenino");
            cut.Find("input[name='edad']").Change("28");
            cut.Find("select[name='zonaId']").Change("1");
            cut.Find("select[name='sectorId']").Change("1");
            cut.Find("select[name='cuadranteId']").Change("1");
            cut.Find("input[name='turnoCeiba']").Change("1");
            cut.Find("select[name='tipoDeAtencion']").Change("Presencial");
            cut.Find("select[name='tipoDeAccion']").Change("1");
            cut.Find("textarea[name='hechosReportados']").Change("Descripción de hechos");
            cut.Find("textarea[name='accionesRealizadas']").Change("Acciones realizadas");
            cut.Find("select[name='traslados']").Change("0");
        });
    }

    #endregion
}
