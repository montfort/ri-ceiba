using Ceiba.Core.Interfaces;
using Ceiba.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ceiba.Web.Controllers;

[ApiController]
[Route("api/automated-report-config")]
[Authorize(Roles = "ADMIN")]
public class AutomatedReportConfigController : ControllerBase
{
    private readonly IAutomatedReportConfigService _configService;
    private readonly ILogger<AutomatedReportConfigController> _logger;

    public AutomatedReportConfigController(
        IAutomatedReportConfigService configService,
        ILogger<AutomatedReportConfigController> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    /// <summary>
    /// Get current automated report configuration
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<AutomatedReportConfigDto>> GetConfiguration(CancellationToken cancellationToken)
    {
        try
        {
            var config = await _configService.GetConfigurationAsync(cancellationToken);

            if (config == null)
            {
                // Create default configuration if none exists
                config = await _configService.EnsureConfigurationExistsAsync(cancellationToken);
            }

            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving automated report configuration");
            return StatusCode(500, new { error = "Error al obtener la configuración" });
        }
    }

    /// <summary>
    /// Update automated report configuration
    /// </summary>
    [HttpPut]
    public async Task<ActionResult<AutomatedReportConfigDto>> UpdateConfiguration(
        [FromBody] AutomatedReportConfigUpdateDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Updating automated report configuration. Enabled: {Enabled}, Time: {Time}, Recipients: {Count}",
                dto.Habilitado, dto.HoraGeneracion, dto.Destinatarios.Length);

            // Validate input
            if (dto.HoraGeneracion < TimeSpan.Zero || dto.HoraGeneracion >= TimeSpan.FromDays(1))
            {
                _logger.LogWarning("Invalid time range: {Time}", dto.HoraGeneracion);
                return BadRequest(new { error = "La hora de generación debe estar entre 00:00:00 y 23:59:59" });
            }

            if (dto.Habilitado && dto.Destinatarios.Length == 0)
            {
                _logger.LogWarning("No recipients specified when enabled");
                return BadRequest(new { error = "Debe especificar al menos un destinatario cuando la generación automática está habilitada" });
            }

            var config = await _configService.UpdateConfigurationAsync(dto, cancellationToken);

            _logger.LogInformation("Configuration updated successfully");
            return Ok(config);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Unauthorized access when updating configuration");
            return StatusCode(401, new { error = "No autorizado. Debe estar autenticado como ADMIN." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating automated report configuration");
            return StatusCode(500, new { error = $"Error al actualizar la configuración: {ex.Message}" });
        }
    }
}
