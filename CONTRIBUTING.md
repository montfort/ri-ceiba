# Guía de Contribución

¡Gracias por tu interés en contribuir a Ceiba! Este documento proporciona las guías y mejores prácticas para contribuir al proyecto.

## Tabla de Contenidos

- [Código de Conducta](#código-de-conducta)
- [¿Cómo Puedo Contribuir?](#cómo-puedo-contribuir)
- [Configuración del Entorno](#configuración-del-entorno)
- [Flujo de Trabajo](#flujo-de-trabajo)
- [Estándares de Código](#estándares-de-código)
- [Testing (TDD Obligatorio)](#testing-tdd-obligatorio)
- [Commits y Pull Requests](#commits-y-pull-requests)
- [Revisión de Código](#revisión-de-código)

## Código de Conducta

Este proyecto se adhiere a un código de conducta. Al participar, se espera que mantengas este código. Por favor, reporta comportamiento inaceptable a los mantenedores del proyecto.

### Nuestros Estándares

- Usar lenguaje acogedor e inclusivo
- Respetar diferentes puntos de vista y experiencias
- Aceptar críticas constructivas con gracia
- Enfocarse en lo que es mejor para la comunidad
- Mostrar empatía hacia otros miembros

## ¿Cómo Puedo Contribuir?

### Reportar Bugs

Antes de crear un reporte de bug:

1. **Revisa los issues existentes** para evitar duplicados
2. **Verifica que puedes reproducir el problema** en la última versión
3. **Recolecta información** sobre tu entorno (versión, navegador, OS)

Para reportar un bug, usa la [plantilla de bug report](https://github.com/montfort/ri-ceiba/issues/new?template=bug_report.md).

### Sugerir Funcionalidades

Las sugerencias de funcionalidades son bienvenidas. Usa la [plantilla de feature request](https://github.com/montfort/ri-ceiba/issues/new?template=feature_request.md) e incluye:

- Descripción clara del problema o necesidad
- Solución propuesta
- Alternativas consideradas
- Criterios de aceptación

### Contribuir Código

1. Busca issues etiquetados como `good first issue` o `help wanted`
2. Comenta en el issue indicando que trabajarás en él
3. Sigue el [flujo de trabajo](#flujo-de-trabajo) descrito abajo

## Configuración del Entorno

### Requisitos Previos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/get-started) y Docker Compose
- [Git](https://git-scm.com/)
- Editor recomendado: VS Code o JetBrains Rider

### Instalación

```bash
# Clonar el repositorio
git clone https://github.com/montfort/ri-ceiba.git
cd ri-ceiba

# Iniciar PostgreSQL con Docker
docker compose up -d ceiba-db

# Restaurar dependencias
dotnet restore

# Aplicar migraciones
cd src/Ceiba.Infrastructure
dotnet ef database update --startup-project ../Ceiba.Web

# Ejecutar la aplicación
cd ../Ceiba.Web
dotnet watch run
```

### Variables de Entorno

Copia el archivo de ejemplo y configura tus variables locales:

```bash
cp .env.example .env
```

Consulta la [documentación de variables de entorno](https://github.com/montfort/ri-ceiba/wiki/Ops-Config-Variables-Entorno) para más detalles.

## Flujo de Trabajo

### Ramas

| Rama | Propósito |
|------|-----------|
| `main` | Código estable de producción |
| `develop` | Integración de desarrollo |
| `feature/*` | Nuevas funcionalidades |
| `bugfix/*` | Corrección de bugs |
| `hotfix/*` | Correcciones urgentes de producción |

### Proceso de Contribución

1. **Fork** el repositorio (colaboradores externos)
2. **Crea una rama** desde `develop`:
   ```bash
   git checkout develop
   git pull origin develop
   git checkout -b feature/mi-nueva-funcionalidad
   ```
3. **Desarrolla** siguiendo TDD (ver [Testing](#testing-tdd-obligatorio))
4. **Commit** tus cambios (ver [Commits](#commits-y-pull-requests))
5. **Push** tu rama:
   ```bash
   git push origin feature/mi-nueva-funcionalidad
   ```
6. **Crea un Pull Request** hacia `develop`

## Estándares de Código

### Convenciones de Nombres (C#)

| Elemento | Convención | Ejemplo |
|----------|------------|---------|
| Clases, Métodos, Propiedades | PascalCase | `ReporteIncidencia`, `GetById()` |
| Variables locales, parámetros | camelCase | `reporteId`, `usuario` |
| Campos privados | _camelCase | `_context`, `_logger` |
| Interfaces | IPascalCase | `IReportService` |
| Constantes | UPPER_SNAKE_CASE | `MAX_PAGE_SIZE` |

### Estructura de Archivos

```
src/
├── Ceiba.Web/           # Capa de presentación (Blazor Server)
├── Ceiba.Core/          # Capa de dominio (entidades, interfaces)
├── Ceiba.Application/   # Capa de aplicación (servicios, DTOs)
├── Ceiba.Infrastructure/# Capa de infraestructura (EF Core, servicios externos)
└── Ceiba.Shared/        # DTOs compartidos, constantes
```

### Principios de Diseño

1. **Modularidad**: Cambios contenidos dentro de límites de módulo
2. **Separación de responsabilidades**: Cada clase tiene una única responsabilidad
3. **Inyección de dependencias**: Usar constructores para DI
4. **Inmutabilidad**: Preferir objetos inmutables cuando sea posible

### Lo que Debes Evitar

- ❌ Código comentado (elimínalo)
- ❌ `using` statements no utilizados
- ❌ Concatenación de SQL con input de usuario
- ❌ Credenciales hardcodeadas
- ❌ Métodos con más de 50 líneas
- ❌ Clases con más de 500 líneas

## Testing (TDD Obligatorio)

**El desarrollo guiado por tests (TDD) es obligatorio en este proyecto.**

### Ciclo Red-Green-Refactor

1. **🔴 RED**: Escribe un test que falle
2. **🟢 GREEN**: Escribe el código mínimo para pasar el test
3. **🔄 REFACTOR**: Mejora el código manteniendo los tests verdes

### Tipos de Tests

| Tipo | Ubicación | Framework | Cobertura Mínima |
|------|-----------|-----------|------------------|
| Unit | `tests/Ceiba.Core.Tests/` | xUnit | 90% |
| Service | `tests/Ceiba.Application.Tests/` | xUnit | 80% |
| Integration | `tests/Ceiba.Infrastructure.Tests/` | xUnit + Testcontainers | 70% |
| Component | `tests/Ceiba.Web.Tests/` | bUnit | Flujos clave |
| E2E | `tests/Ceiba.Integration.Tests/` | Playwright | Flujos críticos |

### Ejecutar Tests

```bash
# Todos los tests
dotnet test

# Por categoría
dotnet test --filter "Category=Unit"
dotnet test --filter "Category=Integration"

# Con cobertura
dotnet test --collect:"XPlat Code Coverage"
```

### Ejemplo de Test

```csharp
public class ReportServiceTests
{
    [Fact]
    public async Task CreateReport_WithValidData_ReturnsNewReport()
    {
        // Arrange
        var service = CreateService();
        var dto = new CreateReportDto { /* ... */ };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
    }
}
```

## Commits y Pull Requests

### Formato de Commits

Usamos [Conventional Commits](https://www.conventionalcommits.org/):

```
<tipo>(<alcance>): <descripción>

[cuerpo opcional]

[footer opcional]
```

#### Tipos Permitidos

| Tipo | Uso |
|------|-----|
| `feat` | Nueva funcionalidad |
| `fix` | Corrección de bug |
| `docs` | Cambios en documentación |
| `style` | Formato (sin cambios de código) |
| `refactor` | Refactorización |
| `test` | Agregar o modificar tests |
| `chore` | Tareas de mantenimiento |
| `perf` | Mejoras de rendimiento |
| `ci` | Cambios en CI/CD |

#### Ejemplos

```bash
feat(reports): add PDF export functionality

fix(auth): resolve session timeout not redirecting to login

docs(wiki): update installation guide for Docker

test(reports): add unit tests for CreateReportService
```

### Pull Requests

1. **Usa la plantilla** de PR proporcionada
2. **Completa todos los checklists** aplicables
3. **Vincula el issue** relacionado (`Closes #123`)
4. **Asegúrate** de que todos los tests pasen
5. **Solicita revisión** de al menos un mantenedor

#### Tamaño del PR

- **Ideal**: < 400 líneas de código
- **Máximo recomendado**: 800 líneas
- PRs grandes deben dividirse en PRs más pequeños

## Revisión de Código

### Como Autor

- Responde a los comentarios en tiempo razonable
- Explica decisiones técnicas cuando se solicite
- Haz los cambios solicitados o discute por qué no son apropiados
- No hagas merge de tu propio PR sin aprobación

### Como Revisor

- Sé constructivo y respetuoso
- Explica el "por qué" detrás de tus sugerencias
- Distingue entre "debe cambiar" y "sugerencia"
- Aprueba cuando el código cumpla los estándares

### Criterios de Aprobación

- [ ] Código sigue los estándares del proyecto
- [ ] Tests escritos y pasando (TDD)
- [ ] Sin vulnerabilidades de seguridad
- [ ] Documentación actualizada si aplica
- [ ] Checklist de PR completado

## Seguridad

### Reportar Vulnerabilidades

**NO reportes vulnerabilidades de seguridad en issues públicos.**

Usa [GitHub Security Advisories](https://github.com/montfort/ri-ceiba/security/advisories/new) para reportar vulnerabilidades de forma privada.

### Consideraciones de Seguridad

Al contribuir código, considera:

- Validación de input en servidor
- Prevención de SQL injection (usar EF Core/LINQ)
- Prevención de XSS (usar escapado de Blazor)
- Manejo seguro de credenciales
- Logging sin PII

## Recursos

- [Wiki del Proyecto](https://github.com/montfort/ri-ceiba/wiki)
- [Arquitectura del Sistema](https://github.com/montfort/ri-ceiba/wiki/Dev-Arquitectura)
- [Guía de Testing](https://github.com/montfort/ri-ceiba/wiki/Dev-Testing-TDD)
- [Estándares de Código](https://github.com/montfort/ri-ceiba/wiki/Dev-Estandares-Codigo)

## ¿Preguntas?

Si tienes preguntas que no están cubiertas aquí:

1. Revisa la [Wiki](https://github.com/montfort/ri-ceiba/wiki)
2. Busca en [Issues](https://github.com/montfort/ri-ceiba/issues) existentes
3. Abre una [Discussion](https://github.com/montfort/ri-ceiba/discussions)

---

¡Gracias por contribuir a Ceiba! 🌳
