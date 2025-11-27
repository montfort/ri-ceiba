using Ceiba.Core.Enums;
using Ceiba.Core.Interfaces;
using System;
using System.Security.Claims;

namespace Ceiba.Web.Middleware;

/// <summary>
/// RS-001 Mitigation: Authorization logging middleware.
/// Logs all unauthorized access attempts for security monitoring.
/// </summary>
public class AuthorizationLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthorizationLoggingMiddleware> _logger;

    public AuthorizationLoggingMiddleware(RequestDelegate next, ILogger<AuthorizationLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invoca el siguiente middleware y registra intentos no autorizados (403).
    /// Protege contra parsing inválido del claim de usuario y captura errores del servicio de auditoría.
    /// </summary>
    public async Task InvokeAsync(HttpContext context, IAuditService auditService)
    {
        await _next(context);

        // Log unauthorized attempts (403 Forbidden)
        if (context.Response.StatusCode == StatusCodes.Status403Forbidden)
        {
            Guid? userId = null;

            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var idClaimValue = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!string.IsNullOrEmpty(idClaimValue))
                {
                    if (Guid.TryParse(idClaimValue, out var parsedGuid))
                    {
                        userId = parsedGuid;
                    }
                    else
                    {
                        // Registramos el valor inválido pero no interrumpimos el flujo
                        _logger.LogWarning(
                            "Invalid NameIdentifier claim format. ClaimValue: {ClaimValue}, Path: {Path}",
                            idClaimValue,
                            context.Request.Path
                        );
                    }
                }
            }

            _logger.LogWarning(
                "Unauthorized access attempt. User: {UserId}, Path: {Path}, IP: {IP}",
                userId,
                context.Request.Path,
                context.Connection.RemoteIpAddress
            );

            try
            {
                await auditService.LogAsync(
                    AuditActionCode.SECURITY_UNAUTHORIZED_ACCESS,
                    detalles: System.Text.Json.JsonSerializer.Serialize(new
                    {
                        Path = context.Request.Path.ToString(),
                        Method = context.Request.Method,
                        User = context.User?.Identity?.Name
                    }),
                    ip: context.Connection.RemoteIpAddress?.ToString()
                );
            }
            catch (Exception ex)
            {
                // Evitar que fallos en el servicio de auditoría interrumpan la respuesta al cliente.
                _logger.LogError(
                    ex,
                    "Failed to log unauthorized access to audit service. Path: {Path}, User: {User}",
                    context.Request.Path,
                    context.User?.Identity?.Name
                );
            }
        }
    }
}
