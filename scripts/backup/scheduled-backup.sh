#!/bin/bash
# T115a: Automated backup cron job script for Ceiba application
# Designed to be run by cron scheduler
# Usage: Add to crontab: 0 2 * * * /path/to/scheduled-backup.sh
#
# Crontab examples:
#   Daily at 2 AM:     0 2 * * * /opt/ceiba/scripts/backup/scheduled-backup.sh
#   Every 6 hours:     0 */6 * * * /opt/ceiba/scripts/backup/scheduled-backup.sh
#   Weekly (Sunday 3 AM): 0 3 * * 0 /opt/ceiba/scripts/backup/scheduled-backup.sh weekly

set -euo pipefail

# Script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

# Configuration
BACKUP_TYPE="${1:-daily}"  # daily, weekly, or monthly
BACKUP_ROOT="${BACKUP_ROOT:-$PROJECT_ROOT/backups}"
LOG_DIR="$BACKUP_ROOT/logs"
LOG_FILE="$LOG_DIR/scheduled-backup-$(date +%Y%m).log"

# Retention policies (days to keep)
KEEP_DAILY=7
KEEP_WEEKLY=28
KEEP_MONTHLY=365

# Notification settings (optional)
NOTIFY_EMAIL="${NOTIFY_EMAIL:-}"
NOTIFY_ON_SUCCESS="${NOTIFY_ON_SUCCESS:-false}"
NOTIFY_ON_FAILURE="${NOTIFY_ON_FAILURE:-true}"

# Docker settings (if running in Docker)
DOCKER_CONTAINER="${DOCKER_CONTAINER:-ceiba-db}"
USE_DOCKER="${USE_DOCKER:-false}"

# Create directories
mkdir -p "$LOG_DIR"
mkdir -p "$BACKUP_ROOT/$BACKUP_TYPE"

log() {
    local msg="[$(date '+%Y-%m-%d %H:%M:%S')] [$BACKUP_TYPE] $1"
    echo "$msg" >> "$LOG_FILE"
    echo "$msg"
}

send_notification() {
    local subject="$1"
    local body="$2"

    if [ -n "$NOTIFY_EMAIL" ]; then
        echo "$body" | mail -s "$subject" "$NOTIFY_EMAIL" 2>/dev/null || true
    fi
}

cleanup_old_backups() {
    local backup_dir="$1"
    local keep_days="$2"

    log "Cleaning up backups older than $keep_days days in $backup_dir..."

    find "$backup_dir" -name "*.dump" -mtime +$keep_days -delete 2>/dev/null || true
    find "$backup_dir" -name "*.sql.gz" -mtime +$keep_days -delete 2>/dev/null || true
    find "$backup_dir" -name "*.sha256" -mtime +$keep_days -delete 2>/dev/null || true

    # Remove empty directories
    find "$backup_dir" -type d -empty -delete 2>/dev/null || true

    local remaining=$(find "$backup_dir" -name "*.dump" -o -name "*.sql.gz" 2>/dev/null | wc -l)
    log "Cleanup complete. $remaining backups remaining."
}

# Start backup process
log "=========================================="
log "Starting scheduled backup ($BACKUP_TYPE)"
log "=========================================="

START_TIME=$(date +%s)
BACKUP_SUCCESS=false
BACKUP_FILE=""

# Load environment if available
if [ -f "$PROJECT_ROOT/.env" ]; then
    export $(grep -v '^#' "$PROJECT_ROOT/.env" | xargs)
    log "Loaded environment from .env"
fi

# Perform backup
if [ "$USE_DOCKER" = "true" ]; then
    log "Using Docker container: $DOCKER_CONTAINER"

    # Docker-based backup
    TIMESTAMP=$(date +%Y%m%d-%H%M%S)
    BACKUP_FILE="$BACKUP_ROOT/$BACKUP_TYPE/ceiba-$BACKUP_TYPE-$TIMESTAMP.dump"

    if docker exec "$DOCKER_CONTAINER" pg_dump -U ceiba -d ceiba \
        --format=custom --compress=9 > "$BACKUP_FILE" 2>&1; then
        BACKUP_SUCCESS=true
    fi
else
    # Standard backup using backup script
    log "Using standard backup script"

    if "$SCRIPT_DIR/backup-database.sh" "$BACKUP_ROOT/$BACKUP_TYPE" 2>&1 | tee -a "$LOG_FILE"; then
        BACKUP_SUCCESS=true
        # Find the latest backup file
        BACKUP_FILE=$(find "$BACKUP_ROOT/$BACKUP_TYPE" -name "*.dump" -o -name "*.sql.gz" 2>/dev/null |
            sort -r | head -1)
    fi
fi

END_TIME=$(date +%s)
DURATION=$((END_TIME - START_TIME))

if [ "$BACKUP_SUCCESS" = true ] && [ -n "$BACKUP_FILE" ] && [ -f "$BACKUP_FILE" ]; then
    BACKUP_SIZE=$(du -h "$BACKUP_FILE" | cut -f1)

    log "✓ Backup completed successfully"
    log "  File: $BACKUP_FILE"
    log "  Size: $BACKUP_SIZE"
    log "  Duration: ${DURATION}s"

    # Create checksum
    sha256sum "$BACKUP_FILE" > "$BACKUP_FILE.sha256"

    # Run cleanup based on backup type
    case "$BACKUP_TYPE" in
        daily)
            cleanup_old_backups "$BACKUP_ROOT/daily" "$KEEP_DAILY"
            ;;
        weekly)
            cleanup_old_backups "$BACKUP_ROOT/weekly" "$KEEP_WEEKLY"
            ;;
        monthly)
            cleanup_old_backups "$BACKUP_ROOT/monthly" "$KEEP_MONTHLY"
            ;;
    esac

    # Send success notification if enabled
    if [ "$NOTIFY_ON_SUCCESS" = "true" ]; then
        send_notification \
            "[Ceiba] Backup Success: $BACKUP_TYPE" \
            "Backup completed successfully.\n\nFile: $BACKUP_FILE\nSize: $BACKUP_SIZE\nDuration: ${DURATION}s"
    fi

    log "=========================================="
    log "Scheduled backup completed"
    log "=========================================="
    exit 0
else
    log "✗ Backup FAILED"

    # Send failure notification
    if [ "$NOTIFY_ON_FAILURE" = "true" ]; then
        send_notification \
            "[Ceiba] BACKUP FAILED: $BACKUP_TYPE" \
            "Scheduled backup failed!\n\nType: $BACKUP_TYPE\nTime: $(date)\nDuration: ${DURATION}s\n\nCheck logs: $LOG_FILE"
    fi

    log "=========================================="
    log "Scheduled backup FAILED"
    log "=========================================="
    exit 1
fi
