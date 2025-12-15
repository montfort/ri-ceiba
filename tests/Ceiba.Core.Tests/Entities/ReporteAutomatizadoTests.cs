using Ceiba.Core.Entities;
using FluentAssertions;

namespace Ceiba.Core.Tests.Entities;

/// <summary>
/// Unit tests for ReporteAutomatizado entity.
/// Tests automated daily summary report functionality.
/// US4: Reportes Automatizados Diarios con IA.
/// </summary>
[Trait("Category", "Unit")]
public class ReporteAutomatizadoTests
{
    #region Default Value Tests

    [Fact(DisplayName = "ReporteAutomatizado should have Id default to 0")]
    public void ReporteAutomatizado_Id_ShouldDefaultToZero()
    {
        // Arrange & Act
        var reporte = new ReporteAutomatizado();

        // Assert
        reporte.Id.Should().Be(0);
    }

    [Fact(DisplayName = "ReporteAutomatizado should have ContenidoMarkdown default to empty string")]
    public void ReporteAutomatizado_ContenidoMarkdown_ShouldDefaultToEmptyString()
    {
        // Arrange & Act
        var reporte = new ReporteAutomatizado();

        // Assert
        reporte.ContenidoMarkdown.Should().BeEmpty();
    }

    [Fact(DisplayName = "ReporteAutomatizado should have ContenidoWordPath default to null")]
    public void ReporteAutomatizado_ContenidoWordPath_ShouldDefaultToNull()
    {
        // Arrange & Act
        var reporte = new ReporteAutomatizado();

        // Assert
        reporte.ContenidoWordPath.Should().BeNull();
    }

    [Fact(DisplayName = "ReporteAutomatizado should have Estadisticas default to empty JSON object")]
    public void ReporteAutomatizado_Estadisticas_ShouldDefaultToEmptyJsonObject()
    {
        // Arrange & Act
        var reporte = new ReporteAutomatizado();

        // Assert
        reporte.Estadisticas.Should().Be("{}");
    }

    [Fact(DisplayName = "ReporteAutomatizado should have Enviado default to false")]
    public void ReporteAutomatizado_Enviado_ShouldDefaultToFalse()
    {
        // Arrange & Act
        var reporte = new ReporteAutomatizado();

        // Assert
        reporte.Enviado.Should().BeFalse();
    }

    [Fact(DisplayName = "ReporteAutomatizado should have FechaEnvio default to null")]
    public void ReporteAutomatizado_FechaEnvio_ShouldDefaultToNull()
    {
        // Arrange & Act
        var reporte = new ReporteAutomatizado();

        // Assert
        reporte.FechaEnvio.Should().BeNull();
    }

    [Fact(DisplayName = "ReporteAutomatizado should have ErrorMensaje default to null")]
    public void ReporteAutomatizado_ErrorMensaje_ShouldDefaultToNull()
    {
        // Arrange & Act
        var reporte = new ReporteAutomatizado();

        // Assert
        reporte.ErrorMensaje.Should().BeNull();
    }

    [Fact(DisplayName = "ReporteAutomatizado should have ModeloReporteId default to null")]
    public void ReporteAutomatizado_ModeloReporteId_ShouldDefaultToNull()
    {
        // Arrange & Act
        var reporte = new ReporteAutomatizado();

        // Assert
        reporte.ModeloReporteId.Should().BeNull();
    }

    [Fact(DisplayName = "ReporteAutomatizado should have CreatedAt set to UTC now")]
    public void ReporteAutomatizado_CreatedAt_ShouldDefaultToUtcNow()
    {
        // Arrange
        var before = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var reporte = new ReporteAutomatizado();

        // Assert
        reporte.CreatedAt.Should().BeAfter(before);
        reporte.CreatedAt.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));
    }

    #endregion

    #region Reporting Period Tests

    [Fact(DisplayName = "ReporteAutomatizado should set reporting period dates")]
    public void ReporteAutomatizado_ShouldSetReportingPeriodDates()
    {
        // Arrange
        var reporte = new ReporteAutomatizado();
        var fechaInicio = new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var fechaFin = new DateTime(2024, 7, 2, 0, 0, 0, DateTimeKind.Utc);

        // Act
        reporte.FechaInicio = fechaInicio;
        reporte.FechaFin = fechaFin;

        // Assert
        reporte.FechaInicio.Should().Be(fechaInicio);
        reporte.FechaFin.Should().Be(fechaFin);
    }

    [Fact(DisplayName = "ReporteAutomatizado reporting period should span one day")]
    public void ReporteAutomatizado_ReportingPeriod_ShouldSpanOneDay()
    {
        // Arrange
        var fechaInicio = new DateTime(2024, 7, 15, 0, 0, 0, DateTimeKind.Utc);
        var fechaFin = new DateTime(2024, 7, 16, 0, 0, 0, DateTimeKind.Utc);

        var reporte = new ReporteAutomatizado
        {
            FechaInicio = fechaInicio,
            FechaFin = fechaFin
        };

        // Assert
        var duration = reporte.FechaFin - reporte.FechaInicio;
        duration.TotalDays.Should().Be(1);
    }

    [Fact(DisplayName = "ReporteAutomatizado should support weekly reporting periods")]
    public void ReporteAutomatizado_ShouldSupportWeeklyReportingPeriods()
    {
        // Arrange
        var fechaInicio = new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var fechaFin = new DateTime(2024, 7, 8, 0, 0, 0, DateTimeKind.Utc);

        var reporte = new ReporteAutomatizado
        {
            FechaInicio = fechaInicio,
            FechaFin = fechaFin
        };

        // Assert
        var duration = reporte.FechaFin - reporte.FechaInicio;
        duration.TotalDays.Should().Be(7);
    }

    #endregion

    #region Markdown Content Tests

    [Fact(DisplayName = "ReporteAutomatizado should store markdown content")]
    public void ReporteAutomatizado_ShouldStoreMarkdownContent()
    {
        // Arrange
        var reporte = new ReporteAutomatizado();
        var markdown = @"# Reporte Diario de Incidencias
## Período: 2024-07-15

### Resumen Estadístico
- Total de reportes: 45
- Delitos más frecuentes: Robo (15), Vandalismo (10)

### Narrativa
Durante el período reportado se observó...";

        // Act
        reporte.ContenidoMarkdown = markdown;

        // Assert
        reporte.ContenidoMarkdown.Should().Be(markdown);
        reporte.ContenidoMarkdown.Should().Contain("# Reporte Diario");
        reporte.ContenidoMarkdown.Should().Contain("Total de reportes");
    }

    #endregion

    #region Statistics JSON Tests

    [Fact(DisplayName = "ReporteAutomatizado should store statistics in JSON format")]
    public void ReporteAutomatizado_ShouldStoreStatisticsInJsonFormat()
    {
        // Arrange
        var reporte = new ReporteAutomatizado();
        var estadisticas = @"{
            ""total_reportes"": 45,
            ""por_delito"": {""Robo"": 15, ""Vandalismo"": 10, ""Otros"": 20},
            ""por_zona"": {""Norte"": 20, ""Sur"": 15, ""Centro"": 10},
            ""por_sexo"": {""Masculino"": 30, ""Femenino"": 15}
        }";

        // Act
        reporte.Estadisticas = estadisticas;

        // Assert
        reporte.Estadisticas.Should().Contain("total_reportes");
        reporte.Estadisticas.Should().Contain("por_delito");
        reporte.Estadisticas.Should().Contain("por_zona");
    }

    #endregion

    #region Email Delivery Tests

    [Fact(DisplayName = "ReporteAutomatizado should track successful email delivery")]
    public void ReporteAutomatizado_ShouldTrackSuccessfulEmailDelivery()
    {
        // Arrange
        var reporte = new ReporteAutomatizado();
        var fechaEnvio = DateTime.UtcNow;

        // Act
        reporte.Enviado = true;
        reporte.FechaEnvio = fechaEnvio;
        reporte.ErrorMensaje = null;

        // Assert
        reporte.Enviado.Should().BeTrue();
        reporte.FechaEnvio.Should().Be(fechaEnvio);
        reporte.ErrorMensaje.Should().BeNull();
    }

    [Fact(DisplayName = "ReporteAutomatizado should track failed email delivery")]
    public void ReporteAutomatizado_ShouldTrackFailedEmailDelivery()
    {
        // Arrange
        var reporte = new ReporteAutomatizado();
        var errorMessage = "SMTP server connection failed: timeout after 30 seconds";

        // Act
        reporte.Enviado = false;
        reporte.FechaEnvio = null;
        reporte.ErrorMensaje = errorMessage;

        // Assert
        reporte.Enviado.Should().BeFalse();
        reporte.FechaEnvio.Should().BeNull();
        reporte.ErrorMensaje.Should().Be(errorMessage);
    }

    #endregion

    #region Word Document Tests

    [Fact(DisplayName = "ReporteAutomatizado should store Word document path")]
    public void ReporteAutomatizado_ShouldStoreWordDocumentPath()
    {
        // Arrange
        var reporte = new ReporteAutomatizado();
        var wordPath = "/generated-reports/2024-07-15/reporte_diario_20240715.docx";

        // Act
        reporte.ContenidoWordPath = wordPath;

        // Assert
        reporte.ContenidoWordPath.Should().Be(wordPath);
    }

    [Fact(DisplayName = "ReporteAutomatizado should allow null Word path when generation fails")]
    public void ReporteAutomatizado_ShouldAllowNullWordPathWhenGenerationFails()
    {
        // Arrange
        var reporte = new ReporteAutomatizado
        {
            ContenidoMarkdown = "# Report content",
            ContenidoWordPath = null,
            ErrorMensaje = "Word generation failed: LibreOffice not available"
        };

        // Assert
        reporte.ContenidoWordPath.Should().BeNull();
        reporte.ErrorMensaje.Should().Contain("Word generation failed");
    }

    #endregion

    #region Template Navigation Tests

    [Fact(DisplayName = "ReporteAutomatizado should reference ModeloReporte by Id")]
    public void ReporteAutomatizado_ShouldReferenceModeloReporteById()
    {
        // Arrange
        var reporte = new ReporteAutomatizado();

        // Act
        reporte.ModeloReporteId = 5;

        // Assert
        reporte.ModeloReporteId.Should().Be(5);
    }

    [Fact(DisplayName = "ReporteAutomatizado should allow setting navigation property")]
    public void ReporteAutomatizado_ShouldAllowSettingNavigationProperty()
    {
        // Arrange
        var modelo = new ModeloReporte
        {
            Id = 1,
            Nombre = "Plantilla Diaria",
            Activo = true
        };
        var reporte = new ReporteAutomatizado
        {
            ModeloReporteId = 1
        };

        // Act
        reporte.ModeloReporte = modelo;

        // Assert
        reporte.ModeloReporte.Should().Be(modelo);
        reporte.ModeloReporte.Nombre.Should().Be("Plantilla Diaria");
    }

    [Fact(DisplayName = "ReporteAutomatizado should allow null ModeloReporteId for default template")]
    public void ReporteAutomatizado_ShouldAllowNullModeloReporteIdForDefaultTemplate()
    {
        // Arrange
        var reporte = new ReporteAutomatizado
        {
            ModeloReporteId = null,
            ModeloReporte = null
        };

        // Assert
        reporte.ModeloReporteId.Should().BeNull();
        reporte.ModeloReporte.Should().BeNull();
    }

    #endregion

    #region Complete Report Tests

    [Fact(DisplayName = "ReporteAutomatizado should allow setting all properties")]
    public void ReporteAutomatizado_ShouldAllowSettingAllProperties()
    {
        // Arrange
        var fechaInicio = new DateTime(2024, 7, 15, 0, 0, 0, DateTimeKind.Utc);
        var fechaFin = new DateTime(2024, 7, 16, 0, 0, 0, DateTimeKind.Utc);
        var fechaEnvio = new DateTime(2024, 7, 16, 6, 30, 0, DateTimeKind.Utc);
        var createdAt = new DateTime(2024, 7, 16, 6, 0, 0, DateTimeKind.Utc);

        // Act
        var reporte = new ReporteAutomatizado
        {
            Id = 100,
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
            ContenidoMarkdown = "# Reporte del 15 de Julio",
            ContenidoWordPath = "/reports/2024-07-15.docx",
            Estadisticas = "{\"total\": 50}",
            Enviado = true,
            FechaEnvio = fechaEnvio,
            ErrorMensaje = null,
            ModeloReporteId = 1,
            CreatedAt = createdAt
        };

        // Assert
        reporte.Id.Should().Be(100);
        reporte.FechaInicio.Should().Be(fechaInicio);
        reporte.FechaFin.Should().Be(fechaFin);
        reporte.ContenidoMarkdown.Should().Contain("Reporte del 15 de Julio");
        reporte.ContenidoWordPath.Should().EndWith(".docx");
        reporte.Estadisticas.Should().Contain("total");
        reporte.Enviado.Should().BeTrue();
        reporte.FechaEnvio.Should().Be(fechaEnvio);
        reporte.ErrorMensaje.Should().BeNull();
        reporte.ModeloReporteId.Should().Be(1);
        reporte.CreatedAt.Should().Be(createdAt);
    }

    #endregion

    #region Inheritance Tests

    [Fact(DisplayName = "ReporteAutomatizado should inherit from BaseEntity")]
    public void ReporteAutomatizado_ShouldInheritFromBaseEntity()
    {
        // Arrange & Act
        var reporte = new ReporteAutomatizado();

        // Assert
        reporte.Should().BeAssignableTo<BaseEntity>();
    }

    #endregion
}
