# Database Migrations Changelog

**Purpose**: Track all database schema changes for Ceiba - Sistema de Gestión de Reportes de Incidencias

**Format**: `## [Version] - YYYY-MM-DD - Author - Description`

**RT-004 Mitigation**: This changelog ensures transparency and traceability of all schema changes.

---

## [1.0.0] - 2025-11-24 - Claude - InitialCreate

**Migration**: `InitialCreate`

**Description**: Initial database schema for Phase 2 - Foundational infrastructure

**Changes**:
- Created Identity tables (USUARIO, ROL, USUARIO_ROL, etc.)
- Created geographic hierarchy: ZONA → SECTOR → CUADRANTE
- Created CATALOGO_SUGERENCIA for configurable suggestion lists
- Created REPORTE_INCIDENCIA (minimal schema, will be expanded in US1)
- Created AUDITORIA for comprehensive audit logging

**Entities**:
- `USUARIO`: ASP.NET Identity user table
- `ROL`: ASP.NET Identity role table (CREADOR, REVISOR, ADMIN)
- `USUARIO_ROL`: User-role assignments
- `ZONA`: Geographic zones (id, nombre, activo, usuario_id, created_at)
- `SECTOR`: Geographic sectors (id, nombre, zona_id, activo, usuario_id, created_at)
- `CUADRANTE`: Geographic quadrants (id, nombre, sector_id, activo, usuario_id, created_at)
- `CATALOGO_SUGERENCIA`: Suggestion catalogs (id, campo, valor, orden, activo, usuario_id, created_at)
- `REPORTE_INCIDENCIA`: Incident reports placeholder (id, tipo_reporte, estado, zona_id, sector_id, cuadrante_id, usuario_id, created_at)
- `AUDITORIA`: Audit logs (id, codigo, id_relacionado, tabla_relacionada, usuario_id, ip, detalles, created_at)

**Indexes Created**:
- `idx_zona_nombre_unique` (ZONA.nombre) - UNIQUE
- `idx_zona_activo` (ZONA.activo)
- `idx_sector_zona_nombre_unique` (SECTOR.zona_id, nombre) - UNIQUE
- `idx_sector_activo` (SECTOR.activo)
- `idx_cuadrante_sector_nombre_unique` (CUADRANTE.sector_id, nombre) - UNIQUE
- `idx_cuadrante_activo` (CUADRANTE.activo)
- `idx_sugerencia_campo_valor_unique` (CATALOGO_SUGERENCIA.campo, valor) - UNIQUE
- `idx_sugerencia_campo_activo` (CATALOGO_SUGERENCIA.campo, activo)
- `idx_auditoria_fecha` (AUDITORIA.created_at)
- `idx_auditoria_usuario` (AUDITORIA.usuario_id)
- `idx_auditoria_codigo` (AUDITORIA.codigo)
- `idx_auditoria_entidad` (AUDITORIA.tabla_relacionada, id_relacionado)

**Foreign Keys**:
- SECTOR.zona_id → ZONA.id (ON DELETE RESTRICT)
- CUADRANTE.sector_id → SECTOR.id (ON DELETE RESTRICT)
- REPORTE_INCIDENCIA.zona_id → ZONA.id (ON DELETE RESTRICT)
- REPORTE_INCIDENCIA.sector_id → SECTOR.id (ON DELETE RESTRICT)
- REPORTE_INCIDENCIA.cuadrante_id → CUADRANTE.id (ON DELETE RESTRICT)

**Rollback Plan**: `dotnet ef migrations remove` (no data loss - initial migration)

**Validation**: Row count queries for all tables, FK integrity check

**Downtime**: None (new database)

**Notes**:
- Schema version 1.0 in all base entities
- All timestamps use TIMESTAMPTZ (UTC) per project conventions
- Audit logging automatic via EF Core interceptor
- JSONB fields prepared for extensibility (RT-004)

---

## Future Migrations

**Format for new migrations**:

```markdown
## [X.Y.Z] - YYYY-MM-DD - Author - Description

**Migration**: `MigrationName`

**Description**: Brief description of changes

**Changes**:
- List of DDL operations (CREATE, ALTER, DROP)
- List of affected tables

**Impact**:
- Breaking changes (if any)
- Affected features

**Rollback Plan**: Steps to rollback if migration fails

**Validation**: Post-migration checks performed

**Downtime**: Expected downtime window (if applicable)

**Notes**: Additional context, risks, or considerations
```

---

## Migration Best Practices

1. **Pre-Migration Checklist**:
   - [ ] Backup database (`pg_dump`)
   - [ ] Test migration in staging environment
   - [ ] Review migration SQL script
   - [ ] Verify rollback plan
   - [ ] Schedule downtime window (2-6 AM)
   - [ ] Notify stakeholders 48h in advance

2. **Post-Migration Checklist**:
   - [ ] Verify row counts match pre-migration
   - [ ] Run FK integrity checks
   - [ ] Test affected features
   - [ ] Monitor application logs for errors
   - [ ] Update this MIGRATIONS.md file

3. **Extensibility Strategy** (RT-004):
   - Use JSONB fields for optional/future data
   - Add nullable columns for non-breaking changes
   - Use feature flags for new functionality
   - Maintain schema_version in entities

4. **Emergency Rollback**:
   ```bash
   # Rollback last migration
   dotnet ef database update PreviousMigrationName --startup-project src/Ceiba.Web

   # Restore from backup if needed
   pg_restore -U ceiba -d ceiba backup.sql
   ```

---

## Contact

For migration questions or issues, contact:
- **Tech Lead**: [Name]
- **DBA**: [Name]
- **Documentation**: See `docs/runbooks/rollback.md`
