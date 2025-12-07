using Ceiba.Core.Entities;
using Ceiba.Shared.DTOs;

namespace Ceiba.Core.Interfaces;

/// <summary>
/// Service for managing automated daily reports.
/// US4: Reportes Automatizados Diarios con IA.
/// </summary>
public interface IAutomatedReportService
{
    #region Automated Reports

    /// <summary>
    /// Gets a paginated list of automated reports.
    /// </summary>
    Task<List<AutomatedReportListDto>> GetReportsAsync(
        int skip = 0,
        int take = 20,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        bool? enviado = null);

    /// <summary>
    /// Gets the detail of a specific automated report.
    /// </summary>
    Task<AutomatedReportDetailDto?> GetReportByIdAsync(int id);

    /// <summary>
    /// Generates a new automated report for the specified period.
    /// </summary>
    Task<AutomatedReportDetailDto> GenerateReportAsync(
        GenerateReportRequestDto request,
        Guid? userId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an existing report by email.
    /// </summary>
    Task<bool> SendReportByEmailAsync(
        int reportId,
        List<string>? recipients = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an automated report.
    /// </summary>
    Task<bool> DeleteReportAsync(int id);

    /// <summary>
    /// Regenerates the Word document for a report.
    /// </summary>
    Task<string?> RegenerateWordDocumentAsync(int reportId);

    #endregion

    #region Templates

    /// <summary>
    /// Gets all report templates.
    /// </summary>
    Task<List<ReportTemplateListDto>> GetTemplatesAsync(bool includeInactive = false);

    /// <summary>
    /// Gets a template by ID.
    /// </summary>
    Task<ReportTemplateDto?> GetTemplateByIdAsync(int id);

    /// <summary>
    /// Gets the default template.
    /// </summary>
    Task<ReportTemplateDto?> GetDefaultTemplateAsync();

    /// <summary>
    /// Creates a new template.
    /// </summary>
    Task<ReportTemplateDto> CreateTemplateAsync(CreateTemplateDto dto, Guid userId);

    /// <summary>
    /// Updates an existing template.
    /// </summary>
    Task<ReportTemplateDto?> UpdateTemplateAsync(int id, UpdateTemplateDto dto, Guid userId);

    /// <summary>
    /// Deletes a template.
    /// </summary>
    Task<bool> DeleteTemplateAsync(int id);

    /// <summary>
    /// Sets a template as the default.
    /// </summary>
    Task<bool> SetDefaultTemplateAsync(int id);

    #endregion

    #region Statistics

    /// <summary>
    /// Calculates statistics for a given period.
    /// </summary>
    Task<ReportStatisticsDto> CalculateStatisticsAsync(
        DateTime fechaInicio,
        DateTime fechaFin,
        CancellationToken cancellationToken = default);

    #endregion

    #region Configuration

    /// <summary>
    /// Gets the current automated report configuration.
    /// </summary>
    Task<AutomatedReportConfigDto> GetConfigurationAsync();

    /// <summary>
    /// Updates the automated report configuration.
    /// </summary>
    Task UpdateConfigurationAsync(AutomatedReportConfigDto config);

    #endregion
}
