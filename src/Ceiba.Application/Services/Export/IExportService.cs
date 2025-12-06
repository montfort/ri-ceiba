using Ceiba.Shared.DTOs.Export;

namespace Ceiba.Application.Services.Export;

/// <summary>
/// High-level service for exporting incident reports
/// Handles authorization, data retrieval, and format conversion
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Exports reports based on request parameters
    /// </summary>
    /// <param name="request">Export request with format and options</param>
    /// <param name="userId">ID of user requesting export</param>
    /// <param name="isRevisor">Whether user has REVISOR role</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Export result with file data and metadata</returns>
    Task<ExportResultDto> ExportReportsAsync(
        ExportRequestDto request,
        Guid userId,
        bool isRevisor,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports a single report by ID
    /// </summary>
    /// <param name="reportId">ID of report to export</param>
    /// <param name="format">Export format (PDF or JSON)</param>
    /// <param name="userId">ID of user requesting export</param>
    /// <param name="isRevisor">Whether user has REVISOR role</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Export result with file data and metadata</returns>
    Task<ExportResultDto> ExportSingleReportAsync(
        int reportId,
        ExportFormat format,
        Guid userId,
        bool isRevisor,
        CancellationToken cancellationToken = default);
}
