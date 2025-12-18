# Registro de Auditoría

El sistema Ceiba registra todas las operaciones importantes para trazabilidad y seguridad.

## ¿Qué se Audita?

El sistema registra automáticamente:

| Categoría | Operaciones Registradas |
|-----------|------------------------|
| **Autenticación** | Inicio de sesión, cierre de sesión, intentos fallidos |
| **Usuarios** | Creación, edición, suspensión, eliminación, cambios de rol |
| **Reportes** | Creación, edición, entrega, exportación |
| **Catálogos** | Creación, edición, eliminación de zonas, regiones, etc. |
| **Sugerencias** | Creación, edición, eliminación de valores |
| **Configuración** | Cambios en IA, email, sistema |

## Acceder a Auditoría

1. Desde el panel de administración, haz clic en **Auditoría**
2. O navega a `/admin/audit`

![Visor de auditoría](images/admin-auditoria-lista.png)

## Información del Registro

Cada entrada de auditoría contiene:

| Campo | Descripción |
|-------|-------------|
| **Fecha/Hora** | Cuándo ocurrió la operación (UTC) |
| **Usuario** | Quién realizó la acción |
| **Acción** | Qué tipo de operación fue |
| **Entidad** | Sobre qué elemento (reporte, usuario, etc.) |
| **ID Entidad** | Identificador del elemento afectado |
| **Detalles** | Información adicional de la operación |
| **IP** | Dirección IP desde donde se realizó |

## Filtrar Registros

Usa los filtros para encontrar registros específicos:

![Filtros de auditoría](images/admin-auditoria-filtros.png)

| Filtro | Descripción |
|--------|-------------|
| **Fecha Desde** | Inicio del rango |
| **Fecha Hasta** | Fin del rango |
| **Usuario** | Filtrar por usuario específico |
| **Acción** | Tipo de operación |
| **Entidad** | Tipo de elemento |

### Tipos de Acción

- `LOGIN` - Inicio de sesión
- `LOGOUT` - Cierre de sesión
- `CREATE` - Creación de elemento
- `UPDATE` - Modificación
- `DELETE` - Eliminación
- `EXPORT` - Exportación de datos
- `SUBMIT` - Entrega de reporte

## Ver Detalle de un Registro

Para ver información completa:

1. Haz clic en la fila del registro
2. Se abrirá el panel de detalle

![Detalle de auditoría](images/admin-auditoria-detalle.png)

### Información Detallada

El detalle puede incluir:
- Valores anteriores (antes del cambio)
- Valores nuevos (después del cambio)
- Metadata adicional de la operación

## Casos de Uso

### Investigar Acceso No Autorizado

1. Filtra por acción `LOGIN` o `LOGIN_FAILED`
2. Revisa intentos desde IPs desconocidas
3. Identifica patrones sospechosos

### Rastrear Cambios a un Reporte

1. Filtra por entidad `REPORTE`
2. Usa el ID del reporte si lo conoces
3. Revisa quién hizo cambios y cuándo

### Verificar Cambios de Usuario

1. Filtra por entidad `USUARIO`
2. Revisa creaciones, suspensiones, cambios de rol

### Auditar Exportaciones

1. Filtra por acción `EXPORT`
2. Verifica qué datos se exportaron y por quién

## Retención de Datos

> **Importante:** Los registros de auditoría se conservan **indefinidamente**. Nunca se eliminan automáticamente.

Esto cumple con:
- Requisitos legales de trazabilidad
- Políticas de seguridad institucional
- Necesidades de investigación forense

## Exportar Registros de Auditoría

Para obtener los registros en formato descargable:

1. Aplica los filtros deseados
2. Haz clic en **Exportar**
3. Selecciona el formato (CSV o JSON)
4. Descarga el archivo

## Interpretando los Registros

### Operaciones Normales

- Inicios de sesión durante horario laboral
- Creación de reportes por usuarios CREADOR
- Exportaciones por usuarios REVISOR

### Operaciones a Investigar

- Múltiples intentos fallidos de login
- Accesos fuera de horario normal
- Cambios masivos en corto tiempo
- Exportaciones inusuales

## Limitaciones

- No puedes modificar los registros de auditoría
- No puedes eliminar registros específicos
- La búsqueda de texto completo no está disponible

## Próximos Pasos

- [[Usuario-Admin-Configuracion|Configuración del sistema]]
- [[Usuario-Admin-Gestion-Usuarios|Gestionar usuarios]]
- [[Usuario-Admin-FAQ|Preguntas frecuentes]]
