# Auditoría de Seguridad

Esta guía describe cómo realizar auditorías de seguridad en Ceiba.

## Sistema de Auditoría de la Aplicación

### Eventos Auditados

Ceiba registra automáticamente:

| Evento | Información Capturada |
|--------|----------------------|
| Login exitoso | Usuario, IP, timestamp, User-Agent |
| Login fallido | Email intentado, IP, timestamp |
| Logout | Usuario, IP, timestamp |
| Crear reporte | Usuario, ID reporte, datos |
| Editar reporte | Usuario, ID reporte, cambios |
| Eliminar reporte | Usuario, ID reporte |
| Cambio de estado | Usuario, ID reporte, estado anterior/nuevo |
| Gestión usuarios | Admin, acción, usuario afectado |
| Cambios configuración | Admin, parámetro, valor anterior/nuevo |

### Consultar Logs de Auditoría

```sql
-- Últimas 100 acciones
SELECT
    ra.fecha,
    u.email as usuario,
    ra.accion,
    ra.entidad,
    ra.entidad_id,
    ra.ip_address,
    ra.detalles
FROM registro_auditoria ra
LEFT JOIN usuario u ON ra.usuario_id = u.id
ORDER BY ra.fecha DESC
LIMIT 100;

-- Acciones de un usuario específico
SELECT * FROM registro_auditoria
WHERE usuario_id = (SELECT id FROM usuario WHERE email = 'usuario@org.com')
ORDER BY fecha DESC;

-- Intentos de login fallidos
SELECT
    fecha,
    ip_address,
    detalles
FROM registro_auditoria
WHERE accion = 'LOGIN_FAILED'
ORDER BY fecha DESC;

-- Acciones administrativas
SELECT * FROM registro_auditoria
WHERE accion IN ('CREATE_USER', 'UPDATE_USER', 'DELETE_USER', 'CHANGE_ROLE')
ORDER BY fecha DESC;
```

### Exportar Auditoría

```sql
-- Exportar a CSV
COPY (
    SELECT
        ra.fecha,
        u.email as usuario,
        ra.accion,
        ra.entidad,
        ra.entidad_id,
        ra.ip_address
    FROM registro_auditoria ra
    LEFT JOIN usuario u ON ra.usuario_id = u.id
    WHERE ra.fecha >= '2025-01-01'
    ORDER BY ra.fecha
) TO '/tmp/auditoria_2025.csv' WITH CSV HEADER;
```

## Auditoría del Sistema Operativo

### Configurar auditd

```bash
# Instalar
sudo dnf install audit  # Fedora
sudo apt install auditd  # Ubuntu

# Habilitar
sudo systemctl enable --now auditd
```

### Reglas de Auditoría

```bash
# /etc/audit/rules.d/ceiba.rules

# Monitorear acceso a archivos de configuración
-w /opt/ceiba/.env -p wa -k ceiba_config
-w /opt/ceiba/docker-compose.yml -p wa -k ceiba_config

# Monitorear comandos Docker
-w /usr/bin/docker -p x -k docker_exec

# Monitorear cambios de usuario
-w /etc/passwd -p wa -k user_changes
-w /etc/group -p wa -k group_changes

# Monitorear sudo
-w /var/log/sudo.log -p wa -k sudo_log

# Monitorear SSH
-w /etc/ssh/sshd_config -p wa -k ssh_config
```

### Ver Logs de auditd

```bash
# Ver eventos recientes
sudo ausearch -k ceiba_config -ts recent

# Ver por usuario
sudo ausearch -ua usuario -ts today

# Generar reporte
sudo aureport --summary
sudo aureport --login
sudo aureport --failed
```

## Auditoría de PostgreSQL

### Habilitar Logging

```sql
-- postgresql.conf
ALTER SYSTEM SET log_statement = 'ddl';  -- O 'all' para todo
ALTER SYSTEM SET log_connections = on;
ALTER SYSTEM SET log_disconnections = on;
ALTER SYSTEM SET log_line_prefix = '%t [%p]: [%l-1] user=%u,db=%d,app=%a,client=%h ';

SELECT pg_reload_conf();
```

### Extensión pgAudit (Opcional)

```sql
-- Instalar extensión
CREATE EXTENSION pgaudit;

-- Configurar
ALTER SYSTEM SET pgaudit.log = 'write, ddl, role';
ALTER SYSTEM SET pgaudit.log_client = on;
```

### Ver Logs de BD

```bash
# Logs de PostgreSQL
docker compose exec ceiba-db cat /var/lib/postgresql/data/log/postgresql-*.log

# Filtrar conexiones
grep "connection" /var/lib/postgresql/data/log/postgresql-*.log
```

## Auditoría de Docker

### Logs de Contenedores

```bash
# Ver logs con timestamps
docker compose logs -t ceiba-web

# Exportar logs
docker compose logs ceiba-web > audit_logs_$(date +%Y%m%d).txt
```

### Eventos de Docker

```bash
# Ver eventos en tiempo real
docker events --filter container=ceiba-web

# Eventos desde una fecha
docker events --since "2025-01-15T00:00:00"
```

## Auditoría de Nginx

### Formato de Log Detallado

```nginx
# /etc/nginx/nginx.conf
log_format detailed '$remote_addr - $remote_user [$time_local] '
                    '"$request" $status $body_bytes_sent '
                    '"$http_referer" "$http_user_agent" '
                    '$request_time $upstream_response_time';

access_log /var/log/nginx/ceiba_access.log detailed;
```

### Análisis de Logs

```bash
# Requests por IP
awk '{print $1}' /var/log/nginx/ceiba_access.log | sort | uniq -c | sort -rn | head

# Errores 4xx/5xx
grep -E '" [45][0-9]{2} ' /var/log/nginx/ceiba_access.log

# Requests sospechosos (SQL injection, etc.)
grep -iE "(union|select|drop|insert|update|delete|script|alert)" /var/log/nginx/ceiba_access.log
```

## Checklist de Auditoría Periódica

### Diario
- [ ] Revisar intentos de login fallidos
- [ ] Verificar logs de errores
- [ ] Comprobar uso de recursos

### Semanal
- [ ] Analizar patrones de acceso inusuales
- [ ] Revisar cambios de configuración
- [ ] Verificar backups exitosos

### Mensual
- [ ] Auditoría de usuarios activos
- [ ] Revisión de permisos
- [ ] Análisis de vulnerabilidades
- [ ] Actualización de dependencias

### Trimestral
- [ ] Auditoría completa de accesos
- [ ] Test de penetración básico
- [ ] Revisión de políticas de seguridad
- [ ] Simulacro de recuperación

## Script de Reporte de Auditoría

```bash
#!/bin/bash
# /opt/ceiba/scripts/audit-report.sh

DATE=$(date +%Y%m%d)
REPORT_DIR="/opt/ceiba/audit-reports"
mkdir -p $REPORT_DIR

echo "=== Reporte de Auditoría $DATE ===" > $REPORT_DIR/report_$DATE.txt

echo -e "\n--- Intentos de Login Fallidos ---" >> $REPORT_DIR/report_$DATE.txt
docker compose exec -T ceiba-db psql -U ceiba -c "
SELECT fecha, ip_address, detalles
FROM registro_auditoria
WHERE accion = 'LOGIN_FAILED'
AND fecha >= CURRENT_DATE - INTERVAL '7 days'
ORDER BY fecha DESC;
" >> $REPORT_DIR/report_$DATE.txt

echo -e "\n--- Acciones Administrativas ---" >> $REPORT_DIR/report_$DATE.txt
docker compose exec -T ceiba-db psql -U ceiba -c "
SELECT fecha, u.email, accion, entidad, detalles
FROM registro_auditoria ra
JOIN usuario u ON ra.usuario_id = u.id
WHERE accion LIKE '%USER%' OR accion LIKE '%ROLE%'
AND fecha >= CURRENT_DATE - INTERVAL '7 days'
ORDER BY fecha DESC;
" >> $REPORT_DIR/report_$DATE.txt

echo -e "\n--- IPs con más actividad ---" >> $REPORT_DIR/report_$DATE.txt
docker compose exec -T ceiba-db psql -U ceiba -c "
SELECT ip_address, COUNT(*) as acciones
FROM registro_auditoria
WHERE fecha >= CURRENT_DATE - INTERVAL '7 days'
GROUP BY ip_address
ORDER BY acciones DESC
LIMIT 10;
" >> $REPORT_DIR/report_$DATE.txt

echo "Reporte generado: $REPORT_DIR/report_$DATE.txt"
```

## Retención de Logs

### Política de Retención

| Tipo de Log | Retención |
|-------------|-----------|
| Auditoría de aplicación | Indefinida |
| Logs de acceso | 1 año |
| Logs de errores | 90 días |
| Logs de sistema | 30 días |

### Archivado

```bash
# Archivar logs antiguos
find /var/log/ceiba -name "*.log" -mtime +30 | xargs gzip

# Mover a almacenamiento a largo plazo
aws s3 cp /var/log/ceiba/archived/ s3://bucket/logs/ --recursive
```

## Próximos Pasos

- [[Ops Seguridad Incidentes|Respuesta a incidentes]]
- [[Ops Seguridad Hardening|Hardening del sistema]]
- [[Usuario Admin Auditoria|Ver auditoría en la aplicación]]
