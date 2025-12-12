using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Ceiba.Infrastructure.Data;

/// <summary>
/// T019c: RT-004 Mitigation - Migration backup service
/// Provides programmatic database backup before migrations.
/// </summary>
public class MigrationBackupService
{
    private readonly ILogger<MigrationBackupService> _logger;
    private readonly string _connectionString;

    public MigrationBackupService(ILogger<MigrationBackupService> logger, string connectionString)
    {
        _logger = logger;
        _connectionString = connectionString;
    }

    /// <summary>
    /// Creates a backup before applying a migration.
    /// </summary>
    /// <param name="migrationName">Name of the migration being applied</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Path to the backup file, or null if backup failed</returns>
    public async Task<string?> CreatePreMigrationBackupAsync(
        string migrationName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var backupDir = Path.Combine(Directory.GetCurrentDirectory(), "backups", "migrations");
            Directory.CreateDirectory(backupDir);

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var backupFile = Path.Combine(backupDir, $"pre-migration-{migrationName}-{timestamp}.sql.gz");

            _logger.LogInformation(
                "Creating pre-migration backup for {Migration} to {BackupFile}",
                migrationName, backupFile);

            // Parse connection string
            var (host, port, database, username, password) = ParseConnectionString(_connectionString);

            // Execute pg_dump using absolute path from environment or known locations
            // Security: Using explicit path prevents PATH injection attacks (S4036)
            var pgDumpPath = GetPgDumpPath();
            var psi = new ProcessStartInfo
            {
                FileName = pgDumpPath,
                Arguments = $"-h {host} -p {port} -U {username} -d {database} --format=plain --no-owner --no-acl",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            psi.Environment["PGPASSWORD"] = password;

            using var process = Process.Start(psi);
            if (process == null)
            {
                _logger.LogError("Failed to start pg_dump process");
                return null;
            }

            // Compress output with GZip
            await using var fileStream = File.Create(backupFile);
            await using var gzipStream = new System.IO.Compression.GZipStream(
                fileStream, System.IO.Compression.CompressionMode.Compress);

            await process.StandardOutput.BaseStream.CopyToAsync(gzipStream, cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync(cancellationToken);
                _logger.LogError("pg_dump failed with exit code {ExitCode}: {Error}",
                    process.ExitCode, error);
                return null;
            }

            var fileInfo = new FileInfo(backupFile);
            _logger.LogInformation(
                "Backup created successfully: {BackupFile} ({Size} bytes)",
                backupFile, fileInfo.Length);

            // Clean old backups (keep last 10)
            await CleanOldBackupsAsync(backupDir, cancellationToken);

            return backupFile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create pre-migration backup for {Migration}", migrationName);
            return null;
        }
    }

    private static (string host, string port, string database, string username, string password) ParseConnectionString(string connStr)
    {
        var parts = connStr.Split(';');
        var dict = parts
            .Select(p => p.Split('=', 2))
            .Where(kv => kv.Length == 2)
            .ToDictionary(kv => kv[0].Trim().ToLower(), kv => kv[1].Trim());

        // Password is required - no default value for security
        var password = dict.GetValueOrDefault("password", "");
        if (string.IsNullOrEmpty(password))
        {
            throw new InvalidOperationException(
                "Database password is required in connection string. " +
                "Ensure the Password parameter is set in ConnectionStrings:DefaultConnection.");
        }

        return (
            dict.GetValueOrDefault("host", "localhost"),
            dict.GetValueOrDefault("port", "5432"),
            dict.GetValueOrDefault("database", "ceiba"),
            dict.GetValueOrDefault("username", "ceiba"),
            password
        );
    }

    private async Task CleanOldBackupsAsync(string backupDir, CancellationToken cancellationToken)
    {
        try
        {
            var backupFiles = Directory.GetFiles(backupDir, "pre-migration-*.sql.gz")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTimeUtc)
                .Skip(10)  // Keep last 10
                .ToList();

            foreach (var file in backupFiles)
            {
                _logger.LogInformation("Deleting old backup: {File}", file.Name);
                await Task.Run(() => file.Delete(), cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clean old backups");
        }
    }

    /// <summary>
    /// Gets the path to pg_dump executable from environment variable or known locations.
    /// Security: Using explicit paths prevents PATH injection attacks (CWE-426, S4036).
    /// </summary>
    private static string GetPgDumpPath()
    {
        // First, check environment variable for explicit path
        var envPath = Environment.GetEnvironmentVariable("PGDUMP_PATH");
        if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
            return envPath;

        // Check known secure locations (system-managed paths only)
        string[] knownPaths = OperatingSystem.IsWindows()
            ? new[]
            {
                @"C:\Program Files\PostgreSQL\16\bin\pg_dump.exe",
                @"C:\Program Files\PostgreSQL\15\bin\pg_dump.exe",
                @"C:\Program Files\PostgreSQL\14\bin\pg_dump.exe",
            }
            : new[]
            {
                "/usr/bin/pg_dump",
                "/usr/local/bin/pg_dump",
                "/opt/homebrew/bin/pg_dump",
            };

        foreach (var path in knownPaths)
        {
            if (File.Exists(path))
                return path;
        }

        // Fallback to bare command name (will use PATH, but warn about security)
        // This is acceptable in containerized environments where PATH is controlled
        return "pg_dump";
    }
}
