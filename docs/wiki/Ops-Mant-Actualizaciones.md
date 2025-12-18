# Actualizaciones del Sistema

Esta guía describe cómo actualizar Ceiba de forma segura.

## Antes de Actualizar

### Checklist Pre-Actualización

- [ ] Leer notas de la versión (CHANGELOG)
- [ ] Verificar compatibilidad de dependencias
- [ ] Realizar backup de base de datos
- [ ] Notificar a usuarios del mantenimiento
- [ ] Preparar plan de rollback

### Backup Pre-Actualización

```bash
# Backup de base de datos
docker compose exec ceiba-db pg_dump -U ceiba -Fc ceiba > backup_pre_update_$(date +%Y%m%d_%H%M%S).dump

# Backup de configuración
cp .env .env.backup
cp docker-compose.yml docker-compose.yml.backup
```

## Actualización con Docker

### Actualización Estándar

```bash
# 1. Obtener última versión
cd /opt/ceiba
git fetch origin
git checkout v1.2.0  # O la versión deseada

# 2. Detener aplicación
docker compose down

# 3. Reconstruir imagen
docker compose build --no-cache

# 4. Aplicar migraciones (si hay)
docker compose up -d ceiba-db
docker compose run --rm ceiba-web dotnet ef database update

# 5. Iniciar servicios
docker compose up -d

# 6. Verificar
docker compose ps
curl http://localhost:5000/health
```

### Actualización con Zero Downtime

```bash
# Usar rolling update si tienes múltiples réplicas
docker compose up -d --scale ceiba-web=2
sleep 30  # Esperar que arranque la nueva instancia
docker compose up -d --scale ceiba-web=1 --no-recreate
```

## Actualización Nativa (sin Docker)

### Pasos

```bash
# 1. Detener servicio
sudo systemctl stop ceiba

# 2. Backup
sudo -u ceiba pg_dump -Fc ceiba > /tmp/backup_$(date +%Y%m%d).dump

# 3. Obtener código
cd /opt/ceiba
sudo -u ceiba git fetch origin
sudo -u ceiba git checkout v1.2.0

# 4. Compilar
sudo -u ceiba dotnet publish src/Ceiba.Web -c Release -o /opt/ceiba/publish

# 5. Aplicar migraciones
sudo -u ceiba dotnet ef database update \
    --project src/Ceiba.Infrastructure \
    --startup-project src/Ceiba.Web

# 6. Iniciar servicio
sudo systemctl start ceiba

# 7. Verificar
sudo systemctl status ceiba
curl http://localhost:5000/health
```

## Migraciones de Base de Datos

### Verificar Migraciones Pendientes

```bash
# Ver estado de migraciones
dotnet ef migrations list \
    --project src/Ceiba.Infrastructure \
    --startup-project src/Ceiba.Web
```

### Aplicar Migraciones

```bash
# Aplicar todas las pendientes
dotnet ef database update \
    --project src/Ceiba.Infrastructure \
    --startup-project src/Ceiba.Web

# En Docker
docker compose run --rm ceiba-web dotnet ef database update
```

### Generar Script SQL

Para revisión manual antes de aplicar:

```bash
# Generar script de todas las migraciones
dotnet ef migrations script \
    --project src/Ceiba.Infrastructure \
    --startup-project src/Ceiba.Web \
    --output migrations.sql

# Script solo de las pendientes
dotnet ef migrations script MigracionActual MigracionNueva \
    --project src/Ceiba.Infrastructure \
    --startup-project src/Ceiba.Web \
    --output pending_migrations.sql
```

## Rollback

### Rollback de Aplicación

```bash
# Con Git
git checkout v1.1.0  # Versión anterior
docker compose build
docker compose up -d

# O restaurar backup de imagen
docker tag ceiba-web:backup ceiba-web:latest
docker compose up -d
```

### Rollback de Base de Datos

```bash
# Revertir a migración específica
dotnet ef database update NombreMigracionAnterior \
    --project src/Ceiba.Infrastructure \
    --startup-project src/Ceiba.Web

# O restaurar backup
docker compose exec -T ceiba-db pg_restore -U ceiba -d ceiba --clean backup_pre_update.dump
```

## Actualizaciones de Dependencias

### Sistema Operativo

```bash
# Fedora/RHEL
sudo dnf update -y

# Ubuntu/Debian
sudo apt update && sudo apt upgrade -y
```

### Docker

```bash
# Actualizar imágenes base
docker compose pull
docker compose up -d

# Limpiar imágenes antiguas
docker image prune -a
```

### .NET SDK/Runtime

```bash
# Verificar versión actual
dotnet --version

# Fedora - actualizar
sudo dnf update dotnet-sdk-10.0

# Ubuntu - actualizar
sudo apt update
sudo apt install dotnet-sdk-10.0
```

### PostgreSQL

Actualización de versión mayor requiere planificación:

```bash
# 1. Backup
pg_dump -Fc ceiba > backup_before_pg_upgrade.dump

# 2. Actualizar contenedor
# Modificar versión en docker-compose.yml: postgres:19

# 3. Recrear contenedor
docker compose up -d ceiba-db

# 4. Restaurar si es necesario
pg_restore -d ceiba backup_before_pg_upgrade.dump
```

## Ventana de Mantenimiento

### Notificación a Usuarios

Enviar notificación con:
- Fecha y hora del mantenimiento
- Duración estimada
- Funcionalidades afectadas
- Contacto para emergencias

### Modo Mantenimiento

```bash
# Activar página de mantenimiento en Nginx
sudo cp /etc/nginx/maintenance.html /var/www/html/
sudo ln -sf /etc/nginx/sites-available/maintenance /etc/nginx/sites-enabled/default
sudo systemctl reload nginx

# Realizar actualización...

# Desactivar modo mantenimiento
sudo rm /etc/nginx/sites-enabled/default
sudo ln -sf /etc/nginx/sites-available/ceiba /etc/nginx/sites-enabled/default
sudo systemctl reload nginx
```

## Automatización

### Script de Actualización

```bash
#!/bin/bash
# /opt/ceiba/scripts/update.sh

set -e

VERSION=$1

if [ -z "$VERSION" ]; then
    echo "Uso: $0 <version>"
    exit 1
fi

echo "=== Actualizando Ceiba a $VERSION ==="

# Backup
echo "Creando backup..."
docker compose exec -T ceiba-db pg_dump -U ceiba -Fc ceiba > /opt/ceiba/backups/pre_update_$(date +%Y%m%d_%H%M%S).dump

# Obtener código
echo "Obteniendo versión $VERSION..."
cd /opt/ceiba
git fetch origin
git checkout $VERSION

# Detener y reconstruir
echo "Reconstruyendo..."
docker compose down
docker compose build --no-cache

# Migraciones
echo "Aplicando migraciones..."
docker compose up -d ceiba-db
sleep 10
docker compose run --rm ceiba-web dotnet ef database update

# Iniciar
echo "Iniciando servicios..."
docker compose up -d

# Verificar
echo "Verificando..."
sleep 15
if curl -sf http://localhost:5000/health > /dev/null; then
    echo "✓ Actualización completada exitosamente"
else
    echo "✗ ERROR: Health check falló"
    exit 1
fi
```

## Próximos Pasos

- [Troubleshooting](Ops-Mant-Troubleshooting)
- [Backup y restauración](Ops-Mant-Backup-Restore)
- [Monitoreo post-actualización](Ops-Mant-Monitoreo)
