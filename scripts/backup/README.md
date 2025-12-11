# Backup Scripts - Sistema Ceiba

Este directorio contiene scripts para respaldo y restauración de la base de datos PostgreSQL del sistema Ceiba.

## Scripts Disponibles

### `backup-database.sh` / `backup-database.ps1` (T115)

Script principal de respaldo de base de datos.

**Uso (Linux/macOS):**
```bash
./backup-database.sh [output_dir]
```

**Uso (Windows PowerShell):**
```powershell
.\backup-database.ps1 [-OutputDir <path>]
```

**Características:**
- Compresión con nivel 9 (máxima)
- Formato custom de PostgreSQL (portable)
- Validación de integridad post-backup
- Generación de checksum SHA-256
- Logging detallado

**Variables de entorno:**
- `DB_HOST` - Host de PostgreSQL (default: localhost)
- `DB_PORT` - Puerto (default: 5432)
- `DB_NAME` - Nombre de la base de datos (default: ceiba)
- `DB_USER` - Usuario (default: ceiba)
- `DB_PASSWORD` - Contraseña (default: ceiba123)

### `restore-database.sh` / `restore-database.ps1` (T115b)

Script de restauración de base de datos.

**Uso (Linux/macOS):**
```bash
./restore-database.sh <backup_file> [--confirm]
```

**Uso (Windows PowerShell):**
```powershell
.\restore-database.ps1 -BackupFile <path> [-Confirm]
```

**Características:**
- Soporta formatos .sql.gz y .dump (custom)
- Verificación de checksum antes de restaurar
- Backup automático pre-restauración
- Confirmación interactiva (o flag --confirm)
- Verificación post-restauración

### `scheduled-backup.sh` (T115a)

Script para respaldos automatizados vía cron.

**Uso:**
```bash
./scheduled-backup.sh [daily|weekly|monthly]
```

**Configuración de crontab:**
```cron
# Diario a las 2 AM
0 2 * * * /opt/ceiba/scripts/backup/scheduled-backup.sh daily

# Semanal (Domingo 3 AM)
0 3 * * 0 /opt/ceiba/scripts/backup/scheduled-backup.sh weekly

# Mensual (día 1 a las 4 AM)
0 4 1 * * /opt/ceiba/scripts/backup/scheduled-backup.sh monthly
```

**Políticas de retención:**
- Diarios: 7 días
- Semanales: 28 días (4 semanas)
- Mensuales: 365 días (1 año)

**Variables adicionales:**
- `BACKUP_ROOT` - Directorio raíz de respaldos
- `NOTIFY_EMAIL` - Email para notificaciones
- `NOTIFY_ON_SUCCESS` - Notificar en éxito (default: false)
- `NOTIFY_ON_FAILURE` - Notificar en fallo (default: true)
- `USE_DOCKER` - Usar Docker para backup (default: false)
- `DOCKER_CONTAINER` - Nombre del contenedor (default: ceiba-db)

## Estructura de Respaldos

```
backups/
├── daily/
│   └── 2025/01/
│       ├── ceiba-backup-20250111-020000.dump
│       ├── ceiba-backup-20250111-020000.dump.sha256
│       └── ...
├── weekly/
│   └── ceiba-weekly-20250105-030000.dump
├── monthly/
│   └── ceiba-monthly-20250101-040000.dump
├── pre-restore/
│   └── pre-restore-20250110-153000.dump
├── migrations/
│   └── pre-migration-AddNewField-20250110-120000.sql.gz
├── logs/
│   └── scheduled-backup-202501.log
└── latest.sql.gz -> daily/2025/01/ceiba-backup-20250111-020000.dump
```

## Ejemplos de Uso

### Backup Manual

```bash
# Linux
cd /opt/ceiba
./scripts/backup/backup-database.sh

# Windows PowerShell
cd C:\ceiba
.\scripts\backup\backup-database.ps1
```

### Restaurar desde Backup

```bash
# Listar backups disponibles
./scripts/backup/restore-database.sh

# Restaurar con confirmación interactiva
./scripts/backup/restore-database.sh backups/daily/2025/01/ceiba-backup-20250111-020000.dump

# Restaurar sin confirmación (para automatización)
./scripts/backup/restore-database.sh backups/latest.sql.gz --confirm
```

### Backup con Docker

```bash
# Usando el contenedor Docker directamente
docker exec ceiba-db pg_dump -U ceiba -d ceiba --format=custom --compress=9 > backup.dump

# O usando el script con USE_DOCKER=true
USE_DOCKER=true DOCKER_CONTAINER=ceiba-db ./scripts/backup/scheduled-backup.sh
```

### Configurar Respaldos Automáticos

1. Copiar scripts a ubicación permanente:
   ```bash
   sudo cp -r scripts/backup /opt/ceiba/scripts/
   sudo chmod +x /opt/ceiba/scripts/backup/*.sh
   ```

2. Crear directorio de respaldos:
   ```bash
   sudo mkdir -p /var/backups/ceiba
   sudo chown $(whoami) /var/backups/ceiba
   ```

3. Configurar variables de entorno:
   ```bash
   cat > /opt/ceiba/.env << EOF
   DB_HOST=localhost
   DB_PORT=5432
   DB_NAME=ceiba
   DB_USER=ceiba
   DB_PASSWORD=your_secure_password
   BACKUP_ROOT=/var/backups/ceiba
   NOTIFY_EMAIL=admin@example.com
   EOF
   ```

4. Agregar al crontab:
   ```bash
   crontab -e
   # Agregar líneas:
   0 2 * * * /opt/ceiba/scripts/backup/scheduled-backup.sh daily
   0 3 * * 0 /opt/ceiba/scripts/backup/scheduled-backup.sh weekly
   0 4 1 * * /opt/ceiba/scripts/backup/scheduled-backup.sh monthly
   ```

## Recuperación de Desastres

### Escenario 1: Restaurar Backup Más Reciente

```bash
./scripts/backup/restore-database.sh backups/latest.sql.gz --confirm
```

### Escenario 2: Restaurar a Punto Específico

```bash
# Listar backups con fechas
ls -la backups/daily/2025/01/

# Restaurar backup específico
./scripts/backup/restore-database.sh backups/daily/2025/01/ceiba-backup-20250110-020000.dump --confirm
```

### Escenario 3: Migración a Nuevo Servidor

1. En servidor origen:
   ```bash
   ./scripts/backup/backup-database.sh
   scp backups/latest.sql.gz user@new-server:/tmp/
   ```

2. En servidor destino:
   ```bash
   ./scripts/backup/restore-database.sh /tmp/latest.sql.gz --confirm
   ```

## Verificación de Backups

Para verificar la integridad de un backup:

```bash
# Verificar checksum
sha256sum -c backups/daily/2025/01/ceiba-backup-20250111-020000.dump.sha256

# Listar contenido del backup
pg_restore --list backups/daily/2025/01/ceiba-backup-20250111-020000.dump

# Restaurar a base de datos temporal para verificar
createdb ceiba_verify
pg_restore -d ceiba_verify backups/latest.sql.gz
psql ceiba_verify -c "SELECT count(*) FROM \"REPORTE_INCIDENCIA\""
dropdb ceiba_verify
```

## Troubleshooting

### Error: "Cannot connect to PostgreSQL"
- Verificar que PostgreSQL está corriendo
- Verificar credenciales en variables de entorno
- Verificar conectividad de red

### Error: "Permission denied"
- Verificar permisos de ejecución: `chmod +x script.sh`
- Verificar permisos del directorio de respaldos
- Verificar usuario de PostgreSQL tiene permisos de lectura

### Error: "Checksum verification failed"
- El archivo puede estar corrupto
- Re-descargar o usar otro backup
- Verificar espacio en disco durante el backup

### Backup muy lento
- Verificar espacio en disco
- Considerar reducir nivel de compresión
- Verificar carga del servidor PostgreSQL
