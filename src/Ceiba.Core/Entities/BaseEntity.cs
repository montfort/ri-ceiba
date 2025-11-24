namespace Ceiba.Core.Entities;

/// <summary>
/// Base entity class with common audit fields.
/// All entities should inherit from this class to ensure consistent audit tracking.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// UTC timestamp when the entity was created.
    /// Automatically set by the database interceptor.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Base entity class for entities with a user creator.
/// Extends BaseEntity with user tracking.
/// </summary>
public abstract class BaseEntityWithUser : BaseEntity
{
    /// <summary>
    /// User who created or last modified this entity.
    /// References Usuario.Id (ASP.NET Identity user).
    /// </summary>
    public Guid UsuarioId { get; set; }
}

/// <summary>
/// Base entity class for catalog entities that can be activated/deactivated.
/// Extends BaseEntityWithUser with active status flag.
/// </summary>
public abstract class BaseCatalogEntity : BaseEntityWithUser
{
    /// <summary>
    /// Indicates if this catalog entry is active.
    /// Inactive entries are hidden from user selection but retained for historical data.
    /// </summary>
    public bool Activo { get; set; } = true;
}
