# Changelog

Todos los cambios notables en este proyecto serán documentados en este archivo.

El formato está basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.0.0/),
y este proyecto adhiere a [Semantic Versioning](https://semver.org/lang/es/).

## [Unreleased]

### Agregado
- Nada aún

### Cambiado
- Nada aún

### Corregido
- Nada aún

---

## [1.0.0] - 2025-01-18

### Agregado

#### Módulo de Autenticación
- Sistema de autenticación con ASP.NET Identity
- Tres roles de usuario: CREADOR, REVISOR, ADMIN
- Timeout de sesión configurable (30 minutos por defecto)
- Página de login con validación de credenciales
- Logout seguro con limpieza de sesión

#### Módulo de Reportes de Incidencias
- Creación de reportes Tipo A con formulario completo
- Estados de reporte: Borrador y Entregado
- Guardado automático de borradores
- Transición de estados (Borrador → Entregado)
- Validación de campos requeridos
- Campos con sugerencias autocompletables (Sexo, Delito, Tipo de Atención, etc.)

#### Módulo de Revisión (Supervisores)
- Vista de todos los reportes del sistema
- Edición de reportes (incluyendo entregados)
- Exportación individual a PDF
- Exportación individual a JSON
- Exportación masiva (bulk) de reportes
- Filtros por fecha, estado y creador

#### Módulo de Administración
- Gestión de usuarios (crear, editar, suspender, eliminar)
- Asignación de roles múltiples por usuario
- Gestión de catálogos geográficos (Zona, Región, Sector, Cuadrante)
- Gestión de sugerencias de reportes
- Visualización de logs de auditoría con filtros

#### Módulo de Reportes Automatizados
- Generación diaria de reportes con IA
- Configuración de horario de generación
- Plantillas de reportes personalizables
- Envío automático por email
- Cola de emails con reintentos
- Historial de reportes generados

#### Módulo de Auditoría
- Registro automático de todas las operaciones
- Interceptor de EF Core para cambios en entidades
- Redacción de PII en logs (PIIRedactionEnricher)
- Filtros por usuario, acción y fecha
- Retención indefinida de registros

#### Setup Wizard
- Configuración inicial estilo WordPress
- Creación de usuario administrador
- Validación de conexión a base de datos
- Seed de datos iniciales (catálogos, sugerencias)

#### Infraestructura
- Soporte para PostgreSQL 18
- Migraciones de Entity Framework Core
- Docker Compose para desarrollo y producción
- Health checks (base de datos, email)
- Logging estructurado con Serilog
- Soporte para múltiples proveedores de IA (OpenAI, Azure OpenAI, Local)

#### Documentación
- Wiki completa en GitHub (66 páginas)
- Documentación para usuarios (CREADOR, REVISOR, ADMIN)
- Documentación para desarrolladores
- Documentación para operaciones/DevOps
- Templates de issues (bug report, feature request)
- Template de pull request con checklist de seguridad
- Guía de contribución (CONTRIBUTING.md)
- Código de conducta (CODE_OF_CONDUCT.md)
- Política de seguridad (SECURITY.md)

#### Testing
- Tests unitarios con xUnit
- Tests de componentes Blazor con bUnit
- Tests de integración
- Cobertura de código > 80%
- Integración con SonarCloud

### Seguridad
- Prevención de SQL Injection (EF Core/LINQ obligatorio)
- Prevención de XSS (escapado automático de Blazor)
- Headers de seguridad (CSP, HSTS, X-Frame-Options)
- Validación de User-Agent para prevenir secuestro de sesión
- Tokens anti-CSRF en formularios
- Política de contraseñas seguras (mín. 10 caracteres)
- Redacción de PII en logs

---

## Convenciones de Versionado

Este proyecto usa [Semantic Versioning](https://semver.org/lang/es/):

- **MAJOR**: Cambios incompatibles con versiones anteriores
- **MINOR**: Nueva funcionalidad compatible hacia atrás
- **PATCH**: Correcciones de bugs compatibles hacia atrás

## Tipos de Cambios

- **Agregado**: Nuevas funcionalidades
- **Cambiado**: Cambios en funcionalidad existente
- **Obsoleto**: Funcionalidades que serán removidas próximamente
- **Eliminado**: Funcionalidades removidas
- **Corregido**: Correcciones de bugs
- **Seguridad**: Correcciones de vulnerabilidades

---

[Unreleased]: https://github.com/montfort/ri-ceiba/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/montfort/ri-ceiba/releases/tag/v1.0.0
