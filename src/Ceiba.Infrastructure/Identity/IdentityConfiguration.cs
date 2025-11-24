using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Ceiba.Infrastructure.Identity;

/// <summary>
/// Configuración de ASP.NET Identity según requisitos de seguridad.
/// FR-001: Contraseña mínimo 10 caracteres, mayúscula + número
/// FR-005: Timeout de sesión 30 minutos
/// RS-005: Cookies seguras, regeneración de ID de sesión
/// </summary>
public static class IdentityConfiguration
{
    public static IServiceCollection ConfigureIdentity(this IServiceCollection services)
    {
        // Configurar opciones de contraseña (FR-001)
        services.Configure<IdentityOptions>(options =>
        {
            // Password requirements
            options.Password.RequiredLength = 10;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = false; // No requerido explícitamente
            options.Password.RequireNonAlphanumeric = false; // No requerido explícitamente
            options.Password.RequiredUniqueChars = 3;

            // Lockout settings (RS-004 mitigation)
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // User settings
            options.User.RequireUniqueEmail = true;

            // SignIn settings
            options.SignIn.RequireConfirmedEmail = false; // No hay email verification en Phase 1
            options.SignIn.RequireConfirmedAccount = false;
        });

        // Configurar cookies de autenticación (RS-005 mitigation)
        services.ConfigureApplicationCookie(options =>
        {
            // Cookie settings
            options.Cookie.HttpOnly = true; // RS-005: HttpOnly
            options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always; // RS-005: Secure
            options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict; // RS-005: SameSite=Strict
            options.Cookie.Name = "Ceiba.Auth";

            // Session timeout (FR-005: 30 minutos)
            options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
            options.SlidingExpiration = true; // Renovar si hay actividad

            // Paths
            options.LoginPath = "/Auth/Login";
            options.LogoutPath = "/Auth/Logout";
            options.AccessDeniedPath = "/Auth/AccessDenied";
        });

        return services;
    }
}
