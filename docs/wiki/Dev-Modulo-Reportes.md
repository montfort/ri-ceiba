# Módulo de Reportes

El módulo de reportes gestiona el ciclo de vida completo de los reportes de incidencias.

## Estructura

```
src/
├── Ceiba.Core/
│   └── Entities/
│       └── ReporteIncidencia.cs
├── Ceiba.Application/
│   └── Services/
│       └── ReportService.cs
├── Ceiba.Shared/
│   └── DTOs/
│       ├── ReportDto.cs
│       ├── CreateReportDto.cs
│       └── UpdateReportDto.cs
└── Ceiba.Web/
    └── Components/Pages/Reports/
        ├── ReportForm.razor
        ├── ReportList.razor
        └── ReportView.razor
```

## Estados del Reporte

| Estado | Valor | Descripción |
|--------|-------|-------------|
| Borrador | 0 | En edición, no entregado |
| Entregado | 1 | Enviado para revisión |

### Transiciones de Estado

```
Borrador (0) ──[Submit]──► Entregado (1)
```

## Servicio de Reportes

### Crear Reporte

```csharp
public async Task<ReportDto> CreateReportAsync(CreateReportDto dto, Guid userId)
{
    // Validar
    ValidateReportData(dto);

    // Crear entidad
    var report = new ReporteIncidencia
    {
        TipoReporte = dto.TipoReporte,
        DatetimeHechos = dto.DatetimeHechos.ToUniversalTime(),
        Estado = 0, // Borrador
        CreadorId = userId,
        // ... mapear campos
    };

    _context.Reports.Add(report);
    await _context.SaveChangesAsync();

    // Auditar
    await _auditService.LogAsync("CREATE", "Reporte", report.Id.ToString(), userId);

    return MapToDto(report);
}
```

### Listar Reportes

```csharp
public async Task<PagedResult<ReportDto>> ListReportsAsync(
    ReportFilterDto filter,
    Guid userId,
    bool isRevisor)
{
    var query = _context.Reports
        .Include(r => r.Zona)
        .Include(r => r.Creador)
        .AsQueryable();

    // CREADOR solo ve sus reportes
    if (!isRevisor)
    {
        query = query.Where(r => r.CreadorId == userId);
    }

    // Aplicar filtros
    if (filter.Estado.HasValue)
        query = query.Where(r => r.Estado == filter.Estado);

    if (!string.IsNullOrEmpty(filter.Delito))
        query = query.Where(r => r.Delito.Contains(filter.Delito));

    // Ordenar y paginar
    var total = await query.CountAsync();
    var items = await query
        .OrderByDescending(r => r.CreatedAt)
        .Skip((filter.Page - 1) * filter.PageSize)
        .Take(filter.PageSize)
        .ToListAsync();

    return new PagedResult<ReportDto>
    {
        Items = items.Select(MapToDto).ToList(),
        TotalCount = total,
        Page = filter.Page,
        PageSize = filter.PageSize
    };
}
```

### Entregar Reporte

```csharp
public async Task SubmitReportAsync(int id, Guid userId)
{
    var report = await _context.Reports.FindAsync(id);

    if (report == null)
        throw new NotFoundException("Reporte no encontrado");

    if (report.CreadorId != userId)
        throw new ForbiddenException("No tienes permiso");

    if (report.Estado != 0)
        throw new ValidationException("El reporte ya fue entregado");

    report.Estado = 1;
    report.UpdatedAt = DateTime.UtcNow;

    await _context.SaveChangesAsync();
    await _auditService.LogAsync("SUBMIT", "Reporte", id.ToString(), userId);
}
```

## Componente de Formulario

### ReportForm.razor

```razor
@page "/reports/new"
@page "/reports/edit/{ReportId:int}"

<EditForm Model="@Model" OnValidSubmit="HandleValidSubmit">
    <DataAnnotationsValidator />

    <!-- Campos del formulario -->
    <div class="mb-3">
        <label>Fecha y Hora de los Hechos</label>
        <InputDate @bind-Value="Model.DatetimeHechos" Type="InputDateType.DateTimeLocal" />
    </div>

    <!-- Ubicación en cascada -->
    <CascadingSelect Id="zona" @bind-Value="Model.ZonaId" Items="@Zonas" />
    <CascadingSelect Id="region" @bind-Value="Model.RegionId" Items="@Regiones" />

    <!-- Acciones -->
    <button type="submit">Guardar Borrador</button>
    @if (IsEditMode && Model.Estado == 0)
    {
        <button type="button" @onclick="HandleSubmit">Guardar y Entregar</button>
    }
</EditForm>
```

## Validación

### Validación de DTOs

```csharp
public class CreateReportDto
{
    [Required(ErrorMessage = "El delito es requerido")]
    [MaxLength(200)]
    public string Delito { get; set; }

    [Required]
    [Range(1, 150, ErrorMessage = "Edad debe ser entre 1 y 150")]
    public int Edad { get; set; }

    [Required]
    [MinLength(10, ErrorMessage = "Mínimo 10 caracteres")]
    [MaxLength(10000)]
    public string HechosReportados { get; set; }
}
```

## Próximos Pasos

- [Módulo de Exportación](Dev-Modulo-Exportacion)
- [Agregar un campo al reporte](Dev-Guia-Agregar-Campo)
