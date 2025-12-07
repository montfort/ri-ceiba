using Ceiba.Shared.DTOs;

namespace Ceiba.Core.Interfaces;

/// <summary>
/// Service interface for catalog administration operations.
/// Only accessible by ADMIN role.
/// US3: FR-027 to FR-030
/// </summary>
public interface ICatalogAdminService
{
    #region Zona Management (FR-027)

    /// <summary>
    /// Gets all zones with their sector count.
    /// </summary>
    Task<List<ZonaDto>> GetZonasAsync(
        bool? activo = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a zone by ID.
    /// </summary>
    Task<ZonaDto?> GetZonaByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new zone.
    /// FR-027: Configure zones (create)
    /// </summary>
    Task<ZonaDto> CreateZonaAsync(
        CreateZonaDto createDto,
        Guid adminUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing zone.
    /// FR-027: Configure zones (edit)
    /// </summary>
    Task<ZonaDto> UpdateZonaAsync(
        int id,
        CreateZonaDto updateDto,
        Guid adminUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Toggles zone active status.
    /// FR-027: Configure zones (activate/deactivate)
    /// </summary>
    Task<ZonaDto> ToggleZonaActivoAsync(
        int id,
        Guid adminUserId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Sector Management (FR-028)

    /// <summary>
    /// Gets sectors, optionally filtered by zone.
    /// </summary>
    Task<List<SectorDto>> GetSectoresAsync(
        int? zonaId = null,
        bool? activo = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a sector by ID.
    /// </summary>
    Task<SectorDto?> GetSectorByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new sector linked to a zone.
    /// FR-028: Configure sectors associated to zones
    /// </summary>
    Task<SectorDto> CreateSectorAsync(
        CreateSectorDto createDto,
        Guid adminUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing sector.
    /// </summary>
    Task<SectorDto> UpdateSectorAsync(
        int id,
        CreateSectorDto updateDto,
        Guid adminUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Toggles sector active status.
    /// </summary>
    Task<SectorDto> ToggleSectorActivoAsync(
        int id,
        Guid adminUserId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Cuadrante Management (FR-029)

    /// <summary>
    /// Gets cuadrantes, optionally filtered by sector.
    /// </summary>
    Task<List<CuadranteDto>> GetCuadrantesAsync(
        int? sectorId = null,
        bool? activo = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a cuadrante by ID.
    /// </summary>
    Task<CuadranteDto?> GetCuadranteByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new cuadrante linked to a sector.
    /// FR-029: Configure cuadrantes associated to sectors
    /// </summary>
    Task<CuadranteDto> CreateCuadranteAsync(
        CreateCuadranteDto createDto,
        Guid adminUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing cuadrante.
    /// </summary>
    Task<CuadranteDto> UpdateCuadranteAsync(
        int id,
        CreateCuadranteDto updateDto,
        Guid adminUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Toggles cuadrante active status.
    /// </summary>
    Task<CuadranteDto> ToggleCuadranteActivoAsync(
        int id,
        Guid adminUserId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Suggestion Management (FR-030)

    /// <summary>
    /// Gets suggestions, optionally filtered by field.
    /// FR-030: Configure suggestion lists
    /// </summary>
    Task<List<SugerenciaDto>> GetSugerenciasAsync(
        string? campo = null,
        bool? activo = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a suggestion by ID.
    /// </summary>
    Task<SugerenciaDto?> GetSugerenciaByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new suggestion.
    /// </summary>
    Task<SugerenciaDto> CreateSugerenciaAsync(
        CreateSugerenciaDto createDto,
        Guid adminUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing suggestion.
    /// </summary>
    Task<SugerenciaDto> UpdateSugerenciaAsync(
        int id,
        CreateSugerenciaDto updateDto,
        Guid adminUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a suggestion.
    /// </summary>
    Task DeleteSugerenciaAsync(
        int id,
        Guid adminUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reorders suggestions within a field.
    /// </summary>
    Task ReorderSugerenciasAsync(
        string campo,
        int[] orderedIds,
        Guid adminUserId,
        CancellationToken cancellationToken = default);

    #endregion
}
