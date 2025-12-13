namespace Ceiba.Core.Entities;

/// <summary>
/// Geographic sector entity (third level in geographic hierarchy).
/// Belongs to a Región, contains multiple Cuadrantes.
/// Example: "Sector 1", "Sector A"
/// Part of hierarchical catalog: Zona → Región → Sector → Cuadrante
/// </summary>
public class Sector : BaseCatalogEntity
{
    /// <summary>
    /// Unique identifier for the sector.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Display name of the sector.
    /// Required, max 100 characters.
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key to the parent Región.
    /// Required - sector must belong to a region.
    /// </summary>
    public int RegionId { get; set; }

    /// <summary>
    /// Navigation property: Parent region.
    /// </summary>
    public virtual Region Region { get; set; } = null!;

    /// <summary>
    /// Navigation property: Cuadrantes within this sector.
    /// Cascade delete configured in EF Core configuration.
    /// </summary>
    public virtual ICollection<Cuadrante> Cuadrantes { get; set; } = new List<Cuadrante>();

    /// <summary>
    /// Navigation property: Reports assigned to this sector.
    /// </summary>
    public virtual ICollection<ReporteIncidencia> Reportes { get; set; } = new List<ReporteIncidencia>();
}
