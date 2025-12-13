using Ceiba.Shared.DTOs;

namespace Ceiba.Core.Interfaces;

/// <summary>
/// Service interface for catalog operations (Zona, Región, Sector, Cuadrante, Suggestions).
/// US1: T034
/// </summary>
public interface ICatalogService
{
    /// <summary>
    /// Gets all active zones.
    /// </summary>
    /// <returns>List of zones</returns>
    Task<List<CatalogItemDto>> GetZonasAsync();

    /// <summary>
    /// Gets regions for a specific zone.
    /// </summary>
    /// <param name="zonaId">Zone ID</param>
    /// <returns>List of regions in the zone</returns>
    Task<List<CatalogItemDto>> GetRegionesByZonaAsync(int zonaId);

    /// <summary>
    /// Gets sectors for a specific region.
    /// </summary>
    /// <param name="regionId">Region ID</param>
    /// <returns>List of sectors in the region</returns>
    Task<List<CatalogItemDto>> GetSectoresByRegionAsync(int regionId);

    /// <summary>
    /// Gets quadrants for a specific sector.
    /// </summary>
    /// <param name="sectorId">Sector ID</param>
    /// <returns>List of quadrants in the sector</returns>
    Task<List<CatalogItemDto>> GetCuadrantesBySectorAsync(int sectorId);

    /// <summary>
    /// Gets suggestions for a specific field.
    /// </summary>
    /// <param name="campo">Field name (sexo, delito, tipo_de_atencion)</param>
    /// <returns>List of suggestions ordered by orden field</returns>
    Task<List<string>> GetSuggestionsAsync(string campo);

    /// <summary>
    /// Validates the geographic hierarchy (Zona → Región → Sector → Cuadrante).
    /// Ensures that the region belongs to the zona, sector belongs to the region,
    /// and the cuadrante belongs to the sector.
    /// </summary>
    /// <param name="zonaId">Zone ID</param>
    /// <param name="regionId">Region ID</param>
    /// <param name="sectorId">Sector ID</param>
    /// <param name="cuadranteId">Quadrant ID</param>
    /// <returns>True if hierarchy is valid, false otherwise</returns>
    Task<bool> ValidateHierarchyAsync(int zonaId, int regionId, int sectorId, int cuadranteId);
}
