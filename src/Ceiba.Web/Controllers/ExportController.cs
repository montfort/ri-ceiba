using Ceiba.Application.Services.Export;
using Ceiba.Core.Interfaces;
using Ceiba.Shared.DTOs.Export;
using Ceiba.Web.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ceiba.Web.Controllers;

/// <summary>
/// API controller for report export functionality.
/// US2: Supervisor Review and Export - REVISOR role only
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AuthorizeBeforeModelBinding("REVISOR")] // Only REVISOR can access export
[IgnoreAntiforgeryToken] // APIs REST don't use antiforgery tokens
public class ExportController : ControllerBase
{
    private readonly IExportService _exportService;
    private readonly IAuditService _auditService;
    private readonly ILogger<ExportController> _logger;

    public ExportController(
        IExportService exportService,
        IAuditService auditService,
        ILogger<ExportController> logger)
    {
        _exportService = exportService;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Exports a single report to PDF or JSON format.
    /// Only REVISOR role can access this endpoint.
    /// </summary>
    /// <param name="id">Report ID to export</param>
    /// <param name="format">Export format (PDF or JSON)</param>
    /// <returns>File download with the exported report</returns>
    [HttpGet("report/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportReport(int id, [FromQuery] ExportFormat format = ExportFormat.PDF)
    {
        try
        {
            var userId = GetUsuarioId();
            _logger.LogInformation("User {UserId} exporting report {ReportId} to {Format}", userId, id, format);

            var result = await _exportService.ExportSingleReportAsync(id, format, userId, isRevisor: true);

            // Log audit entry for export
            await _auditService.LogAsync(
                "REPORT_EXPORT",
                id,
                "ReporteIncidencia",
                $"Reporte exportado a {format}. Archivo: {result.FileName}",
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.RequestAborted
            );

            return File(result.Data, result.ContentType, result.FileName);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Report {ReportId} not found for export", id);
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized export attempt for report {ReportId}", id);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting report {ReportId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error al exportar el reporte" });
        }
    }

    /// <summary>
    /// Exports multiple reports in bulk (PDF or JSON).
    /// Only REVISOR role can access this endpoint.
    /// </summary>
    /// <param name="request">Export request with report IDs and options</param>
    /// <returns>File download with all exported reports</returns>
    [HttpPost("bulk")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportBulk([FromBody] ExportRequestDto request)
    {
        try
        {
            var userId = GetUsuarioId();
            _logger.LogInformation("User {UserId} exporting {Count} reports to {Format}",
                userId, request.ReportIds?.Length ?? 0, request.Format);

            var result = await _exportService.ExportReportsAsync(request, userId, isRevisor: true);

            // Log audit entry for bulk export
            await _auditService.LogAsync(
                "REPORT_EXPORT_BULK",
                null,
                "ReporteIncidencia",
                $"Exportación masiva de {result.ReportCount} reportes a {request.Format}. Archivo: {result.FileName}",
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.RequestAborted
            );

            return File(result.Data, result.ContentType, result.FileName);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid bulk export request");
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "No reports found for bulk export");
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized bulk export attempt");
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing bulk export");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error al exportar reportes" });
        }
    }

    /// <summary>
    /// Gets available export formats.
    /// </summary>
    [HttpGet("formats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<string[]> GetFormats()
    {
        return Ok(new[]
        {
            new { value = "PDF", label = "PDF - Documento portátil", description = "Formato para impresión y archivo" },
            new { value = "JSON", label = "JSON - Datos estructurados", description = "Formato para análisis e integración" }
        });
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
