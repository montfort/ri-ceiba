using System.Text.Json;
using Xunit.Abstractions;

namespace Ceiba.Integration.Tests.Infrastructure;

/// <summary>
/// T136-T145: RO-002 Resource management tests
/// Validates Docker resource limits, connection pooling, and monitoring configuration.
/// </summary>
[Trait("Category", "Infrastructure")]
public class ResourceManagementTests
{
    private readonly ITestOutputHelper _output;
    private readonly string _projectRoot;

    public ResourceManagementTests(ITestOutputHelper output)
    {
        _output = output;

        var current = Directory.GetCurrentDirectory();
        while (!string.IsNullOrEmpty(current))
        {
            if (File.Exists(Path.Combine(current, "Ceiba.sln")))
            {
                _projectRoot = current;
                break;
            }
            current = Directory.GetParent(current)?.FullName;
        }

        _projectRoot ??= Directory.GetCurrentDirectory();
    }

    [Fact]
    [Trait("NFR", "T136")]
    public void DockerCompose_HasResourceLimits()
    {
        // Arrange
        var composePath = Path.Combine(_projectRoot, "docker", "docker-compose.prod.yml");

        if (!File.Exists(composePath))
        {
            _output.WriteLine("docker-compose.prod.yml not found, skipping");
            return;
        }

        var content = File.ReadAllText(composePath);

        // Assert - Resource limits should be configured
        Assert.Contains("resources:", content);
        Assert.Contains("limits:", content);
        Assert.Contains("memory:", content);
        Assert.Contains("cpus:", content);

        _output.WriteLine("✓ Docker resource limits configured");
    }

    [Fact]
    [Trait("NFR", "T137")]
    public void DockerCompose_HasPostgresOptimization()
    {
        // Arrange
        var composePath = Path.Combine(_projectRoot, "docker", "docker-compose.prod.yml");

        if (!File.Exists(composePath))
        {
            _output.WriteLine("docker-compose.prod.yml not found, skipping");
            return;
        }

        var content = File.ReadAllText(composePath);

        // Assert - PostgreSQL optimization settings
        Assert.Contains("max_connections", content);
        Assert.Contains("shared_buffers", content);

        _output.WriteLine("✓ PostgreSQL optimization configured");
    }

    [Fact]
    [Trait("NFR", "T138")]
    public void AppSettings_HasConnectionPooling()
    {
        // Arrange
        var appsettingsPath = Path.Combine(_projectRoot, "src", "Ceiba.Web", "appsettings.json");

        if (!File.Exists(appsettingsPath))
        {
            Assert.Fail("appsettings.json not found");
            return;
        }

        var content = File.ReadAllText(appsettingsPath);

        // Assert - Connection pooling should be configured
        Assert.Contains("Pooling=true", content, StringComparison.OrdinalIgnoreCase);
        Assert.True(
            content.Contains("MaxPoolSize", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("Max Pool Size", StringComparison.OrdinalIgnoreCase),
            "Max pool size should be configured");

        _output.WriteLine("✓ Connection pooling configured");
    }

    [Fact]
    [Trait("NFR", "T139")]
    public void AppSettings_HasResourceLimitsSection()
    {
        // Arrange
        var appsettingsPath = Path.Combine(_projectRoot, "src", "Ceiba.Web", "appsettings.json");

        if (!File.Exists(appsettingsPath))
        {
            Assert.Fail("appsettings.json not found");
            return;
        }

        var content = File.ReadAllText(appsettingsPath);

        // Assert - ResourceLimits section should exist
        Assert.Contains("ResourceLimits", content);

        _output.WriteLine("✓ ResourceLimits section exists");
    }

    [Fact]
    [Trait("NFR", "T140")]
    public void DockerCompose_HasLoggingConfiguration()
    {
        // Arrange
        var composePath = Path.Combine(_projectRoot, "docker", "docker-compose.prod.yml");

        if (!File.Exists(composePath))
        {
            _output.WriteLine("docker-compose.prod.yml not found, skipping");
            return;
        }

        var content = File.ReadAllText(composePath);

        // Assert - Logging configuration
        Assert.Contains("logging:", content);
        Assert.Contains("max-size", content);
        Assert.Contains("max-file", content);

        _output.WriteLine("✓ Docker logging configured with rotation");
    }

    [Fact]
    [Trait("NFR", "T141")]
    public void DockerCompose_HasHealthChecks()
    {
        // Arrange
        var composePath = Path.Combine(_projectRoot, "docker", "docker-compose.prod.yml");

        if (!File.Exists(composePath))
        {
            _output.WriteLine("docker-compose.prod.yml not found, skipping");
            return;
        }

        var content = File.ReadAllText(composePath);

        // Assert - Health checks should be configured
        Assert.Contains("healthcheck:", content);
        Assert.Contains("interval:", content);
        Assert.Contains("timeout:", content);
        Assert.Contains("retries:", content);

        _output.WriteLine("✓ Docker health checks configured");
    }

    [Fact]
    [Trait("NFR", "T142")]
    public void DockerCompose_HasRestartPolicy()
    {
        // Arrange
        var composePath = Path.Combine(_projectRoot, "docker", "docker-compose.prod.yml");

        if (!File.Exists(composePath))
        {
            _output.WriteLine("docker-compose.prod.yml not found, skipping");
            return;
        }

        var content = File.ReadAllText(composePath);

        // Assert - Restart policy for production
        Assert.Contains("restart: always", content);

        _output.WriteLine("✓ Docker restart policy configured");
    }

    [Fact]
    [Trait("NFR", "T143")]
    public void AppSettings_HasLoggingConfiguration()
    {
        // Arrange
        var appsettingsPath = Path.Combine(_projectRoot, "src", "Ceiba.Web", "appsettings.json");

        if (!File.Exists(appsettingsPath))
        {
            Assert.Fail("appsettings.json not found");
            return;
        }

        var content = File.ReadAllText(appsettingsPath);

        // Assert - Serilog configuration
        Assert.Contains("Serilog", content);
        Assert.Contains("WriteTo", content);
        Assert.Contains("rollingInterval", content);
        Assert.Contains("retainedFileCountLimit", content);

        _output.WriteLine("✓ Logging configuration with rotation");
    }

    [Fact]
    [Trait("NFR", "T144")]
    public void AppSettings_ParsesCorrectly()
    {
        // Arrange
        var appsettingsPath = Path.Combine(_projectRoot, "src", "Ceiba.Web", "appsettings.json");

        if (!File.Exists(appsettingsPath))
        {
            Assert.Fail("appsettings.json not found");
            return;
        }

        var content = File.ReadAllText(appsettingsPath);

        // Act & Assert - Should parse as valid JSON
        var exception = Record.Exception(() => JsonDocument.Parse(content));
        Assert.Null(exception);

        _output.WriteLine("✓ appsettings.json is valid JSON");
    }

    [Fact]
    [Trait("NFR", "T145")]
    public void DockerCompose_NetworkConfiguration()
    {
        // Arrange
        var composePath = Path.Combine(_projectRoot, "docker", "docker-compose.prod.yml");

        if (!File.Exists(composePath))
        {
            _output.WriteLine("docker-compose.prod.yml not found, skipping");
            return;
        }

        var content = File.ReadAllText(composePath);

        // Assert - Network isolation
        Assert.Contains("networks:", content);
        Assert.Contains("ceiba-network", content);

        _output.WriteLine("✓ Docker network isolation configured");
    }
}
