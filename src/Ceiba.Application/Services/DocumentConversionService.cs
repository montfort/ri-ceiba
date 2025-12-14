using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace Ceiba.Application.Services;

/// <summary>
/// Service for converting Markdown documents to Word format using Pandoc.
/// T094: Implement markdown to Word conversion.
/// RT-006 Mitigations: Pandoc availability check, process timeout, error handling.
/// </summary>
public interface IDocumentConversionService
{
    /// <summary>
    /// Converts markdown content to Word document (.docx).
    /// </summary>
    /// <param name="markdownContent">Markdown content to convert</param>
    /// <param name="title">Document title</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Word document as byte array</returns>
    Task<byte[]> ConvertMarkdownToWordAsync(
        string markdownContent,
        string title,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if Pandoc is available on the system.
    /// T094a: Pandoc availability check.
    /// </summary>
    bool IsPandocAvailable();

    /// <summary>
    /// Gets the installed Pandoc version.
    /// </summary>
    string? GetPandocVersion();
}

/// <summary>
/// Pandoc-based document conversion service.
/// </summary>
public class DocumentConversionService : IDocumentConversionService
{
    private readonly ILogger<DocumentConversionService> _logger;
    private bool? _pandocAvailable;
    private string? _pandocVersion;

    /// <summary>
    /// Maximum time allowed for Pandoc conversion (T094b)
    /// </summary>
    public const int ConversionTimeoutSeconds = 60;

    /// <summary>
    /// Maximum input size in characters (T094c)
    /// </summary>
    public const int MaxInputCharacters = 500_000;

    public DocumentConversionService(ILogger<DocumentConversionService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<byte[]> ConvertMarkdownToWordAsync(
        string markdownContent,
        string title,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(markdownContent))
        {
            throw new ArgumentException("Markdown content cannot be empty", nameof(markdownContent));
        }

        // T094c: Input size validation
        if (markdownContent.Length > MaxInputCharacters)
        {
            throw new ArgumentException(
                $"El contenido excede el límite máximo de {MaxInputCharacters:N0} caracteres. " +
                $"Tamaño actual: {markdownContent.Length:N0} caracteres.",
                nameof(markdownContent));
        }

        // T094a: Check Pandoc availability
        if (!IsPandocAvailable())
        {
            _logger.LogError("Pandoc is not available on this system. Word conversion cannot proceed.");
            throw new InvalidOperationException(
                "Pandoc no está disponible en el sistema. " +
                "Por favor, instale Pandoc para habilitar la conversión a Word. " +
                "Visite: https://pandoc.org/installing.html");
        }

        var stopwatch = Stopwatch.StartNew();

        // Security: Create a private subdirectory in temp to avoid publicly writable directory issues
        // This mitigates symlink attacks and unauthorized access to temp files (SonarQube security)
        var tempDir = CreateSecureTempDirectory();
        var inputFile = Path.Combine(tempDir, $"input_{Guid.NewGuid():N}.md");
        var outputFile = Path.Combine(tempDir, $"output_{Guid.NewGuid():N}.docx");

        try
        {
            // Prepare markdown with title
            var fullContent = string.IsNullOrEmpty(title)
                ? markdownContent
                : $"---\ntitle: \"{title}\"\n---\n\n{markdownContent}";

            // Write markdown to temp file (Pandoc works better with files for large content)
            await File.WriteAllTextAsync(inputFile, fullContent, Encoding.UTF8, cancellationToken);

            // Execute Pandoc
            var result = await ExecutePandocAsync(inputFile, outputFile, cancellationToken);

            if (!result.Success)
            {
                _logger.LogError("Pandoc conversion failed: {Error}", result.Error);
                throw new InvalidOperationException($"Error en la conversión: {result.Error}");
            }

            // Read output file
            var wordBytes = await File.ReadAllBytesAsync(outputFile, cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation(
                "Markdown to Word conversion completed. " +
                "Input: {InputChars} chars, Output: {OutputBytes} bytes, Duration: {DurationMs}ms",
                markdownContent.Length, wordBytes.Length, stopwatch.ElapsedMilliseconds);

            return wordBytes;
        }
        finally
        {
            // Cleanup temp files and directory
            TryDeleteFile(inputFile);
            TryDeleteFile(outputFile);
            TryDeleteDirectory(tempDir);
        }
    }

    /// <inheritdoc />
    public bool IsPandocAvailable()
    {
        if (_pandocAvailable.HasValue)
        {
            return _pandocAvailable.Value;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = GetPandocExecutable(),
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                _pandocAvailable = false;
                return false;
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);

            _pandocAvailable = process.ExitCode == 0;

            if (_pandocAvailable.Value && output.StartsWith("pandoc"))
            {
                // Extract version from first line
                var firstLine = output.Split('\n')[0];
                _pandocVersion = firstLine.Replace("pandoc", "").Trim();
                _logger.LogInformation("Pandoc detected: version {Version}", _pandocVersion);
            }

            return _pandocAvailable.Value;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to detect Pandoc availability");
            _pandocAvailable = false;
            return false;
        }
    }

    /// <inheritdoc />
    public string? GetPandocVersion()
    {
        if (!IsPandocAvailable())
        {
            return null;
        }
        return _pandocVersion;
    }

    /// <summary>
    /// Executes Pandoc to convert markdown to Word.
    /// T094b: Process timeout handling.
    /// </summary>
    private async Task<PandocResult> ExecutePandocAsync(
        string inputFile,
        string outputFile,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = GetPandocExecutable(),
            Arguments = $"-f markdown -t docx -o \"{outputFile}\" \"{inputFile}\" --standalone",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        _logger.LogDebug("Executing Pandoc: {FileName} {Arguments}", startInfo.FileName, startInfo.Arguments);

        using var process = new Process { StartInfo = startInfo };

        try
        {
            process.Start();

            // T094b: Timeout handling
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(ConversionTimeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var errorTask = process.StandardError.ReadToEndAsync(linkedCts.Token);

            try
            {
                await process.WaitForExitAsync(linkedCts.Token);
            }
            catch (OperationCanceledException ex) when (timeoutCts.IsCancellationRequested)
            {
                process.Kill(entireProcessTree: true);
                _logger.LogError(ex, "Pandoc conversion timed out after {Timeout}s", ConversionTimeoutSeconds);
                return new PandocResult
                {
                    Success = false,
                    Error = $"La conversión excedió el tiempo límite de {ConversionTimeoutSeconds} segundos."
                };
            }

            var errorOutput = await errorTask;

            if (process.ExitCode != 0)
            {
                return new PandocResult
                {
                    Success = false,
                    Error = string.IsNullOrWhiteSpace(errorOutput)
                        ? $"Pandoc terminó con código de error {process.ExitCode}"
                        : errorOutput
                };
            }

            if (!File.Exists(outputFile))
            {
                return new PandocResult
                {
                    Success = false,
                    Error = "El archivo de salida no fue generado"
                };
            }

            return new PandocResult { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pandoc execution failed");
            return new PandocResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Gets the Pandoc executable path based on OS.
    /// </summary>
    private static string GetPandocExecutable()
    {
        // On Windows, Pandoc is typically in PATH or Program Files
        if (OperatingSystem.IsWindows())
        {
            return "pandoc.exe";
        }

        // On Linux/macOS, it's typically in /usr/bin or /usr/local/bin
        return "pandoc";
    }

    /// <summary>
    /// Creates a secure temporary directory for file operations.
    /// Uses application-specific directory instead of system temp to avoid publicly writable directory risks.
    /// Mitigations for CWE-377/CWE-379:
    /// - Uses unpredictable GUID-based directory names (prevents race conditions)
    /// - Creates under user-specific AppData folder (not publicly writable)
    /// - Directory is cleaned up immediately after use
    /// </summary>
    private static string CreateSecureTempDirectory()
    {
        // Use application data folder instead of system temp (CWE-377, CWE-379 mitigation)
        // AppData/Local is user-specific and not publicly writable
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        // Fallback to CommonApplicationData if LocalApplicationData is not available (e.g., service accounts)
        if (string.IsNullOrEmpty(appDataPath))
        {
            appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        }

        // Create unique subdirectory with unpredictable GUID name (race condition mitigation)
        var secureTempDir = Path.Combine(appDataPath, "Ceiba", "pandoc-temp", Guid.NewGuid().ToString("N"));

        // Create directory - inherits permissions from parent (user-only for LocalApplicationData)
        Directory.CreateDirectory(secureTempDir);

        return secureTempDir;
    }

    /// <summary>
    /// Safely tries to delete a file.
    /// </summary>
    private void TryDeleteFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete temp file: {FilePath}", filePath);
        }
    }

    /// <summary>
    /// Safely tries to delete a directory.
    /// </summary>
    private void TryDeleteDirectory(string dirPath)
    {
        try
        {
            if (Directory.Exists(dirPath))
            {
                Directory.Delete(dirPath, recursive: false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete temp directory: {DirPath}", dirPath);
        }
    }

    private sealed record PandocResult
    {
        public bool Success { get; init; }
        public string? Error { get; init; }
    }
}

/// <summary>
/// T094a: Pandoc availability validator for application startup.
/// </summary>
public static class PandocStartupValidator
{
    /// <summary>
    /// Validates Pandoc availability during application startup.
    /// Logs a warning if Pandoc is not available (non-blocking).
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <returns>True if Pandoc is available, false otherwise</returns>
    public static bool ValidateAndLog(ILogger logger)
    {
        var service = new DocumentConversionService(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<DocumentConversionService>.Instance);

        var isAvailable = service.IsPandocAvailable();
        var version = service.GetPandocVersion();

        if (isAvailable)
        {
            logger.LogInformation(
                "Pandoc availability check PASSED. Version: {Version}. " +
                "Word document conversion is available.",
                version);
        }
        else
        {
            logger.LogWarning(
                "Pandoc availability check FAILED. Word document conversion will not be available. " +
                "To enable this feature, install Pandoc: https://pandoc.org/installing.html");
        }

        return isAvailable;
    }
}
