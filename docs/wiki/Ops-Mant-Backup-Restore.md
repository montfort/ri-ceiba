# Backup y Restauración

Esta guía describe cómo realizar copias de seguridad y restauración de Ceiba.

## Componentes a Respaldar

| Componente | Prioridad | Frecuencia |
|------------|-----------|------------|
| Base de datos PostgreSQL | Crítica | Diario |
| Archivos de configuración | Alta | Semanal |
| Logs de auditoría | Media | Mensual |
| Certificados SSL | Alta | Antes de expirar |

## Backup de Base de Datos

### Backup Manual

```bash
# Con Docker
docker compose exec ceiba-db pg_dump -U ceiba ceiba > backup_$(date +%Y%m%d_%H%M%S).sql

# Con compresión
docker compose exec ceiba-db pg_dump -U ceiba ceiba | gzip > backup_$(date +%Y%m%d).sql.gz

# Sin Docker
pg_dump -h localhost -U ceiba ceiba > backup_$(date +%Y%m%d).sql
```

### Backup con Formato Custom (más eficiente)

```bash
# Crear backup comprimido con formato custom
docker compose exec ceiba-db pg_dump -U ceiba -Fc ceiba > backup_$(date +%Y%m%d).dump

# Este formato permite restauración selectiva de tablas
```

### Script de Backup Automatizado

```bash
#!/bin/bash
# /opt/ceiba/scripts/backup.sh

set -e

BACKUP_DIR="/opt/ceiba/backups"
DATE=$(date +%Y%m%d_%H%M%S)
RETENTION_DAYS=30

# Crear directorio si no existe
mkdir -p "$BACKUP_DIR"

# Backup de base de datos
echo "Iniciando backup de base de datos..."
docker compose exec -T ceiba-db pg_dump -U ceiba -Fc ceiba > "$BACKUP_DIR/db_$DATE.dump"

# Backup de configuración
echo "Respaldando configuración..."
tar -czf "$BACKUP_DIR/config_$DATE.tar.gz" \
    /opt/ceiba/.env \
    /opt/ceiba/docker-compose.yml \
    /opt/ceiba/publish/appsettings.Production.json 2>/dev/null || true

# Eliminar backups antiguos
echo "Limpiando backups antiguos..."
find "$BACKUP_DIR" -name "*.dump" -mtime +$RETENTION_DAYS -delete
find "$BACKUP_DIR" -name "*.tar.gz" -mtime +$RETENTION_DAYS -delete

# Verificar integridad
echo "Verificando backup..."
pg_restore -l "$BACKUP_DIR/db_$DATE.dump" > /dev/null

echo "Backup completado: $BACKUP_DIR/db_$DATE.dump"

# Opcional: subir a almacenamiento remoto
# aws s3 cp "$BACKUP_DIR/db_$DATE.dump" s3://mi-bucket/backups/
# rclone copy "$BACKUP_DIR/db_$DATE.dump" remote:backups/
```

### Programar con Cron

```bash
# Editar crontab
sudo crontab -e

# Backup diario a las 2:00 AM
0 2 * * * /opt/ceiba/scripts/backup.sh >> /var/log/ceiba-backup.log 2>&1

# Backup semanal completo los domingos a las 3:00 AM
0 3 * * 0 /opt/ceiba/scripts/backup-full.sh >> /var/log/ceiba-backup.log 2>&1
```

### Programar con Systemd Timer

```ini
# /etc/systemd/system/ceiba-backup.service
[Unit]
Description=Ceiba Database Backup
After=docker.service

[Service]
Type=oneshot
ExecStart=/opt/ceiba/scripts/backup.sh
User=root
```

```ini
# /etc/systemd/system/ceiba-backup.timer
[Unit]
Description=Run Ceiba backup daily

[Timer]
OnCalendar=*-*-* 02:00:00
Persistent=true

[Install]
WantedBy=timers.target
```

```bash
sudo systemctl enable --now ceiba-backup.timer
```

## Restauración

### Restaurar Base de Datos Completa

```bash
# Detener la aplicación
docker compose stop ceiba-web

# Restaurar desde archivo SQL
docker compose exec -T ceiba-db psql -U ceiba -d ceiba < backup_20250115.sql

# Restaurar desde formato custom
docker compose exec -T ceiba-db pg_restore -U ceiba -d ceiba --clean backup_20250115.dump

# Reiniciar aplicación
docker compose start ceiba-web
```

### Restaurar en Base de Datos Nueva

```bash
# Crear nueva base de datos
docker compose exec ceiba-db psql -U postgres -c "CREATE DATABASE ceiba_restore OWNER ceiba;"

# Restaurar
docker compose exec -T ceiba-db pg_restore -U ceiba -d ceiba_restore backup_20250115.dump

# Verificar
docker compose exec ceiba-db psql -U ceiba -d ceiba_restore -c "SELECT COUNT(*) FROM reporte_incidencia;"
```

### Restaurar Tabla Específica

```bash
# Listar contenido del backup
pg_restore -l backup_20250115.dump

# Restaurar solo una tabla
pg_restore -U ceiba -d ceiba --table=reporte_incidencia backup_20250115.dump
```

## Backup Remoto

### Hacia S3 (AWS)

```bash
#!/bin/bash
# Configurar AWS CLI primero: aws configure

BUCKET="mi-bucket-backups"
DATE=$(date +%Y%m%d)

# Crear backup
docker compose exec -T ceiba-db pg_dump -U ceiba -Fc ceiba > /tmp/db_$DATE.dump

# Subir a S3
aws s3 cp /tmp/db_$DATE.dump s3://$BUCKET/ceiba/db_$DATE.dump

# Limpiar
rm /tmp/db_$DATE.dump

# Eliminar backups antiguos en S3 (más de 30 días)
aws s3 ls s3://$BUCKET/ceiba/ | while read -r line; do
    file_date=$(echo $line | awk '{print $1}')
    if [[ $(date -d "$file_date" +%s) -lt $(date -d "30 days ago" +%s) ]]; then
        file_name=$(echo $line | awk '{print $4}')
        aws s3 rm s3://$BUCKET/ceiba/$file_name
    fi
done
```

### Hacia Servidor Remoto (rsync)

```bash
#!/bin/bash
REMOTE_HOST="backup-server.tudominio.com"
REMOTE_DIR="/backups/ceiba"
LOCAL_BACKUP="/opt/ceiba/backups"

# Sincronizar backups
rsync -avz --delete $LOCAL_BACKUP/ $REMOTE_HOST:$REMOTE_DIR/
```

## Verificación de Backups

### Script de Verificación

```bash
#!/bin/bash
# /opt/ceiba/scripts/verify-backup.sh

BACKUP_FILE=$1

if [ -z "$BACKUP_FILE" ]; then
    echo "Uso: $0 <archivo_backup>"
    exit 1
fi

# Verificar que el archivo existe
if [ ! -f "$BACKUP_FILE" ]; then
    echo "ERROR: Archivo no encontrado: $BACKUP_FILE"
    exit 1
fi

# Verificar integridad
echo "Verificando integridad del backup..."
if pg_restore -l "$BACKUP_FILE" > /dev/null 2>&1; then
    echo "✓ Backup íntegro"
else
    echo "✗ ERROR: Backup corrupto"
    exit 1
fi

# Verificar contenido
echo "Contenido del backup:"
pg_restore -l "$BACKUP_FILE" | head -20

# Contar tablas
TABLE_COUNT=$(pg_restore -l "$BACKUP_FILE" | grep "TABLE " | wc -l)
echo "Tablas en el backup: $TABLE_COUNT"

echo "Verificación completada"
```

### Test de Restauración

```bash
# Crear base de datos de prueba
docker compose exec ceiba-db psql -U postgres -c "CREATE DATABASE ceiba_test OWNER ceiba;"

# Restaurar
docker compose exec -T ceiba-db pg_restore -U ceiba -d ceiba_test backup.dump

# Verificar datos
docker compose exec ceiba-db psql -U ceiba -d ceiba_test -c "
SELECT 'usuarios' as tabla, COUNT(*) as registros FROM usuario
UNION ALL
SELECT 'reportes', COUNT(*) FROM reporte_incidencia
UNION ALL
SELECT 'auditoria', COUNT(*) FROM registro_auditoria;
"

# Eliminar base de prueba
docker compose exec ceiba-db psql -U postgres -c "DROP DATABASE ceiba_test;"
```

## Plan de Recuperación ante Desastres

### RTO y RPO

| Métrica | Objetivo |
|---------|----------|
| RPO (Recovery Point Objective) | Máximo 24 horas de pérdida de datos |
| RTO (Recovery Time Objective) | Máximo 4 horas para restaurar servicio |

### Procedimiento de Recuperación

1. **Evaluar el daño**
   - Identificar qué falló (servidor, BD, aplicación)
   - Determinar el último backup válido

2. **Provisionar infraestructura**
   - Servidor nuevo o reparado
   - Instalar Docker y dependencias

3. **Restaurar datos**
   ```bash
   # Obtener último backup
   aws s3 cp s3://bucket/backups/latest.dump /tmp/

   # Restaurar
   docker compose up -d ceiba-db
   docker compose exec -T ceiba-db pg_restore -U ceiba -d ceiba /tmp/latest.dump
   ```

4. **Restaurar aplicación**
   ```bash
   git clone https://github.com/org/ceiba.git /opt/ceiba
   cp /backup/config/.env /opt/ceiba/
   docker compose up -d
   ```

5. **Verificar**
   - Probar login
   - Verificar reportes existentes
   - Confirmar integridad de datos

6. **Notificar**
   - Informar a usuarios del incidente
   - Documentar lecciones aprendidas

## Próximos Pasos

- [[Ops-Mant-Monitoreo|Monitorear backups]]
- [[Ops-Seguridad-Incidentes|Plan de respuesta a incidentes]]
- [[Ops-Config-Base-de-Datos|Configuración de base de datos]]
