using Ceiba.Shared.DTOs;

namespace Ceiba.Core.Interfaces;

/// <summary>
/// Service interface for report management operations.
/// US1: T033
/// </summary>
public interface IReportService
{
    /// <summary>
    /// Creates a new incident report in Borrador state.
    /// </summary>
    /// <param name="createDto">Report creation data</param>
    /// <param name="usuarioId">ID of the CREADOR user creating the report</param>
    /// <returns>Created report details</returns>
    Task<ReportDto> CreateReportAsync(CreateReportDto createDto, Guid usuarioId);

    /// <summary>
    /// Updates an existing report.
    /// CREADOR can only edit their own reports in Borrador state.
    /// REVISOR can edit any report regardless of state.
    /// </summary>
    /// <param name="reportId">Report ID</param>
    /// <param name="updateDto">Update data</param>
    /// <param name="usuarioId">ID of the user performing the update</param>
    /// <param name="isRevisor">Whether the user has REVISOR role</param>
    /// <returns>Updated report details</returns>
    Task<ReportDto> UpdateReportAsync(int reportId, UpdateReportDto updateDto, Guid usuarioId, bool isRevisor = false);

    /// <summary>
    /// Submits a report, changing estado from Borrador (0) to Entregado (1).
    /// Only the creator can submit their own reports while in Borrador state.
    /// This transition is irreversible.
    /// </summary>
    /// <param name="reportId">Report ID</param>
    /// <param name="usuarioId">ID of the CREADOR user submitting the report</param>
    /// <returns>Updated report details</returns>
    Task<ReportDto> SubmitReportAsync(int reportId, Guid usuarioId);

    /// <summary>
    /// Gets a report by ID.
    /// CREADOR can only view their own reports.
    /// REVISOR can view all reports.
    /// </summary>
    /// <param name="reportId">Report ID</param>
    /// <param name="usuarioId">ID of the user requesting the report</param>
    /// <param name="isRevisor">Whether the user has REVISOR role</param>
    /// <returns>Report details</returns>
    Task<ReportDto> GetReportByIdAsync(int reportId, Guid usuarioId, bool isRevisor = false);

    /// <summary>
    /// Lists reports with filtering and pagination.
    /// CREADOR sees only their own reports.
    /// REVISOR sees all reports.
    /// </summary>
    /// <param name="filter">Filter criteria</param>
    /// <param name="usuarioId">ID of the user requesting the list</param>
    /// <param name="isRevisor">Whether the user has REVISOR role</param>
    /// <returns>Paginated list of reports</returns>
    Task<ReportListResponse> ListReportsAsync(ReportFilterDto filter, Guid usuarioId, bool isRevisor = false);
}
