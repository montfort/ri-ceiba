namespace Ceiba.Core.Entities;

/// <summary>
/// Geographic region entity (second level in geographic hierarchy).
/// Belongs to a Zona, contains multiple Sectores.
/// Example: "Región Norte", "Región Centro"
/// Part of hierarchical catalog: Zona → Región → Sector → Cuadrante
/// </summary>
public class Region : BaseCatalogEntity
{
    /// <summary>
    /// Unique identifier for the region.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Display name of the region.
    /// Required, max 100 characters.
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key to the parent Zona.
    /// Required - region must belong to a zone.
    /// </summary>
    public int ZonaId { get; set; }

    /// <summary>
    /// Navigation property: Parent zone.
    /// </summary>
    public virtual Zona Zona { get; set; } = null!;

    /// <summary>
    /// Navigation property: Sectors within this region.
    /// Cascade delete configured in EF Core configuration.
    /// </summary>
    public virtual ICollection<Sector> Sectores { get; set; } = new List<Sector>();

    /// <summary>
    /// Navigation property: Reports assigned to this region.
    /// </summary>
    public virtual ICollection<ReporteIncidencia> Reportes { get; set; } = new List<ReporteIncidencia>();
}
