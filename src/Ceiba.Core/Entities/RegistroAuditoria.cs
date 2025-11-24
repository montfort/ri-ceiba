namespace Ceiba.Core.Entities;

/// <summary>
/// Audit log entry for all system operations.
/// Immutable record of who did what, when.
/// Retention: Indefinite (never deleted per FR-035b).
/// </summary>
public class RegistroAuditoria : BaseEntity
{
    /// <summary>
    /// Unique identifier for the audit log entry.
    /// Uses BIGINT to support high volume logging.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Action code identifying the operation performed.
    /// See AuditActionCode class for standard codes.
    /// Required, max 50 characters.
    /// Example: "AUTH_LOGIN", "REPORT_CREATE", "USER_SUSPEND"
    /// </summary>
    public string Codigo { get; set; } = string.Empty;

    /// <summary>
    /// ID of the related entity (if applicable).
    /// Example: Report ID for REPORT_CREATE, User ID for USER_SUSPEND
    /// Nullable - system-level operations may not have a related entity.
    /// </summary>
    public int? IdRelacionado { get; set; }

    /// <summary>
    /// Table name of the related entity.
    /// Example: "REPORTE_INCIDENCIA", "USUARIO", "ZONA"
    /// Nullable - system-level operations may not have a related table.
    /// Max 50 characters.
    /// </summary>
    public string? TablaRelacionada { get; set; }

    /// <summary>
    /// User who performed the action.
    /// References Usuario.Id (ASP.NET Identity user).
    /// Nullable - system-triggered operations have NULL user.
    /// </summary>
    public Guid? UsuarioId { get; set; }

    /// <summary>
    /// Client IP address from which the action originated.
    /// Supports both IPv4 and IPv6 (max 45 characters).
    /// Nullable - may not be available for background jobs.
    /// </summary>
    public string? Ip { get; set; }

    /// <summary>
    /// Additional details in JSON format.
    /// Stores context-specific information about the operation.
    /// Example: {"old_value": "Borrador", "new_value": "Entregado"}
    /// Nullable - simple operations may not need extra details.
    /// </summary>
    public string? Detalles { get; set; }
}
