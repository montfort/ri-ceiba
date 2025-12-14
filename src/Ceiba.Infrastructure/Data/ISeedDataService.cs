namespace Ceiba.Infrastructure.Data;

/// <summary>
/// Interface for database seeding operations.
/// Enables dependency injection and testability.
/// </summary>
public interface ISeedDataService
{
    /// <summary>
    /// Seeds all initial data. Idempotent - safe to run multiple times.
    /// </summary>
    Task SeedAsync();

    /// <summary>
    /// Reloads geographic catalogs from regiones.json, replacing existing data.
    /// This is useful for updating the database with new catalog data.
    /// </summary>
    Task ReloadGeographicCatalogsAsync();
}
