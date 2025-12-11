# T120: Quickstart guide validation script
# Validates that the quickstart documentation is accurate
# Usage: .\validate-quickstart.ps1

[CmdletBinding()]
param(
    [Parameter()]
    [switch]$SkipBuild,

    [Parameter()]
    [switch]$SkipDocker
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

$passCount = 0
$failCount = 0
$skipCount = 0

function Test-Check {
    param(
        [string]$Name,
        [scriptblock]$Test,
        [switch]$Skip
    )

    Write-Host -NoNewline "  Checking: $Name... "

    if ($Skip) {
        Write-Host "SKIP" -ForegroundColor Yellow
        $script:skipCount++
        return
    }

    try {
        $result = & $Test
        if ($result) {
            Write-Host "OK" -ForegroundColor Green
            $script:passCount++
        } else {
            Write-Host "FAIL" -ForegroundColor Red
            $script:failCount++
        }
    } catch {
        Write-Host "FAIL - $_" -ForegroundColor Red
        $script:failCount++
    }
}

Write-Host "=========================================="
Write-Host "Quickstart Guide Validation"
Write-Host "=========================================="
Write-Host "Project: $ProjectRoot"
Write-Host ""

# =====================================
# Prerequisites
# =====================================
Write-Host "Prerequisites:" -ForegroundColor Cyan

Test-Check ".NET SDK installed" {
    $version = dotnet --version 2>$null
    $version -match "^10\." -or $version -match "^9\." -or $version -match "^8\."
}

Test-Check "Docker installed" -Skip:$SkipDocker {
    docker --version 2>$null | Out-Null
    $LASTEXITCODE -eq 0
}

Test-Check "Docker Compose installed" -Skip:$SkipDocker {
    docker compose version 2>$null | Out-Null
    $LASTEXITCODE -eq 0
}

Test-Check "Git installed" {
    git --version 2>$null | Out-Null
    $LASTEXITCODE -eq 0
}

# =====================================
# Project Structure
# =====================================
Write-Host ""
Write-Host "Project Structure:" -ForegroundColor Cyan

Test-Check "src/Ceiba.Web exists" {
    Test-Path (Join-Path $ProjectRoot "src\Ceiba.Web")
}

Test-Check "src/Ceiba.Core exists" {
    Test-Path (Join-Path $ProjectRoot "src\Ceiba.Core")
}

Test-Check "src/Ceiba.Application exists" {
    Test-Path (Join-Path $ProjectRoot "src\Ceiba.Application")
}

Test-Check "src/Ceiba.Infrastructure exists" {
    Test-Path (Join-Path $ProjectRoot "src\Ceiba.Infrastructure")
}

Test-Check "src/Ceiba.Shared exists" {
    Test-Path (Join-Path $ProjectRoot "src\Ceiba.Shared")
}

Test-Check "tests/Ceiba.Core.Tests exists" {
    Test-Path (Join-Path $ProjectRoot "tests\Ceiba.Core.Tests")
}

Test-Check "tests/Ceiba.Application.Tests exists" {
    Test-Path (Join-Path $ProjectRoot "tests\Ceiba.Application.Tests")
}

Test-Check "tests/Ceiba.Infrastructure.Tests exists" {
    Test-Path (Join-Path $ProjectRoot "tests\Ceiba.Infrastructure.Tests")
}

Test-Check "tests/Ceiba.Web.Tests exists" {
    Test-Path (Join-Path $ProjectRoot "tests\Ceiba.Web.Tests")
}

Test-Check "tests/Ceiba.Integration.Tests exists" {
    Test-Path (Join-Path $ProjectRoot "tests\Ceiba.Integration.Tests")
}

# =====================================
# Configuration Files
# =====================================
Write-Host ""
Write-Host "Configuration Files:" -ForegroundColor Cyan

Test-Check "appsettings.json exists" {
    Test-Path (Join-Path $ProjectRoot "src\Ceiba.Web\appsettings.json")
}

Test-Check "docker-compose.yml exists" {
    Test-Path (Join-Path $ProjectRoot "docker\docker-compose.yml")
}

Test-Check "docker-compose.prod.yml exists" {
    Test-Path (Join-Path $ProjectRoot "docker\docker-compose.prod.yml")
}

Test-Check "Dockerfile exists" {
    Test-Path (Join-Path $ProjectRoot "docker\Dockerfile")
}

Test-Check "coverlet.runsettings exists" {
    Test-Path (Join-Path $ProjectRoot "coverlet.runsettings")
}

# =====================================
# Build Validation
# =====================================
Write-Host ""
Write-Host "Build Validation:" -ForegroundColor Cyan

Test-Check "dotnet restore succeeds" -Skip:$SkipBuild {
    Push-Location $ProjectRoot
    try {
        dotnet restore --verbosity quiet 2>&1 | Out-Null
        $LASTEXITCODE -eq 0
    } finally {
        Pop-Location
    }
}

Test-Check "dotnet build succeeds" -Skip:$SkipBuild {
    Push-Location $ProjectRoot
    try {
        dotnet build --no-restore --verbosity quiet 2>&1 | Out-Null
        $LASTEXITCODE -eq 0
    } finally {
        Pop-Location
    }
}

# =====================================
# Test Validation
# =====================================
Write-Host ""
Write-Host "Test Validation:" -ForegroundColor Cyan

Test-Check "Unit tests pass" -Skip:$SkipBuild {
    Push-Location $ProjectRoot
    try {
        $result = dotnet test --no-build --filter "Category!=E2E&Category!=Integration" --verbosity quiet 2>&1
        $LASTEXITCODE -eq 0
    } finally {
        Pop-Location
    }
}

# =====================================
# Documentation Files
# =====================================
Write-Host ""
Write-Host "Documentation Files:" -ForegroundColor Cyan

Test-Check "quickstart.md exists" {
    Test-Path (Join-Path $ProjectRoot "specs\001-incident-management-system\quickstart.md")
}

Test-Check "spec.md exists" {
    Test-Path (Join-Path $ProjectRoot "specs\001-incident-management-system\spec.md")
}

Test-Check "data-model.md exists" {
    Test-Path (Join-Path $ProjectRoot "specs\001-incident-management-system\data-model.md")
}

Test-Check "constitution.md exists" {
    Test-Path (Join-Path $ProjectRoot ".specify\memory\constitution.md")
}

Test-Check "CLAUDE.md exists" {
    Test-Path (Join-Path $ProjectRoot "CLAUDE.md")
}

# =====================================
# Scripts
# =====================================
Write-Host ""
Write-Host "Scripts:" -ForegroundColor Cyan

Test-Check "Backup scripts exist" {
    (Test-Path (Join-Path $ProjectRoot "scripts\backup\backup-database.sh")) -or
    (Test-Path (Join-Path $ProjectRoot "scripts\backup\backup-database.ps1"))
}

Test-Check "Restore scripts exist" {
    (Test-Path (Join-Path $ProjectRoot "scripts\backup\restore-database.sh")) -or
    (Test-Path (Join-Path $ProjectRoot "scripts\backup\restore-database.ps1"))
}

Test-Check "Database setup scripts exist" {
    Test-Path (Join-Path $ProjectRoot "scripts\setup-database.sql")
}

# =====================================
# API Contracts
# =====================================
Write-Host ""
Write-Host "API Contracts:" -ForegroundColor Cyan

Test-Check "api-auth.yaml exists" {
    Test-Path (Join-Path $ProjectRoot "specs\001-incident-management-system\contracts\api-auth.yaml")
}

Test-Check "api-reports.yaml exists" {
    Test-Path (Join-Path $ProjectRoot "specs\001-incident-management-system\contracts\api-reports.yaml")
}

Test-Check "api-admin.yaml exists" {
    Test-Path (Join-Path $ProjectRoot "specs\001-incident-management-system\contracts\api-admin.yaml")
}

Test-Check "api-audit.yaml exists" {
    Test-Path (Join-Path $ProjectRoot "specs\001-incident-management-system\contracts\api-audit.yaml")
}

# =====================================
# Summary
# =====================================
Write-Host ""
Write-Host "=========================================="
Write-Host "Validation Summary"
Write-Host "=========================================="
Write-Host "Passed:  $passCount" -ForegroundColor Green
Write-Host "Failed:  $failCount" -ForegroundColor $(if ($failCount -gt 0) { "Red" } else { "Green" })
Write-Host "Skipped: $skipCount" -ForegroundColor Yellow
Write-Host "Total:   $($passCount + $failCount + $skipCount)"
Write-Host ""

if ($failCount -gt 0) {
    Write-Host "✗ Quickstart validation FAILED" -ForegroundColor Red
    Write-Host "Please update quickstart.md to match current project structure."
    exit 1
} else {
    Write-Host "✓ Quickstart validation PASSED" -ForegroundColor Green
    exit 0
}
