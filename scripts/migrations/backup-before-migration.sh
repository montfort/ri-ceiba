#!/bin/bash
# T019c: RT-004 Mitigation - Pre-migration backup script
# Creates full database backup before applying migrations
# Usage: ./backup-before-migration.sh [migration_name]

set -euo pipefail

DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-5432}"
DB_NAME="${DB_NAME:-ceiba}"
DB_USER="${DB_USER:-ceiba}"

# DB_PASSWORD is required - no default for security
if [ -z "${DB_PASSWORD:-}" ]; then
    echo "ERROR: DB_PASSWORD environment variable is required"
    echo "Usage: DB_PASSWORD=your_password $0 [migration_name]"
    exit 1
fi
export PGPASSWORD="$DB_PASSWORD"

MIGRATION_NAME="${1:-unknown}"
BACKUP_DIR="${BACKUP_DIR:-backups/migrations}"
TIMESTAMP=$(date +%Y%m%d-%H%M%S)
BACKUP_FILE="$BACKUP_DIR/pre-migration-$MIGRATION_NAME-$TIMESTAMP.sql.gz"

mkdir -p "$BACKUP_DIR"

echo "=========================================="
echo "Pre-Migration Backup"
echo "=========================================="
echo "Migration: $MIGRATION_NAME"
echo "Timestamp: $TIMESTAMP"
echo "Backup file: $BACKUP_FILE"
echo ""

# Perform backup
echo "Creating backup..."
if pg_dump -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" \
    --format=plain --no-owner --no-acl \
    | gzip > "$BACKUP_FILE"; then

    BACKUP_SIZE=$(du -h "$BACKUP_FILE" | cut -f1)
    echo "✓ Backup created successfully ($BACKUP_SIZE)"

    # Validate backup can be read
    echo "Validating backup integrity..."
    if gunzip -t "$BACKUP_FILE" 2>/dev/null; then
        echo "✓ Backup validation passed"
    else
        echo "✗ Backup validation FAILED - file may be corrupted"
        exit 1
    fi

    # Keep last 10 backups only
    echo "Cleaning old backups (keeping last 10)..."
    ls -t "$BACKUP_DIR"/pre-migration-*.sql.gz 2>/dev/null | tail -n +11 | xargs rm -f || true

    echo ""
    echo "=========================================="
    echo "Backup complete: $BACKUP_FILE"
    echo "To restore: gunzip -c $BACKUP_FILE | psql -h $DB_HOST -U $DB_USER -d $DB_NAME"
    echo "=========================================="
    exit 0
else
    echo "✗ Backup FAILED"
    exit 1
fi
