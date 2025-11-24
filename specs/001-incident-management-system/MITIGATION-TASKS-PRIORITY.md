# Priorización de Tareas de Mitigación de Riesgos Técnicos

**Fecha**: 2025-11-21
**Total de Tareas**: 30 tareas específicas de mitigación

---

## 🎯 Criterios de Priorización

Las tareas se priorizan según:
1. **Prioridad del Riesgo Original** (P1 > P2 > P3)
2. **Fase de Implementación** (Setup/Foundation > User Stories > Polish)
3. **Dependencias** (Blocking tasks primero)
4. **Impacto en Múltiples Historias** (Cross-cutting primero)

---

## 📊 Distribución por Fase del Proyecto

| Fase | Riesgos | Tareas | % del Total |
|------|---------|--------|-------------|
| **Phase 1: Setup** | RT-005, RT-006 | 2 | 6.7% |
| **Phase 2: Foundational** | RT-004 | 5 | 16.7% |
| **Phase 4: User Story 2** | RT-003 | 5 | 16.7% |
| **Phase 6: User Story 4** | RT-001, RT-006 | 9 | 30.0% |
| **Phase 8: Polish** | RT-002, RT-005 | 10 | 33.3% |

---

## 🔴 CRÍTICO: Tareas Fundacionales (Blocking)

**Deben completarse ANTES de iniciar User Stories**

### Phase 1: Setup (2 tareas)

| ID | Riesgo | Tarea | Justificación |
|----|--------|-------|---------------|
| **T002** | RT-005 | Initialize .NET 10 projects with Playwright | Infraestructura base de testing |
| **T003a** | RT-006 | Add Pandoc to Dockerfile | Dependencia de runtime necesaria |

**Estimación**: 1-2 horas
**Bloquea**: Todo el desarrollo posterior

---

### Phase 2: Foundational (5 tareas)

| ID | Riesgo | Tarea | Justificación |
|----|--------|-------|---------------|
| **T019a** | RT-004 | Add campos_adicionales (JSONB) + schema_version | Extensibilidad del modelo de datos |
| **T019b** | RT-004 | Create MIGRATIONS.md changelog | Documentación de cambios de esquema |
| **T019c** | RT-004 | Implement pre-migration backup script | Seguridad en migraciones |
| **T019d** | RT-004 | Add feature flag configuration system | Control de despliegue de features |
| **T019e** | RT-004 | Create migration validation scripts | Integridad post-migración |

**Estimación**: 1-2 días
**Bloquea**: Todas las User Stories (requieren esquema de BD)

---

## 🟡 ALTA PRIORIDAD: Tareas de User Stories Core

**Implementar durante desarrollo de funcionalidad principal**

### User Story 2: Exportación (5 tareas) - Riesgo P2

| ID | Riesgo | Tarea | Implementar en |
|----|--------|-------|----------------|
| **T052a** | RT-003 | Enforce export limits (50 PDFs, 100 JSONs) | ExportService.cs |
| **T052b** | RT-003 | Implement streaming PDF generation | FileStreamResult |
| **T052c** | RT-003 | Create background export job with email | ExportJob.cs |
| **T052d** | RT-003 | Configure Hangfire limits (3 jobs, 2min timeout) | Program.cs |
| **T052e** | RT-003 | Add export monitoring with alerts | ExportService.cs |

**Estimación**: 2-3 días
**Dependencias**: T051 (IExportService), T052 (ExportService base)
**Prioridad**: ⭐⭐⭐ Alta (User Story P2, export es funcionalidad crítica para REVISOR)

---

### User Story 4: Reportes Automatizados (9 tareas) - Riesgo P2 y P3

#### Bloque A: Integración con IA (5 tareas) - RT-001 (Riesgo P2)

| ID | Riesgo | Tarea | Implementar en |
|----|--------|-------|----------------|
| **T092a** | RT-001 | Configure Polly policies (30s timeout, circuit breaker) | AIService.cs |
| **T092b** | RT-001 | Implement AIServiceMock | Mocks/AIServiceMock.cs |
| **T092c** | RT-001 | Add response caching (IMemoryCache) | AIService.cs |
| **T092d** | RT-001 | Implement graceful fallback (stats-only reports) | AutomatedReportService.cs |
| **T092e** | RT-001 | Add AI call monitoring | AutomatedReportService.cs |

**Estimación**: 1-2 días
**Dependencias**: T092 (AIService base)
**Prioridad**: ⭐⭐ Media-Alta (User Story P4, pero integración externa compleja)

---

#### Bloque B: Conversión Pandoc (4 tareas) - RT-006 (Riesgo P3)

| ID | Riesgo | Tarea | Implementar en |
|----|--------|-------|----------------|
| **T094a** | RT-006 | Add Pandoc availability check at startup | Program.cs |
| **T094b** | RT-006 | Implement 10-second timeout on Pandoc | DocumentConversionService.cs |
| **T094c** | RT-006 | Add HTML email fallback if Pandoc fails | DocumentConversionService.cs |
| **T094d** | RT-006 | Create integration tests with Markdown samples | ConversionTests.cs |

**Estimación**: 1 día
**Dependencias**: T094 (DocumentConversionService base), T003a (Pandoc en Docker)
**Prioridad**: ⭐⭐ Media (User Story P4, dependencia externa)

---

## 🟢 OPTIMIZACIÓN: Tareas de Polish

**Implementar DESPUÉS de funcionalidad core validada**

### Phase 8: Rendimiento de Búsquedas (5 tareas) - RT-002 (Riesgo P1)

| ID | Riesgo | Tarea | Implementar en |
|----|--------|-------|----------------|
| **T117a** | RT-002 | Create composite indexes | EF Core Migration |
| **T117b** | RT-002 | Implement PostgreSQL full-text search (GIN) | EF Core Migration |
| **T117c** | RT-002 | Add search result caching (IMemoryCache, 5min TTL) | ReportService.cs |
| **T117d** | RT-002 | Enforce pagination limit (500 records/page) | ReportService.cs |
| **T117e** | RT-002 | Add EXPLAIN ANALYZE in integration tests | PerformanceTests.cs |

**Estimación**: 2 días
**Dependencias**: T053 (ReportService search methods), datos de prueba >1000 reportes
**Prioridad**: ⭐⭐⭐⭐ MUY ALTA (Riesgo P1, pero puede implementarse después de MVP)
**Nota**: Aunque es riesgo P1, se implementa en Phase 8 porque requiere volumen de datos para validar

---

### Phase 8: Cross-Browser Testing (5 tareas) - RT-005 (Riesgo P3)

| ID | Riesgo | Tarea | Implementar en |
|----|--------|-------|----------------|
| **T116a** | RT-005 | Configure Playwright E2E tests (Chrome, Firefox, Edge, Safari) | E2E.Tests/ |
| **T116b** | RT-005 | Add responsive viewport tests (4 viewports) | Playwright suite |
| **T116c** | RT-005 | Integrate axe-core a11y checks | Playwright suite |
| **T116d** | RT-005 | Add visual regression testing | Playwright suite |
| **T116e** | RT-005 | Configure Playwright in CI/CD (blocking merge) | GitHub Actions |

**Estimación**: 2-3 días
**Dependencias**: T116 (CI/CD base), UI completas de User Stories 1-3
**Prioridad**: ⭐⭐⭐ Alta (Calidad, pero no bloquea funcionalidad)

---

## 📋 Plan de Implementación Recomendado

### Sprint 0: Setup & Foundation (2-3 días)
```
✅ COMPLETAR PRIMERO (BLOQUEA TODO):
1. T002 - Playwright en dependencias
2. T003a - Pandoc en Docker
3. T019a - Campos JSONB + schema_version
4. T019b - MIGRATIONS.md
5. T019c - Script de backup pre-migración
6. T019d - Sistema de feature flags
7. T019e - Scripts de validación de migraciones
```
**Resultado**: Infraestructura lista para User Stories

---

### Sprint 1-2: User Story 1 (CREADOR - Creación de Reportes)
```
Sin tareas de mitigación específicas en US1
→ Implementar funcionalidad core según tasks.md originales
```

---

### Sprint 3-4: User Story 2 (REVISOR - Exportación)
```
✅ IMPLEMENTAR CON LA FUNCIONALIDAD:
8. T052a - Límites de exportación (50 PDFs, 100 JSONs)
9. T052b - Streaming PDF
10. T052c - Background export job
11. T052d - Hangfire limits
12. T052e - Export monitoring
```
**Resultado**: Exportación robusta y escalable

---

### Sprint 5: User Story 3 (ADMIN - Gestión)
```
Sin tareas de mitigación específicas en US3
→ Implementar funcionalidad core según tasks.md originales
```

---

### Sprint 6-7: User Story 4 (Reportes Automatizados)
```
✅ BLOQUE A - IA Integration:
13. T092a - Polly policies
14. T092b - AIServiceMock
15. T092c - AI response caching
16. T092d - Graceful fallback
17. T092e - AI monitoring

✅ BLOQUE B - Pandoc Integration:
18. T094a - Pandoc startup check
19. T094b - Pandoc timeout
20. T094c - HTML fallback
21. T094d - Pandoc integration tests
```
**Resultado**: Reportes automatizados confiables

---

### Sprint 8-9: Polish & Optimization
```
✅ GRUPO A - Performance:
22. T117a - Composite indexes
23. T117b - Full-text search indexes
24. T117c - Search caching
25. T117d - Pagination limits
26. T117e - EXPLAIN ANALYZE tests

✅ GRUPO B - Cross-Browser:
27. T116a - Playwright multi-browser
28. T116b - Responsive viewports
29. T116c - Axe-core a11y
30. T116d - Visual regression
31. T116e - CI/CD integration
```
**Resultado**: Sistema optimizado y validado en todos los navegadores

---

## 🎯 Orden de Ejecución Óptimo

### Secuencia por Dependencias

```
NIVEL 1 (Blocking - Día 1):
├── T002 (RT-005) ─┐
└── T003a (RT-006) ─┼─> HABILITA NIVEL 2

NIVEL 2 (Foundation - Días 2-3):
├── T019a (RT-004) ─┐
├── T019b (RT-004) ─┤
├── T019c (RT-004) ─┼─> HABILITA TODAS LAS USER STORIES
├── T019d (RT-004) ─┤
└── T019e (RT-004) ─┘

NIVEL 3 (User Story 2 - Sprint 3-4):
├── T052a (RT-003) ─┐
├── T052b (RT-003) ─┤
├── T052c (RT-003) ─┼─> EXPORTACIÓN COMPLETA
├── T052d (RT-003) ─┤
└── T052e (RT-003) ─┘

NIVEL 4 (User Story 4 - Sprint 6-7):
├── T092a (RT-001) ─┐
├── T092b (RT-001) ─┤
├── T092c (RT-001) ─┼─> IA INTEGRATION
├── T092d (RT-001) ─┤
├── T092e (RT-001) ─┘
├── T094a (RT-006) ─┐
├── T094b (RT-006) ─┤
├── T094c (RT-006) ─┼─> PANDOC INTEGRATION
└── T094d (RT-006) ─┘

NIVEL 5 (Polish - Sprint 8-9):
├── T117a (RT-002) ─┐
├── T117b (RT-002) ─┤
├── T117c (RT-002) ─┼─> PERFORMANCE OPTIMIZATION
├── T117d (RT-002) ─┤
├── T117e (RT-002) ─┘
├── T116a (RT-005) ─┐
├── T116b (RT-005) ─┤
├── T116c (RT-005) ─┼─> CROSS-BROWSER VALIDATION
├── T116d (RT-005) ─┤
└── T116e (RT-005) ─┘
```

---

## 📊 Estimaciones de Esfuerzo

| Fase | Tareas | Días de Desarrollo | Story Points |
|------|--------|-------------------|--------------|
| Setup & Foundation | 7 | 2-3 días | 8 |
| User Story 2 | 5 | 2-3 días | 5 |
| User Story 4 - IA | 5 | 1-2 días | 5 |
| User Story 4 - Pandoc | 4 | 1 día | 3 |
| Polish - Performance | 5 | 2 días | 5 |
| Polish - Cross-Browser | 5 | 2-3 días | 5 |
| **TOTAL** | **30** | **10-14 días** | **31 SP** |

**Nota**: Estimaciones asumen 1 desarrollador full-time. Con equipo de 2-3 personas, algunas fases se pueden paralelizar.

---

## 🔍 Tareas Paralelizables

Todas las tareas de mitigación están marcadas con `[P]` (excepto T002 que no necesita el marcador).

### Dentro de cada fase:

**Foundation (T019a-e)**: Todas paralelizables
- Developer A: T019a, T019b (Esquema + docs)
- Developer B: T019c, T019d (Scripts + feature flags)
- Developer C: T019e (Validación)

**User Story 2 (T052a-e)**: Secuencia recomendada
1. T052a, T052b (límites + streaming) - paralelo
2. T052c (background job) - depende de 1
3. T052d (Hangfire config) - depende de 2
4. T052e (monitoring) - paralelo con 3

**User Story 4 - IA (T092a-e)**: Todas paralelizables
- Developer A: T092a, T092b (Polly + Mock)
- Developer B: T092c, T092d (Cache + Fallback)
- Developer C: T092e (Monitoring)

**User Story 4 - Pandoc (T094a-d)**: Todas paralelizables

**Performance (T117a-e)**: Secuencia recomendada
1. T117a, T117b (crear índices) - debe ejecutarse primero
2. T117c, T117d, T117e (código + tests) - paralelo después de 1

**Cross-Browser (T116a-e)**: Todas paralelizables

---

## ✅ Checklist de Completitud por Riesgo

### RT-001: Integración con IA
- [ ] T092a - Polly timeout + circuit breaker
- [ ] T092b - Mock service para testing
- [ ] T092c - Caché de respuestas
- [ ] T092d - Fallback a stats-only
- [ ] T092e - Monitoring de llamadas
- [ ] **Criterio de Aceptación**: Reportes se generan con/sin IA disponible, <15s latencia promedio

### RT-002: Rendimiento de Búsquedas
- [ ] T117a - Índices compuestos
- [ ] T117b - Full-text search GIN
- [ ] T117c - Caché de búsquedas
- [ ] T117d - Paginación forzada
- [ ] T117e - EXPLAIN ANALYZE tests
- [ ] **Criterio de Aceptación**: Búsqueda en 10,000 reportes <3s, tests validan uso de índices

### RT-003: Generación de PDF
- [ ] T052a - Límites de exportación
- [ ] T052b - Streaming PDF
- [ ] T052c - Background jobs
- [ ] T052d - Hangfire concurrency
- [ ] T052e - Export monitoring
- [ ] **Criterio de Aceptación**: 50 PDFs en <30s, exportaciones >50 via background job

### RT-004: Migraciones de Esquema
- [ ] T019a - JSONB + schema_version
- [ ] T019b - MIGRATIONS.md
- [ ] T019c - Pre-migration backup
- [ ] T019d - Feature flags
- [ ] T019e - Validation scripts
- [ ] **Criterio de Aceptación**: Tipo B de reporte agregable sin migración, MIGRATIONS.md actualizado

### RT-005: Cross-Browser
- [ ] T002 - Playwright en dependencies
- [ ] T116a - Multi-browser tests
- [ ] T116b - Responsive viewports
- [ ] T116c - Axe-core a11y
- [ ] T116d - Visual regression
- [ ] T116e - CI/CD integration
- [ ] **Criterio de Aceptación**: Tests pasan en 4 navegadores x 4 viewports, a11y score 100%

### RT-006: Dependencia de Pandoc
- [ ] T003a - Pandoc en Dockerfile
- [ ] T094a - Startup validation
- [ ] T094b - Process timeout
- [ ] T094c - HTML fallback
- [ ] T094d - Integration tests
- [ ] **Criterio de Aceptación**: App falla rápido si Pandoc missing, fallback funciona, conversión <3s

---

## 📝 Notas de Implementación

### Consideraciones Especiales

1. **RT-004 (Foundation)**: DEBE completarse antes de primera migración EF Core
2. **RT-002 (Performance)**: Requiere datos de prueba (seed >1000 reportes para validar)
3. **RT-003 (Export)**: Probar con reportes reales de diferentes tamaños
4. **RT-005 (Cross-Browser)**: Ejecutar en CI/CD desde primer PR de UI
5. **RT-001 (IA)**: Usar mock en development, API real solo en staging/prod

### Definition of Done para Tareas de Mitigación

- [ ] Código implementado según especificación
- [ ] Tests unitarios passing (si aplica)
- [ ] Tests de integración passing (si aplica)
- [ ] Documentación actualizada (research.md, data-model.md, etc.)
- [ ] Code review aprobado con checklist de seguridad
- [ ] Validación manual de mitigación (smoke test)
- [ ] Actualizado en risk-analysis.md como "Implementado"

---

**Documento creado**: 2025-11-21
**Próxima revisión**: Al completar cada sprint
**Responsable de tracking**: Project Manager / Tech Lead
