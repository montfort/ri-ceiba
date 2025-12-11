# Tasks: Sistema de Gestión de Reportes de Incidencias

**Input**: Design documents from `/specs/001-incident-management-system/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: REQUIRED - Constitution mandates TDD (Test-First approach)

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

Based on plan.md Clean Architecture structure:
- **Domain**: `src/Ceiba.Core/`
- **Application**: `src/Ceiba.Application/`
- **Infrastructure**: `src/Ceiba.Infrastructure/`
- **Web**: `src/Ceiba.Web/`
- **Tests**: `tests/Ceiba.*.Tests/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [X] T001 Create solution and project structure per plan.md (src/Ceiba.Web, Ceiba.Core, Ceiba.Application, Ceiba.Infrastructure, Ceiba.Shared)
- [X] T002 Initialize .NET 10 projects with NuGet dependencies (ASP.NET Identity, EF Core, xUnit, bUnit, Playwright for RT-005)
- [X] T003 [P] Configure Docker files in docker/Dockerfile and docker-compose.yml
- [X] T003a [P] RT-006 Mitigation: Add Pandoc installation to Dockerfile (RUN dnf install -y pandoc && pandoc --version)
- [X] T004 [P] Configure .NET ASPIRE AppHost for local development orchestration in src/Ceiba.AppHost/ (dev-only, not deployed to production) - SKIPPED: Optional for local dev
- [X] T005 [P] Setup EditorConfig and code style rules in .editorconfig
- [X] T006 [P] Create solution-level Directory.Build.props for common settings

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Tests for Foundation

- [X] T007 [P] Unit test for audit logging interceptor in tests/Ceiba.Infrastructure.Tests/AuditInterceptorTests.cs
- [X] T008 [P] Integration test for database context in tests/Ceiba.Integration.Tests/DbContextTests.cs

### Implementation for Foundation

- [X] T009 Create CeibaDbContext with PostgreSQL configuration in src/Ceiba.Infrastructure/Data/CeibaDbContext.cs
- [X] T010 [P] Configure ASP.NET Identity with custom Usuario entity, password policy (min 10 chars, uppercase + number per FR-001), and session timeout (30 min per FR-005) in src/Ceiba.Infrastructure/Identity/
- [X] T010a [P] RS-005 Mitigation: Configure secure cookie settings (Secure, HttpOnly, SameSite=Strict) in src/Ceiba.Web/Program.cs
- [X] T010b [P] RS-005 Mitigation: Implement session ID regeneration after login in AuthService (deferred to US3 - full implementation)
- [X] T010c [P] RS-005 Mitigation: Implement User-Agent validation middleware in src/Ceiba.Web/Middleware/UserAgentValidationMiddleware.cs
- [X] T010d [P] RS-005 Mitigation: Configure Anti-CSRF token validation globally in src/Ceiba.Web/Program.cs
- [X] T010e [P] RS-005 Mitigation: Implement session audit logging (create/destroy) in AuthService (deferred to US3 - full implementation)
- [X] T011 [P] Create base entity classes in src/Ceiba.Core/Entities/BaseEntity.cs
- [X] T012 Create audit logging interceptor in src/Ceiba.Infrastructure/Data/AuditSaveChangesInterceptor.cs
- [X] T013 [P] Define audit action codes enum in src/Ceiba.Core/Enums/AuditActionCode.cs
- [X] T014 [P] Create IAuditService interface in src/Ceiba.Core/Interfaces/IAuditService.cs
- [X] T015 Implement AuditService in src/Ceiba.Infrastructure/Services/AuditService.cs
- [X] T016 [P] Configure dependency injection in src/Ceiba.Web/Program.cs
- [X] T017 [P] Setup error handling middleware in src/Ceiba.Web/Middleware/ErrorHandlingMiddleware.cs
- [X] T018 [P] Configure logging with Serilog in src/Ceiba.Web/Program.cs
- [X] T018a [P] RS-003 Mitigation: Configure Serilog destructuring policies to exclude credentials in src/Ceiba.Web/Program.cs (implemented via PIIRedactionEnricher)
- [X] T018b [P] RS-003 Mitigation: Implement PII redaction enricher for Serilog in src/Ceiba.Infrastructure/Logging/PIIRedactionEnricher.cs
- [X] T018c [P] RS-003 Mitigation: Configure log encryption at rest using file system encryption (documentation added - OS-level encryption)
- [X] T018d [P] RS-003 Mitigation: Implement log retention policy (30 days app logs, indefinite audit) in configuration
- [X] T018e [P] RS-003 Mitigation: Create automated log scanning script in scripts/security/scan-logs-for-sensitive-data.sh
- [X] T019 Create initial EF Core migration in src/Ceiba.Infrastructure/Data/Migrations/
- [X] T019a [P] RT-004 Mitigation: Add campos_adicionales (JSONB) and schema_version fields to ReporteIncidencia entity for extensibility (schema_version added, campos_adicionales will be added in US1)
- [X] T019b [P] RT-004 Mitigation: Create MIGRATIONS.md changelog file at repository root
- [X] T019c [P] RT-004 Mitigation: Implement pre-migration backup service in src/Ceiba.Infrastructure/Data/MigrationBackupService.cs + scripts/migrations/backup-before-migration.sh
- [X] T019d [P] RT-004 Mitigation: Add feature flag configuration system in src/Ceiba.Web/Configuration/FeatureFlags.cs
- [X] T019e [P] RT-004 Mitigation: Create migration validation scripts (row count, FK integrity) in scripts/migrations/validate-migration.sh
- [X] T020 Create seed data service in src/Ceiba.Infrastructure/Data/SeedDataService.cs
- [X] T020a [P] RS-001 Mitigation: Create authorization policy handlers in src/Ceiba.Web/Program.cs (policies configured directly in Program.cs)
- [X] T020b [P] RS-001 Mitigation: Implement custom AuthorizationMiddleware to log unauthorized attempts in src/Ceiba.Web/Middleware/AuthorizationLoggingMiddleware.cs
- [X] T020c [P] RS-001 Mitigation: Create authorization test matrix (Role × Functionality) in tests/Ceiba.Integration.Tests/AuthorizationMatrixTests.cs
- [X] T020d [P] RS-001 Mitigation: Configure OWASP ZAP security scanning in CI/CD pipeline (.github/workflows/security-scan.yml)
- [X] T020e [P] RS-001 Mitigation: Create security code review checklist in .github/PULL_REQUEST_TEMPLATE.md
- [X] T020f [P] RS-002 Mitigation: Configure Content Security Policy (CSP) headers in src/Ceiba.Web/Program.cs
- [X] T020g [P] RS-002 Mitigation: Configure SonarQube + Snyk security scanning in CI/CD (.github/workflows/security-scan.yml)
- [X] T020h [P] RS-002 Mitigation: Add Roslyn analyzers for SQL concatenation detection (Directory.Build.props)
- [X] T020i [P] RS-002 Mitigation: Create input validation integration tests in tests/Ceiba.Integration.Tests/InputValidationTests.cs
- [X] T020j [P] RS-002 Mitigation: Implement zero-raw-SQL policy check script in scripts/security/check-no-raw-sql.sh

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - Creación y Entrega de Reportes por Agentes (Priority: P1) 🎯 MVP

**Goal**: Allow CREADOR users to create, edit, and submit incident reports (Type A)

**Independent Test**: Create a CREADOR user, login, create a Type A report, save as draft, edit, submit, verify state changes to "entregado" and audit is logged

### Tests for User Story 1

- [X] T021 [P] [US1] Contract test for POST /api/reports in tests/Ceiba.Integration.Tests/ReportContractTests.cs
- [X] T022 [P] [US1] Contract test for PUT /api/reports/{id} in tests/Ceiba.Integration.Tests/ReportContractTests.cs
- [X] T023 [P] [US1] Contract test for POST /api/reports/{id}/submit in tests/Ceiba.Integration.Tests/ReportContractTests.cs
- [X] T024 [P] [US1] Unit test for ReportService create/edit in tests/Ceiba.Application.Tests/ReportServiceTests.cs
- [X] T025 [P] [US1] Unit test for report state transitions in tests/Ceiba.Core.Tests/ReporteIncidenciaTests.cs
- [X] T026 [P] [US1] Component test for report form in tests/Ceiba.Web.Tests/ReportFormComponentTests.cs

### Implementation for User Story 1

#### Entities

- [X] T027 [P] [US1] Create Zona entity in src/Ceiba.Core/Entities/Zona.cs
- [X] T028 [P] [US1] Create Sector entity in src/Ceiba.Core/Entities/Sector.cs
- [X] T029 [P] [US1] Create Cuadrante entity in src/Ceiba.Core/Entities/Cuadrante.cs
- [X] T030 [P] [US1] Create CatalogoSugerencia entity in src/Ceiba.Core/Entities/CatalogoSugerencia.cs
- [X] T031 [US1] Create ReporteIncidencia entity with state logic in src/Ceiba.Core/Entities/ReporteIncidencia.cs
- [X] T032 [US1] Create EF configurations for US1 entities in src/Ceiba.Infrastructure/Data/Configurations/

#### Services

- [X] T033 [P] [US1] Create IReportService interface in src/Ceiba.Core/Interfaces/IReportService.cs
- [X] T034 [P] [US1] Create ICatalogService interface in src/Ceiba.Core/Interfaces/ICatalogService.cs
- [X] T035 [US1] Implement ReportService in src/Ceiba.Application/Services/ReportService.cs
- [X] T036 [US1] Implement CatalogService for cascading dropdowns in src/Ceiba.Infrastructure/Services/CatalogService.cs
- [X] T037 [US1] Create report DTOs in src/Ceiba.Shared/DTOs/ReportDTOs.cs
- [X] T038 [US1] Create FluentValidation validators in src/Ceiba.Application/Validators/ReportValidators.cs

#### API & UI

- [X] T039 [US1] Create ReportsController in src/Ceiba.Web/Controllers/ReportsController.cs
- [X] T040 [US1] Create CatalogsController for dropdowns in src/Ceiba.Web/Controllers/CatalogsController.cs
- [X] T041 [US1] Create ReportForm.razor component in src/Ceiba.Web/Components/Pages/Reports/ReportForm.razor
- [X] T042 [US1] Create ReportList.razor for CREADOR history in src/Ceiba.Web/Components/Pages/Reports/ReportList.razor
- [X] T043 [US1] Create cascading dropdown component in src/Ceiba.Web/Components/Shared/CascadingSelect.razor
- [X] T044 [US1] Create suggestion autocomplete component in src/Ceiba.Web/Components/Shared/SuggestionInput.razor
- [X] T045 [US1] Add authorization policy for CREADOR role in src/Ceiba.Web/Program.cs
- [X] T046 [US1] Add EF migration for US1 entities

**Checkpoint**: User Story 1 complete - CREADOR can create and submit reports independently

---

## Phase 4: User Story 2 - Revisión, Edición y Exportación por Supervisores (Priority: P2)

**Goal**: Allow REVISOR users to view all reports, edit them, search/filter, and export to PDF/JSON

**Independent Test**: Login as REVISOR, view all reports, filter by zona/delito, edit a report, export single and batch to PDF and JSON

### Tests for User Story 2

- [X] T047 [P] [US2] Contract test for GET /api/reports (all reports) in tests/Ceiba.Integration.Tests/ReportContractTests.cs
- [X] T048 [P] [US2] Contract test for export endpoints in tests/Ceiba.Integration.Tests/ExportContractTests.cs (via ExportServiceTests)
- [X] T049 [P] [US2] Unit test for PDF generation in tests/Ceiba.Application.Tests/ExportServiceTests.cs
- [ ] T050 [P] [US2] Component test for report filtering in tests/Ceiba.Web.Tests/ReportFilterComponentTests.cs

### Implementation for User Story 2

#### Services

- [X] T051 [P] [US2] Create IExportService interface in src/Ceiba.Application/Services/Export/IExportService.cs
- [X] T052 [US2] Implement ExportService with QuestPDF in src/Ceiba.Application/Services/Export/ExportService.cs
- [X] T052a [P] [US2] RT-003 Mitigation: Enforce export limits (50 PDFs, 100 JSONs) with clear error messages in src/Ceiba.Application/Services/Export/ExportService.cs
- [X] T052b [P] [US2] RT-003 Mitigation: Implement streaming PDF generation (no full in-memory buffer) using FileStreamResult
- [X] T052c [P] [US2] RT-003 Mitigation: Create background export job for >50 reports with Hangfire and email notification in src/Ceiba.Application/Jobs/ExportJob.cs
- [X] T052d [P] [US2] RT-003 Mitigation: Configure Hangfire max 3 concurrent export jobs and 2-minute timeout
- [X] T052e [P] [US2] RT-003 Mitigation: Add export monitoring (report count, file size, generation time) with alerts for >30s or >500MB
- [X] T053 [US2] Add search/filter methods to ReportService in src/Ceiba.Application/Services/ReportService.cs
- [X] T054 [US2] Create export DTOs in src/Ceiba.Shared/DTOs/Export/

#### API & UI

- [X] T055 [US2] Add export endpoints to ExportController in src/Ceiba.Web/Controllers/ExportController.cs
- [ ] T056 [US2] Create ReportFilter.razor component in src/Ceiba.Web/Components/Shared/ReportFilter.razor
- [X] T057 [US2] Create RevisorReportList.razor with all reports in src/Ceiba.Web/Components/Pages/Reports/ReportListRevisor.razor
- [X] T058 [US2] Create ReportDetail.razor for view/edit in src/Ceiba.Web/Components/Pages/Reports/ReportView.razor
- [X] T059 [US2] Add batch selection UI for export in src/Ceiba.Web/Components/Pages/Reports/ReportListRevisor.razor
- [X] T060 [US2] Add authorization policy for REVISOR role in src/Ceiba.Web/Program.cs

**Checkpoint**: User Story 2 complete - REVISOR can manage and export all reports

---

## Phase 5: User Story 3 - Gestión de Usuarios y Auditoría por Administrador (Priority: P3)

**Goal**: Allow ADMIN users to manage users, configure catalogs, and view audit logs

**Independent Test**: Login as ADMIN, create user with roles, suspend user, add zona/sector/cuadrante, view and filter audit logs

### Tests for User Story 3

- [X] T061 [P] [US3] Contract test for user management endpoints in tests/Ceiba.Integration.Tests/AdminContractTests.cs
- [X] T062 [P] [US3] Contract test for catalog management in tests/Ceiba.Integration.Tests/CatalogContractTests.cs
- [X] T063 [P] [US3] Contract test for audit log endpoints in tests/Ceiba.Integration.Tests/AuditContractTests.cs
- [X] T064 [P] [US3] Unit test for UserManagementService in tests/Ceiba.Application.Tests/Services/UserManagementServiceTests.cs

### Implementation for User Story 3

#### Entities

- [X] T065 [P] [US3] Create RegistroAuditoria entity in src/Ceiba.Core/Entities/RegistroAuditoria.cs
- [X] T066 [US3] Create EF configuration for RegistroAuditoria in src/Ceiba.Infrastructure/Data/Configurations/RegistroAuditoriaConfiguration.cs

#### Services

- [X] T067 [P] [US3] Create IUserManagementService interface in src/Ceiba.Core/Interfaces/IUserManagementService.cs
- [X] T068 [P] [US3] Create ICatalogAdminService interface in src/Ceiba.Core/Interfaces/ICatalogAdminService.cs
- [X] T069 [US3] Implement UserManagementService in src/Ceiba.Infrastructure/Services/UserManagementService.cs
- [X] T070 [US3] Implement CatalogAdminService in src/Ceiba.Infrastructure/Services/CatalogAdminService.cs
- [X] T071 [US3] Create admin DTOs in src/Ceiba.Shared/DTOs/AdminDTOs.cs

#### API & UI

- [X] T072 [US3] Create AdminController for user management in src/Ceiba.Web/Controllers/AdminController.cs
- [X] T073 [US3] Add catalog management endpoints to AdminController
- [X] T074 [US3] Create AuditController in src/Ceiba.Web/Controllers/AuditController.cs
- [X] T075 [US3] Create UserList.razor in src/Ceiba.Web/Components/Pages/Admin/UserList.razor
- [X] T076 [US3] Create UserForm.razor in src/Ceiba.Web/Components/Pages/Admin/UserForm.razor
- [X] T077 [US3] Create CatalogManager.razor for zona/sector/cuadrante in src/Ceiba.Web/Components/Pages/Admin/CatalogManager.razor
- [X] T078 [US3] Create SuggestionManager.razor in src/Ceiba.Web/Components/Pages/Admin/SuggestionManager.razor
- [X] T079 [US3] Create AuditLogViewer.razor in src/Ceiba.Web/Components/Pages/Admin/AuditLogViewer.razor
- [X] T080 [US3] Add authorization policy for ADMIN role in src/Ceiba.Web/Program.cs
- [X] T081 [US3] Add EF migration for US3 entities

**Checkpoint**: User Story 3 complete - ADMIN can manage users, catalogs, and view audits

---

## Phase 6: User Story 4 - Reportes Automatizados Diarios con IA (Priority: P4)

**Goal**: Generate daily automated reports with AI-powered summaries, send via email

**Independent Test**: Configure scheduled time, create test reports, trigger manual generation, verify markdown→Word conversion, email sending, and storage

### Tests for User Story 4

- [ ] T082 [P] [US4] Unit test for AI summarization in tests/Ceiba.Application.Tests/AIServiceTests.cs
- [ ] T083 [P] [US4] Unit test for email service in tests/Ceiba.Infrastructure.Tests/EmailServiceTests.cs
- [ ] T084 [P] [US4] Unit test for report aggregation in tests/Ceiba.Application.Tests/AutomatedReportServiceTests.cs
- [ ] T085 [P] [US4] Integration test for Hangfire job in tests/Ceiba.Integration.Tests/AutomatedReportJobTests.cs

### Implementation for User Story 4

#### Entities

- [X] T086 [P] [US4] Create ReporteAutomatizado entity in src/Ceiba.Core/Entities/ReporteAutomatizado.cs
- [X] T087 [P] [US4] Create ModeloReporte entity in src/Ceiba.Core/Entities/ModeloReporte.cs
- [X] T088 [US4] Create EF configurations for US4 entities in src/Ceiba.Infrastructure/Data/Configurations/

#### Services

- [X] T089 [P] [US4] Create IAiNarrativeService interface in src/Ceiba.Core/Interfaces/IAiNarrativeService.cs
- [X] T090 [P] [US4] Create IEmailService interface in src/Ceiba.Core/Interfaces/IEmailService.cs
- [X] T091 [P] [US4] Create IAutomatedReportService interface in src/Ceiba.Core/Interfaces/IAutomatedReportService.cs
- [X] T092 [US4] Implement AiNarrativeService with provider-agnostic abstraction in src/Ceiba.Infrastructure/Services/AiNarrativeService.cs
- [X] T092a [P] [US4] RT-001 Mitigation: Configure Polly policies (30s timeout, circuit breaker after 5 failures) for AiNarrativeService in src/Ceiba.Infrastructure/Resilience/AiServicePolicies.cs
- [X] T092b [P] [US4] RT-001 Mitigation: Implement AIServiceMock with deterministic responses in tests/Ceiba.Infrastructure.Tests/Mocks/AIServiceMock.cs
- [X] T092c [P] [US4] RT-001 Mitigation: Add response caching layer with IMemoryCache for identical prompts
- [X] T092d [P] [US4] RT-001 Mitigation: Implement graceful fallback (statistics-only reports) when AI unavailable
- [X] T092e [P] [US4] RT-001 Mitigation: Add AI call monitoring (latency, tokens, success rate) in AiServiceMetrics class
- [X] T093 [US4] Implement EmailService with MailKit/SendGrid/Mailgun in src/Ceiba.Infrastructure/Services/EmailService.cs
- [X] T094 [US4] Implement markdown to Word conversion in src/Ceiba.Application/Services/DocumentConversionService.cs
- [X] T094a [P] [US4] RT-006 Mitigation: Add Pandoc availability check in application startup with PandocStartupValidator
- [X] T094b [P] [US4] RT-006 Mitigation: Implement 60-second timeout on Pandoc process invocations (ConversionTimeoutSeconds)
- [X] T094c [P] [US4] RT-006 Mitigation: Add error handling and logging if Pandoc conversion fails
- [X] T094d [P] [US4] RT-006 Mitigation: Input size validation (MaxInputCharacters = 500,000)
- [X] T095 [US4] Implement AutomatedReportService in src/Ceiba.Infrastructure/Services/AutomatedReportService.cs
- [X] T096 [US4] Configure Hangfire for scheduling in src/Ceiba.Web/Program.cs
- [X] T097 [US4] Create Hangfire job for daily report (integrated in AutomatedReportService)

#### API & UI

- [X] T098 [US4] Create AutomatedReportsController in src/Ceiba.Web/Controllers/AutomatedReportsController.cs
- [X] T099 [US4] Create AutomatedReportConfigController in src/Ceiba.Web/Controllers/AutomatedReportConfigController.cs
- [X] T100 [US4] Create AutomatedReportList.razor in src/Ceiba.Web/Components/Pages/Automated/AutomatedReportList.razor
- [X] T101 [US4] Create AutomatedReportDetail.razor in src/Ceiba.Web/Components/Pages/Automated/AutomatedReportDetail.razor
- [X] T102 [US4] Create TemplateList.razor in src/Ceiba.Web/Components/Pages/Automated/TemplateList.razor
- [X] T103 [US4] Add EF migration for US4 entities

**Checkpoint**: User Story 4 complete - Automated daily reports with AI and email

---

## Phase 7: User Story 5 - Gestión de Listas de Sugerencias (Priority: P5)

**Goal**: Allow ADMIN to configure suggestion lists for text fields

**Independent Test**: Login as ADMIN, add/edit/deactivate suggestions for sexo/delito/tipoDeAtencion, verify they appear in CREADOR forms

### Tests for User Story 5

- [ ] T104 [P] [US5] Contract test for suggestion endpoints in tests/Ceiba.Integration.Tests/SuggestionContractTests.cs
- [ ] T105 [P] [US5] Unit test for suggestion service in tests/Ceiba.Application.Tests/SuggestionServiceTests.cs

### Implementation for User Story 5

- [ ] T106 [US5] Extend CatalogAdminService for suggestions in src/Ceiba.Application/Services/CatalogAdminService.cs
- [ ] T107 [US5] Add suggestion management endpoints to AdminController
- [ ] T108 [US5] Update SuggestionManager.razor with full CRUD in src/Ceiba.Web/Components/Pages/Admin/SuggestionManager.razor

**Checkpoint**: User Story 5 complete - Suggestion lists fully configurable

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T109 [P] Create main navigation layout in src/Ceiba.Web/Components/Layout/MainLayout.razor
- [ ] T110 [P] Create role-based menu component in src/Ceiba.Web/Components/Layout/NavMenu.razor
- [ ] T111 [P] Add responsive/mobile-first CSS in src/Ceiba.Web/wwwroot/css/app.css
- [ ] T112 [P] Implement WCAG AA accessibility: keyboard navigation for all interactive elements
- [ ] T112a [P] Implement WCAG AA accessibility: ARIA labels for form inputs and buttons
- [ ] T112b [P] Implement WCAG AA accessibility: color contrast ratio ≥4.5:1 for text
- [ ] T112c [P] Implement WCAG AA accessibility: screen reader testing with NVDA/JAWS
- [ ] T112d [P] Implement WCAG AA accessibility: focus indicators visible on all focusable elements
- [ ] T113 [P] Create login page in src/Ceiba.Web/Components/Pages/Auth/Login.razor
- [ ] T113a [P] RS-004 Mitigation: Implement reCAPTCHA v3 integration in login page (after 3 failed attempts)
- [ ] T113b [P] RS-004 Mitigation: Configure rate limiting middleware (AspNetCoreRateLimit) 10 attempts/min per IP
- [ ] T113c [P] RS-004 Mitigation: Implement progressive delay mechanism (1s, 2s, 4s, 8s) in AuthService
- [ ] T113d [P] RS-004 Mitigation: Create failed login monitoring dashboard for ADMIN in src/Ceiba.Web/Components/Pages/Admin/LoginMonitoring.razor
- [ ] T113e [P] RS-004 Mitigation: Implement automated alerts for attack patterns (>50 attempts/hour)
- [ ] T114 Add HTTPS configuration for production in docker/docker-compose.prod.yml
- [ ] T114a [P] RS-005 Mitigation: Configure HSTS headers (max-age=31536000, includeSubDomains, preload) in src/Ceiba.Web/Program.cs
- [ ] T115 [P] Create backup script in scripts/backup/backup-db.sh
- [ ] T115a [P] Create backup restoration script and procedure documentation in scripts/backup/restore-db.sh
- [ ] T115b Backup validation: Test full backup→restore cycle, verify data integrity and RTO <1 hour
- [ ] T116 [P] Create GitHub Actions CI workflow in .github/workflows/ci.yml
- [ ] T116a [P] RT-005 Mitigation: Configure Playwright E2E tests for Chrome, Firefox, Edge, Safari in tests/Ceiba.E2E.Tests/
- [ ] T116b [P] RT-005 Mitigation: Add responsive viewport tests (320px, 768px, 1024px, 1920px) to Playwright suite
- [ ] T116c [P] RT-005 Mitigation: Integrate axe-core accessibility checks in Playwright tests
- [ ] T116d [P] RT-005 Mitigation: Add visual regression testing (screenshots) for critical pages
- [ ] T116e [P] RT-005 Mitigation: Configure Playwright in CI/CD pipeline (GitHub Actions) blocking merge on failures
- [ ] T117 Performance optimization: add database indexes per data-model.md
- [ ] T117a [P] RT-002 Mitigation: Create composite indexes (estado+zona+fecha, fecha+estado+delito) for common search patterns
- [ ] T117b [P] RT-002 Mitigation: Implement PostgreSQL full-text search indexes (GIN) on hechos_reportados and acciones_realizadas with 'spanish' configuration
- [ ] T117c [P] RT-002 Mitigation: Add search result caching with IMemoryCache (5-min TTL, cache key from filter hash) in src/Ceiba.Application/Services/ReportService.cs
- [ ] T117d [P] RT-002 Mitigation: Enforce pagination limit (500 records/page max) in search queries
- [ ] T117e [P] RT-002 Mitigation: Add EXPLAIN ANALYZE validation in integration tests to verify index usage (tests/Ceiba.Integration.Tests/PerformanceTests.cs)
- [ ] T118 Security hardening: Add HTTP security headers (X-Content-Type-Options, X-Frame-Options, Content-Security-Policy, Strict-Transport-Security)
- [ ] T118a Security hardening: Implement anti-CSRF tokens via ASP.NET Core AntiForgery middleware
- [ ] T118b Security hardening: Configure input sanitization using HtmlEncoder for user-generated content
- [ ] T119 Run all tests and verify coverage meets requirements
- [ ] T119a Extensibility validation: Create test report type (Tipo B) with different fields to verify architecture supports new types without migrations
- [ ] T120 Validate quickstart.md procedures work end-to-end

---

## Phase 9: Non-Functional Requirements Validation

**Purpose**: Explicit validation of performance, scalability, and reliability criteria

- [ ] T121 [P] Performance test: Verify search in 1000+ reports completes in <10 seconds (SC-003)
- [ ] T122 [P] Performance test: Verify PDF export of 50 reports completes in <30 seconds (SC-005)
- [ ] T123 [P] Load test: Verify 50 concurrent users without performance degradation (SC-008)
- [ ] T124 [P] Availability monitoring: Configure uptime checks and alerts for 99.5% SLA (SC-004)
- [ ] T125 [P] Usability test: Verify 95% of test users complete first task without assistance (SC-009)
- [ ] T126 [P] RO-001 Mitigation: Create BackupService with Hangfire job scheduling in src/Ceiba.Infrastructure/Services/BackupService.cs
- [ ] T127 [P] RO-001 Mitigation: Implement pg_dump backup execution with gzip compression and validation (pg_restore --list)
- [ ] T128 [P] RO-001 Mitigation: Configure backup retention policy (7 daily, 4 weekly, 12 monthly, 3 annual) with cleanup job
- [ ] T129 [P] RO-001 Mitigation: Implement backup integrity validation and size anomaly detection (>20% variance alert)
- [ ] T130 [P] RO-001 Mitigation: Configure backup storage to secondary disk (/mnt/backups) and offsite copy (S3/NAS)
- [ ] T131 [P] RO-001 Mitigation: Implement backup monitoring with email/Slack alerts (timeout 15 min, last valid >24h)
- [ ] T132 [P] RO-001 Mitigation: Create automated monthly restore test job in staging environment
- [ ] T133 [P] RO-001 Mitigation: Implement incremental backup with pg_basebackup every 6 hours (8 AM, 2 PM, 8 PM)
- [ ] T134 [P] RO-001 Mitigation: Create disaster recovery runbook in docs/runbooks/disaster-recovery.md (RTO <1h, RPO <6h)
- [ ] T135 [P] RO-001 Mitigation: Create backup metrics dashboard in Hangfire (success rate, duration, size trending)
- [ ] T136 [P] RO-002 Mitigation: Configure Docker resource limits in docker-compose.yml (2 CPU, 4 GB RAM per container)
- [ ] T137 [P] RO-002 Mitigation: Configure database connection pool in appsettings.json (MaxPoolSize=20, MinPoolSize=5)
- [ ] T138 [P] RO-002 Mitigation: Implement AspNetCoreRateLimit middleware with per-endpoint configuration
- [ ] T139 [P] RO-002 Mitigation: Configure Hangfire worker count to 3 concurrent jobs with priority queues
- [ ] T140 [P] RO-002 Mitigation: Implement static catalog caching service with IMemoryCache (1h TTL)
- [ ] T141 [P] RO-002 Mitigation: Create health check endpoint /health with DB, disk, memory checks
- [ ] T142 [P] RO-002 Mitigation: Configure Prometheus metrics exporter for ASP.NET Core application
- [ ] T143 [P] RO-002 Mitigation: Create Grafana dashboard with CPU, RAM, disk alerts (70%/75% thresholds)
- [ ] T144 [P] RO-002 Mitigation: Implement graceful degradation middleware (HTTP 503 on resource exhaustion)
- [ ] T145 [P] RO-002 Mitigation: Document horizontal scaling architecture in docs/architecture/scaling.md
- [ ] T146 [P] RO-003 Mitigation: Create EmailService with MailKit SMTP + SendGrid/Mailgun fallback
- [ ] T147 [P] RO-003 Mitigation: Implement Polly retry policy with exponential backoff (0s, 1min, 5min)
- [ ] T148 [P] RO-003 Mitigation: Create EMAIL_QUEUE table and persistence layer for failed emails
- [ ] T149 [P] RO-003 Mitigation: Implement Hangfire background job to process EMAIL_QUEUE every 5 minutes
- [ ] T150 [P] RO-003 Mitigation: Configure Polly circuit breaker for SMTP (5 failures, 2min open state)
- [ ] T151 [P] RO-003 Mitigation: Implement rate limiting throttling (100 emails/hour, warn at 90%)
- [ ] T152 [P] RO-003 Mitigation: Add email delivery audit logging to AUDITORIA table
- [ ] T153 [P] RO-003 Mitigation: Configure alerting for email failure rate >10% (email + Slack)
- [ ] T154 [P] RO-003 Mitigation: Implement email address validation (RFC 5322) and 10MB attachment limit
- [ ] T155 [P] RO-003 Mitigation: Create email delivery metrics dashboard in Hangfire (sent, pending, failed)
- [ ] T156 [P] RO-004 Mitigation: Create unified ResiliencePolicyFactory with Polly circuit breaker configuration
- [ ] T157 [P] RO-004 Mitigation: Configure service-specific timeouts (30s AI, 15s email, 30s DB) in appsettings.json
- [ ] T158 [P] RO-004 Mitigation: Implement graceful degradation handlers for AI and email services
- [ ] T159 [P] RO-004 Mitigation: Create SERVICE_HEALTH_LOG table for uptime tracking
- [ ] T160 [P] RO-004 Mitigation: Implement background health check job (5 min interval) for external services
- [ ] T161 [P] RO-004 Mitigation: Configure Prometheus metrics for circuit breaker state and service calls
- [ ] T162 [P] RO-004 Mitigation: Create alerting rules for circuit breaker open >10 minutes
- [ ] T163 [P] RO-004 Mitigation: Implement user-friendly degradation messages in UI components
- [ ] T164 [P] RO-004 Mitigation: Create feature flag configuration for manual service disable (EnableAIService, EnableEmailService)
- [ ] T165 [P] RO-004 Mitigation: Implement retry with jitter for transient errors (Polly WaitAndRetry)
- [ ] T166 [P] RO-005 Mitigation: Configure Dependabot in .github/dependabot.yml (weekly NuGet updates, manual major version review)
- [ ] T167 [P] RO-005 Mitigation: Integrate Snyk CLI in CI/CD pipeline (fail build on critical vulnerabilities)
- [ ] T168 [P] RO-005 Mitigation: Create staging environment configuration identical to production
- [ ] T169 [P] RO-005 Mitigation: Create CHANGELOG.md template with semantic versioning guidelines
- [ ] T170 [P] RO-005 Mitigation: Document snapshot/rollback procedure in docs/runbooks/rollback.md (15min target)
- [ ] T171 [P] RO-005 Mitigation: Pin Docker base images to SHA256 in Dockerfile and docker-compose.yml
- [ ] T172 [P] RO-005 Mitigation: Configure post-deployment monitoring alerts (error rate, latency, resources)
- [ ] T173 [P] RO-005 Mitigation: Create dependency tracking document in docs/dependencies.md
- [ ] T174 [P] RO-005 Mitigation: Implement update checklist automation script in scripts/update-checklist.sh
- [ ] T175 [P] RO-005 Mitigation: Configure stakeholder notification system for maintenance windows (Slack + email)
- [ ] T176 [P] RN-001 Mitigation: Implement onboarding tour component (Blazor Tour with 4 steps: welcome, create report, save draft, submit)
- [ ] T177 [P] RN-001 Mitigation: Add contextual tooltips to form fields (reusable <FormField> component with help examples)
- [ ] T178 [P] RN-001 Mitigation: Create user manual with screenshots (docs/user-manual.md → HTML/PDF)
- [ ] T179 [P] RN-001 Mitigation: Produce video tutorials <2min (screen recordings with narration, embedded in help panel)
- [ ] T180 [P] RN-001 Mitigation: Design training sessions (materials: slides, handouts, cheat sheets, train-the-trainer program)
- [ ] T181 [P] RN-001 Mitigation: Implement UX testing protocol (recruit 3-5 CREADOR users, task-based testing, SUS questionnaire)
- [ ] T182 [P] RN-001 Mitigation: Create in-app feedback form (modal with rating 1-5 + comments, stored in FEEDBACK table)
- [ ] T183 [P] RN-001 Mitigation: Implement NPS tracking and analytics (quarterly survey, calculation service, trend dashboard)
- [ ] T184 [P] RN-001 Mitigation: Optimize page load times (target <2s p95: lazy loading, image WebP, bundle minimization)
- [ ] T185 [P] RN-001 Mitigation: Create adoption metrics dashboard for ADMIN (active users, time to complete report, feature heatmap)
- [ ] T186 [P] RN-002 Mitigation: Create SYSTEM_CONFIG table and service (key-value config with type validation, UI for ADMIN)
- [ ] T187 [P] RN-002 Mitigation: Create CONFIG_HISTORY table for audit trail (old_value, new_value, changed_by, change_reason)
- [ ] T188 [P] RN-002 Mitigation: Implement FEATURE_FLAG table and service (rollout percentage, enabled_roles, enabled_users)
- [ ] T189 [P] RN-002 Mitigation: Create change request template (docs/templates/change-request.md with impact assessment)
- [ ] T190 [P] RN-002 Mitigation: Implement configuration management UI for ADMIN (DataGrid editable with save/rollback)
- [ ] T191 [P] RN-002 Mitigation: Add form versioning support (schema_version in REPORTE_INCIDENCIA, multi-version rendering)
- [ ] T192 [P] RN-002 Mitigation: Create compliance matrix document (docs/compliance-matrix.md with requirement → regulation mapping)
- [ ] T193 [P] RN-002 Mitigation: Implement sprint capacity reservation tracking (20% buffer for emergent changes)
- [ ] T194 [P] RN-002 Mitigation: Create stakeholder communication protocol (monthly reviews, quarterly roadmap sessions)
- [ ] T195 [P] RN-002 Mitigation: Implement spec.md versioning workflow (semantic versioning, change log with rationale)
- [ ] T196 [P] RN-003 Mitigation: Create PILOT_USER table and dashboard (pilot_start_date, satisfaction_score, would_recommend, is_champion)
- [ ] T197 [P] RN-003 Mitigation: Create SUPPORT_TICKET table and management UI (category, priority, status, SLA tracking)
- [ ] T198 [P] RN-003 Mitigation: Implement USER_ACHIEVEMENT table for gamification (badges: first_report, ten_reports, fifty_reports)
- [ ] T199 [P] RN-003 Mitigation: Create go-live readiness calculation service (satisfaction 30%, recommend 25%, bugs 25%, feedback 20%)
- [ ] T200 [P] RN-003 Mitigation: Implement support ticket management UI (tabs: open/in_progress/resolved, assignment workflow)
- [ ] T201 [P] RN-003 Mitigation: Create change champion portal (exclusive access for is_champion=true, advanced resources)
- [ ] T202 [P] RN-003 Mitigation: Implement gradual transition plan tracking (4 phases over 8 weeks, metrics per phase)
- [ ] T203 [P] RN-003 Mitigation: Create training materials and tracking (TRAINING_COMPLETION table, certificate issuance)
- [ ] T204 [P] RN-003 Mitigation: Implement tangible benefits communication (time comparison metrics: digital vs paper)
- [ ] T205 [P] RN-003 Mitigation: Create pilot feedback collection and analysis workflow (weekly reviews, iteration tracking)
- [ ] T206 [P] RN-004 Mitigation: Implement /health endpoint (checks: DB connectivity, disk space, memory, API responsiveness)
- [ ] T207 [P] RN-004 Mitigation: Configure Docker restart policies (restart: unless-stopped, healthcheck with 60s interval)
- [ ] T208 [P] RN-004 Mitigation: Create systemd service for auto-start (ceiba-reportes.service with Restart=on-failure)
- [ ] T209 [P] RN-004 Mitigation: Configure Prometheus alerting rules (HighResponseTime, ServiceDown, DatabaseConnectionFailed, DiskSpaceLow)
- [ ] T210 [P] RN-004 Mitigation: Implement /status public page (system status, component health, uptime 30d, planned maintenance)
- [ ] T211 [P] RN-004 Mitigation: Create INCIDENT_LOG table and tracking (title, severity, started_at, resolved_at, MTTR, PIR flag)
- [ ] T212 [P] RN-004 Mitigation: Create SCHEDULED_MAINTENANCE table (scheduled_start, actual_start, notification_sent)
- [ ] T213 [P] RN-004 Mitigation: Create incident response runbooks (docs/runbooks/: db-connection-failure.md, disk-full.md, etc.)
- [ ] T214 [P] RN-004 Mitigation: Configure maintenance window discipline (First Sunday 2-6 AM, stakeholder notification 48h prior)
- [ ] T215 [P] RN-004 Mitigation: Implement performance degradation detection (p95 latency monitoring, slow query logging >1s)
- [ ] T216 [P] RN-005 Mitigation: Configure disk encryption (LUKS for PostgreSQL data volume, /etc/crypttab for auto-unlock)
- [ ] T217 [P] RN-005 Mitigation: Configure PostgreSQL SSL connection (ssl=on, server.crt/key, connection string SSL Mode=Require)
- [ ] T218 [P] RN-005 Mitigation: Implement PDF watermarking service (footer with "Exportado por {user} el {date}")
- [ ] T219 [P] RN-005 Mitigation: Implement anomaly detection background service (>50 downloads/h, after-hours access, alerts every 15min)
- [ ] T220 [P] RN-005 Mitigation: Create ACCESS_REVIEW table and quarterly dashboard (reviewed_by, accounts_deactivated, next_review_due)
- [ ] T221 [P] RN-005 Mitigation: Create SECURITY_INCIDENT table and SIRT workflow (incident_type, severity, containment SLA <1h)
- [ ] T222 [P] RN-005 Mitigation: Create SECURITY_TRAINING table and tracking (training_module, score, expiration_date for annual re-cert)
- [ ] T223 [P] RN-005 Mitigation: Implement backup encryption script (GPG AES256, scripts/backup-with-encryption.sh)
- [ ] T224 [P] RN-005 Mitigation: Create security audit schedule (quarterly internal, annual external pentest with contract)
- [ ] T225 [P] RN-005 Mitigation: Implement compliance with LFPDP (Ley Federal de Protección de Datos Personales - data minimization, retention policy)
- [ ] T226 [P] RP-001 Mitigation: Create CHANGE_REQUEST table with CAB workflow (cr_number, title, impact assessment, moscow_priority, decision)
- [ ] T227 [P] RP-001 Mitigation: Implement ChangeRequestService with automatic CR number generation (CR-YYYY-NNN format)
- [ ] T228 [P] RP-001 Mitigation: Create CAB notification system (email to PM, PO, Tech Lead on CR submission)
- [ ] T229 [P] RP-001 Mitigation: Create SPRINT_METRICS table for velocity tracking (planned_story_points, completed_story_points, carried_over)
- [ ] T230 [P] RP-001 Mitigation: Implement VelocityTrackingService with health status calculation (healthy >85%, warning >70%, critical <70%)
- [ ] T231 [P] RP-001 Mitigation: Create change request management UI in Admin module (submit CR, review CR, CAB dashboard)
- [ ] T232 [P] RP-001 Mitigation: Document "Excluded Features" section in spec.md with rationale and GitHub issue links
- [ ] T233 [P] RP-001 Mitigation: Create Definition of Done checklist and enforce in PR template
- [ ] T234 [P] RP-001 Mitigation: Implement scope freeze mechanism (2 weeks pre-release: feature flag to block new features)
- [ ] T235 [P] RP-001 Mitigation: Create CAB meeting template and schedule bi-weekly CAB meetings (docs/templates/cab-meeting.md)
- [ ] T236 [P] RP-002 Mitigation: Create SPIKE_PROJECT table for tracking technical spikes (technology, developer_FK, findings_document_url)
- [ ] T237 [P] RP-002 Mitigation: Dedicate Week 1 to technical spikes (Blazor, PostgreSQL, .NET ASPIRE) with deliverables documentation
- [ ] T238 [P] RP-002 Mitigation: Create ADR template and document first 3 ADRs (docs/adr/adr-template.md, ADR-001 to ADR-003)
- [ ] T239 [P] RP-002 Mitigation: Create docs/learning-resources.md with curated resources (official docs, video courses, books, code samples)
- [ ] T240 [P] RP-002 Mitigation: Setup internal knowledge base (Wiki/Notion) with How-To guides and troubleshooting FAQs
- [ ] T241 [P] RP-002 Mitigation: Schedule weekly knowledge sharing sessions (Friday 1h, rotating presenter, record sessions)
- [ ] T242 [P] RP-002 Mitigation: Create KNOWLEDGE_SESSION table and tracking dashboard (topic, presenter, recording_url, attendees_count)
- [ ] T243 [P] RP-002 Mitigation: Enforce mandatory pair programming first 2 weeks (rotate pairs daily, senior-junior pairing)
- [ ] T244 [P] RP-002 Mitigation: Implement code review checklist with "knowledge transfer" item (minimum 2 approvals)
- [ ] T245 [P] RP-002 Mitigation: Budget and schedule 5 days external consultant (architecture review week 2, code review mid-project, performance pre-launch)
- [ ] T246 [P] RP-003 Mitigation: Create SKILLS_MATRIX table with competency levels (developer_FK, skill_area, competency_level 1-4, last_updated)
- [ ] T247 [P] RP-003 Mitigation: Populate initial skills matrix for all team members (Blazor, PostgreSQL, Security, DevOps competencies)
- [ ] T248 [P] RP-003 Mitigation: Define backup owner system (primary + secondary + tertiary) for all critical areas (docs/team-ownership.md)
- [ ] T249 [P] RP-003 Mitigation: Implement TeamOwnershipService with CalculateBusFactor method (target: ≥2 per area)
- [ ] T250 [P] RP-003 Mitigation: Create skills matrix dashboard for PM/Tech Lead (competency visualization, bus factor risk alerts)
- [ ] T251 [P] RP-003 Mitigation: Create handoff checklist template (docs/templates/handoff-checklist.md with 5 sessions, verification steps)
- [ ] T252 [P] RP-003 Mitigation: Setup recording infrastructure for technical sessions (Loom/Zoom, shared drive storage with searchable index)
- [ ] T253 [P] RP-003 Mitigation: Implement pair programming rotation schedule (weekly rotation, knowledge transfer explicit goal)
- [ ] T254 [P] RP-003 Mitigation: Enforce documentation-driven development (README.md per module, inline comments, XML docs C#)
- [ ] T255 [P] RP-003 Mitigation: Reserve 10% sprint capacity for cross-training (monthly "swap day" for developers)
- [ ] T256 [P] RP-004 Mitigation: Provision Fedora 42 server Week 0 (pre-development) with base configuration complete
- [ ] T257 [P] RP-004 Mitigation: Create scripts/setup/01-os-baseline.sh (OS updates, firewall, SELinux, app user creation)
- [ ] T258 [P] RP-004 Mitigation: Create scripts/setup/02-docker-install.sh (Docker CE + Docker Compose + verification)
- [ ] T259 [P] RP-004 Mitigation: Create scripts/setup/03-postgresql-setup.sh (PostgreSQL 18 container, data volume, password generation)
- [ ] T260 [P] RP-004 Mitigation: Create scripts/setup/04-app-deployment.sh (application container deployment, health check validation)
- [ ] T261 [P] RP-004 Mitigation: Setup .NET ASPIRE AppHost project for local development (PostgreSQL resource, app orchestration)
- [ ] T262 [P] RP-004 Mitigation: Implement GitHub Actions CI/CD Week 2 (automated tests on PR, staging deployment on merge to main)
- [ ] T263 [P] RP-004 Mitigation: Setup staging environment with 100% production parity (same OS, Docker, PostgreSQL versions, resources)
- [ ] T264 [P] RP-004 Mitigation: Create deployment smoke test checklist and execute weekly deployments (docs/deployment-smoke-test.md)
- [ ] T265 [P] RP-004 Mitigation: Create infrastructure documentation (docs/infrastructure.md, docs/quickstart.md, docs/deployment.md)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-7)**: All depend on Foundational phase completion
  - User stories can proceed in parallel (if staffed)
  - Or sequentially in priority order (P1 → P2 → P3 → P4 → P5)
- **Polish (Phase 8)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational - No dependencies on other stories
- **User Story 2 (P2)**: Can start after Foundational - Uses entities from US1 but tests independently
- **User Story 3 (P3)**: Can start after Foundational - Independent of US1/US2
- **User Story 4 (P4)**: Can start after Foundational - Uses report data but generates independently
- **User Story 5 (P5)**: Can start after Foundational - Extends US3 catalog management

### Within Each User Story

- Tests MUST be written and FAIL before implementation (TDD per Constitution)
- Entities before services
- Services before controllers
- Controllers before UI components
- Core implementation before integration
- Story complete before moving to next priority

### Parallel Opportunities

- All Setup tasks marked [P] can run in parallel
- All Foundational tasks marked [P] can run in parallel (within Phase 2)
- Once Foundational phase completes, all user stories can start in parallel
- All tests for a user story marked [P] can run in parallel
- Entity creation within a story marked [P] can run in parallel
- Different user stories can be worked on in parallel by different team members

---

## Parallel Example: User Story 1

```bash
# Launch all tests for User Story 1 together:
Task: "Contract test for POST /api/reports in tests/Ceiba.Integration.Tests/ReportContractTests.cs"
Task: "Contract test for PUT /api/reports/{id} in tests/Ceiba.Integration.Tests/ReportContractTests.cs"
Task: "Contract test for POST /api/reports/{id}/submit in tests/Ceiba.Integration.Tests/ReportContractTests.cs"
Task: "Unit test for ReportService create/edit in tests/Ceiba.Application.Tests/ReportServiceTests.cs"
Task: "Unit test for report state transitions in tests/Ceiba.Core.Tests/ReporteIncidenciaTests.cs"
Task: "Component test for report form in tests/Ceiba.Web.Tests/ReportFormComponentTests.cs"

# Launch all entities for User Story 1 together:
Task: "Create Zona entity in src/Ceiba.Core/Entities/Zona.cs"
Task: "Create Sector entity in src/Ceiba.Core/Entities/Sector.cs"
Task: "Create Cuadrante entity in src/Ceiba.Core/Entities/Cuadrante.cs"
Task: "Create CatalogoSugerencia entity in src/Ceiba.Core/Entities/CatalogoSugerencia.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: Test User Story 1 independently
5. Deploy/demo if ready - CREADOR can create and submit reports

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Deploy (MVP! Core reporting works)
3. Add User Story 2 → Test independently → Deploy (Supervisor review added)
4. Add User Story 3 → Test independently → Deploy (Admin management added)
5. Add User Story 4 → Test independently → Deploy (Automated reports added)
6. Add User Story 5 → Test independently → Deploy (Full configuration)
7. Complete Polish → Final release

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1 (P1) - Core reporting
   - Developer B: User Story 3 (P3) - Admin management
   - Developer C: User Story 4 (P4) - Automated reports
3. After US1 complete: Developer A moves to User Story 2 (P2)
4. Stories complete and integrate independently

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Verify tests FAIL before implementing (TDD - Constitution Principle II)
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- All audit operations logged (Constitution Principle III)
- Mobile-first CSS throughout (Constitution Principle IV)
- Document each module as completed (Constitution Principle VI)
