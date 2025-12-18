# Configuración del Sistema

Como administrador, puedes configurar los servicios externos que utiliza el sistema Ceiba.

## Módulos de Configuración

### Configuración de IA

Configura el proveedor de inteligencia artificial para los reportes automatizados.

**Acceso:** Panel de Admin → **Config. IA** o `/admin/ai-config`

#### Proveedores Soportados

| Proveedor | Descripción |
|-----------|-------------|
| OpenAI | API de OpenAI (GPT-4, etc.) |
| Azure OpenAI | Servicio de Azure OpenAI |
| Local | LLM local (Ollama, etc.) |

#### Parámetros de Configuración

| Parámetro | Descripción |
|-----------|-------------|
| **Proveedor** | Selecciona OpenAI, Azure, o Local |
| **API Key** | Clave de acceso al servicio |
| **Modelo** | Modelo específico (ej: gpt-4) |
| **Endpoint** | URL del servicio (para Azure/Local) |
| **Temperatura** | Control de creatividad (0-1) |
| **Max Tokens** | Límite de tokens por respuesta |

#### Probar Configuración

1. Completa los parámetros
2. Haz clic en **Probar Conexión**
3. Verifica que la respuesta sea exitosa
4. Guarda la configuración

### Configuración de Email

Configura el servidor SMTP para envío de correos electrónicos.

**Acceso:** Panel de Admin → **Config. Email** o `/admin/email-config`

#### Parámetros SMTP

| Parámetro | Descripción | Ejemplo |
|-----------|-------------|---------|
| **Host** | Servidor SMTP | smtp.gmail.com |
| **Puerto** | Puerto del servidor | 587 |
| **Usuario** | Cuenta de correo | sistema@org.com |
| **Contraseña** | Contraseña o app password | ******** |
| **Usar SSL** | Conexión segura | Si |
| **Remitente** | Email que aparece como remitente | ceiba@org.com |

#### Probar Email

1. Configura los parámetros
2. Haz clic en **Enviar Correo de Prueba**
3. Ingresa un email de destino
4. Verifica la recepción
5. Guarda la configuración

## Seguridad de Credenciales

> **Importante:** Las credenciales sensibles (API Keys, contraseñas) se almacenan encriptadas en la base de datos.

### Recomendaciones

1. **API Keys**: Usa keys con permisos mínimos necesarios
2. **SMTP**: Usa contraseñas de aplicación, no la principal
3. **Rotación**: Cambia credenciales periódicamente
4. **Monitoreo**: Revisa uso de APIs regularmente

## Configuración de Reportes Automatizados

Los reportes automatizados se configuran desde el rol REVISOR:

1. Plantillas de generación
2. Horarios de ejecución
3. Destinatarios de email

Ver [[Usuario-Revisor-Reportes-Automatizados|Reportes Automatizados]].

## Variables de Entorno

Algunas configuraciones se manejan a nivel de servidor mediante variables de entorno:

| Variable | Descripción |
|----------|-------------|
| `ASPNETCORE_ENVIRONMENT` | Entorno (Development/Production) |
| `ConnectionStrings__DefaultConnection` | Cadena de conexión a BD |
| `SessionTimeout` | Tiempo de sesión en minutos |

Consulta [[Ops-Config-Variables-Entorno|Variables de Entorno]] para más detalles.

## Respaldo de Configuración

Antes de hacer cambios importantes:

1. Documenta la configuración actual
2. Haz una captura de pantalla
3. Prueba en un entorno de desarrollo primero

## Problemas Comunes

### "Error de conexión con IA"

- Verifica la API Key
- Confirma el endpoint correcto
- Revisa cuotas del servicio

### "No se pueden enviar correos"

- Verifica credenciales SMTP
- Confirma que el puerto no esté bloqueado
- Revisa políticas de spam del proveedor

### "Configuración no guardada"

- Verifica que tienes rol ADMIN
- Revisa errores de validación
- Intenta refrescar la página

## Auditoría de Configuración

Todos los cambios de configuración quedan registrados en auditoría:

- Quién cambió la configuración
- Qué parámetro se modificó
- Cuándo ocurrió el cambio

## Próximos Pasos

- [[Usuario-Admin-Auditoria|Ver registro de auditoría]]
- [[Usuario-Admin-FAQ|Preguntas frecuentes]]
- [[Ops-Config-Variables-Entorno|Variables de entorno (DevOps)]]
