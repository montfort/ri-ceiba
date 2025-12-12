using Ceiba.Core.Interfaces;
using Ceiba.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ceiba.Web.Controllers;

[ApiController]
[Route("api/email-config")]
[Authorize(Roles = "ADMIN")]
[AutoValidateAntiforgeryToken]
public class EmailConfigController : ControllerBase
{
    private readonly IEmailConfigService _configService;
    private readonly ILogger<EmailConfigController> _logger;

    public EmailConfigController(
        IEmailConfigService configService,
        ILogger<EmailConfigController> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    /// <summary>
    /// Get current email configuration
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<EmailConfigDto>> GetConfiguration(CancellationToken cancellationToken)
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
            _logger.LogError(ex, "Error retrieving email configuration");
            return StatusCode(500, new { error = "Error al obtener la configuración de email" });
        }
    }

    /// <summary>
    /// Update email configuration
    /// </summary>
    [HttpPut]
    public async Task<ActionResult<EmailConfigDto>> UpdateConfiguration(
        [FromBody] EmailConfigUpdateDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Updating email configuration. Provider: {Provider}, Enabled: {Enabled}",
                dto.Proveedor, dto.Habilitado);

            var validationError = await ValidateEmailConfigAsync(dto, cancellationToken);
            if (validationError != null)
            {
                return BadRequest(new { error = validationError });
            }

            var config = await _configService.UpdateConfigurationAsync(dto, cancellationToken);

            _logger.LogInformation("Email configuration updated successfully");
            return Ok(config);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Unauthorized access when updating email configuration");
            return StatusCode(401, new { error = "No autorizado. Debe estar autenticado como ADMIN." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating email configuration");
            return StatusCode(500, new { error = $"Error al actualizar la configuración: {ex.Message}" });
        }
    }

    /// <summary>
    /// Validates email configuration input based on provider.
    /// </summary>
    /// <returns>Error message if validation fails, null if valid.</returns>
    private async Task<string?> ValidateEmailConfigAsync(EmailConfigUpdateDto dto, CancellationToken cancellationToken)
    {
        if (!dto.Habilitado)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(dto.FromEmail) || string.IsNullOrWhiteSpace(dto.FromName))
        {
            return "El email y nombre del remitente son obligatorios";
        }

        return dto.Proveedor switch
        {
            "SMTP" => ValidateSmtpConfig(dto),
            "SendGrid" => await ValidateSendGridConfigAsync(dto, cancellationToken),
            _ => null
        };
    }

    private static string? ValidateSmtpConfig(EmailConfigUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.SmtpHost) || !dto.SmtpPort.HasValue)
        {
            return "El host y puerto SMTP son obligatorios";
        }
        return null;
    }

    private async Task<string?> ValidateSendGridConfigAsync(EmailConfigUpdateDto dto, CancellationToken cancellationToken)
    {
        var existingConfig = await _configService.GetConfigurationAsync(cancellationToken);
        if (existingConfig == null && string.IsNullOrWhiteSpace(dto.SendGridApiKey))
        {
            return "La API Key de SendGrid es obligatoria";
        }
        return null;
    }

    /// <summary>
    /// Test email configuration by sending a test email
    /// </summary>
    [HttpPost("test")]
    public async Task<ActionResult<EmailConfigTestResultDto>> TestConfiguration(
        [FromBody] TestEmailConfigDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.TestRecipient))
            {
                return BadRequest(new { error = "El email de prueba es obligatorio" });
            }

            _logger.LogInformation("Testing email configuration with recipient: {Recipient}", dto.TestRecipient);

            var result = await _configService.TestConfigurationAsync(dto.TestRecipient, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing email configuration");
            return StatusCode(500, new { error = $"Error al probar la configuración: {ex.Message}" });
        }
    }
}
