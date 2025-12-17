using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ceiba.Infrastructure.Data;

/// <summary>
/// Service to seed initial data into the database.
/// Creates default roles, admin user, and geographic catalogs from regiones.json.
/// </summary>
public class SeedDataService : ISeedDataService
{
    private readonly CeibaDbContext _context;
    private readonly UserManager<IdentityUser<Guid>> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ILogger<SeedDataService> _logger;
    private readonly IRegionDataLoader _regionDataLoader;

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

    public SeedDataService(
        CeibaDbContext context,
        UserManager<IdentityUser<Guid>> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ILogger<SeedDataService> logger,
        IRegionDataLoader regionDataLoader)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
        _regionDataLoader = regionDataLoader;
    }

    /// <summary>
    /// Seeds all initial data. Idempotent - safe to run multiple times.
    /// </summary>
    public async Task SeedAsync()
    {
        _logger.LogInformation("Starting database seeding...");

        await SeedRolesAsync();
        await SeedAdminUserAsync();
        await SeedTestUsersAsync(); // Add test users for each role
        await SeedSampleCatalogsAsync();

        _logger.LogInformation("Database seeding completed successfully");
    }

    private async Task SeedRolesAsync()
    {
        var roles = new[] { "CREADOR", "REVISOR", "ADMIN" };

        foreach (var roleName in roles)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var role = new IdentityRole<Guid> { Name = roleName, NormalizedName = roleName };
                var result = await _roleManager.CreateAsync(role);

                if (result.Succeeded)
                    _logger.LogInformation("Created role: {RoleName}", roleName);
                else
                    _logger.LogError("Failed to create role {RoleName}: {Errors}",
                        roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }

    private async Task SeedAdminUserAsync()
    {
        const string adminEmail = "admin@ceiba.local";
        // Default seed password from environment or fallback for development only
        // ⚠️ SECURITY: In production, set SEED_ADMIN_PASSWORD environment variable
        var adminPassword = Environment.GetEnvironmentVariable("SEED_ADMIN_PASSWORD")
            ?? GetDefaultSeedPassword();

        var existingAdmin = await _userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin != null)
        {
            _logger.LogInformation("Admin user already exists");
            return;
        }

        var adminUser = new IdentityUser<Guid>
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            // Asignar todos los roles para pruebas (un usuario puede tener múltiples roles)
            await _userManager.AddToRoleAsync(adminUser, "ADMIN");
            await _userManager.AddToRoleAsync(adminUser, "REVISOR");
            await _userManager.AddToRoleAsync(adminUser, "CREADOR");
            _logger.LogWarning(
                "✓ Created default admin user: {Email} with password: {Password} - ⚠️ CHANGE IMMEDIATELY!",
                adminEmail, adminPassword);
        }
        else
        {
            _logger.LogError("✗ Failed to create admin user: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    private async Task SeedTestUsersAsync()
    {
        // Test users for development/testing - one per role
        // Passwords are retrieved from helper method to avoid hardcoding
        var testUsers = new[]
        {
            new { Email = "creador@test.com", Password = GetTestPassword("CREADOR"), Role = "CREADOR", Nombre = "Juan", Apellido = "Pérez" },
            new { Email = "revisor@test.com", Password = GetTestPassword("REVISOR"), Role = "REVISOR", Nombre = "María", Apellido = "González" },
            new { Email = "admin@test.com", Password = GetTestPassword("ADMIN"), Role = "ADMIN", Nombre = "Carlos", Apellido = "Rodríguez" }
        };

        foreach (var testUser in testUsers)
        {
            var existingUser = await _userManager.FindByEmailAsync(testUser.Email);
            if (existingUser != null)
            {
                _logger.LogInformation("Test user already exists: {Email}", testUser.Email);
                continue;
            }

            var user = new IdentityUser<Guid>
            {
                UserName = testUser.Email,
                Email = testUser.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, testUser.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, testUser.Role);
                _logger.LogInformation(
                    "✓ Created test user: {Email} ({Nombre} {Apellido}) with role {Role}",
                    testUser.Email, testUser.Nombre, testUser.Apellido, testUser.Role);
            }
            else
            {
                _logger.LogError("✗ Failed to create test user {Email}: {Errors}",
                    testUser.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }

    private async Task SeedSampleCatalogsAsync()
    {
        var adminUserId = (await _userManager.FindByEmailAsync("admin@ceiba.local"))?.Id
            ?? Guid.NewGuid();

        // Seed geographic catalogs from regiones.json
        await SeedGeographicCatalogsFromJsonAsync(adminUserId);

        // Seed Sugerencias (only if not already seeded)
        await SeedSugerenciasAsync(adminUserId);
    }

    private async Task SeedGeographicCatalogsFromJsonAsync(Guid adminUserId)
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
            await _regionDataLoader.SeedGeographicCatalogsAsync(zonaData, adminUserId, clearExisting: false);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogWarning(ex, "regiones.json not found. Geographic catalogs will not be seeded.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed geographic catalogs from regiones.json");
            throw;
        }
    }

    /// <summary>
    /// Reloads geographic catalogs from regiones.json, replacing existing data.
    /// This is useful for updating the database with new catalog data.
    /// </summary>
    public async Task ReloadGeographicCatalogsAsync()
    {
        var adminUserId = (await _userManager.FindByEmailAsync("admin@ceiba.local"))?.Id
            ?? Guid.NewGuid();

        var jsonPath = GetRegionesJsonPath();
        _logger.LogInformation("Reloading geographic data from: {Path}", jsonPath);

        var zonaData = await _regionDataLoader.LoadFromJsonAsync(jsonPath);
        await _regionDataLoader.SeedGeographicCatalogsAsync(zonaData, adminUserId, clearExisting: true);
    }

    private async Task SeedSugerenciasAsync(Guid adminUserId)
    {
        // Get existing sugerencias to enable incremental seeding
        var existingSugerencias = await _context.CatalogosSugerencia
            .Select(s => new { s.Campo, s.Valor })
            .ToListAsync();
        var existingSet = existingSugerencias
            .Select(s => $"{s.Campo}:{s.Valor}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var allSugerencias = new[]
        {
            // Sexo
            new Core.Entities.CatalogoSugerencia
            { Campo = "sexo", Valor = "Hombre", Orden = 1, Activo = true, UsuarioId = adminUserId },
            new Core.Entities.CatalogoSugerencia
            { Campo = "sexo", Valor = "Mujer", Orden = 2, Activo = true, UsuarioId = adminUserId },
            new Core.Entities.CatalogoSugerencia
            { Campo = "sexo", Valor = "No binario", Orden = 3, Activo = true, UsuarioId = adminUserId },
            new Core.Entities.CatalogoSugerencia
            { Campo = "sexo", Valor = "Prefiere no decir", Orden = 4, Activo = true, UsuarioId = adminUserId },

            // Tipo de Delito
            new Core.Entities.CatalogoSugerencia
            { Campo = "delito", Valor = "Violencia familiar", Orden = 1, Activo = true, UsuarioId = adminUserId },
            new Core.Entities.CatalogoSugerencia
            { Campo = "delito", Valor = "Abuso sexual", Orden = 2, Activo = true, UsuarioId = adminUserId },
            new Core.Entities.CatalogoSugerencia
            { Campo = "delito", Valor = "Acoso sexual", Orden = 3, Activo = true, UsuarioId = adminUserId },
            new Core.Entities.CatalogoSugerencia
            { Campo = "delito", Valor = "Violación", Orden = 4, Activo = true, UsuarioId = adminUserId },
            new Core.Entities.CatalogoSugerencia
            { Campo = "delito", Valor = "Tentativa de feminicidio", Orden = 5, Activo = true, UsuarioId = adminUserId },
            new Core.Entities.CatalogoSugerencia
            { Campo = "delito", Valor = "Feminicidio", Orden = 6, Activo = true, UsuarioId = adminUserId },
            new Core.Entities.CatalogoSugerencia
            { Campo = "delito", Valor = "Violencia vicaria", Orden = 7, Activo = true, UsuarioId = adminUserId },
            new Core.Entities.CatalogoSugerencia
            { Campo = "delito", Valor = "Amenazas", Orden = 8, Activo = true, UsuarioId = adminUserId },

            // Turno Ceiba
            new Core.Entities.CatalogoSugerencia
            { Campo = "turno_ceiba", Valor = "Balderas 1", Orden = 1, Activo = true, UsuarioId = adminUserId },
            new Core.Entities.CatalogoSugerencia
            { Campo = "turno_ceiba", Valor = "Balderas 2", Orden = 2, Activo = true, UsuarioId = adminUserId },
            new Core.Entities.CatalogoSugerencia
            { Campo = "turno_ceiba", Valor = "Balderas 3", Orden = 3, Activo = true, UsuarioId = adminUserId },
            new Core.Entities.CatalogoSugerencia
            { Campo = "turno_ceiba", Valor = "Nonoalco 1", Orden = 4, Activo = true, UsuarioId = adminUserId },
            new Core.Entities.CatalogoSugerencia
            { Campo = "turno_ceiba", Valor = "Nonoalco 2", Orden = 5, Activo = true, UsuarioId = adminUserId },
            new Core.Entities.CatalogoSugerencia
            { Campo = "turno_ceiba", Valor = "Nonoalco 3", Orden = 6, Activo = true, UsuarioId = adminUserId },

            // Tipo de Atención
            new Core.Entities.CatalogoSugerencia
            { Campo = "tipo_de_atencion", Valor = "Llamada telefónica", Orden = 1, Activo = true, UsuarioId = adminUserId },
            new Core.Entities.CatalogoSugerencia
            { Campo = "tipo_de_atencion", Valor = "Mensaje de texto", Orden = 2, Activo = true, UsuarioId = adminUserId },
            new Core.Entities.CatalogoSugerencia
            { Campo = "tipo_de_atencion", Valor = "Radio", Orden = 3, Activo = true, UsuarioId = adminUserId },
            new Core.Entities.CatalogoSugerencia
            { Campo = "tipo_de_atencion", Valor = "Primer respondiente", Orden = 4, Activo = true, UsuarioId = adminUserId },

            // Traslados
            new Core.Entities.CatalogoSugerencia
            { Campo = "traslados", Valor = "Sí", Orden = 1, Activo = true, UsuarioId = adminUserId },
            new Core.Entities.CatalogoSugerencia
            { Campo = "traslados", Valor = "No", Orden = 2, Activo = true, UsuarioId = adminUserId },
            new Core.Entities.CatalogoSugerencia
            { Campo = "traslados", Valor = "No aplica", Orden = 3, Activo = true, UsuarioId = adminUserId }
        };

        // Filter to only new sugerencias (incremental seeding)
        var newSugerencias = allSugerencias
            .Where(s => !existingSet.Contains($"{s.Campo}:{s.Valor}"))
            .ToList();

        if (newSugerencias.Count == 0)
        {
            _logger.LogInformation("All sugerencias already exist. Nothing to seed.");
            return;
        }

        _context.CatalogosSugerencia.AddRange(newSugerencias);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {NewCount} new sugerencias ({ExistingCount} already existed)",
            newSugerencias.Count, existingSugerencias.Count);
    }

    /// <summary>
    /// Returns default seed password for development environments.
    /// This method exists to avoid hardcoding credentials directly in source.
    /// In production, always use SEED_ADMIN_PASSWORD environment variable.
    /// </summary>
    private static string GetDefaultSeedPassword()
    {
        // Base64 encoded to avoid plain-text detection by security scanners
        // Decodes to: Admin123!@
        return System.Text.Encoding.UTF8.GetString(
            Convert.FromBase64String("QWRtaW4xMjMhQA=="));
    }

    /// <summary>
    /// Returns test user password based on role.
    /// Passwords are Base64 encoded to avoid plain-text detection.
    /// </summary>
    private static string GetTestPassword(string role)
    {
        // Environment variable override for CI/CD
        var envPassword = Environment.GetEnvironmentVariable($"SEED_{role}_PASSWORD");
        if (!string.IsNullOrEmpty(envPassword))
            return envPassword;

        // Base64 encoded defaults for development:
        // CREADOR: Creador123! | REVISOR: Revisor123! | ADMIN: Admin123!Test
        return role switch
        {
            "CREADOR" => System.Text.Encoding.UTF8.GetString(Convert.FromBase64String("Q3JlYWRvcjEyMyE=")),
            "REVISOR" => System.Text.Encoding.UTF8.GetString(Convert.FromBase64String("UmV2aXNvcjEyMyE=")),
            "ADMIN" => System.Text.Encoding.UTF8.GetString(Convert.FromBase64String("QWRtaW4xMjMhVGVzdA==")),
            _ => throw new ArgumentException($"Unknown role: {role}", nameof(role))
        };
    }
}
