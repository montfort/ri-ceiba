#!/bin/bash
# T019e: RT-004 Mitigation - Migration validation script
# Validates database state after migration (row counts, FK integrity)
# Usage: ./validate-migration.sh [connection_string]

set -euo pipefail

DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-5432}"
DB_NAME="${DB_NAME:-ceiba}"
DB_USER="${DB_USER:-ceiba}"
export PGPASSWORD="${DB_PASSWORD:-ceiba123}"

CONN_STRING="${1:-host=$DB_HOST port=$DB_PORT dbname=$DB_NAME user=$DB_USER}"

echo "=========================================="
echo "Migration Validation - $(date)"
echo "=========================================="
echo ""

# Test connection
echo "[1/4] Testing database connection..."
if psql "$CONN_STRING" -c "SELECT version();" > /dev/null 2>&1; then
    echo "  ✓ Connection successful"
else
    echo "  ✗ Connection failed"
    exit 1
fi
echo ""

# Validate row counts
echo "[2/4] Validating table row counts..."
TABLES=("USUARIO" "ROL" "ZONA" "SECTOR" "CUADRANTE" "CATALOGO_SUGERENCIA" "AUDITORIA")

for table in "${TABLES[@]}"; do
    COUNT=$(psql "$CONN_STRING" -t -c "SELECT COUNT(*) FROM \"$table\";" 2>/dev/null || echo "ERROR")
    if [ "$COUNT" = "ERROR" ]; then
        echo "  ✗ Table $table: MISSING or INACCESSIBLE"
        exit 1
    else
        echo "  ✓ Table $table: $COUNT rows"
    fi
done
echo ""

# Validate foreign key integrity
echo "[3/4] Validating foreign key constraints..."
FK_VIOLATIONS=$(psql "$CONN_STRING" -t -c "
    SELECT COUNT(*)
    FROM (
        SELECT 'SECTOR' as table_name, zona_id as fk_value
        FROM \"SECTOR\"
        WHERE zona_id NOT IN (SELECT id FROM \"ZONA\")
        UNION ALL
        SELECT 'CUADRANTE', sector_id
        FROM \"CUADRANTE\"
        WHERE sector_id NOT IN (SELECT id FROM \"SECTOR\")
    ) violations;
" 2>/dev/null || echo "ERROR")

if [ "$FK_VIOLATIONS" = "ERROR" ]; then
    echo "  ✗ FK validation query failed"
    exit 1
elif [ "$FK_VIOLATIONS" -gt 0 ]; then
    echo "  ✗ Found $FK_VIOLATIONS FK violations!"
    exit 1
else
    echo "  ✓ All foreign keys valid"
fi
echo ""

# Validate indexes exist
echo "[4/4] Validating critical indexes..."
REQUIRED_INDEXES=(
    "idx_zona_nombre_unique"
    "idx_sector_zona_nombre_unique"
    "idx_auditoria_fecha"
    "idx_auditoria_usuario"
)

for index in "${REQUIRED_INDEXES[@]}"; do
    EXISTS=$(psql "$CONN_STRING" -t -c "
        SELECT COUNT(*) FROM pg_indexes
        WHERE indexname = '$index';
    " 2>/dev/null || echo "0")

    if [ "$EXISTS" -gt 0 ]; then
        echo "  ✓ Index $index exists"
    else
        echo "  ✗ Index $index MISSING"
        exit 1
    fi
done
echo ""

echo "=========================================="
echo "✓ Migration validation PASSED"
echo "=========================================="
exit 0
