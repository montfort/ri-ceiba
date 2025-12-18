# Política de Seguridad

## Versiones Soportadas

Las siguientes versiones de Ceiba reciben actualizaciones de seguridad:

| Versión | Soportada          |
| ------- | ------------------ |
| 1.0.x   | :white_check_mark: |
| < 1.0   | :x:                |

## Reportar una Vulnerabilidad

La seguridad de Ceiba es una prioridad. Si descubres una vulnerabilidad de seguridad, te pedimos que la reportes de manera responsable.

### Cómo Reportar

1. **No publiques** la vulnerabilidad en Issues públicos, foros o redes sociales.

2. **Envía un reporte privado** a través de uno de estos canales:
   - **GitHub Security Advisories**: [Reportar vulnerabilidad](https://github.com/montfort/ri-ceiba/security/advisories/new)
   - **Email**: Contacta al equipo de desarrollo directamente

3. **Incluye en tu reporte**:
   - Descripción detallada de la vulnerabilidad
   - Pasos para reproducir el problema
   - Versión afectada de Ceiba
   - Impacto potencial de la vulnerabilidad
   - Si es posible, una sugerencia de solución

### Qué Esperar

| Etapa | Tiempo Estimado |
|-------|-----------------|
| Confirmación de recepción | 48 horas |
| Evaluación inicial | 5 días hábiles |
| Actualización de estado | Cada 7 días |
| Resolución (según severidad) | 30-90 días |

### Proceso de Divulgación

1. **Recepción**: Confirmamos la recepción de tu reporte.
2. **Evaluación**: Verificamos y evaluamos la severidad de la vulnerabilidad.
3. **Desarrollo**: Trabajamos en una solución.
4. **Notificación**: Te informamos cuando el parche esté listo.
5. **Publicación**: Liberamos la actualización de seguridad.
6. **Reconocimiento**: Con tu permiso, te incluimos en los agradecimientos.

### Severidad

Clasificamos las vulnerabilidades según su impacto:

| Severidad | Descripción | Tiempo de Respuesta |
|-----------|-------------|---------------------|
| **Crítica** | Compromiso del sistema, acceso no autorizado a datos sensibles | 24-48 horas |
| **Alta** | Escalación de privilegios, bypass de autenticación | 7 días |
| **Media** | Exposición de información, XSS, CSRF | 30 días |
| **Baja** | Problemas menores de configuración | 90 días |

## Buenas Prácticas de Seguridad

### Para Administradores

- Mantén Ceiba actualizado a la última versión
- Usa contraseñas fuertes y únicas
- Configura HTTPS en producción
- Revisa los logs de auditoría regularmente
- Limita el acceso a la base de datos
- Realiza backups periódicos

### Para Desarrolladores

- Sigue los [estándares de código](https://github.com/montfort/ri-ceiba/wiki/Dev-Estandares-Codigo) del proyecto
- Nunca incluyas credenciales en el código
- Valida y sanitiza todas las entradas de usuario
- Usa consultas parametrizadas (EF Core)
- Revisa las dependencias por vulnerabilidades conocidas

## Alcance

Esta política aplica a:

- El código fuente del repositorio principal
- La aplicación web Ceiba
- Las APIs expuestas
- La documentación oficial

**No aplica a**:
- Servicios de terceros (PostgreSQL, proveedores de email, etc.)
- Infraestructura del usuario
- Configuraciones personalizadas fuera del ámbito del proyecto

## Reconocimientos

Agradecemos a quienes han reportado vulnerabilidades de manera responsable:

*Ningún reporte hasta la fecha.*

---

Gracias por ayudar a mantener Ceiba seguro.
