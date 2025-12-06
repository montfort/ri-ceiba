using Ceiba.Shared.DTOs.Export;

namespace Ceiba.Application.Services.Export;

/// <summary>
/// Service for generating PDF documents from incident reports
/// </summary>
public interface IPdfGenerator
{
    /// <summary>
    /// Generates a PDF document from a single report
    /// </summary>
    /// <param name="report">Report data to export</param>
    /// <returns>PDF file as byte array</returns>
    byte[] GenerateSingleReport(ReportExportDto report);

    /// <summary>
    /// Generates a PDF document containing multiple reports
    /// </summary>
    /// <param name="reports">Collection of reports to export</param>
    /// <returns>PDF file as byte array</returns>
    byte[] GenerateMultipleReports(IEnumerable<ReportExportDto> reports);
}
