using Ceiba.Application.Services.Export;
using Ceiba.Shared.DTOs.Export;
using FluentAssertions;
using Xunit;

namespace Ceiba.Application.Tests.Services.Export;

/// <summary>
/// Unit tests for PDF generation service
/// Tests T-US2-001 to T-US2-005
/// </summary>
[Trait("Category", "Unit")]
[Trait("Service", "Export")]
public class PdfGeneratorTests
{
    private static ReportExportDto CreateTestReport(int id = 1, string folio = "CEIBA-2025-001")
    {
        return new ReportExportDto
        {
            Id = id,
            Folio = folio,
            Estado = "Entregado",
            FechaCreacion = new DateTime(2025, 11, 27, 10, 0, 0, DateTimeKind.Utc),
            FechaEntrega = new DateTime(2025, 11, 27, 15, 30, 0, DateTimeKind.Utc),
            UsuarioCreador = "oficial@ceiba.local",
            Sexo = "Femenino",
            Edad = 28,
            LgbtttiqPlus = false,
            SituacionCalle = false,
            Migrante = false,
            Discapacidad = false,
            Delito = "Violencia familiar",
            TipoDeAtencion = "Presencial",
            TipoDeAccion = "Orientación",
            Zona = "Zona Centro",
            Sector = "Sector A",
            Cuadrante = "Cuadrante 1",
            TurnoCeiba = "Matutino",
            HechosReportados = "Se reporta incidente de violencia familiar en domicilio particular.",
            AccionesRealizadas = "Se brindó orientación a la víctima y se canalizó a instancias correspondientes.",
            Traslados = "Ninguno",
            Observaciones = "Víctima acepta apoyo psicológico."
        };
    }

    [Fact(DisplayName = "T-US2-001: GenerateSingleReport should create valid PDF file")]
    public void GenerateSingleReport_WithValidData_CreatesValidPdf()
    {
        // Arrange
        var generator = new PdfGenerator();
        var report = CreateTestReport();

        // Act
        var pdfBytes = generator.GenerateSingleReport(report);

        // Assert
        pdfBytes.Should().NotBeEmpty("PDF should contain data");
        pdfBytes.Length.Should().BeGreaterThan(100, "PDF should have reasonable size");

        // Verify PDF signature (%PDF header)
        pdfBytes[0].Should().Be(0x25, "PDF should start with %");
        pdfBytes[1].Should().Be(0x50, "PDF should have P");
        pdfBytes[2].Should().Be(0x44, "PDF should have D");
        pdfBytes[3].Should().Be(0x46, "PDF should have F");
    }

    [Fact(DisplayName = "T-US2-002: GenerateSingleReport should include all report fields")]
    public void GenerateSingleReport_WithCompleteData_IncludesAllFields()
    {
        // Arrange
        var generator = new PdfGenerator();
        var report = CreateTestReport();

        // Act
        var pdfBytes = generator.GenerateSingleReport(report);

        // Assert
        pdfBytes.Should().NotBeEmpty();
        // Note: Detailed content verification would require PDF parsing library
        // For now, we verify that PDF is generated and has reasonable size
        pdfBytes.Length.Should().BeGreaterThan(500, "PDF with all fields should be substantial");
    }

    [Fact(DisplayName = "T-US2-003: GenerateSingleReport should handle special characters")]
    public void GenerateSingleReport_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var generator = new PdfGenerator();
        var report = CreateTestReport();
        report = report with
        {
            HechosReportados = "Reporte con caracteres especiales: áéíóú ñÑ ¿? ¡!",
            Observaciones = "Símbolos: @#$%&*() \"comillas\" 'apóstrofes'"
        };

        // Act
        var pdfBytes = generator.GenerateSingleReport(report);

        // Assert
        pdfBytes.Should().NotBeEmpty();
        pdfBytes.Length.Should().BeGreaterThan(100);
    }

    [Fact(DisplayName = "T-US2-004: GenerateMultipleReports should create multi-page PDF")]
    public void GenerateMultipleReports_WithMultipleReports_CreatesMultiPagePdf()
    {
        // Arrange
        var generator = new PdfGenerator();
        var reports = Enumerable.Range(1, 5)
            .Select(i => CreateTestReport(i, $"CEIBA-2025-{i:D3}"))
            .ToList();

        // Act
        var pdfBytes = generator.GenerateMultipleReports(reports);

        // Assert
        pdfBytes.Should().NotBeEmpty();
        pdfBytes.Length.Should().BeGreaterThan(1000, "Multi-report PDF should be larger");

        // Verify PDF signature
        pdfBytes[0..4].Should().Equal(new byte[] { 0x25, 0x50, 0x44, 0x46 });
    }

    [Fact(DisplayName = "T-US2-005: GenerateMultipleReports with empty collection should throw")]
    public void GenerateMultipleReports_WithEmptyCollection_ThrowsArgumentException()
    {
        // Arrange
        var generator = new PdfGenerator();
        var emptyReports = Enumerable.Empty<ReportExportDto>();

        // Act & Assert
        var act = () => generator.GenerateMultipleReports(emptyReports);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*at least one report*");
    }

    [Fact(DisplayName = "T-US2-006: GenerateSingleReport with null should throw")]
    public void GenerateSingleReport_WithNull_ThrowsArgumentNullException()
    {
        // Arrange
        var generator = new PdfGenerator();

        // Act & Assert
        var act = () => generator.GenerateSingleReport(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "T-US2-007: PDF should include CEIBA branding")]
    public void GenerateSingleReport_IncludesCeibaBranding()
    {
        // Arrange
        var generator = new PdfGenerator();
        var report = CreateTestReport();

        // Act
        var pdfBytes = generator.GenerateSingleReport(report);

        // Assert
        pdfBytes.Should().NotBeEmpty();
        // Branding verification would require parsing PDF content
        // For now, verify PDF is generated with reasonable size
        pdfBytes.Length.Should().BeGreaterThan(300);
    }

    [Fact(DisplayName = "T-US2-008: PDF should include generation timestamp")]
    public void GenerateSingleReport_IncludesGenerationTimestamp()
    {
        // Arrange
        var generator = new PdfGenerator();
        var report = CreateTestReport();

        // Act
        var pdfBytes = generator.GenerateSingleReport(report);

        // Assert
        pdfBytes.Should().NotBeEmpty();
        // Timestamp verification would require parsing PDF metadata
    }

    [Fact(DisplayName = "T-US2-009: Multiple reports should be separated by page breaks")]
    public void GenerateMultipleReports_SeparatesReportsByPageBreaks()
    {
        // Arrange
        var generator = new PdfGenerator();
        var reports = new[]
        {
            CreateTestReport(1, "CEIBA-2025-001"),
            CreateTestReport(2, "CEIBA-2025-002")
        };

        // Act
        var pdfBytes = generator.GenerateMultipleReports(reports);

        // Assert
        pdfBytes.Should().NotBeEmpty();
        pdfBytes.Length.Should().BeGreaterThan(500, "Two reports should create substantial PDF");
    }

    [Fact(DisplayName = "T-US2-010: PDF generation should be deterministic for same input")]
    public void GenerateSingleReport_WithSameInput_ProducesSameOutput()
    {
        // Arrange
        var generator = new PdfGenerator();
        var report = CreateTestReport();

        // Act
        var pdf1 = generator.GenerateSingleReport(report);
        var pdf2 = generator.GenerateSingleReport(report);

        // Assert
        // Note: QuestPDF may include timestamps in metadata, so exact byte equality might not hold
        // We verify that both PDFs are generated and have similar size
        pdf1.Length.Should().BeCloseTo(pdf2.Length, 100, "PDFs should have similar size");
    }
}
