# Carga de Catálogos Geográficos desde regiones.json

Este documento describe el sistema implementado para cargar los catálogos geográficos (Zonas, Regiones, Sectores, Cuadrantes) desde el archivo `regiones.json`.

## Arquitectura

### Jerarquía Geográfica

```
Zona → Región → Sector → Cuadrante
```

- **Zona**: Nivel más alto (Sur, Norte, Centro, Oriente, Poniente)
- **Región**: Numeradas dentro de cada zona (Región 1, Región 2, etc.)
- **Sector**: Nombres descriptivos (Portales, Nativitas, Chapultepec, etc.)
- **Cuadrante**: Numerados dentro de cada sector (1, 2, 3, ...)

## Componentes Implementados

### 1. RegionDataLoader

**Archivo**: `src/Ceiba.Infrastructure/Data/RegionDataLoader.cs`

Servicio que maneja la carga de datos geográficos desde JSON.

```csharp
// Cargar datos desde JSON
var zonaData = await regionLoader.LoadFromJsonAsync(jsonPath);

// Insertar en base de datos (clearExisting: true para reemplazar)
await regionLoader.SeedGeographicCatalogsAsync(zonaData, adminUserId, clearExisting: true);

// Obtener estadísticas actuales
var stats = await regionLoader.GetCurrentStatsAsync();
```

**Métodos disponibles**:
- `LoadFromJsonAsync(string jsonPath)` - Lee y deserializa el archivo JSON
- `SeedGeographicCatalogsAsync(...)` - Inserta los datos en la base de datos
- `ClearGeographicCatalogsAsync()` - Elimina todos los catálogos geográficos
- `GetCurrentStatsAsync()` - Retorna conteos de cada tabla

### 2. SeedDataService (Actualizado)

**Archivo**: `src/Ceiba.Infrastructure/Data/SeedDataService.cs`

Ahora utiliza `regiones.json` para el seed inicial de la aplicación.

```csharp
// Para recargar catálogos manualmente
await seedDataService.ReloadGeographicCatalogsAsync();
```

**Búsqueda del archivo JSON** (en orden de prioridad):
1. Variable de entorno `CEIBA_REGIONES_JSON_PATH`
2. `{AppBaseDirectory}/SeedData/regiones.json`
3. `{AppBaseDirectory}/regiones.json`
4. `{CurrentDirectory}/docs/regiones.json`

### 3. Endpoints API

**Controlador**: `AdminController` (requiere rol ADMIN)

#### Obtener estadísticas
```http
GET /api/admin/catalogs/geographic-stats
```

**Respuesta**:
```json
{
  "message": "Estadísticas actuales de catálogos geográficos",
  "zonasCount": 5,
  "regionesCount": 16,
  "sectoresCount": 72,
  "cuadrantesCount": 1015
}
```

#### Recargar catálogos
```http
POST /api/admin/catalogs/reload-geographic
```

**Respuesta**:
```json
{
  "message": "Catálogos geográficos recargados exitosamente desde regiones.json",
  "zonasCount": 5,
  "regionesCount": 16,
  "sectoresCount": 72,
  "cuadrantesCount": 1015
}
```

> **Advertencia**: Este endpoint elimina TODOS los datos geográficos existentes y los reemplaza con los del JSON.

### 4. Herramienta CLI

**Ubicación**: `tools/ReloadGeographicCatalogs/`

Herramienta de línea de comandos para recargar catálogos sin necesidad de la aplicación web.

#### Uso

```bash
# Con cadena de conexión como argumento
dotnet run --project tools/ReloadGeographicCatalogs -- \
  "Host=localhost;Port=5432;Database=ceiba;Username=postgres;Password=xxx"

# Con variable de entorno
export ConnectionStrings__DefaultConnection="Host=localhost;..."
dotnet run --project tools/ReloadGeographicCatalogs
```

#### Opciones
- **Argumento 1**: Cadena de conexión a PostgreSQL
- **Argumento 2**: Ruta al archivo `regiones.json` (opcional)

#### Ejemplo de salida
```
=== Ceiba - Geographic Catalogs Reload Tool ===

Connection: Host=localhost;Port=32768;Database=ceiba;Username=postgres;Password=****
JSON Path:  /path/to/docs/regiones.json

Checking database connection...
Database connection OK.

Current data: Zonas: 5, Regiones: 12, Sectores: 30, Cuadrantes: 120

WARNING: Existing geographic data will be DELETED and replaced!
Continue? (y/N): y

Loading data from JSON...
Clearing existing data and inserting new records...

Final data:  Zonas: 5, Regiones: 16, Sectores: 72, Cuadrantes: 1015

Geographic catalogs reloaded successfully!
```

## Estructura del archivo regiones.json

```json
[
  {
    "nombre_zona": "Sur",
    "regiones": [
      {
        "numero_region": 1,
        "sectores": [
          {
            "nombre_sector": "Portales",
            "cuadrantes": [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11]
          },
          {
            "nombre_sector": "Nativitas",
            "cuadrantes": [1, 2, 3, 4, 5, 6, 7, 8, 9]
          }
        ]
      }
    ]
  }
]
```

## Datos Actuales

| Catálogo | Cantidad |
|----------|----------|
| Zonas | 5 |
| Regiones | 16 |
| Sectores | 72 |
| Cuadrantes | 1,015 |

### Distribución por Zona

| Zona | Regiones | Sectores |
|------|----------|----------|
| Sur | 4 | 17 |
| Norte | 3 | 17 |
| Centro | 1 | 8 |
| Oriente | 4 | 15 |
| Poniente | 4 | 15 |

## Configuración para Despliegue

### Ubicación del archivo JSON

El archivo `regiones.json` debe estar disponible en una de las siguientes ubicaciones:

1. **Recomendado para producción**: Establecer la variable de entorno:
   ```bash
   export CEIBA_REGIONES_JSON_PATH=/path/to/regiones.json
   ```

2. **Incluido en el build**: El archivo se copia automáticamente a `SeedData/regiones.json` en el directorio de salida.

### Docker

Para contenedores Docker, montar el archivo o incluirlo en la imagen:

```dockerfile
COPY docs/regiones.json /app/SeedData/regiones.json
```

O usar volumen:
```yaml
volumes:
  - ./docs/regiones.json:/app/SeedData/regiones.json:ro
```

## Consideraciones de Seguridad

- El endpoint de recarga requiere rol **ADMIN**
- Se registra en logs cuando un administrador inicia la recarga
- Los reportes existentes mantienen sus referencias geográficas como NULL si se eliminan los catálogos

## Manejo de Errores

- Si `regiones.json` no existe durante el seed inicial, se omite el seed de catálogos geográficos (con advertencia en logs)
- Si hay reportes con referencias geográficas, estas se establecen a NULL antes de eliminar los catálogos
- Errores de conexión a base de datos se reportan claramente en la herramienta CLI
