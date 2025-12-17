using Ceiba.Core.Interfaces;
using Microsoft.Extensions.Hosting;

namespace Ceiba.Web.Middleware;

/// <summary>
/// Middleware that redirects all requests to the Setup Wizard if initial setup is required.
/// Similar to WordPress's first-run setup experience.
/// </summary>
public class SetupRedirectMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SetupRedirectMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    /// <summary>
    /// Paths that are allowed even when setup is required.
    /// </summary>
    private static readonly string[] AllowedPaths =
    {
        "/setup",           // Setup wizard pages
        "/_blazor",         // Blazor SignalR hub
        "/_framework",      // Blazor framework files
        "/health",          // Health checks
        "/alive",           // Liveness probe
        "/css",             // Static CSS files
        "/js",              // Static JS files
        "/lib",             // Static library files
        "/favicon",         // Favicon
        "/_content"         // Blazor content
    };

    /// <summary>
    /// File extensions for static files.
    /// </summary>
    private static readonly string[] StaticFileExtensions =
    {
        ".css", ".js", ".ico", ".png", ".jpg", ".jpeg", ".gif", ".svg",
        ".woff", ".woff2", ".ttf", ".eot", ".map"
    };

    public SetupRedirectMiddleware(RequestDelegate next, ILogger<SetupRedirectMiddleware> logger, IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context, ISetupService setupService)
    {
        // Skip setup redirect in Testing environment (tests seed their own data)
        if (_environment.IsEnvironment("Testing"))
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? "/";

        // Skip check for allowed paths and static files
        if (IsAllowedPath(path))
        {
            await _next(context);
            return;
        }

        // Check if setup is required
        var setupRequired = await setupService.IsSetupRequiredAsync();

        if (setupRequired)
        {
            _logger.LogInformation("Setup required, redirecting {Path} to /setup", path);
            context.Response.Redirect("/setup");
            return;
        }

        await _next(context);
    }

    private static bool IsAllowedPath(string path)
    {
        // Check exact prefix matches
        if (AllowedPaths.Any(allowedPath =>
            path.StartsWith(allowedPath, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        // Check static file extensions
        return StaticFileExtensions.Any(ext =>
            path.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Extension methods for SetupRedirectMiddleware.
/// </summary>
public static class SetupRedirectMiddlewareExtensions
{
    /// <summary>
    /// Adds the Setup Redirect middleware to the pipeline.
    /// Should be registered after routing but before authentication.
    /// </summary>
    public static IApplicationBuilder UseSetupRedirect(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SetupRedirectMiddleware>();
    }
}
