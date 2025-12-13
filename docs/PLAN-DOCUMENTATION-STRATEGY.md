# Plan: Estrategia de Documentación para GitHub Wiki

> **Estado**: Planificación
> **Prioridad**: Alta
> **Fecha**: 2025-12-12

## Resumen Ejecutivo

Este plan establece una estrategia de documentación completa para el sistema Ceiba, diseñada específicamente para:

1. **Almacenarse en el repositorio** - Versionada junto con el código
2. **Publicarse en GitHub Wiki** - Navegable y accesible para usuarios
3. **Tres audiencias** - Usuarios finales, programadores e implementadores

## Arquitectura de Documentación

### Principios Clave

```
┌─────────────────────────────────────────────────────────────┐
│                    REPOSITORIO PRINCIPAL                     │
│                                                              │
│  docs/wiki/                                                  │
│  ├── Home.md              ← Página principal del Wiki        │
│  ├── _Sidebar.md          ← Navegación lateral               │
│  ├── _Footer.md           ← Pie de página común              │
│  ├── *.md                 ← Páginas de documentación         │
│  └── images/              ← Capturas de pantalla             │
│                                                              │
│  ══════════════════════════════════════════════════════════  │
│                           ↓ sync ↓                           │
│  ══════════════════════════════════════════════════════════  │
│                                                              │
│                    GITHUB WIKI (automático)                  │
│                    https://github.com/org/repo/wiki          │
└─────────────────────────────────────────────────────────────┘
```

### ¿Por qué esta arquitectura?

| Requisito | Solución |
|-----------|----------|
| Versionado con git | Docs en `docs/wiki/` del repo principal |
| Navegable en GitHub Wiki | Sincronización automática o manual |
| Capturas de pantalla | Carpeta `images/` con rutas relativas |
| Multi-audiencia | Prefijos en nombres de archivo |

## Estructura de Archivos para GitHub Wiki

### Convenciones de Nombrado

GitHub Wiki tiene requisitos específicos:

| Regla | Ejemplo |
|-------|---------|
| Sin subdirectorios | `Usuario-Creador-Crear-Reporte.md` (no `users/creador/crear-reporte.md`) |
| Guiones para espacios | `Guia-de-Instalacion.md` |
| PascalCase o kebab-case | Ambos funcionan |
| Home.md obligatorio | Página de inicio |
| _Sidebar.md | Navegación lateral (opcional pero recomendado) |
| _Footer.md | Pie de página común (opcional) |

### Estructura Propuesta

```
docs/wiki/
├── Home.md                                    # Página principal
├── _Sidebar.md                                # Navegación lateral
├── _Footer.md                                 # Pie de página
│
├── images/                                    # Todas las capturas
│   ├── login-pantalla-principal.png
│   ├── creador-formulario-reporte.png
│   ├── revisor-lista-reportes.png
│   ├── admin-gestion-usuarios.png
│   └── ...
│
│── # ═══════════════════════════════════════
│── # DOCUMENTACIÓN PARA USUARIOS
│── # ═══════════════════════════════════════
│
├── Usuario-Primeros-Pasos.md                  # Común a todos los roles
│
├── Usuario-Creador-Introduccion.md            # Rol CREADOR
├── Usuario-Creador-Crear-Reporte.md
├── Usuario-Creador-Editar-Reporte.md
├── Usuario-Creador-Enviar-Reporte.md
├── Usuario-Creador-Historial.md
├── Usuario-Creador-FAQ.md
│
├── Usuario-Revisor-Introduccion.md            # Rol REVISOR
├── Usuario-Revisor-Ver-Reportes.md
├── Usuario-Revisor-Editar-Reportes.md
├── Usuario-Revisor-Exportar-PDF.md
├── Usuario-Revisor-Exportar-JSON.md
├── Usuario-Revisor-Exportacion-Masiva.md
├── Usuario-Revisor-Reportes-Automatizados.md
├── Usuario-Revisor-FAQ.md
│
├── Usuario-Admin-Introduccion.md              # Rol ADMIN
├── Usuario-Admin-Gestion-Usuarios.md
├── Usuario-Admin-Asignar-Roles.md
├── Usuario-Admin-Catalogos-Geograficos.md
├── Usuario-Admin-Catalogos-Sugerencias.md
├── Usuario-Admin-Auditoria.md
├── Usuario-Admin-Configuracion.md
├── Usuario-Admin-FAQ.md
│
│── # ═══════════════════════════════════════
│── # DOCUMENTACIÓN PARA PROGRAMADORES
│── # ═══════════════════════════════════════
│
├── Dev-Inicio-Rapido.md                       # Getting started
├── Dev-Arquitectura.md                        # Arquitectura del sistema
├── Dev-Estandares-Codigo.md                   # Coding standards
├── Dev-Testing-TDD.md                         # Guía de testing
├── Dev-Base-de-Datos.md                       # Esquema y migraciones
├── Dev-API-Referencia.md                      # APIs internas
│
├── Dev-Modulo-Autenticacion.md                # Módulos
├── Dev-Modulo-Reportes.md
├── Dev-Modulo-Exportacion.md
├── Dev-Modulo-Auditoria.md
├── Dev-Modulo-Catalogos.md
├── Dev-Modulo-Reportes-Automatizados.md
│
├── Dev-Guia-Agregar-Campo.md                  # Guías prácticas
├── Dev-Guia-Agregar-Rol.md
├── Dev-Guia-Migraciones.md
├── Dev-Guia-Componentes-Blazor.md
├── Dev-Guia-Debugging.md
│
├── ADR-001-Blazor-Server.md                   # Architecture Decision Records
├── ADR-002-PostgreSQL.md
├── ADR-003-QuestPDF.md
├── ADR-Template.md
│
│── # ═══════════════════════════════════════
│── # DOCUMENTACIÓN PARA IMPLEMENTADORES
│── # ═══════════════════════════════════════
│
├── Ops-Requisitos-Sistema.md                  # Requisitos
├── Ops-Instalacion-Linux.md                   # Instalación
├── Ops-Instalacion-Windows.md
├── Ops-Instalacion-Docker.md
│
├── Ops-Config-Variables-Entorno.md            # Configuración
├── Ops-Config-Base-de-Datos.md
├── Ops-Config-Email-SMTP.md
├── Ops-Config-IA.md
├── Ops-Config-SSL-HTTPS.md
├── Ops-Config-Reverse-Proxy.md
│
├── Ops-Mant-Backup-Restore.md                 # Mantenimiento
├── Ops-Mant-Monitoreo.md
├── Ops-Mant-Logs.md
├── Ops-Mant-Actualizaciones.md
├── Ops-Mant-Troubleshooting.md
│
├── Ops-Seguridad-Hardening.md                 # Seguridad
├── Ops-Seguridad-Firewall.md
├── Ops-Seguridad-Auditoria.md
└── Ops-Seguridad-Incidentes.md
```

## Archivos Especiales de GitHub Wiki

### Home.md (Página Principal)

```markdown
# Ceiba - Sistema de Reportes de Incidencias

Bienvenido a la documentación oficial del sistema Ceiba.

## Selecciona tu perfil

### Para Usuarios del Sistema

Si eres un usuario del sistema (oficial de policía, supervisor o administrador):

- [[Usuario-Primeros-Pasos|Primeros Pasos]]
- [[Usuario-Creador-Introduccion|Guía para Creadores]]
- [[Usuario-Revisor-Introduccion|Guía para Revisores]]
- [[Usuario-Admin-Introduccion|Guía para Administradores]]

### Para Desarrolladores

Si vas a contribuir al código o extender el sistema:

- [[Dev-Inicio-Rapido|Inicio Rápido]]
- [[Dev-Arquitectura|Arquitectura del Sistema]]
- [[Dev-Estandares-Codigo|Estándares de Código]]

### Para Implementadores / DevOps

Si vas a instalar o mantener el sistema:

- [[Ops-Requisitos-Sistema|Requisitos del Sistema]]
- [[Ops-Instalacion-Docker|Instalación con Docker]]
- [[Ops-Instalacion-Linux|Instalación en Linux]]
- [[Ops-Instalacion-Windows|Instalación en Windows]]

---

**Versión**: 1.0
**Última actualización**: 2025-12-12
```

### _Sidebar.md (Navegación Lateral)

```markdown
**[[Home|Inicio]]**

---

**Usuarios**

* [[Usuario-Primeros-Pasos|Primeros Pasos]]

_Creador_
* [[Usuario-Creador-Introduccion|Introducción]]
* [[Usuario-Creador-Crear-Reporte|Crear Reporte]]
* [[Usuario-Creador-Editar-Reporte|Editar Reporte]]
* [[Usuario-Creador-Enviar-Reporte|Enviar Reporte]]
* [[Usuario-Creador-Historial|Historial]]
* [[Usuario-Creador-FAQ|FAQ]]

_Revisor_
* [[Usuario-Revisor-Introduccion|Introducción]]
* [[Usuario-Revisor-Ver-Reportes|Ver Reportes]]
* [[Usuario-Revisor-Exportar-PDF|Exportar PDF]]
* [[Usuario-Revisor-FAQ|FAQ]]

_Administrador_
* [[Usuario-Admin-Introduccion|Introducción]]
* [[Usuario-Admin-Gestion-Usuarios|Gestión de Usuarios]]
* [[Usuario-Admin-FAQ|FAQ]]

---

**Desarrolladores**

* [[Dev-Inicio-Rapido|Inicio Rápido]]
* [[Dev-Arquitectura|Arquitectura]]
* [[Dev-Testing-TDD|Testing (TDD)]]
* [[Dev-Base-de-Datos|Base de Datos]]

---

**Operaciones**

* [[Ops-Requisitos-Sistema|Requisitos]]
* [[Ops-Instalacion-Docker|Docker]]
* [[Ops-Instalacion-Linux|Linux]]
* [[Ops-Instalacion-Windows|Windows]]
* [[Ops-Mant-Backup-Restore|Backup/Restore]]
```

### _Footer.md (Pie de Página)

```markdown
---
[Repositorio](https://github.com/org/ceiba) |
[Reportar un problema](https://github.com/org/ceiba/issues) |
[Licencia](https://github.com/org/ceiba/blob/main/LICENSE)
```

## Convenciones para GitHub Wiki

### Enlaces Internos

GitHub Wiki usa una sintaxis especial para enlaces:

```markdown
# Enlace simple (título = nombre de archivo sin extensión)
[[Nombre-Del-Archivo]]

# Enlace con texto personalizado
[[Nombre-Del-Archivo|Texto que se muestra]]

# Ejemplos:
[[Usuario-Creador-FAQ]]
[[Usuario-Creador-FAQ|Preguntas Frecuentes del Creador]]
[[Home|Volver al inicio]]
```

**NO usar** enlaces relativos estándar como `[texto](archivo.md)` para páginas del Wiki.

### Imágenes

Las imágenes deben almacenarse en `docs/wiki/images/` y referenciarse así:

```markdown
# Sintaxis para imágenes en GitHub Wiki
![Descripción de la imagen](images/nombre-imagen.png)

# Ejemplo:
![Pantalla de login](images/login-pantalla-principal.png)

# Con tamaño específico (HTML permitido)
<img src="images/login-pantalla-principal.png" width="600" alt="Pantalla de login">
```

### Convenciones de Nombrado de Imágenes

| Patrón | Ejemplo |
|--------|---------|
| `{rol}-{pantalla}-{elemento}.png` | `creador-reporte-formulario.png` |
| `{modulo}-{accion}-{detalle}.png` | `admin-usuarios-crear-modal.png` |
| `{seccion}-{paso}-{descripcion}.png` | `instalacion-paso3-docker-compose.png` |

### Tablas

```markdown
| Columna 1 | Columna 2 | Columna 3 |
|-----------|-----------|-----------|
| Valor 1   | Valor 2   | Valor 3   |
```

### Bloques de Código

````markdown
```bash
# Comandos de terminal
docker compose up -d
```

```csharp
// Código C#
public class Ejemplo { }
```

```json
{
  "configuracion": "valor"
}
```
````

### Alertas y Notas

```markdown
> **Nota:** Información importante que el usuario debe conocer.

> **Advertencia:** Algo que podría causar problemas si se ignora.

> **Peligro:** Acción que podría causar pérdida de datos o problemas graves.
```

## Capturas de Pantalla Requeridas

### Instrucciones para el Usuario

Cuando llegue el momento de crear las capturas, te indicaré exactamente qué capturar. Aquí está la lista completa:

#### Capturas Generales (Comunes)

| ID | Nombre de Archivo | Descripción | Pantalla a Capturar |
|----|-------------------|-------------|---------------------|
| G01 | `login-pantalla-principal.png` | Pantalla de login | Página de inicio de sesión completa |
| G02 | `login-error-credenciales.png` | Error de credenciales | Login mostrando mensaje de error |

#### Capturas para Rol CREADOR

| ID | Nombre de Archivo | Descripción | Pantalla a Capturar |
|----|-------------------|-------------|---------------------|
| C01 | `creador-dashboard.png` | Dashboard principal | Vista inicial después de login |
| C02 | `creador-reporte-nuevo-vacio.png` | Formulario vacío | Formulario de nuevo reporte sin datos |
| C03 | `creador-reporte-nuevo-llenado.png` | Formulario con datos | Formulario con datos de ejemplo |
| C04 | `creador-reporte-guardado.png` | Confirmación guardado | Mensaje de éxito al guardar borrador |
| C05 | `creador-lista-reportes.png` | Lista de reportes | Vista de reportes propios |
| C06 | `creador-reporte-detalle.png` | Detalle de reporte | Vista de un reporte individual |
| C07 | `creador-reporte-editar.png` | Edición de reporte | Formulario en modo edición |
| C08 | `creador-reporte-enviar-confirmacion.png` | Confirmar envío | Diálogo de confirmación de entrega |
| C09 | `creador-reporte-enviado.png` | Reporte enviado | Reporte en estado "Entregado" |
| C10 | `creador-filtros-historial.png` | Filtros de búsqueda | Panel de filtros expandido |

#### Capturas para Rol REVISOR

| ID | Nombre de Archivo | Descripción | Pantalla a Capturar |
|----|-------------------|-------------|---------------------|
| R01 | `revisor-dashboard.png` | Dashboard principal | Vista inicial del revisor |
| R02 | `revisor-lista-todos-reportes.png` | Lista completa | Todos los reportes del sistema |
| R03 | `revisor-filtros-avanzados.png` | Filtros | Panel de filtros con opciones |
| R04 | `revisor-reporte-detalle.png` | Detalle reporte | Vista de reporte de otro usuario |
| R05 | `revisor-reporte-editar.png` | Editar reporte | Editando reporte de otro usuario |
| R06 | `revisor-exportar-menu.png` | Menú exportar | Opciones de exportación |
| R07 | `revisor-exportar-pdf-preview.png` | Preview PDF | Vista previa del PDF |
| R08 | `revisor-exportar-masivo.png` | Exportación masiva | Selección múltiple + exportar |
| R09 | `revisor-reportes-auto-config.png` | Config. automáticos | Panel de configuración IA |
| R10 | `revisor-reportes-auto-lista.png` | Lista automáticos | Reportes generados por IA |

#### Capturas para Rol ADMIN

| ID | Nombre de Archivo | Descripción | Pantalla a Capturar |
|----|-------------------|-------------|---------------------|
| A01 | `admin-dashboard.png` | Dashboard principal | Vista inicial del admin |
| A02 | `admin-usuarios-lista.png` | Lista usuarios | Tabla de usuarios del sistema |
| A03 | `admin-usuarios-crear.png` | Crear usuario | Formulario de nuevo usuario |
| A04 | `admin-usuarios-editar.png` | Editar usuario | Formulario de edición |
| A05 | `admin-usuarios-roles.png` | Asignar roles | Panel de asignación de roles |
| A06 | `admin-catalogos-zonas.png` | Catálogo zonas | Lista de zonas |
| A07 | `admin-catalogos-sectores.png` | Catálogo sectores | Lista de sectores |
| A08 | `admin-catalogos-cuadrantes.png` | Catálogo cuadrantes | Lista de cuadrantes |
| A09 | `admin-sugerencias-lista.png` | Sugerencias | Catálogos de sugerencias |
| A10 | `admin-auditoria-lista.png` | Auditoría | Logs de auditoría |
| A11 | `admin-auditoria-filtros.png` | Filtros auditoría | Filtros de búsqueda |
| A12 | `admin-auditoria-detalle.png` | Detalle auditoría | Detalle de un registro |

#### Capturas para Operaciones

| ID | Nombre de Archivo | Descripción | Pantalla a Capturar |
|----|-------------------|-------------|---------------------|
| O01 | `ops-docker-compose-up.png` | Docker running | Terminal con containers corriendo |
| O02 | `ops-health-check.png` | Health check | Respuesta del endpoint /health |
| O03 | `ops-logs-ejemplo.png` | Logs aplicación | Ejemplo de logs en terminal |

## Sincronización con GitHub Wiki

### Opción 1: Sincronización Manual (Recomendada inicialmente)

El Wiki de GitHub es un repositorio git separado. Para sincronizar:

```bash
# 1. Clonar el wiki (primera vez)
git clone https://github.com/ORG/REPO.wiki.git ceiba-wiki

# 2. Copiar archivos desde docs/wiki/
cp -r docs/wiki/* ceiba-wiki/

# 3. Commit y push al wiki
cd ceiba-wiki
git add .
git commit -m "Actualizar documentación"
git push
```

### Opción 2: Script de Sincronización

**Linux** (`scripts/sync-wiki.sh`):
```bash
#!/bin/bash
set -e

REPO_URL="https://github.com/ORG/REPO.wiki.git"
WIKI_DIR=".wiki-sync"
DOCS_DIR="docs/wiki"

echo "=== Sincronizando documentación con GitHub Wiki ==="

# Clonar o actualizar wiki
if [ -d "$WIKI_DIR" ]; then
    cd "$WIKI_DIR"
    git pull
    cd ..
else
    git clone "$REPO_URL" "$WIKI_DIR"
fi

# Copiar archivos
rsync -av --delete "$DOCS_DIR/" "$WIKI_DIR/"

# Commit y push
cd "$WIKI_DIR"
git add .
if git diff --staged --quiet; then
    echo "Sin cambios para sincronizar"
else
    git commit -m "Sync: $(date +%Y-%m-%d)"
    git push
    echo "Wiki actualizado exitosamente"
fi
```

**Windows** (`scripts/sync-wiki.ps1`):
```powershell
$ErrorActionPreference = "Stop"

$RepoUrl = "https://github.com/ORG/REPO.wiki.git"
$WikiDir = ".wiki-sync"
$DocsDir = "docs/wiki"

Write-Host "=== Sincronizando documentación con GitHub Wiki ===" -ForegroundColor Cyan

# Clonar o actualizar wiki
if (Test-Path $WikiDir) {
    Push-Location $WikiDir
    git pull
    Pop-Location
} else {
    git clone $RepoUrl $WikiDir
}

# Copiar archivos
Copy-Item -Path "$DocsDir\*" -Destination $WikiDir -Recurse -Force

# Commit y push
Push-Location $WikiDir
git add .
$status = git status --porcelain
if ($status) {
    git commit -m "Sync: $(Get-Date -Format 'yyyy-MM-dd')"
    git push
    Write-Host "Wiki actualizado exitosamente" -ForegroundColor Green
} else {
    Write-Host "Sin cambios para sincronizar" -ForegroundColor Yellow
}
Pop-Location
```

### Opción 3: GitHub Action (Automatizado)

`.github/workflows/sync-wiki.yml`:
```yaml
name: Sync Wiki

on:
  push:
    branches: [main]
    paths:
      - 'docs/wiki/**'

jobs:
  sync:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Sync to Wiki
        uses: Andrew-Chen-Wang/github-wiki-action@v4
        with:
          path: docs/wiki
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

## Plan de Implementación

### Fase 1: Estructura Base

**Tareas**:
- [ ] Crear carpeta `docs/wiki/`
- [ ] Crear `Home.md`
- [ ] Crear `_Sidebar.md`
- [ ] Crear `_Footer.md`
- [ ] Crear carpeta `images/`
- [ ] Crear archivos placeholder para cada documento
- [ ] Habilitar Wiki en el repositorio de GitHub
- [ ] Sincronización inicial

**Entregables**:
- Estructura completa de archivos
- Wiki navegable (aunque con contenido placeholder)

### Fase 2: Documentación de Usuarios

**Tareas**:
- [ ] Escribir `Usuario-Primeros-Pasos.md`
- [ ] Escribir documentos de rol CREADOR (6 archivos)
- [ ] Escribir documentos de rol REVISOR (8 archivos)
- [ ] Escribir documentos de rol ADMIN (8 archivos)
- [ ] **Capturar screenshots** (lista proporcionada arriba)
- [ ] Actualizar `_Sidebar.md` con enlaces

**Entregables**:
- 23 documentos de usuario completos
- ~35 capturas de pantalla
- Navegación funcional

### Fase 3: Documentación de Programadores

**Tareas**:
- [ ] Escribir documentos principales (6 archivos)
- [ ] Escribir documentos de módulos (6 archivos)
- [ ] Escribir guías prácticas (5 archivos)
- [ ] Crear ADRs (3+ archivos)
- [ ] Actualizar `_Sidebar.md`

**Entregables**:
- 20+ documentos técnicos
- Diagramas de arquitectura

### Fase 4: Documentación de Operaciones

**Tareas**:
- [ ] Escribir guías de instalación (4 archivos)
- [ ] Escribir guías de configuración (6 archivos)
- [ ] Escribir guías de mantenimiento (5 archivos)
- [ ] Escribir guías de seguridad (4 archivos)
- [ ] **Capturar screenshots** de terminal/comandos
- [ ] Actualizar `_Sidebar.md`

**Entregables**:
- 19 documentos de operaciones
- Scripts de sincronización

### Fase 5: Revisión y Publicación

**Tareas**:
- [ ] Revisar ortografía y gramática
- [ ] Verificar todos los enlaces internos
- [ ] Verificar todas las imágenes cargan
- [ ] Configurar GitHub Action para sync automático
- [ ] Probar navegación completa en Wiki

**Entregables**:
- Wiki 100% funcional y navegable
- Sincronización automática activa

## Resumen de Archivos

| Categoría | Cantidad | Prefijo |
|-----------|----------|---------|
| Usuarios - General | 1 | `Usuario-` |
| Usuarios - Creador | 6 | `Usuario-Creador-` |
| Usuarios - Revisor | 8 | `Usuario-Revisor-` |
| Usuarios - Admin | 8 | `Usuario-Admin-` |
| Desarrolladores | 17 | `Dev-` |
| ADRs | 4 | `ADR-` |
| Operaciones | 19 | `Ops-` |
| Especiales | 3 | `Home`, `_Sidebar`, `_Footer` |
| **Total** | **66** | - |

## Criterios de Aceptación

1. [ ] Todos los documentos están en `docs/wiki/`
2. [ ] Todos los enlaces `[[Pagina]]` funcionan en GitHub Wiki
3. [ ] Todas las imágenes se visualizan correctamente
4. [ ] La navegación `_Sidebar.md` permite acceder a todas las secciones
5. [ ] Los documentos versionados en git reflejan el Wiki publicado
6. [ ] Existe script de sincronización para Linux (.sh) y Windows (.ps1)

---

**Próximos pasos**: Crear la estructura base de `docs/wiki/` con los archivos especiales (`Home.md`, `_Sidebar.md`, `_Footer.md`) y archivos placeholder.
