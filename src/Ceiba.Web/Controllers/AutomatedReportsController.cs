using Ceiba.Core.Interfaces;
using Ceiba.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ceiba.Web.Controllers;

[ApiController]
[Route("api/automated-reports")]
[Authorize(Roles = "REVISOR")]
[AutoValidateAntiforgeryToken]
public class AutomatedReportsController : ControllerBase
{
    private readonly IAutomatedReportService _reportService;
    private readonly ILogger<AutomatedReportsController> _logger;

    public AutomatedReportsController(
        IAutomatedReportService reportService,
        ILogger<AutomatedReportsController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    /// <summary>
    /// Download Word document for an automated report
    /// </summary>
    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadReport(int id, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Downloading report {Id}", id);

            var report = await _reportService.GetReportByIdAsync(id);

            if (report == null)
            {
                _logger.LogWarning("Report {Id} not found", id);
                return NotFound(new { error = "Reporte no encontrado" });
            }

            if (string.IsNullOrEmpty(report.ContenidoWordPath))
            {
                _logger.LogWarning("Report {Id} has no Word document", id);
                return NotFound(new { error = "El reporte no tiene un documento Word generado" });
            }

            if (!System.IO.File.Exists(report.ContenidoWordPath))
            {
                _logger.LogError("Word file not found at path: {Path}", report.ContenidoWordPath);
                return NotFound(new { error = "El archivo Word no se encuentra en el servidor" });
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(report.ContenidoWordPath, cancellationToken);
            var fileName = $"Reporte_{report.FechaInicio:yyyyMMdd}_{report.FechaFin:yyyyMMdd}.docx";

            _logger.LogInformation("Successfully downloaded report {Id}, file size: {Size} bytes", id, fileBytes.Length);

            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading report {Id}", id);
            return StatusCode(500, new { error = "Error al descargar el reporte" });
        }
    }

    /// <summary>
    /// Get paginated list of automated reports
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<AutomatedReportListDto>>> GetReports(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        [FromQuery] DateTime? fechaDesde = null,
        [FromQuery] DateTime? fechaHasta = null,
        [FromQuery] bool? enviado = null)
    {
        try
        {
            var reports = await _reportService.GetReportsAsync(skip, take, fechaDesde, fechaHasta, enviado);
            return Ok(reports);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reports list");
            return StatusCode(500, new { error = "Error al obtener la lista de reportes" });
        }
    }

    /// <summary>
    /// Get detailed information for a specific report
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<AutomatedReportDetailDto>> GetReport(int id)
    {
        try
        {
            var report = await _reportService.GetReportByIdAsync(id);

            if (report == null)
            {
                return NotFound(new { error = "Reporte no encontrado" });
            }

            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving report {Id}", id);
            return StatusCode(500, new { error = "Error al obtener el reporte" });
        }
    }

    /// <summary>
    /// Send report by email
    /// </summary>
    [HttpPost("{id}/send")]
    public async Task<IActionResult> SendReport(
        int id,
        [FromBody] List<string>? recipients = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending report {Id} by email", id);

            var success = await _reportService.SendReportByEmailAsync(id, recipients, cancellationToken);

            if (!success)
            {
                return BadRequest(new { error = "No se pudo enviar el reporte" });
            }

            _logger.LogInformation("Report {Id} sent successfully", id);
            return Ok(new { message = "Reporte enviado exitosamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending report {Id}", id);
            return StatusCode(500, new { error = "Error al enviar el reporte" });
        }
    }

    /// <summary>
    /// Regenerate Word document for a report
    /// </summary>
    [HttpPost("{id}/regenerate-word")]
    public async Task<IActionResult> RegenerateWord(int id)
    {
        try
        {
            _logger.LogInformation("Regenerating Word document for report {Id}", id);

            var wordPath = await _reportService.RegenerateWordDocumentAsync(id);

            if (string.IsNullOrEmpty(wordPath))
            {
                return BadRequest(new { error = "No se pudo regenerar el documento Word" });
            }

            _logger.LogInformation("Word document regenerated successfully for report {Id}", id);
            return Ok(new { message = "Documento Word regenerado exitosamente", path = wordPath });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regenerating Word document for report {Id}", id);
            return StatusCode(500, new { error = "Error al regenerar el documento Word" });
        }
    }

    /// <summary>
    /// Generate a new automated report manually
    /// </summary>
    [HttpPost("generate")]
    public async Task<ActionResult<AutomatedReportDetailDto>> GenerateReport(
        [FromBody] GenerateReportRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Generating manual report from {Start} to {End}",
                request.FechaInicio, request.FechaFin);

            // Get current user ID from claims
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            Guid? userId = userIdClaim != null ? Guid.Parse(userIdClaim.Value) : null;

            var report = await _reportService.GenerateReportAsync(request, userId, cancellationToken);

            _logger.LogInformation("Report generated successfully with ID {Id}", report.Id);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating manual report");
            return StatusCode(500, new { error = $"Error al generar el reporte: {ex.Message}" });
        }
    }
}
