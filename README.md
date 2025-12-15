<p align="center">
  <img src="docs/insignia_ceiba_200.png" alt="Insignia Ceiba" width="120" />
</p>

<h1 align="center">Ceiba - Sistema de Gestión de Reportes de Incidencias</h1>

<p align="center">
  <strong>Unidad Especializada en Género • Secretaría de Seguridad Ciudadana CDMX</strong>
</p>

<p align="center">
  <a href="https://sonarcloud.io/summary/new_code?id=montfort_ri-ceiba">
    <img src="https://sonarcloud.io/api/project_badges/quality_gate?project=montfort_ri-ceiba&token=7fdf5d3c3105b47b2629a78dc7e8103915109b22" alt="Quality Gate Status" />
  </a>
</p>

<p align="center">
  <a href="#características-principales">Características</a> •
  <a href="#arquitectura">Arquitectura</a> •
  <a href="#tecnologías">Tecnologías</a> •
  <a href="#instalación">Instalación</a> •
  <a href="#documentación">Documentación</a>
</p>

---

## 🛡️ Calidad de Código

Este proyecto ha sido analizado y **aprobado** por **SonarCloud**, una plataforma líder en análisis estático de código. El badge de "Quality Gate Passed" certifica que el código cumple con estándares profesionales de calidad en las siguientes dimensiones:

| Dimensión | Descripción |
|-----------|-------------|
| **Fiabilidad** | Código libre de bugs que podrían causar comportamiento inesperado |
| **Seguridad** | Sin vulnerabilidades conocidas (OWASP, inyección SQL, XSS, etc.) |
| **Mantenibilidad** | Código limpio, sin "code smells" críticos ni deuda técnica excesiva |
| **Cobertura** | Pruebas unitarias que validan la funcionalidad del sistema |
| **Duplicación** | Bajo nivel de código duplicado, promoviendo reutilización |

Este análisis continuo garantiza que cada cambio en el código mantiene los estándares de calidad requeridos para sistemas críticos de seguridad pública.

---

## 📋 Descripción

**Ceiba** es una aplicación web empresarial desarrollada para la **Unidad Especializada en Género de la Secretaría de Seguridad Ciudadana de la Ciudad de México (SSC CDMX)**. El sistema digitaliza y optimiza el proceso de registro, seguimiento y análisis de reportes de incidencias relacionadas con casos de género.

### 🎯 Propósito

- **Digitalizar** el proceso de reportes que tradicionalmente se manejaba en papel
- **Centralizar** la información de incidencias para análisis estadístico
- **Automatizar** la generación de informes ejecutivos con apoyo de IA
- **Garantizar** la trazabilidad completa mediante auditoría exhaustiva
- **Facilitar** la toma de decisiones con datos en tiempo real

---

## ✨ Características Principales

### 📝 Módulo de Reportes de Incidencias
- Creación de reportes **Tipo A** con formularios estructurados
- Flujo de estados: `Borrador` → `Entregado`
- Campos configurables: sexo, edad, tipo de delito, zona/sector/cuadrante
- Registro de hechos reportados, acciones realizadas y traslados
- Historial personal de reportes con búsqueda y filtrado

### 👁️ Módulo de Revisión (Supervisores)
- Visualización de **todos** los reportes del sistema
- Edición y complementación de información
- Exportación individual o masiva a **PDF** y **JSON**
- Filtros avanzados por fecha, zona, tipo de delito, agente

### 👥 Módulo de Administración
- Gestión completa de usuarios (crear, suspender, eliminar)
- Asignación de roles con permisos granulares
- Configuración de catálogos jerárquicos: Zona → Sector → Cuadrante
- Gestión de listas de sugerencias (delitos, tipos de atención, etc.)

### 📊 Módulo de Auditoría
- Registro automático de todas las operaciones críticas
- Almacenamiento de IP, usuario, timestamp y detalles de acción
- Retención **indefinida** de logs (nunca se eliminan)
- Búsqueda y filtrado por usuario, fecha y tipo de operación

### 🤖 Reportes Automatizados con IA
- Generación diaria programable de resúmenes ejecutivos
- Estadísticas: total de reportes, delitos frecuentes, zonas críticas
- Narrativa generada por **Inteligencia Artificial**
- Envío automático por correo electrónico
- Almacenamiento para consulta histórica

### 🔐 Seguridad
- Autenticación robusta con **ASP.NET Core Identity**
- Control de acceso basado en roles (RBAC)
- Timeout de sesión configurable (30 minutos por defecto)
- Política de contraseñas: mínimo 10 caracteres, mayúscula + número
- Cumplimiento con **OWASP Top 10**

---

## 👥 Roles y Permisos

| Funcionalidad | CREADOR | REVISOR | ADMIN |
|---------------|:-------:|:-------:|:-----:|
| Crear reportes | ✅ | ❌ | ❌ |
| Ver reportes propios | ✅ | ✅ | ❌ |
| Ver todos los reportes | ❌ | ✅ | ❌ |
| Editar reportes entregados | ❌ | ✅ | ❌ |
| Exportar PDF/JSON | ❌ | ✅ | ❌ |
| Gestionar usuarios | ❌ | ❌ | ✅ |
| Configurar catálogos | ❌ | ❌ | ✅ |
| Ver auditoría | ❌ | ❌ | ✅ |
| Reportes automatizados | ❌ | ✅ | ❌ |

---

## 🏗️ Arquitectura

El proyecto implementa **Clean Architecture** con principios de **Domain-Driven Design (DDD)** en una arquitectura monolítica modular:

```
┌─────────────────────────────────────────────────────────────────┐
│                        Ceiba.Web                                │
│                   (Blazor Server - UI)                          │
├─────────────────────────────────────────────────────────────────┤
│                     Ceiba.Application                           │
│                  (Servicios de Aplicación)                      │
├─────────────────────────────────────────────────────────────────┤
│                       Ceiba.Core                                │
│              (Entidades, Interfaces, Enums)                     │
├─────────────────────────────────────────────────────────────────┤
│                   Ceiba.Infrastructure                          │
│         (EF Core, Repositorios, Servicios Externos)             │
├─────────────────────────────────────────────────────────────────┤
│                      Ceiba.Shared                               │
│                   (DTOs, Constantes)                            │
└─────────────────────────────────────────────────────────────────┘
```

### Estructura del Proyecto

```
ri-ceiba/
├── src/
│   ├── Ceiba.Web/              # Presentación (Blazor Server SSR)
│   │   ├── Components/
│   │   │   ├── Layout/         # Layouts compartidos
│   │   │   └── Pages/          # Páginas por módulo
│   │   │       ├── Auth/       # Login, sesión
│   │   │       ├── Reports/    # CRUD de reportes
│   │   │       ├── Admin/      # Gestión de usuarios y catálogos
│   │   │       └── Automated/  # Reportes automatizados
│   │   └── wwwroot/            # Assets estáticos
│   │
│   ├── Ceiba.Core/             # Dominio (sin dependencias)
│   │   ├── Entities/           # Usuario, ReporteIncidencia, Zona...
│   │   ├── Interfaces/         # Contratos de repositorios
│   │   └── Enums/              # EstadoReporte, TipoReporte...
│   │
│   ├── Ceiba.Application/      # Casos de uso
│   │   ├── Services/           # Lógica de aplicación
│   │   ├── DTOs/               # Objetos de transferencia
│   │   └── Validators/         # Validaciones FluentValidation
│   │
│   ├── Ceiba.Infrastructure/   # Implementaciones técnicas
│   │   ├── Data/               # DbContext, Migrations
│   │   ├── Repositories/       # Implementación de repositorios
│   │   └── Services/           # PDF, Email, AI
│   │
│   └── Ceiba.Shared/           # Compartido entre capas
│
├── tests/                      # Suite completa de pruebas
│   ├── Ceiba.Core.Tests/       # Pruebas unitarias del dominio
│   ├── Ceiba.Application.Tests/# Pruebas de servicios
│   ├── Ceiba.Infrastructure.Tests/
│   ├── Ceiba.Web.Tests/        # Pruebas de componentes (bUnit)
│   └── Ceiba.Integration.Tests/# Pruebas E2E (Playwright)
│
├── specs/                      # Especificaciones del proyecto
│   └── 001-incident-management-system/
│       ├── spec.md             # 4 User Stories (P1-P4)
│       ├── plan.md             # Plan de implementación
│       ├── data-model.md       # Modelo de datos ER
│       ├── tasks.md            # 330+ tareas de implementación
│       └── contracts/          # OpenAPI 3.0 specs
│
├── docker/                     # Configuración Docker
├── scripts/                    # Scripts de utilidad
└── docs/                       # Documentación adicional
```

---

## 🛠️ Tecnologías

### Backend
| Tecnología | Versión | Propósito |
|------------|---------|-----------|
| .NET | 10.0 | Framework principal |
| ASP.NET Core | 10.0 | Framework web |
| Blazor Server | 10.0 | UI interactiva SSR |
| Entity Framework Core | 10.0 | ORM |
| ASP.NET Identity | 10.0 | Autenticación/Autorización |

### Base de Datos
| Tecnología | Versión | Propósito |
|------------|---------|-----------|
| PostgreSQL | 18 | Base de datos principal |
| Npgsql | 10.0 | Driver .NET para PostgreSQL |

### Servicios Externos
| Tecnología | Propósito |
|------------|-----------|
| QuestPDF | Generación de documentos PDF |
| MailKit | Envío de correos (SMTP) |
| OpenAI/Gemini/Otros | Generación de narrativas con IA |

### Observabilidad
| Tecnología | Propósito |
|------------|-----------|
| Serilog | Logging estructurado |
| OpenTelemetry | Trazas y métricas |
| .NET Aspire | Orquestación en desarrollo |

### Pruebas
| Framework | Propósito |
|-----------|-----------|
| xUnit | Framework de pruebas |
| bUnit | Pruebas de componentes Blazor |
| FluentAssertions | Aserciones legibles |
| Playwright | Pruebas E2E |
| Coverlet | Cobertura de código |

---

## 🚀 Instalación

### Prerrequisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/products/docker-desktop/) (recomendado)
- [PostgreSQL 18](https://www.postgresql.org/download/) (si no usa Docker)

### Opción 1: Con .NET Aspire (Recomendado)

Aspire orquesta automáticamente PostgreSQL en contenedor Docker:

```bash
# Clonar el repositorio
git clone https://github.com/montfort/ri-ceiba.git
cd ri-ceiba

# Iniciar con Aspire
dotnet run --project Ceiba.AppHost --launch-profile https
```

**Servicios disponibles:**
- 🎛️ **Dashboard Aspire:** https://localhost:17157 (métricas, logs, trazas)
- 🌐 **Ceiba Web:** URL mostrada en el dashboard
- 🐘 **PostgreSQL:** Contenedor Docker con persistencia

### Opción 2: Sin Aspire

```bash
# Clonar el repositorio
git clone https://github.com/montfort/ri-ceiba.git
cd ri-ceiba

# Configurar conexión a PostgreSQL
# Crear: src/Ceiba.Web/appsettings.Development.json
# Con: ConnectionStrings:DefaultConnection

# Aplicar migraciones
dotnet ef database update \
  --project src/Ceiba.Infrastructure \
  --startup-project src/Ceiba.Web

# Ejecutar
dotnet run --project src/Ceiba.Web
```

### Opción 3: Docker Compose (Producción)

```bash
# Desarrollo
docker compose -f docker/docker-compose.yml up -d

# Producción
docker compose -f docker/docker-compose.prod.yml up -d
```

---

## 🧪 Pruebas

El proyecto sigue **TDD (Test-Driven Development)** como metodología obligatoria:

```bash
# Ejecutar todas las pruebas
dotnet test

# Por categoría
dotnet test --filter "Category=Unit"        # Unitarias
dotnet test --filter "Category=Integration" # Integración
dotnet test --filter "Category=E2E"         # End-to-End

# Con cobertura
dotnet test --collect:"XPlat Code Coverage"
```

### Cobertura Objetivo
| Capa | Cobertura Mínima |
|------|------------------|
| Core | 90% |
| Application | 80% |
| Infrastructure | 70% |
| Web | Flujos críticos |

---

## 📖 Documentación de Diseño

| Documento | Descripción |
|-----------|-------------|
| [spec.md](specs/001-incident-management-system/spec.md) | Especificación de funcionalidades (4 User Stories) |
| [plan.md](specs/001-incident-management-system/plan.md) | Plan de implementación y arquitectura |
| [data-model.md](specs/001-incident-management-system/data-model.md) | Modelo de datos y diagrama ER |
| [quickstart.md](specs/001-incident-management-system/quickstart.md) | Guía rápida de desarrollo |
| [tasks.md](specs/001-incident-management-system/tasks.md) | 330+ tareas de implementación |
| [contracts/](specs/001-incident-management-system/contracts/) | Especificaciones OpenAPI 3.0 |

---

## ⚙️ Configuración

### Variables de Entorno (Producción)

```bash
# Base de datos
ConnectionStrings__DefaultConnection=Host=...;Database=ceiba;Username=...;Password=...

# Email
Email__Host=smtp.example.com
Email__Port=587
Email__Username=...
Email__Password=...

# IA (opcional)
AI__Provider=OpenAI|AzureOpenAI|Gemini|Local
AI__ApiKey=...
AI__Model=gpt-4

# Reportes automatizados
AutomatedReports__GenerationTime=06:00:00
AutomatedReports__Recipients=["email1@example.com"]
```

### Configuración de Seguridad

| Parámetro | Valor |
|-----------|-------|
| Timeout de sesión | 30 minutos |
| Longitud mínima de contraseña | 10 caracteres |
| Requisitos de contraseña | Mayúscula + Número |
| Retención de auditoría | Indefinida |
| Zona horaria | UTC |

---

## 🤝 Contribución

Este proyecto sigue principios estrictos de desarrollo:

1. **TDD Obligatorio** - Pruebas antes de implementación
2. **Clean Architecture** - Separación estricta de capas
3. **RBAC Estricto** - Usuarios solo ven lo permitido por su rol
4. **Auditoría Total** - Todas las acciones quedan registradas
5. **Documentación** - Todo módulo y API documentado

---

## 📄 Licencia

Proyecto desarrollado para la **Secretaría de Seguridad Ciudadana de la Ciudad de México**.

Uso restringido a personal autorizado de la SSC CDMX.

---

<p align="center">
  <sub>Diseño y desarrollo: <strong>José Villaseñor Montfort</strong> • <a href="https://enigmora.com">Enigmora SC</a></sub>
</p>
