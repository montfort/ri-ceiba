#!/bin/bash
# T115: Database backup script for Ceiba application
# Creates compressed PostgreSQL backup with timestamp
# Usage: ./backup-database.sh [output_dir]

set -euo pipefail

# Configuration - can be overridden by environment variables
DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-5432}"
DB_NAME="${DB_NAME:-ceiba}"
DB_USER="${DB_USER:-ceiba}"
export PGPASSWORD="${DB_PASSWORD:-ceiba123}"

# Backup configuration
BACKUP_DIR="${1:-backups/daily}"
TIMESTAMP=$(date +%Y%m%d-%H%M%S)
DATE_DIR=$(date +%Y/%m)
BACKUP_FILE="$BACKUP_DIR/$DATE_DIR/ceiba-backup-$TIMESTAMP.sql.gz"
LOG_FILE="$BACKUP_DIR/backup.log"

# Retention policy
KEEP_DAILY=7
KEEP_WEEKLY=4
KEEP_MONTHLY=12

# Create directories
mkdir -p "$BACKUP_DIR/$DATE_DIR"

log() {
    local msg="[$(date '+%Y-%m-%d %H:%M:%S')] $1"
    echo "$msg"
    echo "$msg" >> "$LOG_FILE"
}

error() {
    log "ERROR: $1"
    exit 1
}

log "=========================================="
log "Ceiba Database Backup"
log "=========================================="
log "Host: $DB_HOST:$DB_PORT"
log "Database: $DB_NAME"
log "Backup file: $BACKUP_FILE"
log ""

# Check PostgreSQL connection
log "Testing database connection..."
if ! pg_isready -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" >/dev/null 2>&1; then
    error "Cannot connect to PostgreSQL database"
fi
log "✓ Database connection OK"

# Get database size for progress estimation
DB_SIZE=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -t -c \
    "SELECT pg_size_pretty(pg_database_size('$DB_NAME'))" 2>/dev/null | xargs)
log "Database size: $DB_SIZE"

# Perform backup with progress
log "Creating backup..."
START_TIME=$(date +%s)

if pg_dump -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" \
    --format=custom \
    --compress=9 \
    --verbose \
    --no-owner \
    --no-acl \
    --file="$BACKUP_FILE" 2>&1 | while read -r line; do
        log "  $line"
    done; then

    END_TIME=$(date +%s)
    DURATION=$((END_TIME - START_TIME))
    BACKUP_SIZE=$(du -h "$BACKUP_FILE" | cut -f1)

    log "✓ Backup completed in ${DURATION}s ($BACKUP_SIZE)"
else
    error "Backup failed"
fi

# Validate backup
log "Validating backup integrity..."
if pg_restore --list "$BACKUP_FILE" >/dev/null 2>&1; then
    log "✓ Backup validation passed"
else
    error "Backup validation FAILED - file may be corrupted"
fi

# Create checksum
log "Creating checksum..."
sha256sum "$BACKUP_FILE" > "$BACKUP_FILE.sha256"
log "✓ Checksum created: $(cat "$BACKUP_FILE.sha256" | cut -d' ' -f1)"

# Create symlink to latest backup
ln -sf "$(basename "$BACKUP_FILE")" "$BACKUP_DIR/latest.sql.gz" 2>/dev/null || true

log ""
log "=========================================="
log "Backup Complete"
log "=========================================="
log "File: $BACKUP_FILE"
log "Size: $BACKUP_SIZE"
log "Duration: ${DURATION}s"
log "Checksum: $BACKUP_FILE.sha256"
log ""
log "To restore:"
log "  pg_restore -h $DB_HOST -U $DB_USER -d $DB_NAME -c $BACKUP_FILE"
log "=========================================="

exit 0
