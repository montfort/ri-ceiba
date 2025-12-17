namespace Ceiba.Infrastructure.Data.Seeding;

/// <summary>
/// Base interface for all seed data services.
/// </summary>
public interface ISeedDataService
{
    /// <summary>
    /// Seeds data. Idempotent - safe to run multiple times.
    /// </summary>
    Task SeedAsync();
}

/// <summary>
/// Interface for production seed service.
/// Seeds only essential data: roles and suggestions.
/// Does NOT seed users - those are created via Setup Wizard.
/// </summary>
public interface IProductionSeedService : ISeedDataService
{
    /// <summary>
    /// Seeds roles (CREADOR, REVISOR, ADMIN).
    /// </summary>
    Task SeedRolesAsync();

    /// <summary>
    /// Seeds field suggestions (sexo, delito, turno_ceiba, etc.).
    /// </summary>
    Task SeedSugerenciasAsync(Guid creatorUserId);
}

/// <summary>
/// Interface for geographic catalog seed service.
/// Seeds Zona, Region, Sector, Cuadrante from regiones.json.
/// </summary>
public interface IGeographicSeedService : ISeedDataService
{
    /// <summary>
    /// Reloads geographic catalogs from regiones.json, replacing existing data.
    /// </summary>
    Task ReloadAsync();
}

/// <summary>
/// Interface for development-only seed service.
/// Seeds test users for local development.
/// </summary>
public interface IDevelopmentSeedService : ISeedDataService
{
    /// <summary>
    /// Seeds development test users (creador@test.com, revisor@test.com, admin@test.com).
    /// </summary>
    Task SeedTestUsersAsync();
}

/// <summary>
/// Orchestrator that coordinates all seed services.
/// Determines which services to run based on environment.
/// </summary>
public interface ISeedOrchestrator
{
    /// <summary>
    /// Runs all appropriate seed services for the current environment.
    /// </summary>
    Task SeedAllAsync();

    /// <summary>
    /// Reloads geographic catalogs from regiones.json.
    /// </summary>
    Task ReloadGeographicCatalogsAsync();
}
