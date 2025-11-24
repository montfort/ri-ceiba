#!/bin/bash
# T018e: RS-003 Mitigation - Automated log scanning for sensitive data
# Scans log files for potentially leaked PII and credentials
# Run via cron: 0 2 * * * /path/to/scan-logs-for-sensitive-data.sh

set -euo pipefail

LOG_DIR="${LOG_DIR:-logs}"
REPORT_FILE="logs/security-scan-$(date +%Y%m%d-%H%M%S).txt"
ALERT_THRESHOLD=5  # Alert if more than 5 potential leaks found

echo "========================================" | tee "$REPORT_FILE"
echo "Security Log Scan - $(date)" | tee -a "$REPORT_FILE"
echo "========================================" | tee -a "$REPORT_FILE"
echo "" | tee -a "$REPORT_FILE"

TOTAL_ISSUES=0

# Scan for email addresses (potential PII leak)
echo "[1/5] Scanning for email addresses..." | tee -a "$REPORT_FILE"
EMAIL_COUNT=$(grep -rioE '\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b' "$LOG_DIR" 2>/dev/null | wc -l || echo 0)
if [ "$EMAIL_COUNT" -gt 0 ]; then
    echo "  ⚠️  Found $EMAIL_COUNT potential email leaks" | tee -a "$REPORT_FILE"
    grep -rioE '\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b' "$LOG_DIR" 2>/dev/null | head -10 | tee -a "$REPORT_FILE"
    TOTAL_ISSUES=$((TOTAL_ISSUES + EMAIL_COUNT))
else
    echo "  ✓ No email addresses found" | tee -a "$REPORT_FILE"
fi
echo "" | tee -a "$REPORT_FILE"

# Scan for passwords (various patterns)
echo "[2/5] Scanning for password patterns..." | tee -a "$REPORT_FILE"
PASS_COUNT=$(grep -riE 'password["\s:=]+[^"\s,}]{8,}' "$LOG_DIR" 2>/dev/null | wc -l || echo 0)
if [ "$PASS_COUNT" -gt 0 ]; then
    echo "  ⚠️  Found $PASS_COUNT potential password leaks" | tee -a "$REPORT_FILE"
    grep -riE 'password["\s:=]+' "$LOG_DIR" 2>/dev/null | head -5 | tee -a "$REPORT_FILE"
    TOTAL_ISSUES=$((TOTAL_ISSUES + PASS_COUNT))
else
    echo "  ✓ No password patterns found" | tee -a "$REPORT_FILE"
fi
echo "" | tee -a "$REPORT_FILE"

# Scan for IP addresses (potential PII)
echo "[3/5] Scanning for full IP addresses..." | tee -a "$REPORT_FILE"
IP_COUNT=$(grep -rioE '\b(?:[0-9]{1,3}\.){3}[0-9]{1,3}\b' "$LOG_DIR" 2>/dev/null | grep -v '\*\*\*' | wc -l || echo 0)
if [ "$IP_COUNT" -gt 10 ]; then  # Allow some IPs, but alert on excessive logging
    echo "  ⚠️  Found $IP_COUNT full IP addresses (expected redaction: XXX.***.***.***)" | tee -a "$REPORT_FILE"
    TOTAL_ISSUES=$((TOTAL_ISSUES + IP_COUNT - 10))
else
    echo "  ✓ IP logging appears redacted ($IP_COUNT full IPs)" | tee -a "$REPORT_FILE"
fi
echo "" | tee -a "$REPORT_FILE"

# Scan for credit card patterns (should never appear)
echo "[4/5] Scanning for credit card patterns..." | tee -a "$REPORT_FILE"
CC_COUNT=$(grep -rioE '\b[0-9]{4}[- ]?[0-9]{4}[- ]?[0-9]{4}[- ]?[0-9]{4}\b' "$LOG_DIR" 2>/dev/null | wc -l || echo 0)
if [ "$CC_COUNT" -gt 0 ]; then
    echo "  🚨 CRITICAL: Found $CC_COUNT potential credit card numbers!" | tee -a "$REPORT_FILE"
    TOTAL_ISSUES=$((TOTAL_ISSUES + CC_COUNT * 10))  # Weight heavily
else
    echo "  ✓ No credit card patterns found" | tee -a "$REPORT_FILE"
fi
echo "" | tee -a "$REPORT_FILE"

# Scan for social security / ID numbers (Mexican CURP pattern)
echo "[5/5] Scanning for CURP/RFC patterns..." | tee -a "$REPORT_FILE"
CURP_COUNT=$(grep -rioE '\b[A-Z]{4}[0-9]{6}[HM][A-Z]{5}[0-9A-Z]{2}\b' "$LOG_DIR" 2>/dev/null | wc -l || echo 0)
if [ "$CURP_COUNT" -gt 0 ]; then
    echo "  🚨 CRITICAL: Found $CURP_COUNT potential CURP numbers!" | tee -a "$REPORT_FILE"
    TOTAL_ISSUES=$((TOTAL_ISSUES + CURP_COUNT * 10))  # Weight heavily
else
    echo "  ✓ No CURP patterns found" | tee -a "$REPORT_FILE"
fi
echo "" | tee -a "$REPORT_FILE"

# Summary
echo "========================================" | tee -a "$REPORT_FILE"
echo "SUMMARY" | tee -a "$REPORT_FILE"
echo "========================================" | tee -a "$REPORT_FILE"
echo "Total potential issues: $TOTAL_ISSUES" | tee -a "$REPORT_FILE"

if [ "$TOTAL_ISSUES" -gt "$ALERT_THRESHOLD" ]; then
    echo "⚠️  ALERT: Threshold exceeded ($TOTAL_ISSUES > $ALERT_THRESHOLD)" | tee -a "$REPORT_FILE"
    echo "Review PIIRedactionEnricher configuration" | tee -a "$REPORT_FILE"

    # Send alert email (configure SMTP settings)
    if command -v mail &> /dev/null; then
        echo "Sending alert email to admin..." | tee -a "$REPORT_FILE"
        mail -s "⚠️ Ceiba Log Security Scan Alert" admin@ceiba.local < "$REPORT_FILE" || echo "Failed to send email"
    fi

    exit 1
else
    echo "✓ Log security scan passed" | tee -a "$REPORT_FILE"
    exit 0
fi
