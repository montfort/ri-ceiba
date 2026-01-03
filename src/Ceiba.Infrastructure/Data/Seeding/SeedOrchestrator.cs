using Ceiba.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ceiba.Infrastructure.Data.Seeding;

/// <summary>
/// Orchestrates all seeding services based on environment.
/// In Production: Seeds only roles and geographic data.
/// In Development: Also seeds test users.
/// </summary>
public class SeedOrchestrator : ISeedOrchestrator
{
    private readonly IProductionSeedService _productionSeedService;
    private readonly IGeographicSeedService _geographicSeedService;
    private readonly IDevelopmentSeedService? _developmentSeedService;
    private readonly UserManager<Usuario> _userManager;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<SeedOrchestrator> _logger;

    public SeedOrchestrator(
        IProductionSeedService productionSeedService,
        IGeographicSeedService geographicSeedService,
        UserManager<Usuario> userManager,
        IHostEnvironment environment,
        ILogger<SeedOrchestrator> logger,
        IDevelopmentSeedService? developmentSeedService = null)
    {
        _productionSeedService = productionSeedService;
        _geographicSeedService = geographicSeedService;
        _userManager = userManager;
        _environment = environment;
        _logger = logger;
        _developmentSeedService = developmentSeedService;
    }

    /// <inheritdoc />
    public async Task SeedAllAsync()
    {
        _logger.LogInformation("Starting database seeding in {Environment} environment...",
            _environment.EnvironmentName);

        // 1. Always seed roles (required for all environments)
        await _productionSeedService.SeedRolesAsync();

        // 2. Always seed geographic catalogs
        await _geographicSeedService.SeedAsync();

        // 3. Users and suggestions are NOT seeded automatically.
        //    The Setup Wizard handles first admin creation and seeds suggestions.
        //    This ensures consistent behavior across all environments.

        _logger.LogInformation("Database seeding completed (roles and geographic catalogs only)");
        _logger.LogInformation("First administrator should be created via Setup Wizard at /setup");
    }

    /// <inheritdoc />
    public async Task ReloadGeographicCatalogsAsync()
    {
        _logger.LogInformation("Reloading geographic catalogs...");

        if (_geographicSeedService is GeographicSeedService geoService)
        {
            // Try to get an admin user for the creator ID
            var adminUser = await _userManager.FindByEmailAsync("admin@ceiba.local")
                ?? await _userManager.Users.FirstOrDefaultAsync();

            await geoService.ReloadAsync(adminUser?.Id ?? Guid.Empty);
        }
        else
        {
            await _geographicSeedService.ReloadAsync();
        }

        _logger.LogInformation("Geographic catalogs reloaded");
    }

    /// <summary>
    /// Seeds test users for development purposes.
    /// This method should be called manually (e.g., via admin endpoint) when test users are needed.
    /// Only available in Development environment.
    /// </summary>
    /// <returns>True if test users were seeded, false if not available.</returns>
    public async Task<bool> SeedTestUsersAsync()
    {
        if (!_environment.IsDevelopment())
        {
            _logger.LogWarning("Test user seeding is only available in Development environment");
            return false;
        }

        if (_developmentSeedService == null)
        {
            _logger.LogWarning("DevelopmentSeedService is not registered");
            return false;
        }

        _logger.LogWarning("Manually seeding test users for development...");
        await _developmentSeedService.SeedAsync();

        // Get admin user ID for suggestions
        var adminUser = await _userManager.FindByEmailAsync("admin@ceiba.local");
        if (adminUser != null)
        {
            await _productionSeedService.SeedSugerenciasAsync(adminUser.Id);
        }

        _logger.LogWarning("Test users seeded successfully");
        return true;
    }
}
