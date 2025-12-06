namespace Ceiba.Shared.DTOs.Export;

/// <summary>
/// Complete report data for export purposes
/// Contains all fields formatted for PDF and JSON export
/// </summary>
public record ReportExportDto
{
    // Report Metadata
    public int Id { get; init; }
    public string Folio { get; init; } = string.Empty;
    public string Estado { get; init; } = string.Empty;
    public DateTime FechaCreacion { get; init; }
    public DateTime? FechaEntrega { get; init; }
    public string UsuarioCreador { get; init; } = string.Empty;

    // Demographic Data
    public string Sexo { get; init; } = string.Empty;
    public int Edad { get; init; }
    public bool LgbtttiqPlus { get; init; }
    public bool SituacionCalle { get; init; }
    public bool Migrante { get; init; }
    public bool Discapacidad { get; init; }

    // Classification
    public string Delito { get; init; } = string.Empty;
    public string TipoDeAtencion { get; init; } = string.Empty;
    public string TipoDeAccion { get; init; } = string.Empty;

    // Geographic Location
    public string Zona { get; init; } = string.Empty;
    public string Sector { get; init; } = string.Empty;
    public string Cuadrante { get; init; } = string.Empty;
    public string TurnoCeiba { get; init; } = string.Empty;

    // Incident Details
    public string HechosReportados { get; init; } = string.Empty;
    public string AccionesRealizadas { get; init; } = string.Empty;
    public string Traslados { get; init; } = string.Empty;
    public string? Observaciones { get; init; }

    // Audit Information (optional, based on ExportOptions.IncludeAuditInfo)
    public DateTime? FechaUltimaModificacion { get; init; }
    public string? UsuarioUltimaModificacion { get; init; }
}
