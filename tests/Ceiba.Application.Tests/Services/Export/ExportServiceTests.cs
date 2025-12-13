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
            TipoDeAccion = 1,
            HechosReportados = "Incidente reportado",
            AccionesRealizadas = "Acciones tomadas",
            Traslados = 0,
            Observaciones = "Observaciones del caso",
            ZonaId = 1,
            Zona = new Zona { Id = 1, Nombre = "Zona Centro" },
            SectorId = 1,
            Sector = new Sector { Id = 1, Nombre = "Sector A", RegionId = 1 },
            CuadranteId = 1,
            Cuadrante = new Cuadrante { Id = 1, Nombre = "Cuadrante 1", SectorId = 1 },
            TurnoCeiba = 1,
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
}
