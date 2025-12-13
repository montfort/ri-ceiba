namespace Ceiba.Core.Entities;

/// <summary>
/// Geographic zone entity (highest level in geographic hierarchy).
/// Example: "Zona Norte", "Zona Sur", "Zona Centro"
/// Part of hierarchical catalog: Zona → Región → Sector → Cuadrante
/// </summary>
public class Zona : BaseCatalogEntity
{
    /// <summary>
    /// Unique identifier for the zone.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Display name of the zone.
    /// Required, max 100 characters.
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property: Regions within this zone.
    /// Cascade delete configured in EF Core configuration.
    /// </summary>
    public virtual ICollection<Region> Regiones { get; set; } = new List<Region>();

    /// <summary>
    /// Navigation property: Reports assigned to this zone.
    /// </summary>
    public virtual ICollection<ReporteIncidencia> Reportes { get; set; } = new List<ReporteIncidencia>();
}
