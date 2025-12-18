# Asignar Roles a Usuarios

Los roles determinan qué puede hacer cada usuario en el sistema. Esta guía explica los roles disponibles y cómo asignarlos.

## Roles Disponibles

### CREADOR

**Destinado a:** Oficiales de policía que crean reportes de incidencias.

| Permiso | Acceso |
|---------|--------|
| Crear reportes | Si |
| Ver reportes propios | Si |
| Editar borradores propios | Si |
| Enviar reportes | Si |
| Ver reportes de otros | No |
| Editar reportes entregados | No |
| Exportar | No |

### REVISOR

**Destinado a:** Supervisores que revisan y gestionan todos los reportes.

| Permiso | Acceso |
|---------|--------|
| Ver todos los reportes | Si |
| Editar cualquier reporte | Si |
| Exportar PDF/JSON | Si |
| Exportación masiva | Si |
| Reportes automatizados | Si |
| Gestionar usuarios | No |
| Gestionar catálogos | No |

### ADMIN

**Destinado a:** Administradores técnicos del sistema.

| Permiso | Acceso |
|---------|--------|
| Gestionar usuarios | Si |
| Asignar roles | Si |
| Gestionar catálogos | Si |
| Ver auditoría | Si |
| Configurar sistema | Si |
| Ver reportes | No |
| Exportar | No |

## Asignar Roles

### Al Crear un Usuario

1. Abre el formulario de nuevo usuario
2. En la sección de roles, marca las casillas correspondientes
3. Puedes seleccionar múltiples roles

![Asignación de roles](images/admin-usuarios-roles.png)

### A un Usuario Existente

1. Busca el usuario en la lista
2. Haz clic en **Editar**
3. Modifica los roles seleccionados
4. Guarda los cambios

## Usuarios con Múltiples Roles

Un usuario puede tener varios roles simultáneamente.

### Combinaciones Comunes

| Combinación | Caso de Uso |
|-------------|-------------|
| CREADOR + REVISOR | Oficial que también supervisa |
| REVISOR + ADMIN | Supervisor con acceso a configuración |
| CREADOR + REVISOR + ADMIN | Usuario con acceso completo |

### Ejemplo: CREADOR + REVISOR

Un usuario con ambos roles puede:
- Crear sus propios reportes
- Ver todos los reportes del sistema
- Editar cualquier reporte
- Exportar datos

El sistema muestra ambos paneles en la pantalla de inicio.

## Reglas de Asignación

### Mínimo un Rol

- Todo usuario debe tener al menos un rol asignado
- El sistema no permite guardar sin seleccionar ningún rol

### No Puedes Cambiar tus Propios Roles

- Por seguridad, un administrador no puede modificar sus propios roles
- Otro administrador debe hacer este cambio

### Mantener al Menos un Administrador

- El sistema debe tener siempre al menos un ADMIN
- No puedes quitar el rol ADMIN si es el último administrador

## Cuándo Cambiar Roles

### Agregar Rol

- Promoción del usuario
- Nuevas responsabilidades
- Acceso temporal a funciones

### Quitar Rol

- Cambio de puesto
- Reducción de responsabilidades
- Finalización de acceso temporal

## Efectos del Cambio de Rol

Los cambios de rol son **inmediatos**:

1. Si el usuario tiene sesión activa, verá los cambios al navegar
2. Los nuevos menús aparecerán automáticamente
3. Los menús removidos desaparecerán

> **Nota:** El usuario no necesita cerrar sesión para que los cambios apliquen.

## Auditoría de Cambios de Rol

Todos los cambios de roles quedan registrados:

- Usuario que realizó el cambio
- Usuario afectado
- Roles antes del cambio
- Roles después del cambio
- Fecha y hora

Puedes ver estos registros en el módulo de [Auditoría](Usuario-Admin-Auditoria).

## Mejores Prácticas

1. **Principio de mínimo privilegio**: Asigna solo los roles necesarios
2. **Revisar periódicamente**: Verifica que los roles sigan siendo apropiados
3. **Documentar**: Mantén registro de por qué se asignó cada rol
4. **Separación de funciones**: Evita dar todos los roles a un solo usuario

## Próximos Pasos

- [Gestión completa de usuarios](Usuario-Admin-Gestion-Usuarios)
- [Ver cambios en auditoría](Usuario-Admin-Auditoria)
- [Preguntas frecuentes](Usuario-Admin-FAQ)
