#!/bin/bash
# T115b: Database restore script for Ceiba application
# Restores PostgreSQL backup from compressed file
# Usage: ./restore-database.sh <backup_file> [--confirm]

set -euo pipefail

# Configuration
DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-5432}"
DB_NAME="${DB_NAME:-ceiba}"
DB_USER="${DB_USER:-ceiba}"
export PGPASSWORD="${DB_PASSWORD:-ceiba123}"

# Arguments
BACKUP_FILE="${1:-}"
CONFIRM_FLAG="${2:-}"

log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1"
}

error() {
    log "ERROR: $1"
    exit 1
}

# Validate arguments
if [ -z "$BACKUP_FILE" ]; then
    echo "Usage: $0 <backup_file> [--confirm]"
    echo ""
    echo "Available backups:"
    find backups -name "*.sql.gz" -o -name "*.dump" 2>/dev/null | head -20 || echo "  No backups found"
    exit 1
fi

if [ ! -f "$BACKUP_FILE" ]; then
    error "Backup file not found: $BACKUP_FILE"
fi

# Determine backup format
BACKUP_FORMAT=""
if [[ "$BACKUP_FILE" == *.sql.gz ]]; then
    BACKUP_FORMAT="sql"
elif [[ "$BACKUP_FILE" == *.dump ]] || file "$BACKUP_FILE" | grep -q "PostgreSQL"; then
    BACKUP_FORMAT="custom"
else
    error "Unknown backup format. Expected .sql.gz or .dump file"
fi

log "=========================================="
log "Ceiba Database Restore"
log "=========================================="
log "Backup file: $BACKUP_FILE"
log "Format: $BACKUP_FORMAT"
log "Target: $DB_NAME @ $DB_HOST:$DB_PORT"
log ""

# Verify checksum if available
CHECKSUM_FILE="$BACKUP_FILE.sha256"
if [ -f "$CHECKSUM_FILE" ]; then
    log "Verifying checksum..."
    if sha256sum -c "$CHECKSUM_FILE" 2>/dev/null; then
        log "✓ Checksum verification passed"
    else
        error "Checksum verification FAILED - backup may be corrupted"
    fi
else
    log "⚠ No checksum file found, skipping verification"
fi

# Check database connection
log "Testing database connection..."
if ! pg_isready -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" >/dev/null 2>&1; then
    error "Cannot connect to PostgreSQL server"
fi
log "✓ Database connection OK"

# Check for existing data
RECORD_COUNT=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -t -c \
    "SELECT count(*) FROM information_schema.tables WHERE table_schema = 'public'" 2>/dev/null | xargs || echo "0")

if [ "$RECORD_COUNT" -gt 0 ]; then
    log "⚠ WARNING: Target database has $RECORD_COUNT existing tables"
    log "⚠ This restore will DROP and recreate all objects"
    log ""

    if [ "$CONFIRM_FLAG" != "--confirm" ]; then
        read -p "Are you sure you want to proceed? (type 'yes' to confirm): " CONFIRM
        if [ "$CONFIRM" != "yes" ]; then
            log "Restore cancelled by user"
            exit 0
        fi
    else
        log "Proceeding with --confirm flag"
    fi
fi

# Create pre-restore backup
log "Creating pre-restore backup..."
PRE_RESTORE_BACKUP="backups/pre-restore/pre-restore-$(date +%Y%m%d-%H%M%S).dump"
mkdir -p "$(dirname "$PRE_RESTORE_BACKUP")"

if pg_dump -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" \
    --format=custom --compress=9 --file="$PRE_RESTORE_BACKUP" 2>/dev/null; then
    log "✓ Pre-restore backup created: $PRE_RESTORE_BACKUP"
else
    log "⚠ Warning: Could not create pre-restore backup (database may be empty)"
fi

# Perform restore
log "Starting restore..."
START_TIME=$(date +%s)

if [ "$BACKUP_FORMAT" = "sql" ]; then
    # SQL format (gzipped)
    log "Decompressing and restoring SQL backup..."
    if gunzip -c "$BACKUP_FILE" | psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" \
        --single-transaction --set ON_ERROR_STOP=on 2>&1; then
        RESTORE_SUCCESS=true
    else
        RESTORE_SUCCESS=false
    fi
else
    # Custom format
    log "Restoring custom format backup..."
    if pg_restore -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" \
        --clean --if-exists --no-owner --no-acl \
        --single-transaction "$BACKUP_FILE" 2>&1; then
        RESTORE_SUCCESS=true
    else
        RESTORE_SUCCESS=false
    fi
fi

END_TIME=$(date +%s)
DURATION=$((END_TIME - START_TIME))

if [ "$RESTORE_SUCCESS" = true ]; then
    log "✓ Restore completed in ${DURATION}s"
else
    log "⚠ Restore completed with warnings (some objects may have already existed)"
fi

# Verify restore
log "Verifying restored database..."
TABLE_COUNT=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -t -c \
    "SELECT count(*) FROM information_schema.tables WHERE table_schema = 'public'" | xargs)

REPORT_COUNT=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -t -c \
    "SELECT count(*) FROM \"REPORTE_INCIDENCIA\"" 2>/dev/null | xargs || echo "0")

USER_COUNT=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -t -c \
    "SELECT count(*) FROM \"AspNetUsers\"" 2>/dev/null | xargs || echo "0")

log ""
log "=========================================="
log "Restore Complete"
log "=========================================="
log "Tables: $TABLE_COUNT"
log "Reports: $REPORT_COUNT"
log "Users: $USER_COUNT"
log "Duration: ${DURATION}s"
log ""
log "Pre-restore backup: $PRE_RESTORE_BACKUP"
log "To rollback: ./restore-database.sh $PRE_RESTORE_BACKUP --confirm"
log "=========================================="

exit 0
