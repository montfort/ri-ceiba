#!/bin/bash
# T020j: RS-002 Mitigation - Zero Raw SQL Policy Check
# Scans codebase for raw SQL queries to prevent SQL injection vulnerabilities
# Exit code 0 = pass, 1 = violations found

set -euo pipefail

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
PROJECT_ROOT="${1:-.}"
VIOLATIONS=0
WARNINGS=0

echo "=================================================="
echo "Zero Raw SQL Policy Check"
echo "T020j: RS-002 Mitigation"
echo "=================================================="
echo ""

# Patterns that indicate raw SQL usage (violations)
declare -a SQL_PATTERNS=(
    'ExecuteSqlRaw'
    'ExecuteSqlRawAsync'
    'FromSqlRaw'
    'FromSqlInterpolated'
    'SqlQuery'
    'Database\.SqlQuery'
    'context\.Database\.ExecuteSqlCommand'
    'SELECT \* FROM'
    'INSERT INTO'
    'UPDATE .* SET'
    'DELETE FROM'
    'DROP TABLE'
    'CREATE TABLE'
    'ALTER TABLE'
)

# Allowed patterns (not violations)
declare -a ALLOWED_PATTERNS=(
    'ExecuteSqlInterpolated'  # Safe: uses parameterized queries
    'FromSqlInterpolated'     # Safe: uses parameterized queries
    '\/\/ APPROVED:'          # Explicit approval comment
    '\/\* APPROVED:'          # Explicit approval comment
)

# File extensions to scan
FILE_EXTENSIONS=("*.cs")

# Directories to exclude
EXCLUDE_DIRS=(
    "*/bin/*"
    "*/obj/*"
    "*/Migrations/*"
    "*/node_modules/*"
    "*/.git/*"
)

# Build exclude pattern for find command
EXCLUDE_PATTERN=""
for dir in "${EXCLUDE_DIRS[@]}"; do
    EXCLUDE_PATTERN="$EXCLUDE_PATTERN -not -path '$dir'"
done

echo "Scanning for raw SQL usage in C# files..."
echo ""

# Function to check if line is approved
is_approved() {
    local line="$1"
    for pattern in "${ALLOWED_PATTERNS[@]}"; do
        if echo "$line" | grep -qE "$pattern"; then
            return 0  # Approved
        fi
    done
    return 1  # Not approved
}

# Function to check if line contains SQL keyword
contains_sql_keyword() {
    local line="$1"
    # SQL keywords that should not appear in string literals
    if echo "$line" | grep -qiE '(SELECT|INSERT|UPDATE|DELETE|DROP|CREATE|ALTER)\s+(FROM|INTO|TABLE|SET)'; then
        return 0  # Contains SQL
    fi
    return 1  # No SQL
}

# Scan each file extension
for ext in "${FILE_EXTENSIONS[@]}"; do
    # Find all matching files, excluding specified directories
    while IFS= read -r file; do
        line_number=0
        while IFS= read -r line; do
            ((line_number++))

            # Check for SQL patterns
            for pattern in "${SQL_PATTERNS[@]}"; do
                if echo "$line" | grep -qE "$pattern"; then
                    # Check if approved
                    if is_approved "$line"; then
                        echo -e "${YELLOW}[APPROVED]${NC} $file:$line_number"
                        echo "  $line"
                        ((WARNINGS++))
                    else
                        echo -e "${RED}[VIOLATION]${NC} $file:$line_number"
                        echo "  $line"
                        ((VIOLATIONS++))
                    fi
                fi
            done

            # Check for SQL keywords in string literals
            if contains_sql_keyword "$line"; then
                # Check if it's in a comment or approved
                if echo "$line" | grep -qE '^\s*(\/\/|\/\*|\*)'; then
                    continue  # Skip comments
                fi

                if is_approved "$line"; then
                    continue  # Skip approved
                fi

                # Check if it's a string concatenation (dangerous)
                if echo "$line" | grep -qE '(\+|string\.Format|String\.Format|\$).*("|\`)'; then
                    echo -e "${RED}[POTENTIAL SQL INJECTION]${NC} $file:$line_number"
                    echo "  $line"
                    ((VIOLATIONS++))
                fi
            fi
        done < "$file"
    done < <(eval find "$PROJECT_ROOT" -type f -name "$ext" $EXCLUDE_PATTERN)
done

echo ""
echo "=================================================="
echo "Scan Complete"
echo "=================================================="
echo -e "Violations: ${RED}$VIOLATIONS${NC}"
echo -e "Approved Raw SQL: ${YELLOW}$WARNINGS${NC}"
echo ""

if [ $VIOLATIONS -gt 0 ]; then
    echo -e "${RED}❌ FAILED: Raw SQL violations detected${NC}"
    echo ""
    echo "To fix violations:"
    echo "1. Use Entity Framework LINQ queries instead of raw SQL"
    echo "2. Use ExecuteSqlInterpolated() or FromSqlInterpolated() with parameterized queries"
    echo "3. Add '// APPROVED: <reason>' comment for necessary raw SQL"
    echo ""
    echo "Example - BAD (SQL Injection Risk):"
    echo "  context.Database.ExecuteSqlRaw(\"SELECT * FROM Users WHERE Name = '\" + userName + \"'\");"
    echo ""
    echo "Example - GOOD (Parameterized):"
    echo "  context.Database.ExecuteSqlInterpolated(\$\"SELECT * FROM Users WHERE Name = {userName}\");"
    echo ""
    echo "Example - BEST (LINQ):"
    echo "  context.Users.Where(u => u.Name == userName).ToList();"
    echo ""
    exit 1
else
    echo -e "${GREEN}✅ PASSED: No raw SQL violations detected${NC}"
    if [ $WARNINGS -gt 0 ]; then
        echo -e "${YELLOW}⚠️  Note: $WARNINGS approved raw SQL usage(s) found${NC}"
        echo "   These are explicitly approved but should be reviewed periodically."
    fi
    echo ""
    exit 0
fi
