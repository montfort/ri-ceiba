using Ceiba.Core.Entities;
using FluentAssertions;

namespace Ceiba.Core.Tests.Entities;

/// <summary>
/// Unit tests for ModeloReporte entity.
/// Tests automated report template functionality.
/// US4: Reportes Automatizados Diarios con IA.
/// </summary>
[Trait("Category", "Unit")]
public class ModeloReporteTests
{
    #region Default Value Tests

    [Fact(DisplayName = "ModeloReporte should have Id default to 0")]
    public void ModeloReporte_Id_ShouldDefaultToZero()
    {
        // Arrange & Act
        var modelo = new ModeloReporte();

        // Assert
        modelo.Id.Should().Be(0);
    }

    [Fact(DisplayName = "ModeloReporte should have Nombre default to empty string")]
    public void ModeloReporte_Nombre_ShouldDefaultToEmptyString()
    {
        // Arrange & Act
        var modelo = new ModeloReporte();

        // Assert
        modelo.Nombre.Should().BeEmpty();
    }

    [Fact(DisplayName = "ModeloReporte should have Descripcion default to null")]
    public void ModeloReporte_Descripcion_ShouldDefaultToNull()
    {
        // Arrange & Act
        var modelo = new ModeloReporte();

        // Assert
        modelo.Descripcion.Should().BeNull();
    }

    [Fact(DisplayName = "ModeloReporte should have ContenidoMarkdown default to empty string")]
    public void ModeloReporte_ContenidoMarkdown_ShouldDefaultToEmptyString()
    {
        // Arrange & Act
        var modelo = new ModeloReporte();

        // Assert
        modelo.ContenidoMarkdown.Should().BeEmpty();
    }

    [Fact(DisplayName = "ModeloReporte should have Activo default to true")]
    public void ModeloReporte_Activo_ShouldDefaultToTrue()
    {
        // Arrange & Act
        var modelo = new ModeloReporte();

        // Assert
        modelo.Activo.Should().BeTrue();
    }

    [Fact(DisplayName = "ModeloReporte should have EsDefault default to false")]
    public void ModeloReporte_EsDefault_ShouldDefaultToFalse()
    {
        // Arrange & Act
        var modelo = new ModeloReporte();

        // Assert
        modelo.EsDefault.Should().BeFalse();
    }

    [Fact(DisplayName = "ModeloReporte should have UpdatedAt default to null")]
    public void ModeloReporte_UpdatedAt_ShouldDefaultToNull()
    {
        // Arrange & Act
        var modelo = new ModeloReporte();

        // Assert
        modelo.UpdatedAt.Should().BeNull();
    }

    [Fact(DisplayName = "ModeloReporte should have UsuarioId default to empty Guid")]
    public void ModeloReporte_UsuarioId_ShouldDefaultToEmptyGuid()
    {
        // Arrange & Act
        var modelo = new ModeloReporte();

        // Assert
        modelo.UsuarioId.Should().Be(Guid.Empty);
    }

    [Fact(DisplayName = "ModeloReporte should have CreatedAt set to UTC now")]
    public void ModeloReporte_CreatedAt_ShouldDefaultToUtcNow()
    {
        // Arrange
        var before = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var modelo = new ModeloReporte();

        // Assert
        modelo.CreatedAt.Should().BeAfter(before);
        modelo.CreatedAt.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));
    }

    #endregion

    #region Template Content Tests

    [Fact(DisplayName = "ModeloReporte should store template name")]
    public void ModeloReporte_ShouldStoreTemplateName()
    {
        // Arrange
        var modelo = new ModeloReporte();

        // Act
        modelo.Nombre = "Plantilla Diaria Estándar";

        // Assert
        modelo.Nombre.Should().Be("Plantilla Diaria Estándar");
    }

    [Fact(DisplayName = "ModeloReporte should store description")]
    public void ModeloReporte_ShouldStoreDescription()
    {
        // Arrange
        var modelo = new ModeloReporte();

        // Act
        modelo.Descripcion = "Plantilla para reportes diarios con estadísticas completas y narrativa AI";

        // Assert
        modelo.Descripcion.Should().Contain("reportes diarios");
        modelo.Descripcion.Should().Contain("narrativa AI");
    }

    [Fact(DisplayName = "ModeloReporte should store markdown template with placeholders")]
    public void ModeloReporte_ShouldStoreMarkdownTemplateWithPlaceholders()
    {
        // Arrange
        var modelo = new ModeloReporte();
        var template = @"# Reporte de Incidencias
## Período: {{fecha_inicio}} - {{fecha_fin}}

### Resumen
Total de reportes: {{total_reportes}}

### Estadísticas por Delito
{{tabla_delitos}}

### Distribución por Zona
{{tabla_zonas}}

### Narrativa Generada por IA
{{narrativa_ia}}";

        // Act
        modelo.ContenidoMarkdown = template;

        // Assert
        modelo.ContenidoMarkdown.Should().Contain("{{fecha_inicio}}");
        modelo.ContenidoMarkdown.Should().Contain("{{fecha_fin}}");
        modelo.ContenidoMarkdown.Should().Contain("{{total_reportes}}");
        modelo.ContenidoMarkdown.Should().Contain("{{tabla_delitos}}");
        modelo.ContenidoMarkdown.Should().Contain("{{tabla_zonas}}");
        modelo.ContenidoMarkdown.Should().Contain("{{narrativa_ia}}");
    }

    #endregion

    #region Active Status Tests

    [Fact(DisplayName = "ModeloReporte should allow deactivating template")]
    public void ModeloReporte_ShouldAllowDeactivatingTemplate()
    {
        // Arrange
        var modelo = new ModeloReporte { Activo = true };

        // Act
        modelo.Activo = false;

        // Assert
        modelo.Activo.Should().BeFalse();
    }

    [Fact(DisplayName = "ModeloReporte should allow setting as default")]
    public void ModeloReporte_ShouldAllowSettingAsDefault()
    {
        // Arrange
        var modelo = new ModeloReporte { EsDefault = false };

        // Act
        modelo.EsDefault = true;

        // Assert
        modelo.EsDefault.Should().BeTrue();
    }

    #endregion

    #region Navigation Property Tests

    [Fact(DisplayName = "ModeloReporte ReportesGenerados collection should be initialized")]
    public void ModeloReporte_ReportesGenerados_ShouldBeInitialized()
    {
        // Arrange & Act
        var modelo = new ModeloReporte();

        // Assert
        modelo.ReportesGenerados.Should().NotBeNull();
        modelo.ReportesGenerados.Should().BeEmpty();
    }

    [Fact(DisplayName = "ModeloReporte should allow adding generated reports")]
    public void ModeloReporte_ShouldAllowAddingGeneratedReports()
    {
        // Arrange
        var modelo = new ModeloReporte { Id = 1, Nombre = "Plantilla Test" };
        var reporte1 = new ReporteAutomatizado { Id = 1, ModeloReporteId = 1 };
        var reporte2 = new ReporteAutomatizado { Id = 2, ModeloReporteId = 1 };

        // Act
        modelo.ReportesGenerados.Add(reporte1);
        modelo.ReportesGenerados.Add(reporte2);

        // Assert
        modelo.ReportesGenerados.Should().HaveCount(2);
        modelo.ReportesGenerados.Should().Contain(reporte1);
        modelo.ReportesGenerados.Should().Contain(reporte2);
    }

    [Fact(DisplayName = "ModeloReporte should support many generated reports")]
    public void ModeloReporte_ShouldSupportManyGeneratedReports()
    {
        // Arrange
        var modelo = new ModeloReporte { Id = 1, Nombre = "Plantilla Diaria" };
        var reportes = Enumerable.Range(1, 365).Select(i => new ReporteAutomatizado
        {
            Id = i,
            ModeloReporteId = 1,
            FechaInicio = new DateTime(2024, 1, 1).AddDays(i - 1),
            FechaFin = new DateTime(2024, 1, 1).AddDays(i)
        }).ToList();

        // Act
        foreach (var reporte in reportes)
        {
            modelo.ReportesGenerados.Add(reporte);
        }

        // Assert
        modelo.ReportesGenerados.Should().HaveCount(365);
    }

    #endregion

    #region Update Tracking Tests

    [Fact(DisplayName = "ModeloReporte should track update timestamp")]
    public void ModeloReporte_ShouldTrackUpdateTimestamp()
    {
        // Arrange
        var modelo = new ModeloReporte();
        var updateTime = new DateTime(2024, 7, 25, 14, 30, 0, DateTimeKind.Utc);

        // Act
        modelo.UpdatedAt = updateTime;

        // Assert
        modelo.UpdatedAt.Should().Be(updateTime);
    }

    #endregion

    #region Complete Template Tests

    [Fact(DisplayName = "ModeloReporte should allow setting all properties")]
    public void ModeloReporte_ShouldAllowSettingAllProperties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var createdAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2024, 7, 25, 14, 30, 0, DateTimeKind.Utc);

        // Act
        var modelo = new ModeloReporte
        {
            Id = 1,
            Nombre = "Plantilla Ejecutiva",
            Descripcion = "Plantilla resumida para reportes ejecutivos",
            ContenidoMarkdown = "# Resumen Ejecutivo\n{{narrativa_ia}}",
            Activo = true,
            EsDefault = true,
            UsuarioId = userId,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        // Assert
        modelo.Id.Should().Be(1);
        modelo.Nombre.Should().Be("Plantilla Ejecutiva");
        modelo.Descripcion.Should().Contain("ejecutivos");
        modelo.ContenidoMarkdown.Should().Contain("{{narrativa_ia}}");
        modelo.Activo.Should().BeTrue();
        modelo.EsDefault.Should().BeTrue();
        modelo.UsuarioId.Should().Be(userId);
        modelo.CreatedAt.Should().Be(createdAt);
        modelo.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact(DisplayName = "ModeloReporte should represent default template scenario")]
    public void ModeloReporte_ShouldRepresentDefaultTemplateScenario()
    {
        // Arrange & Act
        var defaultModelo = new ModeloReporte
        {
            Id = 1,
            Nombre = "Plantilla Predeterminada",
            Descripcion = "Plantilla estándar del sistema",
            ContenidoMarkdown = @"# Reporte Diario de Incidencias
## Fecha: {{fecha_inicio}}

### Estadísticas
- Total: {{total_reportes}} reportes
- {{estadisticas}}

### Análisis
{{narrativa_ia}}

---
*Generado automáticamente por el Sistema Ceiba*",
            Activo = true,
            EsDefault = true
        };

        // Assert
        defaultModelo.Activo.Should().BeTrue();
        defaultModelo.EsDefault.Should().BeTrue();
        defaultModelo.ContenidoMarkdown.Should().Contain("Sistema Ceiba");
    }

    #endregion

    #region Inheritance Tests

    [Fact(DisplayName = "ModeloReporte should inherit from BaseEntityWithUser")]
    public void ModeloReporte_ShouldInheritFromBaseEntityWithUser()
    {
        // Arrange & Act
        var modelo = new ModeloReporte();

        // Assert
        modelo.Should().BeAssignableTo<BaseEntityWithUser>();
    }

    [Fact(DisplayName = "ModeloReporte should allow setting UsuarioId")]
    public void ModeloReporte_ShouldAllowSettingUsuarioId()
    {
        // Arrange
        var modelo = new ModeloReporte();
        var userId = Guid.NewGuid();

        // Act
        modelo.UsuarioId = userId;

        // Assert
        modelo.UsuarioId.Should().Be(userId);
    }

    #endregion
}
