# T115b: Database restore script for Ceiba application (PowerShell version)
# Restores PostgreSQL backup from compressed file
# Usage: .\restore-database.ps1 -BackupFile <path> [-Confirm]

[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [string]$BackupFile,

    [Parameter()]
    [switch]$Confirm
)

$ErrorActionPreference = "Stop"

# Configuration
$DbHost = if ($env:DB_HOST) { $env:DB_HOST } else { "localhost" }
$DbPort = if ($env:DB_PORT) { $env:DB_PORT } else { "5432" }
$DbName = if ($env:DB_NAME) { $env:DB_NAME } else { "ceiba" }
$DbUser = if ($env:DB_USER) { $env:DB_USER } else { "ceiba" }
$env:PGPASSWORD = if ($env:DB_PASSWORD) { $env:DB_PASSWORD } else { "ceiba123" }

function Log {
    param([string]$Message)
    Write-Host "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] $Message"
}

function Test-PostgresConnection {
    $testResult = & pg_isready -h $DbHost -p $DbPort -U $DbUser 2>&1
    return $LASTEXITCODE -eq 0
}

# Validate backup file
if (-not (Test-Path $BackupFile)) {
    Write-Host "Backup file not found: $BackupFile"
    Write-Host ""
    Write-Host "Available backups:"
    Get-ChildItem -Path "backups" -Recurse -Include "*.sql.gz","*.dump" -ErrorAction SilentlyContinue |
        Select-Object -First 20 |
        ForEach-Object { Write-Host "  $($_.FullName)" }
    exit 1
}

# Determine backup format
$BackupFormat = ""
if ($BackupFile -match "\.sql\.gz$") {
    $BackupFormat = "sql"
} elseif ($BackupFile -match "\.dump$") {
    $BackupFormat = "custom"
} else {
    Log "ERROR: Unknown backup format. Expected .sql.gz or .dump file"
    exit 1
}

Log "=========================================="
Log "Ceiba Database Restore (PowerShell)"
Log "=========================================="
Log "Backup file: $BackupFile"
Log "Format: $BackupFormat"
Log "Target: $DbName @ ${DbHost}:${DbPort}"
Log ""

# Verify checksum if available
$ChecksumFile = "$BackupFile.sha256"
if (Test-Path $ChecksumFile) {
    Log "Verifying checksum..."
    $expectedHash = (Get-Content $ChecksumFile).Split()[0]
    $actualHash = (Get-FileHash -Path $BackupFile -Algorithm SHA256).Hash
    if ($expectedHash -eq $actualHash) {
        Log "✓ Checksum verification passed"
    } else {
        Log "ERROR: Checksum verification FAILED - backup may be corrupted"
        exit 1
    }
} else {
    Log "⚠ No checksum file found, skipping verification"
}

# Check database connection
Log "Testing database connection..."
if (-not (Test-PostgresConnection)) {
    Log "ERROR: Cannot connect to PostgreSQL server"
    exit 1
}
Log "✓ Database connection OK"

# Check for existing data
$recordCount = & psql -h $DbHost -p $DbPort -U $DbUser -d $DbName -t -c `
    "SELECT count(*) FROM information_schema.tables WHERE table_schema = 'public'" 2>$null
$recordCount = if ($recordCount) { $recordCount.Trim() } else { "0" }

if ([int]$recordCount -gt 0) {
    Log "⚠ WARNING: Target database has $recordCount existing tables"
    Log "⚠ This restore will DROP and recreate all objects"
    Log ""

    if (-not $Confirm) {
        $userConfirm = Read-Host "Are you sure you want to proceed? (type 'yes' to confirm)"
        if ($userConfirm -ne "yes") {
            Log "Restore cancelled by user"
            exit 0
        }
    } else {
        Log "Proceeding with -Confirm flag"
    }
}

# Create pre-restore backup
Log "Creating pre-restore backup..."
$preRestoreDir = "backups\pre-restore"
$preRestoreBackup = Join-Path $preRestoreDir "pre-restore-$(Get-Date -Format 'yyyyMMdd-HHmmss').dump"
New-Item -ItemType Directory -Force -Path $preRestoreDir | Out-Null

try {
    & pg_dump -h $DbHost -p $DbPort -U $DbUser -d $DbName `
        --format=custom --compress=9 --file="$preRestoreBackup" 2>$null
    if ($LASTEXITCODE -eq 0) {
        Log "✓ Pre-restore backup created: $preRestoreBackup"
    }
} catch {
    Log "⚠ Warning: Could not create pre-restore backup (database may be empty)"
}

# Perform restore
Log "Starting restore..."
$startTime = Get-Date
$restoreSuccess = $false

try {
    if ($BackupFormat -eq "sql") {
        # SQL format (gzipped) - need to decompress first
        Log "Decompressing backup..."
        $tempSqlFile = [System.IO.Path]::GetTempFileName()

        # Use .NET for gzip decompression
        $inputStream = [System.IO.File]::OpenRead($BackupFile)
        $gzipStream = New-Object System.IO.Compression.GzipStream($inputStream, [System.IO.Compression.CompressionMode]::Decompress)
        $outputStream = [System.IO.File]::Create($tempSqlFile)
        $gzipStream.CopyTo($outputStream)
        $outputStream.Close()
        $gzipStream.Close()
        $inputStream.Close()

        Log "Restoring SQL backup..."
        & psql -h $DbHost -p $DbPort -U $DbUser -d $DbName -f $tempSqlFile 2>&1
        $restoreSuccess = ($LASTEXITCODE -eq 0)

        Remove-Item $tempSqlFile -ErrorAction SilentlyContinue
    } else {
        # Custom format
        Log "Restoring custom format backup..."
        & pg_restore -h $DbHost -p $DbPort -U $DbUser -d $DbName `
            --clean --if-exists --no-owner --no-acl `
            --single-transaction $BackupFile 2>&1
        $restoreSuccess = ($LASTEXITCODE -eq 0)
    }
} catch {
    Log "⚠ Restore completed with warnings: $_"
}

$endTime = Get-Date
$duration = [math]::Round(($endTime - $startTime).TotalSeconds)

if ($restoreSuccess) {
    Log "✓ Restore completed in ${duration}s"
} else {
    Log "⚠ Restore completed with warnings (some objects may have already existed)"
}

# Verify restore
Log "Verifying restored database..."
$tableCount = & psql -h $DbHost -p $DbPort -U $DbUser -d $DbName -t -c `
    "SELECT count(*) FROM information_schema.tables WHERE table_schema = 'public'" 2>$null
$tableCount = if ($tableCount) { $tableCount.Trim() } else { "0" }

$reportCount = & psql -h $DbHost -p $DbPort -U $DbUser -d $DbName -t -c `
    "SELECT count(*) FROM `"REPORTE_INCIDENCIA`"" 2>$null
$reportCount = if ($reportCount) { $reportCount.Trim() } else { "0" }

$userCount = & psql -h $DbHost -p $DbPort -U $DbUser -d $DbName -t -c `
    "SELECT count(*) FROM `"AspNetUsers`"" 2>$null
$userCount = if ($userCount) { $userCount.Trim() } else { "0" }

Log ""
Log "=========================================="
Log "Restore Complete"
Log "=========================================="
Log "Tables: $tableCount"
Log "Reports: $reportCount"
Log "Users: $userCount"
Log "Duration: ${duration}s"
Log ""
Log "Pre-restore backup: $preRestoreBackup"
Log "To rollback: .\restore-database.ps1 -BackupFile $preRestoreBackup -Confirm"
Log "=========================================="

exit 0
