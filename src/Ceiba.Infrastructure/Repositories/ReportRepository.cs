using Ceiba.Core.Entities;
using Ceiba.Core.Interfaces;
using Ceiba.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ceiba.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for ReporteIncidencia entity.
/// US1: T035 (supporting implementation)
/// </summary>
public class ReportRepository : IReportRepository
{
    private readonly CeibaDbContext _context;

    public ReportRepository(CeibaDbContext context)
    {
        _context = context;
    }

    public async Task<ReporteIncidencia?> GetByIdAsync(int id)
    {
        return await _context.ReportesIncidencia
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<ReporteIncidencia?> GetByIdWithRelationsAsync(int id)
    {
        return await _context.ReportesIncidencia
            .Include(r => r.Zona)
            .Include(r => r.Sector)
            .Include(r => r.Cuadrante)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<List<ReporteIncidencia>> GetByUsuarioIdAsync(Guid usuarioId)
    {
        return await _context.ReportesIncidencia
            .Include(r => r.Zona)
            .Include(r => r.Sector)
            .Include(r => r.Cuadrante)
            .Where(r => r.UsuarioId == usuarioId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<(List<ReporteIncidencia> Items, int TotalCount)> SearchAsync(
        int? estado = null,
        int? zonaId = null,
        string? delito = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        Guid? usuarioId = null,
        int page = 1,
        int pageSize = 20,
        string sortBy = "createdAt",
        bool sortDesc = true)
    {
        var query = _context.ReportesIncidencia
            .Include(r => r.Zona)
            .Include(r => r.Sector)
            .Include(r => r.Cuadrante)
            .AsQueryable();

        // Apply filters
        if (estado.HasValue)
            query = query.Where(r => r.Estado == estado.Value);

        if (zonaId.HasValue)
            query = query.Where(r => r.ZonaId == zonaId.Value);

        if (!string.IsNullOrWhiteSpace(delito))
            query = query.Where(r => r.Delito.Contains(delito));

        if (fechaDesde.HasValue)
            query = query.Where(r => r.CreatedAt >= fechaDesde.Value);

        if (fechaHasta.HasValue)
            query = query.Where(r => r.CreatedAt <= fechaHasta.Value);

        if (usuarioId.HasValue)
            query = query.Where(r => r.UsuarioId == usuarioId.Value);

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply sorting
        query = sortBy.ToLower() switch
        {
            "createdat" => sortDesc
                ? query.OrderByDescending(r => r.CreatedAt)
                : query.OrderBy(r => r.CreatedAt),
            "estado" => sortDesc
                ? query.OrderByDescending(r => r.Estado)
                : query.OrderBy(r => r.Estado),
            "delito" => sortDesc
                ? query.OrderByDescending(r => r.Delito)
                : query.OrderBy(r => r.Delito),
            _ => query.OrderByDescending(r => r.CreatedAt)
        };

        // Apply pagination
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<ReporteIncidencia> AddAsync(ReporteIncidencia report)
    {
        _context.ReportesIncidencia.Add(report);
        await _context.SaveChangesAsync();
        return report;
    }

    public async Task<ReporteIncidencia> UpdateAsync(ReporteIncidencia report)
    {
        _context.ReportesIncidencia.Update(report);
        await _context.SaveChangesAsync();
        return report;
    }

    public async Task DeleteAsync(int id)
    {
        var report = await GetByIdAsync(id);
        if (report != null)
        {
            _context.ReportesIncidencia.Remove(report);
            await _context.SaveChangesAsync();
        }
    }
}
