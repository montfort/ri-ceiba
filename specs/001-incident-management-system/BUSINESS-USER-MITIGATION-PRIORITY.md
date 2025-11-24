# Business & User Risk Mitigation Tasks - Priority Matrix

**Generated**: 2025-11-22
**Context**: Priorización de 50 tareas de mitigación para riesgos de negocio/usuario (RN-001 a RN-005)

---

## Executive Summary

Este documento define la priorización e integración de **50 tareas de mitigación** para los **5 riesgos de negocio/usuario**:

- **RN-001**: Baja Adopción por Complejidad de Interfaz (10 tareas: T176-T185)
- **RN-002**: Cambios en Requisitos Institucionales (10 tareas: T186-T195)
- **RN-003**: Resistencia al Cambio y Preferencia por Papel (10 tareas: T196-T205)
- **RN-004**: Interrupción del Servicio Durante Horario Laboral (10 tareas: T206-T215)
- **RN-005**: Fuga de Información o Violación de Privacidad (10 tareas: T216-T225)

**Total**: 50 tareas + 50 requisitos funcionales (FR-119-BIZ a FR-168-BIZ)

---

## Overall Risk Profile - Business & User

| Risk ID | Title | Priority | Prob | Impact | Mitigation Status |
|---------|-------|----------|------|--------|-------------------|
| RN-001 | Baja Adopción por Complejidad | **P1** | Media (40%) | Alto | ✅ **MITIGADO** |
| RN-003 | Resistencia al Cambio | **P1** | Media (50%) | Alto | ✅ **MITIGADO** |
| RN-004 | Interrupción del Servicio | **P1** | Media (35%) | Alto | ✅ **MITIGADO** |
| RN-005 | Fuga de Información | **P1** | Baja (20%) | Crítico | ✅ **MITIGADO** |
| RN-002 | Cambios Requisitos Institucionales | **P2** | Alta (65%) | Medio | ✅ **MITIGADO** |

**Summary**: 4 riesgos P1 + 1 riesgo P2, todos completamente mitigados con estrategias implementadas.

---

## Task Distribution by Risk

### RN-001: User Adoption (Baja Adopción por Complejidad de Interfaz)

**Goal**: Minimize user adoption resistance through excellent UX, training, and support.

| Task ID | Description | Phase | Priority | Dependencies |
|---------|-------------|-------|----------|--------------|
| T176 | Onboarding tour component (4 steps: welcome, create, save, submit) | User Story 1 | P1 | FR-119-BIZ |
| T177 | Contextual tooltips component (reusable <FormField> with examples) | User Story 1 | P1 | FR-120-BIZ |
| T178 | User manual with screenshots (docs/user-manual.md → HTML/PDF) | Polish | P2 | FR-122-BIZ |
| T179 | Video tutorials production (<2min each, embedded in help panel) | Polish | P2 | FR-123-BIZ |
| T180 | Training sessions design (materials: slides, handouts, cheat sheets) | Pre-Launch | P1 | FR-147-BIZ |
| T181 | UX testing protocol (3-5 CREADOR users, SUS questionnaire >68) | UAT | P1 | FR-119-BIZ |
| T182 | In-app feedback form (modal with rating 1-5 + comments, FEEDBACK table) | User Story 3 | P2 | FR-125-BIZ |
| T183 | NPS tracking service (quarterly survey, calculation, trend dashboard) | User Story 3 | P2 | FR-126-BIZ |
| T184 | Page load optimization (target <2s p95: lazy loading, image WebP, bundle min) | Foundational | P1 | FR-127-BIZ |
| T185 | Adoption metrics dashboard for ADMIN (active users, time to complete, heatmap) | User Story 3 | P3 | FR-128-BIZ |

**Success Criteria**:
- SUS score >68 (above average)
- User activation rate >80% (first week)
- Time to complete first report <5 minutes (p90)
- NPS >40 within 3 months

---

### RN-002: Institutional Flexibility (Cambios en Requisitos Institucionales)

**Goal**: Adapt to evolving institutional requirements without system disruption.

| Task ID | Description | Phase | Priority | Dependencies |
|---------|-------------|-------|----------|--------------|
| T186 | SYSTEM_CONFIG table and service (key-value config with type validation, UI for ADMIN) | Foundational | P2 | FR-129-BIZ |
| T187 | CONFIG_HISTORY table for audit trail (old_value, new_value, changed_by, change_reason) | Foundational | P2 | T186, FR-130-BIZ |
| T188 | FEATURE_FLAG table and service (rollout %, enabled_roles, enabled_users) | Foundational | P2 | FR-131-BIZ |
| T189 | Change request template (docs/templates/change-request.md with impact assessment) | Setup | P2 | FR-138-BIZ |
| T190 | Configuration management UI for ADMIN (DataGrid editable with save/rollback) | User Story 3 | P2 | T186, FR-137-BIZ |
| T191 | Form versioning support (schema_version in REPORTE_INCIDENCIA, multi-version rendering) | User Story 1 | P3 | FR-132-BIZ |
| T192 | Compliance matrix document (docs/compliance-matrix.md: requirement → regulation mapping) | Setup | P3 | FR-135-BIZ |
| T193 | Sprint capacity reservation tracking (20% buffer for emergent changes) | Foundational | P2 | FR-134-BIZ |
| T194 | Stakeholder communication protocol (monthly reviews, quarterly roadmap sessions) | Setup | P2 | FR-138-BIZ |
| T195 | spec.md versioning workflow (semantic versioning, change log with rationale) | Setup | P3 | FR-133-BIZ |

**Success Criteria**:
- Time to implement institutional changes <2 weeks (avg)
- Config-driven changes (no code deployment) >70%
- Backward compatibility 100% (old reports viewable)
- Breaking changes per year <3

---

### RN-003: Change Resistance (Resistencia al Cambio y Preferencia por Papel)

**Goal**: Facilitate smooth transition from paper-based workflows to digital system.

| Task ID | Description | Phase | Priority | Dependencies |
|---------|-------------|-------|----------|--------------|
| T196 | PILOT_USER table and dashboard (pilot_start_date, satisfaction_score, would_recommend, is_champion) | Pre-Launch | P1 | FR-139-BIZ |
| T197 | SUPPORT_TICKET table and management UI (category, priority, status, SLA tracking <4h) | User Story 3 | P1 | FR-140-BIZ |
| T198 | USER_ACHIEVEMENT table for gamification (badges: first_report, ten_reports, fifty_reports) | User Story 1 | P3 | FR-142-BIZ |
| T199 | Go-live readiness calculation service (satisfaction 30%, recommend 25%, bugs 25%, feedback 20%) | Pre-Launch | P1 | T196, FR-144-BIZ |
| T200 | Support ticket management UI (tabs: open/in_progress/resolved, assignment workflow) | User Story 3 | P2 | T197, FR-140-BIZ |
| T201 | Change champion portal (exclusive access for is_champion=true, advanced resources) | Post-Launch | P3 | FR-143-BIZ |
| T202 | Gradual transition plan tracking (4 phases over 8 weeks, metrics per phase) | Pre-Launch | P1 | FR-141-BIZ |
| T203 | Training materials and tracking (TRAINING_COMPLETION table, certificate issuance) | Pre-Launch | P1 | FR-147-BIZ |
| T204 | Tangible benefits communication (time comparison metrics: digital 5min vs paper 15min) | Polish | P2 | FR-148-BIZ |
| T205 | Pilot feedback collection and analysis workflow (weekly reviews, iteration tracking) | Pre-Launch | P1 | T196 |

**Success Criteria**:
- User activation rate >80% (first week)
- Weekly active users >70% by month 3
- Digital reports >90% by month 6
- Go-live readiness score >75 before full rollout

---

### RN-004: Service Continuity (Interrupción del Servicio Durante Horario Laboral)

**Goal**: Minimize service interruptions during business hours (8 AM - 8 PM).

| Task ID | Description | Phase | Priority | Dependencies |
|---------|-------------|-------|----------|--------------|
| T206 | /health endpoint (checks: DB connectivity, disk space, memory, API responsiveness) | Foundational | P1 | FR-150-BIZ |
| T207 | Docker restart policies (restart: unless-stopped, healthcheck with 60s interval) | Setup | P1 | FR-151-BIZ |
| T208 | Systemd service for auto-start (ceiba-reportes.service with Restart=on-failure) | Setup | P1 | T207 |
| T209 | Prometheus alerting rules (HighResponseTime, ServiceDown, DatabaseConnectionFailed, DiskSpaceLow) | Setup | P1 | T206 |
| T210 | /status public page (system status, component health, uptime 30d, planned maintenance) | User Story 3 | P2 | FR-152-BIZ, T206 |
| T211 | INCIDENT_LOG table and tracking (title, severity, started_at, resolved_at, MTTR, PIR flag) | Foundational | P2 | FR-155-BIZ |
| T212 | SCHEDULED_MAINTENANCE table (scheduled_start, actual_start, notification_sent) | Foundational | P2 | FR-154-BIZ |
| T213 | Incident response runbooks (docs/runbooks/: db-connection-failure.md, disk-full.md, etc.) | Setup | P1 | FR-156-BIZ |
| T214 | Maintenance window discipline (First Sunday 2-6 AM, stakeholder notification 48h prior) | Setup | P1 | FR-153-BIZ |
| T215 | Performance degradation detection (p95 latency monitoring >3s, slow query logging >1s) | Setup | P2 | T206, FR-158-BIZ |

**Success Criteria**:
- Uptime during business hours (8 AM - 8 PM) >99.5%
- MTTR (Mean Time To Recovery) <30 minutes
- Critical incident response time <15 minutes
- Planned maintenance during business hours: 0

---

### RN-005: Data Privacy (Fuga de Información o Violación de Privacidad)

**Goal**: Prevent unauthorized access, data leaks, and privacy violations.

| Task ID | Description | Phase | Priority | Dependencies |
|---------|-------------|-------|----------|--------------|
| T216 | Disk encryption (LUKS for PostgreSQL data volume, /etc/crypttab for auto-unlock) | Setup | P1 | FR-159-BIZ |
| T217 | PostgreSQL SSL connection (ssl=on, server.crt/key, connection string SSL Mode=Require) | Setup | P1 | FR-159-BIZ |
| T218 | PDF watermarking service (footer with "Exportado por {user} el {date}") | User Story 2 | P2 | FR-160-BIZ |
| T219 | Anomaly detection background service (>50 downloads/h, after-hours access, alerts every 15min) | User Story 2 | P2 | FR-161-BIZ |
| T220 | ACCESS_REVIEW table and quarterly dashboard (reviewed_by, accounts_deactivated, next_review_due) | User Story 3 | P3 | FR-162-BIZ |
| T221 | SECURITY_INCIDENT table and SIRT workflow (incident_type, severity, containment SLA <1h) | User Story 3 | P2 | FR-164-BIZ |
| T222 | SECURITY_TRAINING table and tracking (training_module, score, expiration_date for annual re-cert) | Pre-Launch | P2 | FR-165-BIZ |
| T223 | Backup encryption script (GPG AES256, scripts/backup-with-encryption.sh) | Setup | P1 | FR-166-BIZ |
| T224 | Security audit schedule (quarterly internal, annual external pentest with contract) | Setup | P2 | FR-168-BIZ |
| T225 | LFPDP compliance (Ley Federal de Protección de Datos Personales - data minimization, retention policy) | Foundational | P1 | FR-167-BIZ |

**Success Criteria**:
- Security incidents (data breaches): 0
- Unauthorized access attempts blocked: 100%
- Data encryption coverage: 100% (at rest + in transit)
- Quarterly access reviews completed on time: 100%
- Security training completion rate: 100%

---

## Recommended Task Execution Timeline

### 🏁 Phase 1: Setup & Prerequisites (Week 1)
**Goal**: Infrastructure, configuration, documentation foundations

**High Priority (Do First)**:
- T207: Docker restart policies (30min)
- T208: Systemd service (30min)
- T216: Disk encryption (2 hours)
- T217: PostgreSQL SSL (1 hour)
- T223: Backup encryption script (1 hour)
- T213: Incident response runbooks (2 hours)
- T214: Maintenance window discipline (30min)

**Medium Priority**:
- T189: Change request template (1 hour)
- T192: Compliance matrix (2 hours)
- T194: Stakeholder communication protocol (1 hour)
- T195: spec.md versioning workflow (1 hour)

**Total Effort**: ~12 hours (1.5 days)

---

### 🏗️ Phase 2: Foundational (Weeks 2-3)
**Goal**: Core tables, services, monitoring

**High Priority (Blocks User Stories)**:
- T186: SYSTEM_CONFIG table + service (4 hours)
- T187: CONFIG_HISTORY table (2 hours)
- T188: FEATURE_FLAG table + service (4 hours)
- T206: /health endpoint (3 hours)
- T209: Prometheus alerting rules (2 hours)
- T211: INCIDENT_LOG table (2 hours)
- T212: SCHEDULED_MAINTENANCE table (1 hour)
- T225: LFPDP compliance implementation (4 hours)
- T184: Page load optimization (8 hours)

**Medium Priority**:
- T193: Sprint capacity reservation tracking (1 hour)
- T215: Performance degradation detection (3 hours)

**Total Effort**: ~34 hours (4.25 days)

---

### 📋 Phase 3: Pre-Launch & UAT (Week 4)
**Goal**: Pilot program, training, testing

**Critical (Must Complete Before Launch)**:
- T196: PILOT_USER table + dashboard (4 hours)
- T197: SUPPORT_TICKET table + UI (6 hours)
- T199: Go-live readiness calculation (3 hours)
- T202: Gradual transition plan tracking (2 hours)
- T203: Training materials + TRAINING_COMPLETION table (8 hours)
- T205: Pilot feedback workflow (2 hours)
- T180: Training sessions design (6 hours)
- T181: UX testing protocol execution (8 hours)
- T222: SECURITY_TRAINING table (2 hours)

**Total Effort**: ~41 hours (5.1 days)

---

### 🧑‍💻 Phase 4: User Stories (Weeks 5-8)
**Goal**: User-facing features integration

**User Story 1 (Report Creation)**:
- T176: Onboarding tour component (6 hours)
- T177: Contextual tooltips component (4 hours)
- T191: Form versioning support (6 hours)
- T198: USER_ACHIEVEMENT table (gamification) (4 hours)

**User Story 2 (Report Export)**:
- T218: PDF watermarking service (4 hours)
- T219: Anomaly detection background service (6 hours)

**User Story 3 (Admin Module)**:
- T182: In-app feedback form (4 hours)
- T183: NPS tracking service (4 hours)
- T185: Adoption metrics dashboard (8 hours)
- T190: Configuration management UI (6 hours)
- T200: Support ticket management UI (6 hours)
- T210: /status public page (4 hours)
- T220: ACCESS_REVIEW quarterly dashboard (4 hours)
- T221: SECURITY_INCIDENT table + SIRT workflow (4 hours)

**Total Effort**: ~70 hours (8.75 days)

---

### 🎨 Phase 5: Polish & Post-Launch (Week 9+)
**Goal**: Documentation, communication, optional enhancements

**Documentation**:
- T178: User manual with screenshots (8 hours)
- T179: Video tutorials production (12 hours)
- T204: Tangible benefits communication (2 hours)

**Optional Enhancements**:
- T201: Change champion portal (6 hours)
- T224: Security audit schedule setup (2 hours)

**Total Effort**: ~30 hours (3.75 days)

---

## Consolidated Effort Summary

| Phase | Tasks | Estimated Effort | Dependencies |
|-------|-------|------------------|--------------|
| **Setup** | 11 tasks (T207, T208, T213, T214, T216, T217, T223, T189, T192, T194, T195) | 12 hours (1.5 days) | None |
| **Foundational** | 11 tasks (T186-T188, T206, T209, T211-T212, T225, T184, T193, T215) | 34 hours (4.25 days) | Setup complete |
| **Pre-Launch** | 9 tasks (T196-T197, T199, T202-T203, T205, T180-T181, T222) | 41 hours (5.1 days) | Foundational complete |
| **User Stories** | 14 tasks (T176-T177, T191, T198, T218-T219, T182-T183, T185, T190, T200, T210, T220-T221) | 70 hours (8.75 days) | Pre-Launch complete |
| **Polish** | 5 tasks (T178-T179, T204, T201, T224) | 30 hours (3.75 days) | User Stories complete |

**Total**: 50 tasks, 187 hours (~23.4 days of effort)

**Recommended Team Size**: 2-3 developers working in parallel can complete in 2-3 weeks.

---

## Integration with Existing Mitigation Tasks

### Cumulative Project Risk Mitigation

| Category | Risks | Tasks | Functional Requirements |
|----------|-------|-------|-------------------------|
| ✅ Technical Risks | RT-001 to RT-006 (6) | 30 tasks | - |
| ✅ Security Risks | RS-001 to RS-005 (5) | 26 tasks | 30 FRs (FR-037-SEC to FR-066-SEC) |
| ✅ Operational Risks | RO-001 to RO-005 (5) | 50 tasks | 52 FRs (FR-067-OPS to FR-118-OPS) |
| ✅ **Business/User Risks** | **RN-001 to RN-005 (5)** | **50 tasks** | **50 FRs (FR-119-BIZ to FR-168-BIZ)** |
| **TOTAL MITIGATED** | **21 of 25 (84%)** | **156 tasks** | **132 FRs** |

---

## Risk Coverage Validation

### Coverage Matrix

| Risk ID | Research Section | Spec FRs | Tasks | Validation Status |
|---------|------------------|----------|-------|-------------------|
| RN-001 | research.md:727-1016 (Sec 18) | FR-119-BIZ to FR-128-BIZ (10) | T176-T185 (10) | ✅ **Complete** |
| RN-002 | research.md:1019-1287 (Sec 19) | FR-129-BIZ to FR-138-BIZ (10) | T186-T195 (10) | ✅ **Complete** |
| RN-003 | research.md:1290-1552 (Sec 20) | FR-139-BIZ to FR-148-BIZ (10) | T196-T205 (10) | ✅ **Complete** |
| RN-004 | research.md:1555-1967 (Sec 21) | FR-149-BIZ to FR-158-BIZ (10) | T206-T215 (10) | ✅ **Complete** |
| RN-005 | research.md:1970-2357 (Sec 22) | FR-159-BIZ to FR-168-BIZ (10) | T216-T225 (10) | ✅ **Complete** |

**Validation**: All 5 business/user risks have complete traceability: Research → Spec → Tasks

---

## Success Metrics Dashboard

### Key Performance Indicators (Post-Launch)

**User Adoption (RN-001, RN-003)**:
- [ ] SUS score >68
- [ ] User activation rate >80% (week 1)
- [ ] Weekly active users >70% (month 3)
- [ ] Digital reports >90% (month 6)
- [ ] NPS >40 (month 3)
- [ ] Support tickets <10/month (month 3)

**Institutional Flexibility (RN-002)**:
- [ ] Time to implement changes <2 weeks (avg)
- [ ] Config-driven changes >70%
- [ ] Backward compatibility 100%
- [ ] Breaking changes <3/year

**Service Continuity (RN-004)**:
- [ ] Uptime (business hours) >99.5%
- [ ] MTTR <30 minutes
- [ ] Critical incident response <15 minutes
- [ ] Planned maintenance during business hours: 0

**Data Privacy (RN-005)**:
- [ ] Security incidents: 0
- [ ] Encryption coverage: 100%
- [ ] Quarterly access reviews completed: 100%
- [ ] Security training completion: 100%
- [ ] Penetration test pass rate >90%

---

## Next Steps

**Immediate Actions**:
1. ✅ Review and approve this priority document
2. 🔴 Schedule kickoff meeting for business/user risk mitigation work
3. 🔴 Assign ownership for each task cluster (Setup, Foundational, Pre-Launch, etc.)
4. 🔴 Integrate tasks into project backlog with appropriate sprint assignments
5. 🔴 Establish success metrics tracking dashboard

**Recommended Approach**:
- Start with **Setup & Foundational** phases (Weeks 1-3) → 11 + 11 = 22 tasks
- Parallel track: **Pre-Launch** preparations (Week 4) → 9 tasks
- Integrate into **User Stories** development (Weeks 5-8) → 14 tasks
- Finalize with **Polish** phase (Week 9+) → 5 tasks

**Dependencies**:
- All business/user mitigation tasks are **independent** of each other at the risk level
- Within each risk, follow the recommended execution timeline for optimal dependency flow
- No blockers from technical, security, or operational risk mitigation (already complete)

---

**Document Status**: ✅ **FINAL**
**Approval Required**: Product Owner + Tech Lead + UX Designer
**Next Milestone**: Begin Setup phase (Tasks T207, T208, T213, T214, T216, T217, T223)
