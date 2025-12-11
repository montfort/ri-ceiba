using Ceiba.Core.Entities;
using Ceiba.Infrastructure.Caching;
using Ceiba.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ceiba.Infrastructure.Services;

/// <summary>
/// Cached catalog service for geographic and suggestion catalogs.
/// T117b: Query caching strategy - Caches rarely-changing catalog data.
/// </summary>
public class CachedCatalogService
{
    private readonly CeibaDbContext _context;
    private readonly ICacheService _cache;
    private readonly ILogger<CachedCatalogService> _logger;

    private static readonly TimeSpan CatalogCacheDuration = TimeSpan.FromHours(2);

    public CachedCatalogService(
        CeibaDbContext context,
        ICacheService cache,
        ILogger<CachedCatalogService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    #region Zonas

    public async Task<List<Zona>> GetAllZonasAsync()
    {
        return await _cache.GetOrCreateAsync(
            CacheKeys.AllZonas,
            async () => await _context.Zonas
                .Where(z => z.Activo)
                .OrderBy(z => z.Nombre)
                .AsNoTracking()
                .ToListAsync(),
            CatalogCacheDuration) ?? new List<Zona>();
    }

    public async Task<Zona?> GetZonaByIdAsync(int id)
    {
        var zonas = await GetAllZonasAsync();
        return zonas.FirstOrDefault(z => z.Id == id);
    }

    #endregion

    #region Sectores

    public async Task<List<Sector>> GetAllSectoresAsync()
    {
        return await _cache.GetOrCreateAsync(
            CacheKeys.AllSectores,
            async () => await _context.Sectores
                .Where(s => s.Activo)
                .OrderBy(s => s.ZonaId)
                .ThenBy(s => s.Nombre)
                .AsNoTracking()
                .ToListAsync(),
            CatalogCacheDuration) ?? new List<Sector>();
    }

    public async Task<List<Sector>> GetSectoresByZonaAsync(int zonaId)
    {
        var cacheKey = CacheKeys.Format(CacheKeys.SectoresByZona, zonaId);

        return await _cache.GetOrCreateAsync(
            cacheKey,
            async () => await _context.Sectores
                .Where(s => s.ZonaId == zonaId && s.Activo)
                .OrderBy(s => s.Nombre)
                .AsNoTracking()
                .ToListAsync(),
            CatalogCacheDuration) ?? new List<Sector>();
    }

    #endregion

    #region Cuadrantes

    public async Task<List<Cuadrante>> GetAllCuadrantesAsync()
    {
        return await _cache.GetOrCreateAsync(
            CacheKeys.AllCuadrantes,
            async () => await _context.Cuadrantes
                .Where(c => c.Activo)
                .OrderBy(c => c.SectorId)
                .ThenBy(c => c.Nombre)
                .AsNoTracking()
                .ToListAsync(),
            CatalogCacheDuration) ?? new List<Cuadrante>();
    }

    public async Task<List<Cuadrante>> GetCuadrantesBySectorAsync(int sectorId)
    {
        var cacheKey = CacheKeys.Format(CacheKeys.CuadrantesBySector, sectorId);

        return await _cache.GetOrCreateAsync(
            cacheKey,
            async () => await _context.Cuadrantes
                .Where(c => c.SectorId == sectorId && c.Activo)
                .OrderBy(c => c.Nombre)
                .AsNoTracking()
                .ToListAsync(),
            CatalogCacheDuration) ?? new List<Cuadrante>();
    }

    #endregion

    #region Sugerencias

    public async Task<List<CatalogoSugerencia>> GetSugerenciasByCampoAsync(string campo)
    {
        var cacheKey = CacheKeys.Format(CacheKeys.SugerenciasByCampo, campo.ToLowerInvariant());

        return await _cache.GetOrCreateAsync(
            cacheKey,
            async () => await _context.CatalogosSugerencia
                .Where(s => s.Campo == campo && s.Activo)
                .OrderBy(s => s.Orden)
                .ThenBy(s => s.Valor)
                .AsNoTracking()
                .ToListAsync(),
            CatalogCacheDuration) ?? new List<CatalogoSugerencia>();
    }

    public async Task<Dictionary<string, List<string>>> GetAllSugerenciasAsync()
    {
        var sugerencias = await _context.CatalogosSugerencia
            .Where(s => s.Activo)
            .OrderBy(s => s.Campo)
            .ThenBy(s => s.Orden)
            .AsNoTracking()
            .ToListAsync();

        return sugerencias
            .GroupBy(s => s.Campo)
            .ToDictionary(g => g.Key, g => g.Select(s => s.Valor).ToList());
    }

    #endregion

    #region Cache Invalidation

    /// <summary>
    /// Invalidate all catalog caches. Call after catalog updates.
    /// </summary>
    public void InvalidateAllCatalogs()
    {
        _cache.RemoveByPrefix("catalog:");
        _logger.LogInformation("All catalog caches invalidated");
    }

    /// <summary>
    /// Invalidate zona-related caches.
    /// </summary>
    public void InvalidateZonas()
    {
        _cache.Remove(CacheKeys.AllZonas);
        _cache.RemoveByPrefix("catalog:sectores:");
        _logger.LogInformation("Zona caches invalidated");
    }

    /// <summary>
    /// Invalidate sector-related caches.
    /// </summary>
    public void InvalidateSectores()
    {
        _cache.Remove(CacheKeys.AllSectores);
        _cache.RemoveByPrefix("catalog:sectores:zona:");
        _cache.RemoveByPrefix("catalog:cuadrantes:");
        _logger.LogInformation("Sector caches invalidated");
    }

    /// <summary>
    /// Invalidate cuadrante-related caches.
    /// </summary>
    public void InvalidateCuadrantes()
    {
        _cache.Remove(CacheKeys.AllCuadrantes);
        _cache.RemoveByPrefix("catalog:cuadrantes:sector:");
        _logger.LogInformation("Cuadrante caches invalidated");
    }

    /// <summary>
    /// Invalidate sugerencia caches for a specific campo.
    /// </summary>
    public void InvalidateSugerencias(string? campo = null)
    {
        if (string.IsNullOrEmpty(campo))
        {
            _cache.RemoveByPrefix("catalog:sugerencias:");
        }
        else
        {
            _cache.Remove(CacheKeys.Format(CacheKeys.SugerenciasByCampo, campo.ToLowerInvariant()));
        }

        _logger.LogInformation("Sugerencia caches invalidated for campo: {Campo}", campo ?? "all");
    }

    #endregion
}
