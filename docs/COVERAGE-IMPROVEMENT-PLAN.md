# Plan de Mejora de Cobertura de Código

**Objetivo**: Incrementar la cobertura de código del 52.7% al 85% mínimo
**Líneas a cubrir**: ~4,100 líneas
**Líneas adicionales necesarias**: ~1,325 líneas (~32.3% adicional)

## Estado Actual

| Métrica | Valor Actual | Objetivo |
|---------|--------------|----------|
| Cobertura | 52.7% | 85% |
| Líneas cubiertas | ~2,160 | ~3,485 |
| Líneas sin cubrir | ~1,940 | ~615 |

## Análisis de Brechas por Capa

### 1. Capa de Aplicación (Ceiba.Application)

#### Servicios SIN Tests o con cobertura insuficiente:

| Archivo | Estado | Prioridad | Líneas Est. |
|---------|--------|-----------|-------------|
| `Jobs/ExportJob.cs` | Sin tests | Alta | ~80 |
| `Jobs/AutomatedReportJob.cs` | Parcial | Alta | ~60 |
| `Services/ReportService.cs` | Parcial | Alta | ~150 |
| `Services/DocumentConversionService.cs` | Parcial | Media | ~100 |

### 2. Capa de Infraestructura (Ceiba.Infrastructure)

#### Servicios SIN Tests o con cobertura insuficiente:

| Archivo | Estado | Prioridad | Líneas Est. |
|---------|--------|-----------|-------------|
| `Services/EmailService.cs` | Parcial | Alta | ~80 |
| `Services/EmailQueueProcessorService.cs` | Sin tests | Alta | ~60 |
| `Services/AutomatedReportBackgroundService.cs` | Sin tests | Alta | ~50 |
| `Services/UserManagementService.cs` | Parcial | Alta | ~120 |
| `Services/AiNarrativeService.cs` | Parcial | Media | ~80 |
| `Services/ResilientEmailService.cs` | Parcial | Media | ~100 |
| `Repositories/ReportRepository.cs` | Parcial | Alta | ~80 |
| `Data/AuditSaveChangesInterceptor.cs` | Parcial | Media | ~40 |
| `Security/InputSanitizer.cs` | Parcial | Media | ~60 |
| `Logging/PIIRedactionEnricher.cs` | Sin tests | Baja | ~30 |

### 3. Capa Web (Ceiba.Web)

#### Controllers SIN Tests o con cobertura insuficiente:

| Archivo | Estado | Prioridad | Líneas Est. |
|---------|--------|-----------|-------------|
| `Controllers/AutomatedReportsController.cs` | Sin tests | Alta | ~80 |
| `Controllers/AutomatedReportConfigController.cs` | Sin tests | Alta | ~60 |
| `Controllers/EmailConfigController.cs` | Sin tests | Media | ~50 |

#### Middleware SIN Tests:

| Archivo | Estado | Prioridad | Líneas Est. |
|---------|--------|-----------|-------------|
| `Middleware/ErrorHandlingMiddleware.cs` | Sin tests | Alta | ~60 |
| `Middleware/SecurityHeadersMiddleware.cs` | Sin tests | Media | ~40 |
| `Middleware/UserAgentValidationMiddleware.cs` | Sin tests | Baja | ~30 |

#### Componentes Blazor con cobertura insuficiente:

| Archivo | Estado | Prioridad | Líneas Est. |
|---------|--------|-----------|-------------|
| `Components/Pages/Reports/` | Parcial | Alta | ~200 |
| `Components/Pages/Admin/` | Parcial | Media | ~150 |
| `Components/Pages/Automated/` | Sin tests | Media | ~100 |

## Plan de Implementación por Fases

### Fase 1: Alta Prioridad (Objetivo: +15% → 67.7%)

**Duración estimada**: 2-3 días
**Archivos a cubrir**: 8 archivos críticos

#### 1.1 Controllers Web
- [ ] `AutomatedReportsControllerTests.cs` - CRUD de reportes automatizados
- [ ] `AutomatedReportConfigControllerTests.cs` - Configuración de reportes

#### 1.2 Servicios de Aplicación
- [ ] `ExportJobTests.cs` - Jobs de exportación en background
- [ ] Ampliar `ReportServiceTests.cs` - Casos edge y errores

#### 1.3 Middleware
- [ ] `ErrorHandlingMiddlewareTests.cs` - Manejo de excepciones
- [ ] Ampliar `AuthorizationLoggingMiddlewareTests.cs`

#### 1.4 Servicios de Infraestructura
- [ ] `EmailQueueProcessorServiceTests.cs` - Procesamiento de cola
- [ ] Ampliar `UserManagementServiceTests.cs`

### Fase 2: Media Prioridad (Objetivo: +10% → 77.7%)

**Duración estimada**: 2 días
**Archivos a cubrir**: 8 archivos

#### 2.1 Controllers
- [ ] `EmailConfigControllerTests.cs`

#### 2.2 Servicios
- [ ] `AutomatedReportBackgroundServiceTests.cs`
- [ ] Ampliar `ResilientEmailServiceTests.cs`
- [ ] Ampliar `AiNarrativeServiceTests.cs`
- [ ] Ampliar `DocumentConversionServiceTests.cs`

#### 2.3 Middleware
- [ ] `SecurityHeadersMiddlewareTests.cs`

#### 2.4 Repositorios
- [ ] Ampliar `ReportRepositoryTests.cs` - Más escenarios

### Fase 3: Completar Objetivo (Objetivo: +7.3% → 85%)

**Duración estimada**: 1-2 días

#### 3.1 Componentes Blazor
- [ ] Tests adicionales para páginas de Reports
- [ ] Tests para páginas de Admin
- [ ] Tests para páginas de Automated Reports

#### 3.2 Utilidades
- [ ] `PIIRedactionEnricherTests.cs`
- [ ] Ampliar `InputSanitizerTests.cs`
- [ ] `UserAgentValidationMiddlewareTests.cs`

## Exclusiones de Cobertura (Ya configuradas)

Los siguientes archivos están excluidos del análisis de cobertura:
- `**/Migrations/**` - Migraciones auto-generadas
- `**/Tests/**` - Código de pruebas
- `**/*.Designer.cs` - Archivos de diseñador
- `**/Data/Configurations/**` - Configuraciones EF
- `**/Interfaces/**` - Solo definiciones
- `**/DTOs/**` - Objetos de transferencia
- `**/Enums/**` - Enumeraciones
- `**/Entities/**` - Entidades (validación ya testeada)
- `**/Exceptions/**` - Clases de excepción
- `**/Program.cs` - Punto de entrada
- `**/GlobalUsings.cs` - Usings globales

## Métricas de Seguimiento

| Fase | Cobertura Objetivo | Tests Nuevos Est. | Líneas Cubiertas |
|------|-------------------|-------------------|------------------|
| Inicial | 52.7% | - | ~2,160 |
| Fase 1 | 67.7% | ~40-50 | ~2,775 |
| Fase 2 | 77.7% | ~30-40 | ~3,185 |
| Fase 3 | 85.0% | ~20-30 | ~3,485 |

## Comandos Útiles

```bash
# Ejecutar tests con cobertura local
dotnet test --collect:"XPlat Code Coverage"

# Ver reporte de cobertura
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coveragereport

# Ejecutar tests específicos
dotnet test --filter "FullyQualifiedName~ExportJob"
```

## Notas de Implementación

1. **TDD**: Escribir tests antes de identificar código faltante
2. **Mocking**: Usar Moq para dependencias externas
3. **Casos Edge**: Incluir casos de error y límites
4. **Integración**: Tests de integración para flujos completos
5. **bUnit**: Para componentes Blazor interactivos

---
*Documento generado: 2025-12-14*
*Última actualización: En progreso*
