# Tareas Pendientes - Sistema Ceiba

**Última actualización**: 2025-12-11
**Progreso total**: 137/330 tareas completadas (41.5%)

---

## Resumen por Fase

| Fase | Descripción | Completadas | Pendientes | Estado |
|------|-------------|-------------|------------|--------|
| 1 | Setup | 6/6 | 0 | ✅ Completa |
| 2 | Foundation | 35/35 | 0 | ✅ Completa |
| 3 | US1 - Creación Reportes | 26/26 | 0 | ✅ Completa |
| 4 | US2 - Revisión/Export | 12/14 | 2 | 🔄 En progreso |
| 5 | US3 - Admin/Auditoría | 20/20 | 0 | ✅ Completa |
| 6 | US4 - Reportes Automatizados | 22/26 | 4 | 🔄 En progreso |
| 7 | US5 - Sugerencias | 0/5 | 5 | ⏳ Pendiente |
| 8 | Polish & Cross-Cutting | 0/37 | 37 | ⏳ Pendiente |
| 9 | NFR Validation | 0/45 | 45 | ⏳ Pendiente |

---

## Tabla de Prioridades

### 🔴 Prioridad CRÍTICA (Bloquea funcionalidad core)

| ID | Tarea | Fase | Dependencias |
|----|-------|------|--------------|
| T050 | Component test for report filtering | US2 | - |
| T056 | Create ReportFilter.razor component | US2 | - |
| T082 | Unit test for AI summarization | US4 | - |
| T083 | Unit test for email service | US4 | - |
| T084 | Unit test for report aggregation | US4 | - |
| T085 | Integration test for Hangfire job | US4 | T082-T084 |

### 🟠 Prioridad ALTA (Funcionalidad de usuario)

| ID | Tarea | Fase | Dependencias |
|----|-------|------|--------------|
| T104 | Contract test for suggestion endpoints | US5 | - |
| T105 | Unit test for suggestion service | US5 | - |
| T106 | Extend CatalogAdminService for suggestions | US5 | T104-T105 |
| T107 | Add suggestion management endpoints | US5 | T106 |
| T108 | Update SuggestionManager.razor | US5 | T107 |
| T109 | Create main navigation layout | Polish | - |
| T110 | Create role-based menu component | Polish | T109 |
| T113 | Create login page | Polish | - |

### 🟡 Prioridad MEDIA (Seguridad y UX)

| ID | Tarea | Fase | Dependencias |
|----|-------|------|--------------|
| T112-T112d | WCAG AA accessibility | Polish | T109-T110 |
| T113a-T113e | Login security (reCAPTCHA, rate limit) | Polish | T113 |
| T114 | HTTPS configuration | Polish | - |
| T114a | HSTS headers | Polish | T114 |
| T118-T118b | Security hardening | Polish | - |
| T116 | GitHub Actions CI workflow | Polish | - |

### 🟢 Prioridad NORMAL (Optimización y operaciones)

| ID | Tarea | Fase | Dependencias |
|----|-------|------|--------------|
| T111 | Responsive/mobile-first CSS | Polish | - |
| T115-T115b | Backup scripts | Polish | - |
| T116a-T116e | Playwright E2E tests | Polish | T116 |
| T117-T117e | Performance optimization (indexes) | Polish | - |
| T119-T120 | Test coverage and validation | Polish | Todo previo |

### 🔵 Prioridad BAJA (Mitigaciones avanzadas)

| ID | Tarea | Fase | Descripción |
|----|-------|------|-------------|
| T121-T125 | Performance/load tests | NFR | Validación no-funcional |
| T126-T135 | RO-001 Backup system | NFR | Sistema de respaldos |
| T136-T145 | RO-002 Resource limits | NFR | Límites y monitoreo |
| T146-T155 | RO-003 Email resilience | NFR | Cola de emails |
| T156-T165 | RO-004 Circuit breakers | NFR | Resiliencia servicios |

---

## Detalle de Tareas Pendientes por Fase

### Fase 4: User Story 2 (2 pendientes)

```
- [ ] T050 [P] [US2] Component test for report filtering
      tests/Ceiba.Web.Tests/ReportFilterComponentTests.cs

- [ ] T056 [US2] Create ReportFilter.razor component
      src/Ceiba.Web/Components/Shared/ReportFilter.razor
```

**Nota**: El componente ReportFilter.razor YA EXISTE pero no tiene los tests de componente.

### Fase 6: User Story 4 (4 pendientes)

```
- [ ] T082 [P] [US4] Unit test for AI summarization
      tests/Ceiba.Application.Tests/AIServiceTests.cs

- [ ] T083 [P] [US4] Unit test for email service
      tests/Ceiba.Infrastructure.Tests/EmailServiceTests.cs

- [ ] T084 [P] [US4] Unit test for report aggregation
      tests/Ceiba.Application.Tests/AutomatedReportServiceTests.cs

- [ ] T085 [P] [US4] Integration test for Hangfire job
      tests/Ceiba.Integration.Tests/AutomatedReportJobTests.cs
```

### Fase 7: User Story 5 (5 pendientes)

```
- [ ] T104 [P] [US5] Contract test for suggestion endpoints
- [ ] T105 [P] [US5] Unit test for suggestion service
- [ ] T106 [US5] Extend CatalogAdminService for suggestions
- [ ] T107 [US5] Add suggestion management endpoints
- [ ] T108 [US5] Update SuggestionManager.razor with full CRUD
```

### Fase 8: Polish & Cross-Cutting (37 pendientes)

**UI/UX (8 tareas)**:
- T109: MainLayout.razor
- T110: NavMenu.razor (role-based)
- T111: Responsive CSS
- T112-T112d: WCAG AA accessibility (5 tareas)

**Autenticación (6 tareas)**:
- T113: Login page
- T113a-T113e: Security enhancements (reCAPTCHA, rate limit, delays, monitoring, alerts)

**Infraestructura (6 tareas)**:
- T114, T114a: HTTPS + HSTS
- T115-T115b: Backup scripts (3 tareas)
- T116: CI workflow

**Testing (5 tareas)**:
- T116a-T116e: Playwright E2E, responsive, accessibility, visual regression

**Performance (5 tareas)**:
- T117-T117e: Database indexes, full-text search, caching, pagination

**Security (3 tareas)**:
- T118-T118b: HTTP headers, CSRF, input sanitization

**Validation (4 tareas)**:
- T119-T119a: Test coverage
- T120: Quickstart validation

### Fase 9: NFR Validation (45 pendientes)

**Performance Tests (5)**:
- T121: Search <10s
- T122: PDF export <30s
- T123: 50 concurrent users
- T124: 99.5% SLA monitoring
- T125: Usability 95%

**RO-001 Backup System (10)**:
- T126-T135: Complete backup infrastructure

**RO-002 Resource Management (10)**:
- T136-T145: Docker limits, connection pools, rate limiting, monitoring

**RO-003 Email Resilience (10)**:
- T146-T155: Email queue, retries, circuit breaker, monitoring

**RO-004 Service Resilience (10)**:
- T156-T165: Circuit breakers, health checks, graceful degradation

---

## Recomendación de Orden de Implementación

### Sprint 1: Completar User Stories (Prioridad CRÍTICA)

1. **US2 - Tests pendientes**
   - T050: Tests de filtro de reportes
   - Verificar que ReportFilter.razor funciona correctamente

2. **US4 - Tests pendientes**
   - T082-T084: Tests unitarios (pueden ejecutarse en paralelo)
   - T085: Test de integración Hangfire

### Sprint 2: User Story 5 (Prioridad ALTA)

3. **US5 - Gestión de Sugerencias**
   - T104-T105: Tests primero (TDD)
   - T106-T108: Implementación

### Sprint 3: UI/UX y Autenticación (Prioridad ALTA/MEDIA)

4. **Layout y Navegación**
   - T109-T110: MainLayout y NavMenu
   - T113: Login page
   - T111: CSS responsivo

5. **Seguridad de Login**
   - T113a-T113e: reCAPTCHA, rate limiting, delays

### Sprint 4: Accesibilidad y Testing (Prioridad MEDIA)

6. **WCAG AA Compliance**
   - T112-T112d: Todas las tareas de accesibilidad

7. **CI/CD y E2E**
   - T116: GitHub Actions
   - T116a-T116e: Playwright tests

### Sprint 5: Performance y Seguridad (Prioridad NORMAL)

8. **Performance**
   - T117-T117e: Índices y optimizaciones

9. **Security Hardening**
   - T114, T114a: HTTPS/HSTS
   - T118-T118b: Headers y sanitización

### Sprint 6+: NFR y Operaciones (Prioridad BAJA)

10. **Validación y Operaciones**
    - T119-T120: Coverage y validación
    - T121-T165: Todas las mitigaciones de riesgos operacionales

---

## Notas Importantes

1. **TDD Obligatorio**: Todas las tareas de implementación deben tener sus tests escritos primero.

2. **Tareas Marcadas [P]**: Pueden ejecutarse en paralelo con otras tareas [P] de la misma fase.

3. **Dependencias**: Verificar que las tareas prerequisito estén completas antes de iniciar.

4. **Tests Actuales**: 312 tests pasando, 6 omitidos (integración con servicios externos).

5. **Build Status**: 38 advertencias menores (SonarAnalyzer en Razor), sin errores.
