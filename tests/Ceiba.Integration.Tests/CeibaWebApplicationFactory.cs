using Ceiba.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Ceiba.Integration.Tests;

/// <summary>
/// Custom WebApplicationFactory for integration tests.
/// Configures in-memory database and test-specific services.
/// Each instance uses a unique database name to prevent cross-test contamination.
/// </summary>
public class CeibaWebApplicationFactory : WebApplicationFactory<Program>
{
    // Unique database name per factory instance to prevent test isolation issues
    private readonly string _databaseName = $"CeibaTestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set content root to Ceiba.Web project directory
        builder.UseContentRoot(GetContentRootPath());

        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registrations (both DbContextOptions and factory)
            var descriptorToRemove = services.Where(
                d => d.ServiceType == typeof(DbContextOptions<CeibaDbContext>) ||
                     d.ServiceType == typeof(CeibaDbContext)).ToList();

            foreach (var descriptor in descriptorToRemove)
            {
                services.Remove(descriptor);
            }

            // Add DbContext using in-memory database for testing
            // Use unique database name per factory instance to prevent test isolation issues
            services.AddDbContext<CeibaDbContext>((sp, options) =>
            {
                options.UseInMemoryDatabase(_databaseName);
                options.EnableSensitiveDataLogging();
            });

            // Add a factory for CeibaDbContext that provides null userId for testing
            services.AddScoped(provider =>
            {
                var optionsBuilder = new DbContextOptionsBuilder<CeibaDbContext>();
                optionsBuilder.UseInMemoryDatabase(_databaseName);
                optionsBuilder.EnableSensitiveDataLogging();
                return new CeibaDbContext(optionsBuilder.Options, null);
            });
        });

        builder.UseEnvironment("Testing");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // Seed test data after host is created (catalogs only for MVP)
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<CeibaDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();

        // Seed roles first (required for authorization tests)
        SeedRoles(roleManager).Wait();

        // Then seed catalog data
        SeedCatalogData(db);

        return host;
    }

    private static void SeedCatalogData(CeibaDbContext context)
    {
        // Only seed if data doesn't already exist (InMemory database is shared across tests)
        if (context.Zonas.Any())
        {
            return; // Data already seeded
        }

        // Seed geographic catalogs
        var zona1 = new Ceiba.Core.Entities.Zona { Id = 1, Nombre = "Zona Centro", Activo = true };
        var zona2 = new Ceiba.Core.Entities.Zona { Id = 2, Nombre = "Zona Norte", Activo = true };
        context.Zonas.AddRange(zona1, zona2);

        // Add regions
        var region1 = new Ceiba.Core.Entities.Region { Id = 1, Nombre = "Región Centro", ZonaId = 1, Activo = true };
        var region2 = new Ceiba.Core.Entities.Region { Id = 2, Nombre = "Región Norte", ZonaId = 2, Activo = true };
        context.Regiones.AddRange(region1, region2);

        var sector1 = new Ceiba.Core.Entities.Sector { Id = 1, Nombre = "Sector Centro", RegionId = 1, Activo = true };
        var sector2 = new Ceiba.Core.Entities.Sector { Id = 2, Nombre = "Sector Sur", RegionId = 1, Activo = true };
        var sector3 = new Ceiba.Core.Entities.Sector { Id = 3, Nombre = "Sector Norte", RegionId = 2, Activo = true };
        context.Sectores.AddRange(sector1, sector2, sector3);

        var cuadrante1 = new Ceiba.Core.Entities.Cuadrante { Id = 1, Nombre = "Cuadrante 1", SectorId = 1, Activo = true };
        var cuadrante2 = new Ceiba.Core.Entities.Cuadrante { Id = 2, Nombre = "Cuadrante 2", SectorId = 1, Activo = true };
        var cuadrante3 = new Ceiba.Core.Entities.Cuadrante { Id = 3, Nombre = "Cuadrante 3", SectorId = 2, Activo = true };
        var cuadrante4 = new Ceiba.Core.Entities.Cuadrante { Id = 4, Nombre = "Cuadrante 4", SectorId = 3, Activo = true };
        context.Cuadrantes.AddRange(cuadrante1, cuadrante2, cuadrante3, cuadrante4);

        // Seed suggestion catalogs
        var sugerencias = new[]
        {
            new Ceiba.Core.Entities.CatalogoSugerencia { Campo = "sexo", Valor = "Masculino", Orden = 1, Activo = true },
            new Ceiba.Core.Entities.CatalogoSugerencia { Campo = "sexo", Valor = "Femenino", Orden = 2, Activo = true },
            new Ceiba.Core.Entities.CatalogoSugerencia { Campo = "delito", Valor = "Robo", Orden = 1, Activo = true },
            new Ceiba.Core.Entities.CatalogoSugerencia { Campo = "delito", Valor = "Violencia familiar", Orden = 2, Activo = true },
            new Ceiba.Core.Entities.CatalogoSugerencia { Campo = "tipo_de_atencion", Valor = "Inmediata", Orden = 1, Activo = true },
            new Ceiba.Core.Entities.CatalogoSugerencia { Campo = "tipo_de_atencion", Valor = "Diferida", Orden = 2, Activo = true }
        };
        context.CatalogosSugerencia.AddRange(sugerencias);

        context.SaveChanges();
    }

    private static async Task SeedRoles(RoleManager<IdentityRole<Guid>> roleManager)
    {
        // Create the three application roles required for authorization tests
        var roles = new[] { "CREADOR", "REVISOR", "ADMIN" };

        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var role = new IdentityRole<Guid>(roleName);
                await roleManager.CreateAsync(role);
            }
        }
    }

    private static string GetContentRootPath()
    {
        // Get the test assembly location (e.g., tests/Ceiba.Integration.Tests/bin/Debug/net10.0/Ceiba.Integration.Tests.dll)
        var testAssemblyPath = typeof(CeibaWebApplicationFactory).Assembly.Location;

        // Navigate: Assembly location -> bin -> Debug -> net10.0 -> Ceiba.Integration.Tests (project) -> tests -> solution root
        var testProjectDir = Directory.GetParent(testAssemblyPath)!.Parent!.Parent!.Parent!; // Ceiba.Integration.Tests directory
        var testsDir = testProjectDir.Parent!; // tests directory
        var solutionDir = testsDir.Parent!; // solution root (ri-ceiba)

        // Build path to Ceiba.Web
        var contentRoot = Path.Combine(solutionDir.FullName, "src", "Ceiba.Web");

        if (!Directory.Exists(contentRoot))
        {
            throw new DirectoryNotFoundException($"Ceiba.Web content root not found at: {contentRoot}. " +
                $"Assembly location: {testAssemblyPath}");
        }

        return contentRoot;
    }
}
