using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ceiba.Infrastructure.Data.Seeding;

/// <summary>
/// Production seed service for essential data.
/// Seeds roles and field suggestions.
/// Does NOT seed users - those are created via Setup Wizard or admin interface.
/// </summary>
public class ProductionSeedService : IProductionSeedService
{
    private readonly CeibaDbContext _context;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ILogger<ProductionSeedService> _logger;

    /// <summary>
    /// Standard roles for the application.
    /// </summary>
    public static readonly string[] Roles = { "CREADOR", "REVISOR", "ADMIN" };

    public ProductionSeedService(
        CeibaDbContext context,
        RoleManager<IdentityRole<Guid>> roleManager,
        ILogger<ProductionSeedService> logger)
    {
        _context = context;
        _roleManager = roleManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SeedAsync()
    {
        _logger.LogInformation("Starting production seed...");
        await SeedRolesAsync();
        // Note: Suggestions require a user ID. They will be seeded
        // when the first admin is created via Setup Wizard.
        _logger.LogInformation("Production seed completed");
    }

    /// <inheritdoc />
    public async Task SeedRolesAsync()
    {
        foreach (var roleName in Roles)
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

    /// <inheritdoc />
    public async Task SeedSugerenciasAsync(Guid creatorUserId)
    {
        // Get existing sugerencias to enable incremental seeding
        var existingSugerencias = await _context.CatalogosSugerencia
            .Select(s => new { s.Campo, s.Valor })
            .ToListAsync();
        var existingSet = existingSugerencias
            .Select(s => $"{s.Campo}:{s.Valor}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var allSugerencias = GetDefaultSugerencias(creatorUserId);

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
    /// Returns the default list of suggestions for all fields.
    /// </summary>
    public static List<Core.Entities.CatalogoSugerencia> GetDefaultSugerencias(Guid creatorUserId)
    {
        return new List<Core.Entities.CatalogoSugerencia>
        {
            // Sexo
            new() { Campo = "sexo", Valor = "Hombre", Orden = 1, Activo = true, UsuarioId = creatorUserId },
            new() { Campo = "sexo", Valor = "Mujer", Orden = 2, Activo = true, UsuarioId = creatorUserId },
            new() { Campo = "sexo", Valor = "No binario", Orden = 3, Activo = true, UsuarioId = creatorUserId },
            new() { Campo = "sexo", Valor = "Prefiere no decir", Orden = 4, Activo = true, UsuarioId = creatorUserId },

            // Tipo de Delito
            new() { Campo = "delito", Valor = "Violencia familiar", Orden = 1, Activo = true, UsuarioId = creatorUserId },
            new() { Campo = "delito", Valor = "Abuso sexual", Orden = 2, Activo = true, UsuarioId = creatorUserId },
            new() { Campo = "delito", Valor = "Acoso sexual", Orden = 3, Activo = true, UsuarioId = creatorUserId },
            new() { Campo = "delito", Valor = "Violación", Orden = 4, Activo = true, UsuarioId = creatorUserId },
            new() { Campo = "delito", Valor = "Tentativa de feminicidio", Orden = 5, Activo = true, UsuarioId = creatorUserId },
            new() { Campo = "delito", Valor = "Feminicidio", Orden = 6, Activo = true, UsuarioId = creatorUserId },
            new() { Campo = "delito", Valor = "Violencia vicaria", Orden = 7, Activo = true, UsuarioId = creatorUserId },
            new() { Campo = "delito", Valor = "Amenazas", Orden = 8, Activo = true, UsuarioId = creatorUserId },

            // Turno Ceiba
            new() { Campo = "turno_ceiba", Valor = "Balderas 1", Orden = 1, Activo = true, UsuarioId = creatorUserId },
            new() { Campo = "turno_ceiba", Valor = "Balderas 2", Orden = 2, Activo = true, UsuarioId = creatorUserId },
            new() { Campo = "turno_ceiba", Valor = "Balderas 3", Orden = 3, Activo = true, UsuarioId = creatorUserId },
            new() { Campo = "turno_ceiba", Valor = "Nonoalco 1", Orden = 4, Activo = true, UsuarioId = creatorUserId },
            new() { Campo = "turno_ceiba", Valor = "Nonoalco 2", Orden = 5, Activo = true, UsuarioId = creatorUserId },
            new() { Campo = "turno_ceiba", Valor = "Nonoalco 3", Orden = 6, Activo = true, UsuarioId = creatorUserId },

            // Tipo de Atención
            new() { Campo = "tipo_de_atencion", Valor = "Llamada telefónica", Orden = 1, Activo = true, UsuarioId = creatorUserId },
            new() { Campo = "tipo_de_atencion", Valor = "Mensaje de texto", Orden = 2, Activo = true, UsuarioId = creatorUserId },
            new() { Campo = "tipo_de_atencion", Valor = "Radio", Orden = 3, Activo = true, UsuarioId = creatorUserId },
            new() { Campo = "tipo_de_atencion", Valor = "Primer respondiente", Orden = 4, Activo = true, UsuarioId = creatorUserId },

            // Traslados
            new() { Campo = "traslados", Valor = "Sí", Orden = 1, Activo = true, UsuarioId = creatorUserId },
            new() { Campo = "traslados", Valor = "No", Orden = 2, Activo = true, UsuarioId = creatorUserId },
            new() { Campo = "traslados", Valor = "No aplica", Orden = 3, Activo = true, UsuarioId = creatorUserId }
        };
    }
}
