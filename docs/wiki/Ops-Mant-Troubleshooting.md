# Troubleshooting

Esta guía ayuda a diagnosticar y resolver problemas comunes en Ceiba.

## Diagnóstico Rápido

### Verificación General

```bash
# Estado de servicios
docker compose ps

# Health check
curl http://localhost:5000/health

# Logs recientes
docker compose logs --tail 50 ceiba-web

# Recursos del sistema
free -h && df -h && uptime
```

## Problemas de Aplicación

### La Aplicación No Inicia

**Síntomas:** Contenedor reinicia constantemente o no arranca.

```bash
# Ver logs de inicio
docker compose logs ceiba-web | head -100

# Verificar variables de entorno
docker compose config

# Iniciar en modo interactivo para ver errores
docker compose run --rm ceiba-web
```

**Causas Comunes:**
1. Variables de entorno faltantes
2. Base de datos no disponible
3. Puerto ya en uso
4. Error en configuración

**Soluciones:**
```bash
# Verificar .env existe y tiene valores
cat .env

# Verificar BD accesible
docker compose exec ceiba-db pg_isready -U ceiba

# Verificar puerto disponible
ss -tlnp | grep 5000
```

### Error 500 - Internal Server Error

```bash
# Ver error específico en logs
docker compose logs ceiba-web | grep -A 10 "500"

# Habilitar logs detallados temporalmente
docker compose exec ceiba-web sh -c 'export Logging__LogLevel__Default=Debug && dotnet Ceiba.Web.dll'
```

**Causas Comunes:**
1. Excepción no manejada
2. Error de base de datos
3. Configuración inválida

### Página en Blanco / No Carga

**Síntomas:** La página carga pero está vacía o parcialmente.

```bash
# Verificar errores de JavaScript en navegador (F12 → Console)

# Verificar que archivos estáticos se sirven
curl -I http://localhost:5000/_framework/blazor.server.js
```

**Causas:**
1. Error de JavaScript
2. Archivos estáticos no accesibles
3. Problema de cache

**Soluciones:**
- Limpiar cache del navegador (Ctrl+Shift+Delete)
- Verificar configuración de proxy/CDN

### "Attempting to reconnect to the server"

**Síntomas:** Mensaje de reconexión de Blazor Server.

```bash
# Verificar WebSockets en proxy
docker compose logs traefik | grep -i websocket

# Verificar conexión SignalR
curl -I http://localhost:5000/_blazor
```

**Causas:**
1. Proxy no soporta WebSockets
2. Timeout muy corto
3. Problemas de red

**Soluciones:**
Ver [[Ops-Config-Reverse-Proxy|Configuración de Reverse Proxy]] para configurar WebSockets.

## Problemas de Base de Datos

### No Se Puede Conectar a PostgreSQL

```bash
# Verificar contenedor corriendo
docker compose ps ceiba-db

# Verificar que acepta conexiones
docker compose exec ceiba-db pg_isready -U ceiba

# Probar conexión manual
docker compose exec ceiba-db psql -U ceiba -d ceiba -c "SELECT 1"
```

**Causas:**
1. Contenedor no iniciado
2. Credenciales incorrectas
3. Puerto bloqueado

**Soluciones:**
```bash
# Reiniciar contenedor
docker compose restart ceiba-db

# Verificar credenciales en .env
grep POSTGRES .env
```

### Queries Lentas

```bash
# Identificar queries lentas
docker compose exec ceiba-db psql -U ceiba -c "
SELECT query, mean_time, calls
FROM pg_stat_statements
ORDER BY mean_time DESC
LIMIT 10;
"

# Verificar bloqueos
docker compose exec ceiba-db psql -U ceiba -c "
SELECT * FROM pg_stat_activity WHERE wait_event_type = 'Lock';
"
```

**Soluciones:**
1. Agregar índices faltantes
2. Optimizar queries
3. Aumentar recursos de BD

### Base de Datos Llena

```bash
# Ver tamaño de tablas
docker compose exec ceiba-db psql -U ceiba -c "
SELECT relname, pg_size_pretty(pg_total_relation_size(relid))
FROM pg_stat_user_tables
ORDER BY pg_total_relation_size(relid) DESC;
"

# Limpiar logs de auditoría antiguos (si aplica)
docker compose exec ceiba-db psql -U ceiba -c "
DELETE FROM registro_auditoria WHERE fecha < NOW() - INTERVAL '1 year';
VACUUM FULL registro_auditoria;
"
```

## Problemas de Autenticación

### No Puedo Iniciar Sesión

```bash
# Verificar usuario existe
docker compose exec ceiba-db psql -U ceiba -c "
SELECT email, activo, lockout_end
FROM usuario
WHERE email = 'usuario@ejemplo.com';
"
```

**Causas:**
1. Usuario bloqueado (lockout)
2. Contraseña incorrecta
3. Usuario inactivo

**Soluciones:**
```bash
# Desbloquear usuario
docker compose exec ceiba-db psql -U ceiba -c "
UPDATE usuario SET lockout_end = NULL, access_failed_count = 0
WHERE email = 'usuario@ejemplo.com';
"

# Activar usuario
docker compose exec ceiba-db psql -U ceiba -c "
UPDATE usuario SET activo = true WHERE email = 'usuario@ejemplo.com';
"
```

### Sesión Expira Muy Rápido

Verificar configuración de timeout:

```bash
# Ver configuración actual
grep -i session appsettings.Production.json
```

El timeout por defecto es 30 minutos de inactividad.

## Problemas de Email

### Emails No Se Envían

```bash
# Ver logs de email
docker compose logs ceiba-web | grep -i "email\|smtp\|mail"

# Probar conexión SMTP
docker compose exec ceiba-web sh -c '
echo "Test" | openssl s_client -connect smtp.gmail.com:587 -starttls smtp
'
```

**Causas:**
1. Credenciales SMTP incorrectas
2. Puerto bloqueado por firewall
3. Proveedor requiere configuración especial

**Soluciones:**
Ver [[Ops-Config-Email-SMTP|Configuración de Email]].

## Problemas de IA

### Reportes Automatizados No Se Generan

```bash
# Verificar configuración
grep AI .env

# Ver logs del servicio de IA
docker compose logs ceiba-web | grep -i "ai\|openai\|narrative"
```

**Causas:**
1. API Key inválida o expirada
2. Límite de rate alcanzado
3. Modelo no disponible

**Soluciones:**
Ver [[Ops-Config-IA|Configuración de IA]].

## Problemas de Rendimiento

### Aplicación Lenta

```bash
# Verificar uso de recursos
docker stats

# Verificar carga del servidor
top -b -n 1 | head -20

# Verificar I/O de disco
iostat -x 1 5
```

**Soluciones:**
1. Aumentar recursos (RAM, CPU)
2. Optimizar queries lentas
3. Habilitar caching
4. Escalar horizontalmente

### Memoria Alta

```bash
# Ver uso de memoria por proceso
ps aux --sort=-%mem | head -10

# Forzar garbage collection en .NET
# (reiniciar la aplicación)
docker compose restart ceiba-web
```

## Problemas de Docker

### Contenedores No Inician

```bash
# Ver estado detallado
docker compose ps -a

# Ver logs de Docker
sudo journalctl -u docker -f

# Verificar espacio en disco
df -h /var/lib/docker
```

### Sin Espacio en Disco

```bash
# Limpiar recursos no usados
docker system prune -a --volumes

# Ver uso de Docker
docker system df
```

## Comandos de Emergencia

### Reinicio Rápido

```bash
docker compose restart
```

### Reinicio Completo

```bash
docker compose down
docker compose up -d
```

### Restaurar Backup

```bash
# Detener app
docker compose stop ceiba-web

# Restaurar
docker compose exec -T ceiba-db pg_restore -U ceiba -d ceiba --clean /backup/latest.dump

# Iniciar app
docker compose start ceiba-web
```

### Acceso de Emergencia

```bash
# Crear usuario admin temporal
docker compose exec ceiba-db psql -U ceiba -c "
INSERT INTO usuario (email, nombre, activo, rol)
VALUES ('emergency@admin.com', 'Emergency Admin', true, 'ADMIN');
"
```

## Obtener Ayuda

Si el problema persiste:

1. Recopilar logs: `docker compose logs > logs.txt`
2. Capturar estado: `docker compose ps > status.txt`
3. Describir pasos para reproducir
4. Contactar soporte técnico

## Próximos Pasos

- [[Ops-Mant-Logs|Gestión de logs]]
- [[Ops-Mant-Monitoreo|Monitoreo]]
- [[Ops-Seguridad-Incidentes|Respuesta a incidentes]]
