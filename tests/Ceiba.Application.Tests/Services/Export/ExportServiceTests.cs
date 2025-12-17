using Ceiba.Application.Services.Export;
using Ceiba.Core.Entities;
using Ceiba.Core.Interfaces;
using Ceiba.Shared.DTOs.Export;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Ceiba.Application.Tests.Services.Export;

/// <summary>
/// Unit tests for Export orchestration service
/// Tests T-US2-021 to T-US2-030
/// </summary>
[Trait("Category", "Unit")]
[Trait("Service", "Export")]
public class ExportServiceTests
{
    private readonly Mock<IReportRepository> _mockReportRepo;
    private readonly Mock<IPdfGenerator> _mockPdfGenerator;
    private readonly Mock<IJsonExporter> _mockJsonExporter;
    private readonly Mock<IUserManagementService> _mockUserService;
    private readonly Mock<ILogger<ExportService>> _mockLogger;
    private readonly ExportService _exportService;

    public ExportServiceTests()
    {
        _mockReportRepo = new Mock<IReportRepository>();
        _mockPdfGenerator = new Mock<IPdfGenerator>();
        _mockJsonExporter = new Mock<IJsonExporter>();
        _mockUserService = new Mock<IUserManagementService>();
        _mockLogger = new Mock<ILogger<ExportService>>();

        _exportService = new ExportService(
            _mockReportRepo.Object,
            _mockPdfGenerator.Object,
            _mockJsonExporter.Object,
            _mockUserService.Object,
            _mockLogger.Object
        );
    }

    private static ReporteIncidencia CreateTestReport(int id = 1)
    {
        return new ReporteIncidencia
        {
            Id = id,
            Estado = 1, // Entregado
            DatetimeHechos = new DateTime(2025, 11, 27, 10, 0, 0, DateTimeKind.Utc),
            Sexo = "Femenino",
            Edad = 28,
            LgbtttiqPlus = false,
            SituacionCalle = false,
            Migrante = false,
            Discapacidad = false,
            Delito = "Violencia familiar",
            TipoDeAtencion = "Presencial",
            TipoDeAccion = "Preventiva",
            HechosReportados = "Incidente reportado",
            AccionesRealizadas = "Acciones tomadas",
            Traslados = "No",
            Observaciones = "Observaciones del caso",
            ZonaId = 1,
            Zona = new Zona { Id = 1, Nombre = "Zona Centro" },
            SectorId = 1,
            Sector = new Sector { Id = 1, Nombre = "Sector A", RegionId = 1 },
            CuadranteId = 1,
            Cuadrante = new Cuadrante { Id = 1, Nombre = "Cuadrante 1", SectorId = 1 },
            TurnoCeiba = "Balderas 1",
            UsuarioId = Guid.NewGuid(),
            CreatedAt = new DateTime(2025, 11, 27, 10, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2025, 11, 27, 15, 30, 0, DateTimeKind.Utc)
        };
    }

    [Fact(DisplayName = "T-US2-021: ExportSingleReport as non-REVISOR should throw UnauthorizedAccessException")]
    public async Task ExportSingleReportAsync_AsNonRevisor_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reportId = 1;

        // Act & Assert
        var act = async () => await _exportService.ExportSingleReportAsync(
            reportId,
            ExportFormat.PDF,
            userId,
            isRevisor: false
        );

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*REVISOR*");
    }

    [Fact(DisplayName = "T-US2-022: ExportSingleReport with non-existent report should throw KeyNotFoundException")]
    public async Task ExportSingleReportAsync_WithNonExistentReport_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reportId = 999;

        _mockReportRepo.Setup(r => r.GetByIdWithRelationsAsync(reportId))
            .ReturnsAsync((ReporteIncidencia?)null);

        // Act & Assert
        var act = async () => await _exportService.ExportSingleReportAsync(
            reportId,
            ExportFormat.PDF,
            userId,
            isRevisor: true
        );

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*not found*");
    }

    [Fact(DisplayName = "T-US2-023: ExportSingleReport as REVISOR with PDF format should generate PDF")]
    public async Task ExportSingleReportAsync_AsRevisorWithPdf_GeneratesPdf()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var report = CreateTestReport();
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF

        _mockReportRepo.Setup(r => r.GetByIdWithRelationsAsync(report.Id))
            .ReturnsAsync(report);

        _mockPdfGenerator.Setup(p => p.GenerateSingleReport(It.IsAny<ReportExportDto>()))
            .Returns(pdfBytes);

        // Act
        var result = await _exportService.ExportSingleReportAsync(
            report.Id,
            ExportFormat.PDF,
            userId,
            isRevisor: true
        );

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().Equal(pdfBytes);
        result.ContentType.Should().Be("application/pdf");
        result.FileName.Should().EndWith(".pdf");
        result.ReportCount.Should().Be(1);

        _mockPdfGenerator.Verify(p => p.GenerateSingleReport(It.IsAny<ReportExportDto>()), Times.Once);
        _mockJsonExporter.Verify(j => j.ExportSingleReport(It.IsAny<ReportExportDto>(), It.IsAny<ExportOptions>()), Times.Never);
    }

    [Fact(DisplayName = "T-US2-024: ExportSingleReport as REVISOR with JSON format should generate JSON")]
    public async Task ExportSingleReportAsync_AsRevisorWithJson_GeneratesJson()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var report = CreateTestReport();
        var jsonBytes = System.Text.Encoding.UTF8.GetBytes("{\"id\":1}");

        _mockReportRepo.Setup(r => r.GetByIdWithRelationsAsync(report.Id))
            .ReturnsAsync(report);

        _mockJsonExporter.Setup(j => j.ExportSingleReport(It.IsAny<ReportExportDto>(), It.IsAny<ExportOptions>()))
            .Returns(jsonBytes);

        // Act
        var result = await _exportService.ExportSingleReportAsync(
            report.Id,
            ExportFormat.JSON,
            userId,
            isRevisor: true
        );

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().Equal(jsonBytes);
        result.ContentType.Should().Be("application/json");
        result.FileName.Should().EndWith(".json");
        result.ReportCount.Should().Be(1);

        _mockJsonExporter.Verify(j => j.ExportSingleReport(It.IsAny<ReportExportDto>(), It.IsAny<ExportOptions>()), Times.Once);
        _mockPdfGenerator.Verify(p => p.GenerateSingleReport(It.IsAny<ReportExportDto>()), Times.Never);
    }

    [Fact(DisplayName = "T-US2-025: ExportReports with multiple IDs should generate multi-report PDF")]
    public async Task ExportReportsAsync_WithMultipleIds_GeneratesMultiReportPdf()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reportIds = new[] { 1, 2, 3 };
        var reports = reportIds.Select(id => CreateTestReport(id)).ToList();
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        foreach (var report in reports)
        {
            _mockReportRepo.Setup(r => r.GetByIdWithRelationsAsync(report.Id))
                .ReturnsAsync(report);
        }

        _mockPdfGenerator.Setup(p => p.GenerateMultipleReports(It.IsAny<IEnumerable<ReportExportDto>>()))
            .Returns(pdfBytes);

        var request = new ExportRequestDto
        {
            ReportIds = reportIds,
            Format = ExportFormat.PDF
        };

        // Act
        var result = await _exportService.ExportReportsAsync(
            request,
            userId,
            isRevisor: true
        );

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().Equal(pdfBytes);
        result.ContentType.Should().Be("application/pdf");
        result.ReportCount.Should().Be(3);

        _mockPdfGenerator.Verify(p => p.GenerateMultipleReports(
            It.Is<IEnumerable<ReportExportDto>>(list => list.Count() == 3)
        ), Times.Once);
    }

    [Fact(DisplayName = "T-US2-026: ExportReports should map entity fields correctly to DTO")]
    public async Task ExportReportsAsync_MapsEntityFieldsCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var report = CreateTestReport(42);
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        _mockReportRepo.Setup(r => r.GetByIdWithRelationsAsync(report.Id))
            .ReturnsAsync(report);

        ReportExportDto? capturedDto = null;
        _mockPdfGenerator.Setup(p => p.GenerateSingleReport(It.IsAny<ReportExportDto>()))
            .Callback<ReportExportDto>(dto => capturedDto = dto)
            .Returns(pdfBytes);

        // Act
        await _exportService.ExportSingleReportAsync(
            report.Id,
            ExportFormat.PDF,
            userId,
            isRevisor: true
        );

        // Assert
        capturedDto.Should().NotBeNull();
        capturedDto!.Id.Should().Be(42);
        capturedDto.Estado.Should().Be("Entregado");
        capturedDto.Sexo.Should().Be("Femenino");
        capturedDto.Edad.Should().Be(28);
        capturedDto.Delito.Should().Be("Violencia familiar");
        capturedDto.Zona.Should().Be("Zona Centro");
        capturedDto.Sector.Should().Be("Sector A");
        capturedDto.Cuadrante.Should().Be("Cuadrante 1");
        capturedDto.UsuarioCreador.Should().NotBeNullOrEmpty();
        // Verify it's a GUID string
        Guid.TryParse(capturedDto.UsuarioCreador, out _).Should().BeTrue();
    }

    [Fact(DisplayName = "T-US2-027: ExportReports should generate folio if not present")]
    public async Task ExportReportsAsync_GeneratesFolioIfNotPresent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var report = CreateTestReport(123);
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        _mockReportRepo.Setup(r => r.GetByIdWithRelationsAsync(report.Id))
            .ReturnsAsync(report);

        ReportExportDto? capturedDto = null;
        _mockPdfGenerator.Setup(p => p.GenerateSingleReport(It.IsAny<ReportExportDto>()))
            .Callback<ReportExportDto>(dto => capturedDto = dto)
            .Returns(pdfBytes);

        // Act
        await _exportService.ExportSingleReportAsync(
            report.Id,
            ExportFormat.PDF,
            userId,
            isRevisor: true
        );

        // Assert
        capturedDto.Should().NotBeNull();
        capturedDto!.Folio.Should().NotBeNullOrEmpty();
        capturedDto.Folio.Should().StartWith("CEIBA-");
        capturedDto.Folio.Should().Contain($"{report.CreatedAt.Year}");
    }

    [Fact(DisplayName = "T-US2-028: ExportReports without report IDs should throw ArgumentException")]
    public async Task ExportReportsAsync_WithoutReportIds_ThrowsArgumentException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new ExportRequestDto
        {
            ReportIds = null, // No report IDs provided
            Format = ExportFormat.PDF
        };

        // Act & Assert
        var act = async () => await _exportService.ExportReportsAsync(
            request,
            userId,
            isRevisor: true
        );

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*at least one report*");
    }

    [Fact(DisplayName = "T-US2-029: ExportReports with empty array should throw ArgumentException")]
    public async Task ExportReportsAsync_WithEmptyArray_ThrowsArgumentException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new ExportRequestDto
        {
            ReportIds = Array.Empty<int>(),
            Format = ExportFormat.PDF
        };

        // Act & Assert
        var act = async () => await _exportService.ExportReportsAsync(
            request,
            userId,
            isRevisor: true
        );

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*at least one report*");
    }

    [Fact(DisplayName = "T-US2-030: ExportReports should set filename with current date")]
    public async Task ExportReportsAsync_SetsFilenameWithCurrentDate()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var report = CreateTestReport();
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        _mockReportRepo.Setup(r => r.GetByIdWithRelationsAsync(report.Id))
            .ReturnsAsync(report);

        _mockPdfGenerator.Setup(p => p.GenerateSingleReport(It.IsAny<ReportExportDto>()))
            .Returns(pdfBytes);

        // Act
        var result = await _exportService.ExportSingleReportAsync(
            report.Id,
            ExportFormat.PDF,
            userId,
            isRevisor: true
        );

        // Assert
        result.FileName.Should().Contain(DateTime.UtcNow.ToString("yyyyMMdd"));
        result.FileName.Should().StartWith("reporte_");
    }

    #region Export Limits Tests (T052a)

    [Fact(DisplayName = "T052a: ExportReports exceeding PDF limit should throw ArgumentException")]
    public async Task ExportReportsAsync_ExceedingPdfLimit_ThrowsArgumentException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reportIds = Enumerable.Range(1, ExportService.MaxPdfReports + 1).ToArray();
        var request = new ExportRequestDto
        {
            ReportIds = reportIds,
            Format = ExportFormat.PDF
        };

        // Act & Assert
        var act = async () => await _exportService.ExportReportsAsync(
            request,
            userId,
            isRevisor: true
        );

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"*{ExportService.MaxPdfReports}*");
    }

    [Fact(DisplayName = "T052a: ExportReports exceeding JSON limit should throw ArgumentException")]
    public async Task ExportReportsAsync_ExceedingJsonLimit_ThrowsArgumentException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reportIds = Enumerable.Range(1, ExportService.MaxJsonReports + 1).ToArray();
        var request = new ExportRequestDto
        {
            ReportIds = reportIds,
            Format = ExportFormat.JSON
        };

        // Act & Assert
        var act = async () => await _exportService.ExportReportsAsync(
            request,
            userId,
            isRevisor: true
        );

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"*{ExportService.MaxJsonReports}*");
    }

    [Fact(DisplayName = "T052a: ExportReports at exact PDF limit should succeed")]
    public async Task ExportReportsAsync_AtExactPdfLimit_Succeeds()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reportIds = Enumerable.Range(1, ExportService.MaxPdfReports).ToArray();
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        foreach (var id in reportIds)
        {
            _mockReportRepo.Setup(r => r.GetByIdWithRelationsAsync(id))
                .ReturnsAsync(CreateTestReport(id));
        }

        _mockPdfGenerator.Setup(p => p.GenerateMultipleReports(It.IsAny<IEnumerable<ReportExportDto>>()))
            .Returns(pdfBytes);

        var request = new ExportRequestDto
        {
            ReportIds = reportIds,
            Format = ExportFormat.PDF
        };

        // Act
        var result = await _exportService.ExportReportsAsync(
            request,
            userId,
            isRevisor: true
        );

        // Assert
        result.Should().NotBeNull();
        result.ReportCount.Should().Be(ExportService.MaxPdfReports);
    }

    #endregion

    #region Multiple Reports JSON Export Tests

    [Fact(DisplayName = "ExportReports with multiple IDs should generate multi-report JSON")]
    public async Task ExportReportsAsync_WithMultipleIds_GeneratesMultiReportJson()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reportIds = new[] { 1, 2, 3 };
        var reports = reportIds.Select(id => CreateTestReport(id)).ToList();
        var jsonBytes = System.Text.Encoding.UTF8.GetBytes("[{\"id\":1},{\"id\":2},{\"id\":3}]");

        foreach (var report in reports)
        {
            _mockReportRepo.Setup(r => r.GetByIdWithRelationsAsync(report.Id))
                .ReturnsAsync(report);
        }

        _mockJsonExporter.Setup(j => j.ExportMultipleReports(It.IsAny<IEnumerable<ReportExportDto>>(), It.IsAny<ExportOptions>()))
            .Returns(jsonBytes);

        var request = new ExportRequestDto
        {
            ReportIds = reportIds,
            Format = ExportFormat.JSON
        };

        // Act
        var result = await _exportService.ExportReportsAsync(
            request,
            userId,
            isRevisor: true
        );

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().Equal(jsonBytes);
        result.ContentType.Should().Be("application/json");
        result.ReportCount.Should().Be(3);
        result.FileName.Should().Contain("reportes_3_");

        _mockJsonExporter.Verify(j => j.ExportMultipleReports(
            It.Is<IEnumerable<ReportExportDto>>(list => list.Count() == 3),
            It.IsAny<ExportOptions>()
        ), Times.Once);
    }

    [Fact(DisplayName = "ExportReports with single ID should use single report generator")]
    public async Task ExportReportsAsync_WithSingleId_UsesSingleReportGenerator()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var report = CreateTestReport(1);
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        _mockReportRepo.Setup(r => r.GetByIdWithRelationsAsync(report.Id))
            .ReturnsAsync(report);

        _mockPdfGenerator.Setup(p => p.GenerateSingleReport(It.IsAny<ReportExportDto>()))
            .Returns(pdfBytes);

        var request = new ExportRequestDto
        {
            ReportIds = new[] { 1 },
            Format = ExportFormat.PDF
        };

        // Act
        var result = await _exportService.ExportReportsAsync(
            request,
            userId,
            isRevisor: true
        );

        // Assert
        result.Should().NotBeNull();
        result.ReportCount.Should().Be(1);
        _mockPdfGenerator.Verify(p => p.GenerateSingleReport(It.IsAny<ReportExportDto>()), Times.Once);
        _mockPdfGenerator.Verify(p => p.GenerateMultipleReports(It.IsAny<IEnumerable<ReportExportDto>>()), Times.Never);
    }

    #endregion

    #region Authorization Tests

    [Fact(DisplayName = "ExportReports as non-REVISOR should throw UnauthorizedAccessException")]
    public async Task ExportReportsAsync_AsNonRevisor_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new ExportRequestDto
        {
            ReportIds = new[] { 1 },
            Format = ExportFormat.PDF
        };

        // Act & Assert
        var act = async () => await _exportService.ExportReportsAsync(
            request,
            userId,
            isRevisor: false
        );

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*REVISOR*");
    }

    #endregion

    #region Report Not Found Tests

    [Fact(DisplayName = "ExportReports with all non-existent reports should throw KeyNotFoundException")]
    public async Task ExportReportsAsync_WithAllNonExistentReports_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new ExportRequestDto
        {
            ReportIds = new[] { 999, 998, 997 },
            Format = ExportFormat.PDF
        };

        _mockReportRepo.Setup(r => r.GetByIdWithRelationsAsync(It.IsAny<int>()))
            .ReturnsAsync((ReporteIncidencia?)null);

        // Act & Assert
        var act = async () => await _exportService.ExportReportsAsync(
            request,
            userId,
            isRevisor: true
        );

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*No reports found*");
    }

    [Fact(DisplayName = "ExportReports with some non-existent reports should export found reports only")]
    public async Task ExportReportsAsync_WithSomeNonExistentReports_ExportsFoundReportsOnly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new ExportRequestDto
        {
            ReportIds = new[] { 1, 999, 2 }, // 999 doesn't exist
            Format = ExportFormat.PDF
        };
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        _mockReportRepo.Setup(r => r.GetByIdWithRelationsAsync(1))
            .ReturnsAsync(CreateTestReport(1));
        _mockReportRepo.Setup(r => r.GetByIdWithRelationsAsync(2))
            .ReturnsAsync(CreateTestReport(2));
        _mockReportRepo.Setup(r => r.GetByIdWithRelationsAsync(999))
            .ReturnsAsync((ReporteIncidencia?)null);

        _mockPdfGenerator.Setup(p => p.GenerateMultipleReports(It.IsAny<IEnumerable<ReportExportDto>>()))
            .Returns(pdfBytes);

        // Act
        var result = await _exportService.ExportReportsAsync(
            request,
            userId,
            isRevisor: true
        );

        // Assert
        result.Should().NotBeNull();
        result.ReportCount.Should().Be(2); // Only 2 reports found
    }

    #endregion

    #region DTO Mapping Tests

    [Fact(DisplayName = "MapToExportDto should use user email when available")]
    public async Task ExportReportsAsync_WithUserEmail_UsesEmailInDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var report = CreateTestReport();
        var userEmail = "usuario@ceiba.local";
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        _mockReportRepo.Setup(r => r.GetByIdWithRelationsAsync(report.Id))
            .ReturnsAsync(report);

        _mockUserService.Setup(u => u.GetUserByIdAsync(report.UsuarioId))
            .ReturnsAsync(new Ceiba.Shared.DTOs.UserDto { Id = report.UsuarioId, Email = userEmail });

        ReportExportDto? capturedDto = null;
        _mockPdfGenerator.Setup(p => p.GenerateSingleReport(It.IsAny<ReportExportDto>()))
            .Callback<ReportExportDto>(dto => capturedDto = dto)
            .Returns(pdfBytes);

        // Act
        await _exportService.ExportSingleReportAsync(
            report.Id,
            ExportFormat.PDF,
            userId,
            isRevisor: true
        );

        // Assert
        capturedDto.Should().NotBeNull();
        capturedDto!.UsuarioCreador.Should().Be(userEmail);
    }

    [Fact(DisplayName = "MapToExportDto should fallback to GUID when user lookup fails")]
    public async Task ExportReportsAsync_WhenUserLookupFails_FallsBackToGuid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var report = CreateTestReport();
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        _mockReportRepo.Setup(r => r.GetByIdWithRelationsAsync(report.Id))
            .ReturnsAsync(report);

        _mockUserService.Setup(u => u.GetUserByIdAsync(report.UsuarioId))
            .ThrowsAsync(new Exception("User service unavailable"));

        ReportExportDto? capturedDto = null;
        _mockPdfGenerator.Setup(p => p.GenerateSingleReport(It.IsAny<ReportExportDto>()))
            .Callback<ReportExportDto>(dto => capturedDto = dto)
            .Returns(pdfBytes);

        // Act
        await _exportService.ExportSingleReportAsync(
            report.Id,
            ExportFormat.PDF,
            userId,
            isRevisor: true
        );

        // Assert
        capturedDto.Should().NotBeNull();
        Guid.TryParse(capturedDto!.UsuarioCreador, out _).Should().BeTrue();
    }

    [Theory(DisplayName = "TipoDeAccion should be passed through directly")]
    [InlineData("Preventiva")]
    [InlineData("Reactiva")]
    [InlineData("Seguimiento")]
    [InlineData("Orientación y apoyo")]
    public async Task ExportReportsAsync_PassesTipoDeAccionDirectly(string tipoDeAccion)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var report = CreateTestReport();
        report.TipoDeAccion = tipoDeAccion;
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        _mockReportRepo.Setup(r => r.GetByIdWithRelationsAsync(report.Id))
            .ReturnsAsync(report);

        ReportExportDto? capturedDto = null;
        _mockPdfGenerator.Setup(p => p.GenerateSingleReport(It.IsAny<ReportExportDto>()))
            .Callback<ReportExportDto>(dto => capturedDto = dto)
            .Returns(pdfBytes);

        // Act
        await _exportService.ExportSingleReportAsync(
            report.Id,
            ExportFormat.PDF,
            userId,
            isRevisor: true
        );

        // Assert
        capturedDto.Should().NotBeNull();
        capturedDto!.TipoDeAccion.Should().Be(tipoDeAccion);
    }

    [Theory(DisplayName = "TurnoCeiba should be passed through correctly")]
    [InlineData("Balderas 1")]
    [InlineData("Balderas 2")]
    [InlineData("Nonoalco 1")]
    public async Task ExportReportsAsync_PassesTurnoCeibaCorrectly(string turnoCeiba)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var report = CreateTestReport();
        report.TurnoCeiba = turnoCeiba;
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        _mockReportRepo.Setup(r => r.GetByIdWithRelationsAsync(report.Id))
            .ReturnsAsync(report);

        ReportExportDto? capturedDto = null;
        _mockPdfGenerator.Setup(p => p.GenerateSingleReport(It.IsAny<ReportExportDto>()))
            .Callback<ReportExportDto>(dto => capturedDto = dto)
            .Returns(pdfBytes);

        // Act
        await _exportService.ExportSingleReportAsync(
            report.Id,
            ExportFormat.PDF,
            userId,
            isRevisor: true
        );

        // Assert
        capturedDto.Should().NotBeNull();
        capturedDto!.TurnoCeiba.Should().Be(turnoCeiba);
    }

    [Theory(DisplayName = "Traslados should be passed through correctly")]
    [InlineData("Sí")]
    [InlineData("No")]
    [InlineData("No aplica")]
    public async Task ExportReportsAsync_PassesTrasladosCorrectly(string traslados)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var report = CreateTestReport();
        report.Traslados = traslados;
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        _mockReportRepo.Setup(r => r.GetByIdWithRelationsAsync(report.Id))
            .ReturnsAsync(report);

        ReportExportDto? capturedDto = null;
        _mockPdfGenerator.Setup(p => p.GenerateSingleReport(It.IsAny<ReportExportDto>()))
            .Callback<ReportExportDto>(dto => capturedDto = dto)
            .Returns(pdfBytes);

        // Act
        await _exportService.ExportSingleReportAsync(
            report.Id,
            ExportFormat.PDF,
            userId,
            isRevisor: true
        );

        // Assert
        capturedDto.Should().NotBeNull();
        capturedDto!.Traslados.Should().Be(traslados);
    }

    [Fact(DisplayName = "MapToExportDto should map Estado Borrador correctly")]
    public async Task ExportReportsAsync_MapsEstadoBorradorCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var report = CreateTestReport();
        report.Estado = 0; // Borrador
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        _mockReportRepo.Setup(r => r.GetByIdWithRelationsAsync(report.Id))
            .ReturnsAsync(report);

        ReportExportDto? capturedDto = null;
        _mockPdfGenerator.Setup(p => p.GenerateSingleReport(It.IsAny<ReportExportDto>()))
            .Callback<ReportExportDto>(dto => capturedDto = dto)
            .Returns(pdfBytes);

        // Act
        await _exportService.ExportSingleReportAsync(
            report.Id,
            ExportFormat.PDF,
            userId,
            isRevisor: true
        );

        // Assert
        capturedDto.Should().NotBeNull();
        capturedDto!.Estado.Should().Be("Borrador");
        capturedDto.FechaEntrega.Should().BeNull();
    }

    [Fact(DisplayName = "MapToExportDto should handle null geographic relations")]
    public async Task ExportReportsAsync_HandlesNullGeographicRelations()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var report = CreateTestReport();
        report.Zona = null!;
        report.Region = null!;
        report.Sector = null!;
        report.Cuadrante = null!;
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        _mockReportRepo.Setup(r => r.GetByIdWithRelationsAsync(report.Id))
            .ReturnsAsync(report);

        ReportExportDto? capturedDto = null;
        _mockPdfGenerator.Setup(p => p.GenerateSingleReport(It.IsAny<ReportExportDto>()))
            .Callback<ReportExportDto>(dto => capturedDto = dto)
            .Returns(pdfBytes);

        // Act
        await _exportService.ExportSingleReportAsync(
            report.Id,
            ExportFormat.PDF,
            userId,
            isRevisor: true
        );

        // Assert
        capturedDto.Should().NotBeNull();
        capturedDto!.Zona.Should().BeEmpty();
        capturedDto.Region.Should().BeEmpty();
        capturedDto.Sector.Should().BeEmpty();
        capturedDto.Cuadrante.Should().BeEmpty();
    }

    #endregion

    #region Cancellation Tests

    [Fact(DisplayName = "ExportReports should respect cancellation token")]
    public async Task ExportReportsAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new ExportRequestDto
        {
            ReportIds = new[] { 1, 2, 3 },
            Format = ExportFormat.PDF
        };
        using var cts = new CancellationTokenSource();

        _mockReportRepo.Setup(r => r.GetByIdWithRelationsAsync(It.IsAny<int>()))
            .ReturnsAsync((int id) =>
            {
                if (id == 2) cts.Cancel(); // Cancel on second report
                return CreateTestReport(id);
            });

        // Act & Assert
        var act = async () => await _exportService.ExportReportsAsync(
            request,
            userId,
            isRevisor: true,
            cts.Token
        );

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region Constants Tests

    [Fact(DisplayName = "MaxPdfReports should be 50")]
    public void MaxPdfReports_ShouldBe50()
    {
        ExportService.MaxPdfReports.Should().Be(50);
    }

    [Fact(DisplayName = "MaxJsonReports should be 100")]
    public void MaxJsonReports_ShouldBe100()
    {
        ExportService.MaxJsonReports.Should().Be(100);
    }

    [Fact(DisplayName = "BackgroundExportThreshold should be 50")]
    public void BackgroundExportThreshold_ShouldBe50()
    {
        ExportService.BackgroundExportThreshold.Should().Be(50);
    }

    [Fact(DisplayName = "AlertDurationSeconds should be 30")]
    public void AlertDurationSeconds_ShouldBe30()
    {
        ExportService.AlertDurationSeconds.Should().Be(30);
    }

    [Fact(DisplayName = "AlertFileSizeBytes should be 500MB")]
    public void AlertFileSizeBytes_ShouldBe500MB()
    {
        ExportService.AlertFileSizeBytes.Should().Be(500 * 1024 * 1024);
    }

    #endregion
}
