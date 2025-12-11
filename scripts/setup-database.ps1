# Script de PowerShell para configurar la base de datos PostgreSQL
# Para el proyecto Ceiba - Reportes de Incidencias

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "  Configuración de Base de Datos PostgreSQL" -ForegroundColor Cyan
Write-Host "  Proyecto: Ceiba - Reportes de Incidencias" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# Verificar si psql está disponible
try {
    $psqlVersion = & psql --version 2>&1
    Write-Host "✓ PostgreSQL CLI encontrado: $psqlVersion" -ForegroundColor Green
} catch {
    Write-Host "✗ ERROR: psql no está disponible en el PATH" -ForegroundColor Red
    Write-Host "  Por favor, instala PostgreSQL o agrega psql.exe al PATH del sistema" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "Este script creará:" -ForegroundColor Yellow
Write-Host "  - Base de datos: ceiba" -ForegroundColor White
Write-Host "  - Usuario: ceiba (contraseña: definida en setup-database.sql)" -ForegroundColor White
Write-Host "  - Permisos completos para el usuario ceiba" -ForegroundColor White
Write-Host ""
Write-Host "IMPORTANTE: Edite setup-database.sql y reemplace 'CHANGE_THIS_PASSWORD' con su contraseña" -ForegroundColor Yellow
Write-Host ""

# Solicitar credenciales de superusuario
Write-Host "Ingresa las credenciales del superusuario de PostgreSQL" -ForegroundColor Cyan
$superuser = Read-Host "Usuario (por defecto: postgres)"
if ([string]::IsNullOrWhiteSpace($superuser)) {
    $superuser = "postgres"
}

Write-Host ""
Write-Host "Ejecutando script SQL..." -ForegroundColor Cyan

# Ejecutar el script SQL
$scriptPath = Join-Path $PSScriptRoot "setup-database.sql"

if (-not (Test-Path $scriptPath)) {
    Write-Host "✗ ERROR: No se encontró el archivo setup-database.sql" -ForegroundColor Red
    Write-Host "  Ruta esperada: $scriptPath" -ForegroundColor Yellow
    exit 1
}

try {
    & psql -U $superuser -f $scriptPath

    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "==================================================" -ForegroundColor Green
        Write-Host "  ✓ Base de datos configurada exitosamente" -ForegroundColor Green
        Write-Host "==================================================" -ForegroundColor Green
        Write-Host ""
        Write-Host "Cadena de conexión (reemplace YOUR_PASSWORD):" -ForegroundColor Cyan
        Write-Host "  Host=localhost;Database=ceiba;Username=ceiba;Password=YOUR_PASSWORD" -ForegroundColor White
        Write-Host ""
        Write-Host "O configure la variable de entorno:" -ForegroundColor Cyan
        Write-Host "  `$env:DB_PASSWORD='your_password'" -ForegroundColor White
        Write-Host ""
        Write-Host "Siguiente paso:" -ForegroundColor Yellow
        Write-Host "  1. Ejecuta la aplicación con: dotnet run --project src/Ceiba.Web" -ForegroundColor White
        Write-Host "  2. Las migraciones se aplicarán automáticamente" -ForegroundColor White
        Write-Host ""
    } else {
        Write-Host ""
        Write-Host "✗ ERROR: Falló la configuración de la base de datos" -ForegroundColor Red
        Write-Host "  Código de salida: $LASTEXITCODE" -ForegroundColor Yellow
        exit $LASTEXITCODE
    }
} catch {
    Write-Host ""
    Write-Host "✗ ERROR: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
