using Ceiba.Core.Entities;
using Ceiba.Core.Interfaces;
using Ceiba.Infrastructure.Data;
using Ceiba.Shared.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ceiba.Infrastructure.Services;

public class EmailConfigService : IEmailConfigService
{
    private readonly CeibaDbContext _context;
    private readonly ILogger<EmailConfigService> _logger;
    private readonly IEmailService _emailService;
    private readonly Guid _currentUserId;

    public EmailConfigService(
        CeibaDbContext context,
        ILogger<EmailConfigService> logger,
        IEmailService emailService,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _logger = logger;
        _emailService = emailService;

        // Get current user ID from claims
        var userIdClaim = httpContextAccessor.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        _currentUserId = userIdClaim != null ? Guid.Parse(userIdClaim.Value) : Guid.Empty;
    }

    public async Task<EmailConfigDto?> GetConfigurationAsync(CancellationToken cancellationToken = default)
    {
        var config = await _context.ConfiguracionesEmail
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (config == null)
            return null;

        return MapToDto(config);
    }

    public async Task<EmailConfigDto> UpdateConfigurationAsync(
        EmailConfigUpdateDto dto,
        CancellationToken cancellationToken = default)
    {
        if (_currentUserId == Guid.Empty)
        {
            _logger.LogError("Cannot update email configuration: User is not authenticated");
            throw new UnauthorizedAccessException("User is not authenticated");
        }

        var config = await _context.ConfiguracionesEmail
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (config == null)
        {
            // Create new configuration
            config = new ConfiguracionEmail
            {
                UsuarioId = _currentUserId,
                CreatedAt = DateTime.UtcNow
            };
            _context.ConfiguracionesEmail.Add(config);
        }

        // Update values
        config.Proveedor = dto.Proveedor;
        config.Habilitado = dto.Habilitado;
        config.FromEmail = dto.FromEmail;
        config.FromName = dto.FromName;

        // Update provider-specific configuration
        if (dto.Proveedor == "SMTP")
        {
            config.SmtpHost = dto.SmtpHost;
            config.SmtpPort = dto.SmtpPort;
            config.SmtpUsername = dto.SmtpUsername;
            config.SmtpUseSsl = dto.SmtpUseSsl;

            // Only update password if provided
            if (!string.IsNullOrEmpty(dto.SmtpPassword))
            {
                config.SmtpPassword = dto.SmtpPassword;
            }

            // Clear other providers when switching to SMTP
            config.SendGridApiKey = null;
            config.MailgunApiKey = null;
            config.MailgunDomain = null;
            config.MailgunRegion = null;
        }
        else if (dto.Proveedor == "SendGrid")
        {
            // Only update API key if provided
            if (!string.IsNullOrEmpty(dto.SendGridApiKey))
            {
                config.SendGridApiKey = dto.SendGridApiKey;
            }

            // Clear other providers when switching to SendGrid
            config.SmtpHost = null;
            config.SmtpPort = null;
            config.SmtpUsername = null;
            config.SmtpPassword = null;
            config.MailgunApiKey = null;
            config.MailgunDomain = null;
            config.MailgunRegion = null;
        }
        else if (dto.Proveedor == "Mailgun")
        {
            // Only update API key if provided
            if (!string.IsNullOrEmpty(dto.MailgunApiKey))
            {
                config.MailgunApiKey = dto.MailgunApiKey;
            }

            config.MailgunDomain = dto.MailgunDomain;
            config.MailgunRegion = dto.MailgunRegion;

            // Clear other providers when switching to Mailgun
            config.SmtpHost = null;
            config.SmtpPort = null;
            config.SmtpUsername = null;
            config.SmtpPassword = null;
            config.SendGridApiKey = null;
        }

        config.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Email configuration updated. Provider: {Provider}, Enabled: {Enabled}",
            config.Proveedor,
            config.Habilitado);

        return MapToDto(config);
    }

    public async Task<EmailConfigDto> EnsureConfigurationExistsAsync(CancellationToken cancellationToken = default)
    {
        var config = await GetConfigurationAsync(cancellationToken);

        if (config != null)
            return config;

        // Create default configuration with system user or current user
        var userId = _currentUserId != Guid.Empty ? _currentUserId : await GetAdminUserIdAsync(cancellationToken);

        var defaultConfig = new ConfiguracionEmail
        {
            Proveedor = "SMTP",
            Habilitado = false,
            SmtpHost = "localhost",
            SmtpPort = 587,
            SmtpUseSsl = true,
            FromEmail = "noreply@ceiba.local",
            FromName = "Ceiba - Reportes de Incidencias",
            UsuarioId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.ConfiguracionesEmail.Add(defaultConfig);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Default email configuration created");

        return MapToDto(defaultConfig);
    }

    public async Task<EmailConfigTestResultDto> TestConfigurationAsync(
        string testRecipient,
        CancellationToken cancellationToken = default)
    {
        var config = await _context.ConfiguracionesEmail
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (config == null)
        {
            return new EmailConfigTestResultDto
            {
                Success = false,
                Error = "No hay configuración de email",
                TestedAt = DateTime.UtcNow
            };
        }

        if (!config.Habilitado)
        {
            return new EmailConfigTestResultDto
            {
                Success = false,
                Error = "El servicio de email está deshabilitado",
                TestedAt = DateTime.UtcNow
            };
        }

        try
        {
            // Temporarily override email service configuration for testing
            var testRequest = new SendEmailRequestDto
            {
                Recipients = new List<string> { testRecipient },
                Subject = "Prueba de Configuración de Email - Ceiba",
                BodyHtml = @"
                    <h2>Prueba de Configuración Exitosa</h2>
                    <p>Este es un email de prueba del sistema Ceiba - Reportes de Incidencias.</p>
                    <p>Si recibes este mensaje, significa que la configuración de email está funcionando correctamente.</p>
                    <hr>
                    <p><small>Fecha de prueba: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + @"</small></p>
                    <p><small>Proveedor: " + config.Proveedor + @"</small></p>
                "
            };

            var result = await _emailService.SendAsync(testRequest, cancellationToken);

            // Update test results in database
            config.LastTestedAt = DateTime.UtcNow;
            config.LastTestSuccess = result.Success;
            config.LastTestError = result.Success ? null : result.Error;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Email configuration test completed. Success: {Success}, Recipient: {Recipient}",
                result.Success,
                testRecipient);

            return new EmailConfigTestResultDto
            {
                Success = result.Success,
                Error = result.Error,
                TestedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing email configuration");

            // Update test results in database
            config.LastTestedAt = DateTime.UtcNow;
            config.LastTestSuccess = false;
            config.LastTestError = ex.Message;
            await _context.SaveChangesAsync(cancellationToken);

            return new EmailConfigTestResultDto
            {
                Success = false,
                Error = ex.Message,
                TestedAt = DateTime.UtcNow
            };
        }
    }

    private async Task<Guid> GetAdminUserIdAsync(CancellationToken cancellationToken)
    {
        // Try to get first ADMIN user
        var adminRole = await _context.Roles
            .Where(r => r.Name == "ADMIN")
            .FirstOrDefaultAsync(cancellationToken);

        if (adminRole != null)
        {
            var adminUser = await _context.UserRoles
                .Where(ur => ur.RoleId == adminRole.Id)
                .Select(ur => ur.UserId)
                .FirstOrDefaultAsync(cancellationToken);

            if (adminUser != Guid.Empty)
                return adminUser;
        }

        // Fallback: get any user
        var anyUser = await _context.Users.Select(u => u.Id).FirstOrDefaultAsync(cancellationToken);
        return anyUser != Guid.Empty ? anyUser : Guid.Empty;
    }

    private static EmailConfigDto MapToDto(ConfiguracionEmail entity)
    {
        return new EmailConfigDto
        {
            Id = entity.Id,
            Proveedor = entity.Proveedor,
            Habilitado = entity.Habilitado,
            SmtpHost = entity.SmtpHost,
            SmtpPort = entity.SmtpPort,
            SmtpUsername = entity.SmtpUsername,
            SmtpUseSsl = entity.SmtpUseSsl,
            HasSendGridApiKey = !string.IsNullOrEmpty(entity.SendGridApiKey),
            HasMailgunApiKey = !string.IsNullOrEmpty(entity.MailgunApiKey),
            MailgunDomain = entity.MailgunDomain,
            MailgunRegion = entity.MailgunRegion,
            FromEmail = entity.FromEmail,
            FromName = entity.FromName,
            LastTestedAt = entity.LastTestedAt,
            LastTestSuccess = entity.LastTestSuccess,
            LastTestError = entity.LastTestError,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}
