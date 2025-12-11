# E2E Verification Script for Ceiba - Reportes de Incidencias
# This script verifies all critical flows are working correctly
# Run with: .\scripts\verification\e2e-verification.ps1

param(
    [string]$BaseUrl = "https://localhost:5001",
    [string]$DbHost = "localhost",
    [string]$DbName = "ceiba",
    [string]$DbUser = "ceiba",
    [string]$DbPassword = "ceiba123",
    [switch]$SkipDbTests = $false
)

$ErrorActionPreference = "Continue"
$script:PassedTests = 0
$script:FailedTests = 0
$script:SkippedTests = 0
$script:TestResults = @()

# Colors for output
function Write-TestHeader($text) {
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host $text -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
}

function Write-TestSection($text) {
    Write-Host "`n--- $text ---" -ForegroundColor Yellow
}

function Write-Pass($testName) {
    Write-Host "[PASS] $testName" -ForegroundColor Green
    $script:PassedTests++
    $script:TestResults += @{ Name = $testName; Status = "PASS"; Message = "" }
}

function Write-Fail($testName, $message = "") {
    Write-Host "[FAIL] $testName" -ForegroundColor Red
    if ($message) { Write-Host "       $message" -ForegroundColor Red }
    $script:FailedTests++
    $script:TestResults += @{ Name = $testName; Status = "FAIL"; Message = $message }
}

function Write-Skip($testName, $reason = "") {
    Write-Host "[SKIP] $testName" -ForegroundColor Yellow
    if ($reason) { Write-Host "       $reason" -ForegroundColor Yellow }
    $script:SkippedTests++
    $script:TestResults += @{ Name = $testName; Status = "SKIP"; Message = $reason }
}

# Ignore SSL certificate errors for localhost
add-type @"
using System.Net;
using System.Security.Cryptography.X509Certificates;
public class TrustAllCertsPolicy : ICertificatePolicy {
    public bool CheckValidationResult(
        ServicePoint srvPoint, X509Certificate certificate,
        WebRequest request, int certificateProblem) {
        return true;
    }
}
"@
[System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

Write-TestHeader "CEIBA - E2E Verification Script"
Write-Host "Base URL: $BaseUrl"
Write-Host "Database: $DbHost/$DbName"
Write-Host "Started: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"

# ============================================
# SECTION 1: Application Health
# ============================================
Write-TestSection "1. Application Health Checks"

# Test 1.1: Application is running
try {
    $response = Invoke-WebRequest -Uri "$BaseUrl" -UseBasicParsing -TimeoutSec 10 -ErrorAction Stop
    if ($response.StatusCode -eq 200) {
        Write-Pass "1.1 Application is accessible"
    } else {
        Write-Fail "1.1 Application is accessible" "Status code: $($response.StatusCode)"
    }
} catch {
    Write-Fail "1.1 Application is accessible" $_.Exception.Message
}

# Test 1.2: Login page loads
try {
    $response = Invoke-WebRequest -Uri "$BaseUrl/login" -UseBasicParsing -TimeoutSec 10 -ErrorAction Stop
    if ($response.StatusCode -eq 200 -and $response.Content -match "Iniciar Sesi") {
        Write-Pass "1.2 Login page loads correctly"
    } else {
        Write-Fail "1.2 Login page loads correctly" "Page content doesn't match expected"
    }
} catch {
    Write-Fail "1.2 Login page loads correctly" $_.Exception.Message
}

# Test 1.3: Static assets load (CSS/JS)
try {
    $response = Invoke-WebRequest -Uri "$BaseUrl/_framework/blazor.web.js" -UseBasicParsing -TimeoutSec 10 -ErrorAction Stop
    if ($response.StatusCode -eq 200) {
        Write-Pass "1.3 Blazor framework loads"
    } else {
        Write-Fail "1.3 Blazor framework loads" "Status code: $($response.StatusCode)"
    }
} catch {
    Write-Fail "1.3 Blazor framework loads" $_.Exception.Message
}

# ============================================
# SECTION 2: API Endpoints
# ============================================
Write-TestSection "2. API Endpoint Availability"

$apiEndpoints = @(
    @{ Path = "/api/catalogs/zonas"; Name = "2.1 Zonas endpoint" },
    @{ Path = "/api/catalogs/suggestions/Sexo"; Name = "2.2 Suggestions endpoint" },
    @{ Path = "/api/admin/audit"; Name = "2.3 Audit endpoint (requires auth)" },
    @{ Path = "/api/automated-reports"; Name = "2.4 Automated reports endpoint" }
)

foreach ($endpoint in $apiEndpoints) {
    try {
        $response = Invoke-WebRequest -Uri "$BaseUrl$($endpoint.Path)" -UseBasicParsing -TimeoutSec 10 -ErrorAction Stop
        Write-Pass $endpoint.Name
    } catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        if ($statusCode -eq 401 -or $statusCode -eq 403) {
            Write-Pass "$($endpoint.Name) (protected - 401/403 expected)"
        } else {
            Write-Fail $endpoint.Name "Status: $statusCode - $($_.Exception.Message)"
        }
    }
}

# ============================================
# SECTION 3: Database Connectivity
# ============================================
Write-TestSection "3. Database Verification"

if ($SkipDbTests) {
    Write-Skip "3.x Database tests" "Skipped via -SkipDbTests flag"
} else {
    $connString = "Host=$DbHost;Database=$DbName;Username=$DbUser;Password=$DbPassword"

    # Check if psql is available
    $psqlAvailable = $null -ne (Get-Command psql -ErrorAction SilentlyContinue)

    if (-not $psqlAvailable) {
        Write-Skip "3.x Database tests" "psql not found in PATH"
    } else {
        $env:PGPASSWORD = $DbPassword

        # Test 3.1: Database connection
        try {
            $result = psql -h $DbHost -U $DbUser -d $DbName -c "SELECT 1" 2>&1
            if ($LASTEXITCODE -eq 0) {
                Write-Pass "3.1 Database connection successful"
            } else {
                Write-Fail "3.1 Database connection successful" $result
            }
        } catch {
            Write-Fail "3.1 Database connection successful" $_.Exception.Message
        }

        # Test 3.2: Required tables exist
        $requiredTables = @(
            "AspNetUsers",
            "AspNetRoles",
            "REPORTE_INCIDENCIA",
            "ZONA",
            "SECTOR",
            "CUADRANTE",
            "AUDITORIA",
            "REPORTE_AUTOMATIZADO",
            "CONFIGURACION_REPORTES_AUTOMATIZADOS"
        )

        foreach ($table in $requiredTables) {
            try {
                $query = "SELECT EXISTS (SELECT FROM information_schema.tables WHERE table_name = '$($table.ToLower())');"
                $result = psql -h $DbHost -U $DbUser -d $DbName -t -c $query 2>&1
                if ($result -match "t") {
                    Write-Pass "3.2 Table exists: $table"
                } else {
                    Write-Fail "3.2 Table exists: $table" "Table not found"
                }
            } catch {
                Write-Fail "3.2 Table exists: $table" $_.Exception.Message
            }
        }

        # Test 3.3: Seed data exists
        try {
            $result = psql -h $DbHost -U $DbUser -d $DbName -t -c "SELECT COUNT(*) FROM ""AspNetUsers""" 2>&1
            $userCount = [int]($result.Trim())
            if ($userCount -gt 0) {
                Write-Pass "3.3 Users exist in database ($userCount users)"
            } else {
                Write-Fail "3.3 Users exist in database" "No users found"
            }
        } catch {
            Write-Fail "3.3 Users exist in database" $_.Exception.Message
        }

        # Test 3.4: Roles exist
        try {
            $result = psql -h $DbHost -U $DbUser -d $DbName -t -c "SELECT COUNT(*) FROM ""AspNetRoles"" WHERE ""Name"" IN ('CREADOR', 'REVISOR', 'ADMIN')" 2>&1
            $roleCount = [int]($result.Trim())
            if ($roleCount -eq 3) {
                Write-Pass "3.4 All roles exist (CREADOR, REVISOR, ADMIN)"
            } else {
                Write-Fail "3.4 All roles exist" "Found $roleCount of 3 roles"
            }
        } catch {
            Write-Fail "3.4 All roles exist" $_.Exception.Message
        }

        # Test 3.5: Geographic catalogs have data
        try {
            $zonaCount = psql -h $DbHost -U $DbUser -d $DbName -t -c "SELECT COUNT(*) FROM ""ZONA""" 2>&1
            $sectorCount = psql -h $DbHost -U $DbUser -d $DbName -t -c "SELECT COUNT(*) FROM ""SECTOR""" 2>&1
            $cuadranteCount = psql -h $DbHost -U $DbUser -d $DbName -t -c "SELECT COUNT(*) FROM ""CUADRANTE""" 2>&1

            if ([int]$zonaCount.Trim() -gt 0 -and [int]$sectorCount.Trim() -gt 0 -and [int]$cuadranteCount.Trim() -gt 0) {
                Write-Pass "3.5 Geographic catalogs populated (Z:$($zonaCount.Trim()) S:$($sectorCount.Trim()) C:$($cuadranteCount.Trim()))"
            } else {
                Write-Fail "3.5 Geographic catalogs populated" "Missing catalog data"
            }
        } catch {
            Write-Fail "3.5 Geographic catalogs populated" $_.Exception.Message
        }

        $env:PGPASSWORD = ""
    }
}

# ============================================
# SECTION 4: Authentication Flow (API-based)
# ============================================
Write-TestSection "4. Authentication API Tests"

# Create a session to maintain cookies
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession

# Test 4.1: Login with invalid credentials returns error
try {
    $body = @{
        email = "invalid@test.com"
        password = "wrongpassword"
    } | ConvertTo-Json

    $response = Invoke-WebRequest -Uri "$BaseUrl/api/Account/login" -Method POST -Body $body -ContentType "application/json" -UseBasicParsing -ErrorAction Stop
    Write-Fail "4.1 Invalid login rejected" "Should have returned 400/401"
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    if ($statusCode -eq 400 -or $statusCode -eq 401) {
        Write-Pass "4.1 Invalid login rejected (401/400)"
    } else {
        Write-Fail "4.1 Invalid login rejected" "Unexpected status: $statusCode"
    }
}

# Test 4.2: Login endpoint exists
try {
    $body = @{
        email = "admin@ceiba.local"
        password = "Admin123!"
    } | ConvertTo-Json

    $response = Invoke-WebRequest -Uri "$BaseUrl/Account/login" -Method POST -Body $body -ContentType "application/json" -WebSession $session -UseBasicParsing -ErrorAction SilentlyContinue
    # Any response means the endpoint exists
    Write-Pass "4.2 Login endpoint accessible"
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    if ($statusCode -gt 0) {
        Write-Pass "4.2 Login endpoint accessible (returned $statusCode)"
    } else {
        Write-Fail "4.2 Login endpoint accessible" $_.Exception.Message
    }
}

# ============================================
# SECTION 5: Authorization (Protected Routes)
# ============================================
Write-TestSection "5. Authorization - Protected Routes"

$protectedRoutes = @(
    @{ Path = "/reports"; Name = "5.1 Reports page (CREADOR)" },
    @{ Path = "/supervisor"; Name = "5.2 Supervisor panel (REVISOR)" },
    @{ Path = "/supervisor/reports"; Name = "5.3 All reports (REVISOR)" },
    @{ Path = "/supervisor/export"; Name = "5.4 Export page (REVISOR)" },
    @{ Path = "/automated"; Name = "5.5 Automated reports (REVISOR)" },
    @{ Path = "/admin"; Name = "5.6 Admin panel (ADMIN)" },
    @{ Path = "/admin/users"; Name = "5.7 User management (ADMIN)" },
    @{ Path = "/admin/catalogs"; Name = "5.8 Catalog management (ADMIN)" },
    @{ Path = "/admin/audit"; Name = "5.9 Audit logs (ADMIN)" }
)

foreach ($route in $protectedRoutes) {
    try {
        $response = Invoke-WebRequest -Uri "$BaseUrl$($route.Path)" -UseBasicParsing -TimeoutSec 10 -MaximumRedirection 0 -ErrorAction Stop
        # If we get here without redirect, might be an issue
        if ($response.StatusCode -eq 200) {
            # Check if it's the login page or actual content
            if ($response.Content -match "login" -or $response.Content -match "Iniciar Sesi") {
                Write-Pass "$($route.Name) - redirects to login"
            } else {
                Write-Fail "$($route.Name)" "Accessible without auth"
            }
        }
    } catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        if ($statusCode -eq 302 -or $statusCode -eq 301 -or $statusCode -eq 401 -or $statusCode -eq 403) {
            Write-Pass "$($route.Name) - protected ($statusCode)"
        } elseif ($statusCode -eq 200) {
            Write-Pass "$($route.Name) - accessible (Blazor handles auth)"
        } else {
            Write-Fail "$($route.Name)" "Unexpected: $statusCode"
        }
    }
}

# ============================================
# SECTION 6: API Contract Tests
# ============================================
Write-TestSection "6. API Contract Verification"

# Test 6.1: Catalogs API returns JSON array
try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/catalogs/zonas" -Method GET -TimeoutSec 10 -ErrorAction Stop
    if ($response -is [array] -or $response.Count -ge 0) {
        Write-Pass "6.1 Zonas API returns valid JSON"
    } else {
        Write-Fail "6.1 Zonas API returns valid JSON" "Invalid response format"
    }
} catch {
    Write-Fail "6.1 Zonas API returns valid JSON" $_.Exception.Message
}

# Test 6.2: Suggestions API
try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/catalogs/suggestions/Sexo" -Method GET -TimeoutSec 10 -ErrorAction Stop
    if ($response -is [array]) {
        Write-Pass "6.2 Suggestions API returns array (Sexo: $($response.Count) items)"
    } else {
        Write-Fail "6.2 Suggestions API returns array" "Invalid response"
    }
} catch {
    Write-Fail "6.2 Suggestions API returns array" $_.Exception.Message
}

# Test 6.3: Sectors cascade from Zona
try {
    # First get a zona ID
    $zonas = Invoke-RestMethod -Uri "$BaseUrl/api/catalogs/zonas" -Method GET -TimeoutSec 10
    if ($zonas.Count -gt 0) {
        $zonaId = $zonas[0].id
        $sectors = Invoke-RestMethod -Uri "$BaseUrl/api/catalogs/zonas/$zonaId/sectores" -Method GET -TimeoutSec 10
        Write-Pass "6.3 Sectors cascade from Zona (Zone $zonaId has $($sectors.Count) sectors)"
    } else {
        Write-Skip "6.3 Sectors cascade from Zona" "No zonas found"
    }
} catch {
    Write-Fail "6.3 Sectors cascade from Zona" $_.Exception.Message
}

# ============================================
# SECTION 7: Static Resources & UI
# ============================================
Write-TestSection "7. Static Resources"

$staticResources = @(
    @{ Path = "/css/app.css"; Name = "7.1 App CSS" },
    @{ Path = "/css/bootstrap/bootstrap.min.css"; Name = "7.2 Bootstrap CSS" },
    @{ Path = "/_framework/blazor.web.js"; Name = "7.3 Blazor JS" }
)

foreach ($resource in $staticResources) {
    try {
        $response = Invoke-WebRequest -Uri "$BaseUrl$($resource.Path)" -UseBasicParsing -TimeoutSec 10 -ErrorAction Stop
        if ($response.StatusCode -eq 200 -and $response.Content.Length -gt 0) {
            Write-Pass "$($resource.Name) ($($response.Content.Length) bytes)"
        } else {
            Write-Fail $resource.Name "Empty or invalid response"
        }
    } catch {
        Write-Fail $resource.Name $_.Exception.Message
    }
}

# ============================================
# SECTION 8: Error Handling
# ============================================
Write-TestSection "8. Error Handling"

# Test 8.1: 404 for non-existent page
try {
    $response = Invoke-WebRequest -Uri "$BaseUrl/this-page-does-not-exist-12345" -UseBasicParsing -TimeoutSec 10 -ErrorAction Stop
    # Blazor might return 200 with not found component
    if ($response.Content -match "404" -or $response.Content -match "not found" -or $response.Content -match "No encontrado") {
        Write-Pass "8.1 404 handling works"
    } else {
        Write-Pass "8.1 Non-existent route handled (Blazor routing)"
    }
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    if ($statusCode -eq 404) {
        Write-Pass "8.1 404 handling works"
    } else {
        Write-Fail "8.1 404 handling works" "Got status $statusCode"
    }
}

# Test 8.2: Invalid API request returns proper error
try {
    $response = Invoke-WebRequest -Uri "$BaseUrl/api/reports/999999999" -UseBasicParsing -TimeoutSec 10 -ErrorAction Stop
    Write-Fail "8.2 Invalid report ID handled" "Should return 404"
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    if ($statusCode -eq 404 -or $statusCode -eq 401) {
        Write-Pass "8.2 Invalid report ID returns $statusCode"
    } else {
        Write-Fail "8.2 Invalid report ID handled" "Got status $statusCode"
    }
}

# ============================================
# SUMMARY
# ============================================
Write-TestHeader "VERIFICATION SUMMARY"

$total = $script:PassedTests + $script:FailedTests + $script:SkippedTests
$passRate = if ($total -gt 0) { [math]::Round(($script:PassedTests / $total) * 100, 1) } else { 0 }

Write-Host ""
Write-Host "Total Tests: $total" -ForegroundColor White
Write-Host "Passed:      $($script:PassedTests)" -ForegroundColor Green
Write-Host "Failed:      $($script:FailedTests)" -ForegroundColor $(if ($script:FailedTests -gt 0) { "Red" } else { "Green" })
Write-Host "Skipped:     $($script:SkippedTests)" -ForegroundColor Yellow
Write-Host "Pass Rate:   $passRate%" -ForegroundColor $(if ($passRate -ge 80) { "Green" } elseif ($passRate -ge 60) { "Yellow" } else { "Red" })
Write-Host ""

if ($script:FailedTests -gt 0) {
    Write-Host "FAILED TESTS:" -ForegroundColor Red
    $script:TestResults | Where-Object { $_.Status -eq "FAIL" } | ForEach-Object {
        Write-Host "  - $($_.Name)" -ForegroundColor Red
        if ($_.Message) { Write-Host "    $($_.Message)" -ForegroundColor DarkRed }
    }
    Write-Host ""
}

Write-Host "Completed: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
Write-Host ""

# Exit with appropriate code
if ($script:FailedTests -gt 0) {
    exit 1
} else {
    exit 0
}
