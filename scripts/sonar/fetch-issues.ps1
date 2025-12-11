<#
.SYNOPSIS
    Fetches SonarQube Cloud issues for analysis.

.DESCRIPTION
    This script fetches issues from SonarCloud API and formats them for analysis.
    Supports filtering by type, severity, and branch.

.PARAMETER Type
    Filter by issue type: BUG, VULNERABILITY, CODE_SMELL, SECURITY_HOTSPOT, or 'all'

.PARAMETER Severity
    Filter by severity: BLOCKER, CRITICAL, MAJOR, MINOR, INFO, or 'all'

.PARAMETER Branch
    Branch to analyze (default: current git branch)

.PARAMETER Format
    Output format: json, summary, or markdown (default: json)

.PARAMETER OutputFile
    Write output to file instead of console

.EXAMPLE
    .\fetch-issues.ps1 -Type BUG -Severity CRITICAL -Format summary

.EXAMPLE
    $env:SONAR_TOKEN = "your_token"
    .\fetch-issues.ps1 -OutputFile sonar-issues.json

.NOTES
    Requires SONAR_TOKEN environment variable.
    Generate token at: https://sonarcloud.io/account/security
#>

param(
    [ValidateSet('BUG', 'VULNERABILITY', 'CODE_SMELL', 'SECURITY_HOTSPOT', 'all')]
    [string]$Type = 'all',

    [ValidateSet('BLOCKER', 'CRITICAL', 'MAJOR', 'MINOR', 'INFO', 'all')]
    [string]$Severity = 'all',

    [string]$Branch = '',

    [ValidateSet('json', 'summary', 'markdown')]
    [string]$Format = 'json',

    [string]$OutputFile = ''
)

# Configuration
$SonarToken = $env:SONAR_TOKEN
$SonarOrg = if ($env:SONAR_ORG) { $env:SONAR_ORG } else { 'montfort' }
$SonarProject = if ($env:SONAR_PROJECT) { $env:SONAR_PROJECT } else { 'montfort_ri-ceiba' }
$SonarApi = 'https://sonarcloud.io/api'

# Validate token
if (-not $SonarToken) {
    Write-Error "SONAR_TOKEN environment variable is required"
    Write-Host "Generate a token at: https://sonarcloud.io/account/security" -ForegroundColor Yellow
    exit 1
}

# Get current branch if not specified
if (-not $Branch) {
    try {
        $Branch = git branch --show-current 2>$null
        if (-not $Branch) { $Branch = 'main' }
    } catch {
        $Branch = 'main'
    }
}

# Build query parameters
$params = @{
    componentKeys = $SonarProject
    organization  = $SonarOrg
    branch        = $Branch
    resolved      = 'false'
    ps            = 500
    facets        = 'types,severities,rules'
}

if ($Type -ne 'all') {
    $params['types'] = $Type
}

if ($Severity -ne 'all') {
    $params['severities'] = $Severity
}

$queryString = ($params.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" }) -join '&'
$url = "$SonarApi/issues/search?$queryString"

# Fetch issues
Write-Host "Fetching issues from SonarCloud..." -ForegroundColor Cyan
Write-Host "Branch: $Branch" -ForegroundColor Gray

try {
    $headers = @{
        'Authorization' = "Bearer $SonarToken"
    }
    $response = Invoke-RestMethod -Uri $url -Headers $headers -Method Get
} catch {
    Write-Error "API request failed: $_"
    exit 1
}

# Check for API errors
if ($response.errors) {
    Write-Error "API Error: $($response.errors.msg -join ', ')"
    exit 1
}

# Format functions
function Format-Summary {
    param($data)

    $output = @()
    $output += "========================================"
    $output += "SonarQube Cloud Issues Summary"
    $output += "========================================"
    $output += ""
    $output += "Project: $SonarProject"
    $output += "Branch:  $Branch"
    $output += "Date:    $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    $output += ""
    $output += "Total Issues: $($data.total)"
    $output += ""

    $output += "By Type:"
    $typeFacet = $data.facets | Where-Object { $_.property -eq 'types' }
    if ($typeFacet.values) {
        foreach ($v in $typeFacet.values) {
            $output += "  $($v.val): $($v.count)"
        }
    }
    $output += ""

    $output += "By Severity:"
    $sevFacet = $data.facets | Where-Object { $_.property -eq 'severities' }
    if ($sevFacet.values) {
        foreach ($v in $sevFacet.values) {
            $output += "  $($v.val): $($v.count)"
        }
    }
    $output += ""

    $output += "Top 10 Rules:"
    $ruleFacet = $data.facets | Where-Object { $_.property -eq 'rules' }
    if ($ruleFacet.values) {
        $top10 = $ruleFacet.values | Select-Object -First 10
        foreach ($v in $top10) {
            $output += "  $($v.val): $($v.count)"
        }
    }

    return $output -join "`n"
}

function Format-Markdown {
    param($data)

    $output = @()
    $output += "# SonarQube Cloud Issues Report"
    $output += ""
    $output += "**Project:** $SonarProject"
    $output += "**Branch:** $Branch"
    $output += "**Date:** $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    $output += "**Total Issues:** $($data.total)"
    $output += ""

    $output += "## Summary by Type"
    $output += ""
    $output += "| Type | Count |"
    $output += "|------|-------|"
    $typeFacet = $data.facets | Where-Object { $_.property -eq 'types' }
    if ($typeFacet.values) {
        foreach ($v in $typeFacet.values) {
            $output += "| $($v.val) | $($v.count) |"
        }
    }
    $output += ""

    $output += "## Summary by Severity"
    $output += ""
    $output += "| Severity | Count |"
    $output += "|----------|-------|"
    $sevFacet = $data.facets | Where-Object { $_.property -eq 'severities' }
    if ($sevFacet.values) {
        foreach ($v in $sevFacet.values) {
            $output += "| $($v.val) | $($v.count) |"
        }
    }
    $output += ""

    $output += "## Issues Detail"
    $output += ""
    foreach ($issue in $data.issues) {
        $file = if ($issue.component) { ($issue.component -split ':')[1] } else { 'N/A' }
        $output += "### $($issue.severity): $($issue.message)"
        $output += ""
        $output += "- **File:** $file"
        $output += "- **Line:** $(if ($issue.line) { $issue.line } else { 'N/A' })"
        $output += "- **Rule:** $($issue.rule)"
        $output += "- **Type:** $($issue.type)"
        $output += "- **Effort:** $(if ($issue.effort) { $issue.effort } else { 'N/A' })"
        $output += ""
    }

    return $output -join "`n"
}

function Format-Json {
    param($data)

    $typeFacet = $data.facets | Where-Object { $_.property -eq 'types' }
    $sevFacet = $data.facets | Where-Object { $_.property -eq 'severities' }

    $byType = @{}
    if ($typeFacet.values) {
        foreach ($v in $typeFacet.values) {
            $byType[$v.val] = $v.count
        }
    }

    $bySeverity = @{}
    if ($sevFacet.values) {
        foreach ($v in $sevFacet.values) {
            $bySeverity[$v.val] = $v.count
        }
    }

    $issues = @()
    foreach ($issue in $data.issues) {
        $file = if ($issue.component) { ($issue.component -split ':')[1] } else { $null }
        $issues += @{
            key      = $issue.key
            rule     = $issue.rule
            severity = $issue.severity
            type     = $issue.type
            file     = $file
            line     = $issue.line
            message  = $issue.message
            effort   = $issue.effort
            debt     = $issue.debt
            tags     = $issue.tags
        }
    }

    $result = @{
        metadata = @{
            project   = $SonarProject
            branch    = $Branch
            timestamp = (Get-Date -Format 'yyyy-MM-ddTHH:mm:ssZ')
            total     = $data.total
        }
        summary  = @{
            byType     = $byType
            bySeverity = $bySeverity
        }
        issues   = $issues
    }

    return $result | ConvertTo-Json -Depth 10
}

# Generate output
$output = switch ($Format) {
    'summary' { Format-Summary -data $response }
    'markdown' { Format-Markdown -data $response }
    default { Format-Json -data $response }
}

# Output
if ($OutputFile) {
    $output | Out-File -FilePath $OutputFile -Encoding UTF8
    Write-Host "Output written to: $OutputFile" -ForegroundColor Green
} else {
    Write-Output $output
}

Write-Host "`nTotal issues found: $($response.total)" -ForegroundColor $(if ($response.total -eq 0) { 'Green' } else { 'Yellow' })
