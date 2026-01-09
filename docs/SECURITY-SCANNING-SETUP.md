# Security Scanning Setup

Documentación de la configuración de escaneos de seguridad en GitHub Actions.

**Fecha:** 2026-01-09
**Archivo:** `.github/workflows/security-scan.yml`

---

## Escaneos habilitados

| Escaneo | Estado | Descripción | Requisitos |
|---------|--------|-------------|------------|
| **CodeQL** | Habilitado | Análisis estático de código C# | Gratis para repos públicos |
| **Snyk** | Habilitado | Escaneo de vulnerabilidades en dependencias | Requiere `SNYK_TOKEN` secret |
| **OWASP ZAP** | Habilitado | Escaneo dinámico de seguridad web | Requiere `ENABLE_OWASP_ZAP=true` variable |
| **Dependency Review** | Habilitado | Revisión de dependencias en PRs | Automático en PRs |

---

## CodeQL Security Analysis

Análisis estático de código usando GitHub CodeQL.

### Configuración

```yaml
- uses: github/codeql-action/init@v4
  with:
    languages: csharp
    queries: security-extended

- uses: github/codeql-action/analyze@v4
  with:
    category: "/language:csharp"
```

### Cuándo se ejecuta

- Push a `main` o `develop`
- Pull requests a `main` o `develop`
- Semanalmente (domingos 2 AM)
- Manualmente via `workflow_dispatch`

---

## Snyk Dependency Scan

Escaneo de vulnerabilidades en dependencias NuGet.

### Configuración

```yaml
- uses: snyk/actions/setup@master

- run: snyk test --file=Ceiba.sln --severity-threshold=high --sarif-file-output=snyk.sarif
  env:
    SNYK_TOKEN: ${{ secrets.SNYK_TOKEN }}

- uses: github/codeql-action/upload-sarif@v4
  with:
    sarif_file: snyk.sarif
    category: snyk-dependency-scan
```

### Requisitos

1. Crear cuenta gratuita en [snyk.io](https://snyk.io)
2. Obtener API token desde Account Settings
3. Agregar `SNYK_TOKEN` como secret en GitHub

### Proyectos escaneados

El escaneo cubre todos los proyectos en `Ceiba.sln`:

- Ceiba.Core
- Ceiba.Application
- Ceiba.Infrastructure
- Ceiba.Shared
- Ceiba.Web
- Ceiba.AppHost
- Ceiba.ServiceDefaults
- Tests (Core, Application, Infrastructure, Web, Integration)

---

## OWASP ZAP Dynamic Scan

Escaneo dinámico de seguridad web ejecutando la aplicación real.

### Configuración

```yaml
owasp-zap-scan:
  if: (github.event_name == 'schedule' ||
       github.event_name == 'workflow_dispatch' ||
       github.event_name == 'release') &&
      vars.ENABLE_OWASP_ZAP == 'true'
```

### Cuándo se ejecuta

| Evento | Descripción |
|--------|-------------|
| `schedule` | Domingos 2 AM (semanal) |
| `workflow_dispatch` | Trigger manual desde GitHub Actions |
| `release: published` | Automáticamente al publicar un release |

### Requisitos

1. Crear variable de repositorio `ENABLE_OWASP_ZAP` con valor `true`
   - Settings → Secrets and variables → Actions → Variables
2. La aplicación debe tener endpoint `/health` que retorne "Healthy"

### Proceso de ejecución

1. Levanta PostgreSQL como servicio
2. Aplica migraciones de base de datos
3. Seed de datos de prueba (roles y usuario admin)
4. Inicia la aplicación en puerto 5000
5. Ejecuta OWASP ZAP Baseline Scan
6. Genera reporte HTML (disponible como artifact)

### Datos de prueba para CI

El workflow crea automáticamente:

```sql
-- Roles
INSERT INTO "ROL" ("Id", "Name", "NormalizedName", "ConcurrencyStamp")
VALUES
  ('11111111-...', 'CREADOR', 'CREADOR', 'stamp1'),
  ('22222222-...', 'REVISOR', 'REVISOR', 'stamp2'),
  ('33333333-...', 'ADMIN', 'ADMIN', 'stamp3');

-- Usuario admin de prueba
INSERT INTO "USUARIO" (...)
VALUES ('aaaaaaaa-...', 'admin@ceiba.local', ...);
```

---

## Dependency Review

Revisa cambios de dependencias en Pull Requests.

### Configuración

```yaml
dependency-review:
  if: github.event_name == 'pull_request'

  steps:
    - uses: actions/dependency-review-action@v4
      with:
        fail-on-severity: high
        deny-licenses: GPL-2.0, GPL-3.0
```

### Qué detecta

- Vulnerabilidades con severidad `high` o `critical`
- Licencias incompatibles (GPL-2.0, GPL-3.0)

### Cuándo se ejecuta

Solo en Pull Requests que modifiquen archivos de dependencias.

---

## Triggers del workflow

```yaml
on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main, develop ]
  release:
    types: [ published ]
  schedule:
    - cron: '0 2 * * 0'  # Domingos 2 AM
  workflow_dispatch:
```

---

## Resumen de seguridad (Security Summary)

Al final de cada ejecución, se genera un resumen:

```markdown
# Security Scan Results

| Scan | Status |
|------|--------|
| Dependency Review | success/skipped |
| CodeQL | success/failure |
| Snyk | success/skipped |
| OWASP ZAP | success/skipped |
```

---

## Troubleshooting

### Snyk no encuentra archivos

**Solución:** Usar `--file=Ceiba.sln` para especificar el archivo de solución.

### OWASP ZAP falla al iniciar la app

**Posibles causas:**
1. Puerto incorrecto → Usar `--no-launch-profile`
2. Falta seed de datos → Verificar que el SQL de seed se ejecute
3. Migraciones fallan → Verificar `dotnet tool restore`

### CodeQL muestra warning de deprecación

**Solución:** Actualizar de `@v3` a `@v4` en todas las referencias de `codeql-action`.

### SARIF upload falla con "multiple runs"

**Solución:** Agregar `category` único al paso de upload-sarif.

---

## Historial de cambios

| Fecha | Cambio |
|-------|--------|
| 2026-01-09 | Configuración inicial completa |
| 2026-01-09 | Migración de Snyk a setup action |
| 2026-01-09 | Actualización CodeQL v3 → v4 |
| 2026-01-09 | Agregado trigger en release para OWASP ZAP |
| 2026-01-09 | Fix de seed SQL para OWASP ZAP |
