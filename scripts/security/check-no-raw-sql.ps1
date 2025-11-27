# T020j: RS-002 Mitigation - Zero Raw SQL Policy Check (PowerShell)
# Scans codebase for raw SQL queries to prevent SQL injection vulnerabilities
# Exit code 0 = pass, 1 = violations found

param(
    [string]$ProjectRoot = "."
)

$ErrorActionPreference = "Stop"

# Configuration
$Violations = 0
$Warnings = 0

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Zero Raw SQL Policy Check" -ForegroundColor Cyan
Write-Host "T020j: RS-002 Mitigation" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# Patterns that indicate raw SQL usage (violations)
$SqlPatterns = @(
    'ExecuteSqlRaw',
    'ExecuteSqlRawAsync',
    'FromSqlRaw',
    'SqlQuery',
    'Database\.SqlQuery',
    'context\.Database\.ExecuteSqlCommand',
    'SELECT \* FROM',
    'INSERT INTO',
    'UPDATE .* SET',
    'DELETE FROM',
    'DROP TABLE',
    'CREATE TABLE',
    'ALTER TABLE'
)

# Allowed patterns (not violations)
$AllowedPatterns = @(
    'ExecuteSqlInterpolated',  # Safe: uses parameterized queries
    'FromSqlInterpolated',     # Safe: uses parameterized queries
    '\/\/ APPROVED:',          # Explicit approval comment
    '\/\* APPROVED:'           # Explicit approval comment
)

# Directories to exclude
$ExcludeDirs = @(
    '*\bin\*',
    '*\obj\*',
    '*\Migrations\*',
    '*\node_modules\*',
    '*\.git\*'
)

Write-Host "Scanning for raw SQL usage in C# files..." -ForegroundColor White
Write-Host ""

# Function to check if line is approved
function Test-Approved {
    param([string]$Line)

    foreach ($pattern in $AllowedPatterns) {
        if ($Line -match $pattern) {
            return $true
        }
    }
    return $false
}

# Function to check if line contains SQL keyword
function Test-SqlKeyword {
    param([string]$Line)

    if ($Line -match '(SELECT|INSERT|UPDATE|DELETE|DROP|CREATE|ALTER)\s+(FROM|INTO|TABLE|SET)') {
        return $true
    }
    return $false
}

# Get all C# files
$files = Get-ChildItem -Path $ProjectRoot -Recurse -Include "*.cs" -File |
    Where-Object {
        $file = $_
        $excluded = $false
        foreach ($excludePattern in $ExcludeDirs) {
            if ($file.FullName -like $excludePattern) {
                $excluded = $true
                break
            }
        }
        -not $excluded
    }

# Scan each file
foreach ($file in $files) {
    $lineNumber = 0
    $content = Get-Content $file.FullName

    foreach ($line in $content) {
        $lineNumber++

        # Check for SQL patterns
        foreach ($pattern in $SqlPatterns) {
            if ($line -match $pattern) {
                $relativePath = $file.FullName.Replace($PWD.Path, ".").Replace("\", "/")

                if (Test-Approved $line) {
                    Write-Host "[APPROVED] " -ForegroundColor Yellow -NoNewline
                    Write-Host "$relativePath:$lineNumber"
                    Write-Host "  $line" -ForegroundColor Gray
                    $Warnings++
                }
                else {
                    Write-Host "[VIOLATION] " -ForegroundColor Red -NoNewline
                    Write-Host "$relativePath:$lineNumber"
                    Write-Host "  $line" -ForegroundColor Gray
                    $Violations++
                }
            }
        }

        # Check for SQL keywords in string literals
        if (Test-SqlKeyword $line) {
            # Skip comments
            if ($line -match '^\s*(\/\/|\/\*|\*)') {
                continue
            }

            # Skip approved
            if (Test-Approved $line) {
                continue
            }

            # Check for string concatenation (dangerous)
            if ($line -match '(\+|string\.Format|String\.Format|\$).*("|\`)') {
                $relativePath = $file.FullName.Replace($PWD.Path, ".").Replace("\", "/")
                Write-Host "[POTENTIAL SQL INJECTION] " -ForegroundColor Red -NoNewline
                Write-Host "$relativePath:$lineNumber"
                Write-Host "  $line" -ForegroundColor Gray
                $Violations++
            }
        }
    }
}

Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Scan Complete" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Violations: " -NoNewline
Write-Host "$Violations" -ForegroundColor Red
Write-Host "Approved Raw SQL: " -NoNewline
Write-Host "$Warnings" -ForegroundColor Yellow
Write-Host ""

if ($Violations -gt 0) {
    Write-Host "❌ FAILED: Raw SQL violations detected" -ForegroundColor Red
    Write-Host ""
    Write-Host "To fix violations:" -ForegroundColor Yellow
    Write-Host "1. Use Entity Framework LINQ queries instead of raw SQL"
    Write-Host "2. Use ExecuteSqlInterpolated() or FromSqlInterpolated() with parameterized queries"
    Write-Host "3. Add '// APPROVED: <reason>' comment for necessary raw SQL"
    Write-Host ""
    Write-Host "Example - BAD (SQL Injection Risk):" -ForegroundColor Red
    Write-Host '  context.Database.ExecuteSqlRaw("SELECT * FROM Users WHERE Name = '" + userName + "'");'
    Write-Host ""
    Write-Host "Example - GOOD (Parameterized):" -ForegroundColor Green
    Write-Host '  context.Database.ExecuteSqlInterpolated($"SELECT * FROM Users WHERE Name = {userName}");'
    Write-Host ""
    Write-Host "Example - BEST (LINQ):" -ForegroundColor Green
    Write-Host '  context.Users.Where(u => u.Name == userName).ToList();'
    Write-Host ""
    exit 1
}
else {
    Write-Host "✅ PASSED: No raw SQL violations detected" -ForegroundColor Green
    if ($Warnings -gt 0) {
        Write-Host "⚠️  Note: $Warnings approved raw SQL usage(s) found" -ForegroundColor Yellow
        Write-Host "   These are explicitly approved but should be reviewed periodically."
    }
    Write-Host ""
    exit 0
}
