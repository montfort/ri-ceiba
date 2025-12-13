# Plan de Reestructuración: Jerarquía Geográfica con Región

**Fecha**: 2025-12-12
**Branch**: `001-incident-management-system`
**Estado**: EN IMPLEMENTACIÓN

---

## Estado de Implementación

| Fase | Estado | Descripción |
|------|--------|-------------|
| Fase 1 | COMPLETADA | Core Layer - Entidad Region, modificaciones a entidades |
| Fase 2 | COMPLETADA | Infrastructure Layer - DbContext, configuraciones EF |
| Fase 3 | COMPLETADA | Shared Layer - DTOs para Region |
| Fase 4 | COMPLETADA | Application Layer - Servicios y validadores |
| Fase 5 | COMPLETADA | Web Layer - Componentes Blazor y controladores |
| Fase 6 | PENDIENTE | Contratos API y documentación OpenAPI |
| Fase 7 | EN PROGRESO | Tests - Actualización de archivos de prueba |
| Fase 8 | PENDIENTE | Migración EF Core |

### Archivos Modificados (Implementación Principal)
- `src/Ceiba.Core/Entities/Region.cs` (NUEVO)
- `src/Ceiba.Core/Entities/Zona.cs`
- `src/Ceiba.Core/Entities/Sector.cs`
- `src/Ceiba.Core/Entities/ReporteIncidencia.cs`
- `src/Ceiba.Core/Interfaces/ICatalogService.cs`
- `src/Ceiba.Core/Interfaces/ICatalogAdminService.cs`
- `src/Ceiba.Infrastructure/Data/CeibaDbContext.cs`
- `src/Ceiba.Infrastructure/Data/Configurations/RegionConfiguration.cs` (NUEVO)
- `src/Ceiba.Infrastructure/Data/Configurations/SectorConfiguration.cs`
- `src/Ceiba.Infrastructure/Data/Configurations/ReporteIncidenciaConfiguration.cs`
- `src/Ceiba.Infrastructure/Services/CatalogService.cs`
- `src/Ceiba.Infrastructure/Services/CachedCatalogService.cs`
- `src/Ceiba.Infrastructure/Services/CatalogAdminService.cs`
- `src/Ceiba.Infrastructure/Data/SeedDataService.cs`
- `src/Ceiba.Infrastructure/Caching/CacheKeys.cs`
- `src/Ceiba.Infrastructure/Repositories/ReportRepository.cs`
- `src/Ceiba.Shared/DTOs/AdminDTOs.cs`
- `src/Ceiba.Shared/DTOs/ReportDTOs.cs`
- `src/Ceiba.Shared/DTOs/Export/ReportExportDto.cs`
- `src/Ceiba.Application/Services/ReportService.cs`
- `src/Ceiba.Application/Validators/ReportValidators.cs`
- `src/Ceiba.Application/Services/Export/ExportService.cs`
- `src/Ceiba.Application/Services/Export/PdfGenerator.cs`
- `src/Ceiba.Web/Controllers/CatalogsController.cs`
- `src/Ceiba.Web/Controllers/AdminController.cs`
- `src/Ceiba.Web/Components/Pages/Reports/ReportForm.razor`
- `src/Ceiba.Web/Components/Pages/Admin/CatalogManager.razor`

---

## 1. Resumen del Cambio

### Modelo Actual (3 niveles)
```
Zona (1) ──< (N) Sector (1) ──< (N) Cuadrante
```

### Modelo Nuevo (4 niveles)
```
Zona (1) ──< (N) Región (1) ──< (N) Sector (1) ──< (N) Cuadrante
```

### Impacto
- **Nueva entidad**: `Región` (nivel intermedio entre Zona y Sector)
- **Modificación de FK**: `Sector.ZonaId` → `Sector.RegionId`
- **Nuevos campos en ReporteIncidencia**: `RegionId` (FK)
- **Archivos afectados**: 116 archivos identificados

---

## 2. Nueva Entidad: Región

### Definición
```csharp
// src/Ceiba.Core/Entities/Region.cs
public class Region : BaseCatalogEntity
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;

    // FK a Zona (padre)
    public int ZonaId { get; set; }
    public virtual Zona Zona { get; set; } = null!;

    // Navegación: Sectores hijos
    public virtual ICollection<Sector> Sectores { get; set; } = new List<Sector>();

    // Navegación: Reportes asignados
    public virtual ICollection<ReporteIncidencia> Reportes { get; set; } = new List<ReporteIncidencia>();
}
```

### Esquema de Base de Datos
```sql
CREATE TABLE region (
    id SERIAL PRIMARY KEY,
    nombre VARCHAR(100) NOT NULL,
    zona_id INTEGER NOT NULL REFERENCES zona(id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    usuario_id UUID NOT NULL,
    activo BOOLEAN NOT NULL DEFAULT true
);

CREATE INDEX idx_region_zona ON region(zona_id);
CREATE INDEX idx_region_activo ON region(activo);
```

---

## 3. Inventario de Archivos Afectados

### 3.1 Capa Core (Entidades e Interfaces)

| Archivo | Acción | Descripción |
|---------|--------|-------------|
| `src/Ceiba.Core/Entities/Region.cs` | **CREAR** | Nueva entidad Región |
| `src/Ceiba.Core/Entities/Zona.cs` | MODIFICAR | Cambiar navegación `Sectores` → `Regiones` |
| `src/Ceiba.Core/Entities/Sector.cs` | MODIFICAR | Cambiar `ZonaId` → `RegionId`, navegación a Región |
| `src/Ceiba.Core/Entities/ReporteIncidencia.cs` | MODIFICAR | Agregar `RegionId`, navegación a Región, validación |
| `src/Ceiba.Core/Interfaces/ICatalogService.cs` | MODIFICAR | Agregar `GetRegionesByZonaAsync()`, modificar `ValidateHierarchyAsync()` |
| `src/Ceiba.Core/Interfaces/ICatalogAdminService.cs` | MODIFICAR | Agregar métodos CRUD para Región |

### 3.2 Capa Infrastructure (Data Access)

| Archivo | Acción | Descripción |
|---------|--------|-------------|
| `src/Ceiba.Infrastructure/Data/CeibaDbContext.cs` | MODIFICAR | Agregar `DbSet<Region> Regiones` |
| `src/Ceiba.Infrastructure/Data/Configurations/RegionConfiguration.cs` | **CREAR** | Configuración EF Core para Región |
| `src/Ceiba.Infrastructure/Data/Configurations/ZonaConfiguration.cs` | MODIFICAR | Cambiar relación con Región |
| `src/Ceiba.Infrastructure/Data/Configurations/SectorConfiguration.cs` | MODIFICAR | Cambiar FK a `RegionId` |
| `src/Ceiba.Infrastructure/Data/Configurations/ReporteIncidenciaConfiguration.cs` | MODIFICAR | Agregar FK a Región |
| `src/Ceiba.Infrastructure/Data/SeedDataService.cs` | MODIFICAR | Agregar datos semilla para Región |
| `src/Ceiba.Infrastructure/Migrations/` | **CREAR** | Nueva migración para agregar Región |
| `src/Ceiba.Infrastructure/Services/CatalogService.cs` | MODIFICAR | Agregar `GetRegionesByZonaAsync()` |
| `src/Ceiba.Infrastructure/Services/CachedCatalogService.cs` | MODIFICAR | Agregar cache para Regiones |
| `src/Ceiba.Infrastructure/Services/CatalogAdminService.cs` | MODIFICAR | CRUD para Región |
| `src/Ceiba.Infrastructure/Repositories/ReportRepository.cs` | MODIFICAR | Include de Región en queries |
| `src/Ceiba.Infrastructure/Caching/CacheKeys.cs` | MODIFICAR | Agregar claves para Región |
| `src/Ceiba.Infrastructure/Data/AuditSaveChangesInterceptor.cs` | MODIFICAR | Auditoría para Región |
| `src/Ceiba.Infrastructure/Data/Migrations/PerformanceIndexes.sql` | MODIFICAR | Índices para Región |

### 3.3 Capa Application (Servicios)

| Archivo | Acción | Descripción |
|---------|--------|-------------|
| `src/Ceiba.Application/Services/ReportService.cs` | MODIFICAR | Validación de jerarquía 4 niveles |
| `src/Ceiba.Application/Validators/ReportValidators.cs` | MODIFICAR | Validación de RegionId |
| `src/Ceiba.Application/Services/Export/PdfGenerator.cs` | MODIFICAR | Incluir Región en exportación |
| `src/Ceiba.Application/Services/Export/ExportService.cs` | MODIFICAR | Mapeo de Región |

### 3.4 Capa Shared (DTOs)

| Archivo | Acción | Descripción |
|---------|--------|-------------|
| `src/Ceiba.Shared/DTOs/AdminDTOs.cs` | MODIFICAR | Agregar `RegionDto`, `CreateRegionDto`, modificar `SectorDto` |
| `src/Ceiba.Shared/DTOs/ReportDTOs.cs` | MODIFICAR | Agregar `RegionId` a DTOs de reporte |
| `src/Ceiba.Shared/DTOs/Export/ReportExportDto.cs` | MODIFICAR | Incluir Región en exportación |
| `src/Ceiba.Shared/DTOs/AutomatedReportDTOs.cs` | MODIFICAR | Incluir Región en reportes automatizados |

### 3.5 Capa Web (UI Blazor)

| Archivo | Acción | Descripción |
|---------|--------|-------------|
| `src/Ceiba.Web/Components/Pages/Reports/ReportForm.razor` | MODIFICAR | Agregar dropdown cascada para Región |
| `src/Ceiba.Web/Components/Pages/Reports/ReportList.razor` | MODIFICAR | Mostrar Región en listado |
| `src/Ceiba.Web/Components/Pages/Reports/ReportListRevisor.razor` | MODIFICAR | Mostrar Región en listado revisor |
| `src/Ceiba.Web/Components/Pages/Reports/ReportView.razor` | MODIFICAR | Mostrar Región en vista detalle |
| `src/Ceiba.Web/Components/Shared/ReportFilter.razor` | MODIFICAR | Filtro por Región |
| `src/Ceiba.Web/Components/Pages/Admin/CatalogManager.razor` | MODIFICAR | CRUD de Regiones |
| `src/Ceiba.Web/Components/Pages/Supervisor/ExportPage.razor` | MODIFICAR | Incluir Región en exportación |
| `src/Ceiba.Web/Controllers/AdminController.cs` | MODIFICAR | Endpoints para Región |
| `src/Ceiba.Web/Controllers/CatalogsController.cs` | MODIFICAR | Endpoint `GetRegionesByZona` |
| `src/Ceiba.Web/Controllers/AuditController.cs` | REVISAR | Códigos de auditoría para Región |

### 3.6 Contratos API (OpenAPI)

| Archivo | Acción | Descripción |
|---------|--------|-------------|
| `specs/.../contracts/api-admin.yaml` | MODIFICAR | Agregar endpoints Región, modificar Sector |
| `specs/.../contracts/api-reports.yaml` | MODIFICAR | Agregar `regionId` a schemas |
| `specs/.../contracts/api-audit.yaml` | MODIFICAR | Código `CONFIG_REGION` |

### 3.7 Documentación y Especificaciones

| Archivo | Acción | Descripción |
|---------|--------|-------------|
| `specs/.../data-model.md` | MODIFICAR | Actualizar diagrama ER y entidades |
| `specs/.../spec.md` | MODIFICAR | Actualizar especificación funcional |
| `specs/.../plan.md` | MODIFICAR | Actualizar plan de implementación |
| `specs/.../tasks.md` | MODIFICAR | Agregar tareas de migración |
| `specs/.../quickstart.md` | MODIFICAR | Actualizar guía de desarrollo |
| `CLAUDE.md` | MODIFICAR | Actualizar documentación del proyecto |
| `MIGRATIONS.md` | MODIFICAR | Documentar migración |

### 3.8 Tests

| Archivo | Acción | Descripción |
|---------|--------|-------------|
| `tests/Ceiba.Core.Tests/RegionTests.cs` | **CREAR** | Tests unitarios para Región |
| `tests/Ceiba.Core.Tests/ReporteIncidenciaTests.cs` | MODIFICAR | Validación con Región |
| `tests/Ceiba.Infrastructure.Tests/Services/CatalogServiceTests.cs` | MODIFICAR | Tests para GetRegiones |
| `tests/Ceiba.Infrastructure.Tests/Services/CachedCatalogServiceTests.cs` | MODIFICAR | Cache de Regiones |
| `tests/Ceiba.Infrastructure.Tests/Services/CatalogAdminServiceTests.cs` | MODIFICAR | CRUD Región |
| `tests/Ceiba.Infrastructure.Tests/Repositories/ReportRepositoryTests.cs` | MODIFICAR | Include Región |
| `tests/Ceiba.Application.Tests/Validators/CreateReportDtoValidatorTests.cs` | MODIFICAR | Validación RegionId |
| `tests/Ceiba.Application.Tests/Validators/UpdateReportDtoValidatorTests.cs` | MODIFICAR | Validación RegionId |
| `tests/Ceiba.Application.Tests/ReportServiceTests.cs` | MODIFICAR | Jerarquía 4 niveles |
| `tests/Ceiba.Application.Tests/Services/Export/*.cs` | MODIFICAR | Exportación con Región |
| `tests/Ceiba.Web.Tests/Components/*.cs` | MODIFICAR | Componentes con Región |
| `tests/Ceiba.Web.Tests/Controllers/*.cs` | MODIFICAR | Endpoints de Región |
| `tests/Ceiba.Integration.Tests/*.cs` | MODIFICAR | Integración con Región |

### 3.9 Scripts

| Archivo | Acción | Descripción |
|---------|--------|-------------|
| `scripts/generate-dummy-reports.sql` | MODIFICAR | Incluir Región en datos de prueba |
| `scripts/reset-database*.ps1` | REVISAR | Verificar compatibilidad |
| `scripts/verification/*.sh` | MODIFICAR | Verificar Región en E2E |
| `scripts/migrations/validate-migration.sh` | MODIFICAR | Validar migración Región |

---

## 4. Estrategia de Migración de Datos

### Fase 1: Crear tabla Región vacía
```sql
-- Crear tabla sin datos
CREATE TABLE region (...);
```

### Fase 2: Poblar Regiones desde Sectores existentes
```sql
-- Opción A: Una Región por Zona (hereda todos los sectores)
INSERT INTO region (nombre, zona_id, created_at, usuario_id, activo)
SELECT
    'Región Principal',  -- O derivar nombre de la zona
    z.id,
    NOW(),
    z.usuario_id,
    true
FROM zona z;

-- Actualizar Sectores con la nueva Región
UPDATE sector s
SET region_id = r.id
FROM region r
WHERE r.zona_id = s.zona_id;
```

### Fase 3: Actualizar ReporteIncidencia
```sql
-- Derivar RegionId desde el Sector
UPDATE reporte_incidencia ri
SET region_id = s.region_id
FROM sector s
WHERE ri.sector_id = s.id;
```

### Fase 4: Aplicar constraints NOT NULL
```sql
ALTER TABLE sector ALTER COLUMN region_id SET NOT NULL;
ALTER TABLE reporte_incidencia ALTER COLUMN region_id SET NOT NULL;
```

---

## 5. Plan de Implementación (Orden de Ejecución)

### Fase 1: Core Layer
1. Crear entidad `Region.cs`
2. Modificar `Zona.cs` - cambiar navegación
3. Modificar `Sector.cs` - cambiar FK
4. Modificar `ReporteIncidencia.cs` - agregar RegionId
5. Actualizar interfaces `ICatalogService`, `ICatalogAdminService`
6. Crear tests unitarios

### Fase 2: Infrastructure Layer
1. Agregar `DbSet<Region>` en DbContext
2. Crear `RegionConfiguration.cs`
3. Modificar configuraciones existentes
4. Crear migración EF Core
5. Actualizar SeedDataService
6. Modificar servicios (CatalogService, CachedCatalogService, etc.)
7. Actualizar repositorios
8. Actualizar CacheKeys
9. Crear tests de integración

### Fase 3: Shared Layer
1. Crear `RegionDto`, `CreateRegionDto`
2. Modificar `SectorDto` (ZonaId → RegionId)
3. Modificar DTOs de reporte
4. Agregar código de auditoría `CONFIG_REGION`

### Fase 4: Application Layer
1. Modificar validadores
2. Actualizar ReportService
3. Actualizar servicios de exportación
4. Crear tests de servicio

### Fase 5: Web Layer
1. Modificar formulario de reporte (cascada 4 niveles)
2. Actualizar listados y vistas
3. Modificar CatalogManager
4. Actualizar controladores
5. Crear tests de componentes

### Fase 6: Contratos y Documentación
1. Actualizar OpenAPI specs
2. Actualizar data-model.md
3. Actualizar CLAUDE.md
4. Documentar migración

### Fase 7: Migración de Datos
1. Ejecutar scripts de migración
2. Validar integridad de datos
3. Tests E2E completos

---

## 6. Cambios en la UI (Cascada de Dropdowns)

### Flujo Actual
```
[Zona ▼] → [Sector ▼] → [Cuadrante ▼]
```

### Flujo Nuevo
```
[Zona ▼] → [Región ▼] → [Sector ▼] → [Cuadrante ▼]
```

### Comportamiento
1. Usuario selecciona **Zona** → Se cargan Regiones de esa Zona
2. Usuario selecciona **Región** → Se cargan Sectores de esa Región
3. Usuario selecciona **Sector** → Se cargan Cuadrantes de ese Sector
4. Usuario selecciona **Cuadrante**

### Validación Backend
```csharp
Task<bool> ValidateHierarchyAsync(int zonaId, int regionId, int sectorId, int cuadranteId);
```

---

## 7. Impacto en Auditoría

### Nuevo Código de Auditoría
```csharp
public const string CONFIG_REGION = "CONFIG_REGION";
```

### Descripción
```csharp
CONFIG_REGION => "Configuración de región modificada"
```

---

## 8. Consideraciones de Compatibilidad

### Breaking Changes
- API: Nuevos campos requeridos (`regionId`)
- UI: Nuevo dropdown en formulario
- Reportes existentes: Requieren migración de datos

### Estrategia de Rollback
1. Script SQL de rollback incluido en migración
2. Backup automático antes de migración
3. Feature flag para desactivar validación temporal

---

## 9. Riesgos y Mitigaciones

| Riesgo | Probabilidad | Impacto | Mitigación |
|--------|--------------|---------|------------|
| Datos huérfanos post-migración | Media | Alto | Validación pre/post migración |
| Performance en cascada 4 niveles | Baja | Medio | Índices optimizados, cache |
| UI confusa con 4 dropdowns | Media | Medio | UX review, labels claros |
| Tests fallidos masivamente | Alta | Medio | Actualizar tests en paralelo |

---

## 10. Checklist de Validación

### Pre-Implementación
- [ ] Aprobación del plan por stakeholders
- [ ] Backup de base de datos
- [ ] Rama de feature creada

### Post-Implementación
- [ ] Migración ejecutada sin errores
- [ ] Todos los tests pasan (unit, integration, E2E)
- [ ] Reportes existentes tienen `region_id` válido
- [ ] UI funciona correctamente en cascada
- [ ] Exportación PDF/JSON incluye Región
- [ ] Auditoría registra cambios en Región
- [ ] Documentación actualizada

---

## 11. Archivos Nuevos a Crear

1. `src/Ceiba.Core/Entities/Region.cs`
2. `src/Ceiba.Infrastructure/Data/Configurations/RegionConfiguration.cs`
3. `src/Ceiba.Infrastructure/Migrations/YYYYMMDD_AddRegionToHierarchy.cs`
4. `tests/Ceiba.Core.Tests/RegionTests.cs`
5. `tests/Ceiba.Infrastructure.Tests/Services/RegionServiceTests.cs`

---

## 12. Estimación de Cambios por Capa

| Capa | Archivos Nuevos | Archivos Modificados | Complejidad |
|------|-----------------|---------------------|-------------|
| Core | 1 | 5 | Media |
| Infrastructure | 2 | 14 | Alta |
| Application | 0 | 4 | Media |
| Shared | 0 | 4 | Baja |
| Web | 0 | 10 | Alta |
| Tests | 2 | 25+ | Alta |
| Docs/Specs | 0 | 8 | Baja |
| **Total** | **5** | **70+** | **Alta** |

---

## Aprobación

| Rol | Nombre | Fecha | Firma |
|-----|--------|-------|-------|
| Tech Lead | | | |
| Product Owner | | | |
| DBA | | | |

---

*Documento generado automáticamente. Revisar antes de implementar.*
