# Plan: Sistema de Configuración Inicial (Setup Wizard)

> **Estado**: Planificación
> **Prioridad**: Alta
> **Fecha**: 2025-12-12

## Resumen Ejecutivo

Este plan describe la implementación de un sistema de configuración inicial tipo WordPress para la aplicación Ceiba. El objetivo es proporcionar una experiencia de primera instalación limpia y segura, donde el administrador pueda crear su cuenta inicial sin exponer credenciales por defecto.

## Problema Actual

1. **Credenciales visibles en Login**: El archivo `Login.razor` (líneas 94-103) muestra hints con credenciales de prueba
2. **Usuarios de prueba en producción**: `SeedDataService.cs` crea usuarios con contraseñas conocidas
3. **Sin wizard de instalación**: No existe un flujo guiado para la primera configuración
4. **Datos de catálogo genéricos**: Los datos de Zona/Sector/Cuadrante son de prueba, no reales

## Solución Propuesta

### Arquitectura del Setup Wizard

```
Primera Instalación
        │
        ▼
┌───────────────────────┐
│  Detectar si es       │
│  primera ejecución    │
│  (sin usuarios admin) │
└───────────────────────┘
        │
        ▼ (Sin admin)
┌───────────────────────┐
│   /setup/welcome      │
│   Página de bienvenida│
└───────────────────────┘
        │
        ▼
┌───────────────────────┐
│   /setup/admin        │
│   Crear cuenta admin  │
└───────────────────────┘
        │
        ▼
┌───────────────────────┐
│   /setup/organization │
│   Datos organización  │
└───────────────────────┘
        │
        ▼
┌───────────────────────┐
│   /setup/catalogs     │
│   Importar catálogos  │
│   (Zona/Sector/Cuad.) │
└───────────────────────┘
        │
        ▼
┌───────────────────────┐
│   /setup/complete     │
│   Resumen y finalizar │
└───────────────────────┘
        │
        ▼
┌───────────────────────┐
│   Redirección a Login │
│   (sin hints visibles)│
└───────────────────────┘
```

## Fases de Implementación

### Fase 1: Detección de Primera Instalación

**Objetivo**: Detectar automáticamente si el sistema requiere configuración inicial.

**Archivos a crear/modificar**:
- `src/Ceiba.Application/Services/ISetupService.cs` (nuevo)
- `src/Ceiba.Infrastructure/Services/SetupService.cs` (nuevo)
- `src/Ceiba.Web/Middleware/SetupRedirectMiddleware.cs` (nuevo)

**Criterios de "primera instalación"**:
1. No existen usuarios con rol ADMIN en la base de datos
2. O existe un archivo marcador `.setup-required` en el directorio de datos
3. O variable de entorno `CEIBA_FORCE_SETUP=true`

**Código propuesto**:
```csharp
public interface ISetupService
{
    Task<bool> IsSetupRequiredAsync();
    Task<bool> IsSetupCompletedAsync();
    Task MarkSetupCompletedAsync();
    Task<SetupStatus> GetSetupStatusAsync();
}

public class SetupStatus
{
    public bool HasAdminUser { get; set; }
    public bool HasOrganizationConfig { get; set; }
    public bool HasGeographicCatalogs { get; set; }
    public bool IsComplete => HasAdminUser && HasOrganizationConfig && HasGeographicCatalogs;
}
```

### Fase 2: Páginas del Setup Wizard

**Objetivo**: Crear las páginas del asistente de configuración.

**Estructura de componentes**:
```
src/Ceiba.Web/Components/Pages/Setup/
├── SetupLayout.razor           # Layout especial para setup (sin nav)
├── Welcome.razor               # Página de bienvenida
├── CreateAdmin.razor           # Formulario crear admin
├── OrganizationConfig.razor    # Configuración de organización
├── ImportCatalogs.razor        # Importar Zona/Sector/Cuadrante
├── ReviewAndComplete.razor     # Revisión y finalización
└── _Imports.razor              # Imports del módulo
```

**Rutas**:
| Ruta | Componente | Descripción |
|------|------------|-------------|
| `/setup` | Welcome.razor | Bienvenida e instrucciones |
| `/setup/admin` | CreateAdmin.razor | Crear primera cuenta admin |
| `/setup/organization` | OrganizationConfig.razor | Nombre organización, logo |
| `/setup/catalogs` | ImportCatalogs.razor | Cargar catálogos geográficos |
| `/setup/complete` | ReviewAndComplete.razor | Resumen y finalizar |

### Fase 3: Formulario de Creación de Admin

**Campos requeridos**:
- Correo electrónico (será el username)
- Nombre completo
- Contraseña (mínimo 10 caracteres, mayúscula, número)
- Confirmar contraseña
- Checkbox: "Entiendo que estas credenciales son confidenciales"

**Validaciones**:
- Email válido y único
- Contraseña cumple política de seguridad
- Confirmación coincide

**Seguridad**:
- Formulario solo accesible si `IsSetupRequired() == true`
- Token CSRF obligatorio
- Rate limiting (máximo 5 intentos por IP)
- Log de auditoría del evento de creación

### Fase 4: Importación de Catálogos Geográficos

**Objetivo**: Permitir cargar datos reales de Zona/Sector/Cuadrante.

**Métodos de importación**:

#### Opción A: Archivo JSON
```json
{
  "zonas": [
    {
      "nombre": "Zona Norte",
      "codigo": "ZN",
      "sectores": [
        {
          "nombre": "Sector 1",
          "codigo": "S1",
          "cuadrantes": [
            { "nombre": "C1-A", "codigo": "C1A" },
            { "nombre": "C1-B", "codigo": "C1B" }
          ]
        }
      ]
    }
  ]
}
```

#### Opción B: Archivo CSV
```csv
zona_nombre,zona_codigo,sector_nombre,sector_codigo,cuadrante_nombre,cuadrante_codigo
Zona Norte,ZN,Sector 1,S1,C1-A,C1A
Zona Norte,ZN,Sector 1,S1,C1-B,C1B
```

#### Opción C: Entrada Manual
- Formulario para agregar zonas una por una
- Interfaz jerárquica para sectores y cuadrantes

**Archivos a crear**:
- `src/Ceiba.Application/Services/ICatalogImportService.cs`
- `src/Ceiba.Infrastructure/Services/CatalogImportService.cs`
- Plantillas de ejemplo en `data/templates/catalogs-template.json`

### Fase 5: Eliminación de Hints de Login

**Objetivo**: Remover las credenciales de prueba visibles en producción.

**Cambios en `Login.razor`**:

```razor
@* ANTES (líneas 94-103) - REMOVER *@
<div class="alert alert-info">
    <strong>Credenciales de prueba:</strong><br />
    Usuario: admin@ceiba.local<br />
    Contraseña: Admin123!@
</div>

@* DESPUÉS - Solo mostrar en desarrollo *@
@if (Environment.IsDevelopment())
{
    <div class="alert alert-warning">
        <small>Modo desarrollo - Ver TEST-USERS.md para credenciales</small>
    </div>
}
```

**Configuración por ambiente**:
- `Development`: Puede mostrar indicador de modo desarrollo
- `Staging`: Sin hints, con banner de ambiente
- `Production`: Sin hints, sin banners

### Fase 6: Refactorización de SeedDataService

**Objetivo**: Separar datos de desarrollo de datos de producción.

**Estructura propuesta**:
```
src/Ceiba.Infrastructure/Data/
├── Seeding/
│   ├── ISeedDataService.cs          # Interfaz común
│   ├── DevelopmentSeedService.cs    # Datos de prueba (desarrollo)
│   ├── ProductionSeedService.cs     # Solo roles básicos
│   └── CatalogSeedService.cs        # Catálogos de sugerencias
```

**DevelopmentSeedService** (solo en Development):
- Crea usuarios de prueba
- Genera datos ficticios de Zona/Sector/Cuadrante
- Crea reportes de ejemplo

**ProductionSeedService** (siempre):
- Crea los 3 roles: CREADOR, REVISOR, ADMIN
- Crea catálogos de sugerencias base (Sexo, Delito, etc.)
- NO crea usuarios (eso lo hace el Setup Wizard)

### Fase 7: Middleware de Redirección

**Objetivo**: Forzar el flujo de setup antes de usar la aplicación.

**Lógica del middleware**:
```csharp
public class SetupRedirectMiddleware
{
    public async Task InvokeAsync(HttpContext context, ISetupService setupService)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        // Permitir siempre: assets, health checks, setup pages
        if (IsAllowedPath(path))
        {
            await _next(context);
            return;
        }

        // Si setup requerido, redirigir
        if (await setupService.IsSetupRequiredAsync())
        {
            context.Response.Redirect("/setup");
            return;
        }

        await _next(context);
    }

    private bool IsAllowedPath(string path)
    {
        return path.StartsWith("/setup") ||
               path.StartsWith("/_blazor") ||
               path.StartsWith("/_framework") ||
               path.StartsWith("/css") ||
               path.StartsWith("/js") ||
               path.StartsWith("/health") ||
               path.StartsWith("/favicon");
    }
}
```

## Archivos de Datos de Catálogo

### Estructura para datos reales de la policía

**Ubicación**: `data/catalogs/`

```
data/
├── catalogs/
│   ├── README.md                    # Instrucciones de uso
│   ├── templates/
│   │   ├── zonas-template.json      # Plantilla vacía
│   │   └── zonas-template.csv       # Plantilla CSV
│   └── examples/
│       └── zonas-ejemplo.json       # Ejemplo con datos ficticios
└── seeds/
    └── sugerencias-base.json        # Catálogos de sugerencias
```

**Formato JSON para catálogos reales** (a proporcionar por el usuario):
```json
{
  "version": "1.0",
  "organizacion": "Policía Municipal",
  "fechaActualizacion": "2025-12-12",
  "zonas": [
    {
      "id": "uuid-opcional",
      "nombre": "string",
      "codigo": "string",
      "descripcion": "string-opcional",
      "activo": true,
      "sectores": [...]
    }
  ]
}
```

## Scripts Cross-Platform

### Script de Inicialización de Base de Datos

**Linux** (`scripts/init-database.sh`):
```bash
#!/bin/bash
set -e

echo "=== Inicializando base de datos Ceiba ==="

# Aplicar migraciones
dotnet ef database update --project src/Ceiba.Infrastructure --startup-project src/Ceiba.Web

# Verificar estado
dotnet run --project src/Ceiba.Web -- --check-setup

echo "=== Base de datos lista ==="
```

**Windows** (`scripts/init-database.ps1`):
```powershell
Write-Host "=== Inicializando base de datos Ceiba ===" -ForegroundColor Cyan

# Aplicar migraciones
dotnet ef database update --project src/Ceiba.Infrastructure --startup-project src/Ceiba.Web

# Verificar estado
dotnet run --project src/Ceiba.Web -- --check-setup

Write-Host "=== Base de datos lista ===" -ForegroundColor Green
```

### Script de Importación de Catálogos

**Linux** (`scripts/import-catalogs.sh`):
```bash
#!/bin/bash
CATALOG_FILE=${1:-"data/catalogs/zonas.json"}
dotnet run --project src/Ceiba.Web -- --import-catalogs "$CATALOG_FILE"
```

**Windows** (`scripts/import-catalogs.ps1`):
```powershell
param([string]$CatalogFile = "data\catalogs\zonas.json")
dotnet run --project src/Ceiba.Web -- --import-catalogs $CatalogFile
```

## Tareas de Implementación

### Prioridad Alta (Semana 1)
- [ ] Crear `ISetupService` y `SetupService`
- [ ] Implementar `SetupRedirectMiddleware`
- [ ] Crear página `/setup/admin` (crear primer admin)
- [ ] Modificar `Login.razor` para ocultar hints en producción
- [ ] Refactorizar `SeedDataService` en Development/Production

### Prioridad Media (Semana 2)
- [ ] Crear páginas completas del wizard
- [ ] Implementar `ICatalogImportService`
- [ ] Crear plantillas JSON/CSV para catálogos
- [ ] Crear scripts cross-platform

### Prioridad Baja (Semana 3)
- [ ] Página de configuración de organización (logo, nombre)
- [ ] Validación avanzada de catálogos
- [ ] Tests E2E del flujo de setup
- [ ] Documentación del proceso de instalación

## Consideraciones de Seguridad

1. **Setup solo una vez**: Una vez completado, las páginas `/setup/*` retornan 404
2. **Sin bypass**: No hay forma de saltar el setup si no hay admin
3. **Audit log**: Registrar quién y cuándo completó el setup
4. **Contraseña segura**: Forzar política de contraseñas en el primer admin
5. **HTTPS obligatorio**: El setup debe realizarse sobre HTTPS en producción

## Criterios de Aceptación

1. [ ] Nueva instalación muestra wizard de setup automáticamente
2. [ ] No es posible acceder a ninguna página sin completar setup
3. [ ] El admin creado puede hacer login inmediatamente después
4. [ ] Los catálogos se importan correctamente desde JSON/CSV
5. [ ] La página de login NO muestra credenciales en producción
6. [ ] El setup solo puede ejecutarse una vez
7. [ ] Todo el proceso queda registrado en auditoría

## Referencias

- WordPress First-Run Experience: https://developer.wordpress.org/advanced-administration/
- ASP.NET Core Middleware: https://docs.microsoft.com/aspnet/core/fundamentals/middleware
- Blazor Server Security: https://docs.microsoft.com/aspnet/core/blazor/security

---

**Próximos pasos**: Una vez aprobado este plan, proceder con la implementación comenzando por la Fase 1 (detección de primera instalación).
