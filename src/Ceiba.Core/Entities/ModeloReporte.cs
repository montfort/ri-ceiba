namespace Ceiba.Core.Entities;

/// <summary>
/// Represents a template for automated report generation.
/// Templates define the structure and format of generated reports.
/// US4: Reportes Automatizados Diarios con IA.
/// </summary>
public class ModeloReporte : BaseEntityWithUser
{
    /// <summary>
    /// Unique identifier for the template.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Display name for the template.
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Template description.
    /// </summary>
    public string? Descripcion { get; set; }

    /// <summary>
    /// Template content in Markdown format.
    /// Supports placeholders for dynamic content:
    /// - {{fecha_inicio}}, {{fecha_fin}} - Report period
    /// - {{total_reportes}} - Total reports count
    /// - {{estadisticas}} - JSON statistics block
    /// - {{narrativa_ia}} - AI-generated narrative
    /// - {{tabla_delitos}} - Crime type breakdown table
    /// - {{tabla_zonas}} - Zone breakdown table
    /// </summary>
    public string ContenidoMarkdown { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if this template is active and available for selection.
    /// </summary>
    public bool Activo { get; set; } = true;

    /// <summary>
    /// Indicates if this is the default template for new automated reports.
    /// Only one template can be the default.
    /// </summary>
    public bool EsDefault { get; set; }

    /// <summary>
    /// Last modification timestamp.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Navigation property to generated reports using this template.
    /// </summary>
    public ICollection<ReporteAutomatizado> ReportesGenerados { get; set; } = new List<ReporteAutomatizado>();
}
