#!/bin/bash
# T119: Test coverage validation script (Bash version)
# Generates test coverage report and validates against thresholds
# Usage: ./test-coverage-report.sh [threshold_percentage]

set -euo pipefail

THRESHOLD="${1:-70}"
OUTPUT_DIR="coverage"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

echo "=========================================="
echo "Ceiba Test Coverage Report"
echo "=========================================="
echo "Threshold: $THRESHOLD%"
echo ""

# Create output directory
COVERAGE_DIR="$PROJECT_ROOT/$OUTPUT_DIR"
mkdir -p "$COVERAGE_DIR"

# Clean previous coverage data
rm -f "$COVERAGE_DIR"/*.xml "$COVERAGE_DIR"/*.json 2>/dev/null || true

echo "Running tests with coverage collection..."
echo ""

# Run tests with coverage
cd "$PROJECT_ROOT"
dotnet test \
    --no-build \
    --filter "Category!=E2E" \
    --collect:"XPlat Code Coverage" \
    --settings coverlet.runsettings \
    --results-directory "$COVERAGE_DIR" \
    --verbosity minimal 2>&1

if [ $? -ne 0 ]; then
    echo "ERROR: Tests failed"
    exit 1
fi

echo ""
echo "=========================================="
echo "Coverage Collection Complete"
echo "=========================================="

# Find coverage files
COVERAGE_FILES=$(find "$COVERAGE_DIR" -name "coverage.cobertura.xml" 2>/dev/null)

if [ -z "$COVERAGE_FILES" ]; then
    echo "WARNING: No coverage files found."
    echo ""
    echo "To enable coverage collection:"
    echo "  dotnet add package coverlet.collector"
    exit 1
fi

FILE_COUNT=$(echo "$COVERAGE_FILES" | wc -l)
echo "Found $FILE_COUNT coverage file(s)"

# Parse coverage using grep/awk (simple extraction)
TOTAL_COVERAGE=0
FILE_COUNT=0

for FILE in $COVERAGE_FILES; do
    echo "  Processing: $(basename "$FILE")"

    # Extract line-rate from coverage element
    LINE_RATE=$(grep -oP 'coverage[^>]*line-rate="\K[0-9.]+' "$FILE" | head -1)

    if [ -n "$LINE_RATE" ]; then
        COVERAGE_PCT=$(echo "$LINE_RATE * 100" | bc -l 2>/dev/null || echo "0")
        TOTAL_COVERAGE=$(echo "$TOTAL_COVERAGE + $COVERAGE_PCT" | bc -l 2>/dev/null || echo "$COVERAGE_PCT")
        FILE_COUNT=$((FILE_COUNT + 1))
    fi
done

# Calculate average coverage
if [ "$FILE_COUNT" -gt 0 ]; then
    AVG_COVERAGE=$(echo "scale=2; $TOTAL_COVERAGE / $FILE_COUNT" | bc -l 2>/dev/null || echo "0")
else
    AVG_COVERAGE="0"
fi

echo ""
echo "=========================================="
echo "Coverage Summary"
echo "=========================================="
echo "Average Line Coverage: ${AVG_COVERAGE}%"
echo ""

# Check against threshold
PASSED=$(echo "$AVG_COVERAGE >= $THRESHOLD" | bc -l 2>/dev/null || echo "0")

if [ "$PASSED" = "1" ]; then
    echo "✓ PASS: Coverage (${AVG_COVERAGE}%) meets threshold (${THRESHOLD}%)"
else
    echo "✗ FAIL: Coverage (${AVG_COVERAGE}%) is below threshold (${THRESHOLD}%)"
fi

echo ""
echo "Coverage files saved to: $COVERAGE_DIR"

# Generate HTML report if ReportGenerator is available
if command -v reportgenerator &> /dev/null; then
    echo ""
    echo "Generating HTML report..."

    REPORT_DIR="$COVERAGE_DIR/html"
    COVERAGE_XML_FILES=$(echo "$COVERAGE_FILES" | tr '\n' ';')

    reportgenerator \
        -reports:"$COVERAGE_XML_FILES" \
        -targetdir:"$REPORT_DIR" \
        -reporttypes:"Html;HtmlSummary;Badges" \
        -verbosity:"Warning"

    if [ $? -eq 0 ]; then
        echo "✓ HTML report generated: $REPORT_DIR/index.html"
    fi
else
    echo ""
    echo "TIP: Install ReportGenerator for HTML reports:"
    echo "  dotnet tool install -g dotnet-reportgenerator-globaltool"
fi

echo ""
echo "=========================================="

if [ "$PASSED" != "1" ]; then
    exit 1
fi

exit 0
