# Guía: Agregar un Nuevo Rol

Esta guía te muestra cómo agregar un nuevo rol al sistema de permisos.

## Contexto

El sistema usa ASP.NET Identity con roles predefinidos:
- CREADOR
- REVISOR
- ADMIN

## Ejemplo: Agregar Rol "SUPERVISOR_REGIONAL"

### Paso 1: Definir el Rol

**Archivo:** `src/Ceiba.Infrastructure/Data/Seeding/RoleSeeder.cs`

```csharp
public static class RoleSeeder
{
    public static readonly string[] Roles = new[]
    {
        "CREADOR",
        "REVISOR",
        "ADMIN",
        "SUPERVISOR_REGIONAL"  // Nuevo rol
    };

    public static async Task SeedAsync(RoleManager<IdentityRole<Guid>> roleManager)
    {
        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }
    }
}
```

### Paso 2: Crear Migración para el Rol

Si quieres insertar el rol por migración:

```bash
dotnet ef migrations add AddSupervisorRegionalRole --startup-project ../Ceiba.Web
```

O simplemente reinicia la aplicación para que el seeder lo cree.

### Paso 3: Crear Páginas/Componentes para el Rol

**Archivo:** `src/Ceiba.Web/Components/Pages/Regional/RegionalIndex.razor`

```razor
@page "/regional"
@attribute [Authorize(Roles = "SUPERVISOR_REGIONAL")]

<PageTitle>Panel Regional</PageTitle>

<div class="container-fluid py-4">
    <h2>Panel de Supervisión Regional</h2>
    <!-- Contenido específico para este rol -->
</div>
```

### Paso 4: Agregar al Dashboard

**Archivo:** `src/Ceiba.Web/Components/Pages/Home.razor`

```razor
<!-- SUPERVISOR_REGIONAL Dashboard -->
<AuthorizeView Roles="SUPERVISOR_REGIONAL" Context="regionalContext">
    <Authorized>
        <div class="col-12">
            <div class="card shadow-sm border-0 border-start border-secondary border-4">
                <div class="card-body p-4">
                    <h3 class="card-title mb-4">
                        <i class="bi bi-globe text-secondary me-2"></i>
                        Panel Regional
                    </h3>
                    <p class="card-text text-muted mb-3">
                        Supervisa los reportes de tu región asignada.
                    </p>
                    <a href="/regional" class="btn btn-secondary">
                        <i class="bi bi-speedometer2 me-2"></i>Ir al Panel Regional
                    </a>
                </div>
            </div>
        </div>
    </Authorized>
</AuthorizeView>
```

### Paso 5: Agregar al Menú de Navegación

**Archivo:** `src/Ceiba.Web/Components/Layout/NavMenu.razor`

```razor
<AuthorizeView Roles="SUPERVISOR_REGIONAL">
    <Authorized>
        <div class="nav-item px-3">
            <NavLink class="nav-link" href="regional">
                <span class="bi bi-globe" aria-hidden="true"></span> Panel Regional
            </NavLink>
        </div>
    </Authorized>
</AuthorizeView>
```

### Paso 6: Actualizar Gestión de Usuarios

El rol aparecerá automáticamente en la lista de roles disponibles porque `GetAvailableRolesAsync()` lee de la base de datos.

### Paso 7: Agregar Permisos en Servicios

Si el nuevo rol necesita permisos especiales en servicios:

```csharp
public async Task<PagedResult<ReportDto>> ListReportsAsync(
    ReportFilterDto filter,
    Guid userId,
    bool isRevisor,
    bool isSupervisorRegional,  // Nuevo parámetro
    int? regionId = null)       // Filtro por región
{
    var query = _context.Reports.AsQueryable();

    if (isSupervisorRegional && regionId.HasValue)
    {
        // Solo ve reportes de su región
        query = query.Where(r => r.RegionId == regionId);
    }
    else if (!isRevisor)
    {
        query = query.Where(r => r.CreadorId == userId);
    }

    // ... resto del método
}
```

### Paso 8: Agregar Badge de Color

**Archivo:** `src/Ceiba.Web/Components/Pages/Admin/UserList.razor`

```csharp
private static string GetRoleBadgeClass(string role) => role switch
{
    "ADMIN" => "bg-danger",
    "REVISOR" => "bg-primary",
    "CREADOR" => "bg-info",
    "SUPERVISOR_REGIONAL" => "bg-secondary",  // Nuevo
    _ => "bg-secondary"
};
```

## Agregar Tests

```csharp
[Fact]
public async Task UserWithSupervisorRegional_CanAccessRegionalPage()
{
    // Arrange
    var user = await CreateUserWithRole("SUPERVISOR_REGIONAL");

    // Act
    var canAccess = await AuthorizeUser(user, "/regional");

    // Assert
    Assert.True(canAccess);
}

[Fact]
public async Task SupervisorRegional_OnlySeesRegionReports()
{
    // Arrange
    var regionId = 1;
    var user = await CreateUserWithRole("SUPERVISOR_REGIONAL", regionId);

    // Act
    var reports = await _service.ListReportsAsync(
        new ReportFilterDto(),
        user.Id,
        isRevisor: false,
        isSupervisorRegional: true,
        regionId: regionId);

    // Assert
    Assert.All(reports.Items, r => Assert.Equal(regionId, r.RegionId));
}
```

## Checklist

- [ ] Rol definido en seeder
- [ ] Migración/seeding ejecutado
- [ ] Páginas específicas creadas
- [ ] Dashboard actualizado
- [ ] Menú de navegación actualizado
- [ ] Servicios con nuevos permisos
- [ ] Badge de color definido
- [ ] Tests escritos
- [ ] Documentación de usuario creada
