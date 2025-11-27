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
/// </summary>
public class CeibaWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
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
            services.AddDbContext<CeibaDbContext>((sp, options) =>
            {
                options.UseInMemoryDatabase("CeibaTestDb");
                options.EnableSensitiveDataLogging();
            });

            // Add a factory for CeibaDbContext that provides null userId for testing
            services.AddScoped(provider =>
            {
                var optionsBuilder = new DbContextOptionsBuilder<CeibaDbContext>();
                optionsBuilder.UseInMemoryDatabase("CeibaTestDb");
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

        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();

        SeedCatalogData(db);

        return host;
    }

    private static void SeedCatalogData(CeibaDbContext context)
    {
        // Seed geographic catalogs
        var zona1 = new Ceiba.Core.Entities.Zona { Id = 1, Nombre = "Zona Centro", Activo = true };
        var zona2 = new Ceiba.Core.Entities.Zona { Id = 2, Nombre = "Zona Norte", Activo = true };
        context.Zonas.AddRange(zona1, zona2);

        var sector1 = new Ceiba.Core.Entities.Sector { Id = 1, Nombre = "Sector A", ZonaId = 1, Activo = true };
        var sector2 = new Ceiba.Core.Entities.Sector { Id = 2, Nombre = "Sector B", ZonaId = 1, Activo = true };
        var sector3 = new Ceiba.Core.Entities.Sector { Id = 3, Nombre = "Sector C", ZonaId = 2, Activo = true };
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
}
