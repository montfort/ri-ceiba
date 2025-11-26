using Ceiba.Core.Exceptions;
using Ceiba.Core.Interfaces;
using Ceiba.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ceiba.Web.Controllers;

/// <summary>
/// API controller for incident report management.
/// US1: T039
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        IReportService reportService,
        ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    /// <summary>
    /// Lists reports with filtering and pagination.
    /// CREADOR sees only their own reports.
    /// REVISOR sees all reports.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ReportListResponse>> ListReports([FromQuery] ReportFilterDto filter)
    {
        try
        {
            var usuarioId = GetUsuarioId();
            var isRevisor = User.IsInRole("REVISOR");

            var result = await _reportService.ListReportsAsync(filter, usuarioId, isRevisor);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing reports");
            return StatusCode(500, new { message = "Error al obtener los reportes" });
        }
    }

    /// <summary>
    /// Gets a report by ID.
    /// CREADOR can only view their own reports.
    /// REVISOR can view all reports.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ReportDto>> GetReport(int id)
    {
        try
        {
            var usuarioId = GetUsuarioId();
            var isRevisor = User.IsInRole("REVISOR");

            var report = await _reportService.GetReportByIdAsync(id, usuarioId, isRevisor);
            return Ok(report);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ForbiddenException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting report {ReportId}", id);
            return StatusCode(500, new { message = "Error al obtener el reporte" });
        }
    }

    /// <summary>
    /// Creates a new report.
    /// Only CREADOR can create reports.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "CREADOR")]
    public async Task<ActionResult<ReportDto>> CreateReport([FromBody] CreateReportDto createDto)
    {
        try
        {
            var usuarioId = GetUsuarioId();
            var report = await _reportService.CreateReportAsync(createDto, usuarioId);

            return CreatedAtAction(
                nameof(GetReport),
                new { id = report.Id },
                report
            );
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating report");
            return StatusCode(500, new { message = "Error al crear el reporte" });
        }
    }

    /// <summary>
    /// Updates an existing report.
    /// CREADOR can only edit their own reports while in Borrador state.
    /// REVISOR can edit any report regardless of state.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ReportDto>> UpdateReport(int id, [FromBody] UpdateReportDto updateDto)
    {
        try
        {
            var usuarioId = GetUsuarioId();
            var isRevisor = User.IsInRole("REVISOR");

            var report = await _reportService.UpdateReportAsync(id, updateDto, usuarioId, isRevisor);
            return Ok(report);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating report {ReportId}", id);
            return StatusCode(500, new { message = "Error al actualizar el reporte" });
        }
    }

    /// <summary>
    /// Submits a report (changes estado from Borrador to Entregado).
    /// Only CREADOR can submit their own reports.
    /// </summary>
    [HttpPost("{id}/submit")]
    [Authorize(Roles = "CREADOR")]
    public async Task<ActionResult<ReportDto>> SubmitReport(int id)
    {
        try
        {
            var usuarioId = GetUsuarioId();
            var report = await _reportService.SubmitReportAsync(id, usuarioId);
            return Ok(report);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (BadRequestException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting report {ReportId}", id);
            return StatusCode(500, new { message = "Error al entregar el reporte" });
        }
    }

    #region Helper Methods

    private Guid GetUsuarioId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Usuario no autenticado");
        }
        return userId;
    }

    #endregion
}
