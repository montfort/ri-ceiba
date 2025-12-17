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
    private readonly UserManager<IdentityUser<Guid>> _userManager;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<SeedOrchestrator> _logger;

    public SeedOrchestrator(
        IProductionSeedService productionSeedService,
        IGeographicSeedService geographicSeedService,
        UserManager<IdentityUser<Guid>> userManager,
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

        // 3. In development, seed test users and suggestions with their ID
        if (_environment.IsDevelopment() && _developmentSeedService != null)
        {
            await _developmentSeedService.SeedAsync();

            // Get admin user ID for suggestions
            var adminUser = await _userManager.FindByEmailAsync("admin@ceiba.local");
            if (adminUser != null)
            {
                await _productionSeedService.SeedSugerenciasAsync(adminUser.Id);
            }
        }
        // 4. In production, suggestions are seeded when first admin is created via Setup Wizard

        _logger.LogInformation("Database seeding completed");
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
}
