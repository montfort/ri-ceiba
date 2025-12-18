# Gestión de Logs

Esta guía describe cómo gestionar y analizar los logs de Ceiba.

## Ubicación de Logs

### Con Docker

| Log | Comando |
|-----|---------|
| Aplicación | `docker compose logs ceiba-web` |
| Base de datos | `docker compose logs ceiba-db` |
| Nginx/Traefik | `docker compose logs traefik` |

### Instalación Nativa

| Log | Ubicación |
|-----|-----------|
| Aplicación | `/var/log/ceiba/app.log` |
| Systemd | `journalctl -u ceiba` |
| PostgreSQL | `/var/lib/pgsql/data/log/` |
| Nginx | `/var/log/nginx/ceiba_*.log` |

## Configuración de Logging

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "Microsoft.EntityFrameworkCore": "Warning",
      "Microsoft.AspNetCore.SignalR": "Warning"
    }
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "/var/log/ceiba/app-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      }
    ]
  }
}
```

### Niveles de Log

| Nivel | Uso |
|-------|-----|
| Trace | Debugging detallado |
| Debug | Información de desarrollo |
| Information | Operaciones normales |
| Warning | Situaciones inusuales |
| Error | Errores que no detienen la app |
| Critical | Errores fatales |

## Ver Logs en Tiempo Real

### Docker

```bash
# Todos los servicios
docker compose logs -f

# Solo aplicación
docker compose logs -f ceiba-web

# Últimas 100 líneas
docker compose logs -f --tail 100 ceiba-web

# Filtrar errores
docker compose logs ceiba-web 2>&1 | grep -i error
```

### Systemd

```bash
# Logs en tiempo real
journalctl -u ceiba -f

# Solo errores
journalctl -u ceiba -p err -f

# Desde una fecha
journalctl -u ceiba --since "2025-01-15 10:00:00"

# Últimos 30 minutos
journalctl -u ceiba --since "30 min ago"
```

### Archivos

```bash
# Seguir archivo
tail -f /var/log/ceiba/app.log

# Últimas 200 líneas
tail -200 /var/log/ceiba/app.log

# Buscar errores
grep -i error /var/log/ceiba/app*.log
```

## Análisis de Logs

### Buscar Errores Específicos

```bash
# Errores de base de datos
grep -i "npgsql\|postgres\|database" /var/log/ceiba/app.log | grep -i error

# Errores de autenticación
grep -i "authentication\|login\|unauthorized" /var/log/ceiba/app.log

# Excepciones
grep -A 5 "Exception" /var/log/ceiba/app.log

# Errores en las últimas 24 horas
journalctl -u ceiba --since "24 hours ago" | grep -i error
```

### Contar Errores por Tipo

```bash
# Contar errores por hora
grep "ERROR" /var/log/ceiba/app.log | cut -d' ' -f1,2 | cut -d':' -f1,2 | uniq -c

# Top 10 errores más comunes
grep "ERROR" /var/log/ceiba/app.log | cut -d']' -f2 | sort | uniq -c | sort -rn | head -10
```

### Logs de Usuario Específico

```bash
# Buscar actividad de un usuario
grep "usuario@example.com" /var/log/ceiba/app.log

# Buscar por ID de usuario
grep "UserId=123" /var/log/ceiba/app.log
```

## Rotación de Logs

### Logrotate (Linux)

```bash
# /etc/logrotate.d/ceiba
/var/log/ceiba/*.log {
    daily
    missingok
    rotate 30
    compress
    delaycompress
    notifempty
    create 640 ceiba ceiba
    postrotate
        systemctl reload ceiba > /dev/null 2>&1 || true
    endscript
}
```

### Docker Logging Driver

```yaml
# docker-compose.yml
services:
  ceiba-web:
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "5"
```

## Logs de Base de Datos

### Habilitar Logging en PostgreSQL

```sql
-- Queries lentas (más de 1 segundo)
ALTER SYSTEM SET log_min_duration_statement = 1000;

-- Log de conexiones
ALTER SYSTEM SET log_connections = on;
ALTER SYSTEM SET log_disconnections = on;

-- Aplicar cambios
SELECT pg_reload_conf();
```

### Ver Logs de PostgreSQL

```bash
# En Docker
docker compose exec ceiba-db cat /var/lib/postgresql/data/log/postgresql-2025-01-15.log

# Queries lentas
docker compose exec ceiba-db grep "duration:" /var/lib/postgresql/data/log/*.log
```

## Logs de Auditoría

Los logs de auditoría de la aplicación se almacenan en la base de datos.

### Consultar Auditoría

```sql
-- Últimas 100 acciones
SELECT
    fecha,
    usuario_id,
    accion,
    entidad,
    entidad_id,
    ip_address
FROM registro_auditoria
ORDER BY fecha DESC
LIMIT 100;

-- Acciones de un usuario
SELECT * FROM registro_auditoria
WHERE usuario_id = 123
ORDER BY fecha DESC;

-- Acciones por tipo
SELECT accion, COUNT(*) as cantidad
FROM registro_auditoria
WHERE fecha >= CURRENT_DATE - INTERVAL '7 days'
GROUP BY accion
ORDER BY cantidad DESC;
```

## Exportar Logs

### A Archivo

```bash
# Exportar logs de Docker
docker compose logs ceiba-web > logs_export_$(date +%Y%m%d).txt

# Exportar con timestamps
docker compose logs -t ceiba-web > logs_export_$(date +%Y%m%d).txt

# Comprimir
docker compose logs ceiba-web | gzip > logs_$(date +%Y%m%d).gz
```

### Auditoría a CSV

```sql
COPY (
    SELECT
        fecha,
        u.email as usuario,
        accion,
        entidad,
        entidad_id,
        ip_address
    FROM registro_auditoria ra
    LEFT JOIN usuario u ON ra.usuario_id = u.id
    WHERE ra.fecha >= '2025-01-01'
    ORDER BY ra.fecha
) TO '/tmp/auditoria.csv' WITH CSV HEADER;
```

## Centralización de Logs (Opcional)

### Con Loki + Grafana

```yaml
# docker-compose.logging.yml
services:
  loki:
    image: grafana/loki:2.9.0
    ports:
      - "3100:3100"
    volumes:
      - loki-data:/loki

  promtail:
    image: grafana/promtail:2.9.0
    volumes:
      - /var/log:/var/log:ro
      - ./promtail-config.yml:/etc/promtail/config.yml

volumes:
  loki-data:
```

### Con ELK Stack

Para implementaciones más grandes, considerar Elasticsearch + Logstash + Kibana.

## Troubleshooting de Logs

### Logs No Aparecen

1. Verificar nivel de log configurado
2. Verificar permisos del directorio de logs
3. Verificar espacio en disco

### Logs Crecen Muy Rápido

1. Aumentar nivel mínimo de log
2. Reducir verbosidad de EF Core
3. Configurar rotación de logs
4. Revisar si hay errores repetitivos

### Logs Ilegibles

```bash
# Si están en binario (Docker json-file)
docker compose logs ceiba-web --no-color > logs.txt

# Formatear JSON
docker compose logs ceiba-web | jq '.'
```

## Próximos Pasos

- [[Ops-Mant-Monitoreo|Monitorear el sistema]]
- [[Ops-Mant-Troubleshooting|Troubleshooting]]
- [[Ops-Seguridad-Auditoria|Auditoría de seguridad]]
