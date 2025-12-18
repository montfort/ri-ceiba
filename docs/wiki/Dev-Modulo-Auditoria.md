# Módulo de Auditoría

El módulo de auditoría registra todas las operaciones críticas del sistema.

## Estructura

```
src/
├── Ceiba.Core/
│   └── Entities/
│       └── RegistroAuditoria.cs
├── Ceiba.Application/
│   └── Services/
│       └── AuditService.cs
└── Ceiba.Web/
    └── Components/Pages/Admin/
        └── AuditLogViewer.razor
```

## Entidad

```csharp
public class RegistroAuditoria
{
    public long Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Accion { get; set; }      // LOGIN, CREATE, UPDATE, DELETE, etc.
    public string Entidad { get; set; }     // Usuario, Reporte, Zona, etc.
    public string? EntidadId { get; set; }  // ID del elemento afectado
    public Guid? UsuarioId { get; set; }    // Quién realizó la acción
    public string? UsuarioEmail { get; set; }
    public string? Detalles { get; set; }   // JSON con información adicional
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
```

## Servicio de Auditoría

```csharp
public class AuditService : IAuditService
{
    private readonly CeibaDbContext _context;
    private readonly IHttpContextAccessor _httpContext;

    public async Task LogAsync(
        string action,
        string entity,
        string? entityId,
        Guid? userId,
        object? details = null)
    {
        var httpContext = _httpContext.HttpContext;

        var registro = new RegistroAuditoria
        {
            Accion = action,
            Entidad = entity,
            EntidadId = entityId,
            UsuarioId = userId,
            UsuarioEmail = httpContext?.User?.Identity?.Name,
            Detalles = details != null
                ? JsonSerializer.Serialize(details)
                : null,
            IpAddress = httpContext?.Connection?.RemoteIpAddress?.ToString(),
            UserAgent = httpContext?.Request?.Headers["User-Agent"].FirstOrDefault()
        };

        _context.Auditoria.Add(registro);
        await _context.SaveChangesAsync();
    }

    public async Task<PagedResult<AuditLogDto>> GetLogsAsync(AuditFilterDto filter)
    {
        var query = _context.Auditoria.AsQueryable();

        // Aplicar filtros
        if (filter.FechaDesde.HasValue)
            query = query.Where(a => a.Timestamp >= filter.FechaDesde);

        if (filter.FechaHasta.HasValue)
            query = query.Where(a => a.Timestamp <= filter.FechaHasta);

        if (!string.IsNullOrEmpty(filter.Accion))
            query = query.Where(a => a.Accion == filter.Accion);

        if (!string.IsNullOrEmpty(filter.Entidad))
            query = query.Where(a => a.Entidad == filter.Entidad);

        if (filter.UsuarioId.HasValue)
            query = query.Where(a => a.UsuarioId == filter.UsuarioId);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedResult<AuditLogDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = total
        };
    }
}
```

## Tipos de Acciones

| Acción | Descripción |
|--------|-------------|
| `LOGIN` | Inicio de sesión exitoso |
| `LOGIN_FAILED` | Intento fallido de login |
| `LOGOUT` | Cierre de sesión |
| `CREATE` | Creación de elemento |
| `UPDATE` | Modificación de elemento |
| `DELETE` | Eliminación de elemento |
| `SUBMIT` | Entrega de reporte |
| `EXPORT` | Exportación de datos |
| `SUSPEND` | Suspensión de usuario |
| `ACTIVATE` | Activación de usuario |

## Uso en Servicios

```csharp
public async Task<ReportDto> CreateReportAsync(CreateReportDto dto, Guid userId)
{
    var report = MapToEntity(dto);
    _context.Reports.Add(report);
    await _context.SaveChangesAsync();

    // Registrar en auditoría
    await _auditService.LogAsync(
        "CREATE",
        "Reporte",
        report.Id.ToString(),
        userId,
        new { Delito = report.Delito, Zona = report.ZonaId });

    return MapToDto(report);
}

public async Task<ReportDto> UpdateReportAsync(int id, UpdateReportDto dto, Guid userId)
{
    var report = await _context.Reports.FindAsync(id);
    var oldValues = new { report.Delito, report.Estado };

    UpdateEntity(report, dto);
    await _context.SaveChangesAsync();

    // Registrar cambios
    await _auditService.LogAsync(
        "UPDATE",
        "Reporte",
        id.ToString(),
        userId,
        new { Before = oldValues, After = new { dto.Delito, report.Estado } });

    return MapToDto(report);
}
```

## Retención de Datos

> **Política:** Los registros de auditoría se conservan **indefinidamente** y no pueden ser eliminados.

Esto cumple con:
- Requisitos legales de trazabilidad
- Políticas de seguridad institucional
- Necesidades de investigación

## Próximos Pasos

- [Módulo de Catálogos](Dev-Modulo-Catalogos)
- [Uso del visor de auditoría](Usuario-Admin-Auditoria)
