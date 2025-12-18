# Estándares de Código

Esta guía define las convenciones y estándares de código para el proyecto Ceiba.

## Convenciones de Nombrado

### C# General

| Elemento | Convención | Ejemplo |
|----------|------------|---------|
| Clases | PascalCase | `ReporteIncidencia` |
| Interfaces | IPascalCase | `IReportService` |
| Métodos | PascalCase | `GetReportById()` |
| Propiedades | PascalCase | `FechaCreacion` |
| Parámetros | camelCase | `reportId` |
| Variables locales | camelCase | `reportCount` |
| Campos privados | _camelCase | `_logger` |
| Constantes | UPPER_SNAKE | `MAX_PAGE_SIZE` |
| Async methods | ...Async | `GetReportByIdAsync()` |

### Ejemplos

```csharp
public class ReportService : IReportService
{
    private readonly ILogger<ReportService> _logger;
    private const int MAX_PAGE_SIZE = 100;

    public async Task<ReportDto> GetReportByIdAsync(int reportId)
    {
        var report = await _context.Reports.FindAsync(reportId);
        return MapToDto(report);
    }
}
```

## Estructura de Archivos

### Una Clase por Archivo

```
❌ Malo: Entities.cs (contiene múltiples clases)
✅ Bueno: Usuario.cs, ReporteIncidencia.cs
```

### Organización de Namespaces

```csharp
namespace Ceiba.Core.Entities;
namespace Ceiba.Application.Services;
namespace Ceiba.Infrastructure.Data;
namespace Ceiba.Web.Components.Pages.Reports;
```

## Documentación de Código

### Cuándo Documentar

- Métodos públicos de interfaces
- Clases y entidades del dominio
- Lógica compleja que no es obvia
- Parámetros con restricciones específicas

### XML Comments

```csharp
/// <summary>
/// Crea un nuevo reporte de incidencia.
/// </summary>
/// <param name="dto">Datos del reporte.</param>
/// <param name="userId">ID del usuario creador.</param>
/// <returns>El reporte creado.</returns>
/// <exception cref="ValidationException">Si los datos son inválidos.</exception>
public async Task<ReportDto> CreateReportAsync(CreateReportDto dto, Guid userId)
```

### No Documentar

- Código que se explica solo
- Getters/Setters triviales
- Implementaciones obvias

## Patrones de Código

### Inyección de Dependencias

```csharp
// ✅ Correcto: Inyección por constructor
public class ReportService : IReportService
{
    private readonly CeibaDbContext _context;
    private readonly ILogger<ReportService> _logger;

    public ReportService(CeibaDbContext context, ILogger<ReportService> logger)
    {
        _context = context;
        _logger = logger;
    }
}

// ❌ Incorrecto: Service Locator
public class ReportService
{
    public void DoSomething()
    {
        var context = ServiceLocator.Get<CeibaDbContext>();
    }
}
```

### Manejo de Errores

```csharp
// ✅ Correcto: Excepciones específicas
public async Task<ReportDto> GetReportByIdAsync(int id)
{
    var report = await _context.Reports.FindAsync(id);
    if (report == null)
        throw new NotFoundException($"Report {id} not found");

    return MapToDto(report);
}

// ❌ Incorrecto: Retornar null o valores mágicos
public async Task<ReportDto?> GetReportByIdAsync(int id)
{
    var report = await _context.Reports.FindAsync(id);
    return report == null ? null : MapToDto(report); // Ambiguo
}
```

### Async/Await

```csharp
// ✅ Correcto: Async todo el camino
public async Task<List<ReportDto>> GetReportsAsync()
{
    var reports = await _context.Reports.ToListAsync();
    return reports.Select(MapToDto).ToList();
}

// ❌ Incorrecto: Bloquear con .Result
public List<ReportDto> GetReports()
{
    var reports = _context.Reports.ToListAsync().Result; // Deadlock risk
    return reports.Select(MapToDto).ToList();
}
```

## Blazor Components

### Estructura de Componentes

```razor
@* 1. Directivas de página *@
@page "/reports"

@* 2. Using statements *@
@using Ceiba.Core.Interfaces

@* 3. Inyección de dependencias *@
@inject IReportService ReportService

@* 4. Atributos *@
@attribute [Authorize(Roles = "CREADOR")]

@* 5. Markup HTML *@
<PageTitle>Mis Reportes</PageTitle>

<div class="container">
    @* contenido *@
</div>

@* 6. Bloque de código *@
@code {
    // Propiedades
    private List<ReportDto> Reports { get; set; } = new();

    // Lifecycle
    protected override async Task OnInitializedAsync()
    {
        await LoadReportsAsync();
    }

    // Métodos privados
    private async Task LoadReportsAsync()
    {
        Reports = await ReportService.GetReportsAsync();
    }
}
```

### Nombrado de Componentes

| Tipo | Convención | Ejemplo |
|------|------------|---------|
| Páginas | `VerbNoun.razor` | `ReportList.razor`, `UserForm.razor` |
| Componentes | `NounDescriptor.razor` | `CascadingSelect.razor` |
| Layouts | `NameLayout.razor` | `MainLayout.razor` |

## Entity Framework

### Consultas Eficientes

```csharp
// ✅ Correcto: Proyección
var dtos = await _context.Reports
    .Where(r => r.CreadorId == userId)
    .Select(r => new ReportDto
    {
        Id = r.Id,
        Delito = r.Delito
    })
    .ToListAsync();

// ❌ Incorrecto: Cargar todo
var reports = await _context.Reports.ToListAsync();
var filtered = reports.Where(r => r.CreadorId == userId);
```

### Tracking

```csharp
// ✅ Para consultas de solo lectura
var report = await _context.Reports
    .AsNoTracking()
    .FirstOrDefaultAsync(r => r.Id == id);
```

## Formateo

### Usar `dotnet format`

```bash
dotnet format
```

### Configuración de EditorConfig

El proyecto incluye `.editorconfig` con reglas predefinidas.

## Code Review Checklist

- [ ] El código sigue las convenciones de nombrado
- [ ] Los métodos async terminan en Async
- [ ] Las dependencias se inyectan por constructor
- [ ] Los errores usan excepciones apropiadas
- [ ] Los tests cubren la funcionalidad
- [ ] No hay código comentado
- [ ] No hay console.log o WriteLine de debug

## Próximos Pasos

- [[Dev Testing TDD|Guía de testing TDD]]
- [[Dev Arquitectura|Arquitectura del sistema]]
- [[Dev Guia Componentes Blazor|Componentes Blazor]]
