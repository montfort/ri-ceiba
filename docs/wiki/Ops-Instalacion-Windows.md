# Instalación en Windows

Esta guía describe cómo instalar Ceiba en Windows Server.

## Prerrequisitos

- Windows Server 2022 o superior
- Acceso de administrador
- PowerShell 7+

## Opción 1: Con Docker Desktop

### Instalar Docker Desktop

1. Descarga Docker Desktop desde [docker.com](https://www.docker.com/products/docker-desktop/)
2. Ejecuta el instalador
3. Reinicia el sistema
4. Activa WSL 2 si se solicita

### Ejecutar Ceiba

```powershell
# Clonar repositorio
git clone https://github.com/org/ceiba.git
cd ceiba

# Crear archivo .env (ver guía Docker)
# Iniciar servicios
docker compose up -d
```

## Opción 2: Instalación Nativa

### 1. Instalar .NET 10

```powershell
# Descargar instalador
Invoke-WebRequest -Uri "https://dot.net/v1/dotnet-install.ps1" -OutFile "dotnet-install.ps1"

# Instalar SDK y Runtime
.\dotnet-install.ps1 -Channel 10.0 -InstallDir "C:\Program Files\dotnet"
```

O descarga desde: https://dotnet.microsoft.com/download

### 2. Instalar PostgreSQL

1. Descarga desde https://www.postgresql.org/download/windows/
2. Ejecuta el instalador
3. Configura:
   - Puerto: 5432
   - Usuario: postgres
   - Password: (tu password)
4. Completa la instalación

### 3. Configurar Base de Datos

Abre pgAdmin o psql:

```sql
CREATE USER ceiba WITH PASSWORD 'tu_password';
CREATE DATABASE ceiba OWNER ceiba;
GRANT ALL PRIVILEGES ON DATABASE ceiba TO ceiba;
```

### 4. Instalar la Aplicación

```powershell
# Crear directorio
New-Item -ItemType Directory -Path "C:\Ceiba" -Force
cd C:\Ceiba

# Clonar repositorio
git clone https://github.com/org/ceiba.git .

# Compilar
dotnet publish src\Ceiba.Web -c Release -o C:\Ceiba\publish
```

### 5. Configurar la Aplicación

Edita `C:\Ceiba\publish\appsettings.Production.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=ceiba;Username=ceiba;Password=tu_password"
  },
  "Email": {
    "Host": "smtp.proveedor.com",
    "Port": 587,
    "Username": "email@org.com",
    "Password": "password"
  }
}
```

### 6. Crear Servicio Windows

Instala NSSM (Non-Sucking Service Manager):

```powershell
# Descargar NSSM
Invoke-WebRequest -Uri "https://nssm.cc/release/nssm-2.24.zip" -OutFile nssm.zip
Expand-Archive nssm.zip -DestinationPath C:\nssm

# Crear servicio
C:\nssm\win64\nssm.exe install Ceiba "C:\Program Files\dotnet\dotnet.exe" "C:\Ceiba\publish\Ceiba.Web.dll"
C:\nssm\win64\nssm.exe set Ceiba AppDirectory "C:\Ceiba\publish"
C:\nssm\win64\nssm.exe set Ceiba AppEnvironmentExtra ASPNETCORE_ENVIRONMENT=Production

# Iniciar servicio
Start-Service Ceiba
```

### 7. Configurar IIS como Reverse Proxy

1. Instala IIS y el módulo URL Rewrite:
   ```powershell
   Install-WindowsFeature -Name Web-Server -IncludeManagementTools
   ```

2. Instala el módulo ASP.NET Core:
   Descarga desde: https://dotnet.microsoft.com/download/dotnet/current/runtime

3. Configura el sitio en IIS Manager:
   - Crea un nuevo sitio
   - Apunta a `C:\Ceiba\publish`
   - Configura los bindings (puerto 80/443)

## Configuración de Firewall

```powershell
# Abrir puertos
New-NetFirewallRule -DisplayName "Ceiba HTTP" -Direction Inbound -LocalPort 80 -Protocol TCP -Action Allow
New-NetFirewallRule -DisplayName "Ceiba HTTPS" -Direction Inbound -LocalPort 443 -Protocol TCP -Action Allow
```

## Aplicar Migraciones

```powershell
cd C:\Ceiba
dotnet ef database update --project src\Ceiba.Infrastructure --startup-project src\Ceiba.Web
```

## Comandos de Gestión

```powershell
# Estado del servicio
Get-Service Ceiba

# Reiniciar
Restart-Service Ceiba

# Ver logs
Get-Content C:\Ceiba\publish\logs\*.log -Tail 100 -Wait
```

## Actualización

```powershell
# Detener servicio
Stop-Service Ceiba

# Actualizar código
cd C:\Ceiba
git pull
dotnet publish src\Ceiba.Web -c Release -o C:\Ceiba\publish

# Reiniciar
Start-Service Ceiba
```

## Próximos Pasos

- [[Ops-Config-SSL-HTTPS|Configurar HTTPS]]
- [[Ops-Mant-Backup-Restore|Configurar backups]]
