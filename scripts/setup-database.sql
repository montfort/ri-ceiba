-- Script de configuración inicial de la base de datos PostgreSQL
-- Para el proyecto Ceiba - Reportes de Incidencias
--
-- Ejecutar como superusuario de PostgreSQL (postgres):
-- psql -U postgres -f scripts/setup-database.sql

-- Crear la base de datos si no existe
SELECT 'CREATE DATABASE ceiba'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'ceiba')\gexec

-- Crear el usuario si no existe
DO
$$
BEGIN
   IF NOT EXISTS (SELECT FROM pg_catalog.pg_user WHERE usename = 'ceiba') THEN
      CREATE USER ceiba WITH PASSWORD 'ceiba123';
   END IF;
END
$$;

-- Otorgar privilegios en la base de datos
GRANT ALL PRIVILEGES ON DATABASE ceiba TO ceiba;

-- Conectar a la base de datos ceiba
\c ceiba

-- Otorgar permisos en el schema public
GRANT ALL ON SCHEMA public TO ceiba;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO ceiba;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO ceiba;

-- Configurar permisos por defecto para objetos futuros
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO ceiba;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO ceiba;

-- Verificar que el usuario tiene los permisos correctos
\du ceiba

-- Mensaje de confirmación
SELECT 'Base de datos "ceiba" configurada exitosamente. Usuario "ceiba" tiene todos los privilegios.' AS status;
