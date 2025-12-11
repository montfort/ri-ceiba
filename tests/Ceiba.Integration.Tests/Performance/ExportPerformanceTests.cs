using Ceiba.Application.Services.Export;
using Ceiba.Shared.DTOs.Export;
using Xunit.Abstractions;

namespace Ceiba.Integration.Tests.Performance;

/// <summary>
/// T122: Export performance tests - PDF export must complete in under 30 seconds.
/// </summary>
[Trait("Category", "Performance")]
public class ExportPerformanceTests : PerformanceTestBase
{
    private static readonly TimeSpan PdfExportThreshold = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan JsonExportThreshold = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan SingleReportThreshold = TimeSpan.FromSeconds(5);

    public ExportPerformanceTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    [Trait("NFR", "T122")]
    public async Task ExportSingleReportToPdf_CompletesUnder5Seconds()
    {
        // Arrange
        var pdfGenerator = new PdfGenerator();
        var report = CreateSampleReport(1);

        // Act & Assert
        await MeasureAsync(
            "Export single report to PDF",
            async () =>
            {
                var pdf = pdfGenerator.GenerateSingleReport(report);
                await Task.CompletedTask;
                return pdf;
            },
            SingleReportThreshold);
    }

    [Fact]
    [Trait("NFR", "T122")]
    public async Task ExportMultipleReportsToPdf_CompletesUnder30Seconds()
    {
        // Arrange
        var pdfGenerator = new PdfGenerator();
        var reports = Enumerable.Range(1, 50).Select(CreateSampleReport).ToList();

        // Act & Assert
        await MeasureAsync(
            "Export 50 reports to PDF",
            async () =>
            {
                var pdf = pdfGenerator.GenerateMultipleReports(reports);
                await Task.CompletedTask;
                return pdf;
            },
            PdfExportThreshold);
    }

    [Fact]
    [Trait("NFR", "T122")]
    public async Task ExportLargeReportToPdf_CompletesUnder10Seconds()
    {
        // Arrange
        var pdfGenerator = new PdfGenerator();
        var report = CreateLargeReport();

        // Act & Assert
        await MeasureAsync(
            "Export large report to PDF",
            async () =>
            {
                var pdf = pdfGenerator.GenerateSingleReport(report);
                await Task.CompletedTask;
                return pdf;
            },
            TimeSpan.FromSeconds(10));
    }

    [Fact]
    [Trait("NFR", "T122")]
    public async Task ExportSingleReportToJson_CompletesUnder1Second()
    {
        // Arrange
        var jsonExporter = new JsonExporter();
        var report = CreateSampleReport(1);

        // Act & Assert
        await MeasureAsync(
            "Export single report to JSON",
            async () =>
            {
                var json = jsonExporter.ExportSingleReport(report);
                await Task.CompletedTask;
                return json;
            },
            TimeSpan.FromSeconds(1));
    }

    [Fact]
    [Trait("NFR", "T122")]
    public async Task ExportMultipleReportsToJson_CompletesUnder5Seconds()
    {
        // Arrange
        var jsonExporter = new JsonExporter();
        var reports = Enumerable.Range(1, 100).Select(CreateSampleReport).ToList();

        // Act & Assert
        await MeasureAsync(
            "Export 100 reports to JSON",
            async () =>
            {
                var json = jsonExporter.ExportMultipleReports(reports);
                await Task.CompletedTask;
                return json;
            },
            JsonExportThreshold);
    }

    [Fact]
    [Trait("NFR", "T122")]
    public async Task PdfGeneration_MultipleIterations_MaintainsPerformance()
    {
        // Arrange
        var pdfGenerator = new PdfGenerator();
        var report = CreateSampleReport(1);

        // Act
        var stats = await RunIterationsAsync(
            "PDF generation (10 iterations)",
            async () =>
            {
                pdfGenerator.GenerateSingleReport(report);
                await Task.CompletedTask;
            },
            iterations: 10);

        // Assert
        Assert.True(stats.P95Ms < SingleReportThreshold.TotalMilliseconds,
            $"P95 generation time ({stats.P95Ms:F2}ms) exceeds threshold ({SingleReportThreshold.TotalMilliseconds}ms)");
    }

    [Fact]
    [Trait("NFR", "T122")]
    public async Task JsonExport_MultipleIterations_MaintainsPerformance()
    {
        // Arrange
        var jsonExporter = new JsonExporter();
        var reports = Enumerable.Range(1, 50).Select(CreateSampleReport).ToList();

        // Act
        var stats = await RunIterationsAsync(
            "JSON export (10 iterations)",
            async () =>
            {
                jsonExporter.ExportMultipleReports(reports);
                await Task.CompletedTask;
            },
            iterations: 10);

        // Assert
        Assert.True(stats.P95Ms < JsonExportThreshold.TotalMilliseconds,
            $"P95 export time ({stats.P95Ms:F2}ms) exceeds threshold ({JsonExportThreshold.TotalMilliseconds}ms)");
    }

    private static ReportExportDto CreateSampleReport(int id)
    {
        return new ReportExportDto
        {
            Id = id,
            Folio = $"RI-2025-{id:D6}",
            Estado = "Borrador",
            FechaCreacion = DateTime.UtcNow.AddDays(-id),
            UsuarioCreador = $"usuario{id}@ceiba.local",
            UsuarioCreadorId = Guid.NewGuid().ToString(),
            Sexo = "Masculino",
            Edad = 30 + (id % 40),
            LgbtttiqPlus = id % 5 == 0,
            SituacionCalle = id % 10 == 0,
            Migrante = id % 8 == 0,
            Discapacidad = id % 12 == 0,
            Delito = "Robo a transeúnte",
            TipoDeAtencion = "Atención inmediata",
            TipoDeAccion = "Patrullaje",
            Zona = "Zona Norte",
            Sector = "Sector A",
            Cuadrante = "Cuadrante 1",
            TurnoCeiba = "Matutino",
            HechosReportados = $"Se reporta incidente #{id}. El denunciante manifiesta que el día de los hechos " +
                              "se encontraba transitando por la vía pública cuando fue abordado por dos personas " +
                              "quienes mediante amenazas le sustrajeron sus pertenencias.",
            AccionesRealizadas = "Se realizó recorrido por la zona. Se entrevistó a testigos. " +
                                "Se coordinó con unidades de vigilancia.",
            Traslados = "No se requirió traslado",
            Observaciones = "Se sugiere aumentar patrullajes en la zona."
        };
    }

    private static ReportExportDto CreateLargeReport()
    {
        var longText = string.Join("\n\n", Enumerable.Range(1, 50).Select(i =>
            $"Párrafo {i}: Lorem ipsum dolor sit amet, consectetur adipiscing elit. " +
            "Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. " +
            "Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris."));

        return new ReportExportDto
        {
            Id = 999,
            Folio = "RI-2025-999999",
            Estado = "Entregado",
            FechaCreacion = DateTime.UtcNow.AddDays(-1),
            FechaEntrega = DateTime.UtcNow,
            UsuarioCreador = "supervisor@ceiba.local",
            UsuarioCreadorId = Guid.NewGuid().ToString(),
            Sexo = "Femenino",
            Edad = 35,
            LgbtttiqPlus = false,
            SituacionCalle = false,
            Migrante = false,
            Discapacidad = false,
            Delito = "Asalto con violencia",
            TipoDeAtencion = "Atención inmediata",
            TipoDeAccion = "Intervención",
            Zona = "Zona Norte",
            Sector = "Sector A",
            Cuadrante = "Cuadrante 1",
            TurnoCeiba = "Vespertino",
            HechosReportados = longText,
            AccionesRealizadas = longText,
            Traslados = "Traslado a hospital",
            Observaciones = longText
        };
    }
}
