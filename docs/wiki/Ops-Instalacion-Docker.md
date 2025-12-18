# Instalación con Docker

Esta guía describe cómo instalar Ceiba usando Docker y Docker Compose.

## Prerrequisitos

- Docker Engine 24+
- Docker Compose 2.20+
- 4GB RAM mínimo
- 20GB espacio en disco

## Pasos de Instalación

### 1. Clonar el Repositorio

```bash
git clone https://github.com/org/ceiba.git
cd ceiba
```

### 2. Configurar Variables de Entorno

Crea un archivo `.env` en la raíz:

```bash
# Base de datos
POSTGRES_USER=ceiba
POSTGRES_PASSWORD=tu_password_seguro
POSTGRES_DB=ceiba

# Aplicación
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=Host=ceiba-db;Database=ceiba;Username=ceiba;Password=tu_password_seguro

# Email
Email__Host=smtp.tuproveedor.com
Email__Port=587
Email__Username=tu_email@org.com
Email__Password=tu_password_email

# IA (opcional para reportes automatizados)
AI__Provider=OpenAI
AI__ApiKey=sk-...
AI__Model=gpt-4
```

### 3. Iniciar los Servicios

```bash
# Iniciar en modo detached
docker compose up -d

# Ver logs
docker compose logs -f
```

### 4. Verificar la Instalación

```bash
# Ver contenedores
docker compose ps

# Verificar salud
curl http://localhost:5000/health
```

### 5. Acceder a la Aplicación

Abre en el navegador: `http://localhost:5000`

Credenciales iniciales:
- **Email:** admin@ceiba.local
- **Password:** Admin123!

> **Importante:** Cambia esta contraseña inmediatamente.

## docker-compose.yml

```yaml
services:
  ceiba-web:
    build:
      context: .
      dockerfile: docker/Dockerfile
    ports:
      - "5000:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=${ConnectionStrings__DefaultConnection}
      - Email__Host=${Email__Host}
      - Email__Port=${Email__Port}
      - Email__Username=${Email__Username}
      - Email__Password=${Email__Password}
      - AI__Provider=${AI__Provider}
      - AI__ApiKey=${AI__ApiKey}
    depends_on:
      ceiba-db:
        condition: service_healthy
    restart: unless-stopped

  ceiba-db:
    image: postgres:18
    environment:
      - POSTGRES_USER=${POSTGRES_USER}
      - POSTGRES_PASSWORD=${POSTGRES_PASSWORD}
      - POSTGRES_DB=${POSTGRES_DB}
    volumes:
      - ceiba-data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER}"]
      interval: 5s
      timeout: 5s
      retries: 5
    restart: unless-stopped

volumes:
  ceiba-data:
```

## Comandos Útiles

### Gestión de Contenedores

```bash
# Iniciar
docker compose up -d

# Detener
docker compose down

# Reiniciar
docker compose restart

# Ver logs
docker compose logs -f ceiba-web
docker compose logs -f ceiba-db

# Entrar al contenedor
docker compose exec ceiba-web bash
docker compose exec ceiba-db psql -U ceiba
```

### Actualización

```bash
# Obtener última versión
git pull

# Reconstruir y reiniciar
docker compose build
docker compose up -d
```

### Backup

```bash
# Backup de base de datos
docker compose exec ceiba-db pg_dump -U ceiba ceiba > backup.sql

# O con compresión
docker compose exec ceiba-db pg_dump -U ceiba ceiba | gzip > backup_$(date +%Y%m%d).sql.gz
```

## Configuración de Producción

### Con HTTPS (Traefik)

```yaml
services:
  traefik:
    image: traefik:v3.0
    command:
      - --entrypoints.web.address=:80
      - --entrypoints.websecure.address=:443
      - --certificatesresolvers.letsencrypt.acme.email=admin@org.com
      - --certificatesresolvers.letsencrypt.acme.storage=/letsencrypt/acme.json
      - --certificatesresolvers.letsencrypt.acme.httpchallenge.entrypoint=web
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
      - ./letsencrypt:/letsencrypt

  ceiba-web:
    labels:
      - traefik.enable=true
      - traefik.http.routers.ceiba.rule=Host(`ceiba.tudominio.com`)
      - traefik.http.routers.ceiba.tls.certresolver=letsencrypt
```

## Troubleshooting

### El contenedor no inicia

```bash
docker compose logs ceiba-web
```

### Error de conexión a base de datos

```bash
# Verificar que ceiba-db está healthy
docker compose ps
docker compose exec ceiba-db pg_isready -U ceiba
```

### Aplicar migraciones manualmente

```bash
docker compose exec ceiba-web dotnet ef database update
```

## Próximos Pasos

- [[Ops-Config-Variables-Entorno|Configurar variables de entorno]]
- [[Ops-Config-SSL-HTTPS|Configurar SSL/HTTPS]]
- [[Ops-Mant-Backup-Restore|Configurar backups]]
