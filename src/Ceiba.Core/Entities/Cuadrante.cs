namespace Ceiba.Core.Entities;

/// <summary>
/// Geographic quadrant entity (lowest level in geographic hierarchy).
/// Belongs to a Sector, which belongs to a Zona.
/// Example: "Cuadrante 1-A", "Cuadrante Norte-1"
/// Part of hierarchical catalog: Zona → Sector → Cuadrante
/// </summary>
public class Cuadrante : BaseCatalogEntity
{
    /// <summary>
    /// Unique identifier for the cuadrante.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Display name of the cuadrante.
    /// Required, max 100 characters.
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key to the parent Sector.
    /// Required - cuadrante must belong to a sector.
    /// </summary>
    public int SectorId { get; set; }

    /// <summary>
    /// Navigation property: Parent sector.
    /// </summary>
    public virtual Sector Sector { get; set; } = null!;

    /// <summary>
    /// Navigation property: Reports assigned to this cuadrante.
    /// </summary>
    public virtual ICollection<ReporteIncidencia> Reportes { get; set; } = new List<ReporteIncidencia>();
}
