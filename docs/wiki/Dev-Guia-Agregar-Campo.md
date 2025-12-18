# Guía: Agregar un Campo al Reporte

Esta guía te muestra cómo agregar un nuevo campo al formulario de reportes de incidencia.

## Pasos Generales

1. Agregar la propiedad a la entidad
2. Crear migración de base de datos
3. Agregar al DTO
4. Modificar el servicio
5. Actualizar el formulario Blazor
6. Agregar tests

## Ejemplo: Agregar Campo "Nacionalidad"

### Paso 1: Modificar la Entidad

**Archivo:** `src/Ceiba.Core/Entities/ReporteIncidencia.cs`

```csharp
public class ReporteIncidencia
{
    // ... propiedades existentes ...

    // NUEVO: Nacionalidad de la persona atendida
    public string? Nacionalidad { get; set; }
}
```

### Paso 2: Crear Migración

```bash
cd src/Ceiba.Infrastructure
dotnet ef migrations add AddNacionalidadToReporte --startup-project ../Ceiba.Web
dotnet ef database update --startup-project ../Ceiba.Web
```

### Paso 3: Agregar al DTO

**Archivo:** `src/Ceiba.Shared/DTOs/CreateReportDto.cs`

```csharp
public class CreateReportDto
{
    // ... propiedades existentes ...

    [MaxLength(100, ErrorMessage = "Nacionalidad máximo 100 caracteres")]
    public string? Nacionalidad { get; set; }
}
```

**Archivo:** `src/Ceiba.Shared/DTOs/UpdateReportDto.cs`

```csharp
public class UpdateReportDto
{
    // ... propiedades existentes ...

    [MaxLength(100)]
    public string? Nacionalidad { get; set; }
}
```

**Archivo:** `src/Ceiba.Shared/DTOs/ReportDto.cs`

```csharp
public class ReportDto
{
    // ... propiedades existentes ...

    public string? Nacionalidad { get; set; }
}
```

### Paso 4: Modificar el Servicio

**Archivo:** `src/Ceiba.Application/Services/ReportService.cs`

En el método `CreateReportAsync`:

```csharp
var report = new ReporteIncidencia
{
    // ... mapeo existente ...
    Nacionalidad = dto.Nacionalidad
};
```

En el método `UpdateReportAsync`:

```csharp
report.Nacionalidad = dto.Nacionalidad;
```

En el método `MapToDto`:

```csharp
private ReportDto MapToDto(ReporteIncidencia report)
{
    return new ReportDto
    {
        // ... mapeo existente ...
        Nacionalidad = report.Nacionalidad
    };
}
```

### Paso 5: Actualizar el Formulario

**Archivo:** `src/Ceiba.Web/Components/Pages/Reports/ReportForm.razor`

Agregar el campo en la sección apropiada:

```razor
<!-- En la sección de datos de la víctima -->
<div class="row mb-3">
    <div class="col-md-4">
        <label for="nacionalidad" class="form-label">Nacionalidad</label>
        <InputText id="nacionalidad"
                   class="form-control"
                   @bind-Value="Model.Nacionalidad"
                   placeholder="Ej: Mexicana" />
        <ValidationMessage For="@(() => Model.Nacionalidad)" />
    </div>
</div>
```

### Paso 6: Agregar Tests

**Archivo:** `tests/Ceiba.Core.Tests/ReporteIncidenciaTests.cs`

```csharp
[Fact]
public void CanSetNacionalidad()
{
    var reporte = new ReporteIncidencia();

    reporte.Nacionalidad = "Mexicana";

    Assert.Equal("Mexicana", reporte.Nacionalidad);
}
```

**Archivo:** `tests/Ceiba.Application.Tests/ReportServiceTests.cs`

```csharp
[Fact]
public async Task CreateReport_WithNacionalidad_StoresValue()
{
    var dto = new CreateReportDto
    {
        // ... campos requeridos ...
        Nacionalidad = "Mexicana"
    };

    var result = await _service.CreateReportAsync(dto, _userId);

    Assert.Equal("Mexicana", result.Nacionalidad);
}
```

## Campo con Sugerencias

Si el nuevo campo debe tener autocompletado:

### 1. Agregar categoría de sugerencias

```sql
INSERT INTO sugerencia_reporte (categoria, valor, orden, activo)
VALUES
    ('nacionalidad', 'Mexicana', 1, true),
    ('nacionalidad', 'Estadounidense', 2, true),
    ('nacionalidad', 'Guatemalteca', 3, true);
```

### 2. Cargar sugerencias en el formulario

```csharp
@code {
    private List<string> NacionalidadSuggestions { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        // ... código existente ...
        NacionalidadSuggestions = await CatalogService.GetSuggestionsAsync("nacionalidad");
    }
}
```

### 3. Usar componente SuggestionInput

```razor
<SuggestionInput Id="nacionalidad"
                 Label="Nacionalidad"
                 Value="@Model.Nacionalidad"
                 ValueChanged="@OnNacionalidadChanged"
                 Suggestions="@NacionalidadSuggestions"
                 PlaceholderText="Ingrese la nacionalidad" />
```

## Actualizar Exportación (si aplica)

Si el campo debe aparecer en PDF y JSON:

**PDF:** Modificar `PdfExportService.cs`
**JSON:** El DTO ya se incluirá automáticamente

## Checklist Final

- [ ] Entidad modificada
- [ ] Migración creada y aplicada
- [ ] DTOs actualizados (Create, Update, Response)
- [ ] Servicio actualizado (mapeo)
- [ ] Formulario actualizado
- [ ] Tests escritos y pasando
- [ ] Exportación actualizada (si aplica)
- [ ] Documentación actualizada
