using Ceiba.Shared.DTOs;

namespace Ceiba.Core.Interfaces;

/// <summary>
/// Service interface for catalog operations (Zona, Sector, Cuadrante, Suggestions).
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
    /// Gets sectors for a specific zone.
    /// </summary>
    /// <param name="zonaId">Zone ID</param>
    /// <returns>List of sectors in the zone</returns>
    Task<List<CatalogItemDto>> GetSectoresByZonaAsync(int zonaId);

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
    /// Validates the geographic hierarchy (Zona → Sector → Cuadrante).
    /// Ensures that the sector belongs to the zona and the cuadrante belongs to the sector.
    /// </summary>
    /// <param name="zonaId">Zone ID</param>
    /// <param name="sectorId">Sector ID</param>
    /// <param name="cuadranteId">Quadrant ID</param>
    /// <returns>True if hierarchy is valid, false otherwise</returns>
    Task<bool> ValidateHierarchyAsync(int zonaId, int sectorId, int cuadranteId);
}
