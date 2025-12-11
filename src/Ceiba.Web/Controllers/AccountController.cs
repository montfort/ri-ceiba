using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Ceiba.Web.Controllers;

[Route("[controller]")]
public class AccountController : Controller
{
    private readonly SignInManager<IdentityUser<Guid>> _signInManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<IdentityUser<Guid>> signInManager,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _logger = logger;
    }

    [HttpPost("login-form")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginForm([FromForm] string email, [FromForm] string password, [FromForm] bool rememberMe = false, [FromForm] string? returnUrl = null)
    {
        try
        {
            var result = await _signInManager.PasswordSignInAsync(
                email,
                password,
                rememberMe,
                lockoutOnFailure: true
            );

            if (result.Succeeded)
            {
                _logger.LogInformation("User {Email} logged in successfully", email);
                var redirectUrl = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl;
                return Redirect(redirectUrl);
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User {Email} account locked out", email);
                return Redirect($"/login?error=locked&returnUrl={returnUrl}");
            }

            if (result.RequiresTwoFactor)
            {
                return Redirect($"/login?error=2fa&returnUrl={returnUrl}");
            }

            _logger.LogWarning("Failed login attempt for {Email}", email);
            return Redirect($"/login?error=invalid&returnUrl={returnUrl}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for {Email}", email);
            return Redirect($"/login?error=server&returnUrl={returnUrl}");
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Datos de login inválidos" });
        }

        try
        {
            var result = await _signInManager.PasswordSignInAsync(
                request.Email,
                request.Password,
                request.RememberMe,
                lockoutOnFailure: true
            );

            if (result.Succeeded)
            {
                _logger.LogInformation("User {Email} logged in successfully", request.Email);
                return Ok(new { success = true, message = "Login exitoso" });
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User {Email} account locked out", request.Email);
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

            _logger.LogWarning("Failed login attempt for {Email}", request.Email);
            return BadRequest(new
            {
                success = false,
                message = "Correo electrónico o contraseña incorrectos."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for {Email}", request.Email);
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
