# Plan: Scripts Cross-Platform (Linux + Windows)

> **Estado**: Planificación
> **Prioridad**: Media
> **Fecha**: 2025-12-12

## Resumen Ejecutivo

Este plan documenta el estado actual de los scripts del proyecto y define el trabajo necesario para garantizar que todos los scripts críticos funcionen tanto en Linux (Bash) como en Windows (PowerShell).

## Estado Actual de Scripts

### Scripts con Ambas Versiones (Completos)

| Script | Bash (.sh) | PowerShell (.ps1) |
|--------|------------|-------------------|
| backup-database | scripts/backup/backup-database.sh | scripts/backup/backup-database.ps1 |
| restore-database | scripts/backup/restore-database.sh | scripts/backup/restore-database.ps1 |
| check-no-raw-sql | scripts/security/check-no-raw-sql.sh | scripts/security/check-no-raw-sql.ps1 |
| fetch-issues (Sonar) | scripts/sonar/fetch-issues.sh | scripts/sonar/fetch-issues.ps1 |
| e2e-verification | scripts/verification/e2e-verification.sh | scripts/verification/e2e-verification.ps1 |
| test-coverage-report | scripts/verification/test-coverage-report.sh | scripts/verification/test-coverage-report.ps1 |

### Scripts Solo en Bash (Necesitan versión PowerShell)

| Script Bash | Propósito | Prioridad |
|-------------|-----------|-----------|
| `backup/monitor-backups.sh` | Monitoreo de backups | Media |
| `backup/scheduled-backup.sh` | Backup programado (cron) | Alta |
| `backup/verify-backup.sh` | Verificar integridad de backup | Alta |
| `migrations/backup-before-migration.sh` | Backup antes de migración | Alta |
| `migrations/validate-migration.sh` | Validar migración | Alta |
| `security/scan-logs-for-sensitive-data.sh` | Escanear logs por PII | Media |

### Scripts Solo en PowerShell (Necesitan versión Bash)

| Script PowerShell | Propósito | Prioridad |
|-------------------|-----------|-----------|
| `create-test-user.ps1` | Crear usuario de prueba | Media |
| `reset-database.ps1` | Reiniciar base de datos | Alta |
| `reset-database-with-postgres.ps1` | Reset con permisos elevados | Alta |
| `setup-database.ps1` | Configuración inicial de BD | Alta |
| `verification/validate-quickstart.ps1` | Validar setup de desarrollo | Media |

## Plan de Implementación

### Fase 1: Scripts Críticos de Base de Datos (Prioridad Alta)

Estos scripts son fundamentales para la instalación y mantenimiento.

#### 1.1 setup-database.sh (Nuevo)

**Funcionalidad**: Crear y configurar la base de datos PostgreSQL inicial.

```bash
#!/bin/bash
# scripts/setup-database.sh
set -e

echo "=== Configuración de Base de Datos Ceiba ==="

# Variables (desde .env o parámetros)
DB_HOST=${DB_HOST:-localhost}
DB_PORT=${DB_PORT:-5432}
DB_NAME=${DB_NAME:-ceiba}
DB_USER=${DB_USER:-ceiba}
DB_PASSWORD=${DB_PASSWORD:-}

if [ -z "$DB_PASSWORD" ]; then
    echo "ERROR: DB_PASSWORD no está configurada"
    echo "Uso: DB_PASSWORD=xxx ./setup-database.sh"
    exit 1
fi

# Crear usuario si no existe
sudo -u postgres psql -c "CREATE USER $DB_USER WITH PASSWORD '$DB_PASSWORD';" 2>/dev/null || true

# Crear base de datos
sudo -u postgres psql -c "CREATE DATABASE $DB_NAME OWNER $DB_USER;" 2>/dev/null || true

# Otorgar permisos
sudo -u postgres psql -c "GRANT ALL PRIVILEGES ON DATABASE $DB_NAME TO $DB_USER;"

echo "=== Base de datos configurada exitosamente ==="
```

#### 1.2 reset-database.sh (Nuevo)

**Funcionalidad**: Reiniciar la base de datos (desarrollo).

```bash
#!/bin/bash
# scripts/reset-database.sh
set -e

echo "=== Reiniciando Base de Datos Ceiba ==="
echo "ADVERTENCIA: Esto eliminará todos los datos"
read -p "¿Continuar? (y/N): " confirm
if [ "$confirm" != "y" ]; then
    echo "Cancelado"
    exit 0
fi

# Eliminar y recrear
cd "$(dirname "$0")/.."
dotnet ef database drop --force --project src/Ceiba.Infrastructure --startup-project src/Ceiba.Web
dotnet ef database update --project src/Ceiba.Infrastructure --startup-project src/Ceiba.Web

echo "=== Base de datos reiniciada ==="
```

#### 1.3 validate-migration.sh → validate-migration.ps1 (Nuevo)

**Funcionalidad**: Validar que una migración se aplicó correctamente.

```powershell
# scripts/migrations/validate-migration.ps1
param(
    [string]$MigrationName = ""
)

Write-Host "=== Validando Migración ===" -ForegroundColor Cyan

# Verificar estado de migraciones
$migrations = dotnet ef migrations list --project src/Ceiba.Infrastructure --startup-project src/Ceiba.Web

if ($MigrationName) {
    if ($migrations -match $MigrationName) {
        Write-Host "Migración '$MigrationName' encontrada" -ForegroundColor Green
    } else {
        Write-Host "ERROR: Migración '$MigrationName' no encontrada" -ForegroundColor Red
        exit 1
    }
}

# Verificar conexión
dotnet ef database update --project src/Ceiba.Infrastructure --startup-project src/Ceiba.Web 2>&1 | Out-Null
if ($LASTEXITCODE -eq 0) {
    Write-Host "Base de datos actualizada correctamente" -ForegroundColor Green
} else {
    Write-Host "ERROR al actualizar base de datos" -ForegroundColor Red
    exit 1
}

Write-Host "=== Validación completada ===" -ForegroundColor Green
```

### Fase 2: Scripts de Backup (Prioridad Alta)

#### 2.1 verify-backup.ps1 (Nuevo)

```powershell
# scripts/backup/verify-backup.ps1
param(
    [Parameter(Mandatory=$true)]
    [string]$BackupFile
)

Write-Host "=== Verificando Backup: $BackupFile ===" -ForegroundColor Cyan

if (-not (Test-Path $BackupFile)) {
    Write-Host "ERROR: Archivo no encontrado: $BackupFile" -ForegroundColor Red
    exit 1
}

# Verificar que es un archivo gzip válido
$extension = [System.IO.Path]::GetExtension($BackupFile)
if ($extension -eq ".gz") {
    try {
        # Intentar leer primeros bytes para verificar formato gzip
        $bytes = [System.IO.File]::ReadAllBytes($BackupFile)
        if ($bytes[0] -ne 0x1f -or $bytes[1] -ne 0x8b) {
            Write-Host "ERROR: No es un archivo gzip válido" -ForegroundColor Red
            exit 1
        }
        Write-Host "Formato gzip válido" -ForegroundColor Green
    } catch {
        Write-Host "ERROR al leer archivo: $_" -ForegroundColor Red
        exit 1
    }
}

# Verificar tamaño
$size = (Get-Item $BackupFile).Length
$sizeMB = [math]::Round($size / 1MB, 2)
Write-Host "Tamaño: $sizeMB MB" -ForegroundColor Cyan

if ($size -lt 1024) {
    Write-Host "ADVERTENCIA: Archivo muy pequeño, podría estar corrupto" -ForegroundColor Yellow
}

Write-Host "=== Verificación completada ===" -ForegroundColor Green
```

#### 2.2 scheduled-backup.ps1 (Nuevo)

```powershell
# scripts/backup/scheduled-backup.ps1
# Para usar con Task Scheduler de Windows

$ErrorActionPreference = "Stop"
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupDir = "$PSScriptRoot\..\..\backups"
$backupFile = "$backupDir\ceiba_backup_$timestamp.sql.gz"

# Crear directorio si no existe
if (-not (Test-Path $backupDir)) {
    New-Item -ItemType Directory -Path $backupDir | Out-Null
}

# Ejecutar backup
& "$PSScriptRoot\backup-database.ps1" -OutputFile $backupFile

# Limpiar backups antiguos (mantener últimos 30 días)
$cutoffDate = (Get-Date).AddDays(-30)
Get-ChildItem -Path $backupDir -Filter "ceiba_backup_*.sql.gz" |
    Where-Object { $_.LastWriteTime -lt $cutoffDate } |
    Remove-Item -Force

Write-Host "Backup completado: $backupFile"
```

#### 2.3 monitor-backups.ps1 (Nuevo)

```powershell
# scripts/backup/monitor-backups.ps1
param(
    [int]$MaxAgeDays = 1,
    [string]$BackupDir = "$PSScriptRoot\..\..\backups"
)

Write-Host "=== Monitor de Backups ===" -ForegroundColor Cyan

if (-not (Test-Path $BackupDir)) {
    Write-Host "ERROR: Directorio de backups no existe: $BackupDir" -ForegroundColor Red
    exit 1
}

$latestBackup = Get-ChildItem -Path $BackupDir -Filter "ceiba_backup_*.sql.gz" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if (-not $latestBackup) {
    Write-Host "ERROR: No se encontraron backups" -ForegroundColor Red
    exit 1
}

$age = (Get-Date) - $latestBackup.LastWriteTime
$ageDays = [math]::Round($age.TotalDays, 2)

Write-Host "Último backup: $($latestBackup.Name)"
Write-Host "Fecha: $($latestBackup.LastWriteTime)"
Write-Host "Antigüedad: $ageDays días"

if ($age.TotalDays -gt $MaxAgeDays) {
    Write-Host "ALERTA: Backup tiene más de $MaxAgeDays días" -ForegroundColor Red
    exit 1
} else {
    Write-Host "Estado: OK" -ForegroundColor Green
}
```

### Fase 3: Scripts de Desarrollo (Prioridad Media)

#### 3.1 create-test-user.sh (Nuevo)

```bash
#!/bin/bash
# scripts/create-test-user.sh
set -e

usage() {
    echo "Uso: $0 -e EMAIL -p PASSWORD -r ROLE"
    echo "  -e EMAIL     Correo electrónico del usuario"
    echo "  -p PASSWORD  Contraseña"
    echo "  -r ROLE      Rol (CREADOR, REVISOR, ADMIN)"
    exit 1
}

while getopts "e:p:r:" opt; do
    case $opt in
        e) EMAIL="$OPTARG" ;;
        p) PASSWORD="$OPTARG" ;;
        r) ROLE="$OPTARG" ;;
        *) usage ;;
    esac
done

if [ -z "$EMAIL" ] || [ -z "$PASSWORD" ] || [ -z "$ROLE" ]; then
    usage
fi

cd "$(dirname "$0")/.."
dotnet run --project src/Ceiba.Web -- --create-user "$EMAIL" "$PASSWORD" "$ROLE"
```

#### 3.2 validate-quickstart.sh (Nuevo)

```bash
#!/bin/bash
# scripts/verification/validate-quickstart.sh
set -e

echo "=== Validación de Quickstart ==="

# Verificar .NET SDK
echo -n "Verificando .NET SDK... "
if command -v dotnet &> /dev/null; then
    version=$(dotnet --version)
    echo "OK ($version)"
else
    echo "FALTA"
    echo "Instalar: https://dotnet.microsoft.com/download"
    exit 1
fi

# Verificar PostgreSQL
echo -n "Verificando PostgreSQL... "
if command -v psql &> /dev/null; then
    version=$(psql --version | head -1)
    echo "OK ($version)"
else
    echo "FALTA"
    echo "Instalar: sudo dnf install postgresql"
    exit 1
fi

# Verificar Docker (opcional)
echo -n "Verificando Docker... "
if command -v docker &> /dev/null; then
    version=$(docker --version)
    echo "OK ($version)"
else
    echo "NO INSTALADO (opcional)"
fi

# Verificar estructura del proyecto
echo -n "Verificando estructura del proyecto... "
if [ -f "src/Ceiba.Web/Ceiba.Web.csproj" ]; then
    echo "OK"
else
    echo "ERROR: No se encuentra el proyecto principal"
    exit 1
fi

# Verificar archivo .env
echo -n "Verificando configuración... "
if [ -f ".env" ]; then
    echo "OK (.env existe)"
else
    echo "ADVERTENCIA: .env no existe, copiando desde .env.example"
    cp .env.example .env
fi

# Compilar proyecto
echo "Compilando proyecto..."
dotnet build --verbosity quiet
if [ $? -eq 0 ]; then
    echo "Compilación: OK"
else
    echo "Compilación: ERROR"
    exit 1
fi

echo "=== Validación completada exitosamente ==="
```

#### 3.3 scan-logs-for-sensitive-data.ps1 (Nuevo)

```powershell
# scripts/security/scan-logs-for-sensitive-data.ps1
param(
    [string]$LogDir = "$PSScriptRoot\..\..\logs"
)

Write-Host "=== Escaneando logs por datos sensibles ===" -ForegroundColor Cyan

$patterns = @(
    @{ Name = "Email"; Pattern = "[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}" },
    @{ Name = "Password"; Pattern = "password\s*[=:]\s*\S+" },
    @{ Name = "API Key"; Pattern = "(api[_-]?key|apikey)\s*[=:]\s*\S+" },
    @{ Name = "Token"; Pattern = "(token|bearer)\s*[=:]\s*\S+" },
    @{ Name = "Credit Card"; Pattern = "\b\d{4}[- ]?\d{4}[- ]?\d{4}[- ]?\d{4}\b" }
)

$foundIssues = @()

Get-ChildItem -Path $LogDir -Filter "*.log" -Recurse | ForEach-Object {
    $file = $_
    $content = Get-Content $file.FullName -Raw

    foreach ($p in $patterns) {
        $matches = [regex]::Matches($content, $p.Pattern, "IgnoreCase")
        if ($matches.Count -gt 0) {
            $foundIssues += [PSCustomObject]@{
                File = $file.Name
                Type = $p.Name
                Count = $matches.Count
            }
        }
    }
}

if ($foundIssues.Count -gt 0) {
    Write-Host "ADVERTENCIA: Se encontraron posibles datos sensibles:" -ForegroundColor Yellow
    $foundIssues | Format-Table -AutoSize
    exit 1
} else {
    Write-Host "No se encontraron datos sensibles" -ForegroundColor Green
}
```

### Fase 4: Scripts de Migración (Prioridad Alta)

#### 4.1 backup-before-migration.ps1 (Nuevo)

```powershell
# scripts/migrations/backup-before-migration.ps1
param(
    [string]$MigrationName = "unknown"
)

$ErrorActionPreference = "Stop"
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupDir = "$PSScriptRoot\..\..\backups\migrations"
$backupFile = "$backupDir\pre_migration_${MigrationName}_$timestamp.sql.gz"

Write-Host "=== Backup antes de migración: $MigrationName ===" -ForegroundColor Cyan

# Crear directorio
if (-not (Test-Path $backupDir)) {
    New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
}

# Ejecutar backup
& "$PSScriptRoot\..\backup\backup-database.ps1" -OutputFile $backupFile

Write-Host "Backup guardado en: $backupFile" -ForegroundColor Green
Write-Host ""
Write-Host "Para restaurar en caso de problemas:" -ForegroundColor Yellow
Write-Host "  .\scripts\backup\restore-database.ps1 -InputFile `"$backupFile`""
```

## Convenciones para Scripts Cross-Platform

### Estructura de Archivos

```
scripts/
├── README.md                    # Documentación de scripts
├── common.sh                    # Funciones comunes Bash
├── common.ps1                   # Funciones comunes PowerShell
│
├── backup/
│   ├── backup-database.sh
│   ├── backup-database.ps1
│   ├── restore-database.sh
│   ├── restore-database.ps1
│   ├── verify-backup.sh
│   ├── verify-backup.ps1
│   ├── scheduled-backup.sh
│   ├── scheduled-backup.ps1
│   ├── monitor-backups.sh
│   └── monitor-backups.ps1
│
├── migrations/
│   ├── backup-before-migration.sh
│   ├── backup-before-migration.ps1
│   ├── validate-migration.sh
│   └── validate-migration.ps1
│
├── security/
│   ├── check-no-raw-sql.sh
│   ├── check-no-raw-sql.ps1
│   ├── scan-logs-for-sensitive-data.sh
│   └── scan-logs-for-sensitive-data.ps1
│
├── verification/
│   ├── e2e-verification.sh
│   ├── e2e-verification.ps1
│   ├── test-coverage-report.sh
│   ├── test-coverage-report.ps1
│   ├── validate-quickstart.sh
│   └── validate-quickstart.ps1
│
├── setup-database.sh
├── setup-database.ps1
├── reset-database.sh
├── reset-database.ps1
├── create-test-user.sh
└── create-test-user.ps1
```

### Convenciones de Código

#### Bash (.sh)

```bash
#!/bin/bash
# Descripción del script
# Uso: ./script.sh [opciones]

set -e  # Salir en caso de error
set -u  # Error si variable no definida

# Colores
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Funciones
log_info() { echo -e "${GREEN}[INFO]${NC} $1"; }
log_warn() { echo -e "${YELLOW}[WARN]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

# Directorio del script
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
```

#### PowerShell (.ps1)

```powershell
<#
.SYNOPSIS
    Descripción breve del script

.DESCRIPTION
    Descripción detallada

.PARAMETER Param1
    Descripción del parámetro

.EXAMPLE
    .\script.ps1 -Param1 valor
#>

param(
    [Parameter(Mandatory=$false)]
    [string]$Param1 = "default"
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

function Write-Info { param($Message) Write-Host "[INFO] $Message" -ForegroundColor Green }
function Write-Warn { param($Message) Write-Host "[WARN] $Message" -ForegroundColor Yellow }
function Write-Err { param($Message) Write-Host "[ERROR] $Message" -ForegroundColor Red }
```

### Equivalencias de Comandos

| Operación | Bash | PowerShell |
|-----------|------|------------|
| Directorio actual | `pwd` | `Get-Location` |
| Listar archivos | `ls -la` | `Get-ChildItem` |
| Crear directorio | `mkdir -p dir` | `New-Item -ItemType Directory -Force` |
| Copiar archivo | `cp src dst` | `Copy-Item src dst` |
| Mover archivo | `mv src dst` | `Move-Item src dst` |
| Eliminar archivo | `rm -f file` | `Remove-Item -Force file` |
| Leer archivo | `cat file` | `Get-Content file` |
| Escribir archivo | `echo "text" > file` | `Set-Content file "text"` |
| Variable entorno | `$VAR` | `$env:VAR` |
| Verificar existencia | `[ -f file ]` | `Test-Path file` |
| Comprimir gzip | `gzip file` | `Compress-Archive` o 7zip |
| Fecha actual | `date +%Y%m%d` | `Get-Date -Format "yyyyMMdd"` |

## Tareas de Implementación

### Prioridad Alta
- [ ] Crear `setup-database.sh`
- [ ] Crear `reset-database.sh`
- [ ] Crear `validate-migration.ps1`
- [ ] Crear `backup-before-migration.ps1`
- [ ] Crear `verify-backup.ps1`
- [ ] Crear `scheduled-backup.ps1`

### Prioridad Media
- [ ] Crear `monitor-backups.ps1`
- [ ] Crear `create-test-user.sh`
- [ ] Crear `validate-quickstart.sh`
- [ ] Crear `scan-logs-for-sensitive-data.ps1`
- [ ] Crear `scripts/README.md` con documentación

### Prioridad Baja
- [ ] Crear `common.sh` con funciones reutilizables
- [ ] Crear `common.ps1` con funciones reutilizables
- [ ] Agregar tests para scripts
- [ ] Agregar CI validation para scripts

## Criterios de Aceptación

1. [ ] Todos los scripts críticos tienen versión .sh y .ps1
2. [ ] Los scripts usan los mismos parámetros en ambas plataformas
3. [ ] Cada script tiene documentación de uso (--help)
4. [ ] Los scripts manejan errores apropiadamente
5. [ ] Los scripts usan códigos de salida consistentes (0=éxito, 1=error)
6. [ ] Existe un README.md documentando todos los scripts

---

**Próximos pasos**: Comenzar con los scripts de prioridad alta, empezando por `setup-database.sh` y `reset-database.sh` ya que son fundamentales para la instalación en Linux.
