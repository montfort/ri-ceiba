# Implementation Plan: Sistema de Gestión de Reportes de Incidencias

**Branch**: `001-incident-management-system` | **Date**: 2025-11-18 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-incident-management-system/spec.md`

## Summary

Sistema web completo para la gestión de reportes de incidencias de la Unidad Especializada en Género de la SSC CDMX. Implementa tres roles (CREADOR, REVISOR, ADMIN) con flujos de trabajo diferenciados para creación, revisión y administración de reportes. Incluye auditoría completa, campos configurables jerárquicos (zona/sector/cuadrante), exportación PDF/JSON, y generación automática de resúmenes diarios con IA.

Enfoque técnico: ASP.NET Core 10 con Blazor Server siguiendo arquitectura de Monolito Modular y Clean Architecture. PostgreSQL 18 para persistencia. Despliegue con Docker en Fedora Linux Server 42.

## Technical Context

**Language/Version**: C# / .NET 10, ASP.NET Core 10, Blazor Server
**Primary Dependencies**: ASP.NET Identity (auth), Entity Framework Core (ORM), QuestPDF o similar (PDF generation), Markdig (Markdown processing), MailKit (SMTP email), AI abstraction layer (OpenAI/Azure OpenAI/local LLM support)
**Storage**: PostgreSQL 18
**Testing**: xUnit, bUnit (Blazor components), FluentAssertions
**Target Platform**: Fedora Linux Server 42 (Docker), navegadores modernos (Chrome, Firefox, Edge, Safari)
**Project Type**: Web application (Blazor Server - monolito modular)
**Performance Goals**: <5 min para completar reporte, <10 seg búsqueda en 1000+ registros, 50 usuarios concurrentes, 99.5% disponibilidad horario laboral
**Constraints**: Mobile-first responsive, WCAG AA accessibility, OWASP Top 10 compliance, session timeout 30 min
**Integration Strategy**: AI via provider-agnostic abstraction (interface-based, supports OpenAI/Azure OpenAI/local LLM backends); Email via SMTP with environment-based configuration (host, port, credentials)
**Scale/Scope**: ~50 usuarios concurrentes, miles de reportes, arquitectura extensible para futuros tipos de reporte

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### I. Arquitectura Modular Orientada al Dominio
- **Status**: ✅ PASS
- **Evidence**: Sistema diseñado con módulos autocontenidos: Authentication, IncidentReports, Administration, Audit, AutomatedReports. Comunicación via contratos de servicio públicos.

### II. Test-First (No Negociable)
- **Status**: ✅ PASS
- **Evidence**: Plan incluye xUnit + bUnit para TDD. Cada módulo tendrá pruebas unitarias, de integración y de contrato antes de implementación.

### III. Seguridad y Auditabilidad por Diseño
- **Status**: ✅ PASS
- **Evidence**: RBAC con ASP.NET Identity, auditoría completa de todas las operaciones (FR-031 a FR-035b), política de contraseñas moderada, session timeout 30 min, retención indefinida de logs.

### IV. Interfaz Adaptable y Accesible
- **Status**: ✅ PASS
- **Evidence**: Requisitos explícitos de mobile-first y WCAG AA (SC-009, Out of Scope confirma web responsiva).

### VI. Documentación como Entregable
- **Status**: ✅ PASS
- **Evidence**: Plan genera research.md, data-model.md, contracts/, quickstart.md. Cada módulo documentado.

**Note**: Constitution defines 5 principles (I, II, III, IV, VI). Principle V is intentionally omitted per constitution.md.

**Gate Result**: All 5 principles satisfied. Proceeding to Phase 0.

## Project Structure

### Documentation (this feature)

```text
specs/001-incident-management-system/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   ├── api-auth.yaml
│   ├── api-reports.yaml
│   ├── api-admin.yaml
│   └── api-audit.yaml
└── tasks.md             # Phase 2 output
```

### Source Code (repository root)

```text
src/
├── Ceiba.Web/                    # Blazor Server application
│   ├── Components/               # Razor components
│   │   ├── Layout/
│   │   ├── Pages/
│   │   │   ├── Auth/
│   │   │   ├── Reports/
│   │   │   ├── Admin/
│   │   │   └── Automated/
│   │   └── Shared/
│   ├── Program.cs
│   └── appsettings.json
│
├── Ceiba.Core/                   # Domain layer (entities, interfaces)
│   ├── Entities/
│   │   ├── Usuario.cs
│   │   ├── ReporteIncidencia.cs
│   │   ├── Zona.cs
│   │   ├── Sector.cs
│   │   ├── Cuadrante.cs
│   │   ├── RegistroAuditoria.cs
│   │   └── ReporteAutomatizado.cs
│   ├── Interfaces/
│   └── Enums/
│
├── Ceiba.Application/            # Application services
│   ├── Services/
│   │   ├── AuthService.cs
│   │   ├── ReportService.cs
│   │   ├── AdminService.cs
│   │   ├── AuditService.cs
│   │   └── AutomatedReportService.cs
│   ├── DTOs/
│   └── Validators/
│
├── Ceiba.Infrastructure/         # Data access, external services
│   ├── Data/
│   │   ├── CeibaDbContext.cs
│   │   └── Migrations/
│   ├── Repositories/
│   ├── ExternalServices/
│   │   ├── EmailService.cs
│   │   └── AIService.cs
│   └── Identity/
│
└── Ceiba.Shared/                 # Shared DTOs, constants
    ├── Constants/
    └── DTOs/

tests/
├── Ceiba.Core.Tests/
├── Ceiba.Application.Tests/
├── Ceiba.Infrastructure.Tests/
├── Ceiba.Web.Tests/              # bUnit component tests
└── Ceiba.Integration.Tests/

docker/
├── Dockerfile              # RT-006: Includes Pandoc installation (dnf install pandoc)
├── docker-compose.yml
└── docker-compose.override.yml

scripts/
├── backup/
│   └── backup-db.sh
└── deploy/
```

**Structure Decision**: Monolito modular con Clean Architecture en capas (Core → Application → Infrastructure → Web). Esta estructura permite:
- Separación clara de responsabilidades
- Testabilidad independiente por capa
- Preparación para eventual separación en microservicios si escala requiere
- Cumplimiento del principio constitucional de módulos autocontenidos

### Orchestration Strategy

**Development**: .NET ASPIRE AppHost (`src/Ceiba.AppHost/`) - Local development orchestration with service discovery, hot reload, and integrated debugging (F5 experience)

**Production**: Docker Compose (`docker/`) - Container orchestration on Fedora Linux Server 42 with PostgreSQL, backup volumes, and HTTPS reverse proxy

**Rationale**: ASPIRE is used exclusively for local development productivity. Production deployments use standard Docker Compose for simplicity, operational familiarity, and compatibility with existing infrastructure. ASPIRE AppHost is NOT deployed to production servers.

## Complexity Tracking

No violations identified. All constitutional principles satisfied with standard patterns.
