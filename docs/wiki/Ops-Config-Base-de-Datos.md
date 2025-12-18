# Configuración de Base de Datos

Esta guía describe cómo configurar PostgreSQL para Ceiba.

## Requisitos

- PostgreSQL 18+
- Extensiones: ninguna adicional requerida
- Espacio en disco: 500MB base + 10MB/1000 reportes

## Cadena de Conexión

### Formato Básico

```
Host=servidor;Database=ceiba;Username=usuario;Password=contraseña
```

### Formato Completo

```
Host=servidor;Port=5432;Database=ceiba;Username=usuario;Password=contraseña;SSL Mode=Prefer;Trust Server Certificate=true;Pooling=true;Minimum Pool Size=5;Maximum Pool Size=100;Connection Idle Lifetime=300
```

### Parámetros

| Parámetro | Descripción | Default |
|-----------|-------------|---------|
| Host | Servidor PostgreSQL | localhost |
| Port | Puerto | 5432 |
| Database | Nombre de BD | - |
| Username | Usuario | - |
| Password | Contraseña | - |
| SSL Mode | Modo SSL | Prefer |
| Pooling | Usar pool de conexiones | true |
| Minimum Pool Size | Conexiones mínimas | 0 |
| Maximum Pool Size | Conexiones máximas | 100 |

## Crear Base de Datos

### Con psql

```sql
-- Conectar como superusuario
sudo -u postgres psql

-- Crear usuario
CREATE USER ceiba WITH PASSWORD 'tu_password_seguro';

-- Crear base de datos
CREATE DATABASE ceiba
    OWNER ceiba
    ENCODING 'UTF8'
    LC_COLLATE 'es_ES.UTF-8'
    LC_CTYPE 'es_ES.UTF-8'
    TEMPLATE template0;

-- Otorgar permisos
GRANT ALL PRIVILEGES ON DATABASE ceiba TO ceiba;

-- Conectar a la base de datos
\c ceiba

-- Otorgar permisos en schema
GRANT ALL ON SCHEMA public TO ceiba;
```

### Con Docker

```bash
# El contenedor crea la BD automáticamente
docker compose up -d ceiba-db

# Verificar
docker compose exec ceiba-db psql -U ceiba -c "SELECT version();"
```

## Configuración de PostgreSQL

### postgresql.conf (Producción)

```ini
# Conexiones
max_connections = 200
superuser_reserved_connections = 3

# Memoria
shared_buffers = 2GB              # 25% de RAM
effective_cache_size = 6GB        # 75% de RAM
work_mem = 64MB
maintenance_work_mem = 512MB

# WAL
wal_level = replica
max_wal_size = 2GB
min_wal_size = 1GB

# Checkpoints
checkpoint_completion_target = 0.9

# Logging
log_destination = 'stderr'
logging_collector = on
log_directory = 'log'
log_filename = 'postgresql-%Y-%m-%d.log'
log_min_duration_statement = 1000  # Log queries > 1s

# Locale
lc_messages = 'es_ES.UTF-8'
lc_monetary = 'es_ES.UTF-8'
lc_numeric = 'es_ES.UTF-8'
lc_time = 'es_ES.UTF-8'
default_text_search_config = 'pg_catalog.spanish'

# Timezone
timezone = 'UTC'
```

### pg_hba.conf (Autenticación)

```
# TYPE  DATABASE    USER    ADDRESS         METHOD
local   all         postgres                peer
local   ceiba       ceiba                   scram-sha-256
host    ceiba       ceiba   127.0.0.1/32    scram-sha-256
host    ceiba       ceiba   ::1/128         scram-sha-256
host    ceiba       ceiba   10.0.0.0/8      scram-sha-256  # Red interna
```

## Migraciones

### Aplicar Migraciones

```bash
# Desde el directorio del proyecto
cd /opt/ceiba

# Aplicar todas las migraciones pendientes
dotnet ef database update \
    --project src/Ceiba.Infrastructure \
    --startup-project src/Ceiba.Web

# Verificar estado
dotnet ef migrations list \
    --project src/Ceiba.Infrastructure \
    --startup-project src/Ceiba.Web
```

### Generar Script SQL

```bash
# Generar script para todas las migraciones
dotnet ef migrations script \
    --project src/Ceiba.Infrastructure \
    --startup-project src/Ceiba.Web \
    --output migrations.sql

# Aplicar manualmente
psql -U ceiba -d ceiba -f migrations.sql
```

### Rollback

```bash
# Revertir a migración específica
dotnet ef database update NombreMigracionAnterior \
    --project src/Ceiba.Infrastructure \
    --startup-project src/Ceiba.Web
```

## Optimización

### Índices Recomendados

Los índices se crean automáticamente con las migraciones. Verificar:

```sql
-- Ver índices existentes
SELECT
    tablename,
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'public'
ORDER BY tablename, indexname;
```

### Vacuum y Analyze

```bash
# Ejecutar vacuum
docker compose exec ceiba-db vacuumdb -U ceiba -d ceiba --analyze

# Programar vacuum automático (ya habilitado por defecto)
# Verificar configuración:
docker compose exec ceiba-db psql -U ceiba -c "SHOW autovacuum;"
```

### Estadísticas

```sql
-- Ver tamaño de tablas
SELECT
    relname AS tabla,
    pg_size_pretty(pg_total_relation_size(relid)) AS tamaño_total,
    pg_size_pretty(pg_relation_size(relid)) AS tamaño_datos
FROM pg_catalog.pg_statio_user_tables
ORDER BY pg_total_relation_size(relid) DESC;

-- Ver conexiones activas
SELECT
    datname,
    usename,
    state,
    query_start,
    query
FROM pg_stat_activity
WHERE datname = 'ceiba';
```

## Alta Disponibilidad

### Replicación Streaming

En el servidor primario:

```ini
# postgresql.conf
wal_level = replica
max_wal_senders = 3
wal_keep_size = 1GB
```

```
# pg_hba.conf
host replication replicator 10.0.0.0/8 scram-sha-256
```

En el servidor réplica:

```bash
pg_basebackup -h primary -U replicator -D /var/lib/pgsql/data -Fp -Xs -P -R
```

### Failover con Patroni

Para configuración de alta disponibilidad con Patroni, consultar la documentación oficial de PostgreSQL.

## Troubleshooting

### Verificar Conexión

```bash
# Desde el servidor de aplicación
psql -h localhost -U ceiba -d ceiba -c "SELECT 1"

# Desde Docker
docker compose exec ceiba-db pg_isready -U ceiba
```

### Conexiones Máximas Alcanzadas

```sql
-- Ver conexiones actuales vs máximo
SELECT
    max_conn,
    used,
    max_conn - used AS available
FROM
    (SELECT count(*) AS used FROM pg_stat_activity) t1,
    (SELECT setting::int AS max_conn FROM pg_settings WHERE name = 'max_connections') t2;

-- Terminar conexiones inactivas
SELECT pg_terminate_backend(pid)
FROM pg_stat_activity
WHERE state = 'idle'
AND query_start < NOW() - INTERVAL '1 hour';
```

### Queries Lentas

```sql
-- Ver queries en ejecución
SELECT
    pid,
    now() - query_start AS duration,
    query,
    state
FROM pg_stat_activity
WHERE state != 'idle'
ORDER BY duration DESC;

-- Cancelar query específica
SELECT pg_cancel_backend(12345);
```

## Próximos Pasos

- [Configurar backups](Ops-Mant-Backup-Restore)
- [Monitorear base de datos](Ops-Mant-Monitoreo)
- [Variables de entorno](Ops-Config-Variables-Entorno)
