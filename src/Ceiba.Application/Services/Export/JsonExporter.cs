using System.Text;
using System.Text.Json;
using Ceiba.Shared.DTOs.Export;

namespace Ceiba.Application.Services.Export;

/// <summary>
/// JSON export service implementation
/// Implements T-US2-011 to T-US2-020 test requirements
/// </summary>
public class JsonExporter : IJsonExporter
{
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly JsonSerializerOptions _compactSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// Exports a single report to JSON format
    /// </summary>
    /// <param name="report">Report data to export</param>
    /// <param name="options">Optional export customization</param>
    /// <returns>JSON file as byte array (UTF-8 encoded)</returns>
    /// <exception cref="ArgumentNullException">If report is null</exception>
    public byte[] ExportSingleReport(ReportExportDto report, ExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(report);

        var optionsToUse = options?.IncludeMetadata == true
            ? _serializerOptions
            : _compactSerializerOptions;

        var jsonString = JsonSerializer.Serialize(report, optionsToUse);
        return Encoding.UTF8.GetBytes(jsonString);
    }

    /// <summary>
    /// Exports multiple reports to JSON format
    /// </summary>
    /// <param name="reports">Collection of reports to export</param>
    /// <param name="options">Optional export customization</param>
    /// <returns>JSON file as byte array (UTF-8 encoded)</returns>
    /// <exception cref="ArgumentException">If reports collection is empty</exception>
    public byte[] ExportMultipleReports(IEnumerable<ReportExportDto> reports, ExportOptions? options = null)
    {
        var reportList = reports?.ToList() ?? throw new ArgumentException("Reports collection cannot be null", nameof(reports));

        if (reportList.Count == 0)
        {
            throw new ArgumentException("Must provide at least one report to export", nameof(reports));
        }

        var optionsToUse = options?.IncludeMetadata == true
            ? _serializerOptions
            : _compactSerializerOptions;

        var jsonString = JsonSerializer.Serialize(reportList, optionsToUse);
        return Encoding.UTF8.GetBytes(jsonString);
    }
}
