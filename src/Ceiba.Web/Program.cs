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
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

// Configurar Serilog (T018, T018a-d)
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build())
    .Enrich.With<PIIRedactionEnricher>() // T018b: PII redaction
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Agregar Aspire ServiceDefaults (health checks, OpenTelemetry, service discovery)
    builder.AddServiceDefaults();

    // Usar Serilog como logger
    builder.Host.UseSerilog();

    // Register IHttpContextAccessor first (needed by AuditSaveChangesInterceptor)
    builder.Services.AddHttpContextAccessor();

    // Register AuditSaveChangesInterceptor as singleton (interceptors must be singletons for pooling)
    builder.Services.AddSingleton<AuditSaveChangesInterceptor>();

    // Configurar DbContext con PostgreSQL
    // Detecta si está corriendo con Aspire (connection string "ceiba") o standalone ("DefaultConnection")
    var aspireConnectionString = builder.Configuration.GetConnectionString("ceiba");
    if (!string.IsNullOrEmpty(aspireConnectionString))
    {
        // Aspire está orquestando - usar integración Aspire para PostgreSQL
        // ASP0000 is suppressed because Aspire's AddNpgsqlDbContext doesn't provide IServiceProvider
        // in configureDbContextOptions callback. BuildServiceProvider is necessary to register the
        // audit interceptor. This is a known limitation with Aspire integration.
#pragma warning disable ASP0000 // Do not call 'BuildServiceProvider' in 'ConfigureServices'
        builder.AddNpgsqlDbContext<CeibaDbContext>("ceiba", configureDbContextOptions: options =>
        {
            // Add audit interceptor via DI (required for DbContext pooling)
            var serviceProvider = builder.Services.BuildServiceProvider();
            var interceptor = serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>();
            options.AddInterceptors(interceptor);
        });
#pragma warning restore ASP0000
        Log.Information("Using Aspire-orchestrated PostgreSQL connection");
    }
    else
    {
        // Ejecución standalone - usar configuración tradicional (T009)
        builder.Services.AddDbContext<CeibaDbContext>((serviceProvider, options) =>
        {
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly("Ceiba.Infrastructure");
                npgsqlOptions.CommandTimeout(30);
            });
            options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());

            // Add audit interceptor
            var interceptor = serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>();
            options.AddInterceptors(interceptor);
        });
        Log.Information("Using standalone PostgreSQL connection");
    }

    // Configurar ASP.NET Identity (T010-T010e)
    builder.Services.AddIdentity<IdentityUser<Guid>, IdentityRole<Guid>>()
        .AddEntityFrameworkStores<CeibaDbContext>()
        .AddDefaultTokenProviders();

    builder.Services.ConfigureIdentity(); // T010: Políticas de contraseña y sesión

    // Configurar sesión (requerido para User-Agent validation)
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(30); // FR-005
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });

    // Configurar feature flags (T019d)
    builder.Services.Configure<FeatureFlags>(builder.Configuration.GetSection("FeatureFlags"));

    // Registrar servicios de aplicación (T016)
    builder.Services.AddScoped<IAuditService, AuditService>();
    builder.Services.AddScoped<IRegionDataLoader, RegionDataLoader>(); // Geographic catalog loader
    builder.Services.AddScoped<ISeedDataService, SeedDataService>(); // T020
    // Note: AddHttpContextAccessor already called above for AuditSaveChangesInterceptor

    // Registrar servicios de User Story 1 (T045)
    builder.Services.AddScoped<IReportService, ReportService>();
    builder.Services.AddScoped<ICatalogService, CatalogService>();
    builder.Services.AddScoped<IReportRepository, ReportRepository>();

    // Registrar servicios de User Story 2 - Export (US2)
    builder.Services.AddScoped<IExportService, ExportService>();
    builder.Services.AddScoped<IPdfGenerator, PdfGenerator>();
    builder.Services.AddScoped<IJsonExporter, JsonExporter>();

    // Registrar servicios de User Story 3 - Admin (US3)
    builder.Services.AddScoped<IUserManagementService, Ceiba.Infrastructure.Services.UserManagementService>();
    builder.Services.AddScoped<ICatalogAdminService, Ceiba.Infrastructure.Services.CatalogAdminService>();

    // Registrar servicio de seguridad de login (T113a-e)
    builder.Services.AddMemoryCache();
    builder.Services.AddSingleton<ILoginSecurityService, LoginSecurityService>();

    // T117b: Query caching services
    builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
    builder.Services.AddScoped<CachedCatalogService>();

    // T118b: Input sanitization
    builder.Services.AddSingleton<IInputSanitizer, InputSanitizer>();

    // Registrar servicios de User Story 4 - Automated Reports (US4)
    builder.Services.AddHttpClient(); // IHttpClientFactory para pruebas de conexión
    builder.Services.AddScoped<IAiConfigurationService, AiConfigurationService>();
    builder.Services.AddHttpClient<IAiNarrativeService, AiNarrativeService>();
    builder.Services.AddScoped<IEmailService, EmailService>();
    builder.Services.AddScoped<IEmailConfigService, EmailConfigService>();
    builder.Services.AddScoped<IAutomatedReportService, AutomatedReportService>();
    builder.Services.AddScoped<IAutomatedReportConfigService, AutomatedReportConfigService>();
    builder.Services.AddHostedService<AutomatedReportBackgroundService>();

    // T146-T155: RO-003 Email resilience (retry, circuit breaker, queue)
    builder.Services.AddSingleton<EmailResilienceOptions>();
    builder.Services.AddScoped<IResilientEmailService, ResilientEmailService>();
    builder.Services.AddHostedService<EmailQueueProcessorService>();

    // T156-T165: RO-004 Service resilience (health checks, graceful degradation)
    builder.Services.AddScoped<IServiceHealthCheck, DatabaseHealthCheck>();
    builder.Services.AddScoped<IServiceHealthCheck, EmailHealthCheck>();
    builder.Services.AddScoped<IServiceHealthCheck, AiServiceHealthCheck>();
    builder.Services.AddScoped<AggregatedHealthCheckService>();
    builder.Services.AddSingleton<GracefulDegradationService>();

    // Configurar HttpClient para componentes Blazor
    builder.Services.AddScoped(sp => new HttpClient
    {
        BaseAddress = new Uri(sp.GetRequiredService<NavigationManager>().BaseUri)
    });

    // Note: CeibaDbContext is registered above via AddNpgsqlDbContext (Aspire) or AddDbContext (standalone)
    // The DbContext already supports UserId injection through the interceptor pattern

    // Configurar autorización (T020a)
    builder.Services.AddAuthorizationBuilder()
        .AddPolicy("RequireCreadorRole", policy => policy.RequireRole("CREADOR"))
        .AddPolicy("RequireRevisorRole", policy => policy.RequireRole("REVISOR"))
        .AddPolicy("RequireAdminRole", policy => policy.RequireRole("ADMIN"));

    // Configurar Razor Components con Blazor Server
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    // Configurar CORS (si se necesita para APIs)
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Default", policy =>
        {
            policy.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [])
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
    });

    // Configurar controladores para APIs (se usarán en User Stories)
    // Registrar controladores y soporte para vistas/antiforgery, necesario para formularios MVC
    builder.Services.AddControllersWithViews();
    builder.Services.AddAntiforgery(options =>
    {
        options.HeaderName = "X-CSRF-TOKEN";
    });

    var app = builder.Build();

    // Configurar pipeline HTTP

    // Middleware de manejo de errores (T017)
    app.UseMiddleware<ErrorHandlingMiddleware>();

    // T114, T114a, T118: Comprehensive security headers middleware
    app.UseSecurityHeaders(options =>
    {
        options.FrameOptions = "DENY";
        options.ReferrerPolicy = "strict-origin-when-cross-origin";
        options.PermissionsPolicy = "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()";
        // CSP must allow Blazor's inline scripts and styles, plus Bootstrap Icons CDN
        options.ContentSecurityPolicy =
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
            "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
            "img-src 'self' data:; " +
            "font-src 'self' https://cdn.jsdelivr.net; " +
            "connect-src 'self' ws: wss:; " +  // Allow WebSocket for Blazor Server
            "frame-ancestors 'none'";
    });

    // HSTS in production (T114a)
    app.UseStrictTransportSecurity(app.Environment);

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        app.UseHsts();
    }

    app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
    app.UseHttpsRedirection();

    app.UseStaticFiles();

    // Sesión (antes de middlewares que la consumen)
    app.UseSession();

    /* Orden obligatorio de middlewares de enrutamiento y seguridad
       - app.UseRouting() debe estar antes de cualquier middleware que dependa del endpoint seleccionado
         (por ejemplo, UseAuthentication/UseAuthorization, UseAntiforgery, etc.).
       - app.UseAuthentication() y app.UseAuthorization() deben ejecutarse después de UseRouting() y
         antes de que los endpoints sean invocados.
       - app.UseAntiforgery() (middleware de validación CSRF) debe situarse entre UseRouting() y el mapeo
         de endpoints (MapControllers, MapRazorComponents, ...) y además después de la autenticación/authorization.
       - Los middlewares que consumen la sesión (por ejemplo UserAgentValidationMiddleware) deben registrarse
         después de app.UseSession().
       Mantener este orden evita InvalidOperationException cuando un endpoint contiene metadatos antiforgery.
    */

    // Enrutamiento obligatorio antes de autenticación/antiforgery y antes de MapEndpoints
    app.UseRouting();

    // Autenticación y autorización
    app.UseAuthentication();
    app.UseAuthorization();

    // Middleware antiforgery: debe ejecutarse entre UseRouting() y Map*() y después de auth
    // CSRF protection enabled for all routes including API endpoints
    // APIs use [AutoValidateAntiforgeryToken] which validates on POST/PUT/DELETE but not GET
    app.UseAntiforgery();

    // Middleware custom (T010c, T020b)
    app.UseMiddleware<UserAgentValidationMiddleware>(); // T010c RS-005
    app.UseMiddleware<AuthorizationLoggingMiddleware>(); // T020b RS-001

    // Anti-CSRF: antiforgery está habilitado mediante servicios y filas de middleware

    app.MapStaticAssets();
    app.MapControllers(); // Para APIs REST
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    // Mapear endpoints de Aspire (health checks: /health, /alive)
    app.MapDefaultEndpoints();

    // Migración automática y seed data en desarrollo (T019, T020)
    // Skip migrations in Testing environment (handled by WebApplicationFactory)
    if (app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CeibaDbContext>();

        try
        {
            // Verifica si puede conectarse a la base de datos
            var canConnect = await db.Database.CanConnectAsync();

            if (canConnect)
            {
                // Si la BD existe, aplica migraciones pendientes
                await db.Database.MigrateAsync();
                Log.Information("Database migrations applied successfully");

                // Seed initial data (T020)
                var seedService = scope.ServiceProvider.GetRequiredService<ISeedDataService>();
                await seedService.SeedAsync();
                Log.Information("Database seeded successfully");
            }
            else
            {
                Log.Warning("Cannot connect to database. Please create the database manually:");
                Log.Warning("  psql -U postgres -c \"CREATE DATABASE ceiba;\"");
                Log.Warning("  psql -U postgres -c \"GRANT ALL PRIVILEGES ON DATABASE ceiba TO ceiba;\"");
            }
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "42501") // Permission denied
        {
            Log.Error("Database connection failed: User does not have permission to create database");
            Log.Error("Please run these commands as PostgreSQL superuser:");
            Log.Error("  CREATE DATABASE ceiba;");
            Log.Error("  GRANT ALL PRIVILEGES ON DATABASE ceiba TO ceiba;");
            Log.Error("Then restart the application.");
            throw;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Database migration failed");
            throw;
        }
    }

    Log.Information("Ceiba application starting...");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

// Make Program class accessible to integration tests
public partial class Program { }
