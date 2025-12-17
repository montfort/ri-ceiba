using Ceiba.Core.Entities;
using Ceiba.Core.Interfaces;
using Ceiba.Infrastructure.Data;
using Ceiba.Infrastructure.Services;
using Ceiba.Shared.DTOs;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Ceiba.Infrastructure.Tests.Services;

/// <summary>
/// Unit tests for AutomatedReportService.
/// Tests automated report generation, template management, and statistics calculation.
/// </summary>
public class AutomatedReportServiceTests : IDisposable
{
    private readonly CeibaDbContext _context;
    private readonly IAiNarrativeService _aiService;
    private readonly IEmailService _emailService;
    private readonly IAuditService _auditService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AutomatedReportService> _logger;
    private readonly AutomatedReportService _service;
    private readonly Guid _testUserId = Guid.NewGuid();

    public AutomatedReportServiceTests()
    {
        var options = new DbContextOptionsBuilder<CeibaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CeibaDbContext(options, _testUserId);

        _aiService = Substitute.For<IAiNarrativeService>();
        _emailService = Substitute.For<IEmailService>();
        _auditService = Substitute.For<IAuditService>();
        _logger = Substitute.For<ILogger<AutomatedReportService>>();

        var configData = new Dictionary<string, string?>
        {
            ["AutomatedReports:GenerationTime"] = "06:00:00",
            ["AutomatedReports:Recipients:0"] = "test@example.com",
            ["AutomatedReports:Enabled"] = "true",
            ["AutomatedReports:OutputPath"] = "/tmp"
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _service = new AutomatedReportService(
            _context,
            _aiService,
            _emailService,
            _auditService,
            _configuration,
            _logger);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region GetReportsAsync Tests

    [Fact(DisplayName = "GetReportsAsync should return paginated reports")]
    public async Task GetReportsAsync_ReturnsPaginatedReports()
    {
        // Arrange
        await SeedAutomatedReports(10);

        // Act
        var result = await _service.GetReportsAsync(skip: 0, take: 5);

        // Assert
        result.Should().HaveCount(5);
    }

    [Fact(DisplayName = "GetReportsAsync should filter by date range")]
    public async Task GetReportsAsync_FiltersByDateRange()
    {
        // Arrange
        await SeedAutomatedReports(5);
        var fechaDesde = DateTime.UtcNow.AddDays(-3);
        var fechaHasta = DateTime.UtcNow.AddDays(1);

        // Act
        var result = await _service.GetReportsAsync(fechaDesde: fechaDesde, fechaHasta: fechaHasta);

        // Assert
        result.Should().NotBeEmpty();
    }

    [Fact(DisplayName = "GetReportsAsync should filter by enviado status")]
    public async Task GetReportsAsync_FiltersByEnviadoStatus()
    {
        // Arrange
        await SeedAutomatedReports(5);

        // Act
        var sentReports = await _service.GetReportsAsync(enviado: true);
        var unsentReports = await _service.GetReportsAsync(enviado: false);

        // Assert
        sentReports.Should().OnlyContain(r => r.Enviado);
        unsentReports.Should().OnlyContain(r => !r.Enviado);
    }

    [Fact(DisplayName = "GetReportsAsync should return empty list when no reports exist")]
    public async Task GetReportsAsync_ReturnsEmptyList_WhenNoReports()
    {
        // Act
        var result = await _service.GetReportsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact(DisplayName = "GetReportsAsync should order by CreatedAt descending")]
    public async Task GetReportsAsync_OrdersByCreatedAtDescending()
    {
        // Arrange
        await SeedAutomatedReports(5);

        // Act
        var result = await _service.GetReportsAsync();

        // Assert
        result.Should().BeInDescendingOrder(r => r.CreatedAt);
    }

    #endregion

    #region GetReportByIdAsync Tests

    [Fact(DisplayName = "GetReportByIdAsync should return report when found")]
    public async Task GetReportByIdAsync_ReturnsReport_WhenFound()
    {
        // Arrange
        var report = await CreateTestReport();

        // Act
        var result = await _service.GetReportByIdAsync(report.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(report.Id);
    }

    [Fact(DisplayName = "GetReportByIdAsync should return null when not found")]
    public async Task GetReportByIdAsync_ReturnsNull_WhenNotFound()
    {
        // Act
        var result = await _service.GetReportByIdAsync(99999);

        // Assert
        result.Should().BeNull();
    }

    [Fact(DisplayName = "GetReportByIdAsync should include template name")]
    public async Task GetReportByIdAsync_IncludesTemplateName()
    {
        // Arrange
        var template = await CreateTestTemplate();
        var report = new ReporteAutomatizado
        {
            FechaInicio = DateTime.UtcNow.AddDays(-1),
            FechaFin = DateTime.UtcNow,
            ContenidoMarkdown = "# Test Report",
            Estadisticas = "{}",
            ModeloReporteId = template.Id
        };
        _context.ReportesAutomatizados.Add(report);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetReportByIdAsync(report.Id);

        // Assert
        result.Should().NotBeNull();
        result!.NombreModelo.Should().Be(template.Nombre);
    }

    #endregion

    #region GenerateReportAsync Tests

    [Fact(DisplayName = "GenerateReportAsync should create report with statistics")]
    public async Task GenerateReportAsync_CreatesReportWithStatistics()
    {
        // Arrange
        await SeedIncidentReports(5);
        SetupAiServiceMock();

        var request = new GenerateReportRequestDto
        {
            FechaInicio = DateTime.UtcNow.AddDays(-7),
            FechaFin = DateTime.UtcNow,
            EnviarEmail = false
        };

        // Act
        var result = await _service.GenerateReportAsync(request, _testUserId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.ContenidoMarkdown.Should().NotBeNullOrEmpty();
        result.Estadisticas.Should().NotBeNull();
    }

    [Fact(DisplayName = "GenerateReportAsync should use specified template")]
    public async Task GenerateReportAsync_UsesSpecifiedTemplate()
    {
        // Arrange
        await SeedIncidentReports(3);
        var template = await CreateTestTemplate();
        SetupAiServiceMock();

        var request = new GenerateReportRequestDto
        {
            FechaInicio = DateTime.UtcNow.AddDays(-7),
            FechaFin = DateTime.UtcNow,
            ModeloReporteId = template.Id,
            EnviarEmail = false
        };

        // Act
        var result = await _service.GenerateReportAsync(request, _testUserId);

        // Assert
        result.Should().NotBeNull();
        result.ModeloReporteId.Should().Be(template.Id);
    }

    [Fact(DisplayName = "GenerateReportAsync should send email when requested")]
    public async Task GenerateReportAsync_SendsEmail_WhenRequested()
    {
        // Arrange
        await SeedIncidentReports(3);
        SetupAiServiceMock();
        SetupEmailServiceMock(success: true);

        var request = new GenerateReportRequestDto
        {
            FechaInicio = DateTime.UtcNow.AddDays(-7),
            FechaFin = DateTime.UtcNow,
            EnviarEmail = true,
            EmailDestinatarios = new List<string> { "recipient@test.com" }
        };

        // Act
        var result = await _service.GenerateReportAsync(request, _testUserId);

        // Assert
        await _emailService.Received(1).SendAsync(
            Arg.Any<SendEmailRequestDto>(),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "GenerateReportAsync should log audit event")]
    public async Task GenerateReportAsync_LogsAuditEvent()
    {
        // Arrange
        await SeedIncidentReports(3);
        SetupAiServiceMock();

        var request = new GenerateReportRequestDto
        {
            FechaInicio = DateTime.UtcNow.AddDays(-7),
            FechaFin = DateTime.UtcNow,
            EnviarEmail = false
        };

        // Act
        await _service.GenerateReportAsync(request, _testUserId);

        // Assert
        await _auditService.Received(1).LogAsync(
            Arg.Is<string>(s => s == AuditCodes.AUTO_REPORT_GEN),
            Arg.Any<int?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "GenerateReportAsync should handle AI service failure")]
    public async Task GenerateReportAsync_HandlesAiServiceFailure()
    {
        // Arrange
        await SeedIncidentReports(3);
        _aiService.GenerateNarrativeAsync(Arg.Any<NarrativeRequestDto>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("AI service unavailable"));

        var request = new GenerateReportRequestDto
        {
            FechaInicio = DateTime.UtcNow.AddDays(-7),
            FechaFin = DateTime.UtcNow,
            EnviarEmail = false
        };

        // Act & Assert
        var act = () => _service.GenerateReportAsync(request, _testUserId);
        await act.Should().ThrowAsync<Exception>();

        // Verify failure was logged
        await _auditService.Received(1).LogAsync(
            Arg.Is<string>(s => s == AuditCodes.AUTO_REPORT_FAIL),
            Arg.Any<int?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region SendReportByEmailAsync Tests

    [Fact(DisplayName = "SendReportByEmailAsync should return false when report not found")]
    public async Task SendReportByEmailAsync_ReturnsFalse_WhenReportNotFound()
    {
        // Act
        var result = await _service.SendReportByEmailAsync(99999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact(DisplayName = "SendReportByEmailAsync should update report status on success")]
    public async Task SendReportByEmailAsync_UpdatesReportStatus_OnSuccess()
    {
        // Arrange
        var report = await CreateTestReport();
        SetupEmailServiceMock(success: true);

        // Act
        var result = await _service.SendReportByEmailAsync(
            report.Id,
            new List<string> { "test@example.com" });

        // Assert
        result.Should().BeTrue();

        var updatedReport = await _context.ReportesAutomatizados.FindAsync(report.Id);
        updatedReport!.Enviado.Should().BeTrue();
        updatedReport.FechaEnvio.Should().NotBeNull();
    }

    [Fact(DisplayName = "SendReportByEmailAsync should store error on failure")]
    public async Task SendReportByEmailAsync_StoresError_OnFailure()
    {
        // Arrange
        var report = await CreateTestReport();
        SetupEmailServiceMock(success: false, error: "SMTP error");

        // Act
        var result = await _service.SendReportByEmailAsync(
            report.Id,
            new List<string> { "test@example.com" });

        // Assert
        result.Should().BeFalse();

        var updatedReport = await _context.ReportesAutomatizados.FindAsync(report.Id);
        updatedReport!.Enviado.Should().BeFalse();
        updatedReport.ErrorMensaje.Should().Be("SMTP error");
    }

    [Fact(DisplayName = "SendReportByEmailAsync should return false when no recipients")]
    public async Task SendReportByEmailAsync_ReturnsFalse_WhenNoRecipients()
    {
        // Arrange
        var report = await CreateTestReport();

        // Use configuration without recipients
        var configData = new Dictionary<string, string?>();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var serviceWithoutRecipients = new AutomatedReportService(
            _context,
            _aiService,
            _emailService,
            _auditService,
            config,
            _logger);

        // Act
        var result = await serviceWithoutRecipients.SendReportByEmailAsync(report.Id);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region DeleteReportAsync Tests

    [Fact(DisplayName = "DeleteReportAsync should return true when report deleted")]
    public async Task DeleteReportAsync_ReturnsTrue_WhenDeleted()
    {
        // Arrange
        var report = await CreateTestReport();

        // Act
        var result = await _service.DeleteReportAsync(report.Id);

        // Assert
        result.Should().BeTrue();
        var deletedReport = await _context.ReportesAutomatizados.FindAsync(report.Id);
        deletedReport.Should().BeNull();
    }

    [Fact(DisplayName = "DeleteReportAsync should return false when report not found")]
    public async Task DeleteReportAsync_ReturnsFalse_WhenNotFound()
    {
        // Act
        var result = await _service.DeleteReportAsync(99999);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Template Management Tests

    [Fact(DisplayName = "GetTemplatesAsync should return active templates")]
    public async Task GetTemplatesAsync_ReturnsActiveTemplates()
    {
        // Arrange
        await CreateTestTemplate(activo: true);
        await CreateTestTemplate(activo: true);
        await CreateTestTemplate(activo: false);

        // Act
        var result = await _service.GetTemplatesAsync(includeInactive: false);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(t => t.Activo);
    }

    [Fact(DisplayName = "GetTemplatesAsync should include inactive when requested")]
    public async Task GetTemplatesAsync_IncludesInactive_WhenRequested()
    {
        // Arrange
        await CreateTestTemplate(activo: true);
        await CreateTestTemplate(activo: false);

        // Act
        var result = await _service.GetTemplatesAsync(includeInactive: true);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact(DisplayName = "GetTemplateByIdAsync should return template when found")]
    public async Task GetTemplateByIdAsync_ReturnsTemplate_WhenFound()
    {
        // Arrange
        var template = await CreateTestTemplate();

        // Act
        var result = await _service.GetTemplateByIdAsync(template.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(template.Id);
    }

    [Fact(DisplayName = "GetTemplateByIdAsync should return null when not found")]
    public async Task GetTemplateByIdAsync_ReturnsNull_WhenNotFound()
    {
        // Act
        var result = await _service.GetTemplateByIdAsync(99999);

        // Assert
        result.Should().BeNull();
    }

    [Fact(DisplayName = "GetDefaultTemplateAsync should return default template")]
    public async Task GetDefaultTemplateAsync_ReturnsDefaultTemplate()
    {
        // Arrange
        await CreateTestTemplate(esDefault: false);
        var defaultTemplate = await CreateTestTemplate(esDefault: true);

        // Act
        var result = await _service.GetDefaultTemplateAsync();

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(defaultTemplate.Id);
        result.EsDefault.Should().BeTrue();
    }

    [Fact(DisplayName = "CreateTemplateAsync should create new template")]
    public async Task CreateTemplateAsync_CreatesNewTemplate()
    {
        // Arrange
        var dto = new CreateTemplateDto
        {
            Nombre = "New Template",
            Descripcion = "Description",
            ContenidoMarkdown = "# Template\n{{narrativa_ia}}",
            EsDefault = false
        };

        // Act
        var result = await _service.CreateTemplateAsync(dto, _testUserId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Nombre.Should().Be(dto.Nombre);
    }

    [Fact(DisplayName = "CreateTemplateAsync should clear other defaults when setting new default")]
    public async Task CreateTemplateAsync_ClearsOtherDefaults_WhenSettingDefault()
    {
        // Arrange
        var existingDefault = await CreateTestTemplate(esDefault: true);

        var dto = new CreateTemplateDto
        {
            Nombre = "New Default",
            Descripcion = "Description",
            ContenidoMarkdown = "# Template",
            EsDefault = true
        };

        // Act
        var result = await _service.CreateTemplateAsync(dto, _testUserId);

        // Assert
        result.EsDefault.Should().BeTrue();

        var oldDefault = await _context.ModelosReporte.FindAsync(existingDefault.Id);
        oldDefault!.EsDefault.Should().BeFalse();
    }

    [Fact(DisplayName = "UpdateTemplateAsync should update template")]
    public async Task UpdateTemplateAsync_UpdatesTemplate()
    {
        // Arrange
        var template = await CreateTestTemplate();
        var dto = new UpdateTemplateDto
        {
            Nombre = "Updated Name",
            Descripcion = "Updated Description",
            ContenidoMarkdown = "# Updated",
            Activo = true,
            EsDefault = false
        };

        // Act
        var result = await _service.UpdateTemplateAsync(template.Id, dto, _testUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Nombre.Should().Be("Updated Name");
        result.Descripcion.Should().Be("Updated Description");
    }

    [Fact(DisplayName = "UpdateTemplateAsync should return null when not found")]
    public async Task UpdateTemplateAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        var dto = new UpdateTemplateDto
        {
            Nombre = "Updated",
            Descripcion = "Desc",
            ContenidoMarkdown = "#",
            Activo = true,
            EsDefault = false
        };

        // Act
        var result = await _service.UpdateTemplateAsync(99999, dto, _testUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact(DisplayName = "DeleteTemplateAsync should soft delete when in use")]
    public async Task DeleteTemplateAsync_SoftDeletes_WhenInUse()
    {
        // Arrange
        var template = await CreateTestTemplate();
        var report = new ReporteAutomatizado
        {
            FechaInicio = DateTime.UtcNow.AddDays(-1),
            FechaFin = DateTime.UtcNow,
            ContenidoMarkdown = "# Report",
            Estadisticas = "{}",
            ModeloReporteId = template.Id
        };
        _context.ReportesAutomatizados.Add(report);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteTemplateAsync(template.Id);

        // Assert
        result.Should().BeTrue();

        var deletedTemplate = await _context.ModelosReporte.FindAsync(template.Id);
        deletedTemplate.Should().NotBeNull();
        deletedTemplate!.Activo.Should().BeFalse();
    }

    [Fact(DisplayName = "DeleteTemplateAsync should hard delete when not in use")]
    public async Task DeleteTemplateAsync_HardDeletes_WhenNotInUse()
    {
        // Arrange
        var template = await CreateTestTemplate();

        // Act
        var result = await _service.DeleteTemplateAsync(template.Id);

        // Assert
        result.Should().BeTrue();

        var deletedTemplate = await _context.ModelosReporte.FindAsync(template.Id);
        deletedTemplate.Should().BeNull();
    }

    [Fact(DisplayName = "SetDefaultTemplateAsync should set template as default")]
    public async Task SetDefaultTemplateAsync_SetsTemplateAsDefault()
    {
        // Arrange
        var template = await CreateTestTemplate(esDefault: false);

        // Act
        var result = await _service.SetDefaultTemplateAsync(template.Id);

        // Assert
        result.Should().BeTrue();

        var updatedTemplate = await _context.ModelosReporte.FindAsync(template.Id);
        updatedTemplate!.EsDefault.Should().BeTrue();
    }

    [Fact(DisplayName = "SetDefaultTemplateAsync should return false for inactive template")]
    public async Task SetDefaultTemplateAsync_ReturnsFalse_ForInactiveTemplate()
    {
        // Arrange
        var template = await CreateTestTemplate(activo: false);

        // Act
        var result = await _service.SetDefaultTemplateAsync(template.Id);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region CalculateStatisticsAsync Tests

    [Fact(DisplayName = "CalculateStatisticsAsync should calculate correct totals")]
    public async Task CalculateStatisticsAsync_CalculatesCorrectTotals()
    {
        // Arrange
        await SeedIncidentReports(10);

        // Act
        var result = await _service.CalculateStatisticsAsync(
            DateTime.UtcNow.AddDays(-7),
            DateTime.UtcNow.AddDays(1));

        // Assert
        result.Should().NotBeNull();
        result.TotalReportes.Should().Be(10);
    }

    [Fact(DisplayName = "CalculateStatisticsAsync should group by delito")]
    public async Task CalculateStatisticsAsync_GroupsByDelito()
    {
        // Arrange
        await SeedIncidentReports(10);

        // Act
        var result = await _service.CalculateStatisticsAsync(
            DateTime.UtcNow.AddDays(-7),
            DateTime.UtcNow.AddDays(1));

        // Assert
        result.PorDelito.Should().NotBeEmpty();
        result.DelitoMasFrecuente.Should().NotBeNullOrEmpty();
    }

    [Fact(DisplayName = "CalculateStatisticsAsync should only include delivered reports")]
    public async Task CalculateStatisticsAsync_OnlyIncludesDeliveredReports()
    {
        // Arrange
        await SeedIncidentReports(5, estado: 1); // Entregados
        await SeedIncidentReports(3, estado: 0); // Borradores

        // Act
        var result = await _service.CalculateStatisticsAsync(
            DateTime.UtcNow.AddDays(-7),
            DateTime.UtcNow.AddDays(1));

        // Assert
        result.TotalReportes.Should().Be(5);
        result.ReportesEntregados.Should().Be(5);
    }

    [Fact(DisplayName = "CalculateStatisticsAsync should return empty statistics for no data")]
    public async Task CalculateStatisticsAsync_ReturnsEmptyStats_WhenNoData()
    {
        // Act
        var result = await _service.CalculateStatisticsAsync(
            DateTime.UtcNow.AddDays(-7),
            DateTime.UtcNow);

        // Assert
        result.TotalReportes.Should().Be(0);
        result.PorDelito.Should().BeEmpty();
    }

    #endregion

    #region GetConfigurationAsync Tests

    [Fact(DisplayName = "GetConfigurationAsync should return configuration")]
    public async Task GetConfigurationAsync_ReturnsConfiguration()
    {
        // Act
        var result = await _service.GetConfigurationAsync();

        // Assert
        result.Should().NotBeNull();
        result.GenerationTime.Should().Be(new TimeSpan(6, 0, 0));
        result.Enabled.Should().BeTrue();
    }

    #endregion

    #region Helper Methods

    private async Task SeedAutomatedReports(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var report = new ReporteAutomatizado
            {
                FechaInicio = DateTime.UtcNow.AddDays(-i - 1),
                FechaFin = DateTime.UtcNow.AddDays(-i),
                ContenidoMarkdown = $"# Report {i}",
                Estadisticas = $"{{\"totalReportes\":{i}}}",
                Enviado = i % 2 == 0
            };
            _context.ReportesAutomatizados.Add(report);
        }
        await _context.SaveChangesAsync();
    }

    private async Task SeedIncidentReports(int count, short estado = 1)
    {
        // Create geographic hierarchy
        var zona = new Zona { Nombre = "Zona Test" };
        _context.Zonas.Add(zona);
        await _context.SaveChangesAsync();

        var region = new Region { Nombre = "Region Test", ZonaId = zona.Id };
        _context.Regiones.Add(region);
        await _context.SaveChangesAsync();

        var sector = new Sector { Nombre = "Sector Test", RegionId = region.Id };
        _context.Sectores.Add(sector);
        await _context.SaveChangesAsync();

        var cuadrante = new Cuadrante { Nombre = "Cuadrante Test", SectorId = sector.Id };
        _context.Cuadrantes.Add(cuadrante);
        await _context.SaveChangesAsync();

        var delitos = new[] { "Robo", "Asalto", "Vandalismo", "Fraude", "Acoso" };
        var sexos = new[] { "M", "F", "NB" };

        for (int i = 0; i < count; i++)
        {
            var report = new ReporteIncidencia
            {
                HechosReportados = $"Hechos del reporte {i}",
                AccionesRealizadas = $"Acciones del reporte {i}",
                Delito = delitos[i % delitos.Length],
                Sexo = sexos[i % sexos.Length],
                Edad = 20 + i,
                TipoDeAtencion = "Presencial",
                TipoDeAccion = "Preventiva",
                TurnoCeiba = "Balderas 1",
                DatetimeHechos = DateTime.UtcNow.AddDays(-i),
                ZonaId = zona.Id,
                RegionId = region.Id,
                SectorId = sector.Id,
                CuadranteId = cuadrante.Id,
                Estado = estado,
                UsuarioId = _testUserId
            };
            _context.ReportesIncidencia.Add(report);
        }
        await _context.SaveChangesAsync();
    }

    private async Task<ReporteAutomatizado> CreateTestReport()
    {
        var report = new ReporteAutomatizado
        {
            FechaInicio = DateTime.UtcNow.AddDays(-1),
            FechaFin = DateTime.UtcNow,
            ContenidoMarkdown = "# Test Report\n\nContent here.",
            Estadisticas = "{\"totalReportes\":5}",
            Enviado = false
        };
        _context.ReportesAutomatizados.Add(report);
        await _context.SaveChangesAsync();
        return report;
    }

    private async Task<ModeloReporte> CreateTestTemplate(bool activo = true, bool esDefault = false)
    {
        var template = new ModeloReporte
        {
            Nombre = $"Template {Guid.NewGuid():N}",
            Descripcion = "Test template description",
            ContenidoMarkdown = "# {{fecha_inicio}} - {{fecha_fin}}\n\n{{narrativa_ia}}",
            Activo = activo,
            EsDefault = esDefault,
            UsuarioId = _testUserId
        };
        _context.ModelosReporte.Add(template);
        await _context.SaveChangesAsync();
        return template;
    }

    private void SetupAiServiceMock()
    {
        _aiService.GenerateNarrativeAsync(Arg.Any<NarrativeRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(new NarrativeResponseDto
            {
                Narrativa = "This is a test AI-generated narrative for the report.",
                Success = true,
                TokensUsed = 100
            });
    }

    private void SetupEmailServiceMock(bool success, string? error = null)
    {
        _emailService.SendAsync(Arg.Any<SendEmailRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(new SendEmailResultDto
            {
                Success = success,
                Error = error,
                SentAt = success ? DateTime.UtcNow : null
            });
    }

    #endregion
}
