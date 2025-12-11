#!/bin/bash
# SonarQube Cloud Issues Fetcher
# Fetches issues from SonarCloud API for analysis
#
# Usage:
#   SONAR_TOKEN=your_token ./fetch-issues.sh [options]
#
# Options:
#   --type TYPE        Filter by type: BUG, VULNERABILITY, CODE_SMELL, SECURITY_HOTSPOT
#   --severity SEV     Filter by severity: BLOCKER, CRITICAL, MAJOR, MINOR, INFO
#   --branch BRANCH    Branch to analyze (default: current git branch)
#   --format FORMAT    Output format: json, summary, markdown (default: json)
#   --output FILE      Write output to file instead of stdout
#
# Environment:
#   SONAR_TOKEN        Required. SonarCloud API token
#   SONAR_ORG          Organization key (default: montfort)
#   SONAR_PROJECT      Project key (default: montfort_ri-ceiba)

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m'

# Configuration
SONAR_TOKEN="${SONAR_TOKEN:-}"
SONAR_ORG="${SONAR_ORG:-montfort}"
SONAR_PROJECT="${SONAR_PROJECT:-montfort_ri-ceiba}"
SONAR_API="https://sonarcloud.io/api"

# Default options
TYPE=""
SEVERITY=""
BRANCH=""
FORMAT="json"
OUTPUT=""

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --type)
            TYPE="$2"
            shift 2
            ;;
        --severity)
            SEVERITY="$2"
            shift 2
            ;;
        --branch)
            BRANCH="$2"
            shift 2
            ;;
        --format)
            FORMAT="$2"
            shift 2
            ;;
        --output)
            OUTPUT="$2"
            shift 2
            ;;
        --help)
            head -25 "$0" | tail -22
            exit 0
            ;;
        *)
            echo -e "${RED}Unknown option: $1${NC}" >&2
            exit 1
            ;;
    esac
done

# Validate token
if [ -z "$SONAR_TOKEN" ]; then
    echo -e "${RED}ERROR: SONAR_TOKEN environment variable is required${NC}" >&2
    echo "Generate a token at: https://sonarcloud.io/account/security" >&2
    exit 1
fi

# Get current branch if not specified
if [ -z "$BRANCH" ]; then
    BRANCH=$(git branch --show-current 2>/dev/null || echo "main")
fi

# Build query parameters
PARAMS="componentKeys=${SONAR_PROJECT}&organization=${SONAR_ORG}&branch=${BRANCH}&resolved=false&ps=500"

if [ -n "$TYPE" ]; then
    PARAMS="${PARAMS}&types=${TYPE}"
fi

if [ -n "$SEVERITY" ]; then
    PARAMS="${PARAMS}&severities=${SEVERITY}"
fi

# Fetch issues
fetch_issues() {
    curl -s "${SONAR_API}/issues/search?${PARAMS}&facets=types,severities,rules" \
        -H "Authorization: Bearer ${SONAR_TOKEN}"
}

# Format as summary
format_summary() {
    local data="$1"

    echo -e "${CYAN}========================================${NC}"
    echo -e "${CYAN}SonarQube Cloud Issues Summary${NC}"
    echo -e "${CYAN}========================================${NC}"
    echo ""
    echo -e "Project: ${SONAR_PROJECT}"
    echo -e "Branch:  ${BRANCH}"
    echo -e "Date:    $(date '+%Y-%m-%d %H:%M:%S')"
    echo ""

    local total=$(echo "$data" | jq -r '.total // 0')
    echo -e "Total Issues: ${YELLOW}${total}${NC}"
    echo ""

    echo -e "${YELLOW}By Type:${NC}"
    echo "$data" | jq -r '.facets[] | select(.property == "types") | .values[] | "  \(.val): \(.count)"' 2>/dev/null || echo "  (no data)"
    echo ""

    echo -e "${YELLOW}By Severity:${NC}"
    echo "$data" | jq -r '.facets[] | select(.property == "severities") | .values[] | "  \(.val): \(.count)"' 2>/dev/null || echo "  (no data)"
    echo ""

    echo -e "${YELLOW}Top 10 Rules:${NC}"
    echo "$data" | jq -r '.facets[] | select(.property == "rules") | .values[:10][] | "  \(.val): \(.count)"' 2>/dev/null || echo "  (no data)"
}

# Format as markdown
format_markdown() {
    local data="$1"

    local total=$(echo "$data" | jq -r '.total // 0')

    echo "# SonarQube Cloud Issues Report"
    echo ""
    echo "**Project:** ${SONAR_PROJECT}"
    echo "**Branch:** ${BRANCH}"
    echo "**Date:** $(date '+%Y-%m-%d %H:%M:%S')"
    echo "**Total Issues:** ${total}"
    echo ""

    echo "## Summary by Type"
    echo ""
    echo "| Type | Count |"
    echo "|------|-------|"
    echo "$data" | jq -r '.facets[] | select(.property == "types") | .values[] | "| \(.val) | \(.count) |"' 2>/dev/null
    echo ""

    echo "## Summary by Severity"
    echo ""
    echo "| Severity | Count |"
    echo "|----------|-------|"
    echo "$data" | jq -r '.facets[] | select(.property == "severities") | .values[] | "| \(.val) | \(.count) |"' 2>/dev/null
    echo ""

    echo "## Issues Detail"
    echo ""
    echo "$data" | jq -r '.issues[] | "### \(.severity): \(.message)\n\n- **File:** \(.component | split(":")[1])\n- **Line:** \(.line // "N/A")\n- **Rule:** \(.rule)\n- **Type:** \(.type)\n- **Effort:** \(.effort // "N/A")\n"' 2>/dev/null
}

# Format as JSON (cleaned up)
format_json() {
    local data="$1"

    echo "$data" | jq '{
        metadata: {
            project: "'"${SONAR_PROJECT}"'",
            branch: "'"${BRANCH}"'",
            timestamp: now | todate,
            total: .total
        },
        summary: {
            byType: ([.facets[] | select(.property == "types") | .values[] | {(.val): .count}] | add // {}),
            bySeverity: ([.facets[] | select(.property == "severities") | .values[] | {(.val): .count}] | add // {})
        },
        issues: [.issues[] | {
            key: .key,
            rule: .rule,
            severity: .severity,
            type: .type,
            file: (.component | split(":")[1]),
            line: .line,
            message: .message,
            effort: .effort,
            debt: .debt,
            tags: .tags
        }]
    }'
}

# Main execution
main() {
    # Check for jq
    if ! command -v jq &> /dev/null; then
        echo -e "${RED}ERROR: jq is required for JSON processing${NC}" >&2
        echo "Install with: apt install jq (Linux) or brew install jq (macOS)" >&2
        exit 1
    fi

    echo -e "${CYAN}Fetching issues from SonarCloud...${NC}" >&2
    local data=$(fetch_issues)

    # Check for API errors
    if echo "$data" | jq -e '.errors' > /dev/null 2>&1; then
        echo -e "${RED}API Error:${NC}" >&2
        echo "$data" | jq -r '.errors[].msg' >&2
        exit 1
    fi

    local output=""
    case "$FORMAT" in
        summary)
            output=$(format_summary "$data")
            ;;
        markdown)
            output=$(format_markdown "$data")
            ;;
        json|*)
            output=$(format_json "$data")
            ;;
    esac

    if [ -n "$OUTPUT" ]; then
        echo "$output" > "$OUTPUT"
        echo -e "${GREEN}Output written to: ${OUTPUT}${NC}" >&2
    else
        echo "$output"
    fi
}

main
