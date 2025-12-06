namespace Ceiba.Shared.DTOs.Export;

/// <summary>
/// Options for customizing export output
/// </summary>
public record ExportOptions
{
    /// <summary>
    /// Include export metadata (date, user, version)
    /// </summary>
    public bool IncludeMetadata { get; init; } = true;

    /// <summary>
    /// Include audit trail information in export
    /// </summary>
    public bool IncludeAuditInfo { get; init; } = false;

    /// <summary>
    /// Custom filename for the export (without extension)
    /// </summary>
    public string? FileName { get; init; }
}
