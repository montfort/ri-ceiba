using Ceiba.Application.Services;
using Ceiba.Application.Services.Export;
using Ceiba.Core.Interfaces;
using Ceiba.Infrastructure.Data;
using Ceiba.Infrastructure.Identity;
using Ceiba.Infrastructure.Logging;
using Ceiba.Infrastructure.Repositories;
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

    // Usar Serilog como logger
    builder.Host.UseSerilog();

    // Configurar DbContext con PostgreSQL (T009)
    builder.Services.AddDbContext<CeibaDbContext>((serviceProvider, options) =>
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly("Ceiba.Infrastructure");
            npgsqlOptions.CommandTimeout(30);
        });
        options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
    });

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
    builder.Services.AddScoped<SeedDataService>(); // T020
    builder.Services.AddHttpContextAccessor(); // Para obtener UserId en DbContext

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

    // Registrar servicios de User Story 4 - Automated Reports (US4)
    builder.Services.AddHttpClient(); // IHttpClientFactory para pruebas de conexión
    builder.Services.AddScoped<IAiConfigurationService, AiConfigurationService>();
    builder.Services.AddHttpClient<IAiNarrativeService, AiNarrativeService>();
    builder.Services.AddScoped<IEmailService, EmailService>();
    builder.Services.AddScoped<IEmailConfigService, EmailConfigService>();
    builder.Services.AddScoped<IAutomatedReportService, AutomatedReportService>();
    builder.Services.AddScoped<IAutomatedReportConfigService, AutomatedReportConfigService>();
    builder.Services.AddHostedService<AutomatedReportBackgroundService>();

    // Configurar HttpClient para componentes Blazor
    builder.Services.AddScoped(sp => new HttpClient
    {
        BaseAddress = new Uri(sp.GetRequiredService<NavigationManager>().BaseUri)
    });

    // Configurar factory para DbContext con UserId del request actual
    builder.Services.AddScoped(provider =>
    {
        var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
        var userId = httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true
            ? Guid.Parse(httpContextAccessor.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString())
            : (Guid?)null;

        var optionsBuilder = new DbContextOptionsBuilder<CeibaDbContext>();
        var connectionString = provider.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection");
        optionsBuilder.UseNpgsql(connectionString);

        return new CeibaDbContext(optionsBuilder.Options, userId);
    });

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

    // Security headers (T114a RS-005, T118)
    app.Use(async (context, next) =>
    {
        // HSTS (T114a)
        context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");

        // Security headers (T118)
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

        // CSP (T020f RS-002)
        // Allow cdn.jsdelivr.net for Bootstrap Icons
        context.Response.Headers.Append("Content-Security-Policy",
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
            "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
            "img-src 'self' data:; " +
            "font-src 'self' https://cdn.jsdelivr.net; " +
            "connect-src 'self'; " +
            "frame-ancestors 'none'");

        await next();
    });

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
    // Excluir rutas /api/* para permitir APIs REST sin CSRF tokens
    app.UseWhen(
        context => !context.Request.Path.StartsWithSegments("/api"),
        appBuilder => appBuilder.UseAntiforgery()
    );

    // Middleware custom (T010c, T020b)
    app.UseMiddleware<UserAgentValidationMiddleware>(); // T010c RS-005
    app.UseMiddleware<AuthorizationLoggingMiddleware>(); // T020b RS-001

    // Anti-CSRF: antiforgery está habilitado mediante servicios y filas de middleware

    app.MapStaticAssets();
    app.MapControllers(); // Para APIs REST
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

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
                var seedService = scope.ServiceProvider.GetRequiredService<SeedDataService>();
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
    app.Run();
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
