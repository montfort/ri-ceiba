# Plan de Implementación de .NET Aspire para Ceiba

## Contexto Actual

| Aspecto | Estado Actual |
|---------|---------------|
| **Framework** | .NET 10.0 |
| **Base de datos** | PostgreSQL local (localhost:5432, usuario: ceiba, password: ceiba123) |
| **ORM** | Entity Framework Core 10.0 con Npgsql |
| **Docker** | docker-compose.yml existente (no usado activamente) |
| **Health checks** | No implementados (docker espera `/health` pero no existe) |
| **Aspire** | No existe |

---

## Fase 1: Crear Proyectos Aspire Base

### 1.1 Crear proyecto AppHost (`Ceiba.AppHost`)

```
Ceiba.AppHost/
├── Ceiba.AppHost.csproj    (SDK: Aspire.AppHost.Sdk)
├── Program.cs              (Orquestación)
└── appsettings.json        (Configuración del orquestador)
```

### 1.2 Crear proyecto ServiceDefaults (`Ceiba.ServiceDefaults`)

```
Ceiba.ServiceDefaults/
├── Ceiba.ServiceDefaults.csproj
└── Extensions.cs           (Health checks, OpenTelemetry, configuración común)
```

### 1.3 Agregar proyectos a la solución

```bash
dotnet new aspire-apphost -n Ceiba.AppHost -o Ceiba.AppHost
dotnet new aspire-servicedefaults -n Ceiba.ServiceDefaults -o Ceiba.ServiceDefaults
dotnet sln add Ceiba.AppHost/Ceiba.AppHost.csproj
dotnet sln add Ceiba.ServiceDefaults/Ceiba.ServiceDefaults.csproj
```

---

## Fase 2: Configurar Orquestación en AppHost

### 2.1 Program.cs del AppHost

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL con persistencia (reemplaza la instancia local)
var postgres = builder.AddPostgres("postgres")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume("ceiba-postgres-data");

var database = postgres.AddDatabase("ceiba");

// Aplicación web Blazor
var web = builder.AddProject<Projects.Ceiba_Web>("ceiba-web")
    .WithReference(database)
    .WaitFor(database)
    .WithExternalHttpEndpoints();

builder.Build().Run();
```

### 2.2 Paquetes requeridos en AppHost

```xml
<PackageReference Include="Aspire.Hosting.PostgreSQL" Version="9.0.0" />
```

---

## Fase 3: Integrar ServiceDefaults en Ceiba.Web

### 3.1 Agregar referencia a ServiceDefaults

```bash
dotnet add src/Ceiba.Web reference Ceiba.ServiceDefaults
```

### 3.2 Modificar Program.cs de Ceiba.Web

```csharp
// Al inicio
builder.AddServiceDefaults();

// Antes de app.Run()
app.MapDefaultEndpoints(); // Incluye /health, /alive
```

### 3.3 Cambiar registro de DbContext para usar Aspire

**Antes (actual):**
```csharp
builder.Services.AddDbContext<CeibaDbContext>((sp, options) =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(connectionString, ...);
});
```

**Después (con Aspire):**
```csharp
builder.AddNpgsqlDbContext<CeibaDbContext>("ceiba", settings =>
{
    settings.DisableRetry = false;
});
```

### 3.4 Paquetes requeridos en Ceiba.Web

```xml
<PackageReference Include="Aspire.Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.0.0" />
```

---

## Fase 4: Configuración de Conexión Dual

**Objetivo:** Permitir ejecutar tanto con Aspire como sin él (desarrollo independiente).

### 4.1 Detección automática en Program.cs

```csharp
// Si hay connection string de Aspire, usarlo; si no, usar el tradicional
if (builder.Configuration.GetConnectionString("ceiba") is not null)
{
    // Aspire está orquestando - usa integración Aspire
    builder.AddNpgsqlDbContext<CeibaDbContext>("ceiba");
}
else
{
    // Ejecución independiente - usa configuración tradicional
    builder.Services.AddDbContext<CeibaDbContext>((sp, options) =>
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly("Ceiba.Infrastructure");
        });
    });
}
```

---

## Fase 5: Migración de Datos

**Problema:** La base de datos PostgreSQL local tiene datos existentes.

**Opciones:**

| Opción | Descripción | Recomendación |
|--------|-------------|---------------|
| **A) Backup/Restore** | Exportar datos de PostgreSQL local e importar en contenedor Aspire | Recomendado si hay datos importantes |
| **B) Seed fresco** | Usar SeedDataService para crear datos iniciales en contenedor nuevo | Más limpio para desarrollo |
| **C) Volumen compartido** | Montar el directorio de datos de PostgreSQL local | Complicado, no recomendado |

**Recomendación:** Opción B (Seed fresco) para desarrollo, con script de backup opcional.

---

## Fase 6: Estructura Final del Proyecto

```
ri-ceiba/
├── Ceiba.sln
├── Ceiba.AppHost/                    # NUEVO - Orquestador Aspire
│   ├── Ceiba.AppHost.csproj
│   ├── Program.cs
│   └── appsettings.json
├── Ceiba.ServiceDefaults/            # NUEVO - Configuración compartida
│   ├── Ceiba.ServiceDefaults.csproj
│   └── Extensions.cs
├── src/
│   ├── Ceiba.Web/                    # MODIFICADO - Usa ServiceDefaults
│   ├── Ceiba.Infrastructure/
│   ├── Ceiba.Application/
│   ├── Ceiba.Core/
│   └── Ceiba.Shared/
├── tests/
│   └── ...
└── docker/                           # Se mantiene como referencia/producción
    └── docker-compose.yml
```

---

## Fase 7: Comandos de Ejecución

### Con Aspire (desarrollo orquestado)

```bash
dotnet run --project Ceiba.AppHost
```

- Inicia PostgreSQL en contenedor
- Inicia Ceiba.Web conectado al contenedor
- Abre dashboard de Aspire (http://localhost:15888)

### Sin Aspire (desarrollo tradicional)

```bash
dotnet run --project src/Ceiba.Web
```

- Usa PostgreSQL local (localhost:5432)
- Funciona como antes

---

## Resumen de Archivos a Crear/Modificar

| Archivo | Acción | Descripción |
|---------|--------|-------------|
| `Ceiba.AppHost/Ceiba.AppHost.csproj` | Crear | Proyecto orquestador |
| `Ceiba.AppHost/Program.cs` | Crear | Definición de recursos |
| `Ceiba.ServiceDefaults/Ceiba.ServiceDefaults.csproj` | Crear | Proyecto de defaults |
| `Ceiba.ServiceDefaults/Extensions.cs` | Crear | Health checks, telemetría |
| `src/Ceiba.Web/Ceiba.Web.csproj` | Modificar | Agregar paquetes Aspire |
| `src/Ceiba.Web/Program.cs` | Modificar | Integrar ServiceDefaults |
| `Ceiba.sln` | Modificar | Agregar nuevos proyectos |

---

## Consideraciones Importantes

1. **PostgreSQL en contenedor vs local:**
   - Aspire creará un contenedor PostgreSQL nuevo
   - Los datos existentes en tu PostgreSQL local NO se migrarán automáticamente
   - Puedes seguir usando el PostgreSQL local ejecutando `dotnet run --project src/Ceiba.Web` directamente

2. **Migraciones EF Core:**
   - Se ejecutarán automáticamente al iniciar con Aspire
   - El contenedor empieza vacío, se aplicarán todas las migraciones

3. **Persistencia de datos:**
   - `WithDataVolume("ceiba-postgres-data")` mantiene los datos entre reinicios
   - `WithLifetime(ContainerLifetime.Persistent)` evita recrear el contenedor

4. **Dashboard de Aspire:**
   - Accesible en http://localhost:15888
   - Muestra logs, métricas, trazas de todos los servicios
