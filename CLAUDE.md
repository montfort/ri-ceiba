# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Ceiba - Reportes de Incidencias**: ASP.NET Core 10 + Blazor Server web application for incident reporting.

**Status**: Specification completed, ready for implementation (Branch: 001-incident-management-system)

**Core Modules**:
1. **Authentication** - ASP.NET Identity, RBAC, session management (30 min timeout)
2. **IncidentReports** - Report CRUD, state transitions (Borrador → Entregado), Type A forms
3. **SupervisorReview** - Report viewing, editing, PDF/JSON export, bulk operations
4. **Administration** - User management, catalog configuration (Zona/Sector/Cuadrante), system config
5. **Audit** - Comprehensive operation tracking with indefinite retention
6. **AutomatedReports** - AI-powered daily report generation with email delivery

## AI Assistant Instructions

### Context7 Integration (Mandatory)

**ALWAYS use Context7 MCP server for library documentation** when implementing code that uses:

- **ASP.NET Core 10** - Controllers, middleware, dependency injection, configuration
- **Entity Framework Core** - DbContext, migrations, relationships, queries
- **Blazor Server** - Components, state management, JavaScript interop, SignalR
- **ASP.NET Identity** - Authentication, authorization, user management, roles
- **PostgreSQL/Npgsql** - Database features, data types, performance optimization
- **QuestPDF** - Document generation, layouts, styling
- **MailKit** - Email sending, SMTP configuration
- **xUnit** - Test patterns, assertions, fixtures
- **bUnit** - Blazor component testing patterns
- **Playwright** - E2E testing, browser automation

**Process**:
1. BEFORE writing implementation code, use Context7 to fetch current API documentation
2. Verify syntax, patterns, and best practices from official sources
3. Use examples from documentation to guide implementation
4. When uncertain about an API, ALWAYS query Context7 first

**Example**:
```bash
# When implementing EF Core migrations
1. Query Context7 to resolve "Entity Framework Core"
2. Query Context7 docs with topic "migrations"
3. Use returned documentation for implementation
```

This ensures code uses latest APIs and follows current best practices.

## Technology Stack

- **Backend**: ASP.NET Core 10, Blazor Server (SSR), C#
- **Database**: PostgreSQL 18 with Entity Framework Core
- **Deployment**: Docker + Docker Compose on Fedora Linux Server 42
- **Authentication**: ASP.NET Identity with RBAC (CREADOR, REVISOR, ADMIN roles)
- **PDF Generation**: QuestPDF or similar
- **Email**: MailKit (SMTP)
- **AI Integration**: Provider-agnostic abstraction layer (OpenAI/Azure OpenAI/local LLM)
- **Testing**: xUnit, bUnit (Blazor), FluentAssertions, Playwright (E2E)

## Architecture

### Pattern
Monolithic Modular Architecture with Clean Architecture principles and Domain-Driven Design organization.

### Project Structure

```text
src/
├── Ceiba.Web/                    # Blazor Server (presentation layer)
│   ├── Components/
│   │   ├── Layout/
│   │   └── Pages/
│   │       ├── Auth/
│   │       ├── Reports/
│   │       ├── Admin/
│   │       └── Automated/
│   ├── Program.cs
│   └── appsettings.json
│
├── Ceiba.Core/                   # Domain layer (entities, interfaces)
│   ├── Entities/
│   │   ├── Usuario.cs
│   │   ├── ReporteIncidencia.cs
│   │   ├── Zona.cs, Sector.cs, Cuadrante.cs
│   │   ├── RegistroAuditoria.cs
│   │   └── ReporteAutomatizado.cs
│   ├── Interfaces/
│   └── Enums/
│
├── Ceiba.Application/            # Application services
│   ├── Services/
│   │   ├── IReportService.cs
│   │   ├── IUserService.cs
│   │   └── IAuditService.cs
│   └── DTOs/
│
├── Ceiba.Infrastructure/         # Data access, external services
│   ├── Data/
│   │   ├── CeibaDbContext.cs
│   │   ├── Migrations/
│   │   └── Configurations/
│   ├── Services/
│   │   ├── PdfExportService.cs (QuestPDF)
│   │   ├── EmailService.cs (MailKit)
│   │   └── AiNarrativeService.cs (abstraction layer)
│   └── Repositories/
│
└── Ceiba.Shared/                 # Shared DTOs, constants

tests/
├── Ceiba.Core.Tests/             # Unit tests (TDD)
├── Ceiba.Application.Tests/      # Service tests
├── Ceiba.Infrastructure.Tests/   # Repository/integration tests
├── Ceiba.Web.Tests/              # Blazor component tests (bUnit)
└── Ceiba.Integration.Tests/      # E2E tests (Playwright)

specs/001-incident-management-system/
├── spec.md                       # Feature specification (4 User Stories)
├── plan.md                       # Implementation plan
├── data-model.md                 # Database schema
├── research.md                   # Technology research
├── quickstart.md                 # Developer guide
├── tasks.md                      # 330+ implementation tasks
└── contracts/                    # OpenAPI specs
    ├── api-auth.yaml
    ├── api-reports.yaml
    ├── api-admin.yaml
    └── api-audit.yaml
```

## User Roles & Permissions

| Role | Reports | Export | Users | Catalogs | Audit | Automated Reports |
|------|---------|--------|-------|----------|-------|-------------------|
| **CREADOR** | ✅ Create/Edit own<br>❌ View others | ❌ | ❌ | ❌ | ❌ | ❌ |
| **REVISOR** | ✅ View/Edit all | ✅ PDF/JSON | ❌ | ❌ | ❌ | ✅ View/Configure |
| **ADMIN** | ❌ No access | ❌ | ✅ CRUD | ✅ Configure | ✅ View logs | ❌ |

### CREADOR (Police Officers)
- Create new incident reports (Type A only)
- Edit own reports while in "Borrador" state
- Submit reports (Borrador → Entregado)
- View own report history
- **Cannot** edit after submission
- **Cannot** view other users' reports

### REVISOR (Supervisors)
- View ALL reports (any user, any state)
- Edit any report (including submitted ones)
- Export reports to PDF and JSON (single or bulk)
- Configure automated report templates
- View and download automated reports
- **Cannot** manage users or catalogs

### ADMIN (Technical Administrators)
- Create, suspend, delete users
- Assign roles (can assign multiple roles to one user)
- Configure catalogs: Zona, Sector, Cuadrante
- Configure suggestion lists (Sexo, Delito, TipoDeAtencion, etc.)
- View audit logs with filtering
- **Cannot** access incident reports module

## Project Constitution (Non-Negotiable Principles)

Located at `.specify/memory/constitution.md`:

1. **Modular Design**: Self-contained modules, public API contracts only, no internal cross-dependencies
2. **TDD Mandatory**: Write tests BEFORE implementation, Red-Green-Refactor cycle
3. **Security by Design**: Least privilege, OWASP Top 10, audit logging on all critical actions
4. **Accessibility**: Mobile-first, WCAG Level AA minimum
5. **Documentation as Deliverable**: All modules, APIs, and entities must be documented

## Development Workflow (Speckit Framework)

The project uses the Speckit framework for structured feature development:

### Phase 1: Requirements & Specification ✅ COMPLETED
```bash
/speckit.clarify    # ✅ 5 clarifications documented
/speckit.specify    # ✅ spec.md with 4 User Stories (P1-P4)
```

**Output**: `specs/001-incident-management-system/spec.md`

### Phase 2: Design & Planning ✅ COMPLETED
```bash
/speckit.plan       # ✅ plan.md with architecture & modules
```

**Outputs**:
- `plan.md` - Implementation strategy, constitution check
- `data-model.md` - ER diagram, entities, relationships
- `research.md` - Technology decisions (QuestPDF, MailKit, AI abstraction)
- `quickstart.md` - Development setup guide
- `contracts/` - OpenAPI 3.0 API specifications

### Phase 3: Task Breakdown ✅ COMPLETED
```bash
/speckit.tasks      # ✅ 330+ tasks generated
```

**Output**: `tasks.md` with dependency-ordered tasks organized by module

### Phase 4: Implementation ⏳ READY
```bash
/speckit.implement  # Execute implementation with TDD
```

**Expected**: Test-first development of all 330+ tasks

### Phase 5: Quality Assurance
```bash
/speckit.analyze    # Cross-artifact consistency check
/speckit.checklist  # Custom validation checklist
```

### Optional: GitHub Integration
```bash
/speckit.taskstoissues  # Convert tasks to GitHub Issues (requires gh auth)
```

## Key Files

### Project Documentation
- **Feature Spec**: `specs/001-incident-management-system/spec.md` - 4 User Stories (P1-P4)
- **Implementation Plan**: `specs/001-incident-management-system/plan.md` - Architecture & phases
- **Data Model**: `specs/001-incident-management-system/data-model.md` - ER diagram & schema
- **Tasks**: `specs/001-incident-management-system/tasks.md` - 330+ tasks organized by module
- **Quickstart Guide**: `specs/001-incident-management-system/quickstart.md` - Setup & development

### Configuration & Governance
- **Constitution**: `.specify/memory/constitution.md` - 5 non-negotiable principles
- **Templates**: `.specify/templates/` - Spec, plan, tasks, checklist templates
- **Validation**: `.validation/FINAL-VALIDATION.md` - Environment validation results

### API Contracts
- `specs/001-incident-management-system/contracts/api-*.yaml` - OpenAPI 3.0 specifications

### Original Requirements
- **Project Requirements**: `preeliminares/proyecto.md` - Original specification
- **ER Diagram**: `preeliminares/diagrama_relaciones.mmd` - Database relationships

## Naming Conventions

### C# Code
- Classes/Properties/Methods: **PascalCase**
- Local variables/parameters: **camelCase**
- Private fields: **_camelCase** (with underscore prefix)
- Interfaces: **IPascalCase** (with "I" prefix)
- Constants: **UPPER_SNAKE_CASE**

### Domain Entities (Core Layer)
- `Usuario` - User entity with ASP.NET Identity integration
- `ReporteIncidencia` - Main incident report (Type A)
- `Zona`, `Sector`, `Cuadrante` - Geographic hierarchical catalogs
- `SugerenciaReporte` - Configurable field suggestions
- `RegistroAuditoria` - Audit log entries
- `ReporteAutomatizado` - Automated daily reports
- `PlantillaReporte` - Report templates for automation

### Database
- Tables: **UPPER_SNAKE_CASE** (e.g., REPORTE_INCIDENCIA)
- Columns: **lower_snake_case**
- Foreign keys: column name + `_FK` suffix
- Timestamps: **TIMESTAMPTZ** for UTC compliance

### Blazor Components
- Components: **PascalCase.razor** (e.g., CreateReportForm.razor)
- Pages: **PascalCase.razor** in Components/Pages/
- Layouts: **PascalCase.razor** in Components/Layout/

## Build & Deployment Commands

### Development
```bash
# Restore & build
dotnet restore
dotnet build

# Run with hot reload
dotnet watch run --project src/Ceiba.Web

# Run tests
dotnet test

# Code formatting
dotnet format
```

### Database Migrations
```bash
# Create migration
cd src/Ceiba.Infrastructure
dotnet ef migrations add <MigrationName> --startup-project ../Ceiba.Web

# Apply migration
dotnet ef database update --startup-project ../Ceiba.Web

# Rollback migration
dotnet ef database update <PreviousMigration> --startup-project ../Ceiba.Web

# Generate SQL script
dotnet ef migrations script --startup-project ../Ceiba.Web --output migration.sql
```

### Docker Operations
```bash
# Start PostgreSQL only
docker compose up -d ceiba-db

# Start full stack
docker compose up -d

# Build application image
docker build -t ceiba-web -f docker/Dockerfile .

# View logs
docker compose logs -f ceiba-web

# Backup database
docker exec ceiba-db pg_dump -U ceiba ceiba | gzip > backup_$(date +%Y%m%d).sql.gz

# Restore database
gunzip -c backup_20250118.sql.gz | docker exec -i ceiba-db psql -U ceiba ceiba
```

### Production Deployment (Fedora 42)
```bash
# Deploy with compose
docker compose -f docker-compose.prod.yml up -d

# Check status
docker compose -f docker-compose.prod.yml ps

# View logs
docker compose -f docker-compose.prod.yml logs -f
```

## Testing Strategy (TDD Mandatory)

### Test Categories
```bash
# Unit tests (Core layer)
dotnet test --filter "Category=Unit"

# Service tests (Application layer)
dotnet test --filter "Category=Service"

# Integration tests (requires PostgreSQL)
dotnet test --filter "Category=Integration"

# Component tests (Blazor with bUnit)
dotnet test --filter "Category=Component"

# E2E tests (Playwright)
dotnet test --filter "Category=E2E"
```

### Test-First Cycle (Red-Green-Refactor)
1. **RED**: Write failing test for new functionality
2. **GREEN**: Write minimal code to pass test
3. **REFACTOR**: Improve code while keeping tests green
4. **REPEAT**: For every feature, no exceptions

### Coverage Requirements
- **Core Layer**: 90%+ unit test coverage
- **Application Layer**: 80%+ service test coverage
- **Infrastructure Layer**: 70%+ integration test coverage
- **Web Layer**: Key user flows with component tests

### Test Frameworks
- **xUnit**: Core test framework
- **bUnit**: Blazor component testing
- **FluentAssertions**: Readable assertions
- **Playwright**: E2E browser automation
- **Testcontainers** (optional): PostgreSQL for integration tests

## Configuration & Environment

### Required Environment Variables (Production)
```bash
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=<postgres-connection-string>
Email__Host=<smtp-host>
Email__Port=587
Email__Username=<smtp-user>
Email__Password=<smtp-password>
AI__Provider=OpenAI|AzureOpenAI|Local
AI__ApiKey=<api-key>
AI__Model=gpt-4
AutomatedReports__GenerationTime=06:00:00
AutomatedReports__Recipients=["email1@example.com","email2@example.com"]
```

### Default Development Credentials
- **Database**: Host=localhost, DB=ceiba, User=ceiba, Pass=ceiba123
- **Admin User**: admin@ceiba.local / Admin123! ⚠️ Change after first login!

### Session & Security Settings
- **Session Timeout**: 30 minutes of inactivity
- **Password Policy**: Min 10 chars, require uppercase + digit
- **Audit Retention**: Indefinite (never delete)
- **Timestamp Standard**: UTC (TIMESTAMPTZ in PostgreSQL)

## Critical Reminders

1. **TDD is mandatory** - No exceptions. Tests written before implementation.
2. **Constitution supersedes all** - Conflicts resolved by constitution principles
3. **Audit everything** - All user actions logged to AUDITORIA table
4. **Module isolation** - Test each module independently
5. **RBAC strict** - Users see ONLY what their role permits
6. **Spanish UI** - Application interface in Spanish
7. **Mobile-first** - Design for mobile, enhance for desktop
8. **Security first** - OWASP Top 10, least privilege, input validation
9. **Documentation required** - Every module, API, and entity documented
10. **UTC timestamps** - All dates/times in UTC (TIMESTAMPTZ)
