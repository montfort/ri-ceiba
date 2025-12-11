# Scripts de Base de Datos - Ceiba

Este directorio contiene scripts para configurar, resetear y mantener la base de datos PostgreSQL de la aplicación Ceiba.

## Scripts Disponibles

### 1. `setup-database.ps1` - Configuración Inicial
Crea la base de datos y el usuario por primera vez.

### 2. `reset-database.ps1` - Reset Estándar
Elimina y recrea la base de datos usando el usuario `ceiba`.

### 3. `reset-database-with-postgres.ps1` - Reset con Permisos Elevados
Elimina y recrea la base de datos usando el superusuario `postgres`. **Usa este si tienes problemas de permisos**.

### 4. `fix-database-ownership.sql` - Corregir Permisos
Script SQL para corregir problemas de propiedad de la base de datos.

## Problemas Comunes y Soluciones

### Problema 1: "debe ser dueño de la base de datos ceiba"

**Síntomas**: Al ejecutar `dotnet ef database drop` o `reset-database.ps1` obtienes:
```
debe ser dueño de la base de datos ceiba
```

**Causa**: El usuario `ceiba` no es el propietario de la base de datos.

**Solución Rápida**:
```powershell
.\scripts\reset-database-with-postgres.ps1
```

Este script usa el superusuario `postgres` para eliminar y recrear la base de datos correctamente.

**Solución Permanente**:
```powershell
# Ejecutar como superusuario postgres
psql -h localhost -U postgres -d postgres -f scripts/fix-database-ownership.sql
```

### Problema 2: "se ha denegado el permiso para crear la base de datos"

**Síntomas**:
```
Npgsql.PostgresException (0x80004005): 42501: se ha denegado el permiso para crear la base de datos
```

**Causa**: El usuario de PostgreSQL no tiene permisos para crear la base de datos.

**Solución**:
```powershell
.\scripts\setup-database.ps1
```

## Solución Rápida - Windows (Recomendado)

Ejecuta el script de PowerShell desde la raíz del proyecto:

```powershell
.\scripts\setup-database.ps1
```

El script te pedirá las credenciales del superusuario de PostgreSQL (por defecto `postgres`) y creará automáticamente:
- Base de datos `ceiba`
- Usuario `ceiba` con la contraseña que especifiques
- Todos los permisos necesarios

> ⚠️ **IMPORTANTE**: Configure la variable de entorno `DB_PASSWORD` con su contraseña segura.

## Solución Manual - SQL

Si prefieres ejecutar los comandos manualmente, conéctate a PostgreSQL como superusuario:

```bash
psql -U postgres
```

Luego ejecuta:

```sql
-- Crear la base de datos
CREATE DATABASE ceiba;

-- Crear el usuario (si no existe) - use a secure password!
CREATE USER ceiba WITH PASSWORD 'YOUR_SECURE_PASSWORD';

-- Otorgar privilegios
GRANT ALL PRIVILEGES ON DATABASE ceiba TO ceiba;

-- Conectar a la base de datos
\c ceiba

-- Permisos en schema public
GRANT ALL ON SCHEMA public TO ceiba;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO ceiba;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO ceiba;

-- Permisos por defecto para objetos futuros
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO ceiba;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO ceiba;
```

## Solución Alternativa - Script SQL

Ejecuta el script SQL incluido:

```bash
psql -U postgres -f scripts/setup-database.sql
```

## Después de Configurar la Base de Datos

Una vez que la base de datos esté configurada:

1. Verifica la cadena de conexión en `src/Ceiba.Web/appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Database=ceiba;Username=ceiba;Password=${DB_PASSWORD}"
   }
   ```

   > **Nota**: Configure la variable de entorno `DB_PASSWORD` con su contraseña.

2. Ejecuta la aplicación:
   ```bash
   dotnet run --project src/Ceiba.Web
   ```

3. Las migraciones de Entity Framework se aplicarán automáticamente en el primer inicio.

## Verificar la Configuración

Para verificar que todo está configurado correctamente:

```bash
# Conectar a la base de datos como usuario ceiba
psql -U ceiba -d ceiba -h localhost

# Si la conexión es exitosa, verás:
# ceiba=>
```

## Configurar Contraseña Segura (Requerido)

⚠️ **IMPORTANTE**: Nunca use contraseñas hardcodeadas. Siempre configure una contraseña segura:

1. Cambia la contraseña en PostgreSQL:
   ```sql
   ALTER USER ceiba WITH PASSWORD 'tu_contraseña_segura';
   ```

2. Actualiza la cadena de conexión en `appsettings.Production.json` o usa variables de entorno:
   ```bash
   export ConnectionStrings__DefaultConnection="Host=localhost;Database=ceiba;Username=ceiba;Password=tu_contraseña_segura"
   ```

## Solución de Problemas

### Error: "psql: command not found"

PostgreSQL no está instalado o no está en el PATH. Soluciones:

- **Windows**: Agrega `C:\Program Files\PostgreSQL\XX\bin` al PATH del sistema
- **Linux**: Instala PostgreSQL: `sudo apt install postgresql postgresql-contrib`
- **macOS**: Instala con Homebrew: `brew install postgresql`

### Error: "FATAL: password authentication failed"

Las credenciales del superusuario son incorrectas. Verifica:
- Usuario correcto (por defecto `postgres`)
- Contraseña configurada durante la instalación de PostgreSQL

### Error: "connection refused"

El servidor PostgreSQL no está corriendo. Inicia el servicio:
- **Windows**: `net start postgresql-x64-XX`
- **Linux**: `sudo systemctl start postgresql`
- **macOS**: `brew services start postgresql`

## Contacto

Si necesitas ayuda adicional, consulta la documentación del proyecto en `/specs/001-incident-management-system/quickstart.md`.
