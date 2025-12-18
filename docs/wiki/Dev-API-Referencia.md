# API Reference

Esta documentación describe las interfaces y servicios principales del sistema Ceiba.

## Servicios de Aplicación

### IReportService

Gestiona el ciclo de vida de los reportes de incidencia.

```csharp
public interface IReportService
{
    /// <summary>
    /// Crea un nuevo reporte de incidencia.
    /// </summary>
    Task<ReportDto> CreateReportAsync(CreateReportDto dto, Guid userId);

    /// <summary>
    /// Obtiene un reporte por ID.
    /// </summary>
    Task<ReportDto> GetReportByIdAsync(int id, Guid userId, bool isRevisor);

    /// <summary>
    /// Lista reportes con filtros y paginación.
    /// </summary>
    Task<PagedResult<ReportDto>> ListReportsAsync(
        ReportFilterDto filter,
        Guid userId,
        bool isRevisor);

    /// <summary>
    /// Actualiza un reporte existente.
    /// </summary>
    Task<ReportDto> UpdateReportAsync(
        int id,
        UpdateReportDto dto,
        Guid userId,
        bool isRevisor);

    /// <summary>
    /// Entrega un reporte (cambia estado a Entregado).
    /// </summary>
    Task SubmitReportAsync(int id, Guid userId);
}
```

### IUserManagementService

Gestiona usuarios del sistema.

```csharp
public interface IUserManagementService
{
    Task<UserDto> CreateUserAsync(CreateUserDto dto, Guid adminId);
    Task<UserDto> GetUserByIdAsync(Guid id);
    Task<PagedResult<UserDto>> ListUsersAsync(UserFilterDto filter);
    Task<UserDto> UpdateUserAsync(Guid id, UpdateUserDto dto, Guid adminId);
    Task SuspendUserAsync(Guid id, Guid adminId);
    Task ActivateUserAsync(Guid id, Guid adminId);
    Task DeleteUserAsync(Guid id, Guid adminId);
    Task<List<string>> GetAvailableRolesAsync();
}
```

### ICatalogService

Gestiona catálogos geográficos y sugerencias.

```csharp
public interface ICatalogService
{
    // Zonas
    Task<List<CatalogItemDto>> GetZonasAsync();
    Task<CatalogItemDto> CreateZonaAsync(CreateCatalogDto dto);
    Task<CatalogItemDto> UpdateZonaAsync(int id, UpdateCatalogDto dto);
    Task DeleteZonaAsync(int id);

    // Regiones
    Task<List<CatalogItemDto>> GetRegionesByZonaAsync(int zonaId);
    Task<CatalogItemDto> CreateRegionAsync(CreateCatalogDto dto);

    // Sectores
    Task<List<CatalogItemDto>> GetSectoresByRegionAsync(int regionId);
    Task<CatalogItemDto> CreateSectorAsync(CreateCatalogDto dto);

    // Cuadrantes
    Task<List<CatalogItemDto>> GetCuadrantesBySectorAsync(int sectorId);
    Task<CatalogItemDto> CreateCuadranteAsync(CreateCatalogDto dto);

    // Sugerencias
    Task<List<string>> GetSuggestionsAsync(string category);
    Task<SuggestionDto> CreateSuggestionAsync(CreateSuggestionDto dto);
}
```

### IExportService

Exporta reportes a PDF y JSON.

```csharp
public interface IExportService
{
    /// <summary>
    /// Exporta reportes al formato especificado.
    /// </summary>
    Task<ExportResultDto> ExportReportsAsync(
        ExportRequestDto request,
        Guid userId,
        bool isRevisor);
}

public class ExportResultDto
{
    public byte[] Data { get; set; }
    public string FileName { get; set; }
    public string ContentType { get; set; }
    public int ReportCount { get; set; }
}
```

### IAuditService

Registra y consulta operaciones de auditoría.

```csharp
public interface IAuditService
{
    Task LogAsync(
        string action,
        string entity,
        string? entityId,
        Guid? userId,
        object? details = null);

    Task<PagedResult<AuditLogDto>> GetLogsAsync(AuditFilterDto filter);
}
```

### IEmailService

Envía correos electrónicos.

```csharp
public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody);
    Task SendWithAttachmentAsync(
        string to,
        string subject,
        string htmlBody,
        byte[] attachment,
        string attachmentName);
    Task<bool> TestConnectionAsync();
}
```

### IAiNarrativeService

Genera narrativas con IA.

```csharp
public interface IAiNarrativeService
{
    Task<string> GenerateNarrativeAsync(
        string prompt,
        IEnumerable<ReportDto> reports);

    Task<bool> TestConnectionAsync();
}
```

## DTOs Principales

### CreateReportDto

```csharp
public class CreateReportDto
{
    public string TipoReporte { get; set; } = "A";
    public DateTime DatetimeHechos { get; set; }

    // Víctima
    public string Sexo { get; set; }
    public int Edad { get; set; }
    public bool LgbtttiqPlus { get; set; }
    public bool SituacionCalle { get; set; }
    public bool Migrante { get; set; }
    public bool Discapacidad { get; set; }

    // Ubicación
    public int ZonaId { get; set; }
    public int RegionId { get; set; }
    public int SectorId { get; set; }
    public int CuadranteId { get; set; }

    // Incidencia
    public string Delito { get; set; }
    public string TipoDeAtencion { get; set; }

    // Operativo
    public string TurnoCeiba { get; set; }
    public string TipoDeAccion { get; set; }
    public string Traslados { get; set; }

    // Narrativa
    public string HechosReportados { get; set; }
    public string AccionesRealizadas { get; set; }
    public string? Observaciones { get; set; }
}
```

### ReportFilterDto

```csharp
public class ReportFilterDto
{
    public int? Estado { get; set; }
    public string? Delito { get; set; }
    public int? ZonaId { get; set; }
    public Guid? CreadorId { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }

    // Paginación
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; }
    public bool SortDesc { get; set; }
}
```

### PagedResult<T>

```csharp
public class PagedResult<T>
{
    public List<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
```

## Excepciones

```csharp
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}

public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
    public Dictionary<string, string[]> Errors { get; set; }
}
```

## Próximos Pasos

- [[Dev-Modulo-Reportes|Módulo de Reportes]]
- [[Dev-Modulo-Exportacion|Módulo de Exportación]]
- [[Dev-Arquitectura|Arquitectura]]
