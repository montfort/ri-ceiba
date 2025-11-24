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
        const string adminPassword = "Admin123!"; // ⚠️ CHANGE AFTER FIRST LOGIN

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
            await _userManager.AddToRoleAsync(adminUser, "ADMIN");
            _logger.LogWarning(
                "Created default admin user: {Email} with password: {Password} - CHANGE IMMEDIATELY!",
                adminEmail, adminPassword);
        }
        else
        {
            _logger.LogError("Failed to create admin user: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
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

        // Seed Sectores (2 per zona)
        var sectores = new List<Core.Entities.Sector>();
        foreach (var zona in zonas)
        {
            sectores.Add(new Core.Entities.Sector
            {
                Nombre = $"Sector A",
                ZonaId = zona.Id,
                Activo = true,
                UsuarioId = adminUserId
            });
            sectores.Add(new Core.Entities.Sector
            {
                Nombre = $"Sector B",
                ZonaId = zona.Id,
                Activo = true,
                UsuarioId = adminUserId
            });
        }

        _context.Sectores.AddRange(sectores);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} sectores", sectores.Count);

        // Seed Cuadrantes (2 per sector)
        var cuadrantes = new List<Core.Entities.Cuadrante>();
        foreach (var sector in sectores)
        {
            cuadrantes.Add(new Core.Entities.Cuadrante
            {
                Nombre = "Cuadrante 1",
                SectorId = sector.Id,
                Activo = true,
                UsuarioId = adminUserId
            });
            cuadrantes.Add(new Core.Entities.Cuadrante
            {
                Nombre = "Cuadrante 2",
                SectorId = sector.Id,
                Activo = true,
                UsuarioId = adminUserId
            });
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
}
