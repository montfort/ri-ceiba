# Respuesta a Incidentes de Seguridad

Esta guía establece el procedimiento para responder a incidentes de seguridad en Ceiba.

## Clasificación de Incidentes

### Severidad

| Nivel | Descripción | Ejemplos | Tiempo de Respuesta |
|-------|-------------|----------|---------------------|
| **Crítico** | Compromiso activo del sistema | Acceso no autorizado, ransomware, fuga de datos | Inmediato (<15 min) |
| **Alto** | Intento de ataque detectado | Múltiples intentos de login, escaneo activo | < 1 hora |
| **Medio** | Vulnerabilidad identificada | Software desactualizado, misconfiguration | < 24 horas |
| **Bajo** | Evento sospechoso | Login desde ubicación inusual | < 72 horas |

## Equipo de Respuesta

| Rol | Responsabilidades |
|-----|-------------------|
| **Líder de Incidente** | Coordina respuesta, toma decisiones |
| **Analista de Seguridad** | Investiga, contiene, erradica |
| **Administrador de Sistemas** | Ejecuta acciones técnicas |
| **Comunicaciones** | Notifica stakeholders |

## Fases de Respuesta

### 1. Detección e Identificación

#### Indicadores de Compromiso (IoC)

```bash
# Verificar logins inusuales
docker compose exec ceiba-db psql -U ceiba -c "
SELECT fecha, ip_address, detalles
FROM registro_auditoria
WHERE accion = 'LOGIN_SUCCESS'
AND fecha >= CURRENT_DATE - INTERVAL '24 hours'
ORDER BY fecha DESC;
"

# Verificar intentos fallidos masivos
docker compose exec ceiba-db psql -U ceiba -c "
SELECT ip_address, COUNT(*) as intentos
FROM registro_auditoria
WHERE accion = 'LOGIN_FAILED'
AND fecha >= CURRENT_DATE - INTERVAL '1 hour'
GROUP BY ip_address
HAVING COUNT(*) > 10;
"

# Verificar acciones sospechosas
docker compose exec ceiba-db psql -U ceiba -c "
SELECT * FROM registro_auditoria
WHERE accion IN ('DELETE', 'BULK_EXPORT', 'CHANGE_ROLE')
AND fecha >= CURRENT_DATE - INTERVAL '24 hours';
"
```

#### Verificar Integridad del Sistema

```bash
# Procesos inusuales
ps aux | grep -v "^USER\|docker\|postgres\|dotnet"

# Conexiones de red sospechosas
ss -tlnp | grep -v "5000\|5432\|22\|80\|443"
netstat -an | grep ESTABLISHED

# Archivos modificados recientemente
find /opt/ceiba -type f -mtime -1 -ls

# Verificar contenedores
docker ps -a
docker images
```

### 2. Contención

#### Contención Inmediata

```bash
# Aislar el servidor (si es posible)
# Desconectar de la red externa manteniendo acceso interno

# Bloquear IP atacante
sudo firewall-cmd --permanent --add-rich-rule='rule family="ipv4" source address="1.2.3.4" reject'
sudo firewall-cmd --reload

# Suspender cuenta comprometida
docker compose exec ceiba-db psql -U ceiba -c "
UPDATE usuario SET activo = false, lockout_end = '2099-12-31'
WHERE email = 'usuario_comprometido@org.com';
"

# Forzar cierre de sesiones
docker compose restart ceiba-web
```

#### Preservar Evidencia

```bash
# Crear snapshot de logs ANTES de cualquier acción
mkdir -p /evidence/$(date +%Y%m%d_%H%M%S)
cd /evidence/$(date +%Y%m%d_%H%M%S)

# Capturar logs
docker compose logs > docker_logs.txt
cp -r /var/log/nginx/ ./nginx_logs/
journalctl -u ceiba > systemd_logs.txt

# Capturar estado de red
ss -tlnp > network_state.txt
netstat -an > netstat.txt

# Capturar procesos
ps auxf > processes.txt

# Capturar auditoría de BD
docker compose exec -T ceiba-db pg_dump -U ceiba -t registro_auditoria ceiba > audit_dump.sql

# Hash de evidencia
find . -type f -exec sha256sum {} \; > evidence_hashes.txt
```

### 3. Erradicación

#### Eliminar Acceso No Autorizado

```bash
# Cambiar todas las contraseñas de servicio
# Regenerar API keys
# Revocar tokens activos

# Rotar credenciales de BD
docker compose exec ceiba-db psql -U postgres -c "
ALTER USER ceiba WITH PASSWORD 'nueva_password_segura';
"

# Actualizar .env con nuevas credenciales
nano /opt/ceiba/.env

# Reiniciar servicios
docker compose down
docker compose up -d
```

#### Parchear Vulnerabilidades

```bash
# Actualizar sistema
sudo dnf update -y

# Reconstruir contenedores con imágenes actualizadas
docker compose build --no-cache
docker compose up -d
```

### 4. Recuperación

#### Restaurar desde Backup Limpio

```bash
# Si los datos fueron comprometidos
docker compose stop ceiba-web

# Restaurar último backup válido (antes del incidente)
docker compose exec -T ceiba-db pg_restore -U ceiba -d ceiba --clean /backups/backup_pre_incident.dump

docker compose start ceiba-web
```

#### Verificar Integridad

```bash
# Verificar funcionamiento
curl http://localhost:5000/health

# Verificar datos
docker compose exec ceiba-db psql -U ceiba -c "
SELECT COUNT(*) FROM usuario;
SELECT COUNT(*) FROM reporte_incidencia;
"

# Probar login
# Probar funcionalidades críticas
```

### 5. Lecciones Aprendidas

#### Documentar el Incidente

| Campo | Información |
|-------|-------------|
| Fecha/Hora de detección | |
| Fecha/Hora de inicio estimado | |
| Tipo de incidente | |
| Sistemas afectados | |
| Datos comprometidos | |
| Vector de ataque | |
| Acciones de contención | |
| Acciones de erradicación | |
| Tiempo de recuperación | |
| Impacto en el negocio | |

#### Mejoras a Implementar

- Nuevas alertas de detección
- Hardening adicional
- Capacitación del equipo
- Actualización de procedimientos

## Playbooks por Tipo de Incidente

### Compromiso de Cuenta

1. Suspender cuenta inmediatamente
2. Revisar auditoría de acciones de la cuenta
3. Verificar si hubo acceso a datos sensibles
4. Notificar al usuario
5. Restablecer contraseña
6. Habilitar MFA si disponible

### Ataque de Fuerza Bruta

1. Identificar IPs atacantes
2. Bloquear IPs en firewall
3. Verificar si alguna cuenta fue comprometida
4. Revisar política de contraseñas
5. Considerar rate limiting adicional

### Ransomware/Malware

1. **Desconectar de la red INMEDIATAMENTE**
2. No pagar rescate
3. Notificar a autoridades
4. Restaurar desde backup offline
5. Análisis forense antes de reconectar

### Fuga de Datos

1. Identificar alcance de la fuga
2. Documentar qué datos fueron expuestos
3. Notificar a afectados (según regulación)
4. Notificar a autoridades si aplica
5. Implementar controles adicionales

## Contactos de Emergencia

| Contacto | Teléfono | Email |
|----------|----------|-------|
| Líder de Seguridad | | |
| Administrador Senior | | |
| Proveedor de Seguridad | | |
| Contacto Legal | | |

## Notificaciones

### Plantilla de Notificación Interna

```
ALERTA DE SEGURIDAD - [SEVERIDAD]

Fecha: [FECHA]
Hora de detección: [HORA]

Resumen: [DESCRIPCIÓN BREVE]

Sistemas afectados: [LISTA]

Estado actual: [INVESTIGANDO/CONTENIDO/RESUELTO]

Acciones tomadas:
- [ACCIÓN 1]
- [ACCIÓN 2]

Próximos pasos:
- [PASO 1]
- [PASO 2]

Contacto: [NOMBRE] - [TELÉFONO]
```

### Notificación a Usuarios (si aplica)

Solo después de aprobación legal y de comunicaciones.

## Herramientas de Investigación

```bash
# Análisis de logs
grep -r "ERROR\|WARN\|attack\|malicious" /var/log/

# Verificar hashes de archivos
sha256sum /opt/ceiba/publish/*

# Timeline de eventos
docker compose exec ceiba-db psql -U ceiba -c "
SELECT fecha, accion, ip_address
FROM registro_auditoria
WHERE fecha BETWEEN '2025-01-15 00:00' AND '2025-01-15 23:59'
ORDER BY fecha;
"

# Analizar tráfico (si se capturó)
tcpdump -r captured_traffic.pcap
```

## Próximos Pasos

- [Auditoría de seguridad](Ops-Seguridad-Auditoria)
- [Backup y restauración](Ops-Mant-Backup-Restore)
- [Hardening del sistema](Ops-Seguridad-Hardening)
