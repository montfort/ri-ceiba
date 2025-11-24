using Microsoft.AspNetCore.Authentication;

namespace Ceiba.Web.Middleware;

/// <summary>
/// RS-005 Mitigation: User-Agent validation middleware.
/// Detects and blocks suspicious session hijacking attempts by validating User-Agent consistency.
/// </summary>
public class UserAgentValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UserAgentValidationMiddleware> _logger;
    private const string UserAgentSessionKey = "Ceiba_UserAgent";

    public UserAgentValidationMiddleware(RequestDelegate next, ILogger<UserAgentValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Solo validar para usuarios autenticados
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var currentUserAgent = context.Request.Headers.UserAgent.ToString();
            var storedUserAgent = context.Session.GetString(UserAgentSessionKey);

            if (string.IsNullOrEmpty(storedUserAgent))
            {
                // Primera petición de la sesión - guardar User-Agent
                context.Session.SetString(UserAgentSessionKey, currentUserAgent);
            }
            else if (storedUserAgent != currentUserAgent)
            {
                // User-Agent cambió - posible session hijacking
                _logger.LogWarning(
                    "User-Agent mismatch detected. User: {UserId}, Original: {Original}, Current: {Current}",
                    context.User.Identity.Name,
                    storedUserAgent,
                    currentUserAgent
                );

                // Invalidar sesión y redirigir a login
                await context.SignOutAsync();
                context.Response.Redirect("/Auth/Login?reason=security");
                return;
            }
        }

        await _next(context);
    }
}
