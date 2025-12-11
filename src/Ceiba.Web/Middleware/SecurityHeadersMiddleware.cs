namespace Ceiba.Web.Middleware;

/// <summary>
/// Middleware to add security headers to all responses.
/// T114, T114a, T118: HTTPS, HSTS, and security headers
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;
    private readonly SecurityHeadersOptions _options;

    public SecurityHeadersMiddleware(
        RequestDelegate next,
        ILogger<SecurityHeadersMiddleware> logger,
        SecurityHeadersOptions options)
    {
        _next = next;
        _logger = logger;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers before the response is sent
        context.Response.OnStarting(() =>
        {
            AddSecurityHeaders(context.Response.Headers);
            return Task.CompletedTask;
        });

        await _next(context);
    }

    private void AddSecurityHeaders(IHeaderDictionary headers)
    {
        // T118: X-Content-Type-Options - Prevents MIME type sniffing
        headers["X-Content-Type-Options"] = "nosniff";

        // T118: X-Frame-Options - Prevents clickjacking
        headers["X-Frame-Options"] = _options.FrameOptions;

        // T118: X-XSS-Protection - Legacy XSS protection (for older browsers)
        headers["X-XSS-Protection"] = "1; mode=block";

        // T118: Referrer-Policy - Controls referrer information
        headers["Referrer-Policy"] = _options.ReferrerPolicy;

        // T118: Permissions-Policy - Controls browser features
        headers["Permissions-Policy"] = _options.PermissionsPolicy;

        // T118: Content-Security-Policy - Controls allowed content sources
        if (!string.IsNullOrEmpty(_options.ContentSecurityPolicy))
        {
            headers["Content-Security-Policy"] = _options.ContentSecurityPolicy;
        }

        // T118: X-Permitted-Cross-Domain-Policies
        headers["X-Permitted-Cross-Domain-Policies"] = "none";

        // Remove headers that could leak information
        headers.Remove("X-Powered-By");
        headers.Remove("Server");
    }
}

/// <summary>
/// Options for security headers configuration.
/// </summary>
public class SecurityHeadersOptions
{
    /// <summary>
    /// X-Frame-Options header value. Default: DENY
    /// </summary>
    public string FrameOptions { get; set; } = "DENY";

    /// <summary>
    /// Referrer-Policy header value. Default: strict-origin-when-cross-origin
    /// </summary>
    public string ReferrerPolicy { get; set; } = "strict-origin-when-cross-origin";

    /// <summary>
    /// Permissions-Policy header value.
    /// </summary>
    public string PermissionsPolicy { get; set; } =
        "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()";

    /// <summary>
    /// Content-Security-Policy header value.
    /// Empty string means no CSP header (Blazor requires special handling).
    /// </summary>
    public string ContentSecurityPolicy { get; set; } = "";
}

/// <summary>
/// Extension methods for SecurityHeadersMiddleware.
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
{
    /// <summary>
    /// Add security headers middleware to the pipeline.
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(
        this IApplicationBuilder app,
        Action<SecurityHeadersOptions>? configureOptions = null)
    {
        var options = new SecurityHeadersOptions();
        configureOptions?.Invoke(options);

        return app.UseMiddleware<SecurityHeadersMiddleware>(options);
    }

    /// <summary>
    /// Add HSTS with recommended settings for production.
    /// T114a: HSTS configuration
    /// </summary>
    public static IApplicationBuilder UseStrictTransportSecurity(
        this IApplicationBuilder app,
        IWebHostEnvironment env)
    {
        if (!env.IsDevelopment())
        {
            app.UseHsts();
        }

        return app;
    }
}
