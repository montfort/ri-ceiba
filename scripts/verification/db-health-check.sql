-- Database Health Check Script for Ceiba
-- Run with: PGPASSWORD=$DB_PASSWORD psql -h localhost -U ceiba -d ceiba -f scripts/verification/db-health-check.sql
-- Note: Set DB_PASSWORD environment variable before running

\echo '========================================'
\echo 'CEIBA DATABASE HEALTH CHECK'
\echo '========================================'
\echo ''

-- 1. Basic connectivity
\echo '--- 1. Database Info ---'
SELECT current_database() AS database, current_user AS user, version() AS version;

-- 2. Table counts
\echo ''
\echo '--- 2. Table Row Counts ---'
SELECT 'USUARIO' AS table_name, COUNT(*) AS row_count FROM "USUARIO"
UNION ALL
SELECT 'ROL', COUNT(*) FROM "ROL"
UNION ALL
SELECT 'USUARIO_ROL', COUNT(*) FROM "USUARIO_ROL"
UNION ALL
SELECT 'ZONA', COUNT(*) FROM "ZONA"
UNION ALL
SELECT 'SECTOR', COUNT(*) FROM "SECTOR"
UNION ALL
SELECT 'CUADRANTE', COUNT(*) FROM "CUADRANTE"
UNION ALL
SELECT 'REPORTE_INCIDENCIA', COUNT(*) FROM "REPORTE_INCIDENCIA"
UNION ALL
SELECT 'AUDITORIA', COUNT(*) FROM "AUDITORIA"
UNION ALL
SELECT 'CATALOGO_SUGERENCIA', COUNT(*) FROM "CATALOGO_SUGERENCIA"
UNION ALL
SELECT 'REPORTE_AUTOMATIZADO', COUNT(*) FROM "REPORTE_AUTOMATIZADO"
UNION ALL
SELECT 'MODELO_REPORTE', COUNT(*) FROM "MODELO_REPORTE"
UNION ALL
SELECT 'CONFIGURACION_IA', COUNT(*) FROM "CONFIGURACION_IA"
UNION ALL
SELECT 'configuracion_email', COUNT(*) FROM "configuracion_email"
UNION ALL
SELECT 'CONFIGURACION_REPORTES_AUTO', COUNT(*) FROM "CONFIGURACION_REPORTES_AUTOMATIZADOS"
ORDER BY table_name;

-- 3. Users and roles (using PascalCase column names with quotes)
\echo ''
\echo '--- 3. Users by Role ---'
SELECT
    r."Name" AS role,
    COUNT(ur."UserId") AS user_count
FROM "ROL" r
LEFT JOIN "USUARIO_ROL" ur ON r."Id" = ur."RoleId"
GROUP BY r."Name"
ORDER BY r."Name";

-- 4. User list with roles
\echo ''
\echo '--- 4. User List ---'
SELECT
    u."Email",
    u."EmailConfirmed" AS confirmed,
    CASE WHEN u."LockoutEnd" IS NOT NULL AND u."LockoutEnd" > NOW() THEN 'LOCKED' ELSE 'ACTIVE' END AS status,
    STRING_AGG(r."Name", ', ') AS roles
FROM "USUARIO" u
LEFT JOIN "USUARIO_ROL" ur ON u."Id" = ur."UserId"
LEFT JOIN "ROL" r ON ur."RoleId" = r."Id"
GROUP BY u."Id", u."Email", u."EmailConfirmed", u."LockoutEnd"
ORDER BY u."Email";

-- 5. Geographic hierarchy
\echo ''
\echo '--- 5. Geographic Hierarchy ---'
SELECT
    z.nombre AS zona,
    COUNT(DISTINCT s.id) AS sectores,
    COUNT(DISTINCT c.id) AS cuadrantes
FROM "ZONA" z
LEFT JOIN "SECTOR" s ON z.id = s.zona_id
LEFT JOIN "CUADRANTE" c ON s.id = c.sector_id
WHERE z.activo = true
GROUP BY z.id, z.nombre
ORDER BY z.nombre;

-- 6. Reports by state
\echo ''
\echo '--- 6. Reports by Estado ---'
SELECT
    CASE estado
        WHEN 0 THEN 'Borrador'
        WHEN 1 THEN 'Entregado'
        ELSE 'Otro'
    END AS estado_nombre,
    COUNT(*) AS cantidad
FROM "REPORTE_INCIDENCIA"
GROUP BY estado
ORDER BY estado;

-- 7. Reports by date (last 7 days)
\echo ''
\echo '--- 7. Reports Last 7 Days ---'
SELECT
    DATE(created_at) AS fecha,
    COUNT(*) AS reportes
FROM "REPORTE_INCIDENCIA"
WHERE created_at >= CURRENT_DATE - INTERVAL '7 days'
GROUP BY DATE(created_at)
ORDER BY fecha DESC;

-- 8. Audit log summary (last 24h)
\echo ''
\echo '--- 8. Audit Actions (Last 24h) ---'
SELECT
    "Codigo" AS action_code,
    COUNT(*) AS count
FROM "AUDITORIA"
WHERE "FechaHora" >= NOW() - INTERVAL '24 hours'
GROUP BY "Codigo"
ORDER BY count DESC
LIMIT 10;

-- 9. Suggestions by field
\echo ''
\echo '--- 9. Suggestions by Campo ---'
SELECT
    campo,
    COUNT(*) AS opciones,
    SUM(CASE WHEN activo THEN 1 ELSE 0 END) AS activas
FROM "CATALOGO_SUGERENCIA"
GROUP BY campo
ORDER BY campo;

-- 10. Automated reports
\echo ''
\echo '--- 10. Automated Reports ---'
SELECT
    COUNT(*) AS total,
    SUM(CASE WHEN enviado THEN 1 ELSE 0 END) AS enviados,
    SUM(CASE WHEN error_mensaje IS NOT NULL THEN 1 ELSE 0 END) AS con_error
FROM "REPORTE_AUTOMATIZADO";

-- 11. AI Configuration
\echo ''
\echo '--- 11. AI Configuration ---'
SELECT
    "Proveedor" AS provider,
    "Habilitado" AS enabled,
    "Modelo" AS model
FROM "CONFIGURACION_IA"
ORDER BY "CreatedAt" DESC
LIMIT 1;

-- 12. Email Configuration
\echo ''
\echo '--- 12. Email Configuration ---'
SELECT
    proveedor AS provider,
    habilitado AS enabled,
    smtp_host
FROM "configuracion_email"
ORDER BY created_at DESC
LIMIT 1;

-- 13. Foreign key integrity check
\echo ''
\echo '--- 13. FK Integrity Checks ---'
SELECT 'Orphan Sectors (no Zone)' AS check_name,
       COUNT(*) AS issues
FROM "SECTOR" s
LEFT JOIN "ZONA" z ON s.zona_id = z.id
WHERE z.id IS NULL
UNION ALL
SELECT 'Orphan Cuadrantes (no Sector)', COUNT(*)
FROM "CUADRANTE" c
LEFT JOIN "SECTOR" s ON c.sector_id = s.id
WHERE s.id IS NULL
UNION ALL
SELECT 'Reports with invalid User', COUNT(*)
FROM "REPORTE_INCIDENCIA" r
LEFT JOIN "USUARIO" u ON r.usuario_id = u."Id"
WHERE u."Id" IS NULL
UNION ALL
SELECT 'Reports with invalid Zone', COUNT(*)
FROM "REPORTE_INCIDENCIA" r
LEFT JOIN "ZONA" z ON r.zona_id = z.id
WHERE z.id IS NULL AND r.zona_id IS NOT NULL;

-- 14. Database size
\echo ''
\echo '--- 14. Database Size ---'
SELECT
    pg_size_pretty(pg_database_size(current_database())) AS database_size;

-- 15. Recent audit activity
\echo ''
\echo '--- 15. Recent Audit Activity (Last 10) ---'
SELECT
    "FechaHora" AS timestamp,
    "Codigo" AS code,
    "Entidad" AS entity,
    SUBSTRING("Detalles", 1, 50) AS details_preview
FROM "AUDITORIA"
ORDER BY "FechaHora" DESC
LIMIT 10;

\echo ''
\echo '========================================'
\echo 'HEALTH CHECK COMPLETE'
\echo '========================================'
