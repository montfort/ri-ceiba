using Ceiba.Core.Entities;
using Ceiba.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ReloadGeographicCatalogs;

/// <summary>
/// CLI tool to reload geographic catalogs from regiones.json.
/// Usage: dotnet run -- [connection-string] [json-path]
///
/// Environment variables:
/// - ConnectionStrings__DefaultConnection: PostgreSQL connection string
/// - CEIBA_REGIONES_JSON_PATH: Path to regiones.json (optional)
/// </summary>
class Program
{
    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("=== Ceiba - Geographic Catalogs Reload Tool ===\n");

        // Get connection string from args or environment
        var connectionString = args.Length > 0
            ? args[0]
            : Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            Console.WriteLine("Error: No connection string provided.");
            Console.WriteLine("\nUsage:");
            Console.WriteLine("  dotnet run -- \"Host=localhost;Database=ceiba;Username=ceiba;Password=xxx\"");
            Console.WriteLine("\nOr set environment variable:");
            Console.WriteLine("  export ConnectionStrings__DefaultConnection=\"...\"");
            return 1;
        }

        // Get JSON path from args or default locations
        var jsonPath = args.Length > 1
            ? args[1]
            : FindRegionesJson();

        if (jsonPath == null || !File.Exists(jsonPath))
        {
            Console.WriteLine($"Error: regiones.json not found.");
            Console.WriteLine("\nSearched locations:");
            Console.WriteLine("  - ./regiones.json");
            Console.WriteLine("  - ../docs/regiones.json");
            Console.WriteLine("  - CEIBA_REGIONES_JSON_PATH environment variable");
            return 1;
        }

        Console.WriteLine($"Connection: {MaskConnectionString(connectionString)}");
        Console.WriteLine($"JSON Path:  {jsonPath}\n");

        try
        {
            // Setup services
            var services = new ServiceCollection();

            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            services.AddDbContext<CeibaDbContext>(options =>
                options.UseNpgsql(connectionString));

            // Add Identity services (required for SeedDataService)
            services.AddIdentity<Usuario, IdentityRole<Guid>>()
                .AddEntityFrameworkStores<CeibaDbContext>()
                .AddDefaultTokenProviders();

            services.AddScoped<RegionDataLoader>();

            var serviceProvider = services.BuildServiceProvider();

            // Execute reload
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<CeibaDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<RegionDataLoader>>();
            var regionLoader = new RegionDataLoader(context, logger);

            // Check database connection
            Console.WriteLine("Checking database connection...");
            if (!await context.Database.CanConnectAsync())
            {
                Console.WriteLine("Error: Cannot connect to database.");
                return 1;
            }
            Console.WriteLine("Database connection OK.\n");

            // Get current stats
            var statsBefore = await regionLoader.GetCurrentStatsAsync();
            Console.WriteLine($"Current data: {statsBefore}");

            // Confirm if data exists
            if (statsBefore.Zonas > 0)
            {
                Console.WriteLine("\nWARNING: Existing geographic data will be DELETED and replaced!");
                Console.Write("Continue? (y/N): ");
                var response = Console.ReadLine()?.Trim().ToLower();
                if (response != "y" && response != "yes")
                {
                    Console.WriteLine("Operation cancelled.");
                    return 0;
                }
            }

            Console.WriteLine("\nLoading data from JSON...");
            var zonaData = await regionLoader.LoadFromJsonAsync(jsonPath);

            Console.WriteLine("Clearing existing data and inserting new records...");

            // Get admin user ID (use a fixed GUID for this tool)
            var adminUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

            await regionLoader.SeedGeographicCatalogsAsync(zonaData, adminUserId, clearExisting: true);

            // Get final stats
            var statsAfter = await regionLoader.GetCurrentStatsAsync();
            Console.WriteLine($"\nFinal data:  {statsAfter}");
            Console.WriteLine("\nGeographic catalogs reloaded successfully!");

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError: {ex.Message}");
            if (ex.InnerException != null)
                Console.WriteLine($"Inner: {ex.InnerException.Message}");
            return 1;
        }
    }

    static string? FindRegionesJson()
    {
        // Check environment variable first
        var envPath = Environment.GetEnvironmentVariable("CEIBA_REGIONES_JSON_PATH");
        if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
            return envPath;

        // Search common locations
        var searchPaths = new[]
        {
            "regiones.json",
            Path.Combine("..", "..", "docs", "regiones.json"),
            Path.Combine("..", "docs", "regiones.json"),
            Path.Combine("docs", "regiones.json")
        };

        foreach (var path in searchPaths)
        {
            var fullPath = Path.GetFullPath(path);
            if (File.Exists(fullPath))
                return fullPath;
        }

        return null;
    }

    static string MaskConnectionString(string connectionString)
    {
        // Mask password in connection string for display
        var parts = connectionString.Split(';');
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].StartsWith("Password=", StringComparison.OrdinalIgnoreCase))
                parts[i] = "Password=****";
        }
        return string.Join(";", parts);
    }
}
