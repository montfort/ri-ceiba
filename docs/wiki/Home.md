# Ceiba - Sistema de Reportes de Incidencias

Bienvenido a la documentación oficial del sistema **Ceiba**, una aplicación web para la gestión de reportes de incidencias policiales.

## Selecciona tu perfil

### Para Usuarios del Sistema

Si eres un usuario del sistema (oficial de policía, supervisor o administrador):

- [Primeros Pasos](Usuario-Primeros-Pasos)
- [Guía para Creadores (Oficiales)](Usuario-Creador-Introduccion)
- [Guía para Revisores (Supervisores)](Usuario-Revisor-Introduccion)
- [Guía para Administradores](Usuario-Admin-Introduccion)

### Para Desarrolladores

Si vas a contribuir al código o extender el sistema:

- [Inicio Rápido](Dev-Inicio-Rapido)
- [Arquitectura del Sistema](Dev-Arquitectura)
- [Estándares de Código](Dev-Estandares-Codigo)
- [Guía de Testing (TDD)](Dev-Testing-TDD)

### Para Implementadores / DevOps

Si vas a instalar o mantener el sistema en producción:

- [Requisitos del Sistema](Ops-Requisitos-Sistema)
- [Instalación con Docker](Ops-Instalacion-Docker)
- [Instalación en Linux](Ops-Instalacion-Linux)
- [Variables de Entorno](Ops-Config-Variables-Entorno)

---

## Características Principales

| Módulo | Descripción |
|--------|-------------|
| **Reportes de Incidencias** | Creación, edición y envío de reportes Tipo A |
| **Revisión de Reportes** | Visualización y edición de todos los reportes |
| **Exportación** | Generación de PDF y JSON individual o masivo |
| **Reportes Automatizados** | Generación diaria con IA y envío por email |
| **Administración** | Gestión de usuarios, roles y catálogos |
| **Auditoría** | Registro completo de operaciones |

## Roles del Sistema

| Rol | Permisos |
|-----|----------|
| **CREADOR** | Crear y editar reportes propios, enviar reportes |
| **REVISOR** | Ver todos los reportes, editar, exportar PDF/JSON |
| **ADMIN** | Gestionar usuarios, catálogos y ver auditoría |

---

**Versión**: 1.0
**Última actualización**: 2025-12-18
