namespace Ceiba.Shared.DTOs.Export;

/// <summary>
/// Result DTO containing exported report data
/// </summary>
public record ExportResultDto
{
    /// <summary>
    /// Exported file content as byte array
    /// </summary>
    public byte[] Data { get; init; } = Array.Empty<byte>();

    /// <summary>
    /// Suggested filename for download
    /// </summary>
    public string FileName { get; init; } = string.Empty;

    /// <summary>
    /// MIME content type (e.g., "application/pdf", "application/json")
    /// </summary>
    public string ContentType { get; init; } = string.Empty;

    /// <summary>
    /// Number of reports included in export
    /// </summary>
    public int ReportCount { get; init; }

    /// <summary>
    /// UTC timestamp when export was generated
    /// </summary>
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
}
