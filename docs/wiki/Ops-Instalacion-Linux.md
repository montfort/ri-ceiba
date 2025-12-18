# Instalación en Linux

Esta guía describe cómo instalar Ceiba directamente en un servidor Linux sin Docker.

## Prerrequisitos

- Fedora 42+ / Ubuntu 22.04+ / RHEL 9+
- Acceso root o sudo
- Conexión a internet

## Instalación de Dependencias

### Fedora/RHEL

```bash
# .NET 10
sudo dnf install dotnet-sdk-10.0 aspnetcore-runtime-10.0

# PostgreSQL 18
sudo dnf install postgresql18-server postgresql18
sudo postgresql-setup --initdb
sudo systemctl enable --now postgresql

# Herramientas
sudo dnf install git nginx
```

### Ubuntu/Debian

```bash
# Agregar repositorio de Microsoft
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb

# .NET 10
sudo apt update
sudo apt install dotnet-sdk-10.0 aspnetcore-runtime-10.0

# PostgreSQL 18
sudo sh -c 'echo "deb http://apt.postgresql.org/pub/repos/apt $(lsb_release -cs)-pgdg main" > /etc/apt/sources.list.d/pgdg.list'
wget --quiet -O - https://www.postgresql.org/media/keys/ACCC4CF8.asc | sudo apt-key add -
sudo apt update
sudo apt install postgresql-18

# Herramientas
sudo apt install git nginx
```

## Configurar PostgreSQL

```bash
# Cambiar a usuario postgres
sudo -u postgres psql

# Crear base de datos y usuario
CREATE USER ceiba WITH PASSWORD 'tu_password_seguro';
CREATE DATABASE ceiba OWNER ceiba;
GRANT ALL PRIVILEGES ON DATABASE ceiba TO ceiba;
\q

# Configurar autenticación (pg_hba.conf)
sudo nano /var/lib/pgsql/data/pg_hba.conf
# Agregar línea:
# host    ceiba    ceiba    127.0.0.1/32    scram-sha-256

# Reiniciar PostgreSQL
sudo systemctl restart postgresql
```

## Instalar la Aplicación

### 1. Crear Usuario del Sistema

```bash
sudo useradd -r -s /bin/false ceiba
sudo mkdir -p /opt/ceiba
sudo chown ceiba:ceiba /opt/ceiba
```

### 2. Clonar y Compilar

```bash
cd /opt/ceiba
sudo -u ceiba git clone https://github.com/org/ceiba.git .
sudo -u ceiba dotnet publish src/Ceiba.Web -c Release -o /opt/ceiba/publish
```

### 3. Configurar la Aplicación

```bash
sudo nano /opt/ceiba/publish/appsettings.Production.json
```

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
  },
  "AI": {
    "Provider": "OpenAI",
    "ApiKey": "sk-...",
    "Model": "gpt-4"
  }
}
```

### 4. Crear Servicio Systemd

```bash
sudo nano /etc/systemd/system/ceiba.service
```

```ini
[Unit]
Description=Ceiba Web Application
After=network.target postgresql.service

[Service]
WorkingDirectory=/opt/ceiba/publish
ExecStart=/usr/bin/dotnet /opt/ceiba/publish/Ceiba.Web.dll
Restart=always
RestartSec=10
User=ceiba
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5000

[Install]
WantedBy=multi-user.target
```

```bash
sudo systemctl daemon-reload
sudo systemctl enable ceiba
sudo systemctl start ceiba
```

### 5. Configurar Nginx como Reverse Proxy

```bash
sudo nano /etc/nginx/conf.d/ceiba.conf
```

```nginx
server {
    listen 80;
    server_name ceiba.tudominio.com;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }
}
```

```bash
sudo nginx -t
sudo systemctl restart nginx
```

### 6. Aplicar Migraciones

```bash
cd /opt/ceiba
sudo -u ceiba dotnet ef database update --project src/Ceiba.Infrastructure --startup-project src/Ceiba.Web
```

## Verificación

```bash
# Estado del servicio
sudo systemctl status ceiba

# Logs de la aplicación
sudo journalctl -u ceiba -f

# Probar conectividad
curl http://localhost:5000/health
```

## Comandos de Gestión

```bash
# Reiniciar aplicación
sudo systemctl restart ceiba

# Ver logs
sudo journalctl -u ceiba -f

# Actualizar aplicación
cd /opt/ceiba
sudo -u ceiba git pull
sudo -u ceiba dotnet publish src/Ceiba.Web -c Release -o /opt/ceiba/publish
sudo systemctl restart ceiba
```

## Próximos Pasos

- [[Ops-Config-SSL-HTTPS|Configurar HTTPS con Let's Encrypt]]
- [[Ops-Seguridad-Firewall|Configurar firewall]]
- [[Ops-Mant-Backup-Restore|Configurar backups]]
