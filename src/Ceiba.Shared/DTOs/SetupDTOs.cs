using System.ComponentModel.DataAnnotations;

namespace Ceiba.Shared.DTOs;

/// <summary>
/// Represents the current setup status of the application.
/// </summary>
public class SetupStatus
{
    /// <summary>
    /// Whether the initial setup is complete.
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// Whether any users exist in the system.
    /// </summary>
    public bool HasUsers { get; set; }

    /// <summary>
    /// Whether roles have been created.
    /// </summary>
    public bool HasRoles { get; set; }

    /// <summary>
    /// Whether geographic catalogs have been seeded.
    /// </summary>
    public bool HasGeographicCatalogs { get; set; }

    /// <summary>
    /// Whether suggestions have been seeded.
    /// </summary>
    public bool HasSuggestions { get; set; }

    /// <summary>
    /// Message describing the current setup state.
    /// </summary>
    public string? Message { get; set; }
}

/// <summary>
/// DTO for creating the first administrator user during setup.
/// </summary>
public class CreateFirstAdminDto
{
    /// <summary>
    /// Administrator's email address. Will also be used as username.
    /// </summary>
    [Required(ErrorMessage = "El correo electrónico es requerido")]
    [EmailAddress(ErrorMessage = "Formato de correo electrónico inválido")]
    [StringLength(256, ErrorMessage = "El correo no puede exceder 256 caracteres")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Administrator's password.
    /// Must meet password policy: min 10 chars, uppercase, digit.
    /// </summary>
    [Required(ErrorMessage = "La contraseña es requerida")]
    [StringLength(100, MinimumLength = 10, ErrorMessage = "La contraseña debe tener entre 10 y 100 caracteres")]
    [RegularExpression(@"^(?=.*[A-Z])(?=.*\d).+$",
        ErrorMessage = "La contraseña debe contener al menos una mayúscula y un número")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Password confirmation.
    /// </summary>
    [Required(ErrorMessage = "Confirme la contraseña")]
    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// Result of the setup process.
/// </summary>
public class SetupResult
{
    /// <summary>
    /// Whether the setup was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error messages if setup failed.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// The ID of the created admin user (if successful).
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static SetupResult Succeeded(Guid userId) => new()
    {
        Success = true,
        UserId = userId
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    public static SetupResult Failed(params string[] errors) => new()
    {
        Success = false,
        Errors = errors.ToList()
    };
}
