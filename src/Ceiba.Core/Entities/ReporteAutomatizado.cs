namespace Ceiba.Core.Entities;

/// <summary>
/// Represents an automatically generated daily summary report.
/// Contains aggregated statistics and AI-generated narrative from incident reports.
/// US4: Reportes Automatizados Diarios con IA.
/// </summary>
public class ReporteAutomatizado : BaseEntity
{
    /// <summary>
    /// Unique identifier for the automated report.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Start of the reporting period (inclusive).
    /// </summary>
    public DateTime FechaInicio { get; set; }

    /// <summary>
    /// End of the reporting period (exclusive).
    /// </summary>
    public DateTime FechaFin { get; set; }

    /// <summary>
    /// Report content in Markdown format.
    /// Includes statistics and AI-generated narrative.
    /// </summary>
    public string ContenidoMarkdown { get; set; } = string.Empty;

    /// <summary>
    /// Path to the generated Word document file.
    /// NULL if Word generation failed or not yet generated.
    /// </summary>
    public string? ContenidoWordPath { get; set; }

    /// <summary>
    /// Aggregated statistics in JSON format.
    /// Contains: total reports, crime types, zones, demographics, etc.
    /// </summary>
    public string Estadisticas { get; set; } = "{}";

    /// <summary>
    /// Indicates if the report was successfully sent by email.
    /// </summary>
    public bool Enviado { get; set; }

    /// <summary>
    /// Timestamp when the email was sent.
    /// NULL if not yet sent or sending failed.
    /// </summary>
    public DateTime? FechaEnvio { get; set; }

    /// <summary>
    /// Error message if generation or sending failed.
    /// NULL on success.
    /// </summary>
    public string? ErrorMensaje { get; set; }

    /// <summary>
    /// ID of the template used for generation.
    /// NULL if using default template.
    /// </summary>
    public int? ModeloReporteId { get; set; }

    /// <summary>
    /// Navigation property to the template used.
    /// </summary>
    public ModeloReporte? ModeloReporte { get; set; }
}
