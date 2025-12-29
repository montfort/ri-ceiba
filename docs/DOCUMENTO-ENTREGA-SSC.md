# Documento de Entrega

<p align="center">
  <img src="insignia_ceiba_200.png" alt="Insignia Ceiba" width="100" />
</p>

<h2 align="center">Ceiba - Sistema de Reportes de Incidencias</h2>

<p align="center">
  <strong>Versión 1.0.1</strong><br>
  Diciembre 2025
</p>

---

## Información del Proyecto

| Campo | Valor |
|-------|-------|
| **Sistema** | Ceiba - Sistema de Reportes de Incidencias |
| **Versión** | 1.0.1 |
| **Fecha de Entrega** | Diciembre 2025 |
| **Destinatario** | Secretaría de Seguridad Ciudadana de la Ciudad de México |
| **Unidad Solicitante** | Unidad Especializada en Género |
| **Desarrollador** | José Villaseñor Montfort |
| **Empresa** | Enigmora SC |

---

## 1. Descripción del Sistema

**Ceiba** es una aplicación web empresarial desarrollada para digitalizar y optimizar el proceso de registro, seguimiento y análisis de reportes de incidencias relacionadas con casos de género.

### Objetivos Cumplidos

- ✅ Digitalización del proceso de reportes (anteriormente en papel)
- ✅ Centralización de información para análisis estadístico
- ✅ Automatización de informes ejecutivos con apoyo de IA
- ✅ Trazabilidad completa mediante auditoría exhaustiva
- ✅ Soporte para toma de decisiones con datos en tiempo real

---

## 2. Componentes Entregados

### 2.1 Software

| Componente | Descripción |
|------------|-------------|
| **Código Fuente** | Aplicación completa en C# / .NET 10 |
| **Base de Datos** | Esquema PostgreSQL con migraciones |
| **Contenedores** | Configuración Docker para desarrollo y producción |
| **Tests** | Suite completa de pruebas automatizadas (2,579 tests) |

### 2.2 Documentación

| Documento | Ubicación | Descripción |
|-----------|-----------|-------------|
| **Wiki Completa** | [GitHub Wiki](https://github.com/montfort/ri-ceiba/wiki) | 66 páginas de documentación |
| **Manual de Usuario** | Wiki - Sección Usuario | Guías para CREADOR, REVISOR, ADMIN |
| **Manual Técnico** | Wiki - Sección Desarrollador | Arquitectura, código, testing |
| **Manual de Operaciones** | Wiki - Sección Operaciones | Instalación, configuración, mantenimiento |
| **Especificaciones** | `/specs/001-incident-management-system/` | Diseño detallado del sistema |

### 2.3 Archivos de Configuración

| Archivo | Propósito |
|---------|-----------|
| `docker/Dockerfile` | Construcción de imagen Docker |
| `docker/docker-compose.yml` | Orquestación para desarrollo |
| `docker/docker-compose.prod.yml` | Orquestación para producción |
| `docker/.env.example` | Plantilla de variables de entorno |

---

## 3. Módulos Funcionales

### 3.1 Módulo de Autenticación

| Característica | Implementación |
|----------------|----------------|
| Framework | ASP.NET Core Identity |
| Roles | CREADOR, REVISOR, ADMIN |
| Sesión | Timeout 30 minutos configurable |
| Contraseñas | Mínimo 10 caracteres, mayúscula + número |

### 3.2 Módulo de Reportes de Incidencias

| Característica | Implementación |
|----------------|----------------|
| Tipo de Reporte | Tipo A (formulario estructurado) |
| Estados | Borrador → Entregado |
| Autoguardado | Sí, automático |
| Campos | Sexo, edad, delito, zona/sector/cuadrante, hechos, acciones |

### 3.3 Módulo de Revisión (Supervisores)

| Característica | Implementación |
|----------------|----------------|
| Vista | Todos los reportes del sistema |
| Edición | Permitida incluso en reportes entregados |
| Exportación | PDF individual, JSON individual, exportación masiva |
| Filtros | Por fecha, zona, tipo de delito, agente |

### 3.4 Módulo de Administración

| Característica | Implementación |
|----------------|----------------|
| Usuarios | Crear, editar, suspender, eliminar |
| Roles | Asignación múltiple por usuario |
| Catálogos | Zona → Región → Sector → Cuadrante |
| Sugerencias | Listas configurables (delitos, tipos de atención, etc.) |

### 3.5 Módulo de Auditoría

| Característica | Implementación |
|----------------|----------------|
| Registro | Automático de todas las operaciones |
| Datos Capturados | Usuario, IP, timestamp, acción, detalles |
| Retención | Indefinida (nunca se eliminan) |
| Búsqueda | Por usuario, fecha, tipo de operación |

### 3.6 Módulo de Reportes Automatizados

| Característica | Implementación |
|----------------|----------------|
| Generación | Diaria, horario configurable |
| Contenido | Estadísticas + narrativa generada por IA |
| Distribución | Envío automático por email |
| Historial | Almacenamiento para consulta posterior |

---

## 4. Especificaciones Técnicas

### 4.1 Stack Tecnológico

| Capa | Tecnología | Versión |
|------|------------|---------|
| **Backend** | .NET | 10.0 |
| **Frontend** | Blazor Server | 10.0 |
| **Base de Datos** | PostgreSQL | 18 |
| **ORM** | Entity Framework Core | 10.0 |
| **Autenticación** | ASP.NET Identity | 10.0 |
| **PDF** | QuestPDF | Última estable |
| **Email** | MailKit | Última estable |
| **Logging** | Serilog | Última estable |

### 4.2 Requisitos de Infraestructura

#### Servidor de Aplicación

| Recurso | Mínimo | Recomendado |
|---------|--------|-------------|
| CPU | 2 cores | 4 cores |
| RAM | 4 GB | 8 GB |
| Almacenamiento | 20 GB SSD | 50 GB SSD |
| Sistema Operativo | Linux (Fedora 42+) o Windows Server 2022 |

#### Servidor de Base de Datos

| Recurso | Mínimo | Recomendado |
|---------|--------|-------------|
| CPU | 2 cores | 4 cores |
| RAM | 4 GB | 8 GB |
| Almacenamiento | 50 GB SSD | 100 GB SSD |
| Software | PostgreSQL 18 |

### 4.3 Puertos de Red

| Puerto | Servicio | Acceso |
|--------|----------|--------|
| 443 | HTTPS (aplicación) | Usuarios |
| 5432 | PostgreSQL | Solo interno |
| 587 | SMTP (email) | Saliente |

---

## 5. Seguridad

### 5.1 Cumplimiento OWASP Top 10

| Vulnerabilidad | Mitigación Implementada |
|----------------|------------------------|
| A01 - Broken Access Control | RBAC estricto, políticas de autorización |
| A02 - Cryptographic Failures | HTTPS, contraseñas hasheadas, PII redactada |
| A03 - Injection | EF Core/LINQ obligatorio, sin SQL raw |
| A04 - Insecure Design | Principio de mínimo privilegio |
| A05 - Security Misconfiguration | Headers de seguridad (CSP, HSTS) |
| A06 - Vulnerable Components | Dependencias actualizadas |
| A07 - Auth Failures | Política de contraseñas, timeout de sesión |
| A08 - Data Integrity | Migraciones con rollback, backups |
| A09 - Logging Failures | Auditoría completa, redacción de PII |
| A10 - SSRF | Sin URLs controladas por usuario |

### 5.2 Análisis de Código

El código ha sido analizado y aprobado por **SonarCloud** verificando:
- Fiabilidad (sin bugs críticos)
- Seguridad (sin vulnerabilidades conocidas)
- Mantenibilidad (código limpio)
- Cobertura de pruebas (>80%)

---

## 6. Calidad del Software

### 6.1 Pruebas Automatizadas

| Tipo | Cantidad | Estado |
|------|----------|--------|
| Pruebas Unitarias (Core) | 442 | ✅ Passed |
| Pruebas de Aplicación | 381 | ✅ Passed |
| Pruebas de Infraestructura | 752 | ✅ Passed |
| Pruebas de Componentes (Blazor) | 764 | ✅ Passed |
| Pruebas de Integración | 48 | ✅ Passed |
| **Total** | **2,579** | **✅ Passed** |

### 6.2 Cobertura de Código

| Capa | Cobertura |
|------|-----------|
| Core (Dominio) | >90% |
| Application (Servicios) | >80% |
| Infrastructure (Datos) | >70% |
| Web (Componentes) | Flujos críticos |

---

## 7. Instalación Rápida

### Opción 1: Docker (Recomendada)

```bash
# Clonar repositorio
git clone https://github.com/montfort/ri-ceiba.git
cd ri-ceiba

# Configurar variables de entorno
cp docker/.env.example docker/.env
# Editar docker/.env con valores de producción

# Iniciar servicios
docker compose -f docker/docker-compose.prod.yml up -d
```

### Opción 2: Manual

Consultar la guía completa en:
- [Instalación en Linux](https://github.com/montfort/ri-ceiba/wiki/Ops-Instalacion-Linux)
- [Instalación con Docker](https://github.com/montfort/ri-ceiba/wiki/Ops-Instalacion-Docker)

---

## 8. Configuración Inicial

### Setup Wizard

Al acceder por primera vez, el sistema presenta un asistente de configuración:

1. **Verificación de BD** - Conexión a PostgreSQL
2. **Creación de Admin** - Usuario administrador inicial
3. **Datos Iniciales** - Carga de catálogos y sugerencias

### Credenciales Iniciales

El usuario administrador se crea durante el Setup Wizard. **No existen credenciales por defecto.**

---

## 9. Documentación de Referencia

### Para Usuarios

| Documento | URL |
|-----------|-----|
| Primeros Pasos | [Wiki: Usuario-Primeros-Pasos](https://github.com/montfort/ri-ceiba/wiki/Usuario-Primeros-Pasos) |
| Guía CREADOR | [Wiki: Usuario-Creador-Introduccion](https://github.com/montfort/ri-ceiba/wiki/Usuario-Creador-Introduccion) |
| Guía REVISOR | [Wiki: Usuario-Revisor-Introduccion](https://github.com/montfort/ri-ceiba/wiki/Usuario-Revisor-Introduccion) |
| Guía ADMIN | [Wiki: Usuario-Admin-Introduccion](https://github.com/montfort/ri-ceiba/wiki/Usuario-Admin-Introduccion) |

### Para Operaciones

| Documento | URL |
|-----------|-----|
| Requisitos del Sistema | [Wiki: Ops-Requisitos-Sistema](https://github.com/montfort/ri-ceiba/wiki/Ops-Requisitos-Sistema) |
| Variables de Entorno | [Wiki: Ops-Config-Variables-Entorno](https://github.com/montfort/ri-ceiba/wiki/Ops-Config-Variables-Entorno) |
| Backup y Restauración | [Wiki: Ops-Mant-Backup-Restore](https://github.com/montfort/ri-ceiba/wiki/Ops-Mant-Backup-Restore) |
| Troubleshooting | [Wiki: Ops-Mant-Troubleshooting](https://github.com/montfort/ri-ceiba/wiki/Ops-Mant-Troubleshooting) |

### Para Desarrolladores

| Documento | URL |
|-----------|-----|
| Arquitectura | [Wiki: Dev-Arquitectura](https://github.com/montfort/ri-ceiba/wiki/Dev-Arquitectura) |
| Estándares de Código | [Wiki: Dev-Estandares-Codigo](https://github.com/montfort/ri-ceiba/wiki/Dev-Estandares-Codigo) |
| Guía de Testing | [Wiki: Dev-Testing-TDD](https://github.com/montfort/ri-ceiba/wiki/Dev-Testing-TDD) |

---

## 10. Licencia y Derechos

### Resumen de Licencia

| Derecho | Permitido |
|---------|:---------:|
| Uso interno por la SSC CDMX | ✅ |
| Acceso al código fuente | ✅ |
| Modificación del código | ✅ |
| Distribución entre unidades de la SSC | ✅ |
| Redistribución pública | ❌ |
| Venta o sublicenciamiento | ❌ |
| Remoción de atribución | ❌ |

### Atribución Requerida

El sistema debe mantener la siguiente atribución visible:

> **Desarrollado por José Villaseñor Montfort**

### Archivos de Licencia

- `LICENSE` - Términos completos de la licencia
- `NOTICE` - Aviso de atribución y tecnologías utilizadas

---

## 11. Soporte Post-Entrega

### Período de Garantía

El desarrollador proporciona soporte para corrección de defectos durante el período acordado contractualmente.

### Canales de Reporte

Para reportar bugs o solicitar soporte técnico:

1. **Issues de GitHub**: [github.com/montfort/ri-ceiba/issues](https://github.com/montfort/ri-ceiba/issues)
2. **Vulnerabilidades de Seguridad**: [GitHub Security Advisories](https://github.com/montfort/ri-ceiba/security/advisories/new)

### Exclusiones de Soporte

- Modificaciones realizadas por terceros
- Problemas de infraestructura del cliente
- Configuraciones fuera del alcance documentado

---

## 12. Anexos

### A. Estructura del Repositorio

```
ri-ceiba/
├── src/                    # Código fuente
│   ├── Ceiba.Web/          # Aplicación Blazor Server
│   ├── Ceiba.Core/         # Dominio (entidades, interfaces)
│   ├── Ceiba.Application/  # Servicios de aplicación
│   ├── Ceiba.Infrastructure/ # Acceso a datos, servicios externos
│   └── Ceiba.Shared/       # DTOs compartidos
├── tests/                  # Pruebas automatizadas
├── specs/                  # Especificaciones de diseño
├── docker/                 # Configuración Docker
├── docs/                   # Documentación adicional
│   └── wiki/               # Contenido del Wiki
├── scripts/                # Scripts de utilidad
├── LICENSE                 # Licencia del software
├── NOTICE                  # Aviso de atribución
├── README.md               # Descripción del proyecto
├── CHANGELOG.md            # Historial de cambios
├── CONTRIBUTING.md         # Guía de contribución
├── CODE_OF_CONDUCT.md      # Código de conducta
└── SECURITY.md             # Política de seguridad
```

### B. Historial de Versiones

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0.1 | Diciembre 2025 | Página de créditos y licencia, correcciones de CI/CD y tests |
| 1.0.0 | Diciembre 2025 | Primera versión estable |

### C. Contacto del Desarrollador

| Campo | Valor |
|-------|-------|
| **Nombre** | José Villaseñor Montfort |
| **Empresa** | Enigmora SC |
| **Web** | [enigmora.com](https://enigmora.com) |

---

## Firmas de Conformidad

### Entrega

| Campo | Valor |
|-------|-------|
| **Entregado por** | _________________________________ |
| **Nombre** | José Villaseñor Montfort |
| **Fecha** | _________________________________ |
| **Firma** | _________________________________ |

### Recepción

| Campo | Valor |
|-------|-------|
| **Recibido por** | _________________________________ |
| **Cargo** | _________________________________ |
| **Unidad** | _________________________________ |
| **Fecha** | _________________________________ |
| **Firma** | _________________________________ |

---

<p align="center">
  <sub>
    <strong>Ceiba v1.0.1</strong><br>
    Sistema de Reportes de Incidencias<br>
    Copyright © 2025 José Villaseñor Montfort. Todos los derechos reservados.
  </sub>
</p>
