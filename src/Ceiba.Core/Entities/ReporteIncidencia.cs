namespace Ceiba.Core.Entities;

/// <summary>
/// Incident report entity (Type A).
/// Core entity for police incident reporting system.
/// Full implementation in Phase 3 (User Story 1).
/// This is a placeholder to support Phase 2 foundational relationships.
/// </summary>
public class ReporteIncidencia : BaseEntityWithUser
{
    /// <summary>
    /// Unique identifier for the report.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Report type identifier.
    /// Default: "A" (Type A report)
    /// Extensible for future types (B, C, etc.)
    /// </summary>
    public string TipoReporte { get; set; } = "A";

    /// <summary>
    /// Report state.
    /// 0 = Borrador (Draft)
    /// 1 = Entregado (Submitted)
    /// </summary>
    public short Estado { get; set; } = 0;

    /// <summary>
    /// Foreign key to Zona.
    /// Required - every report must have a geographic zone.
    /// </summary>
    public int ZonaId { get; set; }

    /// <summary>
    /// Navigation property: Zone where incident occurred.
    /// </summary>
    public virtual Zona Zona { get; set; } = null!;

    /// <summary>
    /// Foreign key to Sector.
    /// Required - every report must have a geographic sector.
    /// </summary>
    public int SectorId { get; set; }

    /// <summary>
    /// Navigation property: Sector where incident occurred.
    /// </summary>
    public virtual Sector Sector { get; set; } = null!;

    /// <summary>
    /// Foreign key to Cuadrante.
    /// Required - every report must have a geographic quadrant.
    /// </summary>
    public int CuadranteId { get; set; }

    /// <summary>
    /// Navigation property: Quadrant where incident occurred.
    /// </summary>
    public virtual Cuadrante Cuadrante { get; set; } = null!;

    // Note: Additional fields will be added in Phase 3 (User Story 1) per data-model.md
    // This minimal entity supports Phase 2 foundational database setup
}
