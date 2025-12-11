-- Performance Indexes Migration
-- Sprint 6: T117, T117a
-- Execute this script after EF Core migrations to add PostgreSQL-specific optimizations

-- ============================================
-- Full-Text Search Indexes (T117a)
-- ============================================

-- Create Spanish text search configuration if not exists
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_ts_config WHERE cfgname = 'spanish_unaccent') THEN
        -- Use default Spanish config (unaccent extension would need to be installed separately)
        CREATE TEXT SEARCH CONFIGURATION spanish_ceiba (COPY = spanish);
    END IF;
EXCEPTION
    WHEN duplicate_object THEN NULL;
END $$;

-- Full-text search index on HechosReportados (most searched text field)
CREATE INDEX IF NOT EXISTS idx_reporte_hechos_fts
ON "REPORTE_INCIDENCIA"
USING GIN (to_tsvector('spanish', hechos_reportados));

-- Full-text search index on AccionesRealizadas
CREATE INDEX IF NOT EXISTS idx_reporte_acciones_fts
ON "REPORTE_INCIDENCIA"
USING GIN (to_tsvector('spanish', acciones_realizadas));

-- Full-text search index on Observaciones (nullable field)
CREATE INDEX IF NOT EXISTS idx_reporte_observaciones_fts
ON "REPORTE_INCIDENCIA"
USING GIN (to_tsvector('spanish', COALESCE(observaciones, '')));

-- Combined full-text search index for all text fields
CREATE INDEX IF NOT EXISTS idx_reporte_all_text_fts
ON "REPORTE_INCIDENCIA"
USING GIN (to_tsvector('spanish',
    COALESCE(hechos_reportados, '') || ' ' ||
    COALESCE(acciones_realizadas, '') || ' ' ||
    COALESCE(observaciones, '') || ' ' ||
    COALESCE(delito, '')
));

-- ============================================
-- JSONB Indexes for CamposAdicionales (T117)
-- ============================================

-- GIN index for JSONB queries on campos_adicionales
CREATE INDEX IF NOT EXISTS idx_reporte_campos_adicionales_gin
ON "REPORTE_INCIDENCIA"
USING GIN (campos_adicionales jsonb_path_ops)
WHERE campos_adicionales IS NOT NULL;

-- ============================================
-- Audit Log Optimizations (T117)
-- ============================================

-- GIN index for JSONB queries on audit detalles
CREATE INDEX IF NOT EXISTS idx_auditoria_detalles_gin
ON "AUDITORIA"
USING GIN (detalles jsonb_path_ops)
WHERE detalles IS NOT NULL;

-- ============================================
-- Partial Indexes for Common Queries (T117)
-- ============================================

-- Partial index for draft reports only (Estado = 0)
CREATE INDEX IF NOT EXISTS idx_reporte_borradores
ON "REPORTE_INCIDENCIA" (usuario_id, created_at DESC)
WHERE estado = 0;

-- Partial index for submitted reports only (Estado = 1)
CREATE INDEX IF NOT EXISTS idx_reporte_entregados
ON "REPORTE_INCIDENCIA" (zona_id, created_at DESC)
WHERE estado = 1;

-- ============================================
-- Statistics and Maintenance
-- ============================================

-- Update statistics for query planner
ANALYZE "REPORTE_INCIDENCIA";
ANALYZE "AUDITORIA";
ANALYZE "ZONA";
ANALYZE "SECTOR";
ANALYZE "CUADRANTE";
ANALYZE "CATALOGO_SUGERENCIA";

-- ============================================
-- Helper Function for Full-Text Search
-- ============================================

-- Function to search reports using full-text search
CREATE OR REPLACE FUNCTION search_reportes_fts(
    search_term TEXT,
    p_estado INTEGER DEFAULT NULL,
    p_zona_id INTEGER DEFAULT NULL,
    p_limit INTEGER DEFAULT 20,
    p_offset INTEGER DEFAULT 0
)
RETURNS TABLE (
    id INTEGER,
    hechos_reportados TEXT,
    delito VARCHAR,
    estado SMALLINT,
    created_at TIMESTAMPTZ,
    rank REAL
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        r.id,
        r.hechos_reportados,
        r.delito,
        r.estado,
        r.created_at,
        ts_rank(
            to_tsvector('spanish',
                COALESCE(r.hechos_reportados, '') || ' ' ||
                COALESCE(r.acciones_realizadas, '') || ' ' ||
                COALESCE(r.delito, '')),
            plainto_tsquery('spanish', search_term)
        ) AS rank
    FROM "REPORTE_INCIDENCIA" r
    WHERE
        to_tsvector('spanish',
            COALESCE(r.hechos_reportados, '') || ' ' ||
            COALESCE(r.acciones_realizadas, '') || ' ' ||
            COALESCE(r.delito, '')) @@ plainto_tsquery('spanish', search_term)
        AND (p_estado IS NULL OR r.estado = p_estado)
        AND (p_zona_id IS NULL OR r.zona_id = p_zona_id)
    ORDER BY rank DESC, r.created_at DESC
    LIMIT p_limit
    OFFSET p_offset;
END;
$$ LANGUAGE plpgsql STABLE;

COMMENT ON FUNCTION search_reportes_fts IS 'Full-text search function for incident reports with ranking (T117a)';
