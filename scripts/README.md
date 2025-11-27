# Scripts de Configuración de Base de Datos

Este directorio contiene scripts para configurar la base de datos PostgreSQL necesaria para ejecutar la aplicación Ceiba.

## Problema Común

Si ves este error al ejecutar la aplicación:

```
Npgsql.PostgresException (0x80004005): 42501: se ha denegado el permiso para crear la base de datos
```

Significa que el usuario de PostgreSQL no tiene permisos para crear la base de datos. Usa estos scripts para resolver el problema.

## Solución Rápida - Windows (Recomendado)

Ejecuta el script de PowerShell desde la raíz del proyecto:

```powershell
.\scripts\setup-database.ps1
```

El script te pedirá las credenciales del superusuario de PostgreSQL (por defecto `postgres`) y creará automáticamente:
- Base de datos `ceiba`
- Usuario `ceiba` con contraseña `ceiba123`
- Todos los permisos necesarios

## Solución Manual - SQL

Si prefieres ejecutar los comandos manualmente, conéctate a PostgreSQL como superusuario:

```bash
psql -U postgres
```

Luego ejecuta:

```sql
-- Crear la base de datos
CREATE DATABASE ceiba;

-- Crear el usuario (si no existe)
CREATE USER ceiba WITH PASSWORD 'ceiba123';

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
     "DefaultConnection": "Host=localhost;Database=ceiba;Username=ceiba;Password=ceiba123"
   }
   ```

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

## Cambiar la Contraseña (Producción)

⚠️ **IMPORTANTE**: La contraseña `ceiba123` es solo para desarrollo. En producción:

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
