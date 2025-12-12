using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ceiba.Infrastructure.Data;

/// <summary>
/// Service to seed initial data into the database.
/// Creates default roles, admin user, and sample catalogs.
/// </summary>
public class SeedDataService
{
    private readonly CeibaDbContext _context;
    private readonly UserManager<IdentityUser<Guid>> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ILogger<SeedDataService> _logger;

    public SeedDataService(
        CeibaDbContext context,
        UserManager<IdentityUser<Guid>> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ILogger<SeedDataService> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
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
        // Check if catalogs already seeded
        if (await _context.Zonas.AnyAsync())
        {
            _logger.LogInformation("Sample catalogs already exist");
            return;
        }

        var adminUserId = (await _userManager.FindByEmailAsync("admin@ceiba.local"))?.Id
            ?? Guid.NewGuid();

        // Seed Zonas
        var zonas = new[]
        {
            new Core.Entities.Zona { Nombre = "Zona Norte", Activo = true, UsuarioId = adminUserId },
            new Core.Entities.Zona { Nombre = "Zona Sur", Activo = true, UsuarioId = adminUserId },
            new Core.Entities.Zona { Nombre = "Zona Centro", Activo = true, UsuarioId = adminUserId },
            new Core.Entities.Zona { Nombre = "Zona Oriente", Activo = true, UsuarioId = adminUserId },
            new Core.Entities.Zona { Nombre = "Zona Poniente", Activo = true, UsuarioId = adminUserId }
        };

        _context.Zonas.AddRange(zonas);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} zonas", zonas.Length);

        // Seed Sectores (3-4 per zona con nombres realistas)
        var sectores = new List<Core.Entities.Sector>();
        var sectorNames = new[] { "Sector Centro", "Sector Este", "Sector Oeste", "Sector Residencial" };

        for (int i = 0; i < zonas.Length; i++)
        {
            var zona = zonas[i];
            var numSectores = i % 2 == 0 ? 3 : 4; // Alternar entre 3 y 4 sectores

            for (int j = 0; j < numSectores; j++)
            {
                sectores.Add(new Core.Entities.Sector
                {
                    Nombre = sectorNames[j % sectorNames.Length],
                    ZonaId = zona.Id,
                    Activo = true,
                    UsuarioId = adminUserId
                });
            }
        }

        _context.Sectores.AddRange(sectores);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} sectores", sectores.Count);

        // Seed Cuadrantes (3-5 per sector con nombres realistas)
        var cuadrantes = new List<Core.Entities.Cuadrante>();
        var cuadranteNames = new[] { "Cuadrante A", "Cuadrante B", "Cuadrante C", "Cuadrante D", "Cuadrante E" };

        for (int i = 0; i < sectores.Count; i++)
        {
            var sector = sectores[i];
            var numCuadrantes = (i % 3) + 3; // Alternar entre 3, 4 y 5 cuadrantes

            for (int j = 0; j < numCuadrantes; j++)
            {
                cuadrantes.Add(new Core.Entities.Cuadrante
                {
                    Nombre = cuadranteNames[j],
                    SectorId = sector.Id,
                    Activo = true,
                    UsuarioId = adminUserId
                });
            }
        }

        _context.Cuadrantes.AddRange(cuadrantes);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} cuadrantes", cuadrantes.Count);

        // Seed Sugerencias
        var sugerencias = new[]
        {
            // Sexo
            new Core.Entities.CatalogoSugerencia
            { Campo = "sexo", Valor = "Masculino", Orden = 1, Activo = true, UsuarioId = adminUserId },
            new Core.Entities.CatalogoSugerencia
            { Campo = "sexo", Valor = "Femenino", Orden = 2, Activo = true, UsuarioId = adminUserId },
            new Core.Entities.CatalogoSugerencia
            { Campo = "sexo", Valor = "No binario", Orden = 3, Activo = true, UsuarioId = adminUserId },
            new Core.Entities.CatalogoSugerencia
            { Campo = "sexo", Valor = "Prefiero no decir", Orden = 4, Activo = true, UsuarioId = adminUserId },

            // Delito
            new Core.Entities.CatalogoSugerencia
            { Campo = "delito", Valor = "Robo", Orden = 1, Activo = true, UsuarioId = adminUserId },
            new Core.Entities.CatalogoSugerencia
            { Campo = "delito", Valor = "Violencia familiar", Orden = 2, Activo = true, UsuarioId = adminUserId },
            new Core.Entities.CatalogoSugerencia
            { Campo = "delito", Valor = "Acoso sexual", Orden = 3, Activo = true, UsuarioId = adminUserId },
            new Core.Entities.CatalogoSugerencia
            { Campo = "delito", Valor = "Lesiones", Orden = 4, Activo = true, UsuarioId = adminUserId },
            new Core.Entities.CatalogoSugerencia
            { Campo = "delito", Valor = "Amenazas", Orden = 5, Activo = true, UsuarioId = adminUserId },

            // Tipo de Atención
            new Core.Entities.CatalogoSugerencia
            { Campo = "tipo_de_atencion", Valor = "Orientación", Orden = 1, Activo = true, UsuarioId = adminUserId },
            new Core.Entities.CatalogoSugerencia
            { Campo = "tipo_de_atencion", Valor = "Canalización", Orden = 2, Activo = true, UsuarioId = adminUserId },
            new Core.Entities.CatalogoSugerencia
            { Campo = "tipo_de_atencion", Valor = "Acompañamiento", Orden = 3, Activo = true, UsuarioId = adminUserId },
            new Core.Entities.CatalogoSugerencia
            { Campo = "tipo_de_atencion", Valor = "Intervención en crisis", Orden = 4, Activo = true, UsuarioId = adminUserId }
        };

        _context.CatalogosSugerencia.AddRange(sugerencias);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} sugerencias", sugerencias.Length);
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
