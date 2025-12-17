using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ceiba.Infrastructure.Data.Seeding;

/// <summary>
/// Service for seeding geographic catalogs (Zona, Region, Sector, Cuadrante).
/// Loads data from regiones.json file.
/// </summary>
public class GeographicSeedService : IGeographicSeedService
{
    private readonly CeibaDbContext _context;
    private readonly IRegionDataLoader _regionDataLoader;
    private readonly ILogger<GeographicSeedService> _logger;

    public GeographicSeedService(
        CeibaDbContext context,
        IRegionDataLoader regionDataLoader,
        ILogger<GeographicSeedService> logger)
    {
        _context = context;
        _regionDataLoader = regionDataLoader;
        _logger = logger;
    }

    /// <summary>
    /// Path to the regiones.json file. Can be overridden via environment variable.
    /// </summary>
    public static string GetRegionesJsonPath()
    {
        // Allow override via environment variable for deployment flexibility
        var envPath = Environment.GetEnvironmentVariable("CEIBA_REGIONES_JSON_PATH");
        if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
            return envPath;

        // Default locations to search (in order of priority)
        var searchPaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "SeedData", "regiones.json"),
            Path.Combine(AppContext.BaseDirectory, "regiones.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "docs", "regiones.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "SeedData", "regiones.json"),
            // Development fallback
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "docs", "regiones.json"))
        };

        return searchPaths.FirstOrDefault(File.Exists)
            ?? throw new FileNotFoundException(
                "regiones.json not found. Set CEIBA_REGIONES_JSON_PATH environment variable or place file in SeedData folder.");
    }

    /// <inheritdoc />
    public async Task SeedAsync()
    {
        await SeedAsync(Guid.Empty); // Use empty GUID as system user
    }

    /// <summary>
    /// Seeds geographic catalogs with the specified creator user ID.
    /// </summary>
    public async Task SeedAsync(Guid creatorUserId)
    {
        // Check if catalogs already seeded
        if (await _context.Zonas.AnyAsync())
        {
            _logger.LogInformation("Geographic catalogs already exist. Skipping seed.");
            return;
        }

        try
        {
            var jsonPath = GetRegionesJsonPath();
            _logger.LogInformation("Loading geographic data from: {Path}", jsonPath);

            var zonaData = await _regionDataLoader.LoadFromJsonAsync(jsonPath);
            await _regionDataLoader.SeedGeographicCatalogsAsync(zonaData, creatorUserId, clearExisting: false);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogWarning(ex, "regiones.json not found. Geographic catalogs will not be seeded.");
        }
        catch (Exception ex) when (ex is not FileNotFoundException)
        {
            _logger.LogError(ex, "Failed to seed geographic catalogs from regiones.json");
            throw new InvalidOperationException(
                $"Failed to seed geographic catalogs. Check that regiones.json is valid JSON and contains the expected structure.", ex);
        }
    }

    /// <inheritdoc />
    public async Task ReloadAsync()
    {
        await ReloadAsync(Guid.Empty);
    }

    /// <summary>
    /// Reloads geographic catalogs with the specified creator user ID.
    /// </summary>
    public async Task ReloadAsync(Guid creatorUserId)
    {
        var jsonPath = GetRegionesJsonPath();
        _logger.LogInformation("Reloading geographic data from: {Path}", jsonPath);

        var zonaData = await _regionDataLoader.LoadFromJsonAsync(jsonPath);
        await _regionDataLoader.SeedGeographicCatalogsAsync(zonaData, creatorUserId, clearExisting: true);
    }
}
