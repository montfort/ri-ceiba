#!/bin/bash
# T126-T135: RO-001 Backup monitoring script
# Monitors backup health and sends alerts if issues are detected
# Usage: ./monitor-backups.sh [--alert-on-failure]

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

# Configuration
BACKUP_ROOT="${BACKUP_ROOT:-$PROJECT_ROOT/backups}"
MAX_BACKUP_AGE_HOURS="${MAX_BACKUP_AGE_HOURS:-26}"  # Alert if no backup in 26 hours
MIN_BACKUP_SIZE_KB="${MIN_BACKUP_SIZE_KB:-10}"      # Alert if backup smaller than 10KB
ALERT_ON_FAILURE="${1:-}"

# Notification settings
NOTIFY_EMAIL="${NOTIFY_EMAIL:-}"
SLACK_WEBHOOK="${SLACK_WEBHOOK:-}"

ISSUES_FOUND=0

log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1"
}

send_alert() {
    local subject="$1"
    local body="$2"

    log "ALERT: $subject"

    # Email notification
    if [ -n "$NOTIFY_EMAIL" ]; then
        echo "$body" | mail -s "[Ceiba Backup] $subject" "$NOTIFY_EMAIL" 2>/dev/null || true
    fi

    # Slack notification
    if [ -n "$SLACK_WEBHOOK" ]; then
        curl -s -X POST -H 'Content-type: application/json' \
            --data "{\"text\":\"🚨 *Ceiba Backup Alert*\n*$subject*\n$body\"}" \
            "$SLACK_WEBHOOK" >/dev/null 2>&1 || true
    fi
}

check_backup_exists() {
    local backup_type="$1"
    local backup_dir="$BACKUP_ROOT/$backup_type"

    if [ ! -d "$backup_dir" ]; then
        log "⚠ No $backup_type backup directory found"
        return 1
    fi

    local latest_backup=$(find "$backup_dir" -name "*.dump" -o -name "*.sql.gz" 2>/dev/null | sort -r | head -1)

    if [ -z "$latest_backup" ]; then
        log "⚠ No $backup_type backups found"
        return 1
    fi

    log "✓ Found $backup_type backup: $(basename "$latest_backup")"
    return 0
}

check_backup_age() {
    local backup_type="$1"
    local max_age_hours="$2"
    local backup_dir="$BACKUP_ROOT/$backup_type"

    local latest_backup=$(find "$backup_dir" -name "*.dump" -o -name "*.sql.gz" 2>/dev/null | sort -r | head -1)

    if [ -z "$latest_backup" ]; then
        return 1
    fi

    local file_age_seconds=$(($(date +%s) - $(stat -c %Y "$latest_backup" 2>/dev/null || stat -f %m "$latest_backup")))
    local file_age_hours=$((file_age_seconds / 3600))

    if [ "$file_age_hours" -gt "$max_age_hours" ]; then
        log "✗ $backup_type backup is ${file_age_hours}h old (max: ${max_age_hours}h)"
        ISSUES_FOUND=$((ISSUES_FOUND + 1))
        return 1
    fi

    log "✓ $backup_type backup is ${file_age_hours}h old (max: ${max_age_hours}h)"
    return 0
}

check_backup_size() {
    local backup_type="$1"
    local min_size_kb="$2"
    local backup_dir="$BACKUP_ROOT/$backup_type"

    local latest_backup=$(find "$backup_dir" -name "*.dump" -o -name "*.sql.gz" 2>/dev/null | sort -r | head -1)

    if [ -z "$latest_backup" ]; then
        return 1
    fi

    local file_size_kb=$(($(stat -c %s "$latest_backup" 2>/dev/null || stat -f %z "$latest_backup") / 1024))

    if [ "$file_size_kb" -lt "$min_size_kb" ]; then
        log "✗ $backup_type backup is only ${file_size_kb}KB (min: ${min_size_kb}KB)"
        ISSUES_FOUND=$((ISSUES_FOUND + 1))
        return 1
    fi

    log "✓ $backup_type backup size: ${file_size_kb}KB"
    return 0
}

check_backup_integrity() {
    local backup_type="$1"
    local backup_dir="$BACKUP_ROOT/$backup_type"

    local latest_backup=$(find "$backup_dir" -name "*.dump" -o -name "*.sql.gz" 2>/dev/null | sort -r | head -1)

    if [ -z "$latest_backup" ]; then
        return 1
    fi

    # Check checksum if available
    local checksum_file="$latest_backup.sha256"
    if [ -f "$checksum_file" ]; then
        if sha256sum -c "$checksum_file" >/dev/null 2>&1; then
            log "✓ $backup_type backup checksum valid"
        else
            log "✗ $backup_type backup checksum INVALID"
            ISSUES_FOUND=$((ISSUES_FOUND + 1))
            return 1
        fi
    fi

    # Verify file integrity
    if [[ "$latest_backup" == *.sql.gz ]]; then
        if ! gunzip -t "$latest_backup" 2>/dev/null; then
            log "✗ $backup_type backup gzip integrity FAILED"
            ISSUES_FOUND=$((ISSUES_FOUND + 1))
            return 1
        fi
    elif [[ "$latest_backup" == *.dump ]]; then
        if ! pg_restore --list "$latest_backup" >/dev/null 2>&1; then
            log "✗ $backup_type backup structure INVALID"
            ISSUES_FOUND=$((ISSUES_FOUND + 1))
            return 1
        fi
    fi

    log "✓ $backup_type backup integrity OK"
    return 0
}

get_backup_stats() {
    local backup_dir="$1"
    local backup_count=$(find "$backup_dir" -name "*.dump" -o -name "*.sql.gz" 2>/dev/null | wc -l)
    local total_size=$(du -sh "$backup_dir" 2>/dev/null | cut -f1)
    echo "$backup_count backups, $total_size total"
}

# Main monitoring
log "=========================================="
log "Ceiba Backup Monitor"
log "=========================================="
log "Backup root: $BACKUP_ROOT"
log ""

# Check daily backups
log "--- Daily Backups ---"
if check_backup_exists "daily"; then
    check_backup_age "daily" "$MAX_BACKUP_AGE_HOURS"
    check_backup_size "daily" "$MIN_BACKUP_SIZE_KB"
    check_backup_integrity "daily"
    log "Stats: $(get_backup_stats "$BACKUP_ROOT/daily")"
else
    ISSUES_FOUND=$((ISSUES_FOUND + 1))
fi

log ""

# Check weekly backups (max 8 days old)
log "--- Weekly Backups ---"
if check_backup_exists "weekly"; then
    check_backup_age "weekly" $((8 * 24))  # 8 days
    check_backup_size "weekly" "$MIN_BACKUP_SIZE_KB"
    check_backup_integrity "weekly"
    log "Stats: $(get_backup_stats "$BACKUP_ROOT/weekly")"
fi

log ""

# Check monthly backups (max 32 days old)
log "--- Monthly Backups ---"
if check_backup_exists "monthly"; then
    check_backup_age "monthly" $((32 * 24))  # 32 days
    check_backup_size "monthly" "$MIN_BACKUP_SIZE_KB"
    check_backup_integrity "monthly"
    log "Stats: $(get_backup_stats "$BACKUP_ROOT/monthly")"
fi

log ""

# Disk space check
log "--- Disk Space ---"
DISK_USAGE=$(df -h "$BACKUP_ROOT" 2>/dev/null | tail -1 | awk '{print $5}' | tr -d '%')
if [ -n "$DISK_USAGE" ]; then
    if [ "$DISK_USAGE" -gt 90 ]; then
        log "✗ Disk usage critical: ${DISK_USAGE}%"
        ISSUES_FOUND=$((ISSUES_FOUND + 1))
    elif [ "$DISK_USAGE" -gt 80 ]; then
        log "⚠ Disk usage high: ${DISK_USAGE}%"
    else
        log "✓ Disk usage: ${DISK_USAGE}%"
    fi
fi

log ""
log "=========================================="
log "Monitor Summary"
log "=========================================="

if [ "$ISSUES_FOUND" -gt 0 ]; then
    log "✗ Found $ISSUES_FOUND issue(s)"

    if [ "$ALERT_ON_FAILURE" = "--alert-on-failure" ]; then
        send_alert "Backup Issues Detected" \
            "Found $ISSUES_FOUND backup issue(s) on $(hostname).\nCheck backup logs for details."
    fi

    exit 1
else
    log "✓ All backup checks passed"
    exit 0
fi
