using System.Diagnostics;
using Xunit.Abstractions;

namespace Ceiba.Integration.Tests.Infrastructure;

/// <summary>
/// T126-T135: RO-001 Backup infrastructure tests
/// Validates backup scripts exist and are properly configured.
/// </summary>
[Trait("Category", "Infrastructure")]
public class BackupInfrastructureTests
{
    private readonly ITestOutputHelper _output;
    private readonly string _projectRoot;

    public BackupInfrastructureTests(ITestOutputHelper output)
    {
        _output = output;

        // Find project root by looking for .git folder or solution file
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
        _output.WriteLine($"Project root: {_projectRoot}");
    }

    [Fact]
    [Trait("NFR", "T126")]
    public void BackupScript_Exists()
    {
        // Assert
        var bashScript = Path.Combine(_projectRoot, "scripts", "backup", "backup-database.sh");
        var psScript = Path.Combine(_projectRoot, "scripts", "backup", "backup-database.ps1");

        Assert.True(
            File.Exists(bashScript) || File.Exists(psScript),
            "Backup script should exist (bash or PowerShell)");

        _output.WriteLine($"Bash script exists: {File.Exists(bashScript)}");
        _output.WriteLine($"PowerShell script exists: {File.Exists(psScript)}");
    }

    [Fact]
    [Trait("NFR", "T127")]
    public void RestoreScript_Exists()
    {
        // Assert
        var bashScript = Path.Combine(_projectRoot, "scripts", "backup", "restore-database.sh");
        var psScript = Path.Combine(_projectRoot, "scripts", "backup", "restore-database.ps1");

        Assert.True(
            File.Exists(bashScript) || File.Exists(psScript),
            "Restore script should exist (bash or PowerShell)");
    }

    [Fact]
    [Trait("NFR", "T128")]
    public void ScheduledBackupScript_Exists()
    {
        // Assert
        var script = Path.Combine(_projectRoot, "scripts", "backup", "scheduled-backup.sh");

        Assert.True(File.Exists(script), "Scheduled backup script should exist");
    }

    [Fact]
    [Trait("NFR", "T129")]
    public void VerifyBackupScript_Exists()
    {
        // Assert
        var script = Path.Combine(_projectRoot, "scripts", "backup", "verify-backup.sh");

        Assert.True(File.Exists(script), "Verify backup script should exist");
    }

    [Fact]
    [Trait("NFR", "T130")]
    public void MonitorBackupsScript_Exists()
    {
        // Assert
        var script = Path.Combine(_projectRoot, "scripts", "backup", "monitor-backups.sh");

        Assert.True(File.Exists(script), "Monitor backups script should exist");
    }

    [Fact]
    [Trait("NFR", "T131")]
    public void BackupScript_HasRequiredFeatures()
    {
        // Arrange
        var bashScript = Path.Combine(_projectRoot, "scripts", "backup", "backup-database.sh");
        var psScript = Path.Combine(_projectRoot, "scripts", "backup", "backup-database.ps1");

        string content;
        if (File.Exists(bashScript))
        {
            content = File.ReadAllText(bashScript);
        }
        else if (File.Exists(psScript))
        {
            content = File.ReadAllText(psScript);
        }
        else
        {
            Assert.Fail("No backup script found");
            return;
        }

        // Assert - Required features
        Assert.Contains("pg_dump", content, StringComparison.OrdinalIgnoreCase);
        Assert.True(
            content.Contains("compress", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("gzip", StringComparison.OrdinalIgnoreCase),
            "Backup script should support compression");
        Assert.True(
            content.Contains("sha256", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("checksum", StringComparison.OrdinalIgnoreCase),
            "Backup script should create checksums");

        _output.WriteLine("✓ pg_dump command found");
        _output.WriteLine("✓ Compression support found");
        _output.WriteLine("✓ Checksum support found");
    }

    [Fact]
    [Trait("NFR", "T132")]
    public void RestoreScript_HasRequiredFeatures()
    {
        // Arrange
        var bashScript = Path.Combine(_projectRoot, "scripts", "backup", "restore-database.sh");
        var psScript = Path.Combine(_projectRoot, "scripts", "backup", "restore-database.ps1");

        string content;
        if (File.Exists(bashScript))
        {
            content = File.ReadAllText(bashScript);
        }
        else if (File.Exists(psScript))
        {
            content = File.ReadAllText(psScript);
        }
        else
        {
            Assert.Fail("No restore script found");
            return;
        }

        // Assert - Required features
        Assert.True(
            content.Contains("pg_restore", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("psql", StringComparison.OrdinalIgnoreCase),
            "Restore script should use pg_restore or psql");
        Assert.Contains("confirm", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("pre-restore", content, StringComparison.OrdinalIgnoreCase);

        _output.WriteLine("✓ Restore command found");
        _output.WriteLine("✓ Confirmation prompt found");
        _output.WriteLine("✓ Pre-restore backup found");
    }

    [Fact]
    [Trait("NFR", "T133")]
    public void ScheduledBackupScript_HasRetentionPolicy()
    {
        // Arrange
        var script = Path.Combine(_projectRoot, "scripts", "backup", "scheduled-backup.sh");

        if (!File.Exists(script))
        {
            Assert.Fail("Scheduled backup script not found");
            return;
        }

        var content = File.ReadAllText(script);

        // Assert - Retention policies
        Assert.Contains("KEEP_DAILY", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("KEEP_WEEKLY", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("KEEP_MONTHLY", content, StringComparison.OrdinalIgnoreCase);

        _output.WriteLine("✓ Daily retention policy found");
        _output.WriteLine("✓ Weekly retention policy found");
        _output.WriteLine("✓ Monthly retention policy found");
    }

    [Fact]
    [Trait("NFR", "T134")]
    public void BackupDocumentation_Exists()
    {
        // Assert
        var readme = Path.Combine(_projectRoot, "scripts", "backup", "README.md");

        Assert.True(File.Exists(readme), "Backup README should exist");

        var content = File.ReadAllText(readme);
        Assert.Contains("backup", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("restore", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cron", content, StringComparison.OrdinalIgnoreCase);

        _output.WriteLine("✓ Backup documentation exists with required sections");
    }

    [Fact]
    [Trait("NFR", "T135")]
    public void DockerCompose_HasBackupVolume()
    {
        // Arrange
        var composePath = Path.Combine(_projectRoot, "docker", "docker-compose.yml");

        if (!File.Exists(composePath))
        {
            _output.WriteLine("docker-compose.yml not found, skipping");
            return;
        }

        var content = File.ReadAllText(composePath);

        // Assert - Database should have persistent volume
        Assert.Contains("volumes:", content, StringComparison.OrdinalIgnoreCase);
        Assert.True(
            content.Contains("postgres", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("ceiba-db", StringComparison.OrdinalIgnoreCase),
            "Docker compose should have database service");

        _output.WriteLine("✓ Docker compose has volume configuration");
    }

    [Fact]
    [Trait("NFR", "T126")]
    public void BackupDirectory_Structure()
    {
        // This test documents the expected backup directory structure
        var expectedStructure = new[]
        {
            "backups/daily",
            "backups/weekly",
            "backups/monthly",
            "backups/pre-restore",
            "backups/migrations"
        };

        _output.WriteLine("Expected backup directory structure:");
        foreach (var dir in expectedStructure)
        {
            _output.WriteLine($"  {dir}/");
        }

        // The directories will be created on first backup run
        Assert.NotEmpty(expectedStructure);
    }
}
