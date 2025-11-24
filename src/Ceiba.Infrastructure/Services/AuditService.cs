using Ceiba.Core.Entities;
using Ceiba.Core.Interfaces;
using Ceiba.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ceiba.Infrastructure.Services;

/// <summary>
/// Implementation of audit logging service.
/// Provides manual audit logging and query capabilities.
/// </summary>
public class AuditService : IAuditService
{
    private readonly CeibaDbContext _context;

    public AuditService(CeibaDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(
        string codigo,
        int? idRelacionado = null,
        string? tablaRelacionada = null,
        string? detalles = null,
        string? ip = null,
        CancellationToken cancellationToken = default)
    {
        var auditLog = new RegistroAuditoria
        {
            Codigo = codigo,
            IdRelacionado = idRelacionado,
            TablaRelacionada = tablaRelacionada,
            UsuarioId = _context.CurrentUserId,
            Ip = ip,
            Detalles = detalles
        };

        _context.RegistrosAuditoria.Add(auditLog);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<AuditLogDto>> QueryAsync(
        Guid? usuarioId = null,
        string? codigo = null,
        DateTime? fechaInicio = null,
        DateTime? fechaFin = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        // Enforce max limit (RT-002 mitigation)
        take = Math.Min(take, 500);

        var query = _context.RegistrosAuditoria.AsQueryable();

        // Apply filters
        if (usuarioId.HasValue)
            query = query.Where(a => a.UsuarioId == usuarioId.Value);

        if (!string.IsNullOrEmpty(codigo))
            query = query.Where(a => a.Codigo == codigo);

        if (fechaInicio.HasValue)
            query = query.Where(a => a.CreatedAt >= fechaInicio.Value);

        if (fechaFin.HasValue)
            query = query.Where(a => a.CreatedAt <= fechaFin.Value);

        // Execute query with pagination
        var results = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Select(a => new AuditLogDto(
                a.Id,
                a.Codigo,
                a.IdRelacionado,
                a.TablaRelacionada,
                a.CreatedAt,
                a.UsuarioId,
                null, // Usuario nombre will be joined in US3
                a.Ip,
                a.Detalles
            ))
            .ToListAsync(cancellationToken);

        return results;
    }
}
