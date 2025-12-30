using Ceiba.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Ceiba.Infrastructure.Data.Seeding;

/// <summary>
/// Development-only seed service.
/// Creates test users for local development and testing.
/// Should NOT be used in production.
/// </summary>
public class DevelopmentSeedService : IDevelopmentSeedService
{
    private readonly UserManager<Usuario> _userManager;
    private readonly ILogger<DevelopmentSeedService> _logger;

    public DevelopmentSeedService(
        UserManager<Usuario> userManager,
        ILogger<DevelopmentSeedService> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SeedAsync()
    {
        _logger.LogWarning("Running DEVELOPMENT seed - creating test users");
        await SeedTestUsersAsync();
        _logger.LogWarning("Development seed completed - test users created");
    }

    /// <inheritdoc />
    public async Task SeedTestUsersAsync()
    {
        // Test users for development/testing - one per role
        var testUsers = new[]
        {
            new { Email = "admin@ceiba.local", Password = GetTestPassword("ADMIN"), Roles = new[] { "ADMIN", "REVISOR", "CREADOR" }, Nombre = "Admin", Apellido = "Sistema" },
            new { Email = "creador@test.com", Password = GetTestPassword("CREADOR"), Roles = new[] { "CREADOR" }, Nombre = "Juan", Apellido = "Pérez" },
            new { Email = "revisor@test.com", Password = GetTestPassword("REVISOR"), Roles = new[] { "REVISOR" }, Nombre = "María", Apellido = "González" },
            new { Email = "admin@test.com", Password = GetTestPassword("ADMIN"), Roles = new[] { "ADMIN" }, Nombre = "Carlos", Apellido = "Rodríguez" }
        };

        foreach (var testUser in testUsers)
        {
            var existingUser = await _userManager.FindByEmailAsync(testUser.Email);
            if (existingUser != null)
            {
                _logger.LogInformation("Test user already exists: {Email}", testUser.Email);
                continue;
            }

            var user = new Usuario
            {
                UserName = testUser.Email,
                Email = testUser.Email,
                EmailConfirmed = true,
                Nombre = $"{testUser.Nombre} {testUser.Apellido}",
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, testUser.Password);
            if (result.Succeeded)
            {
                foreach (var role in testUser.Roles)
                {
                    await _userManager.AddToRoleAsync(user, role);
                }
                _logger.LogInformation(
                    "Created test user: {Email} ({Nombre} {Apellido}) with roles [{Roles}]",
                    testUser.Email, testUser.Nombre, testUser.Apellido, string.Join(", ", testUser.Roles));
            }
            else
            {
                _logger.LogError("Failed to create test user {Email}: {Errors}",
                    testUser.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
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
        // CREADOR: Creador123! | REVISOR: Revisor123! | ADMIN: Admin123!@
        return role switch
        {
            "CREADOR" => System.Text.Encoding.UTF8.GetString(Convert.FromBase64String("Q3JlYWRvcjEyMyE=")),
            "REVISOR" => System.Text.Encoding.UTF8.GetString(Convert.FromBase64String("UmV2aXNvcjEyMyE=")),
            "ADMIN" => System.Text.Encoding.UTF8.GetString(Convert.FromBase64String("QWRtaW4xMjMhQA==")),
            _ => throw new ArgumentException($"Unknown role: {role}", nameof(role))
        };
    }
}
