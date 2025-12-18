# sync-wiki.ps1
# Script para sincronizar docs/wiki/ con el GitHub Wiki en Windows
#
# Uso:
#   .\scripts\sync-wiki.ps1 -Action push
#   .\scripts\sync-wiki.ps1 -Action pull
#
# Prerrequisitos:
#   - Git para Windows instalado
#   - Acceso al repositorio wiki de GitHub
#   - SSH key configurada o token de acceso

# Nota: El wiki debe estar habilitado en GitHub para que el repositorio exista.
# Ve a Settings > Features > Wikis en tu repositorio de GitHub.

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("push", "pull", "clean", "help")]
    [string]$Action,

    # URL del wiki (repo principal + .wiki.git)
    # Usar HTTPS para compatibilidad con autenticación por token/credential helper
    [string]$WikiRepoUrl = "https://github.com/montfort/ri-ceiba.wiki.git"
)

# Configuración
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$WikiSource = Join-Path $ProjectRoot "docs\wiki"
$WikiCloneDir = Join-Path $ProjectRoot ".wiki-temp"

function Write-Info {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Green
}

function Write-Warn {
    param([string]$Message)
    Write-Host "[WARN] $Message" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

function Test-WikiSource {
    if (-not (Test-Path $WikiSource)) {
        Write-Error "No se encontró el directorio $WikiSource"
        exit 1
    }
}

function Initialize-WikiClone {
    if (Test-Path $WikiCloneDir) {
        Write-Info "Actualizando copia local del wiki..."
        Push-Location $WikiCloneDir
        try {
            git fetch origin
            $defaultBranch = git rev-parse --abbrev-ref origin/HEAD 2>$null
            if ($defaultBranch) {
                $branch = $defaultBranch -replace "origin/", ""
                git reset --hard "origin/$branch"
            } else {
                # Intentar master, luego main
                try {
                    git reset --hard origin/master
                } catch {
                    git reset --hard origin/main
                }
            }
        } finally {
            Pop-Location
        }
    } else {
        Write-Info "Clonando wiki desde $WikiRepoUrl..."
        git clone $WikiRepoUrl $WikiCloneDir
    }
}

function Push-ToWiki {
    Test-WikiSource
    Initialize-WikiClone

    Write-Info "Copiando archivos de $WikiSource a $WikiCloneDir..."

    # Limpiar contenido anterior (excepto .git)
    Get-ChildItem $WikiCloneDir -Exclude ".git" | Remove-Item -Recurse -Force

    # Copiar archivos nuevos
    Get-ChildItem $WikiSource | Copy-Item -Destination $WikiCloneDir -Recurse -Force

    Push-Location $WikiCloneDir
    try {
        # Verificar si hay cambios
        $status = git status --porcelain
        if (-not $status) {
            Write-Info "No hay cambios para sincronizar"
            return
        }

        # Agregar y commitear
        git add -A

        # Mostrar resumen de cambios
        Write-Info "Cambios a sincronizar:"
        git status --short

        # Crear commit
        $commitMsg = "Sync wiki from docs/wiki/ - $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
        git commit -m $commitMsg

        # Push
        Write-Info "Subiendo cambios al wiki de GitHub..."
        try {
            git push origin master
        } catch {
            git push origin main
        }

        Write-Info "✓ Wiki sincronizado exitosamente"
    } finally {
        Pop-Location
    }
}

function Pull-FromWiki {
    Initialize-WikiClone

    Write-Info "Copiando archivos de $WikiCloneDir a $WikiSource..."

    # Crear directorio destino si no existe
    if (-not (Test-Path $WikiSource)) {
        New-Item -ItemType Directory -Path $WikiSource -Force | Out-Null
    }

    # Limpiar contenido anterior
    Get-ChildItem $WikiSource | Remove-Item -Recurse -Force

    # Copiar archivos (excluyendo .git)
    Get-ChildItem $WikiCloneDir -Exclude ".git" | Copy-Item -Destination $WikiSource -Recurse -Force

    Write-Info "✓ Contenido del wiki descargado a $WikiSource"
    Write-Info "Recuerda hacer commit de los cambios en el repositorio principal"
}

function Clear-WikiTemp {
    if (Test-Path $WikiCloneDir) {
        Write-Info "Limpiando directorio temporal..."
        Remove-Item $WikiCloneDir -Recurse -Force
    }
}

function Show-Help {
    Write-Host @"
Uso: .\sync-wiki.ps1 -Action <comando> [-WikiRepoUrl <url>]

Comandos:
  push    Sincronizar docs/wiki/ -> GitHub Wiki
  pull    Sincronizar GitHub Wiki -> docs/wiki/
  clean   Limpiar directorio temporal
  help    Mostrar esta ayuda

Parámetros:
  -WikiRepoUrl   URL del repositorio wiki (default: https://github.com/montfort/ri-ceiba.wiki.git)

Ejemplos:
  .\sync-wiki.ps1 -Action push
  .\sync-wiki.ps1 -Action pull
  .\sync-wiki.ps1 -Action push -WikiRepoUrl "git@github.com:otro/repo.wiki.git"
"@
}

# Main
switch ($Action) {
    "push" { Push-ToWiki }
    "pull" { Pull-FromWiki }
    "clean" { Clear-WikiTemp }
    "help" { Show-Help }
}
