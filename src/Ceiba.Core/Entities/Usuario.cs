using Microsoft.AspNetCore.Identity;

namespace Ceiba.Core.Entities;

/// <summary>
/// Custom user entity extending ASP.NET Identity.
/// Adds application-specific properties for user management.
/// </summary>
/// <remarks>
/// Inherits from IdentityUser&lt;Guid&gt; which provides:
/// - Id, UserName, Email, PasswordHash, SecurityStamp, etc.
/// - LockoutEnd, LockoutEnabled, AccessFailedCount for security
/// - EmailConfirmed, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled
/// </remarks>
public class Usuario : IdentityUser<Guid>
{
    /// <summary>
    /// Full display name of the user.
    /// Used for UI display and reports instead of email.
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when the user account was created.
    /// Set automatically on first save.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// UTC timestamp of the user's last successful login.
    /// NULL if the user has never logged in.
    /// Updated by AccountController on successful authentication.
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Indicates whether the user account is active.
    /// Suspended users (Activo = false) cannot log in.
    /// </summary>
    public bool Activo { get; set; } = true;

    /// <summary>
    /// Navigation property to incident reports created by this user.
    /// Only populated for users with CREADOR role.
    /// </summary>
    public virtual ICollection<ReporteIncidencia> Reportes { get; set; } = new List<ReporteIncidencia>();
}
