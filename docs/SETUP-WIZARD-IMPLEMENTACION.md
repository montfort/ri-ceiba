# Implementación del Setup Wizard y Consolidación de Migraciones

*Fecha de implementación: 2025-12-17*

## Resumen Ejecutivo

Se implementó un sistema de configuración inicial estilo WordPress que permite a los administradores configurar la aplicación Ceiba en su primer uso. Además, se consolidaron todas las migraciones de Entity Framework en una única migración "Initial" y se refactorizó el sistema de seeding para separar datos de producción, geográficos y desarrollo.

---

## Commits Generados

### 1. Consolidación de Migraciones y Refactorización de Seeding
**Commit:** `7be2faa`
```
refactor: consolidar migraciones y refactorizar SeedDataService
```

### 2. Implementación del Setup Wizard
**Commit:** `1e23134`
```
feat: implementar Setup Wizard estilo WordPress
```

---

## Fase 0: Consolidación de Migraciones

### Objetivo
Obtener una versión limpia del esquema de base de datos con una única migración.

### Cambios Realizados

| Acción | Archivos |
|--------|----------|
| Eliminado | `20251213063501_InitialCreate.cs` |
| Eliminado | `20251216174429_ChangeTipoDeAccionToString.cs` |
| Eliminado | `20251216223137_ChangeTurnoCeibaAndTrasladosToString.cs` |
| Creado | `20251217033545_Initial.cs` |
| Creado | `docs/DATABASE-SCHEMA.md` |
| Creado | `docs/schema-initial.sql` |

### Documentación Generada
- **DATABASE-SCHEMA.md**: Documentación completa del esquema SQL con todas las tablas, índices y constraints.
- **schema-initial.sql**: Script SQL generado desde la migración para referencia.

---

## Fase 1: Refactorización de SeedDataService

### Objetivo
Separar el seeding en servicios especializados para producción, datos geográficos y desarrollo.

### Nueva Estructura de Archivos

```
src/Ceiba.Infrastructure/Data/Seeding/
├── ISeedDataService.cs          # Interfaces para todos los servicios
├── ProductionSeedService.cs     # Roles y sugerencias
├── GeographicSeedService.cs     # Catálogos geográficos (regiones.json)
├── DevelopmentSeedService.cs    # Usuarios de prueba (solo dev)
└── SeedOrchestrator.cs          # Coordinador de servicios
```

### Interfaces Definidas

```csharp
public interface IProductionSeedService : ISeedDataService
{
    Task SeedRolesAsync();
    Task SeedSugerenciasAsync(Guid creatorUserId);
}

public interface IGeographicSeedService : ISeedDataService
{
    Task ReloadAsync();
}

public interface IDevelopmentSeedService : ISeedDataService
{
    Task SeedTestUsersAsync();
}

public interface ISeedOrchestrator
{
    Task SeedAllAsync();
    Task ReloadGeographicCatalogsAsync();
}
```

### Comportamiento por Ambiente

| Ambiente | Roles | Geográficos | Sugerencias | Usuarios Test |
|----------|-------|-------------|-------------|---------------|
| Production | ✓ | ✓ | ✓* | ✗ |
| Development | ✓ | ✓ | ✓ | ✓ |
| Testing | ✓ | ✓ (manual) | ✓ (manual) | ✓ (manual) |

*En producción, las sugerencias se seedean cuando se crea el primer admin via Setup Wizard.

---

## Fase 2: SetupService

### Objetivo
Lógica para detectar y manejar la configuración inicial.

### Archivos Creados

| Archivo | Descripción |
|---------|-------------|
| `src/Ceiba.Core/Interfaces/ISetupService.cs` | Interface del servicio |
| `src/Ceiba.Infrastructure/Services/SetupService.cs` | Implementación |
| `src/Ceiba.Shared/DTOs/SetupDTOs.cs` | DTOs (SetupStatus, CreateFirstAdminDto, SetupResult) |

### API del Servicio

```csharp
public interface ISetupService
{
    Task<SetupStatus> GetStatusAsync();
    Task<bool> IsSetupRequiredAsync();
    Task MarkSetupCompleteAsync();
    Task<SetupResult> CreateFirstAdminAsync(CreateFirstAdminDto dto);
}
```

### SetupStatus DTO

```csharp
public class SetupStatus
{
    public bool IsComplete { get; set; }
    public bool HasUsers { get; set; }
    public bool HasRoles { get; set; }
    public bool HasGeographicCatalogs { get; set; }
    public bool HasSuggestions { get; set; }
    public string? Message { get; set; }
}
```

### CreateFirstAdminDto

```csharp
public class CreateFirstAdminDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 10)]
    [RegularExpression(@"^(?=.*[A-Z])(?=.*\d).+$")]
    public string Password { get; set; }

    [Required]
    [Compare(nameof(Password))]
    public string ConfirmPassword { get; set; }
}
```

---

## Fase 3: SetupRedirectMiddleware

### Objetivo
Forzar el wizard antes de usar la aplicación.

### Archivo Creado
`src/Ceiba.Web/Middleware/SetupRedirectMiddleware.cs`

### Comportamiento

1. **Verifica ambiente**: Si es `Testing`, no redirige (los tests seedean sus propios datos).
2. **Verifica ruta**: Si la ruta está en la lista de permitidas, continúa.
3. **Verifica setup**: Si no hay usuarios, redirige a `/setup`.

### Rutas Permitidas

```csharp
private static readonly string[] AllowedPaths =
{
    "/setup",           // Setup wizard pages
    "/_blazor",         // Blazor SignalR hub
    "/_framework",      // Blazor framework files
    "/health",          // Health checks
    "/alive",           // Liveness probe
    "/css", "/js", "/lib", "/favicon", "/_content"
};
```

### Registro en Pipeline

```csharp
// En Program.cs, después de UseRouting():
app.UseRouting();
app.UseSetupRedirect();  // <-- Nuevo middleware
app.UseAuthentication();
app.UseAuthorization();
```

---

## Fase 4: Páginas del Setup Wizard

### Estructura de Archivos

```
src/Ceiba.Web/Components/Pages/Setup/
├── _Imports.razor       # Imports compartidos
├── SetupLayout.razor    # Layout sin navegación
├── Welcome.razor        # /setup - Estado del sistema
├── CreateAdmin.razor    # /setup/admin - Crear administrador
└── Complete.razor       # /setup/complete - Confirmación
```

### Flujo del Usuario

```
┌─────────────────┐
│   Usuario       │
│   accede a /    │
└────────┬────────┘
         │
         ▼
┌─────────────────┐      Sí      ┌─────────────────┐
│  ¿Hay usuarios? │─────────────►│   Continuar     │
└────────┬────────┘              │   normalmente   │
         │ No                    └─────────────────┘
         ▼
┌─────────────────┐
│  Redirigir a    │
│    /setup       │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Welcome.razor  │
│  Estado sistema │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ CreateAdmin.razor│
│ Crear primer    │
│ administrador   │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Complete.razor  │
│ Confirmación    │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│   Redirigir a   │
│   /auth/login   │
└─────────────────┘
```

### Características de las Páginas

#### Welcome.razor (`/setup`)
- Muestra estado del sistema (roles, usuarios, catálogos, sugerencias)
- Botón para iniciar configuración
- Si ya está configurado, muestra mensaje y link al inicio

#### CreateAdmin.razor (`/setup/admin`)
- Formulario con validación
- Campos: Email, Password, Confirm Password
- Validación de política de contraseñas
- Feedback de errores en tiempo real

#### Complete.razor (`/setup/complete`)
- Confirmación de éxito
- Resumen de configuración
- Links a login e inicio

---

## Fase 5: Limpieza y Seguridad

### Cambios en Program.cs

```csharp
// Registrar servicios de seeding
builder.Services.AddScoped<IProductionSeedService, ProductionSeedService>();
builder.Services.AddScoped<IGeographicSeedService, GeographicSeedService>();
builder.Services.AddScoped<ISeedOrchestrator, SeedOrchestrator>();

// Registrar SetupService
builder.Services.AddScoped<ISetupService, SetupService>();

// En Development, también registrar DevelopmentSeedService
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<IDevelopmentSeedService, DevelopmentSeedService>();
}
```

### Actualización de Test Factories

Se actualizaron los siguientes archivos para registrar los nuevos servicios:
- `tests/Ceiba.Integration.Tests/CeibaWebApplicationFactory.cs`
- `tests/Ceiba.Integration.Tests/E2E/E2ETestServerFixture.cs`

---

## Cómo Probar

### Escenario: Primera Ejecución

1. Iniciar la aplicación con base de datos vacía:
   ```bash
   dotnet run --project src/Ceiba.Web
   ```

2. Acceder a cualquier URL (e.g., `http://localhost:5000/`)

3. Ser redirigido automáticamente a `/setup`

4. Completar el formulario de creación de administrador

5. Ver confirmación y ser redirigido a login

### Escenario: Aplicación Ya Configurada

1. Si ya existe al menos un usuario, el middleware no redirige
2. La aplicación funciona normalmente

---

## Archivos Eliminados

| Archivo | Razón |
|---------|-------|
| `src/Ceiba.Infrastructure/Data/ISeedDataService.cs` | Reemplazado por interfaces en Seeding/ |
| `src/Ceiba.Infrastructure/Data/SeedDataService.cs` | Reemplazado por servicios especializados |
| Migraciones antiguas | Consolidadas en Initial |

---

## Consideraciones de Seguridad

1. **Ambiente Testing**: El middleware se desactiva para no interferir con tests
2. **Validación de Contraseña**: Mínimo 10 caracteres, mayúscula y número
3. **Primer Admin**: Recibe todos los roles (ADMIN, REVISOR, CREADOR)
4. **Rutas Estáticas**: Permitidas sin autenticación para recursos del UI

---

## Próximos Pasos Sugeridos

1. [ ] Agregar tests unitarios para SetupService
2. [ ] Agregar tests de componentes Blazor para las páginas del wizard
3. [ ] Considerar agregar paso de configuración de organización
4. [ ] Considerar agregar verificación de catálogos geográficos en el wizard
5. [ ] Agregar logging de auditoría para la creación del primer admin

---

*Documento generado automáticamente como parte de la implementación del Setup Wizard.*
