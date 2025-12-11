using Ceiba.Application.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Ceiba.Web.Controllers;

[Route("[controller]")]
public class AccountController : Controller
{
    private readonly SignInManager<IdentityUser<Guid>> _signInManager;
    private readonly ILoginSecurityService _loginSecurity;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<IdentityUser<Guid>> signInManager,
        ILoginSecurityService loginSecurity,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _loginSecurity = loginSecurity;
        _logger = logger;
    }

    [HttpPost("login-form")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginForm(
        [FromForm] string email,
        [FromForm] string password,
        [FromForm] bool rememberMe = false,
        [FromForm] string? returnUrl = null)
    {
        var ipAddress = GetClientIpAddress();
        var safeReturnUrl = GetSafeReturnUrl(returnUrl);

        try
        {
            // Check if IP is blocked
            if (await _loginSecurity.IsIpBlockedAsync(ipAddress))
            {
                var remainingSeconds = await _loginSecurity.GetRemainingLockoutSecondsAsync(ipAddress);
                _logger.LogWarning("Blocked IP {IpAddress} attempted login for {Email}", ipAddress, email);
                return Redirect($"/login?error=blocked&seconds={remainingSeconds}&returnUrl={safeReturnUrl}");
            }

            // Apply progressive delay
            var delayMs = await _loginSecurity.GetProgressiveDelayAsync(ipAddress);
            if (delayMs > 0)
            {
                await Task.Delay(delayMs);
            }

            var result = await _signInManager.PasswordSignInAsync(
                email,
                password,
                rememberMe,
                lockoutOnFailure: true
            );

            if (result.Succeeded)
            {
                await _loginSecurity.RecordSuccessfulLoginAsync(ipAddress, email);
                _logger.LogInformation("User {Email} logged in successfully from IP {IpAddress}", email, ipAddress);
                return Redirect(string.IsNullOrEmpty(safeReturnUrl) ? "/" : safeReturnUrl);
            }

            // Record failed attempt
            await _loginSecurity.RecordFailedAttemptAsync(ipAddress, email);

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User {Email} account locked out (Identity lockout)", email);
                return Redirect($"/login?error=locked&returnUrl={safeReturnUrl}");
            }

            if (result.RequiresTwoFactor)
            {
                return Redirect($"/login?error=2fa&returnUrl={safeReturnUrl}");
            }

            _logger.LogWarning("Failed login attempt for {Email} from IP {IpAddress}", email, ipAddress);
            return Redirect($"/login?error=invalid&returnUrl={safeReturnUrl}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for {Email} from IP {IpAddress}", email, ipAddress);
            return Redirect($"/login?error=server&returnUrl={safeReturnUrl}");
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Datos de login inválidos" });
        }

        var ipAddress = GetClientIpAddress();

        try
        {
            // Check if IP is blocked
            if (await _loginSecurity.IsIpBlockedAsync(ipAddress))
            {
                var remainingSeconds = await _loginSecurity.GetRemainingLockoutSecondsAsync(ipAddress);
                _logger.LogWarning("Blocked IP {IpAddress} attempted login for {Email}", ipAddress, request.Email);
                return StatusCode(429, new
                {
                    success = false,
                    message = $"Demasiados intentos fallidos. Intente nuevamente en {remainingSeconds / 60} minutos.",
                    remainingSeconds
                });
            }

            // Apply progressive delay
            var delayMs = await _loginSecurity.GetProgressiveDelayAsync(ipAddress);
            if (delayMs > 0)
            {
                await Task.Delay(delayMs);
            }

            var result = await _signInManager.PasswordSignInAsync(
                request.Email,
                request.Password,
                request.RememberMe,
                lockoutOnFailure: true
            );

            if (result.Succeeded)
            {
                await _loginSecurity.RecordSuccessfulLoginAsync(ipAddress, request.Email);
                _logger.LogInformation("User {Email} logged in successfully from IP {IpAddress}", request.Email, ipAddress);
                return Ok(new { success = true, message = "Login exitoso" });
            }

            // Record failed attempt
            await _loginSecurity.RecordFailedAttemptAsync(ipAddress, request.Email);
            var failedAttempts = await _loginSecurity.GetFailedAttemptCountAsync(ipAddress);

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User {Email} account locked out (Identity lockout)", request.Email);
                return BadRequest(new
                {
                    success = false,
                    message = "Su cuenta ha sido bloqueada temporalmente por múltiples intentos fallidos. Intente más tarde."
                });
            }

            if (result.RequiresTwoFactor)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Se requiere autenticación de dos factores (no implementado aún)."
                });
            }

            _logger.LogWarning("Failed login attempt {Count} for {Email} from IP {IpAddress}", failedAttempts, request.Email, ipAddress);

            // Generic error message (don't reveal if email exists)
            return BadRequest(new
            {
                success = false,
                message = "Correo electrónico o contraseña incorrectos.",
                remainingAttempts = Math.Max(0, 10 - failedAttempts)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for {Email} from IP {IpAddress}", request.Email, ipAddress);
            return StatusCode(500, new
            {
                success = false,
                message = "Ocurrió un error al intentar iniciar sesión. Por favor, intente nuevamente."
            });
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        try
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out successfully");
            return Ok(new { success = true, message = "Logout exitoso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, new
            {
                success = false,
                message = "Ocurrió un error al cerrar sesión."
            });
        }
    }

    [HttpPost("logout-form")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LogoutForm()
    {
        try
        {
            var userName = User.Identity?.Name ?? "Unknown";
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User {UserName} logged out successfully via form", userName);
            return Redirect("/login?logout=success");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return Redirect("/login?error=logout");
        }
    }

    [HttpGet("perform-logout")]
    public async Task<IActionResult> PerformLogout()
    {
        try
        {
            var userName = User.Identity?.Name ?? "Unknown";
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User {UserName} logged out successfully", userName);
            return Redirect("/login?logout=success");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return Redirect("/login?error=logout");
        }
    }

    /// <summary>
    /// Gets the client IP address, considering proxy headers.
    /// </summary>
    private string GetClientIpAddress()
    {
        // Check for forwarded IP (behind proxy/load balancer)
        var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // Take the first IP in the chain (original client)
            var ip = forwardedFor.Split(',').FirstOrDefault()?.Trim();
            if (!string.IsNullOrEmpty(ip))
            {
                return ip;
            }
        }

        // Check X-Real-IP header (common in nginx)
        var realIp = HttpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // Fall back to connection remote IP
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    /// <summary>
    /// Validates and sanitizes the return URL to prevent open redirect attacks.
    /// </summary>
    private string? GetSafeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrEmpty(returnUrl))
        {
            return null;
        }

        // Only allow local URLs (starting with / but not //)
        if (returnUrl.StartsWith('/') && !returnUrl.StartsWith("//"))
        {
            // Additional check: URL shouldn't contain protocol indicators
            if (!returnUrl.Contains("://") && !returnUrl.Contains("%3a%2f%2f", StringComparison.OrdinalIgnoreCase))
            {
                return returnUrl;
            }
        }

        _logger.LogWarning("Potentially malicious returnUrl blocked: {ReturnUrl}", returnUrl);
        return null;
    }

    public class LoginRequest
    {
        [Required(ErrorMessage = "El correo electrónico es obligatorio")]
        [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [MinLength(10, ErrorMessage = "La contraseña debe tener al menos 10 caracteres")]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; } = false;
    }
}
