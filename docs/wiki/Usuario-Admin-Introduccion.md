# Guía del Administrador - Introducción

Como **Administrador**, tu función principal es gestionar la configuración del sistema, usuarios, catálogos y monitorear las operaciones.

## Tus Permisos

| Acción | Permitido |
|--------|-----------|
| Gestionar usuarios | Si |
| Asignar roles | Si |
| Configurar catálogos geográficos | Si |
| Configurar sugerencias | Si |
| Ver auditoría | Si |
| Configurar IA | Si |
| Configurar email | Si |
| Ver reportes de incidencias | No |
| Exportar reportes | No |

> **Importante:** El rol ADMIN está diseñado para administración técnica y no tiene acceso a los reportes de incidencias. Esto es una separación de responsabilidades por seguridad.

## Tu Panel de Trabajo

Al iniciar sesión, verás el **Panel de Administración** con acceso a:

![Panel del Administrador](images/admin-dashboard.png)

## Módulos Disponibles

| Módulo | Descripción | Ícono |
|--------|-------------|-------|
| **Usuarios** | Crear, editar, suspender usuarios | Personas |
| **Catálogos** | Zonas, Regiones, Sectores, Cuadrantes | Geolocalización |
| **Sugerencias** | Listas de autocompletado | Lista |
| **Config. IA** | Proveedor y parámetros de IA | Robot |
| **Config. Email** | Servidor SMTP para envíos | Correo |
| **Auditoría** | Registro de operaciones | Escudo |

## Responsabilidades Principales

### 1. Gestión de Usuarios

- Crear cuentas para nuevos oficiales y supervisores
- Asignar los roles correctos (CREADOR, REVISOR, ADMIN)
- Suspender usuarios que ya no deben acceder
- Restablecer contraseñas cuando sea necesario

### 2. Mantenimiento de Catálogos

- Mantener actualizada la estructura geográfica
- Agregar nuevas zonas, regiones, sectores o cuadrantes
- Configurar las listas de sugerencias para campos de formulario

### 3. Configuración del Sistema

- Configurar el proveedor de IA para reportes automatizados
- Configurar el servidor de correo electrónico
- Ajustar parámetros del sistema

### 4. Monitoreo y Auditoría

- Revisar el registro de operaciones
- Detectar actividad sospechosa
- Verificar el uso correcto del sistema

## Flujo de Trabajo Típico

```
┌─────────────────┐
│ Crear Usuario   │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Asignar Rol(es) │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Usuario Activo  │
└─────────────────┘
```

## Separación de Roles

| Función | CREADOR | REVISOR | ADMIN |
|---------|---------|---------|-------|
| Reportes | Si | Si | No |
| Usuarios | No | No | Si |
| Catálogos | No | No | Si |
| Auditoría | No | No | Si |

Esta separación asegura que:
- Los datos operativos (reportes) están protegidos
- La gestión técnica está centralizada
- Hay trazabilidad de todas las operaciones

## Próximos Pasos

- [Gestionar usuarios](Usuario-Admin-Gestion-Usuarios)
- [Asignar roles](Usuario-Admin-Asignar-Roles)
- [Configurar catálogos geográficos](Usuario-Admin-Catalogos-Geograficos)
- [Ver auditoría](Usuario-Admin-Auditoria)
