# T115: Database backup script for Ceiba application (PowerShell version)
# Creates compressed PostgreSQL backup with timestamp
# Usage: .\backup-database.ps1 [-OutputDir <path>]

[CmdletBinding()]
param(
    [Parameter()]
    [string]$OutputDir = "backups\daily"
)

$ErrorActionPreference = "Stop"

# Configuration - can be overridden by environment variables
$DbHost = if ($env:DB_HOST) { $env:DB_HOST } else { "localhost" }
$DbPort = if ($env:DB_PORT) { $env:DB_PORT } else { "5432" }
$DbName = if ($env:DB_NAME) { $env:DB_NAME } else { "ceiba" }
$DbUser = if ($env:DB_USER) { $env:DB_USER } else { "ceiba" }
$env:PGPASSWORD = if ($env:DB_PASSWORD) { $env:DB_PASSWORD } else { "ceiba123" }

# Backup configuration
$Timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$DateDir = Get-Date -Format "yyyy\\MM"
$BackupDir = Join-Path $OutputDir $DateDir
$BackupFile = Join-Path $BackupDir "ceiba-backup-$Timestamp.sql.gz"
$LogFile = Join-Path $OutputDir "backup.log"

function Log {
    param([string]$Message)
    $timestampedMsg = "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] $Message"
    Write-Host $timestampedMsg
    Add-Content -Path $LogFile -Value $timestampedMsg -ErrorAction SilentlyContinue
}

function Test-PostgresConnection {
    $testResult = & pg_isready -h $DbHost -p $DbPort -U $DbUser -d $DbName 2>&1
    return $LASTEXITCODE -eq 0
}

# Create directories
New-Item -ItemType Directory -Force -Path $BackupDir | Out-Null

Log "=========================================="
Log "Ceiba Database Backup (PowerShell)"
Log "=========================================="
Log "Host: ${DbHost}:${DbPort}"
Log "Database: $DbName"
Log "Backup file: $BackupFile"
Log ""

# Check PostgreSQL connection
Log "Testing database connection..."
if (-not (Test-PostgresConnection)) {
    Log "ERROR: Cannot connect to PostgreSQL database"
    exit 1
}
Log "✓ Database connection OK"

# Get database size
try {
    $sizeQuery = "SELECT pg_size_pretty(pg_database_size('$DbName'))"
    $dbSize = & psql -h $DbHost -p $DbPort -U $DbUser -d $DbName -t -c $sizeQuery 2>$null | ForEach-Object { $_.Trim() }
    Log "Database size: $dbSize"
} catch {
    Log "Warning: Could not determine database size"
}

# Perform backup
Log "Creating backup..."
$startTime = Get-Date

try {
    # Use pg_dump with custom format and compression
    $backupResult = & pg_dump -h $DbHost -p $DbPort -U $DbUser -d $DbName `
        --format=custom `
        --compress=9 `
        --no-owner `
        --no-acl `
        --file="$BackupFile" 2>&1

    if ($LASTEXITCODE -ne 0) {
        throw "pg_dump failed with exit code $LASTEXITCODE"
    }

    $endTime = Get-Date
    $duration = [math]::Round(($endTime - $startTime).TotalSeconds)
    $backupSize = (Get-Item $BackupFile).Length / 1MB
    $backupSizeStr = "{0:N2} MB" -f $backupSize

    Log "✓ Backup completed in ${duration}s ($backupSizeStr)"
} catch {
    Log "ERROR: Backup failed - $_"
    exit 1
}

# Validate backup
Log "Validating backup integrity..."
try {
    $validateResult = & pg_restore --list "$BackupFile" 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Validation failed"
    }
    Log "✓ Backup validation passed"
} catch {
    Log "ERROR: Backup validation FAILED - file may be corrupted"
    exit 1
}

# Create checksum
Log "Creating checksum..."
try {
    $hash = Get-FileHash -Path $BackupFile -Algorithm SHA256
    $checksumFile = "$BackupFile.sha256"
    "$($hash.Hash)  $(Split-Path $BackupFile -Leaf)" | Out-File -FilePath $checksumFile -Encoding ASCII
    Log "✓ Checksum created: $($hash.Hash.Substring(0, 16))..."
} catch {
    Log "Warning: Could not create checksum"
}

Log ""
Log "=========================================="
Log "Backup Complete"
Log "=========================================="
Log "File: $BackupFile"
Log "Size: $backupSizeStr"
Log "Duration: ${duration}s"
Log ""
Log "To restore:"
Log "  pg_restore -h $DbHost -U $DbUser -d $DbName -c $BackupFile"
Log "=========================================="

exit 0
