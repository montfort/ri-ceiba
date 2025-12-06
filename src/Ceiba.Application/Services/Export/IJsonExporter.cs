using Ceiba.Shared.DTOs.Export;

namespace Ceiba.Application.Services.Export;

/// <summary>
/// Service for exporting incident reports to JSON format
/// </summary>
public interface IJsonExporter
{
    /// <summary>
    /// Exports a single report to JSON format
    /// </summary>
    /// <param name="report">Report data to export</param>
    /// <param name="options">Optional export customization</param>
    /// <returns>JSON file as byte array (UTF-8 encoded)</returns>
    byte[] ExportSingleReport(ReportExportDto report, ExportOptions? options = null);

    /// <summary>
    /// Exports multiple reports to JSON format
    /// </summary>
    /// <param name="reports">Collection of reports to export</param>
    /// <param name="options">Optional export customization</param>
    /// <returns>JSON file as byte array (UTF-8 encoded)</returns>
    byte[] ExportMultipleReports(IEnumerable<ReportExportDto> reports, ExportOptions? options = null);
}
