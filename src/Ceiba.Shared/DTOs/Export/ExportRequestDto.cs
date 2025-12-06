using System.ComponentModel.DataAnnotations;

namespace Ceiba.Shared.DTOs.Export;

/// <summary>
/// Request DTO for exporting incident reports
/// </summary>
public record ExportRequestDto
{
    /// <summary>
    /// IDs of reports to export. Null means export all accessible reports.
    /// </summary>
    public int[]? ReportIds { get; init; }

    /// <summary>
    /// Export format (PDF or JSON)
    /// </summary>
    [Required]
    public ExportFormat Format { get; init; } = ExportFormat.PDF;

    /// <summary>
    /// Optional export customization options
    /// </summary>
    public ExportOptions? Options { get; init; }
}
