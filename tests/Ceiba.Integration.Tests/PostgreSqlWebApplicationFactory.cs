using Ceiba.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Ceiba.Integration.Tests;

/// <summary>
/// WebApplicationFactory that uses real PostgreSQL for integration tests.
/// Use this for tests that require real database features like ExecuteDeleteAsync.
/// </summary>
public class PostgreSqlWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _databaseName = $"ceiba_test_{Guid.NewGuid():N}";

    // Connection string for local PostgreSQL
    // Uses credentials: montfort/montfort78
    private string ConnectionString =>
        $"Host=localhost;Port=5432;Database={_databaseName};Username=montfort;Password=montfort78";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(GetContentRootPath());

        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registrations
            var descriptorToRemove = services.Where(
                d => d.ServiceType == typeof(DbContextOptions<CeibaDbContext>) ||
                     d.ServiceType == typeof(CeibaDbContext)).ToList();

            foreach (var descriptor in descriptorToRemove)
            {
                services.Remove(descriptor);
            }

            // Add DbContext using real PostgreSQL
            services.AddDbContext<CeibaDbContext>((sp, options) =>
            {
                options.UseNpgsql(ConnectionString);
                options.EnableSensitiveDataLogging();
            });

            // Add a factory for CeibaDbContext
            services.AddScoped(provider =>
            {
                var optionsBuilder = new DbContextOptionsBuilder<CeibaDbContext>();
                optionsBuilder.UseNpgsql(ConnectionString);
                optionsBuilder.EnableSensitiveDataLogging();
                return new CeibaDbContext(optionsBuilder.Options, null);
            });
        });

        builder.UseEnvironment("Testing");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // Create database and seed data
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<CeibaDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        // Apply migrations to create schema
        db.Database.Migrate();

        // Seed roles
        SeedRoles(roleManager).Wait();

        return host;
    }

    public async Task InitializeAsync()
    {
        // Create the test database
        var masterConnection = "Host=localhost;Port=5432;Database=postgres;Username=montfort;Password=montfort78";
        await using var connection = new Npgsql.NpgsqlConnection(masterConnection);
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"CREATE DATABASE \"{_databaseName}\"";
        try
        {
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "42P04")
        {
            // Database already exists - ignore
        }
    }

    public new async Task DisposeAsync()
    {
        // Drop the test database
        var masterConnection = "Host=localhost;Port=5432;Database=postgres;Username=montfort;Password=montfort78";
        await using var connection = new Npgsql.NpgsqlConnection(masterConnection);
        await connection.OpenAsync();

        // Terminate existing connections
        await using var terminateCmd = connection.CreateCommand();
        terminateCmd.CommandText = $@"
            SELECT pg_terminate_backend(pg_stat_activity.pid)
            FROM pg_stat_activity
            WHERE pg_stat_activity.datname = '{_databaseName}'
            AND pid <> pg_backend_pid()";
        await terminateCmd.ExecuteNonQueryAsync();

        // Drop database
        await using var dropCmd = connection.CreateCommand();
        dropCmd.CommandText = $"DROP DATABASE IF EXISTS \"{_databaseName}\"";
        await dropCmd.ExecuteNonQueryAsync();

        await base.DisposeAsync();
    }

    private static async Task SeedRoles(RoleManager<IdentityRole<Guid>> roleManager)
    {
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
        var testAssemblyPath = typeof(PostgreSqlWebApplicationFactory).Assembly.Location;
        var testProjectDir = Directory.GetParent(testAssemblyPath)!.Parent!.Parent!.Parent!;
        var testsDir = testProjectDir.Parent!;
        var solutionDir = testsDir.Parent!;

        var contentRoot = Path.Combine(solutionDir.FullName, "src", "Ceiba.Web");

        if (!Directory.Exists(contentRoot))
        {
            throw new DirectoryNotFoundException($"Ceiba.Web content root not found at: {contentRoot}");
        }

        return contentRoot;
    }
}
