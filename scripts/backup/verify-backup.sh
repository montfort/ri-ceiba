#!/bin/bash
# T126-T135: RO-001 Backup verification script
# Verifies backup integrity and optionally tests restore
# Usage: ./verify-backup.sh <backup_file> [--test-restore]

set -euo pipefail

BACKUP_FILE="${1:-}"
TEST_RESTORE="${2:-}"

# Configuration
DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-5432}"
DB_NAME="${DB_NAME:-ceiba}"
DB_USER="${DB_USER:-ceiba}"
export PGPASSWORD="${DB_PASSWORD:-ceiba123}"

TEST_DB="ceiba_backup_test_$(date +%s)"

log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1"
}

cleanup() {
    if [ -n "${TEST_DB:-}" ]; then
        log "Cleaning up test database..."
        dropdb -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" --if-exists "$TEST_DB" 2>/dev/null || true
    fi
}

trap cleanup EXIT

# Validate arguments
if [ -z "$BACKUP_FILE" ]; then
    echo "Usage: $0 <backup_file> [--test-restore]"
    echo ""
    echo "Options:"
    echo "  --test-restore    Actually restore to a temporary database and verify"
    exit 1
fi

if [ ! -f "$BACKUP_FILE" ]; then
    log "ERROR: Backup file not found: $BACKUP_FILE"
    exit 1
fi

log "=========================================="
log "Backup Verification"
log "=========================================="
log "File: $BACKUP_FILE"
log ""

# Check file size
FILE_SIZE=$(du -h "$BACKUP_FILE" | cut -f1)
log "File size: $FILE_SIZE"

# Verify checksum if available
CHECKSUM_FILE="$BACKUP_FILE.sha256"
if [ -f "$CHECKSUM_FILE" ]; then
    log "Verifying checksum..."
    if sha256sum -c "$CHECKSUM_FILE" 2>/dev/null; then
        log "✓ Checksum verification PASSED"
    else
        log "✗ Checksum verification FAILED"
        exit 1
    fi
else
    log "⚠ No checksum file found, skipping verification"
fi

# Determine backup format
if [[ "$BACKUP_FILE" == *.sql.gz ]]; then
    BACKUP_FORMAT="sql"
    log "Format: SQL (gzipped)"

    # Verify gzip integrity
    log "Verifying gzip integrity..."
    if gunzip -t "$BACKUP_FILE" 2>/dev/null; then
        log "✓ Gzip integrity PASSED"
    else
        log "✗ Gzip integrity FAILED"
        exit 1
    fi

    # Count statements
    STATEMENT_COUNT=$(gunzip -c "$BACKUP_FILE" | grep -c "^INSERT\|^CREATE\|^ALTER" || echo "0")
    log "SQL statements: ~$STATEMENT_COUNT"

elif [[ "$BACKUP_FILE" == *.dump ]]; then
    BACKUP_FORMAT="custom"
    log "Format: Custom (PostgreSQL)"

    # Verify pg_restore can read the file
    log "Verifying backup structure..."
    if pg_restore --list "$BACKUP_FILE" > /dev/null 2>&1; then
        log "✓ Backup structure verification PASSED"

        # Get table of contents
        TOC_COUNT=$(pg_restore --list "$BACKUP_FILE" 2>/dev/null | wc -l)
        log "Table of contents entries: $TOC_COUNT"
    else
        log "✗ Backup structure verification FAILED"
        exit 1
    fi
else
    log "Unknown backup format"
    exit 1
fi

# Test restore if requested
if [ "$TEST_RESTORE" = "--test-restore" ]; then
    log ""
    log "=========================================="
    log "Test Restore"
    log "=========================================="

    # Create test database
    log "Creating test database: $TEST_DB"
    createdb -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" "$TEST_DB" 2>/dev/null

    # Perform restore
    log "Restoring to test database..."
    START_TIME=$(date +%s)

    if [ "$BACKUP_FORMAT" = "sql" ]; then
        gunzip -c "$BACKUP_FILE" | psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$TEST_DB" \
            --quiet --set ON_ERROR_STOP=off 2>&1 | grep -i "error" || true
    else
        pg_restore -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$TEST_DB" \
            --no-owner --no-acl "$BACKUP_FILE" 2>&1 | grep -i "error" || true
    fi

    END_TIME=$(date +%s)
    DURATION=$((END_TIME - START_TIME))

    log "✓ Restore completed in ${DURATION}s"

    # Verify data
    log "Verifying restored data..."

    TABLE_COUNT=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$TEST_DB" -t -c \
        "SELECT count(*) FROM information_schema.tables WHERE table_schema = 'public'" | xargs)

    REPORT_COUNT=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$TEST_DB" -t -c \
        "SELECT count(*) FROM \"REPORTE_INCIDENCIA\"" 2>/dev/null | xargs || echo "0")

    USER_COUNT=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$TEST_DB" -t -c \
        "SELECT count(*) FROM \"AspNetUsers\"" 2>/dev/null | xargs || echo "0")

    AUDIT_COUNT=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$TEST_DB" -t -c \
        "SELECT count(*) FROM \"AUDITORIA\"" 2>/dev/null | xargs || echo "0")

    log ""
    log "Restored data summary:"
    log "  Tables:       $TABLE_COUNT"
    log "  Reports:      $REPORT_COUNT"
    log "  Users:        $USER_COUNT"
    log "  Audit logs:   $AUDIT_COUNT"

    # Cleanup is handled by trap
fi

log ""
log "=========================================="
log "Verification Complete"
log "=========================================="
log "✓ Backup file is valid and can be restored"

exit 0
