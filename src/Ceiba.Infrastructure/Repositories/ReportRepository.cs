using Ceiba.Core.Entities;
using Ceiba.Core.Interfaces;
using Ceiba.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ceiba.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for ReporteIncidencia entity.
/// US1: T035 (supporting implementation)
/// T117c, T117d: Pagination and query optimization
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
        // T117d: Use AsNoTracking for read-only queries
        return await _context.ReportesIncidencia
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<ReporteIncidencia?> GetByIdWithRelationsAsync(int id)
    {
        // T117d: Use AsNoTracking and AsSplitQuery for multiple includes
        return await _context.ReportesIncidencia
            .Include(r => r.Zona)
            .Include(r => r.Sector)
            .Include(r => r.Cuadrante)
            .AsSplitQuery() // T117d: Avoid cartesian explosion
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<List<ReporteIncidencia>> GetByUsuarioIdAsync(Guid usuarioId)
    {
        // T117d: Optimized query with split and no tracking
        return await _context.ReportesIncidencia
            .Include(r => r.Zona)
            .Include(r => r.Sector)
            .Include(r => r.Cuadrante)
            .AsSplitQuery()
            .AsNoTracking()
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
        // T117c, T117d: Build optimized query
        var query = _context.ReportesIncidencia.AsQueryable();

        // Apply filters BEFORE includes for better query plan
        if (estado.HasValue)
        {
            query = query.Where(r => r.Estado == estado.Value);
        }

        if (zonaId.HasValue)
        {
            query = query.Where(r => r.ZonaId == zonaId.Value);
        }

        // T117d: Use EF.Functions.Like for better index usage on LIKE queries
        if (!string.IsNullOrWhiteSpace(delito))
        {
            query = query.Where(r => EF.Functions.Like(r.Delito, $"%{delito}%"));
        }

        if (fechaDesde.HasValue)
        {
            query = query.Where(r => r.CreatedAt >= fechaDesde.Value);
        }

        if (fechaHasta.HasValue)
        {
            query = query.Where(r => r.CreatedAt <= fechaHasta.Value);
        }

        if (usuarioId.HasValue)
        {
            query = query.Where(r => r.UsuarioId == usuarioId.Value);
        }

        // T117c: Get count with optimized query (no includes needed for count)
        var totalCount = await query.CountAsync();

        // T117c: Early exit if no results
        if (totalCount == 0)
        {
            return (new List<ReporteIncidencia>(), 0);
        }

        // Apply sorting with explicit ordering for index usage
        query = ApplySorting(query, sortBy, sortDesc);

        // T117c: Apply pagination BEFORE includes for better performance
        var paginatedQuery = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        // T117d: Add includes AFTER pagination with AsSplitQuery
        var items = await paginatedQuery
            .Include(r => r.Zona)
            .Include(r => r.Sector)
            .Include(r => r.Cuadrante)
            .AsSplitQuery()
            .AsNoTracking()
            .ToListAsync();

        return (items, totalCount);
    }

    /// <summary>
    /// T117d: Keyset pagination for better performance on large datasets.
    /// Use this for infinite scroll or "load more" patterns.
    /// </summary>
    public async Task<List<ReporteIncidencia>> SearchWithKeysetAsync(
        DateTime? lastCreatedAt,
        int? lastId,
        int? estado = null,
        int? zonaId = null,
        int pageSize = 20)
    {
        var query = _context.ReportesIncidencia.AsQueryable();

        // Apply filters
        if (estado.HasValue)
        {
            query = query.Where(r => r.Estado == estado.Value);
        }

        if (zonaId.HasValue)
        {
            query = query.Where(r => r.ZonaId == zonaId.Value);
        }

        // Keyset pagination: get records after the last seen record
        if (lastCreatedAt.HasValue && lastId.HasValue)
        {
            query = query.Where(r =>
                r.CreatedAt < lastCreatedAt.Value ||
                (r.CreatedAt == lastCreatedAt.Value && r.Id < lastId.Value));
        }

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.Id)
            .Take(pageSize)
            .Include(r => r.Zona)
            .Include(r => r.Sector)
            .Include(r => r.Cuadrante)
            .AsSplitQuery()
            .AsNoTracking()
            .ToListAsync();
    }

    private static IQueryable<ReporteIncidencia> ApplySorting(
        IQueryable<ReporteIncidencia> query,
        string sortBy,
        bool sortDesc)
    {
        return sortBy.ToLowerInvariant() switch
        {
            "createdat" => sortDesc
                ? query.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id)
                : query.OrderBy(r => r.CreatedAt).ThenBy(r => r.Id),
            "estado" => sortDesc
                ? query.OrderByDescending(r => r.Estado).ThenByDescending(r => r.CreatedAt)
                : query.OrderBy(r => r.Estado).ThenBy(r => r.CreatedAt),
            "delito" => sortDesc
                ? query.OrderByDescending(r => r.Delito).ThenByDescending(r => r.CreatedAt)
                : query.OrderBy(r => r.Delito).ThenBy(r => r.CreatedAt),
            "zona" => sortDesc
                ? query.OrderByDescending(r => r.ZonaId).ThenByDescending(r => r.CreatedAt)
                : query.OrderBy(r => r.ZonaId).ThenBy(r => r.CreatedAt),
            _ => query.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id)
        };
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
