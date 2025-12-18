# Módulo de Catálogos

El módulo de catálogos gestiona las listas configurables del sistema.

## Tipos de Catálogos

### Catálogos Geográficos

Estructura jerárquica de ubicaciones:
- **Zona** → Nivel más alto
- **Región** → Pertenece a una Zona
- **Sector** → Pertenece a una Región
- **Cuadrante** → Pertenece a un Sector

### Catálogos de Sugerencias

Listas de autocompletado para campos de formulario:
- `sexo`
- `delito`
- `tipo_de_atencion`
- `turno_ceiba`
- `traslados`

## Entidades

### Catálogos Geográficos

```csharp
public class Zona
{
    public int Id { get; set; }
    public string Nombre { get; set; }
    public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;
    public virtual ICollection<Region> Regiones { get; set; }
}

public class Region
{
    public int Id { get; set; }
    public string Nombre { get; set; }
    public int ZonaId { get; set; }
    public virtual Zona Zona { get; set; }
    public virtual ICollection<Sector> Sectores { get; set; }
}

public class Sector
{
    public int Id { get; set; }
    public string Nombre { get; set; }
    public int RegionId { get; set; }
    public virtual Region Region { get; set; }
    public virtual ICollection<Cuadrante> Cuadrantes { get; set; }
}

public class Cuadrante
{
    public int Id { get; set; }
    public string Nombre { get; set; }
    public int SectorId { get; set; }
    public virtual Sector Sector { get; set; }
}
```

### Sugerencias

```csharp
public class SugerenciaReporte
{
    public int Id { get; set; }
    public string Categoria { get; set; }  // sexo, delito, etc.
    public string Valor { get; set; }
    public int Orden { get; set; } = 0;
    public bool Activo { get; set; } = true;
}
```

## Servicio de Catálogos

```csharp
public class CatalogService : ICatalogService
{
    // Zonas
    public async Task<List<CatalogItemDto>> GetZonasAsync()
    {
        return await _context.Zonas
            .Where(z => z.Activo)
            .OrderBy(z => z.Nombre)
            .Select(z => new CatalogItemDto { Id = z.Id, Nombre = z.Nombre })
            .ToListAsync();
    }

    // Regiones filtradas por Zona
    public async Task<List<CatalogItemDto>> GetRegionesByZonaAsync(int zonaId)
    {
        return await _context.Regiones
            .Where(r => r.ZonaId == zonaId && r.Activo)
            .OrderBy(r => r.Nombre)
            .Select(r => new CatalogItemDto { Id = r.Id, Nombre = r.Nombre })
            .ToListAsync();
    }

    // Sugerencias por categoría
    public async Task<List<string>> GetSuggestionsAsync(string category)
    {
        return await _context.Sugerencias
            .Where(s => s.Categoria == category && s.Activo)
            .OrderBy(s => s.Orden)
            .ThenBy(s => s.Valor)
            .Select(s => s.Valor)
            .ToListAsync();
    }
}
```

## Componente de Selección en Cascada

```razor
<CascadingSelect Id="zona"
                 Label="Zona"
                 TValue="int"
                 Value="@Model.ZonaId"
                 ValueChanged="@OnZonaChanged"
                 Items="@Zonas"
                 Required="true"
                 PlaceholderText="Seleccione una zona" />

@code {
    private async Task OnZonaChanged(int value)
    {
        Model.ZonaId = value;
        Model.RegionId = 0;
        Model.SectorId = 0;
        Model.CuadranteId = 0;

        Regiones = await CatalogService.GetRegionesByZonaAsync(value);
        Sectores.Clear();
        Cuadrantes.Clear();
    }
}
```

## Componente de Sugerencias

```razor
<SuggestionInput Id="delito"
                 Label="Delito"
                 Value="@Model.Delito"
                 ValueChanged="@OnDelitoChanged"
                 Suggestions="@DelitoSuggestions"
                 Required="true"
                 PlaceholderText="Ingrese el tipo de delito" />
```

## Validación de Dependencias

### No se puede eliminar con dependencias

```csharp
public async Task DeleteZonaAsync(int id)
{
    var zona = await _context.Zonas
        .Include(z => z.Regiones)
        .FirstOrDefaultAsync(z => z.Id == id);

    if (zona.Regiones.Any())
    {
        throw new ValidationException(
            "No se puede eliminar una zona con regiones asociadas");
    }

    _context.Zonas.Remove(zona);
    await _context.SaveChangesAsync();
}
```

## Próximos Pasos

- [Reportes Automatizados](Dev-Modulo-Reportes-Automatizados)
- [Gestión de catálogos](Usuario-Admin-Catalogos-Geograficos)
