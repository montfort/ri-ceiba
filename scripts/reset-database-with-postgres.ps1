# Script para resetear la base de datos usando el usuario postgres (superusuario)
# Usage: .\scripts\reset-database-with-postgres.ps1

param(
    [switch]$SkipConfirmation,
    [string]$PostgresPassword = $env:POSTGRES_PASSWORD
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Reset Database - Ceiba (Con Postgres)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if (-not $SkipConfirmation) {
    Write-Host "ADVERTENCIA: Este script eliminará TODA la base de datos y volverá a crearla." -ForegroundColor Yellow
    Write-Host "Se perderán TODOS los datos existentes." -ForegroundColor Yellow
    Write-Host ""
    $confirmation = Read-Host "¿Está seguro de continuar? (escriba 'SI' para confirmar)"

    if ($confirmation -ne "SI") {
        Write-Host "Operación cancelada." -ForegroundColor Red
        exit 0
    }
}

# Verificar si se proporcionó la contraseña de postgres
if ([string]::IsNullOrEmpty($PostgresPassword)) {
    Write-Host ""
    Write-Host "Ingrese la contraseña del usuario 'postgres':" -ForegroundColor Yellow
    $securePassword = Read-Host -AsSecureString
    $BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    $PostgresPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)
}

Write-Host ""
Write-Host "Paso 1: Terminando conexiones activas a la base de datos..." -ForegroundColor Yellow

try {
    $env:PGPASSWORD = $PostgresPassword

    # Terminar todas las conexiones activas
    $terminateQuery = @"
SELECT pg_terminate_backend(pid)
FROM pg_stat_activity
WHERE datname = 'ceiba' AND pid <> pg_backend_pid();
"@

    $terminateQuery | psql -h localhost -U postgres -d postgres -c 2>&1 | Out-Null
    Write-Host "✓ Conexiones terminadas" -ForegroundColor Green
} catch {
    Write-Host "⚠ Advertencia al terminar conexiones" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Paso 2: Eliminando base de datos..." -ForegroundColor Yellow

try {
    psql -h localhost -U postgres -d postgres -c "DROP DATABASE IF EXISTS ceiba;" 2>&1 | Out-Null
    Write-Host "✓ Base de datos eliminada" -ForegroundColor Green
} catch {
    Write-Host "✗ Error al eliminar la base de datos" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Paso 3: Creando base de datos con propietario 'ceiba'..." -ForegroundColor Yellow

try {
    # Crear usuario ceiba si no existe
    psql -h localhost -U postgres -d postgres -c "CREATE USER ceiba WITH PASSWORD 'ceiba123';" 2>&1 | Out-Null

    # Crear base de datos con propietario ceiba
    psql -h localhost -U postgres -d postgres -c "CREATE DATABASE ceiba OWNER ceiba;" 2>&1 | Out-Null

    # Otorgar todos los privilegios
    psql -h localhost -U postgres -d postgres -c "GRANT ALL PRIVILEGES ON DATABASE ceiba TO ceiba;" 2>&1 | Out-Null

    Write-Host "✓ Base de datos creada con propietario 'ceiba'" -ForegroundColor Green
} catch {
    Write-Host "✗ Error al crear la base de datos" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
} finally {
    # Limpiar la variable de entorno de contraseña
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "Paso 4: Aplicando migraciones..." -ForegroundColor Yellow

try {
    Set-Location src/Ceiba.Web
    dotnet ef database update --no-build
    Write-Host "✓ Migraciones aplicadas correctamente" -ForegroundColor Green
} catch {
    Write-Host "✗ Error al aplicar migraciones" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Set-Location ..\..\
    exit 1
}

Write-Host ""
Write-Host "Paso 5: Ejecutando aplicación para seed de datos..." -ForegroundColor Yellow
Write-Host "(La aplicación se ejecutará brevemente y luego se detendrá)" -ForegroundColor Gray
Write-Host ""

try {
    # Run application briefly to trigger seed
    $job = Start-Job -ScriptBlock {
        Set-Location $using:PWD
        dotnet run --no-build 2>&1
    }

    # Wait for seed to complete (max 30 seconds)
    $timeout = 30
    $elapsed = 0
    $seedCompleted = $false

    while ($elapsed -lt $timeout -and -not $seedCompleted) {
        Start-Sleep -Seconds 1
        $elapsed++

        # Check job output for seed completion
        $output = Receive-Job -Job $job -ErrorAction SilentlyContinue
        if ($output -match "Database seeded successfully|Database seeding completed|Created test user") {
            $seedCompleted = $true
            Write-Host "✓ Datos de prueba insertados correctamente" -ForegroundColor Green
            break
        }

        if ($elapsed % 5 -eq 0) {
            Write-Host "  Esperando seed... ($elapsed segundos)" -ForegroundColor Gray
        }
    }

    # Stop the job
    Stop-Job -Job $job -ErrorAction SilentlyContinue
    Remove-Job -Job $job -Force -ErrorAction SilentlyContinue

    if (-not $seedCompleted) {
        Write-Host "⚠ Timeout esperando seed. Es posible que los datos se hayan insertado." -ForegroundColor Yellow
    }

} catch {
    Write-Host "✗ Error durante seed" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
} finally {
    # Ensure we clean up any running processes
    Get-Process -Name "Ceiba.Web" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Set-Location ..\..\
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Base de datos reseteada exitosamente" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Datos de prueba creados:" -ForegroundColor Green
Write-Host "  • 5 Zonas (Norte, Sur, Centro, Oriente, Poniente)" -ForegroundColor White
Write-Host "  • 17 Sectores (3-4 por zona)" -ForegroundColor White
Write-Host "  • ~65 Cuadrantes (3-5 por sector)" -ForegroundColor White
Write-Host "  • 13 Sugerencias (Sexo, Delito, Tipo de Atención)" -ForegroundColor White
Write-Host ""
Write-Host "Usuarios de prueba:" -ForegroundColor Green
Write-Host "  • creador@test.com / Creador123!" -ForegroundColor White
Write-Host "  • revisor@test.com / Revisor123!" -ForegroundColor White
Write-Host "  • admin@test.com / Admin123!Test" -ForegroundColor White
Write-Host "  • admin@ceiba.local / Admin123!@ (super admin)" -ForegroundColor White
Write-Host ""
Write-Host "Para iniciar la aplicación:" -ForegroundColor Cyan
Write-Host "  cd src/Ceiba.Web" -ForegroundColor Gray
Write-Host "  dotnet run" -ForegroundColor Gray
Write-Host ""
