namespace Ceiba.Core.Interfaces;

/// <summary>
/// Service interface for manual audit logging.
/// Used for operations that need explicit audit trails beyond automatic interceptor logging.
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Logs an audit event with full details.
    /// </summary>
    /// <param name="codigo">Audit action code (see AuditActionCode)</param>
    /// <param name="idRelacionado">Optional related entity ID</param>
    /// <param name="tablaRelacionada">Optional related table name</param>
    /// <param name="detalles">Optional additional details (JSON)</param>
    /// <param name="ip">Optional client IP address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task LogAsync(
        string codigo,
        int? idRelacionado = null,
        string? tablaRelacionada = null,
        string? detalles = null,
        string? ip = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries audit logs with filtering.
    /// </summary>
    /// <param name="usuarioId">Filter by user</param>
    /// <param name="codigo">Filter by action code</param>
    /// <param name="fechaInicio">Filter by start date</param>
    /// <param name="fechaFin">Filter by end date</param>
    /// <param name="skip">Pagination: records to skip</param>
    /// <param name="take">Pagination: records to take (max 500)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of audit log entries</returns>
    Task<List<AuditLogDto>> QueryAsync(
        Guid? usuarioId = null,
        string? codigo = null,
        DateTime? fechaInicio = null,
        DateTime? fechaFin = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// DTO for audit log query results.
/// </summary>
public record AuditLogDto(
    long Id,
    string Codigo,
    int? IdRelacionado,
    string? TablaRelacionada,
    DateTime CreatedAt,
    Guid? UsuarioId,
    string? UsuarioNombre,
    string? Ip,
    string? Detalles
);
