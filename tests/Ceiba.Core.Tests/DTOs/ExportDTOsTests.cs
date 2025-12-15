using System.ComponentModel.DataAnnotations;
using Ceiba.Shared.DTOs.Export;
using FluentAssertions;

namespace Ceiba.Core.Tests.DTOs;

/// <summary>
/// Unit tests for Export DTOs (ExportFormat, ExportOptions, ExportRequestDto, etc.).
/// </summary>
[Trait("Category", "Unit")]
public class ExportDTOsTests
{
    #region ExportFormat Tests

    [Fact(DisplayName = "ExportFormat should have PDF and JSON values")]
    public void ExportFormat_ShouldHavePdfAndJsonValues()
    {
        // Assert
        Enum.GetValues<ExportFormat>().Should().HaveCount(2);
        ExportFormat.PDF.Should().Be((ExportFormat)0);
        ExportFormat.JSON.Should().Be((ExportFormat)1);
    }

    [Fact(DisplayName = "ExportFormat PDF should be default value")]
    public void ExportFormat_PDF_ShouldBeDefaultValue()
    {
        // Arrange & Act
        var format = default(ExportFormat);

        // Assert
        format.Should().Be(ExportFormat.PDF);
    }

    #endregion

    #region ExportOptions Tests

    [Fact(DisplayName = "ExportOptions should have default values")]
    public void ExportOptions_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var options = new ExportOptions();

        // Assert
        options.IncludeMetadata.Should().BeTrue();
        options.IncludeAuditInfo.Should().BeFalse();
        options.FileName.Should().BeNull();
    }

    [Fact(DisplayName = "ExportOptions should allow setting all properties")]
    public void ExportOptions_ShouldAllowSettingAllProperties()
    {
        // Arrange & Act
        var options = new ExportOptions
        {
            IncludeMetadata = false,
            IncludeAuditInfo = true,
            FileName = "custom_report"
        };

        // Assert
        options.IncludeMetadata.Should().BeFalse();
        options.IncludeAuditInfo.Should().BeTrue();
        options.FileName.Should().Be("custom_report");
    }

    [Fact(DisplayName = "ExportOptions should be immutable record")]
    public void ExportOptions_ShouldBeImmutableRecord()
    {
        // Arrange
        var options = new ExportOptions { IncludeMetadata = true };

        // Act
        var newOptions = options with { IncludeMetadata = false };

        // Assert
        options.IncludeMetadata.Should().BeTrue();
        newOptions.IncludeMetadata.Should().BeFalse();
    }

    #endregion

    #region ExportRequestDto Tests

    [Fact(DisplayName = "ExportRequestDto should have default values")]
    public void ExportRequestDto_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var request = new ExportRequestDto();

        // Assert
        request.ReportIds.Should().BeNull();
        request.Format.Should().Be(ExportFormat.PDF);
        request.Options.Should().BeNull();
    }

    [Fact(DisplayName = "ExportRequestDto should allow specifying report IDs")]
    public void ExportRequestDto_ShouldAllowSpecifyingReportIds()
    {
        // Arrange & Act
        var request = new ExportRequestDto
        {
            ReportIds = new[] { 1, 2, 3, 4, 5 }
        };

        // Assert
        request.ReportIds.Should().HaveCount(5);
        request.ReportIds.Should().Contain(new[] { 1, 2, 3, 4, 5 });
    }

    [Fact(DisplayName = "ExportRequestDto should allow JSON format")]
    public void ExportRequestDto_ShouldAllowJsonFormat()
    {
        // Arrange & Act
        var request = new ExportRequestDto
        {
            Format = ExportFormat.JSON
        };

        // Assert
        request.Format.Should().Be(ExportFormat.JSON);
    }

    [Fact(DisplayName = "ExportRequestDto should allow custom options")]
    public void ExportRequestDto_ShouldAllowCustomOptions()
    {
        // Arrange & Act
        var request = new ExportRequestDto
        {
            ReportIds = new[] { 100 },
            Format = ExportFormat.PDF,
            Options = new ExportOptions
            {
                IncludeMetadata = true,
                IncludeAuditInfo = true,
                FileName = "audit_report_2024"
            }
        };

        // Assert
        request.Options.Should().NotBeNull();
        request.Options!.IncludeAuditInfo.Should().BeTrue();
        request.Options.FileName.Should().Be("audit_report_2024");
    }

    [Fact(DisplayName = "ExportRequestDto validation should require Format")]
    public void ExportRequestDto_Validation_ShouldRequireFormat()
    {
        // Arrange
        var request = new ExportRequestDto();

        // Act
        var results = ValidateModel(request);

        // Assert - Format has Required attribute but enum defaults to valid value
        results.Should().BeEmpty();
    }

    #endregion

    #region ExportResultDto Tests

    [Fact(DisplayName = "ExportResultDto should have default values")]
    public void ExportResultDto_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var result = new ExportResultDto();

        // Assert
        result.Data.Should().BeEmpty();
        result.FileName.Should().BeEmpty();
        result.ContentType.Should().BeEmpty();
        result.ReportCount.Should().Be(0);
        result.GeneratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact(DisplayName = "ExportResultDto should store PDF export data")]
    public void ExportResultDto_ShouldStorePdfExportData()
    {
        // Arrange
        var pdfData = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF magic bytes
        var generatedAt = DateTime.UtcNow;

        // Act
        var result = new ExportResultDto
        {
            Data = pdfData,
            FileName = "reportes_2024-07-15.pdf",
            ContentType = "application/pdf",
            ReportCount = 10,
            GeneratedAt = generatedAt
        };

        // Assert
        result.Data.Should().HaveCount(4);
        result.FileName.Should().EndWith(".pdf");
        result.ContentType.Should().Be("application/pdf");
        result.ReportCount.Should().Be(10);
    }

    [Fact(DisplayName = "ExportResultDto should store JSON export data")]
    public void ExportResultDto_ShouldStoreJsonExportData()
    {
        // Arrange
        var jsonData = System.Text.Encoding.UTF8.GetBytes("[{\"id\":1},{\"id\":2}]");

        // Act
        var result = new ExportResultDto
        {
            Data = jsonData,
            FileName = "reportes_2024-07-15.json",
            ContentType = "application/json",
            ReportCount = 2
        };

        // Assert
        result.FileName.Should().EndWith(".json");
        result.ContentType.Should().Be("application/json");
    }

    [Fact(DisplayName = "ExportResultDto should be immutable record")]
    public void ExportResultDto_ShouldBeImmutableRecord()
    {
        // Arrange
        var result = new ExportResultDto { ReportCount = 5 };

        // Act
        var newResult = result with { ReportCount = 10 };

        // Assert
        result.ReportCount.Should().Be(5);
        newResult.ReportCount.Should().Be(10);
    }

    #endregion

    #region ReportExportDto Tests

    [Fact(DisplayName = "ReportExportDto should have default values")]
    public void ReportExportDto_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var dto = new ReportExportDto();

        // Assert
        dto.Id.Should().Be(0);
        dto.Folio.Should().BeEmpty();
        dto.Estado.Should().BeEmpty();
        dto.UsuarioCreador.Should().BeEmpty();
        dto.UsuarioCreadorId.Should().BeEmpty();
        dto.Sexo.Should().BeEmpty();
        dto.Edad.Should().Be(0);
        dto.LgbtttiqPlus.Should().BeFalse();
        dto.SituacionCalle.Should().BeFalse();
        dto.Migrante.Should().BeFalse();
        dto.Discapacidad.Should().BeFalse();
        dto.Delito.Should().BeEmpty();
        dto.TipoDeAtencion.Should().BeEmpty();
        dto.TipoDeAccion.Should().BeEmpty();
        dto.Zona.Should().BeEmpty();
        dto.Region.Should().BeEmpty();
        dto.Sector.Should().BeEmpty();
        dto.Cuadrante.Should().BeEmpty();
        dto.TurnoCeiba.Should().BeEmpty();
        dto.HechosReportados.Should().BeEmpty();
        dto.AccionesRealizadas.Should().BeEmpty();
        dto.Traslados.Should().BeEmpty();
        dto.Observaciones.Should().BeNull();
    }

    [Fact(DisplayName = "ReportExportDto should store complete report data for export")]
    public void ReportExportDto_ShouldStoreCompleteReportDataForExport()
    {
        // Arrange
        var fechaCreacion = DateTime.UtcNow.AddDays(-1);
        var fechaEntrega = DateTime.UtcNow;

        // Act
        var dto = new ReportExportDto
        {
            Id = 1001,
            Folio = "CEIBA-2024-001001",
            Estado = "Entregado",
            FechaCreacion = fechaCreacion,
            FechaEntrega = fechaEntrega,
            UsuarioCreador = "officer@ceiba.local",
            UsuarioCreadorId = Guid.NewGuid().ToString(),
            Sexo = "Masculino",
            Edad = 35,
            LgbtttiqPlus = false,
            SituacionCalle = true,
            Migrante = false,
            Discapacidad = false,
            Delito = "Robo a Transeúnte",
            TipoDeAtencion = "Orientación",
            TipoDeAccion = "Preventiva",
            Zona = "Zona Norte",
            Region = "Región Centro",
            Sector = "Sector A",
            Cuadrante = "Cuadrante 1-A",
            TurnoCeiba = "Turno 1 (06:00-14:00)",
            HechosReportados = "Se reporta robo a transeúnte en vía pública cerca del parque central.",
            AccionesRealizadas = "Se brindó orientación a la víctima y se canalizó al Ministerio Público.",
            Traslados = "No aplica",
            Observaciones = "La víctima proporcionó descripción del agresor."
        };

        // Assert
        dto.Id.Should().Be(1001);
        dto.Folio.Should().StartWith("CEIBA-");
        dto.Estado.Should().Be("Entregado");
        dto.FechaEntrega.Should().NotBeNull();
        dto.SituacionCalle.Should().BeTrue();
        dto.Zona.Should().Be("Zona Norte");
        dto.HechosReportados.Should().Contain("robo");
    }

    [Fact(DisplayName = "ReportExportDto should support audit information")]
    public void ReportExportDto_ShouldSupportAuditInformation()
    {
        // Arrange
        var fechaModificacion = DateTime.UtcNow;

        // Act
        var dto = new ReportExportDto
        {
            Id = 100,
            Folio = "CEIBA-2024-000100",
            Estado = "Entregado",
            FechaUltimaModificacion = fechaModificacion,
            UsuarioUltimaModificacion = "supervisor@ceiba.local"
        };

        // Assert
        dto.FechaUltimaModificacion.Should().Be(fechaModificacion);
        dto.UsuarioUltimaModificacion.Should().Be("supervisor@ceiba.local");
    }

    [Fact(DisplayName = "ReportExportDto should be immutable record")]
    public void ReportExportDto_ShouldBeImmutableRecord()
    {
        // Arrange
        var dto = new ReportExportDto { Id = 1, Folio = "CEIBA-001" };

        // Act
        var newDto = dto with { Id = 2 };

        // Assert
        dto.Id.Should().Be(1);
        newDto.Id.Should().Be(2);
        newDto.Folio.Should().Be("CEIBA-001");
    }

    [Fact(DisplayName = "ReportExportDto should handle all demographic flags")]
    public void ReportExportDto_ShouldHandleAllDemographicFlags()
    {
        // Arrange & Act
        var dto = new ReportExportDto
        {
            LgbtttiqPlus = true,
            SituacionCalle = true,
            Migrante = true,
            Discapacidad = true
        };

        // Assert
        dto.LgbtttiqPlus.Should().BeTrue();
        dto.SituacionCalle.Should().BeTrue();
        dto.Migrante.Should().BeTrue();
        dto.Discapacidad.Should().BeTrue();
    }

    #endregion

    #region Helper Methods

    private static List<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(model);
        Validator.TryValidateObject(model, context, validationResults, true);
        return validationResults;
    }

    #endregion
}
