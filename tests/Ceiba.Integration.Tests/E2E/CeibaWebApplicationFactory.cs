using Ceiba.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Ceiba.Integration.Tests.E2E;

/// <summary>
/// Custom WebApplicationFactory for E2E tests.
/// Configures the application with InMemory database and disables background services.
/// </summary>
public class CeibaWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove all DbContext-related registrations
            var descriptorsToRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<CeibaDbContext>) ||
                           d.ServiceType == typeof(DbContextOptions) ||
                           d.ServiceType == typeof(CeibaDbContext) ||
                           d.ServiceType.Name.Contains("DbContext"))
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // Remove the AuditSaveChangesInterceptor singleton (we'll add a new one)
            var interceptorDescriptor = services.FirstOrDefault(d =>
                d.ServiceType == typeof(AuditSaveChangesInterceptor));
            if (interceptorDescriptor != null)
                services.Remove(interceptorDescriptor);

            // Remove background services that interfere with tests
            services.RemoveAll<IHostedService>();

            // Add new interceptor for tests
            services.AddSingleton<AuditSaveChangesInterceptor>();

            // Add DbContext with InMemory provider
            services.AddDbContext<CeibaDbContext>((sp, options) =>
            {
                options.UseInMemoryDatabase(_databaseName);
                options.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
            });
        });
    }

    /// <summary>
    /// Creates a server host and ensures the database is seeded.
    /// </summary>
    public async Task EnsureDatabaseCreatedAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CeibaDbContext>();
        await db.Database.EnsureCreatedAsync();
        await SeedTestDataAsync(db);
    }

    private static async Task SeedTestDataAsync(CeibaDbContext db)
    {
        // Add minimal test data for E2E tests
        if (!await db.Zonas.AnyAsync())
        {
            db.Zonas.Add(new Core.Entities.Zona
            {
                Nombre = "Zona Norte",
                Activo = true
            });
            await db.SaveChangesAsync();
        }
    }
}
