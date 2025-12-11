# T119: Test coverage validation script
# Generates test coverage report and validates against thresholds
# Usage: .\test-coverage-report.ps1 [-Threshold <percentage>]

[CmdletBinding()]
param(
    [Parameter()]
    [int]$Threshold = 70,

    [Parameter()]
    [switch]$OpenReport,

    [Parameter()]
    [string]$OutputDir = "coverage"
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

Write-Host "=========================================="
Write-Host "Ceiba Test Coverage Report"
Write-Host "=========================================="
Write-Host "Threshold: $Threshold%"
Write-Host ""

# Create output directory
$CoverageDir = Join-Path $ProjectRoot $OutputDir
New-Item -ItemType Directory -Force -Path $CoverageDir | Out-Null

# Clean previous coverage data
Get-ChildItem -Path $CoverageDir -Filter "*.xml" -ErrorAction SilentlyContinue | Remove-Item -Force
Get-ChildItem -Path $CoverageDir -Filter "*.json" -ErrorAction SilentlyContinue | Remove-Item -Force

Write-Host "Running tests with coverage collection..."
Write-Host ""

# Run tests with coverage
$coverageArgs = @(
    "test"
    "--no-build"
    "--filter", "Category!=E2E"
    "--collect:XPlat Code Coverage"
    "--settings", "coverlet.runsettings"
    "--results-directory", $CoverageDir
    "--verbosity", "minimal"
)

Push-Location $ProjectRoot
try {
    & dotnet @coverageArgs 2>&1

    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Tests failed with exit code $LASTEXITCODE"
        exit 1
    }
} finally {
    Pop-Location
}

Write-Host ""
Write-Host "=========================================="
Write-Host "Coverage Collection Complete"
Write-Host "=========================================="

# Find coverage files
$coverageFiles = Get-ChildItem -Path $CoverageDir -Recurse -Filter "coverage.cobertura.xml" -ErrorAction SilentlyContinue

if ($coverageFiles.Count -eq 0) {
    Write-Host "WARNING: No coverage files found. Make sure coverlet is properly configured."
    Write-Host ""
    Write-Host "To install coverlet.collector, run:"
    Write-Host "  dotnet add package coverlet.collector"
    exit 1
}

Write-Host "Found $($coverageFiles.Count) coverage file(s)"

# Parse coverage results
$totalLines = 0
$coveredLines = 0
$totalBranches = 0
$coveredBranches = 0

foreach ($file in $coverageFiles) {
    Write-Host "  Processing: $($file.Name)"

    [xml]$coverage = Get-Content $file.FullName

    foreach ($package in $coverage.coverage.packages.package) {
        foreach ($class in $package.classes.class) {
            foreach ($line in $class.lines.line) {
                $totalLines++
                if ([int]$line.hits -gt 0) {
                    $coveredLines++
                }
            }
        }
    }

    # Get summary from coverage element
    if ($coverage.coverage.'line-rate') {
        $lineRate = [float]$coverage.coverage.'line-rate'
        $branchRate = [float]$coverage.coverage.'branch-rate'
    }
}

# Calculate percentages
$lineCoverage = if ($totalLines -gt 0) { [math]::Round(($coveredLines / $totalLines) * 100, 2) } else { 0 }

Write-Host ""
Write-Host "=========================================="
Write-Host "Coverage Summary"
Write-Host "=========================================="
Write-Host "Total Lines:    $totalLines"
Write-Host "Covered Lines:  $coveredLines"
Write-Host "Line Coverage:  $lineCoverage%"
Write-Host ""

# Check against threshold
$passed = $lineCoverage -ge $Threshold

if ($passed) {
    Write-Host "✓ PASS: Coverage ($lineCoverage%) meets threshold ($Threshold%)" -ForegroundColor Green
} else {
    Write-Host "✗ FAIL: Coverage ($lineCoverage%) is below threshold ($Threshold%)" -ForegroundColor Red
}

Write-Host ""
Write-Host "Coverage files saved to: $CoverageDir"

# Generate HTML report if ReportGenerator is available
$reportGenerator = Get-Command reportgenerator -ErrorAction SilentlyContinue

if ($reportGenerator) {
    Write-Host ""
    Write-Host "Generating HTML report..."

    $reportDir = Join-Path $CoverageDir "html"
    $coverageXmlFiles = ($coverageFiles | ForEach-Object { $_.FullName }) -join ";"

    & reportgenerator `
        -reports:"$coverageXmlFiles" `
        -targetdir:"$reportDir" `
        -reporttypes:"Html;HtmlSummary;Badges" `
        -verbosity:"Warning"

    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ HTML report generated: $reportDir\index.html"

        if ($OpenReport) {
            Start-Process (Join-Path $reportDir "index.html")
        }
    }
} else {
    Write-Host ""
    Write-Host "TIP: Install ReportGenerator for HTML reports:"
    Write-Host "  dotnet tool install -g dotnet-reportgenerator-globaltool"
}

Write-Host ""
Write-Host "=========================================="

if (-not $passed) {
    exit 1
}

exit 0
