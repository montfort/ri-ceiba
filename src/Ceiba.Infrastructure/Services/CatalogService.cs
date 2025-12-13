using Ceiba.Core.Interfaces;
using Ceiba.Infrastructure.Data;
using Ceiba.Shared.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Ceiba.Application.Services;

/// <summary>
/// Service implementation for catalog operations (Zona, Región, Sector, Cuadrante, Suggestions).
/// US1: T036
/// </summary>
public class CatalogService : ICatalogService
{
    private readonly CeibaDbContext _context;

    public CatalogService(CeibaDbContext context)
    {
        _context = context;
    }

    public async Task<List<CatalogItemDto>> GetZonasAsync()
    {
        return await _context.Zonas
            .Where(z => z.Activo)
            .OrderBy(z => z.Nombre)
            .Select(z => new CatalogItemDto
            {
                Id = z.Id,
                Nombre = z.Nombre
            })
            .ToListAsync();
    }

    public async Task<List<CatalogItemDto>> GetRegionesByZonaAsync(int zonaId)
    {
        return await _context.Regiones
            .Where(r => r.ZonaId == zonaId && r.Activo)
            .OrderBy(r => r.Nombre)
            .Select(r => new CatalogItemDto
            {
                Id = r.Id,
                Nombre = r.Nombre
            })
            .ToListAsync();
    }

    public async Task<List<CatalogItemDto>> GetSectoresByRegionAsync(int regionId)
    {
        return await _context.Sectores
            .Where(s => s.RegionId == regionId && s.Activo)
            .OrderBy(s => s.Nombre)
            .Select(s => new CatalogItemDto
            {
                Id = s.Id,
                Nombre = s.Nombre
            })
            .ToListAsync();
    }

    public async Task<List<CatalogItemDto>> GetCuadrantesBySectorAsync(int sectorId)
    {
        return await _context.Cuadrantes
            .Where(c => c.SectorId == sectorId && c.Activo)
            .OrderBy(c => c.Nombre)
            .Select(c => new CatalogItemDto
            {
                Id = c.Id,
                Nombre = c.Nombre
            })
            .ToListAsync();
    }

    public async Task<List<string>> GetSuggestionsAsync(string campo)
    {
        // Validate campo
        var allowedFields = new[] { "sexo", "delito", "tipo_de_atencion" };
        if (!allowedFields.Contains(campo.ToLower()))
        {
            return new List<string>();
        }

        return await _context.CatalogosSugerencia
            .Where(s => s.Campo.ToLower() == campo.ToLower() && s.Activo)
            .OrderBy(s => s.Orden)
            .ThenBy(s => s.Valor)
            .Select(s => s.Valor)
            .ToListAsync();
    }

    public async Task<bool> ValidateHierarchyAsync(int zonaId, int regionId, int sectorId, int cuadranteId)
    {
        // Check if region belongs to zona
        var regionExists = await _context.Regiones
            .AnyAsync(r => r.Id == regionId && r.ZonaId == zonaId && r.Activo);

        if (!regionExists)
            return false;

        // Check if sector belongs to region
        var sectorExists = await _context.Sectores
            .AnyAsync(s => s.Id == sectorId && s.RegionId == regionId && s.Activo);

        if (!sectorExists)
            return false;

        // Check if cuadrante belongs to sector
        var cuadranteExists = await _context.Cuadrantes
            .AnyAsync(c => c.Id == cuadranteId && c.SectorId == sectorId && c.Activo);

        return cuadranteExists;
    }
}
