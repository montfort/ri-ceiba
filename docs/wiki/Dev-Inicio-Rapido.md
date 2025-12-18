# Inicio Rápido para Desarrolladores

Esta guía te ayuda a configurar tu entorno de desarrollo para contribuir al proyecto Ceiba.

## Requisitos Previos

### Software Necesario

| Herramienta | Versión | Descripción |
|-------------|---------|-------------|
| .NET SDK | 10.0+ | SDK de desarrollo |
| PostgreSQL | 18+ | Base de datos |
| Docker | 24+ | Contenedores (opcional) |
| Git | 2.40+ | Control de versiones |
| IDE | VS Code / Rider / VS | Editor de código |

### Extensiones Recomendadas (VS Code)

- C# Dev Kit
- .NET Extension Pack
- PostgreSQL
- Docker

## Clonar el Repositorio

```bash
git clone https://github.com/org/ceiba.git
cd ceiba
```

## Configurar la Base de Datos

### Opción 1: Docker (Recomendada)

```bash
# Iniciar PostgreSQL con Docker
docker compose up -d ceiba-db

# Verificar que está corriendo
docker compose ps
```

### Opción 2: PostgreSQL Local

1. Instala PostgreSQL 18+
2. Crea la base de datos:

```sql
CREATE DATABASE ceiba;
CREATE USER ceiba WITH PASSWORD 'tu_password';
GRANT ALL PRIVILEGES ON DATABASE ceiba TO ceiba;
```

## Configurar la Aplicación

### Variables de Entorno

Crea un archivo `.env` o configura en `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=ceiba;Username=ceiba;Password=tu_password"
  }
}
```

### Restaurar Dependencias

```bash
dotnet restore
```

### Aplicar Migraciones

```bash
cd src/Ceiba.Infrastructure
dotnet ef database update --startup-project ../Ceiba.Web
```

## Ejecutar la Aplicación

### Modo Desarrollo

```bash
cd src/Ceiba.Web
dotnet watch run
```

La aplicación estará disponible en:
- **HTTP**: http://localhost:5000
- **HTTPS**: https://localhost:5001

### Con Hot Reload

El comando `dotnet watch run` habilita hot reload automáticamente para cambios en:
- Archivos Razor
- Código C#
- CSS

## Ejecutar Tests

```bash
# Todos los tests
dotnet test

# Tests de un proyecto específico
dotnet test tests/Ceiba.Core.Tests

# Con cobertura
dotnet test --collect:"XPlat Code Coverage"
```

## Estructura del Proyecto

```
src/
├── Ceiba.Web/           # Blazor Server (UI)
├── Ceiba.Core/          # Entidades y contratos
├── Ceiba.Application/   # Servicios de aplicación
├── Ceiba.Infrastructure/# Data access, servicios externos
└── Ceiba.Shared/        # DTOs compartidos

tests/
├── Ceiba.Core.Tests/
├── Ceiba.Application.Tests/
├── Ceiba.Infrastructure.Tests/
└── Ceiba.Web.Tests/
```

## Flujo de Desarrollo

### 1. Crear Rama

```bash
git checkout -b feature/mi-funcionalidad
```

### 2. Desarrollar con TDD

1. Escribe el test que falla (Red)
2. Implementa el código mínimo (Green)
3. Refactoriza (Refactor)

### 3. Commit

```bash
git add .
git commit -m "feat: descripción del cambio"
```

### 4. Push y PR

```bash
git push -u origin feature/mi-funcionalidad
# Crear Pull Request en GitHub
```

## Usuario de Prueba

Al ejecutar por primera vez, se crea un admin por defecto:
- **Email**: admin@ceiba.local
- **Password**: Admin123!

> **Advertencia:** Cambia esta contraseña en producción.

## Comandos Útiles

| Comando | Descripción |
|---------|-------------|
| `dotnet build` | Compilar el proyecto |
| `dotnet test` | Ejecutar tests |
| `dotnet watch run` | Ejecutar con hot reload |
| `dotnet ef migrations add` | Crear migración |
| `dotnet ef database update` | Aplicar migraciones |
| `dotnet format` | Formatear código |

## Próximos Pasos

- [[Dev-Arquitectura|Entender la arquitectura]]
- [[Dev-Estandares-Codigo|Revisar estándares de código]]
- [[Dev-Testing-TDD|Guía de testing TDD]]
