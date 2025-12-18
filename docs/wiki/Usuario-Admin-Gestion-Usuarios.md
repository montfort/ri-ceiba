# Gestión de Usuarios

Esta guía te enseña cómo administrar los usuarios del sistema Ceiba.

## Acceder a Gestión de Usuarios

1. Desde el panel de administración, haz clic en **Usuarios**
2. O navega a `/admin/users`

![Lista de usuarios](images/admin-usuarios-lista.png)

## Vista de Lista de Usuarios

La lista muestra:

| Columna | Descripción |
|---------|-------------|
| Email | Correo electrónico del usuario |
| Roles | Roles asignados (badges de colores) |
| Estado | Activo o Suspendido |
| Acciones | Botones de gestión |

### Códigos de Color para Roles

| Rol | Color |
|-----|-------|
| ADMIN | Rojo |
| REVISOR | Azul |
| CREADOR | Cyan |

## Filtrar Usuarios

Usa los filtros disponibles:

| Filtro | Descripción |
|--------|-------------|
| **Buscar** | Por email o nombre |
| **Rol** | Filtrar por rol específico |
| **Estado** | Activo o Suspendido |

## Crear un Nuevo Usuario

### Paso 1: Abrir el Formulario

Haz clic en el botón **Nuevo Usuario** en la parte superior derecha.

![Formulario de nuevo usuario](images/admin-usuarios-crear.png)

### Paso 2: Completar los Datos

| Campo | Descripción | Requerido |
|-------|-------------|-----------|
| Email | Correo electrónico único | Si |
| Contraseña | Mínimo 10 caracteres, con mayúscula y número | Si |
| Roles | Selecciona uno o más roles | Si |

### Paso 3: Guardar

1. Verifica los datos ingresados
2. Haz clic en **Crear Usuario**
3. El nuevo usuario aparecerá en la lista

### Requisitos de Contraseña

La contraseña debe cumplir:
- Mínimo 10 caracteres
- Al menos una letra mayúscula
- Al menos un número

## Editar un Usuario

### Paso 1: Seleccionar Usuario

En la lista de usuarios, haz clic en el botón **Editar** (ícono de lápiz).

![Formulario de edición](images/admin-usuarios-editar.png)

### Paso 2: Modificar Datos

Puedes cambiar:
- Email
- Contraseña (opcional, dejar vacío para mantener la actual)
- Roles asignados
- Estado (activo/suspendido)

### Paso 3: Guardar Cambios

Haz clic en **Guardar Cambios**.

## Cambiar Contraseña de Usuario

1. Edita el usuario
2. Ingresa la nueva contraseña en el campo correspondiente
3. Si dejas el campo vacío, la contraseña actual se mantiene
4. Guarda los cambios

> **Nota:** Informa al usuario sobre el cambio de contraseña por un canal seguro.

## Suspender un Usuario

Suspender impide que el usuario inicie sesión sin eliminar su cuenta.

1. En la lista de usuarios, busca al usuario
2. Haz clic en el botón **Suspender** (ícono de pausa)
3. El estado cambiará a "Suspendido" (badge rojo)

### Cuándo Suspender

- El usuario ya no trabaja en la organización
- Sospecha de uso indebido
- Licencia temporal
- Investigación en curso

## Activar un Usuario Suspendido

1. Busca el usuario suspendido
2. Haz clic en el botón **Activar** (ícono de play)
3. El usuario podrá iniciar sesión nuevamente

## Eliminar un Usuario

> **Advertencia:** Esta acción es irreversible. Considera suspender en lugar de eliminar.

1. Busca el usuario en la lista
2. Haz clic en el botón **Eliminar** (ícono de basura)
3. Confirma la eliminación en el diálogo

### Restricciones

- No puedes eliminarte a ti mismo
- No puedes eliminar el último administrador del sistema

## Protecciones del Sistema

### No puedes editarte a ti mismo

Por seguridad, no puedes:
- Cambiar tus propios roles
- Suspenderte
- Eliminarte

Otro administrador debe hacer estos cambios.

### Registro de Auditoría

Todas las operaciones de usuarios quedan registradas:
- Quién realizó la acción
- Qué acción se realizó
- Cuándo ocurrió
- Usuario afectado

## Próximos Pasos

- [[Usuario Admin Asignar Roles|Entender los roles]]
- [[Usuario Admin Auditoria|Ver registro de auditoría]]
- [[Usuario Admin FAQ|Preguntas frecuentes]]
