# Monitoreo del Sistema

Esta guía describe cómo monitorear Ceiba en producción.

## Endpoints de Health Check

### Health Check Básico

```bash
curl http://localhost:5000/health
# Respuesta: {"status":"Healthy"}
```

### Health Check Detallado

```bash
curl http://localhost:5000/health/ready
# Incluye estado de base de datos y servicios externos
```

## Métricas del Sistema

### Recursos del Servidor

```bash
# CPU y memoria
htop

# Uso de disco
df -h

# I/O de disco
iostat -x 1

# Conexiones de red
ss -tuln | grep -E ':5000|:5432|:80|:443'
```

### Docker

```bash
# Estado de contenedores
docker compose ps

# Recursos por contenedor
docker stats

# Logs en tiempo real
docker compose logs -f

# Logs de un servicio específico
docker compose logs -f ceiba-web --tail 100
```

## Monitoreo de Base de Datos

### Conexiones Activas

```sql
SELECT
    datname as database,
    usename as user,
    state,
    count(*) as connections
FROM pg_stat_activity
WHERE datname = 'ceiba'
GROUP BY datname, usename, state;
```

### Queries Lentas

```sql
-- Habilitar logging de queries lentas
ALTER SYSTEM SET log_min_duration_statement = 1000;  -- 1 segundo
SELECT pg_reload_conf();

-- Ver queries lentas en el log
tail -f /var/lib/postgresql/data/log/postgresql-*.log | grep duration
```

### Tamaño de Tablas

```sql
SELECT
    relname as tabla,
    pg_size_pretty(pg_total_relation_size(relid)) as tamaño_total,
    n_live_tup as filas
FROM pg_stat_user_tables
ORDER BY pg_total_relation_size(relid) DESC;
```

### Bloqueos

```sql
SELECT
    blocked.pid as pid_bloqueado,
    blocked.query as query_bloqueada,
    blocking.pid as pid_bloqueante,
    blocking.query as query_bloqueante
FROM pg_stat_activity blocked
JOIN pg_stat_activity blocking ON blocking.pid = ANY(pg_blocking_pids(blocked.pid))
WHERE blocked.datname = 'ceiba';
```

## Monitoreo de Aplicación

### Logs de ASP.NET Core

```bash
# En Docker
docker compose logs ceiba-web | grep -E "ERROR|WARN|Exception"

# Con systemd
journalctl -u ceiba -p err -f

# Archivo de log
tail -f /var/log/ceiba/app.log
```

### Errores Comunes a Monitorear

| Patrón | Significado | Acción |
|--------|-------------|--------|
| `Connection refused` | BD no disponible | Verificar PostgreSQL |
| `Timeout` | Operación lenta | Revisar queries |
| `OutOfMemory` | Falta de RAM | Escalar recursos |
| `SignalR disconnected` | Conexión WebSocket caída | Verificar proxy |

## Alertas

### Script de Monitoreo Básico

```bash
#!/bin/bash
# /opt/ceiba/scripts/monitor.sh

WEBHOOK_URL="https://hooks.slack.com/services/xxx/yyy/zzz"
EMAIL="admin@org.com"

# Verificar aplicación
if ! curl -sf http://localhost:5000/health > /dev/null; then
    MSG="⚠️ Ceiba: Health check fallido"
    curl -X POST -H 'Content-type: application/json' --data "{\"text\":\"$MSG\"}" $WEBHOOK_URL
    echo "$MSG" | mail -s "Alerta Ceiba" $EMAIL
fi

# Verificar base de datos
if ! docker compose exec -T ceiba-db pg_isready -U ceiba > /dev/null 2>&1; then
    MSG="⚠️ Ceiba: Base de datos no responde"
    curl -X POST -H 'Content-type: application/json' --data "{\"text\":\"$MSG\"}" $WEBHOOK_URL
fi

# Verificar espacio en disco (alerta si < 10%)
DISK_USAGE=$(df / | tail -1 | awk '{print $5}' | sed 's/%//')
if [ $DISK_USAGE -gt 90 ]; then
    MSG="⚠️ Ceiba: Disco al $DISK_USAGE%"
    curl -X POST -H 'Content-type: application/json' --data "{\"text\":\"$MSG\"}" $WEBHOOK_URL
fi

# Verificar memoria (alerta si < 10% libre)
MEM_FREE=$(free | grep Mem | awk '{print int($7/$2 * 100)}')
if [ $MEM_FREE -lt 10 ]; then
    MSG="⚠️ Ceiba: Memoria libre al $MEM_FREE%"
    curl -X POST -H 'Content-type: application/json' --data "{\"text\":\"$MSG\"}" $WEBHOOK_URL
fi
```

### Programar Monitoreo

```bash
# Cada 5 minutos
*/5 * * * * /opt/ceiba/scripts/monitor.sh >> /var/log/ceiba-monitor.log 2>&1
```

## Prometheus + Grafana (Opcional)

### docker-compose.monitoring.yml

```yaml
services:
  prometheus:
    image: prom/prometheus
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml
      - prometheus-data:/prometheus
    ports:
      - "9090:9090"

  grafana:
    image: grafana/grafana
    volumes:
      - grafana-data:/var/lib/grafana
    ports:
      - "3000:3000"
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=admin

  node-exporter:
    image: prom/node-exporter
    ports:
      - "9100:9100"

  postgres-exporter:
    image: prometheuscommunity/postgres-exporter
    environment:
      - DATA_SOURCE_NAME=postgresql://ceiba:password@ceiba-db:5432/ceiba?sslmode=disable
    ports:
      - "9187:9187"

volumes:
  prometheus-data:
  grafana-data:
```

### prometheus.yml

```yaml
global:
  scrape_interval: 15s

scrape_configs:
  - job_name: 'node'
    static_configs:
      - targets: ['node-exporter:9100']

  - job_name: 'postgres'
    static_configs:
      - targets: ['postgres-exporter:9187']
```

## Dashboard de Estado

### Página de Estado Simple

Crear endpoint `/status` que muestre:

```json
{
  "application": {
    "status": "healthy",
    "version": "1.0.0",
    "uptime": "5d 12h 30m"
  },
  "database": {
    "status": "connected",
    "responseTime": "5ms"
  },
  "services": {
    "email": "operational",
    "ai": "operational"
  },
  "metrics": {
    "totalReports": 1234,
    "activeUsers": 45,
    "todayReports": 12
  }
}
```

## Métricas de Negocio

### Reportes por Día

```sql
SELECT
    DATE(fecha_creacion) as fecha,
    COUNT(*) as reportes
FROM reporte_incidencia
WHERE fecha_creacion >= CURRENT_DATE - INTERVAL '30 days'
GROUP BY DATE(fecha_creacion)
ORDER BY fecha;
```

### Usuarios Activos

```sql
SELECT COUNT(DISTINCT usuario_id) as usuarios_activos
FROM registro_auditoria
WHERE fecha >= CURRENT_DATE - INTERVAL '7 days';
```

### Tiempo de Respuesta de Queries

```sql
SELECT
    query,
    calls,
    mean_time::numeric(10,2) as tiempo_promedio_ms,
    max_time::numeric(10,2) as tiempo_maximo_ms
FROM pg_stat_statements
WHERE dbid = (SELECT oid FROM pg_database WHERE datname = 'ceiba')
ORDER BY mean_time DESC
LIMIT 10;
```

## Próximos Pasos

- [Gestión de logs](Ops-Mant-Logs)
- [Troubleshooting](Ops-Mant-Troubleshooting)
- [Configurar backups](Ops-Mant-Backup-Restore)
