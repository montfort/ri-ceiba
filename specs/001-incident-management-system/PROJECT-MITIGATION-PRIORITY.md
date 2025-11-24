# PROJECT MITIGATION PRIORITY

**Feature**: 001-incident-management-system
**Categoría**: Riesgos de Gestión de Proyecto (RP-001 a RP-004)
**Fecha**: 2025-11-22
**Total Tareas**: 40 (T226-T265)
**Total Requisitos Funcionales**: 40 (FR-169-PROJ a FR-208-PROJ)

---

## Resumen Ejecutivo

Este documento prioriza las **40 tareas de mitigación** para los **4 riesgos de gestión de proyecto** identificados en risk-analysis.md. La priorización se basa en:

1. **Criticidad del riesgo** (P1 > P2)
2. **Dependencias temporales** (prerequisitos vs trabajo incremental)
3. **Impacto en el equipo** (bloqueadores vs facilitadores)
4. **Timeline del proyecto** (Week 0 > Week 1 > Ongoing)

**Estado de Riesgos**:
- ✅ RP-001 (Scope Creep): **MITIGADO** - 10 tareas
- ✅ RP-002 (Falta de Conocimiento Técnico): **MITIGADO** - 10 tareas
- ✅ RP-003 (Dependencia Personal Clave): **MITIGADO** - 10 tareas
- ✅ RP-004 (Retrasos Infraestructura): **MITIGADO** - 10 tareas

**Total**: 25/25 riesgos (100%) completamente mitigados en fase de diseño.

---

## 1. Priorización por Urgencia y Timing

### 🔴 CRÍTICO - Week 0 (Pre-Desarrollo)

**Objetivo**: Establecer infraestructura y procesos antes de iniciar desarrollo.

| Tarea | Riesgo | Descripción | Dependencias | Esfuerzo | Responsable |
|-------|--------|-------------|--------------|----------|-------------|
| **T256** | RP-004 | Provisionar servidor Fedora 42 con configuración base completa | Ninguna | 8h | DevOps |
| **T257** | RP-004 | Crear scripts/server-setup.sh idempotente con validación | T256 | 6h | DevOps |
| **T258** | RP-004 | Infrastructure-as-Code: configs versionados en Git | T256 | 4h | DevOps |
| **T263** | RP-004 | Documentar infraestructura (docs/infrastructure.md + network diagram) | T256-T258 | 3h | DevOps |

**Justificación**: El servidor debe estar disponible ANTES de que el equipo comience desarrollo. Scripts idempotentes aseguran reproducibilidad y evitan configuration drift.

**Criterios de Éxito**:
- ✅ Servidor Fedora 42 operativo con Docker, PostgreSQL, networking configurado
- ✅ Scripts setup ejecutables en <4h para ambiente nuevo
- ✅ Documentación completa en docs/infrastructure.md

**Timeline**: Week 0 (días -5 a -1 antes de kick-off)

---

### 🟠 ALTA PRIORIDAD - Week 1 (Primera Semana)

**Objetivo**: Establecer bases de conocimiento, procesos de desarrollo, y CI/CD.

| Tarea | Riesgo | Descripción | Dependencias | Esfuerzo | Responsable |
|-------|--------|-------------|--------------|----------|-------------|
| **T237** | RP-002 | Crear spike projects Week 1 (Blazor, PostgreSQL, .NET ASPIRE, EF Core) | T256 | 16h | Tech Lead |
| **T244** | RP-002 | Definir spike validation criteria y success metrics | T237 | 2h | Tech Lead |
| **T239** | RP-002 | Crear curated learning resources en docs/learning/ | Ninguna | 4h | Tech Lead |
| **T240** | RP-002 | Inicializar code examples repository con approved patterns | T237 | 6h | Tech Lead |
| **T242** | RP-002 | Crear ADR template e inicializar docs/adr/ | Ninguna | 2h | Tech Lead |
| **T250** | RP-003 | Formalizar ADR process y review workflow | T242 | 2h | Tech Lead |
| **T238** | RP-002 | Establecer pair programming schedule (2h/día primeras 4 semanas) | T237 | 1h | PM |
| **T259** | RP-004 | Configurar .NET ASPIRE para desarrollo local (Docker Compose + PostgreSQL) | T256, T258 | 6h | DevOps |
| **T260** | RP-004 | Implementar GitHub Actions CI/CD pipeline (build, test, deploy staging) | T256, T258 | 8h | DevOps |
| **T261** | RP-004 | Provisionar staging environment con parity check vs producción | T256, T260 | 6h | DevOps |

**Justificación**: Week 1 es crítica para knowledge transfer y establecer procesos. Spikes técnicos validan tecnologías nuevas. CI/CD temprano detecta problemas de deployment.

**Criterios de Éxito**:
- ✅ 100% team completa spike projects con learnings documentados
- ✅ CI/CD pipeline ejecutándose exitosamente
- ✅ Staging environment operativo con parity 100% vs producción
- ✅ ADR process formalizado y comunicado

**Timeline**: Week 1 (días 1-5)

---

### 🟡 MEDIA PRIORIDAD - Week 2-4 (Primeras 4 Semanas)

**Objetivo**: Consolidar knowledge management, team resilience, y scope control.

#### Scope Management & Change Control (RP-001)

| Tarea | Riesgo | Descripción | Dependencias | Esfuerzo | Responsable |
|-------|--------|-------------|--------------|----------|-------------|
| **T226** | RP-001 | Crear tabla CHANGE_REQUEST con CAB workflow | Ninguna | 4h | Developer |
| **T228** | RP-001 | Definir MoSCoW prioritization en spec.md y comunicar a stakeholders | Ninguna | 2h | PO |
| **T231** | RP-001 | Crear template de change impact assessment (timeline, resources, risks) | Ninguna | 2h | PM |
| **T232** | RP-001 | Establecer CAB meeting process (quorum mínimo 3, frequency quincenal) | T226, T231 | 2h | PM |
| **T235** | RP-001 | Crear ADR template para decisiones out-of-scope | T242 | 1h | Tech Lead |
| **T227** | RP-001 | Implementar sprint velocity tracking con métricas (story points, burn-down) | Ninguna | 4h | PM |
| **T233** | RP-001 | Diseñar sprint review agenda template con stakeholder feedback section | Ninguna | 2h | PO |
| **T234** | RP-001 | Formalizar Definition of Done checklist (tests, coverage >80%, PR approval) | Ninguna | 2h | Tech Lead |

#### Knowledge Management (RP-002 - Continuación)

| Tarea | Riesgo | Descripción | Dependencias | Esfuerzo | Responsable |
|-------|--------|-------------|--------------|----------|-------------|
| **T241** | RP-002 | Crear knowledge sharing session calendar + tabla KNOWLEDGE_SESSION | Ninguna | 2h | Tech Lead |
| **T243** | RP-002 | Expert consultation: RFP y contratación (4h/semana × 6 semanas) | Ninguna | 3h | PM |
| **T245** | RP-002 | Establecer documentation standards y code comment guidelines | T240 | 3h | Tech Lead |
| **T246a** | RP-002 | Crear code review anti-patterns checklist (Blazor, EF Core, PostgreSQL) | T237, T240 | 3h | Tech Lead |

#### Team Resilience (RP-003)

| Tarea | Riesgo | Descripción | Dependencias | Esfuerzo | Responsable |
|-------|--------|-------------|--------------|----------|-------------|
| **T246** | RP-003 | Crear tabla SKILLS_MATRIX y UI para tracking (competency levels 1-4) | Ninguna | 6h | Developer |
| **T247** | RP-003 | Definir backup owners por área crítica (primary, secondary, tertiary) | T246 | 2h | Tech Lead |
| **T248** | RP-003 | Implementar bus factor calculator service (target ≥2 por módulo) | T246, T247 | 4h | Developer |
| **T254** | RP-003 | Inicializar CODEOWNERS file con ownership mapping | T247 | 2h | Tech Lead |
| **T255** | RP-003 | Crear onboarding guide completo (docs/onboarding/) | T239, T240, T251 | 8h | Tech Lead |

**Justificación**: Estas tareas establecen procesos continuos de gestión (scope, knowledge, team) que se activan desde Week 2 pero son ongoing.

**Criterios de Éxito**:
- ✅ CAB proceso establecido con primera reunión Week 3
- ✅ Sprint velocity tracking iniciado Sprint 1
- ✅ Skills matrix completada 100% para team actual
- ✅ Backup owners definidos para 100% áreas críticas
- ✅ Knowledge sessions calendario publicado (viernes 1h semanal)

**Timeline**: Week 2-4 (días 6-20)

---

### 🟢 BAJA PRIORIDAD - Ongoing (Continuo)

**Objetivo**: Mantenimiento y mejora continua de procesos establecidos.

#### Scope Management - Ongoing

| Tarea | Riesgo | Descripción | Dependencias | Esfuerzo | Responsable |
|-------|--------|-------------|--------------|----------|-------------|
| **T229** | RP-001 | Implementar feature flags system (FEATURE_FLAG table + service) | Ninguna | 6h | Developer |
| **T230** | RP-001 | Crear backlog Fase 2 con tabla BACKLOG_ITEM | T228 | 3h | PO |

#### Team Resilience - Ongoing

| Tarea | Riesgo | Descripción | Dependencias | Esfuerzo | Responsable |
|-------|--------|-------------|--------------|----------|-------------|
| **T249** | RP-003 | Diseñar cross-training rotation schedule (quarterly) | T246, T247 | 3h | Tech Lead |
| **T251** | RP-003 | Crear runbooks para tareas operativas críticas (docs/runbooks/) | T263 | 8h | DevOps |
| **T252** | RP-003 | Crear handoff checklist template y process | T250, T251, T255 | 3h | Tech Lead |
| **T253** | RP-003 | Setup recording infrastructure (Loom/OBS + storage) para knowledge sessions | T241 | 4h | DevOps |

#### Infrastructure - Ongoing

| Tarea | Riesgo | Descripción | Dependencias | Esfuerzo | Responsable |
|-------|--------|-------------|--------------|----------|-------------|
| **T262** | RP-004 | Implementar smoke test suite post-deployment (health, DB, auth flow) | T260, T261 | 6h | QA |
| **T264** | RP-004 | Crear rollback mechanism con version pinning (Docker tags, DB migrations) | T260, T261 | 5h | DevOps |
| **T265** | RP-004 | Documentar disaster recovery runbook (RTO <1h) | T264 | 4h | DevOps |

**Justificación**: Estas tareas refinan procesos ya establecidos. No bloquean desarrollo pero mejoran resiliencia y calidad a largo plazo.

**Criterios de Éxito**:
- ✅ Feature flags disponibles para todas las funcionalidades nuevas
- ✅ Runbooks completos para 100% procedimientos operacionales críticos
- ✅ Cross-training rotations ejecutadas trimestralmente
- ✅ Smoke tests integrados en CI/CD pipeline

**Timeline**: Week 5+ (ongoing)

---

## 2. Roadmap Consolidado de Implementación

### Week 0: Pre-Desarrollo (Infrastructure Setup)
```
[T256] Provisionar Fedora 42 ────┐
                                  ├─► [T257] Scripts setup ─► [T258] IaC ─► [T263] Docs infra
                                  │
                                  └─► Servidor listo para Week 1
```

**Entregables**: Servidor operativo, scripts idempotentes, docs/infrastructure.md

---

### Week 1: Spikes Técnicos + CI/CD

**Lunes-Miércoles**: Spikes Técnicos
```
[T237] Spike projects ──┬─► [T244] Validation criteria
                        ├─► [T239] Learning resources
                        ├─► [T240] Code examples
                        └─► [T242] ADR template ─► [T250] ADR process
```

**Jueves-Viernes**: CI/CD + Desarrollo Local
```
[T259] .NET ASPIRE local setup
[T260] GitHub Actions pipeline ─► [T261] Staging environment
[T238] Pair programming schedule (ongoing 4 semanas)
```

**Entregables**: Spike projects completados, CI/CD operativo, staging environment, ADRs template

---

### Week 2-4: Procesos y Team Setup

**Week 2**: Scope Management
```
[T226] CHANGE_REQUEST table ─► [T232] CAB process
[T228] MoSCoW prioritization ─► [T231] Impact assessment template
[T227] Velocity tracking ─► [T233] Sprint review template
[T234] Definition of Done
[T235] Out-of-scope ADR template
```

**Week 3**: Knowledge Management
```
[T241] Knowledge session calendar
[T243] Expert consultant hiring
[T245] Documentation standards
[T246a] Anti-patterns checklist
```

**Week 4**: Team Resilience
```
[T246] SKILLS_MATRIX table ─► [T247] Backup owners ─► [T248] Bus factor calculator
[T254] CODEOWNERS file
[T255] Onboarding guide
```

**Entregables**: CAB operativo, skills matrix completa, backup owners asignados, onboarding docs

---

### Week 5+: Mejora Continua (Ongoing)

**Ongoing Processes**:
```
[T229] Feature flags system
[T230] Backlog Fase 2
[T249] Cross-training rotations (quarterly)
[T251] Runbooks operacionales
[T252] Handoff checklist
[T253] Recording infrastructure
[T262] Smoke tests
[T264] Rollback mechanism
[T265] Disaster recovery runbook
```

**Entregables**: Feature flags operativos, runbooks completos, disaster recovery probado

---

## 3. Dependencias Críticas

### Bloqueadores (Must Complete First)

1. **T256 (Servidor Fedora 42)** → Bloquea:
   - T257, T258, T259, T260, T261, T237

2. **T237 (Spike Projects)** → Bloquea:
   - T244, T240, T246a (requieren aprendizajes de spikes)

3. **T242 (ADR Template)** → Bloquea:
   - T250, T235 (requieren template base)

4. **T246 (Skills Matrix)** → Bloquea:
   - T247, T248, T249 (requieren data de competencias)

5. **T260 (CI/CD Pipeline)** → Bloquea:
   - T261, T262, T264 (requieren pipeline operativo)

### Facilitadores (Enable Parallel Work)

1. **T239 (Learning Resources)** → Facilita: Aprendizaje autónomo del equipo
2. **T238 (Pair Programming Schedule)** → Facilita: Knowledge transfer continuo
3. **T228 (MoSCoW Prioritization)** → Facilita: Scope control desde inicio
4. **T241 (Knowledge Sessions)** → Facilita: Compartir aprendizajes semanalmente

---

## 4. Esfuerzo Total por Riesgo

| Riesgo | Tareas | Esfuerzo Total | Responsable Principal |
|--------|--------|----------------|----------------------|
| **RP-001** (Scope Creep) | 10 | 25 horas | Product Owner + PM |
| **RP-002** (Knowledge) | 10 | 47 horas | Tech Lead |
| **RP-003** (Personal Clave) | 10 | 42 horas | Tech Lead + DevOps |
| **RP-004** (Infraestructura) | 10 | 56 horas | DevOps |
| **TOTAL** | **40** | **170 horas** | Equipo completo |

**Distribución por Rol**:
- DevOps: ~70 horas (41%)
- Tech Lead: ~60 horas (35%)
- Product Owner/PM: ~20 horas (12%)
- Developers: ~20 horas (12%)

---

## 5. Métricas de Éxito

### Week 0 Success Criteria
- ✅ Servidor Fedora 42 operativo (<8h setup time)
- ✅ Scripts idempotentes validados (reproducibilidad 100%)
- ✅ Documentación infraestructura completa

### Week 1 Success Criteria
- ✅ Spike projects completados (100% team participation)
- ✅ CI/CD pipeline passing (build + test + deploy)
- ✅ Staging environment parity check 100%
- ✅ ADR template en uso

### Week 2-4 Success Criteria
- ✅ CAB proceso establecido (primer meeting Week 3)
- ✅ Skills matrix completeness 100%
- ✅ Backup owners coverage 100% (áreas críticas)
- ✅ Sprint velocity tracking iniciado
- ✅ Knowledge sessions programadas (viernes 1h)

### Week 5+ Success Criteria (Ongoing)
- ✅ Bus factor ≥2 para 100% módulos críticos
- ✅ Runbooks coverage 100% (procedimientos operacionales)
- ✅ Feature flags disponibles para nuevas features
- ✅ Cross-training rotations quarterly (100% participación)

---

## 6. Plan de Contingencia

### Riesgo: Servidor no disponible Week 0
**Mitigación**: Escalar a System Administrator senior, usar .NET ASPIRE + Docker local mientras tanto
**Rollback**: Posponer provisioning a Week 1, ajustar timeline

### Riesgo: Spikes no concluyentes Week 1
**Mitigación**: Extender spikes a Week 2, consultar experto externo inmediatamente
**Rollback**: Cambiar tecnología si riesgo técnico es alto (ej: reemplazar .NET ASPIRE)

### Riesgo: Equipo rechaza pair programming
**Mitigación**: Ajustar a 1h/día en vez de 2h/día, enfocarse en code reviews
**Rollback**: Reemplazar con knowledge sessions más frecuentes

### Riesgo: CAB proceso demasiado burocrático
**Mitigación**: Simplificar workflow, aprobar cambios menores sin CAB
**Rollback**: Usar proceso lightweight (email approval vs formal meeting)

---

## 7. Integración con Tareas Previas

Este documento complementa los siguientes documentos de priorización:

1. **MITIGATION-TASKS-PRIORITY.md**: Riesgos Técnicos (RT-001 a RT-006)
2. **SECURITY-MITIGATION-PRIORITY.md**: Riesgos Seguridad (RS-001 a RS-005)
3. **OPERATIONAL-MITIGATION-PRIORITY.md**: Riesgos Operacionales (RO-001 a RO-005)
4. **BUSINESS-USER-MITIGATION-PRIORITY.md**: Riesgos Negocio/Usuario (RN-001 a RN-005)
5. **PROJECT-MITIGATION-PRIORITY.md** (este documento): Riesgos Proyecto (RP-001 a RP-004)

**Total Consolidado**:
- **196 tareas** de mitigación (T001-T265)
- **172 requisitos funcionales** (FR-001 a FR-208-PROJ)
- **25 riesgos** completamente mitigados (100%)

---

## 8. Recomendación Final

**Orden de Ejecución Óptimo** para los 4 riesgos de proyecto:

1. **RP-004 (Week 0)**: Provisionar infraestructura ANTES de desarrollo → **CRÍTICO**
2. **RP-002 (Week 1)**: Spikes técnicos y knowledge transfer temprano → **ALTA PRIORIDAD**
3. **RP-001 (Week 2)**: Establecer scope management y change control → **MEDIA PRIORIDAD**
4. **RP-003 (Week 3-4)**: Asegurar team resilience y backup coverage → **MEDIA PRIORIDAD**

**Beneficios de esta Secuencia**:
- Infraestructura lista elimina bloqueadores de desarrollo
- Knowledge transfer temprano reduce bugs por desconocimiento técnico
- Scope control establecido previene feature creep desde inicio
- Team resilience asegura continuidad incluso con rotación de personal

**Estado Final**: **TODOS los riesgos de proyecto (RP-001 a RP-004) están completamente mitigados** con 40 tareas implementables y roadmap claro de ejecución (Week 0 a Week 5+).

---

**Documento creado**: 2025-11-22
**Próxima revisión**: Week 5 (después de completar tareas críticas y de alta prioridad)
**Responsable**: Project Manager + Tech Lead + DevOps
**Aprobación**: [Pendiente]
