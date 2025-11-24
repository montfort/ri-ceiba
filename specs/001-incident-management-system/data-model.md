# Data Model: Sistema de Gestión de Reportes de Incidencias

**Branch**: `001-incident-management-system` | **Date**: 2025-11-18

## Entity Relationship Overview

```
Usuario (1) ──────< (N) ReporteIncidencia
    │                      │
    │                      ├── Zona (N:1)
    │                      ├── Sector (N:1)
    │                      └── Cuadrante (N:1)
    │
    └──────< (N) RegistroAuditoria

Zona (1) ──────< (N) Sector (1) ──────< (N) Cuadrante

CatalogoSugerencia (independent)

ReporteAutomatizado (independent)
ModeloReporte (independent)
```

## Entities

### Usuario

Represents system users with authentication and role management.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| id | UUID | PK | Unique identifier |
| nombre_usuario | VARCHAR(100) | UNIQUE, NOT NULL | Username for login |
| email | VARCHAR(255) | UNIQUE, NOT NULL | Email address |
| password_hash | VARCHAR(255) | NOT NULL | Hashed password (ASP.NET Identity) |
| activo | BOOLEAN | NOT NULL, DEFAULT true | Account active status |
| created_at | TIMESTAMPTZ | NOT NULL | Account creation timestamp |
| perfil | JSONB | NULL | Additional profile data |

**Relationships**:
- Has many `ReporteIncidencia` (creator)
- Has many `RegistroAuditoria` (actor)
- Has many `UsuarioRol` (role assignments)

**State Transitions**:
- Active (activo=true) → Suspended (activo=false)
- Suspended → Active (by ADMIN)

---

### UsuarioRol

Junction table for user-role assignments (RBAC).

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| usuario_id | UUID | FK, NOT NULL | Reference to Usuario |
| rol | VARCHAR(20) | NOT NULL | Role name: CREADOR, REVISOR, ADMIN |

**Validation Rules**:
- rol must be one of: CREADOR, REVISOR, ADMIN
- Composite PK on (usuario_id, rol)

---

### ReporteIncidencia

Core entity for incident reports (Type A).

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| id | INTEGER | PK, SERIAL | Unique identifier |
| tipo_reporte | VARCHAR(10) | NOT NULL, DEFAULT 'A' | Report type (extensible) |
| estado | SMALLINT | NOT NULL, DEFAULT 0 | 0=borrador, 1=entregado |
| usuario_id | UUID | FK, NOT NULL | Creator reference |
| created_at | TIMESTAMPTZ | NOT NULL | Report creation timestamp |
| datetime_hechos | TIMESTAMPTZ | NOT NULL | Incident date/time |
| sexo | VARCHAR(50) | NOT NULL | Gender (with suggestions) |
| edad | INTEGER | NOT NULL, CHECK(edad > 0 AND edad < 150) | Age |
| lgbtttiq_plus | BOOLEAN | DEFAULT false | LGBTTTIQ+ community |
| situacion_calle | BOOLEAN | DEFAULT false | Homeless status |
| migrante | BOOLEAN | DEFAULT false | Migrant status |
| discapacidad | BOOLEAN | DEFAULT false | Disability status |
| delito | VARCHAR(100) | NOT NULL | Crime type (with suggestions) |
| zona_id | INTEGER | FK, NOT NULL | Zone reference |
| sector_id | INTEGER | FK, NOT NULL | Sector reference |
| cuadrante_id | INTEGER | FK, NOT NULL | Quadrant reference |
| turno_ceiba | INTEGER | NOT NULL | Ceiba shift identifier |
| tipo_de_atencion | VARCHAR(100) | NOT NULL | Attention type (with suggestions) |
| tipo_de_accion | SMALLINT | NOT NULL | 1=ATOS, 2=Capacitación, 3=Prevención |
| hechos_reportados | TEXT | NOT NULL | Incident description |
| acciones_realizadas | TEXT | NOT NULL | Actions taken |
| traslados | SMALLINT | NOT NULL | 0=Sin, 1=Con, 2=No Aplica |
| observaciones | TEXT | NULL | Additional notes |
| updated_at | TIMESTAMPTZ | NULL | Last modification timestamp |
| campos_adicionales | JSONB | NULL | RT-004: Extensible fields for future report types without schema migrations |
| schema_version | VARCHAR(10) | NOT NULL, DEFAULT '1.0' | RT-004: Schema version for migration tracking |

**Validation Rules**:
- estado must be 0 or 1
- tipo_de_accion must be 1, 2, or 3
- traslados must be 0, 1, or 2
- sector must belong to selected zona
- cuadrante must belong to selected sector

**State Transitions**:
- Borrador (estado=0) → Entregado (estado=1) [irreversible by CREADOR]

**Indexes (RT-002 Mitigation)**:
- idx_reporte_usuario (usuario_id) - Creator filtering
- idx_reporte_estado (estado) - State filtering
- idx_reporte_fecha (created_at DESC) - Date ordering (most recent first)
- idx_reporte_zona (zona_id) - Zone filtering
- idx_reporte_delito (delito) - Crime type filtering
- idx_reporte_composite_search (estado, zona_id, created_at DESC) - Common filter combination
- idx_reporte_composite_revisor (created_at DESC, estado, delito) - REVISOR dashboard queries
- idx_reporte_fulltext_hechos (to_tsvector('spanish', hechos_reportados)) USING GIN - Full-text search on incident descriptions
- idx_reporte_fulltext_acciones (to_tsvector('spanish', acciones_realizadas)) USING GIN - Full-text search on actions taken

**Performance Optimizations**:
- VACUUM ANALYZE scheduled weekly to maintain index efficiency
- EXPLAIN ANALYZE required in integration tests for all search queries
- Query result limit: 500 records per page maximum (enforced at service layer)
- Search result caching: 5-minute TTL for identical filter combinations using IMemoryCache

---

### Zona

Geographic zone configuration.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| id | INTEGER | PK, SERIAL | Unique identifier |
| nombre | VARCHAR(100) | NOT NULL | Display name |
| created_at | TIMESTAMPTZ | NOT NULL | Creation timestamp |
| usuario_id | UUID | FK, NOT NULL | Creator reference |
| activo | BOOLEAN | NOT NULL, DEFAULT true | Active status |

**Relationships**:
- Has many `Sector`

---

### Sector

Geographic sector within a zone.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| id | INTEGER | PK, SERIAL | Unique identifier |
| nombre | VARCHAR(100) | NOT NULL | Display name |
| zona_id | INTEGER | FK, NOT NULL | Parent zone |
| created_at | TIMESTAMPTZ | NOT NULL | Creation timestamp |
| usuario_id | UUID | FK, NOT NULL | Creator reference |
| activo | BOOLEAN | NOT NULL, DEFAULT true | Active status |

**Relationships**:
- Belongs to `Zona`
- Has many `Cuadrante`

---

### Cuadrante

Geographic quadrant within a sector.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| id | INTEGER | PK, SERIAL | Unique identifier |
| nombre | VARCHAR(100) | NOT NULL | Display name |
| sector_id | INTEGER | FK, NOT NULL | Parent sector |
| created_at | TIMESTAMPTZ | NOT NULL | Creation timestamp |
| usuario_id | UUID | FK, NOT NULL | Creator reference |
| activo | BOOLEAN | NOT NULL, DEFAULT true | Active status |

**Relationships**:
- Belongs to `Sector`

---

### CatalogoSugerencia

Configurable suggestion lists for text fields.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| id | INTEGER | PK, SERIAL | Unique identifier |
| campo | VARCHAR(50) | NOT NULL | Target field name |
| valor | VARCHAR(200) | NOT NULL | Suggestion value |
| orden | INTEGER | NOT NULL, DEFAULT 0 | Display order |
| activo | BOOLEAN | NOT NULL, DEFAULT true | Active status |
| created_at | TIMESTAMPTZ | NOT NULL | Creation timestamp |
| usuario_id | UUID | FK, NOT NULL | Creator reference |

**Validation Rules**:
- campo must be one of: sexo, delito, tipo_de_atencion
- Unique constraint on (campo, valor)

**Indexes**:
- idx_sugerencia_campo (campo, activo)

---

### RegistroAuditoria

Audit log for all system operations.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| id | BIGINT | PK, SERIAL | Unique identifier |
| codigo | VARCHAR(50) | NOT NULL | Action code |
| id_relacionado | INTEGER | NULL | Related entity ID |
| tabla_relacionada | VARCHAR(50) | NULL | Related entity table |
| created_at | TIMESTAMPTZ | NOT NULL | Event timestamp |
| usuario_id | UUID | FK, NULL | Actor (NULL for system) |
| ip | VARCHAR(45) | NULL | Client IP address |
| detalles | JSONB | NULL | Additional event data |

**Indexes**:
- idx_auditoria_fecha (created_at)
- idx_auditoria_usuario (usuario_id)
- idx_auditoria_codigo (codigo)

**Retention**: Indefinite (no automatic deletion)

---

### ReporteAutomatizado

Generated daily summary reports.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| id | INTEGER | PK, SERIAL | Unique identifier |
| fecha_inicio | TIMESTAMPTZ | NOT NULL | Period start |
| fecha_fin | TIMESTAMPTZ | NOT NULL | Period end |
| contenido_markdown | TEXT | NOT NULL | Report in Markdown |
| contenido_word_path | VARCHAR(500) | NULL | Path to Word file |
| estadisticas | JSONB | NOT NULL | Aggregated statistics |
| enviado | BOOLEAN | NOT NULL, DEFAULT false | Email sent status |
| created_at | TIMESTAMPTZ | NOT NULL | Generation timestamp |

---

### ModeloReporte

Templates for automated report generation.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| id | INTEGER | PK, SERIAL | Unique identifier |
| nombre | VARCHAR(100) | NOT NULL | Template name |
| contenido_markdown | TEXT | NOT NULL | Template in Markdown |
| activo | BOOLEAN | NOT NULL, DEFAULT true | Active status |
| created_at | TIMESTAMPTZ | NOT NULL | Creation timestamp |
| updated_at | TIMESTAMPTZ | NULL | Last update timestamp |
| usuario_id | UUID | FK, NOT NULL | Last editor |

---

## Audit Action Codes

| Code | Description |
|------|-------------|
| AUTH_LOGIN | User login |
| AUTH_LOGOUT | User logout |
| AUTH_FAILED | Failed login attempt |
| USER_CREATE | User created |
| USER_UPDATE | User modified |
| USER_SUSPEND | User suspended |
| USER_DELETE | User deleted |
| REPORT_CREATE | Report created |
| REPORT_UPDATE | Report modified |
| REPORT_SUBMIT | Report submitted |
| REPORT_EXPORT | Report exported |
| CONFIG_ZONA | Zone configuration changed |
| CONFIG_SECTOR | Sector configuration changed |
| CONFIG_CUADRANTE | Quadrant configuration changed |
| CONFIG_SUGERENCIA | Suggestion list changed |
| AUTO_REPORT_GEN | Automated report generated |
| AUTO_REPORT_SEND | Automated report sent |
| AUTO_REPORT_FAIL | Automated report failed |

---

## Database Configuration

**PostgreSQL 18 Settings**:
- Timezone: America/Mexico_City
- Encoding: UTF8
- Locale: es_MX.UTF-8

**Connection Pooling**:
- Min pool size: 5
- Max pool size: 50
- Connection timeout: 30s

---

## Migration Strategy (RT-004 Mitigation)

**Schema Versioning**:
- All tables include `schema_version` field for tracking
- Migrations tagged with semantic version (e.g., 1.0.0, 1.1.0, 2.0.0)
- Breaking changes increment major version, additions increment minor
- Changelog maintained in `MIGRATIONS.md` at repository root

**Extensibility Design**:
- `ReporteIncidencia.campos_adicionales` (JSONB) for future report types (Tipo B, C, etc.)
- New report types add fields to JSONB without schema changes
- Shared fields remain in fixed columns for indexing/performance
- Example future Tipo B: `{"campo_especifico_B": "valor", "otro_campo": 123}`

**Migration Safety**:
- **Pre-migration backup**: Automated pg_dump before applying any migration
- **Rollback plan**: Each migration includes Down() method for EF Core rollback
- **Testing**: All migrations tested in staging environment first
- **Downtime window**: Scheduled 2:00 AM - 6:00 AM only
- **Validation**: Post-migration data integrity checks (row counts, FK constraints)

**Feature Flags**:
- New features deployed behind feature flags (appsettings.json or environment variables)
- Schema changes deployed inactive, activated after validation
- Example: `"Features": { "ReporteTipoB": false }` → enables/disables Tipo B forms

**Backward Compatibility**:
- Additive changes preferred (new nullable columns, JSONB fields)
- Deprecated fields marked but not removed for 2 major versions minimum
- API versioning (v1, v2) if breaking changes required in contracts

**Documentation**:
- All migrations documented in MIGRATIONS.md with date, author, reason
- Data model diagram updated in sync with schema changes
- Rollback procedures tested quarterly
