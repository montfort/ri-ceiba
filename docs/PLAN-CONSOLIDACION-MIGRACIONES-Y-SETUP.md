# Plan Integrado: Consolidación de Migraciones + Setup Wizard

> **Estado**: Planificación
> **Prioridad**: Alta
> **Fecha**: 2025-12-16
> **Relacionado con**: [PLAN-INITIAL-SETUP-WIZARD.md](./PLAN-INITIAL-SETUP-WIZARD.md)

## Resumen Ejecutivo

Este plan integra dos iniciativas complementarias:

1. **Consolidación de Migraciones**: Reemplazar las múltiples migraciones incrementales por una única migración "Initial" que contenga el esquema completo de la base de datos.

2. **Setup Wizard**: Sistema de configuración inicial tipo WordPress donde el administrador crea su cuenta sin credenciales por defecto.

El objetivo es una instalación limpia donde:
- La base de datos se crea con el esquema completo y datos esenciales (geografía + sugerencias)
- NO existen usuarios predefinidos
- El primer ADMIN se crea interactivamente vía Setup Wizard

---

## Problema Actual

### Migraciones
- Múltiples migraciones incrementales dificultan entender el esquema final
- Migraciones de refactorización (TipoDeAccion, TurnoCeiba, Traslados) ya no son necesarias
- Setup lento: cada nueva instalación ejecuta todas las migraciones secuencialmente

### Datos Semilla
- `SeedDataService` crea usuarios con contraseñas conocidas
- Credenciales visibles en `Login.razor`
- No hay separación entre datos de desarrollo y producción
- Datos geográficos y sugerencias mezclados con usuarios de prueba

---

## Solución Propuesta

### Arquitectura del Flujo de Primera Instalación

```
┌─────────────────────────────────────────────────────────────────┐
│                    PRIMERA EJECUCIÓN                            │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  1. MIGRACIÓN "Initial" (automática en startup)                 │
│                                                                 │
│     Crea:                                                       │
│     ├── Todas las tablas del esquema                            │
│     ├── Índices y constraints                                   │
│     └── (Sin datos - solo estructura)                           │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  2. SEED SERVICES (automático post-migración)                   │
│                                                                 │
│     ProductionSeedService (siempre):                            │
│     ├── Roles: CREADOR, REVISOR, ADMIN                          │
│     └── Sugerencias: sexo, delito, tipo_de_atencion,            │
│                      turno_ceiba, traslados                     │
│                                                                 │
│     GeographicSeedService (siempre):                            │
│     └── Zona → Región → Sector → Cuadrante (desde regiones.json)│
│                                                                 │
│     DevelopmentSeedService (solo si ASPNETCORE_ENVIRONMENT=Dev):│
│     └── Usuarios de prueba (creador@test, revisor@test)         │
│         NOTA: NO crea admin - eso lo hace el wizard             │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  3. MIDDLEWARE: SetupRedirectMiddleware                         │
│                                                                 │
│     Verifica: ¿Existe al menos un usuario con rol ADMIN?        │
│                                                                 │
│     NO  → Redirigir a /setup (wizard obligatorio)               │
│     SÍ  → Continuar a /login (flujo normal)                     │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼ (Si no hay ADMIN)
┌─────────────────────────────────────────────────────────────────┐
│  4. SETUP WIZARD                                                │
│                                                                 │
│     /setup              → Bienvenida e instrucciones            │
│     /setup/admin        → Crear primera cuenta ADMIN            │
│     /setup/organization → Configurar nombre/logo (opcional)     │
│     /setup/catalogs     → Verificar catálogos geográficos (opc.)│
│     /setup/complete     → Resumen y finalización                │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  5. APLICACIÓN OPERATIVA                                        │
│                                                                 │
│     • ADMIN puede crear usuarios CREADOR/REVISOR                │
│     • CREADOR puede crear reportes de incidencias               │
│     • REVISOR puede revisar y exportar reportes                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## Estructura de Archivos

### Antes (Actual)
```
src/Ceiba.Infrastructure/
├── Data/
│   ├── SeedDataService.cs              # Todo mezclado
│   └── Migrations/
│       ├── 20251201_Initial.cs
│       ├── 20251205_AddAuditTables.cs
│       ├── 20251210_AddCatalogs.cs
│       ├── 20251216_ChangeTipoDeAccion.cs
│       ├── 20251216_ChangeTurnoCeiba.cs
│       └── ... (muchas más)
```

### Después (Propuesto)
```
src/Ceiba.Infrastructure/
├── Data/
│   ├── Seeding/
│   │   ├── ISeedDataService.cs             # Interfaz común
│   │   ├── ProductionSeedService.cs        # Roles + Sugerencias
│   │   ├── GeographicSeedService.cs        # Datos geográficos
│   │   ├── DevelopmentSeedService.cs       # Usuarios de prueba (dev only)
│   │   └── SeedOrchestrator.cs             # Coordina todos los seeds
│   └── Migrations/
│       └── 00000000000000_Initial.cs       # Migración única consolidada

src/Ceiba.Application/
├── Services/
│   ├── ISetupService.cs                    # Interfaz del servicio de setup
│   └── SetupService.cs                     # Implementación

src/Ceiba.Web/
├── Components/Pages/Setup/
│   ├── SetupLayout.razor                   # Layout sin navegación
│   ├── Welcome.razor                       # /setup
│   ├── CreateAdmin.razor                   # /setup/admin
│   ├── OrganizationConfig.razor            # /setup/organization
│   ├── VerifyCatalogs.razor                # /setup/catalogs
│   └── Complete.razor                      # /setup/complete
├── Middleware/
│   └── SetupRedirectMiddleware.cs          # Redirección si no hay admin

data/
├── regiones.json                           # Datos geográficos oficiales
└── templates/
    ├── regiones-template.json              # Plantilla para importación
    └── sugerencias-base.json               # Catálogo de sugerencias
```

---

## Datos Sembrados por Capa

### 1. Migración Initial (Solo Esquema)

La migración consolidada crea únicamente la estructura de tablas:

```csharp
// 00000000000000_Initial.cs
protected override void Up(MigrationBuilder migrationBuilder)
{
    // === IDENTITY TABLES ===
    // AspNetUsers, AspNetRoles, AspNetUserRoles, etc.

    // === DOMAIN TABLES ===
    // ZONA, REGION, SECTOR, CUADRANTE
    // CATALOGO_SUGERENCIA
    // REPORTE_INCIDENCIA
    // AUDITORIA
    // REPORTE_AUTOMATIZADO, PLANTILLA_REPORTE

    // === INDEXES & CONSTRAINTS ===
    // Todos los índices y foreign keys
}
```

**NO incluye**: Datos de ningún tipo (ni roles, ni usuarios, ni sugerencias).

### 2. ProductionSeedService (Siempre Ejecuta)

Datos mínimos necesarios para que la aplicación funcione:

```csharp
public class ProductionSeedService : ISeedDataService
{
    public async Task SeedAsync()
    {
        // 1. Crear roles si no existen
        await CreateRoleIfNotExistsAsync("CREADOR");
        await CreateRoleIfNotExistsAsync("REVISOR");
        await CreateRoleIfNotExistsAsync("ADMIN");

        // 2. Crear sugerencias base si no existen
        await SeedSugerenciasAsync();
    }

    private async Task SeedSugerenciasAsync()
    {
        if (await _context.CatalogosSugerencia.AnyAsync())
            return; // Ya existen, no duplicar

        var sugerencias = new[]
        {
            // Sexo
            ("sexo", "Hombre", 1),
            ("sexo", "Mujer", 2),
            ("sexo", "No binario", 3),
            ("sexo", "Prefiere no decir", 4),

            // Delito
            ("delito", "Violencia familiar", 1),
            ("delito", "Abuso sexual", 2),
            ("delito", "Acoso sexual", 3),
            ("delito", "Violación", 4),
            ("delito", "Tentativa de feminicidio", 5),
            ("delito", "Feminicidio", 6),
            ("delito", "Violencia vicaria", 7),
            ("delito", "Amenazas", 8),

            // Tipo de Atención
            ("tipo_de_atencion", "Llamada telefónica", 1),
            ("tipo_de_atencion", "Mensaje de texto", 2),
            ("tipo_de_atencion", "Radio", 3),
            ("tipo_de_atencion", "Primer respondiente", 4),

            // Turno CEIBA
            ("turno_ceiba", "Balderas 1", 1),
            ("turno_ceiba", "Balderas 2", 2),
            ("turno_ceiba", "Balderas 3", 3),
            ("turno_ceiba", "Nonoalco 1", 4),
            ("turno_ceiba", "Nonoalco 2", 5),
            ("turno_ceiba", "Nonoalco 3", 6),

            // Traslados
            ("traslados", "Sí", 1),
            ("traslados", "No", 2),
            ("traslados", "No aplica", 3),
        };

        // Insertar todas las sugerencias
        foreach (var (campo, valor, orden) in sugerencias)
        {
            _context.CatalogosSugerencia.Add(new CatalogoSugerencia
            {
                Campo = campo,
                Valor = valor,
                Orden = orden,
                Activo = true,
                UsuarioId = Guid.Empty // Sistema
            });
        }

        await _context.SaveChangesAsync();
    }
}
```

### 3. GeographicSeedService (Siempre Ejecuta)

Carga datos geográficos desde `regiones.json`:

```csharp
public class GeographicSeedService : ISeedDataService
{
    public async Task SeedAsync()
    {
        // Solo cargar si las tablas están vacías
        if (await _context.Zonas.AnyAsync())
        {
            _logger.LogInformation("Datos geográficos ya existen. Omitiendo seed.");
            return;
        }

        var jsonPath = GetRegionesJsonPath();
        if (!File.Exists(jsonPath))
        {
            _logger.LogWarning("regiones.json no encontrado en {Path}. " +
                "Los catálogos geográficos deberán importarse manualmente.", jsonPath);
            return;
        }

        await LoadFromRegionesJsonAsync(jsonPath);
    }
}
```

### 4. DevelopmentSeedService (Solo en Desarrollo)

Usuarios de prueba para facilitar el desarrollo:

```csharp
public class DevelopmentSeedService : ISeedDataService
{
    public async Task SeedAsync()
    {
        // SOLO ejecutar en ambiente de desarrollo
        if (!_environment.IsDevelopment())
            return;

        _logger.LogWarning("⚠️ Creando usuarios de prueba (solo desarrollo)");

        // Crear CREADOR de prueba
        await CreateTestUserIfNotExistsAsync(
            email: "creador@test.com",
            password: GetTestPassword("CREADOR"),
            role: "CREADOR",
            nombre: "Juan",
            apellido: "Pérez"
        );

        // Crear REVISOR de prueba
        await CreateTestUserIfNotExistsAsync(
            email: "revisor@test.com",
            password: GetTestPassword("REVISOR"),
            role: "REVISOR",
            nombre: "María",
            apellido: "González"
        );

        // ⚠️ NO crear ADMIN de prueba
        // El primer ADMIN siempre se crea vía Setup Wizard
        // Esto garantiza que incluso en desarrollo se pruebe el wizard
    }
}
```

---

## Setup Wizard

### Servicio de Detección

```csharp
public interface ISetupService
{
    /// <summary>
    /// Verifica si el sistema requiere configuración inicial.
    /// Retorna true si no existe ningún usuario con rol ADMIN.
    /// </summary>
    Task<bool> IsSetupRequiredAsync();

    /// <summary>
    /// Obtiene el estado detallado del setup.
    /// </summary>
    Task<SetupStatus> GetSetupStatusAsync();

    /// <summary>
    /// Crea el primer usuario administrador.
    /// Solo funciona si IsSetupRequired == true.
    /// </summary>
    Task<IdentityResult> CreateInitialAdminAsync(CreateAdminDto dto);

    /// <summary>
    /// Marca el setup como completado.
    /// </summary>
    Task MarkSetupCompletedAsync();
}

public class SetupStatus
{
    public bool HasAdminUser { get; set; }
    public bool HasRoles { get; set; }
    public bool HasSugerencias { get; set; }
    public bool HasGeographicData { get; set; }
    public int ZonasCount { get; set; }
    public int SugerenciasCount { get; set; }

    public bool IsSetupRequired => !HasAdminUser;
    public bool IsFullyConfigured => HasAdminUser && HasRoles &&
                                      HasSugerencias && HasGeographicData;
}
```

### Middleware de Redirección

```csharp
public class SetupRedirectMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly string[] AllowedPaths = new[]
    {
        "/setup",
        "/_blazor",
        "/_framework",
        "/css",
        "/js",
        "/lib",
        "/favicon",
        "/health",
        "/api/health"
    };

    public async Task InvokeAsync(HttpContext context, ISetupService setupService)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        // Permitir rutas estáticas y del setup
        if (AllowedPaths.Any(p => path.StartsWith(p)))
        {
            await _next(context);
            return;
        }

        // Verificar si setup es requerido
        if (await setupService.IsSetupRequiredAsync())
        {
            context.Response.Redirect("/setup");
            return;
        }

        await _next(context);
    }
}
```

### Páginas del Wizard

| Ruta | Componente | Descripción | Obligatorio |
|------|------------|-------------|-------------|
| `/setup` | Welcome.razor | Bienvenida e instrucciones | Sí |
| `/setup/admin` | CreateAdmin.razor | Crear primera cuenta ADMIN | Sí |
| `/setup/organization` | OrganizationConfig.razor | Nombre y logo de organización | No |
| `/setup/catalogs` | VerifyCatalogs.razor | Verificar/importar catálogos | No |
| `/setup/complete` | Complete.razor | Resumen y finalización | Sí |

---

## Fases de Implementación

### Fase 0: Consolidación de Migraciones (Prerequisito)

**Objetivo**: Base de datos limpia con esquema completo.

**Tareas**:
- [ ] 0.1 Backup del esquema actual (documentar en SQL)
- [ ] 0.2 Eliminar todas las migraciones existentes
- [ ] 0.3 Crear migración "Initial" consolidada
- [ ] 0.4 Verificar que la migración crea el esquema correcto
- [ ] 0.5 Crear tests de integración para validar esquema

**Archivos afectados**:
```
ELIMINAR:
  src/Ceiba.Infrastructure/Migrations/*.cs (todas excepto CeibaDbContextModelSnapshot)

CREAR:
  src/Ceiba.Infrastructure/Migrations/00000000000000_Initial.cs

MODIFICAR:
  src/Ceiba.Infrastructure/Migrations/CeibaDbContextModelSnapshot.cs (regenerar)
```

**Comando**:
```bash
# Eliminar migraciones
rm -rf src/Ceiba.Infrastructure/Migrations/*.cs

# Crear migración inicial
dotnet ef migrations add Initial --project src/Ceiba.Infrastructure --startup-project src/Ceiba.Web

# Verificar
dotnet ef migrations list --project src/Ceiba.Infrastructure --startup-project src/Ceiba.Web
```

---

### Fase 1: Refactorización de SeedDataService

**Objetivo**: Separar datos de producción, geográficos y desarrollo.

**Tareas**:
- [ ] 1.1 Crear interfaz `ISeedDataService`
- [ ] 1.2 Crear `ProductionSeedService` (roles + sugerencias)
- [ ] 1.3 Crear `GeographicSeedService` (zonas desde JSON)
- [ ] 1.4 Crear `DevelopmentSeedService` (usuarios de prueba)
- [ ] 1.5 Crear `SeedOrchestrator` para coordinar
- [ ] 1.6 Modificar `Program.cs` para usar el nuevo sistema
- [ ] 1.7 Eliminar `SeedDataService` original
- [ ] 1.8 Actualizar tests

**Archivos**:
```
CREAR:
  src/Ceiba.Infrastructure/Data/Seeding/ISeedDataService.cs
  src/Ceiba.Infrastructure/Data/Seeding/ProductionSeedService.cs
  src/Ceiba.Infrastructure/Data/Seeding/GeographicSeedService.cs
  src/Ceiba.Infrastructure/Data/Seeding/DevelopmentSeedService.cs
  src/Ceiba.Infrastructure/Data/Seeding/SeedOrchestrator.cs

ELIMINAR:
  src/Ceiba.Infrastructure/Data/SeedDataService.cs
  src/Ceiba.Infrastructure/Data/ISeedDataService.cs

MODIFICAR:
  src/Ceiba.Web/Program.cs
```

---

### Fase 2: Servicio de Setup

**Objetivo**: Lógica para detectar y manejar la configuración inicial.

**Tareas**:
- [ ] 2.1 Crear `ISetupService` interfaz
- [ ] 2.2 Implementar `SetupService`
- [ ] 2.3 Crear `SetupStatus` DTO
- [ ] 2.4 Crear `CreateAdminDto` con validaciones
- [ ] 2.5 Registrar servicio en DI
- [ ] 2.6 Crear tests unitarios

**Archivos**:
```
CREAR:
  src/Ceiba.Core/Interfaces/ISetupService.cs
  src/Ceiba.Infrastructure/Services/SetupService.cs
  src/Ceiba.Shared/DTOs/SetupDTOs.cs
  tests/Ceiba.Infrastructure.Tests/Services/SetupServiceTests.cs
```

---

### Fase 3: Middleware de Redirección

**Objetivo**: Forzar el wizard antes de usar la aplicación.

**Tareas**:
- [ ] 3.1 Crear `SetupRedirectMiddleware`
- [ ] 3.2 Registrar middleware en pipeline (antes de auth)
- [ ] 3.3 Crear tests de integración

**Archivos**:
```
CREAR:
  src/Ceiba.Web/Middleware/SetupRedirectMiddleware.cs
  tests/Ceiba.Integration.Tests/SetupRedirectTests.cs

MODIFICAR:
  src/Ceiba.Web/Program.cs (registrar middleware)
```

---

### Fase 4: Páginas del Setup Wizard

**Objetivo**: UI para la configuración inicial.

**Tareas**:
- [ ] 4.1 Crear `SetupLayout.razor` (sin navegación)
- [ ] 4.2 Crear `Welcome.razor` (/setup)
- [ ] 4.3 Crear `CreateAdmin.razor` (/setup/admin)
- [ ] 4.4 Crear `OrganizationConfig.razor` (/setup/organization)
- [ ] 4.5 Crear `VerifyCatalogs.razor` (/setup/catalogs)
- [ ] 4.6 Crear `Complete.razor` (/setup/complete)
- [ ] 4.7 Crear componentes compartidos del wizard
- [ ] 4.8 Crear tests de componentes (bUnit)

**Archivos**:
```
CREAR:
  src/Ceiba.Web/Components/Pages/Setup/SetupLayout.razor
  src/Ceiba.Web/Components/Pages/Setup/Welcome.razor
  src/Ceiba.Web/Components/Pages/Setup/CreateAdmin.razor
  src/Ceiba.Web/Components/Pages/Setup/OrganizationConfig.razor
  src/Ceiba.Web/Components/Pages/Setup/VerifyCatalogs.razor
  src/Ceiba.Web/Components/Pages/Setup/Complete.razor
  src/Ceiba.Web/Components/Pages/Setup/_Imports.razor
  tests/Ceiba.Web.Tests/Components/Setup/
```

---

### Fase 5: Limpieza y Seguridad

**Objetivo**: Remover código inseguro y finalizar.

**Tareas**:
- [ ] 5.1 Remover hints de credenciales en `Login.razor`
- [ ] 5.2 Agregar logging de auditoría para setup
- [ ] 5.3 Documentar proceso de instalación
- [ ] 5.4 Crear script de instalación limpia
- [ ] 5.5 Tests E2E del flujo completo

**Archivos**:
```
MODIFICAR:
  src/Ceiba.Web/Components/Pages/Auth/Login.razor (remover hints)

CREAR:
  docs/INSTALACION.md
  scripts/clean-install.sh
  scripts/clean-install.ps1
  tests/Ceiba.E2E.Tests/SetupWizardTests.cs
```

---

## Consideraciones de Seguridad

1. **Setup único**: Una vez creado el primer ADMIN, las páginas `/setup/*` retornan 404
2. **Sin bypass**: Imposible acceder a la app sin completar el setup
3. **Contraseña segura**: El primer ADMIN debe cumplir política de seguridad
4. **Auditoría**: El evento de creación del primer admin queda registrado
5. **HTTPS**: En producción, el setup debe realizarse sobre HTTPS
6. **Rate limiting**: Máximo 5 intentos de creación de admin por IP

---

## Criterios de Aceptación

### Consolidación de Migraciones
- [ ] Existe una única migración "Initial"
- [ ] La migración crea todas las tablas correctamente
- [ ] No hay datos de usuarios en la migración
- [ ] Los tests de esquema pasan

### Seed Services
- [ ] Roles se crean automáticamente
- [ ] Sugerencias se crean automáticamente
- [ ] Datos geográficos se cargan desde regiones.json
- [ ] Usuarios de prueba solo se crean en Development
- [ ] NO se crea ningún ADMIN automáticamente

### Setup Wizard
- [ ] Nueva instalación redirige automáticamente a /setup
- [ ] Es imposible acceder a otra página sin completar setup
- [ ] El admin creado puede hacer login inmediatamente
- [ ] El setup solo puede ejecutarse una vez
- [ ] Todo queda registrado en auditoría

### Seguridad
- [ ] Login.razor NO muestra credenciales en producción
- [ ] No existen credenciales por defecto en el código
- [ ] El primer ADMIN cumple política de contraseñas

---

## Estimación de Esfuerzo

| Fase | Descripción | Complejidad | Archivos |
|------|-------------|-------------|----------|
| 0 | Consolidación de Migraciones | Media | ~5 |
| 1 | Refactorización SeedDataService | Media | ~8 |
| 2 | Servicio de Setup | Baja | ~4 |
| 3 | Middleware de Redirección | Baja | ~3 |
| 4 | Páginas del Wizard | Alta | ~10 |
| 5 | Limpieza y Seguridad | Baja | ~5 |

**Total estimado**: ~35 archivos nuevos/modificados

---

## Referencias

- [PLAN-INITIAL-SETUP-WIZARD.md](./PLAN-INITIAL-SETUP-WIZARD.md) - Plan original del wizard
- [WordPress First-Run Experience](https://developer.wordpress.org/advanced-administration/)
- [ASP.NET Core Middleware](https://docs.microsoft.com/aspnet/core/fundamentals/middleware)
- [EF Core Migrations](https://docs.microsoft.com/ef/core/managing-schemas/migrations)

---

## Próximos Pasos

1. **Revisar y aprobar** este plan integrado
2. **Ejecutar Fase 0**: Consolidación de migraciones
3. **Ejecutar Fase 1**: Refactorización de SeedDataService
4. **Continuar** con las fases restantes secuencialmente

---

*Documento creado: 2025-12-16*
*Última actualización: 2025-12-16*
