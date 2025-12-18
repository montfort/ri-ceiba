# Arquitectura del Sistema

El sistema Ceiba sigue una arquitectura **Monolítica Modular** con principios de Clean Architecture y organización Domain-Driven Design (DDD).

## Visión General

```
┌─────────────────────────────────────────────────────────────┐
│                        Ceiba.Web                             │
│                    (Blazor Server UI)                        │
├─────────────────────────────────────────────────────────────┤
│                    Ceiba.Application                         │
│                   (Servicios de Aplicación)                  │
├─────────────────────────────────────────────────────────────┤
│                      Ceiba.Core                              │
│              (Entidades, Interfaces, Enums)                  │
├─────────────────────────────────────────────────────────────┤
│                   Ceiba.Infrastructure                       │
│           (EF Core, Servicios Externos, Email)               │
├─────────────────────────────────────────────────────────────┤
│                      Ceiba.Shared                            │
│                  (DTOs, Constantes)                          │
└─────────────────────────────────────────────────────────────┘
```

## Capas del Sistema

### Ceiba.Core (Dominio)

La capa central que contiene:

- **Entities/**: Entidades del dominio
  - `Usuario.cs` - Usuario del sistema (hereda de IdentityUser)
  - `ReporteIncidencia.cs` - Reporte de incidencia
  - `Zona.cs`, `Region.cs`, `Sector.cs`, `Cuadrante.cs` - Catálogos geográficos
  - `RegistroAuditoria.cs` - Log de auditoría
  - `ReporteAutomatizado.cs` - Reporte generado por IA
  - `PlantillaReporte.cs` - Plantilla de generación

- **Interfaces/**: Contratos de servicios
- **Enums/**: Enumeraciones del dominio
- **Exceptions/**: Excepciones personalizadas

```csharp
// Ejemplo de entidad
public class ReporteIncidencia
{
    public int Id { get; set; }
    public string TipoReporte { get; set; } = "A";
    public DateTime DatetimeHechos { get; set; }
    public int Estado { get; set; } // 0=Borrador, 1=Entregado
    public Guid CreadorId { get; set; }
    // ... más propiedades
}
```

### Ceiba.Application (Aplicación)

Servicios que orquestan la lógica de negocio:

- **Services/**: Implementaciones de servicios
  - `ReportService.cs` - CRUD de reportes
  - `UserManagementService.cs` - Gestión de usuarios
  - `CatalogService.cs` - Gestión de catálogos
  - `AuditService.cs` - Registro de auditoría
  - `ExportService.cs` - Exportación PDF/JSON

- **DTOs/**: Objetos de transferencia (en Ceiba.Shared)

```csharp
// Ejemplo de servicio
public interface IReportService
{
    Task<ReportDto> CreateReportAsync(CreateReportDto dto, Guid userId);
    Task<ReportDto> GetReportByIdAsync(int id, Guid userId, bool isRevisor);
    Task<PagedResult<ReportDto>> ListReportsAsync(ReportFilterDto filter, Guid userId, bool isRevisor);
}
```

### Ceiba.Infrastructure (Infraestructura)

Implementaciones técnicas y acceso a datos:

- **Data/**
  - `CeibaDbContext.cs` - Contexto de Entity Framework
  - `Migrations/` - Migraciones de base de datos
  - `Configurations/` - Configuraciones de Fluent API
  - `Seeding/` - Datos iniciales

- **Services/**
  - `PdfExportService.cs` - Generación de PDF con QuestPDF
  - `JsonExportService.cs` - Exportación JSON
  - `EmailService.cs` - Envío de emails con MailKit
  - `AiNarrativeService.cs` - Integración con IA

- **Repositories/** - Repositorios de datos (opcional)

### Ceiba.Web (Presentación)

Interfaz de usuario con Blazor Server:

- **Components/**
  - `Layout/` - Layouts compartidos
  - `Pages/` - Páginas por módulo
    - `Auth/` - Login, Logout
    - `Reports/` - Gestión de reportes
    - `Admin/` - Administración
    - `Supervisor/` - Panel de revisores
    - `Automated/` - Reportes automatizados
  - `Shared/` - Componentes reutilizables

- **Program.cs** - Configuración de servicios

### Ceiba.Shared

DTOs y constantes compartidos entre capas:

- **DTOs/** - Data Transfer Objects
- **Constants/** - Constantes del sistema

## Principios de Diseño

### Dependency Rule

Las dependencias apuntan hacia adentro:
- Web → Application → Core ← Infrastructure

### Inversión de Dependencias

- Core define interfaces
- Infrastructure implementa interfaces
- Application usa abstracciones

```csharp
// En Core
public interface IEmailService
{
    Task SendAsync(string to, string subject, string body);
}

// En Infrastructure
public class MailKitEmailService : IEmailService
{
    public async Task SendAsync(string to, string subject, string body)
    {
        // Implementación con MailKit
    }
}
```

## Módulos Funcionales

| Módulo | Descripción | Roles |
|--------|-------------|-------|
| Authentication | Login, sesión, autorización | Todos |
| IncidentReports | CRUD de reportes | CREADOR, REVISOR |
| SupervisorReview | Revisión y exportación | REVISOR |
| Administration | Usuarios y catálogos | ADMIN |
| Audit | Registro de operaciones | ADMIN |
| AutomatedReports | Generación con IA | REVISOR |

## Tecnologías Clave

| Componente | Tecnología |
|------------|------------|
| Framework | ASP.NET Core 10 |
| UI | Blazor Server |
| ORM | Entity Framework Core |
| Base de Datos | PostgreSQL 18 |
| Autenticación | ASP.NET Identity |
| PDF | QuestPDF |
| Email | MailKit |
| Testing | xUnit, bUnit, Playwright |

## Próximos Pasos

- [Estándares de código](Dev-Estandares-Codigo)
- [Modelo de datos](Dev-Base-de-Datos)
- [Guía de testing](Dev-Testing-TDD)
