using Ceiba.Core.Entities;
using Ceiba.Core.Interfaces;
using Ceiba.Infrastructure.Data;
using Ceiba.Shared.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ceiba.Infrastructure.Services;

public class AutomatedReportConfigService : IAutomatedReportConfigService
{
    private readonly CeibaDbContext _context;
    private readonly ILogger<AutomatedReportConfigService> _logger;
    private readonly Guid _currentUserId;

    public AutomatedReportConfigService(
        CeibaDbContext context,
        ILogger<AutomatedReportConfigService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _logger = logger;

        // Get current user ID from claims
        var userIdClaim = httpContextAccessor.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        _currentUserId = userIdClaim != null ? Guid.Parse(userIdClaim.Value) : Guid.Empty;
    }

    public async Task<AutomatedReportConfigDto?> GetConfigurationAsync(CancellationToken cancellationToken = default)
    {
        var config = await _context.ConfiguracionReportesAutomatizados
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (config == null)
            return null;

        return MapToDto(config);
    }

    public async Task<AutomatedReportConfigDto> UpdateConfigurationAsync(
        AutomatedReportConfigUpdateDto dto,
        CancellationToken cancellationToken = default)
    {
        if (_currentUserId == Guid.Empty)
        {
            _logger.LogError("Cannot update configuration: User is not authenticated");
            throw new UnauthorizedAccessException("User is not authenticated");
        }

        var config = await _context.ConfiguracionReportesAutomatizados
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (config == null)
        {
            // Create new configuration
            config = new ConfiguracionReportesAutomatizados
            {
                UsuarioId = _currentUserId,
                CreatedAt = DateTime.UtcNow
            };
            _context.ConfiguracionReportesAutomatizados.Add(config);
        }

        // Update values
        config.Habilitado = dto.Habilitado;
        config.HoraGeneracion = dto.HoraGeneracion;
        config.SetDestinatariosArray(dto.Destinatarios);
        config.RutaSalida = dto.RutaSalida;
        config.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Automated report configuration updated. Enabled: {Enabled}, Time: {Time}, Recipients: {Count}",
            config.Habilitado,
            config.HoraGeneracion,
            dto.Destinatarios.Length);

        return MapToDto(config);
    }

    public async Task<AutomatedReportConfigDto> EnsureConfigurationExistsAsync(CancellationToken cancellationToken = default)
    {
        var config = await GetConfigurationAsync(cancellationToken);

        if (config != null)
            return config;

        // Create default configuration with system user or current user
        var userId = _currentUserId != Guid.Empty ? _currentUserId : await GetAdminUserIdAsync(cancellationToken);

        var defaultConfig = new ConfiguracionReportesAutomatizados
        {
            Habilitado = false,
            HoraGeneracion = new TimeSpan(6, 0, 0),
            Destinatarios = string.Empty,
            RutaSalida = "./generated-reports",
            UsuarioId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.ConfiguracionReportesAutomatizados.Add(defaultConfig);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Default automated report configuration created");

        return MapToDto(defaultConfig);
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

    private static AutomatedReportConfigDto MapToDto(ConfiguracionReportesAutomatizados entity)
    {
        return new AutomatedReportConfigDto
        {
            Id = entity.Id,
            Habilitado = entity.Habilitado,
            HoraGeneracion = entity.HoraGeneracion,
            Destinatarios = entity.GetDestinatariosArray(),
            RutaSalida = entity.RutaSalida,
            UpdatedAt = entity.UpdatedAt,
            CreatedAt = entity.CreatedAt
        };
    }
}
