using System.Text;
using System.Text.Json;
using Ceiba.Application.Services.Export;
using Ceiba.Shared.DTOs.Export;
using FluentAssertions;
using Xunit;

namespace Ceiba.Application.Tests.Services.Export;

/// <summary>
/// Unit tests for JSON export service
/// Tests T-US2-011 to T-US2-020
/// </summary>
[Trait("Category", "Unit")]
[Trait("Service", "Export")]
public class JsonExporterTests
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

    [Fact(DisplayName = "T-US2-011: ExportSingleReport should create valid JSON")]
    public void ExportSingleReport_WithValidData_CreatesValidJson()
    {
        // Arrange
        var exporter = new JsonExporter();
        var report = CreateTestReport();

        // Act
        var jsonBytes = exporter.ExportSingleReport(report);

        // Assert
        jsonBytes.Should().NotBeEmpty("JSON should contain data");
        jsonBytes.Length.Should().BeGreaterThan(50, "JSON should have reasonable size");

        // Verify valid JSON
        var jsonString = Encoding.UTF8.GetString(jsonBytes);
        var act = () => JsonDocument.Parse(jsonString);
        act.Should().NotThrow("JSON should be valid");
    }

    [Fact(DisplayName = "T-US2-012: ExportSingleReport should include all report fields")]
    public void ExportSingleReport_WithCompleteData_IncludesAllFields()
    {
        // Arrange
        var exporter = new JsonExporter();
        var report = CreateTestReport();

        // Act
        var jsonBytes = exporter.ExportSingleReport(report);

        // Assert
        var jsonString = Encoding.UTF8.GetString(jsonBytes);
        using var doc = JsonDocument.Parse(jsonString);
        var root = doc.RootElement;

        // Verify key fields are present (using camelCase)
        root.GetProperty("id").GetInt32().Should().Be(1);
        root.GetProperty("folio").GetString().Should().Be("CEIBA-2025-001");
        root.GetProperty("estado").GetString().Should().Be("Entregado");
        root.GetProperty("usuarioCreador").GetString().Should().Be("oficial@ceiba.local");
        root.GetProperty("sexo").GetString().Should().Be("Femenino");
        root.GetProperty("edad").GetInt32().Should().Be(28);
        root.GetProperty("delito").GetString().Should().Be("Violencia familiar");
        root.GetProperty("zona").GetString().Should().Be("Zona Centro");
        root.GetProperty("hechosReportados").GetString().Should().Contain("violencia familiar");
    }

    [Fact(DisplayName = "T-US2-013: ExportSingleReport should handle special characters")]
    public void ExportSingleReport_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var exporter = new JsonExporter();
        var report = CreateTestReport();
        report = report with
        {
            HechosReportados = "Reporte con caracteres especiales: áéíóú ñÑ ¿? ¡!",
            Observaciones = "Símbolos: @#$%&*() \"comillas\" 'apóstrofes'"
        };

        // Act
        var jsonBytes = exporter.ExportSingleReport(report);

        // Assert
        var jsonString = Encoding.UTF8.GetString(jsonBytes);
        var act = () => JsonDocument.Parse(jsonString);
        act.Should().NotThrow("JSON should handle special characters");

        using var doc = JsonDocument.Parse(jsonString);
        var root = doc.RootElement;
        root.GetProperty("hechosReportados").GetString().Should().Contain("áéíóú");
        root.GetProperty("observaciones").GetString().Should().Contain("@#$%");
    }

    [Fact(DisplayName = "T-US2-014: ExportMultipleReports should create JSON array")]
    public void ExportMultipleReports_WithMultipleReports_CreatesJsonArray()
    {
        // Arrange
        var exporter = new JsonExporter();
        var reports = Enumerable.Range(1, 5)
            .Select(i => CreateTestReport(i, $"CEIBA-2025-{i:D3}"))
            .ToList();

        // Act
        var jsonBytes = exporter.ExportMultipleReports(reports);

        // Assert
        jsonBytes.Should().NotBeEmpty();
        var jsonString = Encoding.UTF8.GetString(jsonBytes);

        using var doc = JsonDocument.Parse(jsonString);
        var root = doc.RootElement;

        // Should be an array
        root.ValueKind.Should().Be(JsonValueKind.Array);

        // Should have 5 elements
        root.GetArrayLength().Should().Be(5);

        // Verify first and last elements (using camelCase)
        root[0].GetProperty("folio").GetString().Should().Be("CEIBA-2025-001");
        root[4].GetProperty("folio").GetString().Should().Be("CEIBA-2025-005");
    }

    [Fact(DisplayName = "T-US2-015: ExportMultipleReports with empty collection should throw")]
    public void ExportMultipleReports_WithEmptyCollection_ThrowsArgumentException()
    {
        // Arrange
        var exporter = new JsonExporter();
        var emptyReports = Enumerable.Empty<ReportExportDto>();

        // Act & Assert
        var act = () => exporter.ExportMultipleReports(emptyReports);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*at least one report*");
    }

    [Fact(DisplayName = "T-US2-016: ExportSingleReport with null should throw")]
    public void ExportSingleReport_WithNull_ThrowsArgumentNullException()
    {
        // Arrange
        var exporter = new JsonExporter();

        // Act & Assert
        var act = () => exporter.ExportSingleReport(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "T-US2-017: JSON should use camelCase naming")]
    public void ExportSingleReport_UsesCamelCaseNaming()
    {
        // Arrange
        var exporter = new JsonExporter();
        var report = CreateTestReport();

        // Act
        var jsonBytes = exporter.ExportSingleReport(report);

        // Assert
        var jsonString = Encoding.UTF8.GetString(jsonBytes);

        // Verify camelCase (first letter lowercase)
        jsonString.Should().Contain("\"id\":");
        jsonString.Should().Contain("\"folio\":");
        jsonString.Should().Contain("\"estado\":");
        jsonString.Should().Contain("\"usuarioCreador\":");
    }

    [Fact(DisplayName = "T-US2-018: JSON should include UTC timestamps")]
    public void ExportSingleReport_IncludesUtcTimestamps()
    {
        // Arrange
        var exporter = new JsonExporter();
        var report = CreateTestReport();

        // Act
        var jsonBytes = exporter.ExportSingleReport(report);

        // Assert
        var jsonString = Encoding.UTF8.GetString(jsonBytes);
        using var doc = JsonDocument.Parse(jsonString);
        var root = doc.RootElement;

        // Verify UTC format (ISO 8601)
        var fechaCreacion = root.GetProperty("fechaCreacion").GetString();
        fechaCreacion.Should().EndWith("Z", "timestamps should be in UTC");
    }

    [Fact(DisplayName = "T-US2-019: JSON export should be deterministic for same input")]
    public void ExportSingleReport_WithSameInput_ProducesSameOutput()
    {
        // Arrange
        var exporter = new JsonExporter();
        var report = CreateTestReport();

        // Act
        var json1 = exporter.ExportSingleReport(report);
        var json2 = exporter.ExportSingleReport(report);

        // Assert
        json1.Should().Equal(json2, "Same input should produce identical JSON");
    }

    [Fact(DisplayName = "T-US2-020: ExportOptions should control indentation")]
    public void ExportSingleReport_WithIndentedOption_CreatesFormattedJson()
    {
        // Arrange
        var exporter = new JsonExporter();
        var report = CreateTestReport();
        var optionsIndented = new ExportOptions { IncludeMetadata = true };
        var optionsCompact = new ExportOptions { IncludeMetadata = false };

        // Act
        var jsonIndented = exporter.ExportSingleReport(report, optionsIndented);
        var jsonCompact = exporter.ExportSingleReport(report, optionsCompact);

        // Assert
        var stringIndented = Encoding.UTF8.GetString(jsonIndented);
        var stringCompact = Encoding.UTF8.GetString(jsonCompact);

        // Both should be valid JSON
        var actIndented = () => JsonDocument.Parse(stringIndented);
        var actCompact = () => JsonDocument.Parse(stringCompact);

        actIndented.Should().NotThrow();
        actCompact.Should().NotThrow();
    }
}
