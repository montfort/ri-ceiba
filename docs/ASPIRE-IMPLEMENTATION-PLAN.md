# Plan de Implementación de Aspire 13.0 para Ceiba

> **Estado: ✅ IMPLEMENTADO** (actualizado 2025-12-10)

## Resumen de la Implementación

Aspire 13.0.2 ha sido integrado exitosamente en el proyecto Ceiba con las siguientes características:

- **Orquestación local** con PostgreSQL en contenedor Docker
- **Dashboard de observabilidad** con métricas, logs y trazas
- **Modo dual** - puede ejecutarse con Aspire o de forma tradicional
- **Health checks** integrados (`/health`, `/alive`)

---

## Contexto

| Aspecto | Estado |
|---------|--------|
| **Framework** | .NET 10.0 |
| **Aspire** | 13.0.2 (actualizado desde 9.0.0) |
| **Base de datos (Aspire)** | PostgreSQL en contenedor Docker |
| **Base de datos (tradicional)** | PostgreSQL local (localhost:5432) |
| **ORM** | Entity Framework Core 10.0 con Npgsql |

---

## Estructura de Proyectos Aspire

```
ri-ceiba/
├── Ceiba.AppHost/                    # Orquestador Aspire
│   ├── Ceiba.AppHost.csproj          # SDK: Aspire.AppHost.Sdk/13.0.2
│   ├── Program.cs                    # Definición de recursos
│   └── Properties/
│       └── launchSettings.json       # Perfiles de ejecución
│
├── Ceiba.ServiceDefaults/            # Configuración compartida
│   ├── Ceiba.ServiceDefaults.csproj  # Paquetes OpenTelemetry, Resilience
│   └── Extensions.cs                 # Health checks, telemetría
│
└── src/Ceiba.Web/                    # Aplicación web (usa ServiceDefaults)
    └── Program.cs                    # Modo dual Aspire/tradicional
```

---

## Paquetes Utilizados

### Ceiba.AppHost

```xml
<Project Sdk="Aspire.AppHost.Sdk/13.0.2">
  <PackageReference Include="Aspire.Hosting.PostgreSQL" Version="13.0.2" />
</Project>
```

### Ceiba.ServiceDefaults

```xml
<PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="10.0.0" />
<PackageReference Include="Microsoft.Extensions.ServiceDiscovery" Version="10.0.0" />
<PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.13.1" />
<PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.13.1" />
<PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.13.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.13.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.Runtime" Version="1.13.0" />
```

### Ceiba.Web

```xml
<PackageReference Include="Aspire.Npgsql.EntityFrameworkCore.PostgreSQL" Version="13.0.2" />
```

---

## Comandos de Ejecución

### Con Aspire (desarrollo orquestado)

```bash
# Opción 1: Usando dotnet run
dotnet run --project Ceiba.AppHost --launch-profile https

# Opción 2: Usando Aspire CLI (requiere instalación previa)
aspire run
```

**Resultado:**
- Inicia PostgreSQL en contenedor Docker
- Inicia Ceiba.Web conectado al contenedor
- Dashboard disponible en https://localhost:17157
- Web app disponible en el puerto asignado dinámicamente

### Sin Aspire (desarrollo tradicional)

```bash
dotnet run --project src/Ceiba.Web
```

**Resultado:**
- Usa PostgreSQL local (localhost:5432)
- Funciona como antes, sin orquestación

---

## Instalación de Aspire CLI (Opcional)

El CLI de Aspire facilita la ejecución pero no es obligatorio.

### Windows (PowerShell)

```powershell
irm https://aspire.dev/install.ps1 | iex
```

### Verificar instalación

```bash
aspire --version
# Debe mostrar: 13.0.2+...
```

---

## Configuración de Conexión Dual

El archivo `Program.cs` de Ceiba.Web detecta automáticamente el modo de ejecución:

```csharp
var aspireConnectionString = builder.Configuration.GetConnectionString("ceiba");
if (!string.IsNullOrEmpty(aspireConnectionString))
{
    // Aspire está orquestando - usar integración Aspire para PostgreSQL
    builder.AddNpgsqlDbContext<CeibaDbContext>("ceiba", settings =>
    {
        settings.DisableRetry = false;
    });
}
else
{
    // Ejecución standalone - usar configuración tradicional
    builder.Services.AddDbContext<CeibaDbContext>((sp, options) =>
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        options.UseNpgsql(connectionString, ...);
    });
}
```

---

## Health Checks

Los endpoints de health check están disponibles solo en desarrollo:

- `/health` - Estado general de la aplicación
- `/alive` - Verificación de que la aplicación responde

---

## Persistencia de Datos

El contenedor PostgreSQL mantiene datos entre reinicios gracias a:

```csharp
var postgres = builder.AddPostgres("postgres")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume("ceiba-postgres-data");
```

---

## Referencias

- [Aspire 13.0 - What's New](https://aspire.dev/whats-new/aspire-13/)
- [Aspire Documentation](https://aspire.dev/)
- [Aspire CLI Installation](https://aspire.dev/get-started/install-cli/)
