using System.ComponentModel.DataAnnotations;

namespace Ceiba.Shared.DTOs;

#region User Management DTOs

/// <summary>
/// DTO for user listing in admin panel
/// </summary>
public record UserDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public List<string> Roles { get; init; } = new();
    public bool Activo { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastLogin { get; init; }
}

/// <summary>
/// DTO for creating a new user
/// </summary>
public record CreateUserDto
{
    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres")]
    public string Nombre { get; init; } = string.Empty;

    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "El email no tiene un formato válido")]
    [StringLength(256, ErrorMessage = "El email no puede exceder 256 caracteres")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es requerida")]
    [StringLength(100, MinimumLength = 10, ErrorMessage = "La contraseña debe tener al menos 10 caracteres")]
    public string Password { get; init; } = string.Empty;

    [Required(ErrorMessage = "Debe asignar al menos un rol")]
    [MinLength(1, ErrorMessage = "Debe asignar al menos un rol")]
    public List<string> Roles { get; init; } = new();
}

/// <summary>
/// DTO for updating an existing user
/// </summary>
public record UpdateUserDto
{
    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres")]
    public string Nombre { get; init; } = string.Empty;

    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "El email no tiene un formato válido")]
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Optional: Only set if changing password
    /// </summary>
    [StringLength(100, MinimumLength = 10, ErrorMessage = "La contraseña debe tener al menos 10 caracteres")]
    public string? NewPassword { get; init; }

    [Required(ErrorMessage = "Debe asignar al menos un rol")]
    [MinLength(1, ErrorMessage = "Debe asignar al menos un rol")]
    public List<string> Roles { get; init; } = new();

    public bool Activo { get; init; } = true;
}

/// <summary>
/// DTO for user list response with pagination
/// </summary>
public record UserListResponse
{
    public List<UserDto> Items { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

/// <summary>
/// DTO for user filter/search
/// </summary>
public record UserFilterDto
{
    public string? Search { get; init; }
    public string? Role { get; init; }
    public bool? Activo { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

#endregion

#region Catalog Management DTOs

/// <summary>
/// DTO for Zona (geographic zone - top level)
/// Hierarchy: Zona → Región → Sector → Cuadrante
/// </summary>
public record ZonaDto
{
    public int Id { get; init; }

    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres")]
    public string Nombre { get; init; } = string.Empty;

    public bool Activo { get; init; } = true;
    public int RegionesCount { get; init; }
}

/// <summary>
/// DTO for creating/updating a Zona
/// </summary>
public record CreateZonaDto
{
    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres")]
    public string Nombre { get; init; } = string.Empty;

    public bool Activo { get; init; } = true;
}

/// <summary>
/// DTO for Región (linked to Zona)
/// Hierarchy: Zona → Región → Sector → Cuadrante
/// </summary>
public record RegionDto
{
    public int Id { get; init; }

    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres")]
    public string Nombre { get; init; } = string.Empty;

    public int ZonaId { get; init; }
    public string? ZonaNombre { get; init; }
    public bool Activo { get; init; } = true;
    public int SectoresCount { get; init; }
}

/// <summary>
/// DTO for creating/updating a Región
/// </summary>
public record CreateRegionDto
{
    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres")]
    public string Nombre { get; init; } = string.Empty;

    [Required(ErrorMessage = "La zona es requerida")]
    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar una zona válida")]
    public int ZonaId { get; init; }

    public bool Activo { get; init; } = true;
}

/// <summary>
/// DTO for Sector (linked to Región)
/// Hierarchy: Zona → Región → Sector → Cuadrante
/// </summary>
public record SectorDto
{
    public int Id { get; init; }

    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres")]
    public string Nombre { get; init; } = string.Empty;

    public int RegionId { get; init; }
    public string? RegionNombre { get; init; }
    public int ZonaId { get; init; }
    public string? ZonaNombre { get; init; }
    public bool Activo { get; init; } = true;
    public int CuadrantesCount { get; init; }
}

/// <summary>
/// DTO for creating/updating a Sector
/// </summary>
public record CreateSectorDto
{
    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres")]
    public string Nombre { get; init; } = string.Empty;

    [Required(ErrorMessage = "La región es requerida")]
    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar una región válida")]
    public int RegionId { get; init; }

    public bool Activo { get; init; } = true;
}

/// <summary>
/// DTO for Cuadrante (linked to Sector)
/// Hierarchy: Zona → Región → Sector → Cuadrante
/// </summary>
public record CuadranteDto
{
    public int Id { get; init; }

    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres")]
    public string Nombre { get; init; } = string.Empty;

    public int SectorId { get; init; }
    public string? SectorNombre { get; init; }
    public int RegionId { get; init; }
    public string? RegionNombre { get; init; }
    public int ZonaId { get; init; }
    public string? ZonaNombre { get; init; }
    public bool Activo { get; init; } = true;
}

/// <summary>
/// DTO for creating/updating a Cuadrante
/// </summary>
public record CreateCuadranteDto
{
    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres")]
    public string Nombre { get; init; } = string.Empty;

    [Required(ErrorMessage = "El sector es requerido")]
    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un sector válido")]
    public int SectorId { get; init; }

    public bool Activo { get; init; } = true;
}

/// <summary>
/// DTO for suggestion catalog item
/// </summary>
public record SugerenciaDto
{
    public int Id { get; init; }

    [Required(ErrorMessage = "El campo es requerido")]
    [StringLength(50, ErrorMessage = "El campo no puede exceder 50 caracteres")]
    public string Campo { get; init; } = string.Empty;

    [Required(ErrorMessage = "El valor es requerido")]
    [StringLength(200, ErrorMessage = "El valor no puede exceder 200 caracteres")]
    public string Valor { get; init; } = string.Empty;

    public int Orden { get; init; }
    public bool Activo { get; init; } = true;
}

/// <summary>
/// DTO for creating/updating a suggestion
/// </summary>
public record CreateSugerenciaDto
{
    [Required(ErrorMessage = "El campo es requerido")]
    [StringLength(50, ErrorMessage = "El campo no puede exceder 50 caracteres")]
    public string Campo { get; init; } = string.Empty;

    [Required(ErrorMessage = "El valor es requerido")]
    [StringLength(200, ErrorMessage = "El valor no puede exceder 200 caracteres")]
    public string Valor { get; init; } = string.Empty;

    public int Orden { get; init; } = 0;
    public bool Activo { get; init; } = true;
}

/// <summary>
/// Available suggestion field types
/// </summary>
public static class SugerenciaCampos
{
    // Datos de la Persona
    public const string Sexo = "sexo";
    public const string Delito = "delito";
    public const string TipoDeAtencion = "tipo_de_atencion";

    // Detalles Operativos
    public const string TurnoCeiba = "turno_ceiba";
    public const string TipoDeAccion = "tipo_de_accion";
    public const string Traslados = "traslados";

    public static readonly string[] All = { Sexo, Delito, TipoDeAtencion, TurnoCeiba, TipoDeAccion, Traslados };

    // Grupos para la UI
    public static readonly string[] DatosPersona = { Sexo, Delito, TipoDeAtencion };
    public static readonly string[] DetallesOperativos = { TurnoCeiba, TipoDeAccion, Traslados };

    public static string GetDisplayName(string campo) => campo switch
    {
        Sexo => "Sexo",
        Delito => "Tipo de Delito",
        TipoDeAtencion => "Tipo de Atención",
        TurnoCeiba => "Turno CEIBA",
        TipoDeAccion => "Tipo de Acción",
        Traslados => "Traslados",
        _ => campo
    };
}

#endregion

#region Audit DTOs

/// <summary>
/// DTO for audit log entry display
/// </summary>
public record AuditLogEntryDto
{
    public long Id { get; init; }
    public string Codigo { get; init; } = string.Empty;
    public string? CodigoDescripcion { get; init; }
    public int? IdRelacionado { get; init; }
    public string? TablaRelacionada { get; init; }
    public DateTime CreatedAt { get; init; }
    public Guid? UsuarioId { get; init; }
    public string? UsuarioEmail { get; init; }
    public string? Ip { get; init; }
    public string? Detalles { get; init; }
}

/// <summary>
/// DTO for audit log filter/search
/// </summary>
public record AuditFilterDto
{
    public string? Codigo { get; init; }
    public Guid? UsuarioId { get; init; }
    public DateTime? FechaDesde { get; init; }
    public DateTime? FechaHasta { get; init; }
    public string? TablaRelacionada { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

/// <summary>
/// DTO for audit log list response with pagination
/// </summary>
public record AuditListResponse
{
    public List<AuditLogEntryDto> Items { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

/// <summary>
/// Standard audit action codes
/// </summary>
public static class AuditCodes
{
    // Authentication
    public const string AUTH_LOGIN = "AUTH_LOGIN";
    public const string AUTH_LOGOUT = "AUTH_LOGOUT";
    public const string AUTH_FAILED = "AUTH_FAILED";
    public const string AUTH_LOCKED = "AUTH_LOCKED";

    // User Management
    public const string USER_CREATE = "USER_CREATE";
    public const string USER_UPDATE = "USER_UPDATE";
    public const string USER_SUSPEND = "USER_SUSPEND";
    public const string USER_ACTIVATE = "USER_ACTIVATE";
    public const string USER_DELETE = "USER_DELETE";
    public const string USER_ROLE_CHANGE = "USER_ROLE_CHANGE";

    // Report Operations
    public const string REPORT_CREATE = "REPORT_CREATE";
    public const string REPORT_UPDATE = "REPORT_UPDATE";
    public const string REPORT_SUBMIT = "REPORT_SUBMIT";
    public const string REPORT_DELETE = "REPORT_DELETE";
    public const string REPORT_VIEW = "REPORT_VIEW";
    public const string REPORT_EXPORT = "REPORT_EXPORT";
    public const string REPORT_EXPORT_BULK = "REPORT_EXPORT_BULK";

    // Catalog Operations
    public const string CATALOG_CREATE = "CATALOG_CREATE";
    public const string CATALOG_UPDATE = "CATALOG_UPDATE";
    public const string CATALOG_DELETE = "CATALOG_DELETE";

    // Security
    public const string ACCESS_DENIED = "ACCESS_DENIED";
    public const string SESSION_EXPIRED = "SESSION_EXPIRED";

    // Automated Reports (US4)
    public const string AUTO_REPORT_GEN = "AUTO_REPORT_GEN";
    public const string AUTO_REPORT_SEND = "AUTO_REPORT_SEND";
    public const string AUTO_REPORT_FAIL = "AUTO_REPORT_FAIL";

    public static string GetDescription(string code) => code switch
    {
        AUTH_LOGIN => "Inicio de sesión",
        AUTH_LOGOUT => "Cierre de sesión",
        AUTH_FAILED => "Intento de inicio de sesión fallido",
        AUTH_LOCKED => "Cuenta bloqueada",
        USER_CREATE => "Usuario creado",
        USER_UPDATE => "Usuario actualizado",
        USER_SUSPEND => "Usuario suspendido",
        USER_ACTIVATE => "Usuario activado",
        USER_DELETE => "Usuario eliminado",
        USER_ROLE_CHANGE => "Roles de usuario modificados",
        REPORT_CREATE => "Reporte creado",
        REPORT_UPDATE => "Reporte actualizado",
        REPORT_SUBMIT => "Reporte entregado",
        REPORT_DELETE => "Reporte eliminado",
        REPORT_VIEW => "Reporte visualizado",
        REPORT_EXPORT => "Reporte exportado",
        REPORT_EXPORT_BULK => "Exportación masiva de reportes",
        CATALOG_CREATE => "Catálogo creado",
        CATALOG_UPDATE => "Catálogo actualizado",
        CATALOG_DELETE => "Catálogo eliminado",
        ACCESS_DENIED => "Acceso denegado",
        SESSION_EXPIRED => "Sesión expirada",
        AUTO_REPORT_GEN => "Reporte automatizado generado",
        AUTO_REPORT_SEND => "Reporte automatizado enviado",
        AUTO_REPORT_FAIL => "Error en reporte automatizado",
        _ => code
    };
}

#endregion

#region Geographic Catalog DTOs

/// <summary>
/// DTO for geographic catalog reload statistics
/// </summary>
public record GeographicCatalogStatsDto
{
    public string Message { get; init; } = string.Empty;
    public int ZonasCount { get; init; }
    public int RegionesCount { get; init; }
    public int SectoresCount { get; init; }
    public int CuadrantesCount { get; init; }
}

#endregion
