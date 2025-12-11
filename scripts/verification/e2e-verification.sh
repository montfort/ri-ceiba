#!/bin/bash
# E2E Verification Script for Ceiba - Reportes de Incidencias
# This script verifies all critical flows are working correctly
# Run with: ./scripts/verification/e2e-verification.sh

set -e

# Configuration
BASE_URL="${BASE_URL:-https://localhost:5001}"
DB_HOST="${DB_HOST:-localhost}"
DB_NAME="${DB_NAME:-ceiba}"
DB_USER="${DB_USER:-ceiba}"

# DB_PASSWORD is required - no default for security
if [ -z "${DB_PASSWORD:-}" ]; then
    echo "ERROR: DB_PASSWORD environment variable is required"
    echo "Usage: DB_PASSWORD=your_password $0"
    exit 1
fi

# SSL certificate validation - set to "true" to skip validation (development only)
# WARNING: Only use SKIP_SSL_VERIFY=true for localhost with self-signed certificates
SKIP_SSL_VERIFY="${SKIP_SSL_VERIFY:-false}"
CURL_SSL_OPTS=""
if [ "$SKIP_SSL_VERIFY" = "true" ]; then
    CURL_SSL_OPTS="-k"
    echo "WARNING: SSL certificate validation is disabled (SKIP_SSL_VERIFY=true)"
fi

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Counters
PASSED=0
FAILED=0
SKIPPED=0

# Test results array
declare -a FAILED_TESTS

print_header() {
    echo -e "\n${CYAN}========================================${NC}"
    echo -e "${CYAN}$1${NC}"
    echo -e "${CYAN}========================================${NC}"
}

print_section() {
    echo -e "\n${YELLOW}--- $1 ---${NC}"
}

pass() {
    echo -e "${GREEN}[PASS]${NC} $1"
    ((PASSED++))
}

fail() {
    echo -e "${RED}[FAIL]${NC} $1"
    if [ -n "$2" ]; then
        echo -e "${RED}       $2${NC}"
    fi
    ((FAILED++))
    FAILED_TESTS+=("$1: $2")
}

skip() {
    echo -e "${YELLOW}[SKIP]${NC} $1"
    if [ -n "$2" ]; then
        echo -e "${YELLOW}       $2${NC}"
    fi
    ((SKIPPED++))
}

# HTTP request helper
# Uses CURL_SSL_OPTS which is set based on SKIP_SSL_VERIFY environment variable
http_get() {
    curl -s $CURL_SSL_OPTS -o /dev/null -w "%{http_code}" --max-time 10 "$1" 2>/dev/null || echo "000"
}

http_get_body() {
    curl -s $CURL_SSL_OPTS --max-time 10 "$1" 2>/dev/null || echo ""
}

print_header "CEIBA - E2E Verification Script"
echo "Base URL: $BASE_URL"
echo "Database: $DB_HOST/$DB_NAME"
echo "Started: $(date '+%Y-%m-%d %H:%M:%S')"

# ============================================
# SECTION 1: Application Health
# ============================================
print_section "1. Application Health Checks"

# Test 1.1: Application is running
status=$(http_get "$BASE_URL")
if [ "$status" = "200" ]; then
    pass "1.1 Application is accessible"
else
    fail "1.1 Application is accessible" "Status code: $status"
fi

# Test 1.2: Login page loads
status=$(http_get "$BASE_URL/login")
body=$(http_get_body "$BASE_URL/login")
if [ "$status" = "200" ] && echo "$body" | grep -qi "Iniciar"; then
    pass "1.2 Login page loads correctly"
else
    fail "1.2 Login page loads correctly" "Status: $status"
fi

# Test 1.3: Blazor framework loads
status=$(http_get "$BASE_URL/_framework/blazor.web.js")
if [ "$status" = "200" ]; then
    pass "1.3 Blazor framework loads"
else
    fail "1.3 Blazor framework loads" "Status: $status"
fi

# ============================================
# SECTION 2: API Endpoints
# ============================================
print_section "2. API Endpoint Availability"

# Test 2.1: Zonas endpoint
status=$(http_get "$BASE_URL/api/catalogs/zonas")
if [ "$status" = "200" ]; then
    pass "2.1 Zonas endpoint"
else
    fail "2.1 Zonas endpoint" "Status: $status"
fi

# Test 2.2: Suggestions endpoint
status=$(http_get "$BASE_URL/api/catalogs/suggestions/Sexo")
if [ "$status" = "200" ]; then
    pass "2.2 Suggestions endpoint"
else
    fail "2.2 Suggestions endpoint" "Status: $status"
fi

# Test 2.3: Audit endpoint (should require auth)
status=$(http_get "$BASE_URL/api/admin/audit")
if [ "$status" = "401" ] || [ "$status" = "403" ]; then
    pass "2.3 Audit endpoint protected (401/403)"
elif [ "$status" = "200" ]; then
    pass "2.3 Audit endpoint"
else
    fail "2.3 Audit endpoint" "Status: $status"
fi

# Test 2.4: Automated reports endpoint
status=$(http_get "$BASE_URL/api/automated-reports")
if [ "$status" = "200" ] || [ "$status" = "401" ]; then
    pass "2.4 Automated reports endpoint"
else
    fail "2.4 Automated reports endpoint" "Status: $status"
fi

# ============================================
# SECTION 3: Database Verification
# ============================================
print_section "3. Database Verification"

if ! command -v psql &> /dev/null; then
    skip "3.x Database tests" "psql not found in PATH"
else
    export PGPASSWORD="$DB_PASSWORD"

    # Test 3.1: Database connection
    if psql -h "$DB_HOST" -U "$DB_USER" -d "$DB_NAME" -c "SELECT 1" &> /dev/null; then
        pass "3.1 Database connection successful"
    else
        fail "3.1 Database connection successful" "Cannot connect to database"
    fi

    # Test 3.2: Required tables exist
    tables=("AspNetUsers" "AspNetRoles" "REPORTE_INCIDENCIA" "ZONA" "SECTOR" "CUADRANTE" "AUDITORIA" "REPORTE_AUTOMATIZADO")
    for table in "${tables[@]}"; do
        result=$(psql -h "$DB_HOST" -U "$DB_USER" -d "$DB_NAME" -t -c "SELECT EXISTS (SELECT FROM information_schema.tables WHERE table_name = '${table,,}')" 2>/dev/null | tr -d ' ')
        if [ "$result" = "t" ]; then
            pass "3.2 Table exists: $table"
        else
            fail "3.2 Table exists: $table" "Table not found"
        fi
    done

    # Test 3.3: Users exist
    user_count=$(psql -h "$DB_HOST" -U "$DB_USER" -d "$DB_NAME" -t -c 'SELECT COUNT(*) FROM "AspNetUsers"' 2>/dev/null | tr -d ' ')
    if [ "$user_count" -gt 0 ] 2>/dev/null; then
        pass "3.3 Users exist in database ($user_count users)"
    else
        fail "3.3 Users exist in database" "No users found"
    fi

    # Test 3.4: Roles exist
    role_count=$(psql -h "$DB_HOST" -U "$DB_USER" -d "$DB_NAME" -t -c "SELECT COUNT(*) FROM \"AspNetRoles\" WHERE \"Name\" IN ('CREADOR', 'REVISOR', 'ADMIN')" 2>/dev/null | tr -d ' ')
    if [ "$role_count" = "3" ]; then
        pass "3.4 All roles exist (CREADOR, REVISOR, ADMIN)"
    else
        fail "3.4 All roles exist" "Found $role_count of 3 roles"
    fi

    # Test 3.5: Geographic catalogs have data
    zona_count=$(psql -h "$DB_HOST" -U "$DB_USER" -d "$DB_NAME" -t -c 'SELECT COUNT(*) FROM "ZONA"' 2>/dev/null | tr -d ' ')
    sector_count=$(psql -h "$DB_HOST" -U "$DB_USER" -d "$DB_NAME" -t -c 'SELECT COUNT(*) FROM "SECTOR"' 2>/dev/null | tr -d ' ')
    cuadrante_count=$(psql -h "$DB_HOST" -U "$DB_USER" -d "$DB_NAME" -t -c 'SELECT COUNT(*) FROM "CUADRANTE"' 2>/dev/null | tr -d ' ')

    if [ "$zona_count" -gt 0 ] && [ "$sector_count" -gt 0 ] && [ "$cuadrante_count" -gt 0 ] 2>/dev/null; then
        pass "3.5 Geographic catalogs populated (Z:$zona_count S:$sector_count C:$cuadrante_count)"
    else
        fail "3.5 Geographic catalogs populated" "Missing catalog data"
    fi

    unset PGPASSWORD
fi

# ============================================
# SECTION 4: Protected Routes
# ============================================
print_section "4. Protected Routes Check"

routes=("/reports" "/supervisor" "/supervisor/reports" "/admin" "/admin/users" "/admin/audit")
for route in "${routes[@]}"; do
    status=$(http_get "$BASE_URL$route")
    if [ "$status" = "200" ] || [ "$status" = "302" ] || [ "$status" = "401" ]; then
        pass "4.x Route $route protected/accessible ($status)"
    else
        fail "4.x Route $route" "Status: $status"
    fi
done

# ============================================
# SECTION 5: API Contract Tests
# ============================================
print_section "5. API Contract Verification"

# Test 5.1: Zonas returns JSON array
body=$(http_get_body "$BASE_URL/api/catalogs/zonas")
if echo "$body" | grep -q '\['; then
    pass "5.1 Zonas API returns JSON array"
else
    fail "5.1 Zonas API returns JSON array" "Invalid response"
fi

# Test 5.2: Suggestions returns array
body=$(http_get_body "$BASE_URL/api/catalogs/suggestions/Sexo")
if echo "$body" | grep -q '\['; then
    count=$(echo "$body" | grep -o '"valor"' | wc -l)
    pass "5.2 Suggestions API returns array ($count items)"
else
    fail "5.2 Suggestions API returns array" "Invalid response"
fi

# ============================================
# SECTION 6: Static Resources
# ============================================
print_section "6. Static Resources"

resources=("/css/app.css" "/css/bootstrap/bootstrap.min.css" "/_framework/blazor.web.js")
for resource in "${resources[@]}"; do
    status=$(http_get "$BASE_URL$resource")
    if [ "$status" = "200" ]; then
        pass "6.x Resource $resource"
    else
        fail "6.x Resource $resource" "Status: $status"
    fi
done

# ============================================
# SUMMARY
# ============================================
print_header "VERIFICATION SUMMARY"

TOTAL=$((PASSED + FAILED + SKIPPED))
if [ $TOTAL -gt 0 ]; then
    PASS_RATE=$(echo "scale=1; $PASSED * 100 / $TOTAL" | bc)
else
    PASS_RATE=0
fi

echo ""
echo "Total Tests: $TOTAL"
echo -e "${GREEN}Passed:      $PASSED${NC}"
if [ $FAILED -gt 0 ]; then
    echo -e "${RED}Failed:      $FAILED${NC}"
else
    echo -e "${GREEN}Failed:      $FAILED${NC}"
fi
echo -e "${YELLOW}Skipped:     $SKIPPED${NC}"
echo "Pass Rate:   ${PASS_RATE}%"
echo ""

if [ $FAILED -gt 0 ]; then
    echo -e "${RED}FAILED TESTS:${NC}"
    for test in "${FAILED_TESTS[@]}"; do
        echo -e "  ${RED}- $test${NC}"
    done
    echo ""
fi

echo "Completed: $(date '+%Y-%m-%d %H:%M:%S')"
echo ""

exit $FAILED
