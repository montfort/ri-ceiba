using Ceiba.Application.Services;
using Ceiba.Core.Interfaces;
using Ceiba.Infrastructure.Data;
using Ceiba.Infrastructure.Identity;
using Ceiba.Infrastructure.Logging;
using Ceiba.Infrastructure.Repositories;
using Ceiba.Infrastructure.Services;
using Ceiba.Web.Components;
using Ceiba.Web.Configuration;
using Ceiba.Web.Middleware;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

// Configurar Serilog (T018, T018a-d)
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build())
    .Enrich.With<PIIRedactionEnricher>() // T018b: PII redaction
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/ceiba-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30, // T018d: 30 días retención
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
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
    builder.Services.AddControllers();

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
        context.Response.Headers.Append("Content-Security-Policy",
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data:; " +
            "font-src 'self'; " +
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

    // Sesión (antes de auth)
    app.UseSession();

    // Autenticación y autorización
    app.UseAuthentication();
    app.UseAuthorization();

    // Middleware custom (T010c, T020b)
    app.UseMiddleware<UserAgentValidationMiddleware>(); // T010c RS-005
    app.UseMiddleware<AuthorizationLoggingMiddleware>(); // T020b RS-001

    // Anti-CSRF (T010d RS-005)
    app.UseAntiforgery();

    app.MapStaticAssets();
    app.MapControllers(); // Para APIs REST
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    // Migración automática y seed data en desarrollo (T019, T020)
    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CeibaDbContext>();
        await db.Database.MigrateAsync();
        Log.Information("Database migrations applied successfully");

        // Seed initial data (T020)
        var seedService = scope.ServiceProvider.GetRequiredService<SeedDataService>();
        await seedService.SeedAsync();
        Log.Information("Database seeded successfully");
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
