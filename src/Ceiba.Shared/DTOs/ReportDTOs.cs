using System.ComponentModel.DataAnnotations;

namespace Ceiba.Shared.DTOs;

/// <summary>
/// DTO for creating a new incident report.
/// US1: T037
/// </summary>
public class CreateReportDto
{
    public string TipoReporte { get; set; } = "A";

    [Required(ErrorMessage = "La fecha y hora de los hechos es requerida")]
    public DateTime DatetimeHechos { get; set; }

    [Required(ErrorMessage = "El sexo es requerido")]
    [StringLength(50)]
    public string Sexo { get; set; } = string.Empty;

    [Required(ErrorMessage = "La edad es requerida")]
    [Range(1, 149, ErrorMessage = "La edad debe estar entre 1 y 149")]
    public int Edad { get; set; }

    public bool LgbtttiqPlus { get; set; }
    public bool SituacionCalle { get; set; }
    public bool Migrante { get; set; }
    public bool Discapacidad { get; set; }

    [Required(ErrorMessage = "El delito es requerido")]
    [StringLength(100)]
    public string Delito { get; set; } = string.Empty;

    [Required(ErrorMessage = "La zona es requerida")]
    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar una zona válida")]
    public int ZonaId { get; set; }

    [Required(ErrorMessage = "La región es requerida")]
    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar una región válida")]
    public int RegionId { get; set; }

    [Required(ErrorMessage = "El sector es requerido")]
    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un sector válido")]
    public int SectorId { get; set; }

    [Required(ErrorMessage = "El cuadrante es requerido")]
    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un cuadrante válido")]
    public int CuadranteId { get; set; }

    [Required(ErrorMessage = "El turno CEIBA es requerido")]
    [StringLength(100, ErrorMessage = "El turno CEIBA no puede exceder 100 caracteres")]
    public string TurnoCeiba { get; set; } = string.Empty;

    [Required(ErrorMessage = "El tipo de atención es requerido")]
    [StringLength(100)]
    public string TipoDeAtencion { get; set; } = string.Empty;

    [Required(ErrorMessage = "El tipo de acción es requerido")]
    [StringLength(500, ErrorMessage = "El tipo de acción no puede exceder 500 caracteres")]
    public string TipoDeAccion { get; set; } = string.Empty;

    [Required(ErrorMessage = "Los hechos reportados son requeridos")]
    [MinLength(10, ErrorMessage = "Los hechos reportados deben tener al menos 10 caracteres")]
    public string HechosReportados { get; set; } = string.Empty;

    [Required(ErrorMessage = "Las acciones realizadas son requeridas")]
    [MinLength(10, ErrorMessage = "Las acciones realizadas deben tener al menos 10 caracteres")]
    public string AccionesRealizadas { get; set; } = string.Empty;

    [Required(ErrorMessage = "El estado de traslados es requerido")]
    [StringLength(100, ErrorMessage = "Traslados no puede exceder 100 caracteres")]
    public string Traslados { get; set; } = string.Empty;

    public string? Observaciones { get; set; }
}

/// <summary>
/// DTO for updating an existing report.
/// All fields are optional - only provided fields will be updated.
/// US1: T037
/// </summary>
public class UpdateReportDto
{
    public DateTime? DatetimeHechos { get; set; }

    [StringLength(50)]
    public string? Sexo { get; set; }

    [Range(1, 149, ErrorMessage = "La edad debe estar entre 1 y 149")]
    public int? Edad { get; set; }

    public bool? LgbtttiqPlus { get; set; }
    public bool? SituacionCalle { get; set; }
    public bool? Migrante { get; set; }
    public bool? Discapacidad { get; set; }

    [StringLength(100)]
    public string? Delito { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar una zona válida")]
    public int? ZonaId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar una región válida")]
    public int? RegionId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un sector válido")]
    public int? SectorId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un cuadrante válido")]
    public int? CuadranteId { get; set; }

    [StringLength(100, ErrorMessage = "El turno CEIBA no puede exceder 100 caracteres")]
    public string? TurnoCeiba { get; set; }

    [StringLength(100)]
    public string? TipoDeAtencion { get; set; }

    [StringLength(500, ErrorMessage = "El tipo de acción no puede exceder 500 caracteres")]
    public string? TipoDeAccion { get; set; }

    [MinLength(10, ErrorMessage = "Los hechos reportados deben tener al menos 10 caracteres")]
    public string? HechosReportados { get; set; }

    [MinLength(10, ErrorMessage = "Las acciones realizadas deben tener al menos 10 caracteres")]
    public string? AccionesRealizadas { get; set; }

    [StringLength(100, ErrorMessage = "Traslados no puede exceder 100 caracteres")]
    public string? Traslados { get; set; }

    public string? Observaciones { get; set; }
}

/// <summary>
/// DTO for report responses.
/// US1: T037
/// </summary>
public class ReportDto
{
    public int Id { get; set; }
    public string TipoReporte { get; set; } = string.Empty;
    public int Estado { get; set; }
    public Guid UsuarioId { get; set; }
    public string? UsuarioEmail { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime DatetimeHechos { get; set; }
    public string Sexo { get; set; } = string.Empty;
    public int Edad { get; set; }
    public bool LgbtttiqPlus { get; set; }
    public bool SituacionCalle { get; set; }
    public bool Migrante { get; set; }
    public bool Discapacidad { get; set; }
    public string Delito { get; set; } = string.Empty;
    public CatalogItemDto Zona { get; set; } = null!;
    public CatalogItemDto Region { get; set; } = null!;
    public CatalogItemDto Sector { get; set; } = null!;
    public CatalogItemDto Cuadrante { get; set; } = null!;
    public string TurnoCeiba { get; set; } = string.Empty;
    public string TipoDeAtencion { get; set; } = string.Empty;
    public string TipoDeAccion { get; set; } = string.Empty;
    public string HechosReportados { get; set; } = string.Empty;
    public string AccionesRealizadas { get; set; } = string.Empty;
    public string Traslados { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
}

/// <summary>
/// DTO for catalog items (Zona, Región, Sector, Cuadrante).
/// US1: T037
/// </summary>
public class CatalogItemDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

/// <summary>
/// DTO for report filtering and search.
/// US1: T037
/// </summary>
public class ReportFilterDto
{
    public int? Estado { get; set; }
    public int? ZonaId { get; set; }
    public string? Delito { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "createdAt";
    public bool SortDesc { get; set; } = true;
}

/// <summary>
/// DTO for paginated report list response.
/// US1: T037
/// </summary>
public class ReportListResponse
{
    public List<ReportDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
