using Ceiba.Core.Interfaces;
using Ceiba.Infrastructure.Data;
using Ceiba.Infrastructure.Data.Seeding;
using Ceiba.Shared.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ceiba.Infrastructure.Services;

/// <summary>
/// Service for managing the initial application setup (WordPress-style wizard).
/// Handles detection of setup requirement and first admin user creation.
/// </summary>
public class SetupService : ISetupService
{
    private readonly CeibaDbContext _context;
    private readonly UserManager<IdentityUser<Guid>> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly IProductionSeedService _productionSeedService;
    private readonly IGeographicSeedService _geographicSeedService;
    private readonly ILogger<SetupService> _logger;

    public SetupService(
        CeibaDbContext context,
        UserManager<IdentityUser<Guid>> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IProductionSeedService productionSeedService,
        IGeographicSeedService geographicSeedService,
        ILogger<SetupService> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _productionSeedService = productionSeedService;
        _geographicSeedService = geographicSeedService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SetupStatus> GetStatusAsync()
    {
        var hasUsers = await _userManager.Users.AnyAsync();
        var hasRoles = await _roleManager.Roles.AnyAsync();
        var hasGeographicCatalogs = await _context.Zonas.AnyAsync();
        var hasSuggestions = await _context.CatalogosSugerencia.AnyAsync();

        var isComplete = hasUsers && hasRoles && hasGeographicCatalogs && hasSuggestions;

        string message;
        if (isComplete)
        {
            message = "La aplicación está configurada correctamente.";
        }
        else if (!hasUsers)
        {
            message = "Se requiere crear el primer usuario administrador.";
        }
        else if (!hasGeographicCatalogs)
        {
            message = "Los catálogos geográficos no han sido cargados.";
        }
        else if (!hasSuggestions)
        {
            message = "Las sugerencias de campos no han sido configuradas.";
        }
        else
        {
            message = "Configuración incompleta.";
        }

        return new SetupStatus
        {
            IsComplete = isComplete,
            HasUsers = hasUsers,
            HasRoles = hasRoles,
            HasGeographicCatalogs = hasGeographicCatalogs,
            HasSuggestions = hasSuggestions,
            Message = message
        };
    }

    /// <inheritdoc />
    public async Task<bool> IsSetupRequiredAsync()
    {
        // Setup is required if no users exist
        return !await _userManager.Users.AnyAsync();
    }

    /// <inheritdoc />
    public async Task MarkSetupCompleteAsync()
    {
        _logger.LogInformation("Setup marked as complete");
        // The setup is considered complete when:
        // 1. At least one user exists
        // 2. Roles are created
        // 3. Geographic catalogs are loaded
        // 4. Suggestions are seeded
        // This is detected automatically by GetStatusAsync
    }

    /// <inheritdoc />
    public async Task<SetupResult> CreateFirstAdminAsync(CreateFirstAdminDto dto)
    {
        _logger.LogInformation("Starting first admin creation for {Email}", dto.Email);

        try
        {
            // Validate that no users exist yet
            if (await _userManager.Users.AnyAsync())
            {
                _logger.LogWarning("Attempted to create first admin when users already exist");
                return SetupResult.Failed("Ya existe al menos un usuario en el sistema. El setup ya fue completado.");
            }

            // Ensure roles exist
            await _productionSeedService.SeedRolesAsync();

            // Create the admin user
            var adminUser = new IdentityUser<Guid>
            {
                UserName = dto.Email,
                Email = dto.Email,
                EmailConfirmed = true // Auto-confirm for first admin
            };

            var createResult = await _userManager.CreateAsync(adminUser, dto.Password);
            if (!createResult.Succeeded)
            {
                var errors = createResult.Errors.Select(e => e.Description).ToArray();
                _logger.LogError("Failed to create admin user: {Errors}", string.Join(", ", errors));
                return SetupResult.Failed(errors);
            }

            // Assign all roles to the first admin (full access)
            foreach (var role in ProductionSeedService.Roles)
            {
                await _userManager.AddToRoleAsync(adminUser, role);
            }

            _logger.LogInformation("Created admin user {Email} with ID {UserId}", dto.Email, adminUser.Id);

            // Seed geographic catalogs if not already present
            if (_geographicSeedService is GeographicSeedService geoService)
            {
                await geoService.SeedAsync(adminUser.Id);
            }
            else
            {
                await _geographicSeedService.SeedAsync();
            }

            // Seed suggestions with the new admin's ID
            await _productionSeedService.SeedSugerenciasAsync(adminUser.Id);

            _logger.LogInformation("Initial setup completed successfully");

            return SetupResult.Succeeded(adminUser.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during first admin creation");
            return SetupResult.Failed($"Error inesperado: {ex.Message}");
        }
    }
}
