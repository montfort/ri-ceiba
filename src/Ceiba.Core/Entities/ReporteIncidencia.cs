using System.Text.Json;

namespace Ceiba.Core.Entities;

/// <summary>
/// Incident report entity (Type A).
/// Core entity for police incident reporting system.
/// US1: T031
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
    /// 0 = Borrador (Draft) - editable by CREADOR
    /// 1 = Entregado (Submitted) - not editable by CREADOR
    /// </summary>
    public short Estado { get; set; } = 0;

    /// <summary>
    /// Date and time when the incident occurred.
    /// Required field, stored in UTC.
    /// </summary>
    public DateTime DatetimeHechos { get; set; }

    /// <summary>
    /// Gender of the person involved.
    /// Suggested values configured in CatalogoSugerencia.
    /// Examples: "Masculino", "Femenino", "No binario"
    /// </summary>
    public string Sexo { get; set; } = string.Empty;

    /// <summary>
    /// Age of the person involved.
    /// Validation: 1-149
    /// </summary>
    public int Edad { get; set; }

    /// <summary>
    /// LGBTTTIQ+ community member indicator.
    /// Default: false
    /// </summary>
    public bool LgbtttiqPlus { get; set; } = false;

    /// <summary>
    /// Homeless person indicator.
    /// Default: false
    /// </summary>
    public bool SituacionCalle { get; set; } = false;

    /// <summary>
    /// Migrant person indicator.
    /// Default: false
    /// </summary>
    public bool Migrante { get; set; } = false;

    /// <summary>
    /// Disability indicator.
    /// Default: false
    /// </summary>
    public bool Discapacidad { get; set; } = false;

    /// <summary>
    /// Crime/incident type.
    /// Suggested values configured in CatalogoSugerencia.
    /// Examples: "Violencia familiar", "Robo", "Lesiones"
    /// </summary>
    public string Delito { get; set; } = string.Empty;

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
    /// Must belong to the selected Zona.
    /// </summary>
    public int SectorId { get; set; }

    /// <summary>
    /// Navigation property: Sector where incident occurred.
    /// </summary>
    public virtual Sector Sector { get; set; } = null!;

    /// <summary>
    /// Foreign key to Cuadrante.
    /// Required - every report must have a geographic quadrant.
    /// Must belong to the selected Sector.
    /// </summary>
    public int CuadranteId { get; set; }

    /// <summary>
    /// Navigation property: Quadrant where incident occurred.
    /// </summary>
    public virtual Cuadrante Cuadrante { get; set; } = null!;

    /// <summary>
    /// Ceiba shift identifier.
    /// Required field identifying the shift during which the incident occurred.
    /// </summary>
    public int TurnoCeiba { get; set; }

    /// <summary>
    /// Type of attention/service provided.
    /// Suggested values configured in CatalogoSugerencia.
    /// Examples: "Presencial", "Telefónica", "Derivación"
    /// </summary>
    public string TipoDeAtencion { get; set; } = string.Empty;

    /// <summary>
    /// Type of action taken.
    /// 1 = ATOS (Atención y Orientación)
    /// 2 = Capacitación
    /// 3 = Prevención
    /// </summary>
    public short TipoDeAccion { get; set; }

    /// <summary>
    /// Detailed description of the reported facts/incident.
    /// Required field, long text.
    /// </summary>
    public string HechosReportados { get; set; } = string.Empty;

    /// <summary>
    /// Actions taken by the officer in response to the incident.
    /// Required field, long text.
    /// </summary>
    public string AccionesRealizadas { get; set; } = string.Empty;

    /// <summary>
    /// Transfer status.
    /// 0 = Sin traslados (No transfers)
    /// 1 = Con traslados (With transfers)
    /// 2 = No aplica (Not applicable)
    /// </summary>
    public short Traslados { get; set; }

    /// <summary>
    /// Additional observations/notes.
    /// Optional field, long text.
    /// </summary>
    public string? Observaciones { get; set; }

    /// <summary>
    /// Last update timestamp (UTC).
    /// Nullable - only set after updates.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Extensible JSON fields for future report types (RT-004 mitigation).
    /// Allows adding new fields without schema migrations.
    /// Example: {"campo_especifico_B": "valor", "otro_campo": 123}
    /// </summary>
    public string? CamposAdicionales { get; set; }

    /// <summary>
    /// Schema version for migration tracking (RT-004 mitigation).
    /// Default: "1.0"
    /// </summary>
    public string SchemaVersion { get; set; } = "1.0";

    #region State Transition Methods

    /// <summary>
    /// Checks if the report is in Borrador (draft) state.
    /// </summary>
    public bool IsBorrador() => Estado == 0;

    /// <summary>
    /// Checks if the report is in Entregado (submitted) state.
    /// </summary>
    public bool IsEntregado() => Estado == 1;

    /// <summary>
    /// Submits the report, changing estado from 0 to 1.
    /// Irreversible transition.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if report is already submitted</exception>
    public void Submit()
    {
        if (Estado == 1)
            throw new InvalidOperationException("El reporte ya fue entregado y no puede ser entregado nuevamente.");

        Estado = 1;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the report can be edited by a CREADOR user.
    /// CREADOR can only edit their own reports while in Borrador state.
    /// </summary>
    public bool CanBeEditedByCreador(Guid creadorUserId)
    {
        return Estado == 0 && UsuarioId == creadorUserId;
    }

    /// <summary>
    /// Checks if the report can be submitted by a CREADOR user.
    /// CREADOR can only submit their own reports while in Borrador state.
    /// </summary>
    public bool CanBeSubmittedByCreador(Guid creadorUserId)
    {
        return Estado == 0 && UsuarioId == creadorUserId;
    }

    #endregion

    #region Validation Methods

    /// <summary>
    /// Validates the entity's field values.
    /// </summary>
    public ValidationResult Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Sexo))
            errors.Add("El campo 'sexo' es requerido.");

        if (Edad < 1 || Edad > 149)
            errors.Add("El campo 'edad' debe estar entre 1 y 149.");

        if (string.IsNullOrWhiteSpace(Delito))
            errors.Add("El campo 'delito' es requerido.");

        if (ZonaId <= 0)
            errors.Add("El campo 'zonaId' es requerido.");

        if (SectorId <= 0)
            errors.Add("El campo 'sectorId' es requerido.");

        if (CuadranteId <= 0)
            errors.Add("El campo 'cuadranteId' es requerido.");

        if (!ValidateTipoDeAccion())
            errors.Add("El campo 'tipoDeAccion' debe ser 1, 2 o 3.");

        if (!ValidateTraslados())
            errors.Add("El campo 'traslados' debe ser 0, 1 o 2.");

        if (string.IsNullOrWhiteSpace(HechosReportados))
            errors.Add("El campo 'hechosReportados' es requerido.");

        if (string.IsNullOrWhiteSpace(AccionesRealizadas))
            errors.Add("El campo 'accionesRealizadas' es requerido.");

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }

    /// <summary>
    /// Validates that TipoDeAccion is one of the allowed values (1, 2, 3).
    /// </summary>
    public bool ValidateTipoDeAccion()
    {
        return TipoDeAccion >= 1 && TipoDeAccion <= 3;
    }

    /// <summary>
    /// Validates that Traslados is one of the allowed values (0, 1, 2).
    /// </summary>
    public bool ValidateTraslados()
    {
        return Traslados >= 0 && Traslados <= 2;
    }

    #endregion

    #region JSONB Helper Methods

    /// <summary>
    /// Sets additional fields in the JSONB column.
    /// </summary>
    public void SetCamposAdicionales(Dictionary<string, object> additionalFields)
    {
        CamposAdicionales = JsonSerializer.Serialize(additionalFields);
    }

    /// <summary>
    /// Gets additional fields from the JSONB column.
    /// </summary>
    public Dictionary<string, object> GetCamposAdicionales()
    {
        if (string.IsNullOrWhiteSpace(CamposAdicionales))
            return new Dictionary<string, object>();

        return JsonSerializer.Deserialize<Dictionary<string, object>>(CamposAdicionales)
            ?? new Dictionary<string, object>();
    }

    #endregion
}

/// <summary>
/// Validation result container.
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}
