-- Script para corregir permisos de base de datos PostgreSQL
-- Ejecutar como superusuario (postgres) o usuario con privilegios suficientes

-- Opción 1: Cambiar el propietario de la base de datos
-- Ejecutar como usuario postgres
ALTER DATABASE ceiba OWNER TO ceiba;

-- Opción 2: Otorgar todos los privilegios
-- Ejecutar como usuario postgres
GRANT ALL PRIVILEGES ON DATABASE ceiba TO ceiba;

-- Opción 3: Verificar el propietario actual
SELECT d.datname AS database_name,
       pg_catalog.pg_get_userbyid(d.datdba) AS owner
FROM pg_catalog.pg_database d
WHERE d.datname = 'ceiba';

-- Opción 4: Si necesitas eliminar la base de datos como superusuario
-- DROP DATABASE ceiba;
-- CREATE DATABASE ceiba OWNER ceiba;
