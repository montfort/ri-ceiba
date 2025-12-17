# Refactorización del Campo "Tipo de Acción"

**Fecha:** 2024-12-16
**Versión:** 1.0
**Estado:** Completado

## Resumen

Se refactorizó el campo "Tipo de Acción" (`TipoDeAccion`) del formulario de reportes de incidencias, cambiando de un campo numérico con valores predefinidos (combobox) a un campo de texto libre editable.

## Motivación

El campo original estaba implementado como un `short` con valores 1, 2, 3 representando opciones fijas ("Preventiva", "Reactiva", "Seguimiento"). Esta implementación era demasiado restrictiva y no permitía a los usuarios describir adecuadamente el tipo de acción realizada.

## Cambios Realizados

### 1. Modelo de Datos

#### Entidad `ReporteIncidencia.cs`
```csharp
// Antes
public short TipoDeAccion { get; set; }

// Después
public string TipoDeAccion { get; set; } = string.Empty;
```

#### Configuración de Base de Datos
- Se eliminó la restricción check `CK_REPORTE_TIPO_ACCION`
- Se cambió el tipo de columna de `smallint` a `character varying(500)`

### 2. DTOs

#### `CreateReportDto`
```csharp
// Antes
[Range(1, 3)]
public int TipoDeAccion { get; set; }

// Después
[Required]
[StringLength(500)]
public string TipoDeAccion { get; set; } = string.Empty;
```

#### `UpdateReportDto`
```csharp
// Antes
[Range(1, 3)]
public int? TipoDeAccion { get; set; }

// Después
[StringLength(500)]
public string? TipoDeAccion { get; set; }
```

#### `ReportDto`
```csharp
// Antes
public int TipoDeAccion { get; set; }

// Después
public string TipoDeAccion { get; set; } = string.Empty;
```

### 3. Validadores FluentValidation

#### `CreateReportDtoValidator`
```csharp
// Antes
RuleFor(x => x.TipoDeAccion)
    .InclusiveBetween(1, 3);

// Después
RuleFor(x => x.TipoDeAccion)
    .NotEmpty()
    .MaximumLength(500);
```

#### `UpdateReportDtoValidator`
```csharp
// Antes
RuleFor(x => x.TipoDeAccion)
    .InclusiveBetween(1, 3)
    .When(x => x.TipoDeAccion.HasValue);

// Después
RuleFor(x => x.TipoDeAccion)
    .MaximumLength(500)
    .When(x => !string.IsNullOrWhiteSpace(x.TipoDeAccion));
```

### 4. Interfaz de Usuario

#### `ReportForm.razor`
```html
<!-- Antes -->
<InputSelect @bind-Value="Model.TipoDeAccion">
    <option value="0">Seleccione una acción</option>
    <option value="1">Preventiva</option>
    <option value="2">Reactiva</option>
    <option value="3">Seguimiento</option>
</InputSelect>

<!-- Después -->
<InputTextArea @bind-Value="Model.TipoDeAccion" rows="3" />
<small class="form-text text-muted">Máximo 500 caracteres</small>
```

#### `ReportView.razor`
```html
<!-- Antes -->
<p><strong>Tipo de Accion:</strong> @GetTipoAccionText(Report.TipoDeAccion)</p>

<!-- Después -->
<p><strong>Tipo de Accion:</strong> @Report.TipoDeAccion</p>
```

### 5. Servicios

#### `ReportService.cs`
- Se eliminaron los casts `(short)` al asignar `TipoDeAccion`
- Se cambió la verificación de `HasValue` a comparación con `null` para strings

#### `ExportService.cs`
- Se eliminó el método `MapTipoDeAccion()` que convertía valores numéricos a texto
- El valor string se pasa directamente al DTO de exportación

#### `AutomatedReportService.cs`
- Se modificó el agrupamiento estadístico para usar el string directamente:
```csharp
// Antes
stats.PorTipoAccion = reports
    .GroupBy(r => r.TipoDeAccion switch { 1 => "ATOS", 2 => "Capacitación", ... })
    .ToDictionary(...);

// Después
stats.PorTipoAccion = reports
    .GroupBy(r => string.IsNullOrWhiteSpace(r.TipoDeAccion) ? "Sin especificar" : r.TipoDeAccion)
    .ToDictionary(...);
```

### 6. Configuración de Sugerencias

#### `AdminDTOs.cs` - `SugerenciaCampos`
- Se eliminó `TipoDeAccion` de la lista de campos con sugerencias configurables
- `SugerenciaCampos.All` ahora contiene 5 elementos (antes 6)
- `SugerenciaCampos.DetallesOperativos` ahora contiene 2 elementos (antes 3)

## Migración de Base de Datos

Se generó la migración `ChangeTipoDeAccionToString`:

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropCheckConstraint(
        name: "CK_REPORTE_TIPO_ACCION",
        table: "REPORTE_INCIDENCIA");

    migrationBuilder.AlterColumn<string>(
        name: "tipo_de_accion",
        table: "REPORTE_INCIDENCIA",
        type: "character varying(500)",
        maxLength: 500,
        nullable: false,
        oldClrType: typeof(short),
        oldType: "smallint");
}
```

### Aplicar la Migración

```bash
cd src/Ceiba.Infrastructure
dotnet ef database update --startup-project ../Ceiba.Web
```

### Migración de Datos Existentes (si aplica)

Si ya existen reportes con valores numéricos, ejecutar antes de la migración:

```sql
-- Convertir valores numéricos existentes a texto
UPDATE "REPORTE_INCIDENCIA"
SET tipo_de_accion = CASE
    WHEN tipo_de_accion::text = '1' THEN 'Preventiva'
    WHEN tipo_de_accion::text = '2' THEN 'Reactiva'
    WHEN tipo_de_accion::text = '3' THEN 'Seguimiento'
    ELSE 'Sin especificar'
END
WHERE tipo_de_accion IS NOT NULL;
```

## Archivos Modificados

### Código Fuente
| Archivo | Cambio |
|---------|--------|
| `src/Ceiba.Core/Entities/ReporteIncidencia.cs` | Tipo de propiedad y validación |
| `src/Ceiba.Shared/DTOs/ReportDTOs.cs` | Tipo en DTOs |
| `src/Ceiba.Shared/DTOs/AdminDTOs.cs` | Eliminado de sugerencias |
| `src/Ceiba.Application/Validators/ReportValidators.cs` | Reglas de validación |
| `src/Ceiba.Application/Services/ReportService.cs` | Mapeo y asignación |
| `src/Ceiba.Application/Services/Export/ExportService.cs` | Eliminado mapeo |
| `src/Ceiba.Infrastructure/Data/Configurations/ReporteIncidenciaConfiguration.cs` | Config de columna |
| `src/Ceiba.Infrastructure/Services/AutomatedReportService.cs` | Agrupamiento |
| `src/Ceiba.Web/Components/Pages/Reports/ReportForm.razor` | UI del formulario |
| `src/Ceiba.Web/Components/Pages/Reports/ReportView.razor` | Vista de reporte |

### Tests Actualizados
| Proyecto | Archivos |
|----------|----------|
| `Ceiba.Core.Tests` | `ReporteIncidenciaTests.cs`, `DTOs/ReportDTOsTests.cs`, `DTOs/AdminDTOsTests.cs` |
| `Ceiba.Application.Tests` | `ReportServiceTests.cs`, `Services/Export/ExportServiceTests.cs`, `Services/AutomatedReportServiceTests.cs`, `Validators/*.cs` |
| `Ceiba.Infrastructure.Tests` | `Repositories/ReportRepositoryTests.cs`, `Services/AutomatedReportServiceTests.cs` |
| `Ceiba.Web.Tests` | `Components/ReportFormTests.cs`, `Components/ReportViewTests.cs`, `Components/ReportListTests.cs`, `Controllers/ReportsControllerTests.cs` |
| `Ceiba.Integration.Tests` | `InputValidationTests.cs`, `AuthorizationMatrixTests.cs`, `AutomatedReportJobTests.cs` |

## Compatibilidad

- **Breaking Change**: Sí, para cualquier integración que espere valores numéricos
- **API**: Los endpoints ahora esperan/retornan strings en lugar de enteros
- **Base de Datos**: Requiere migración y posible conversión de datos existentes

## Verificación

```bash
# Compilar el proyecto
dotnet build

# Ejecutar tests
dotnet test --filter "Category!=E2E"
```

## Notas Adicionales

1. El campo ahora permite hasta 500 caracteres de texto libre
2. Los reportes automatizados agrupan por el texto exacto ingresado
3. Las exportaciones PDF/JSON muestran el texto tal como fue ingresado
4. No hay valores predefinidos; el usuario tiene libertad total de entrada
