using System.Net;
using System.Net.Sockets;
using Ceiba.Application.Services;
using Ceiba.Application.Services.Export;
using Ceiba.Core.Interfaces;
using Ceiba.Infrastructure.Caching;
using Ceiba.Infrastructure.Data;
using Ceiba.Infrastructure.Identity;
using Ceiba.Infrastructure.Logging;
using Ceiba.Infrastructure.Repositories;
using Ceiba.Infrastructure.Security;
using Ceiba.Infrastructure.Services;
using Ceiba.Web.Components;
using Ceiba.Web.Configuration;
using Ceiba.Web.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Ceiba.Integration.Tests.E2E;

/// <summary>
/// Shared fixture that starts a real HTTP server for Playwright E2E tests.
/// The server is started once and shared across all E2E tests in the collection.
/// Uses the actual application configuration with InMemory database.
/// </summary>
public class E2ETestServerFixture : IAsyncLifetime
{
    private WebApplication? _app;
    private readonly string _databaseName = $"E2ETests_{Guid.NewGuid()}";

    /// <summary>
    /// The base URL of the test server (e.g., "http://localhost:5001").
    /// </summary>
    public string BaseUrl { get; private set; } = string.Empty;

    /// <summary>
    /// The port the test server is listening on.
    /// </summary>
    public int Port { get; private set; }

    public async Task InitializeAsync()
    {
        // Find an available port
        Port = GetAvailablePort();
        BaseUrl = $"http://localhost:{Port}";

        // Find the Ceiba.Web project directory for content root
        var webProjectPath = FindWebProjectPath();

        // Create the web application builder with correct content root
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing",
            ContentRootPath = webProjectPath
        });

        // Configure Kestrel to listen on our port
        builder.WebHost.UseUrls(BaseUrl);
        builder.WebHost.UseContentRoot(webProjectPath);
        builder.WebHost.UseWebRoot(Path.Combine(webProjectPath, "wwwroot"));

        // Configure all services matching Program.cs but with InMemory database
        ConfigureServices(builder);

        // Build the application
        _app = builder.Build();

        // Configure the request pipeline
        ConfigurePipeline(_app);

        // Start the server
        await _app.StartAsync();

        // Seed the database
        await SeedDatabaseAsync();
    }

    private void ConfigureServices(WebApplicationBuilder builder)
    {
        var services = builder.Services;

        // Register IHttpContextAccessor
        services.AddHttpContextAccessor();

        // Register AuditSaveChangesInterceptor as singleton
        services.AddSingleton<AuditSaveChangesInterceptor>();

        // Add DbContext with InMemory provider for tests
        services.AddDbContext<CeibaDbContext>((sp, options) =>
        {
            options.UseInMemoryDatabase(_databaseName);
            options.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
        });

        // Configure ASP.NET Identity
        services.AddIdentity<IdentityUser<Guid>, IdentityRole<Guid>>()
            .AddEntityFrameworkStores<CeibaDbContext>()
            .AddDefaultTokenProviders();

        services.ConfigureIdentity();

        // Configure session
        services.AddDistributedMemoryCache();
        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(30);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });

        // Feature flags
        services.Configure<FeatureFlags>(builder.Configuration.GetSection("FeatureFlags"));

        // Register application services
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IRegionDataLoader, RegionDataLoader>();
        services.AddScoped<ISeedDataService, SeedDataService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IExportService, ExportService>();
        services.AddScoped<IPdfGenerator, PdfGenerator>();
        services.AddScoped<IJsonExporter, JsonExporter>();
        services.AddScoped<IUserManagementService, Ceiba.Infrastructure.Services.UserManagementService>();
        services.AddScoped<ICatalogAdminService, Ceiba.Infrastructure.Services.CatalogAdminService>();

        // Security services
        services.AddMemoryCache();
        services.AddSingleton<ILoginSecurityService, LoginSecurityService>();
        services.AddSingleton<ICacheService, MemoryCacheService>();
        services.AddScoped<CachedCatalogService>();
        services.AddSingleton<IInputSanitizer, InputSanitizer>();

        // Automated reports services (with test-friendly stubs)
        services.AddHttpClient();
        services.AddScoped<IAiConfigurationService, AiConfigurationService>();
        services.AddHttpClient<IAiNarrativeService, AiNarrativeService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IEmailConfigService, EmailConfigService>();
        services.AddScoped<IAutomatedReportService, AutomatedReportService>();
        services.AddScoped<IAutomatedReportConfigService, AutomatedReportConfigService>();
        // NOTE: Background services are NOT registered for tests

        // Resilience services
        services.AddSingleton<EmailResilienceOptions>();
        services.AddScoped<IResilientEmailService, ResilientEmailService>();

        // Health check services
        services.AddScoped<IServiceHealthCheck, DatabaseHealthCheck>();
        services.AddScoped<IServiceHealthCheck, EmailHealthCheck>();
        services.AddScoped<IServiceHealthCheck, AiServiceHealthCheck>();
        services.AddScoped<AggregatedHealthCheckService>();
        services.AddSingleton<GracefulDegradationService>();

        // HttpClient for Blazor components
        services.AddScoped(sp => new HttpClient
        {
            BaseAddress = new Uri(sp.GetRequiredService<NavigationManager>().BaseUri)
        });

        // Authorization policies
        services.AddAuthorizationBuilder()
            .AddPolicy("RequireCreadorRole", policy => policy.RequireRole("CREADOR"))
            .AddPolicy("RequireRevisorRole", policy => policy.RequireRole("REVISOR"))
            .AddPolicy("RequireAdminRole", policy => policy.RequireRole("ADMIN"));

        // Razor Components with Blazor Server
        services.AddRazorComponents()
            .AddInteractiveServerComponents();

        // CORS
        services.AddCors(options =>
        {
            options.AddPolicy("Default", policy =>
            {
                policy.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });

        // Controllers and antiforgery
        services.AddControllersWithViews();
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
        });
    }

    private static void ConfigurePipeline(WebApplication app)
    {
        // Error handling middleware
        app.UseMiddleware<ErrorHandlingMiddleware>();

        // Security headers (simplified for tests)
        app.UseSecurityHeaders(options =>
        {
            options.FrameOptions = "DENY";
            options.ReferrerPolicy = "strict-origin-when-cross-origin";
            options.PermissionsPolicy = "accelerometer=(), camera=(), geolocation=()";
            options.ContentSecurityPolicy =
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
                "img-src 'self' data:; " +
                "font-src 'self' https://cdn.jsdelivr.net; " +
                "connect-src 'self' ws: wss:; " +
                "frame-ancestors 'none'";
        });

        // Static files
        app.UseStaticFiles();

        // Session
        app.UseSession();

        // Routing
        app.UseRouting();

        // Auth
        app.UseAuthentication();
        app.UseAuthorization();

        // Antiforgery
        app.UseAntiforgery();

        // Custom middleware
        app.UseMiddleware<UserAgentValidationMiddleware>();
        app.UseMiddleware<AuthorizationLoggingMiddleware>();

        // Map endpoints - use UseStaticFiles instead of MapStaticAssets for tests
        // MapStaticAssets requires a manifest file that doesn't exist in test context
        app.MapControllers();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();
    }

    private async Task SeedDatabaseAsync()
    {
        if (_app == null) return;

        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CeibaDbContext>();
        await db.Database.EnsureCreatedAsync();

        // Seed basic test data
        var seedService = scope.ServiceProvider.GetRequiredService<ISeedDataService>();
        await seedService.SeedAsync();
    }

    public async Task DisposeAsync()
    {
        if (_app != null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    private static int GetAvailablePort()
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        return ((IPEndPoint)socket.LocalEndPoint!).Port;
    }

    private static string FindWebProjectPath()
    {
        // Navigate up from the test assembly to find the solution root
        var currentDir = AppContext.BaseDirectory;
        var directory = new DirectoryInfo(currentDir);

        while (directory != null)
        {
            var webProjectPath = Path.Combine(directory.FullName, "src", "Ceiba.Web");
            if (Directory.Exists(webProjectPath))
            {
                return webProjectPath;
            }

            // Check if we're in the solution root
            var solutionFile = directory.GetFiles("*.sln").FirstOrDefault();
            if (solutionFile != null)
            {
                webProjectPath = Path.Combine(directory.FullName, "src", "Ceiba.Web");
                if (Directory.Exists(webProjectPath))
                {
                    return webProjectPath;
                }
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            "Could not find Ceiba.Web project. Make sure the test is run from the solution directory.");
    }
}

/// <summary>
/// Collection definition for E2E tests.
/// All test classes decorated with [Collection("E2E")] will share the same server instance.
/// </summary>
[CollectionDefinition("E2E")]
public class E2ETestCollection : ICollectionFixture<E2ETestServerFixture>
{
}
