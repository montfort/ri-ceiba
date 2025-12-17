# Flujo de Credenciales de Base de Datos en Aspire

*Documentación del mecanismo por el cual Aspire pasa las credenciales de PostgreSQL a la aplicación web.*

---

## Resumen

Aspire genera automáticamente contraseñas seguras para PostgreSQL y las inyecta en las aplicaciones dependientes mediante variables de entorno. La aplicación nunca necesita conocer ni gestionar la contraseña directamente.

---

## Flujo Completo

### 1. AppHost genera la contraseña (Ceiba.AppHost/Program.cs)

```csharp
var postgres = builder.AddPostgres("postgres")  // ← Genera contraseña aleatoria
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume("ceiba-postgres-data");

var database = postgres.AddDatabase("ceiba");   // ← Crea la DB "ceiba"

builder.AddProject<Projects.Ceiba_Web>("ceiba-web")
    .WithReference(database)                    // ← Inyecta connection string
    .WaitFor(database);
```

### 2. Aspire inyecta variables de entorno

Cuando Aspire inicia `Ceiba.Web`, automáticamente inyecta:

```bash
ConnectionStrings__ceiba=Host=localhost;Port=5432;Database=ceiba;Username=postgres;Password=<RANDOM_PASSWORD>
```

El formato `ConnectionStrings__ceiba` se traduce automáticamente por el sistema de configuración de .NET a:

```json
{
  "ConnectionStrings": {
    "ceiba": "Host=localhost;Port=..."
  }
}
```

### 3. Ceiba.Web lee la configuración (Program.cs)

```csharp
// Detecta si hay connection string de Aspire
var aspireConnectionString = builder.Configuration.GetConnectionString("ceiba");

if (!string.IsNullOrEmpty(aspireConnectionString))
{
    // Usa integración Aspire para PostgreSQL
    builder.AddNpgsqlDbContext<CeibaDbContext>("ceiba", ...);
    Log.Information("Using Aspire-orchestrated PostgreSQL connection");
}
else
{
    // Ejecución standalone - usa "DefaultConnection" de appsettings.json
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    ...
}
```

---

## Diagrama del Flujo

```
┌─────────────────────────────────────────────────────────────────┐
│                        Aspire AppHost                           │
├─────────────────────────────────────────────────────────────────┤
│  1. AddPostgres("postgres")                                     │
│     └── Genera: Password = "xK9#mP2$nL7..." (aleatorio)        │
│                                                                 │
│  2. AddDatabase("ceiba")                                        │
│     └── Crea DB en el contenedor PostgreSQL                    │
│                                                                 │
│  3. WithReference(database)                                     │
│     └── Construye connection string completo                   │
│                                                                 │
│  4. Al iniciar Ceiba.Web, inyecta:                             │
│     ConnectionStrings__ceiba=Host=...;Password=xK9#mP2$nL7...  │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                         Ceiba.Web                               │
├─────────────────────────────────────────────────────────────────┤
│  Configuration.GetConnectionString("ceiba")                     │
│     └── Lee la variable de entorno inyectada                   │
│                                                                 │
│  AddNpgsqlDbContext<CeibaDbContext>("ceiba")                   │
│     └── Configura EF Core con ese connection string            │
└─────────────────────────────────────────────────────────────────┘
```

---

## Resumen de Características

| Aspecto | Detalle |
|---------|---------|
| **Generación** | Aspire genera una contraseña aleatoria segura al crear el contenedor |
| **Almacenamiento** | La contraseña se almacena en el volumen persistente del contenedor |
| **Transmisión** | Se pasa vía variables de entorno al proceso hijo |
| **Seguridad** | Nunca se escribe en archivos de configuración ni logs |
| **Consistencia** | `WithLifetime(ContainerLifetime.Persistent)` mantiene la misma contraseña entre reinicios |

---

## Regeneración de Contraseña

La contraseña solo se regenera si eliminas el volumen Docker:

```bash
# Detener contenedores
docker stop postgres-XXXXX

# Eliminar contenedor y volumen
docker rm postgres-XXXXX
docker volume rm ceiba-postgres-data
```

En ese caso, Aspire genera una nueva contraseña para el nuevo contenedor al reiniciar.

---

## Modo Standalone (sin Aspire)

Cuando la aplicación se ejecuta sin Aspire (por ejemplo, en producción con Docker Compose), el connection string se lee de `appsettings.json` o variables de entorno:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=db;Database=ceiba;Username=ceiba;Password=..."
  }
}
```

El código en `Program.cs` detecta automáticamente qué modo usar basándose en la presencia del connection string `"ceiba"` (Aspire) vs `"DefaultConnection"` (standalone).

---

*Documento generado como parte de la documentación de arquitectura del proyecto Ceiba.*
