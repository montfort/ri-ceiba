-- Script to clear AI configuration with incorrect API key
-- Run this to reset the AI configuration

-- First, check what's in the table
SELECT Id, Proveedor, Modelo, 
       CASE WHEN "ApiKey" IS NOT NULL THEN LEFT("ApiKey", 4) || '...' || RIGHT("ApiKey", 4) ELSE 'NULL' END as ApiKeyMasked,
       "Activo", "CreatedAt", "UpdatedAt"
FROM "CONFIGURACION_IA";

-- Delete all configurations to start fresh
-- DELETE FROM "CONFIGURACION_IA";
