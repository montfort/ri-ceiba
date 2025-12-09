namespace Ceiba.Shared.DTOs;

/// <summary>
/// DTO for automated report configuration.
/// US4 Enhancement: Configurable automated reports.
/// </summary>
public class AutomatedReportConfigDto
{
    public int Id { get; set; }
    public bool Habilitado { get; set; }
    public TimeSpan HoraGeneracion { get; set; }
    public string[] Destinatarios { get; set; } = Array.Empty<string>();
    public string RutaSalida { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Compatibility properties for existing code
    public TimeSpan GenerationTime
    {
        get => HoraGeneracion;
        set => HoraGeneracion = value;
    }

    public List<string> Recipients
    {
        get => Destinatarios.ToList();
        set => Destinatarios = value.ToArray();
    }

    public bool Enabled
    {
        get => Habilitado;
        set => Habilitado = value;
    }

    public int? DefaultTemplateId { get; set; }
}

/// <summary>
/// DTO for updating automated report configuration.
/// </summary>
public class AutomatedReportConfigUpdateDto
{
    public bool Habilitado { get; set; }
    public TimeSpan HoraGeneracion { get; set; }
    public string[] Destinatarios { get; set; } = Array.Empty<string>();
    public string RutaSalida { get; set; } = string.Empty;
}

/// <summary>
/// DTO for displaying automated report in list view.
/// US4: Reportes Automatizados Diarios con IA.
/// </summary>
public class AutomatedReportListDto
{
    public int Id { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool Enviado { get; set; }
    public DateTime? FechaEnvio { get; set; }
    public bool TieneError { get; set; }
    public string? NombreModelo { get; set; }
    public int TotalReportes { get; set; }
}

/// <summary>
/// DTO for automated report detail view.
/// </summary>
public class AutomatedReportDetailDto
{
    public int Id { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string ContenidoMarkdown { get; set; } = string.Empty;
    public string? ContenidoWordPath { get; set; }
    public ReportStatisticsDto Estadisticas { get; set; } = new();
    public bool Enviado { get; set; }
    public DateTime? FechaEnvio { get; set; }
    public string? ErrorMensaje { get; set; }
    public int? ModeloReporteId { get; set; }
    public string? NombreModelo { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for report statistics in automated reports.
/// </summary>
public class ReportStatisticsDto
{
    public int TotalReportes { get; set; }
    public int ReportesEntregados { get; set; }
    public int ReportesBorrador { get; set; }

    // Demographics
    public Dictionary<string, int> PorSexo { get; set; } = new();
    public Dictionary<string, int> PorRangoEdad { get; set; } = new();
    public int TotalLgbtttiq { get; set; }
    public int TotalMigrantes { get; set; }
    public int TotalSituacionCalle { get; set; }
    public int TotalDiscapacidad { get; set; }

    // Crime types
    public Dictionary<string, int> PorDelito { get; set; } = new();
    public string? DelitoMasFrecuente { get; set; }

    // Geographic
    public Dictionary<string, int> PorZona { get; set; } = new();
    public string? ZonaMasActiva { get; set; }

    // Attention types
    public Dictionary<string, int> PorTipoAtencion { get; set; } = new();

    // Action types
    public Dictionary<string, int> PorTipoAccion { get; set; } = new();

    // Transfers
    public int ConTraslado { get; set; }
    public int SinTraslado { get; set; }
    public int TrasladoNoAplica { get; set; }
}

/// <summary>
/// DTO for template list view.
/// </summary>
public class ReportTemplateListDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; }
    public bool EsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO for template detail/edit.
/// </summary>
public class ReportTemplateDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string ContenidoMarkdown { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public bool EsDefault { get; set; }
}

/// <summary>
/// DTO for creating a new template.
/// </summary>
public class CreateTemplateDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string ContenidoMarkdown { get; set; } = string.Empty;
    public bool EsDefault { get; set; }
}

/// <summary>
/// DTO for updating a template.
/// </summary>
public class UpdateTemplateDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string ContenidoMarkdown { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public bool EsDefault { get; set; }
}

/// <summary>
/// DTO for manual report generation request.
/// </summary>
public class GenerateReportRequestDto
{
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public int? ModeloReporteId { get; set; }
    public bool EnviarEmail { get; set; }
    public List<string>? EmailDestinatarios { get; set; }
}

/// <summary>
/// DTO for AI narrative generation request.
/// </summary>
public class NarrativeRequestDto
{
    public ReportStatisticsDto Statistics { get; set; } = new();
    public List<IncidentSummaryDto> Incidents { get; set; } = new();
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
}

/// <summary>
/// DTO for incident summary in narrative generation.
/// </summary>
public class IncidentSummaryDto
{
    public int Id { get; set; }
    public string Folio { get; set; } = string.Empty;
    public string Delito { get; set; } = string.Empty;
    public string HechosReportados { get; set; } = string.Empty;
    public string AccionesRealizadas { get; set; } = string.Empty;
    public DateTime FechaReporte { get; set; }
}

/// <summary>
/// DTO for AI narrative response.
/// </summary>
public class NarrativeResponseDto
{
    public string Narrativa { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Error { get; set; }
    public int TokensUsed { get; set; }
}

/// <summary>
/// DTO for email sending request.
/// </summary>
public class SendEmailRequestDto
{
    public List<string> Recipients { get; set; } = new();
    public string Subject { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public List<EmailAttachmentDto> Attachments { get; set; } = new();
}

/// <summary>
/// DTO for email attachment.
/// </summary>
public class EmailAttachmentDto
{
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/octet-stream";
}

/// <summary>
/// DTO for email sending result.
/// </summary>
public class SendEmailResultDto
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public DateTime? SentAt { get; set; }
}
