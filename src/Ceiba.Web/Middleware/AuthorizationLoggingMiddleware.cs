using Ceiba.Core.Enums;
using Ceiba.Core.Interfaces;

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

    public async Task InvokeAsync(HttpContext context, IAuditService auditService)
    {
        await _next(context);

        // Log unauthorized attempts (403 Forbidden)
        if (context.Response.StatusCode == StatusCodes.Status403Forbidden)
        {
            var userId = context.User?.Identity?.IsAuthenticated == true
                ? Guid.Parse(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString())
                : (Guid?)null;

            _logger.LogWarning(
                "Unauthorized access attempt. User: {UserId}, Path: {Path}, IP: {IP}",
                userId,
                context.Request.Path,
                context.Connection.RemoteIpAddress
            );

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
    }
}
