# Preguntas Frecuentes - Administrador

Respuestas a las preguntas más comunes para usuarios con rol ADMIN.

## Gestión de Usuarios

### ¿Por qué no puedo eliminarme a mí mismo?

Por seguridad, el sistema impide que un administrador se elimine o suspenda a sí mismo. Esto previene:
- Bloqueo accidental del sistema
- Quedarse sin administradores
- Acciones malintencionadas

Otro administrador debe realizar estas acciones.

### ¿Puedo crear un usuario sin rol?

**No**, todo usuario debe tener al menos un rol asignado. El sistema valida esto antes de guardar.

### ¿Qué pasa con los reportes de un usuario eliminado?

Los reportes **permanecen en el sistema**. Solo se elimina la cuenta de usuario. Los reportes siguen mostrando el email original del creador.

### ¿Cómo restablezco la contraseña de un usuario?

1. Ve a Gestión de Usuarios
2. Edita el usuario
3. Ingresa una nueva contraseña
4. Guarda los cambios
5. Comunica la nueva contraseña al usuario de forma segura

### ¿Puedo asignar múltiples roles a un usuario?

**Sí**, un usuario puede tener cualquier combinación de roles. Por ejemplo, un supervisor podría tener CREADOR + REVISOR.

## Catálogos

### ¿Por qué no puedo eliminar una zona?

Las zonas solo se pueden eliminar si no tienen regiones asociadas. Debes eliminar primero:
1. Todos los cuadrantes de los sectores
2. Todos los sectores de las regiones
3. Todas las regiones de la zona
4. Finalmente la zona

### ¿Puedo renombrar una zona existente?

**Sí**, edita la zona y cambia su nombre. Los reportes existentes mostrarán el nuevo nombre.

### ¿Los cambios en catálogos afectan reportes existentes?

- **Renombrar**: Los reportes muestran el nuevo nombre
- **Eliminar**: No es posible si hay reportes asociados
- **Agregar**: Solo afecta futuros reportes

### ¿Puedo importar catálogos desde un archivo?

Actualmente no hay función de importación en la interfaz. Contacta al equipo técnico para importaciones masivas.

## Sugerencias

### ¿Las sugerencias son obligatorias para los usuarios?

**No**, las sugerencias son ayudas de autocompletado. Los usuarios pueden escribir valores personalizados que no estén en la lista.

### ¿Qué categorías de sugerencias existen?

- `sexo` - Opciones de sexo
- `delito` - Tipos de delito
- `tipo_de_atencion` - Formas de atención
- `turno_ceiba` - Turnos de trabajo
- `traslados` - Estados de traslado

### ¿Puedo crear nuevas categorías de sugerencias?

No desde la interfaz. Las categorías están predefinidas y requieren cambios de desarrollo.

## Auditoría

### ¿Puedo eliminar registros de auditoría?

**No**, los registros de auditoría son permanentes e inmutables. Esto es por diseño para garantizar trazabilidad.

### ¿Cuánto tiempo se conservan los registros?

**Indefinidamente**. Los registros nunca se eliminan automáticamente.

### ¿Puedo exportar los registros de auditoría?

**Sí**, usa los filtros para seleccionar el rango y haz clic en Exportar para descargar en CSV o JSON.

### ¿Qué información contiene cada registro?

- Fecha y hora (UTC)
- Usuario que realizó la acción
- Tipo de acción
- Entidad afectada
- Detalles del cambio
- Dirección IP

## Configuración

### ¿Dónde configuro el tiempo de sesión?

El tiempo de sesión (30 minutos por defecto) se configura a nivel de servidor mediante variables de entorno. Contacta al equipo DevOps.

### ¿Cómo pruebo la configuración de IA?

1. Ve a Config. IA
2. Configura los parámetros
3. Haz clic en "Probar Conexión"
4. Verifica la respuesta exitosa

### ¿Por qué no se envían los correos?

Verifica:
1. Configuración SMTP correcta
2. Credenciales válidas
3. Puerto no bloqueado por firewall
4. Límites del proveedor de email

### ¿Las credenciales se guardan de forma segura?

**Sí**, las API keys y contraseñas se almacenan encriptadas en la base de datos.

## Permisos

### ¿Por qué no puedo ver los reportes de incidencias?

El rol ADMIN está diseñado para administración técnica y **no tiene acceso** al módulo de reportes. Esto es una separación de funciones por seguridad.

Si necesitas ver reportes, solicita que te asignen también el rol REVISOR.

### ¿Puedo tener rol ADMIN junto con otros roles?

**Sí**, puedes tener ADMIN + CREADOR, ADMIN + REVISOR, o los tres roles simultáneamente.

### ¿Quién puede asignar el rol ADMIN?

Solo usuarios que ya tienen el rol ADMIN pueden asignar o quitar este rol a otros usuarios.

## Problemas Técnicos

### La lista de usuarios no carga

1. Actualiza la página (F5)
2. Verifica tu conexión a internet
3. Cierra sesión y vuelve a iniciar
4. Contacta al equipo técnico

### No puedo guardar cambios en catálogos

Verifica:
- Los campos obligatorios están completos
- No hay caracteres inválidos
- El elemento padre existe (para regiones, sectores, cuadrantes)

### El sistema está lento

- Puede haber alta carga de usuarios
- Verifica tu conexión a internet
- Reporta al equipo técnico si persiste

## Contacto

### ¿A quién contacto para problemas técnicos?

Contacta al equipo de desarrollo o infraestructura según la naturaleza del problema:
- **Errores del sistema**: Equipo de desarrollo
- **Problemas de servidor**: Equipo de infraestructura
- **Configuración de email/IA**: Equipo DevOps

---

¿No encontraste tu pregunta? Contacta al equipo de soporte técnico.
