namespace Ceiba.Infrastructure.Data;

/// <summary>
/// Interface for loading and managing geographic hierarchy data.
/// Enables dependency injection and testability.
/// </summary>
public interface IRegionDataLoader
{
    /// <summary>
    /// Loads geographic data from a JSON file.
    /// </summary>
    /// <param name="jsonPath">Path to the regiones.json file</param>
    /// <returns>List of parsed zone data</returns>
    Task<List<RegionJsonZona>> LoadFromJsonAsync(string jsonPath);

    /// <summary>
    /// Seeds geographic catalogs from JSON data.
    /// </summary>
    /// <param name="zonaData">Parsed zone data from JSON</param>
    /// <param name="adminUserId">User ID for audit tracking</param>
    /// <param name="clearExisting">If true, clears existing data before seeding</param>
    Task SeedGeographicCatalogsAsync(
        List<RegionJsonZona> zonaData,
        Guid adminUserId,
        bool clearExisting = false);

    /// <summary>
    /// Clears all geographic catalog data from the database.
    /// Nullifies report references before deletion.
    /// </summary>
    Task ClearGeographicCatalogsAsync();

    /// <summary>
    /// Gets statistics about the current geographic catalog data.
    /// </summary>
    Task<RegionDataLoader.SeedingStats> GetCurrentStatsAsync();
}
