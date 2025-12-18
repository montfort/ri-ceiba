# Configuración de Reverse Proxy

Esta guía describe cómo configurar un reverse proxy para Ceiba.

## ¿Por qué usar Reverse Proxy?

- **SSL Termination**: Manejo centralizado de HTTPS
- **Load Balancing**: Distribuir carga entre instancias
- **Caching**: Cachear contenido estático
- **Security**: Capa adicional de protección
- **WebSockets**: Blazor Server requiere soporte de WebSockets

## Opción 1: Nginx

### Instalación

```bash
# Fedora/RHEL
sudo dnf install nginx

# Ubuntu/Debian
sudo apt install nginx

# Iniciar y habilitar
sudo systemctl enable --now nginx
```

### Configuración para Blazor Server

```nginx
# /etc/nginx/conf.d/ceiba.conf

upstream ceiba_backend {
    server 127.0.0.1:5000;
    keepalive 32;
}

server {
    listen 80;
    server_name ceiba.tudominio.com;
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name ceiba.tudominio.com;

    # SSL (ver guía SSL/HTTPS)
    ssl_certificate /etc/letsencrypt/live/ceiba.tudominio.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/ceiba.tudominio.com/privkey.pem;
    ssl_protocols TLSv1.2 TLSv1.3;

    # Logs
    access_log /var/log/nginx/ceiba_access.log;
    error_log /var/log/nginx/ceiba_error.log;

    # Tamaño máximo de upload
    client_max_body_size 10M;

    # Timeouts para conexiones largas (Blazor SignalR)
    proxy_read_timeout 300s;
    proxy_connect_timeout 75s;
    proxy_send_timeout 300s;

    # Compresión
    gzip on;
    gzip_types text/plain text/css application/json application/javascript text/xml application/xml;

    # Headers de seguridad
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;

    # Contenido estático
    location /_content/ {
        proxy_pass http://ceiba_backend;
        proxy_cache_valid 200 1d;
        expires 1d;
        add_header Cache-Control "public, immutable";
    }

    location /_framework/ {
        proxy_pass http://ceiba_backend;
        proxy_cache_valid 200 1d;
        expires 1d;
        add_header Cache-Control "public, immutable";
    }

    # WebSockets para Blazor Server (SignalR)
    location /_blazor {
        proxy_pass http://ceiba_backend;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;

        # Timeouts largos para WebSockets
        proxy_read_timeout 86400s;
        proxy_send_timeout 86400s;
    }

    # Aplicación principal
    location / {
        proxy_pass http://ceiba_backend;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection $http_connection;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }
}
```

### Aplicar y Verificar

```bash
# Verificar configuración
sudo nginx -t

# Recargar
sudo systemctl reload nginx

# Ver logs
sudo tail -f /var/log/nginx/ceiba_error.log
```

## Opción 2: Apache

### Instalación

```bash
# Fedora/RHEL
sudo dnf install httpd mod_ssl

# Ubuntu/Debian
sudo apt install apache2

# Habilitar módulos
sudo a2enmod proxy proxy_http proxy_wstunnel ssl headers rewrite
```

### Configuración

```apache
# /etc/httpd/conf.d/ceiba.conf (Fedora)
# /etc/apache2/sites-available/ceiba.conf (Ubuntu)

<VirtualHost *:80>
    ServerName ceiba.tudominio.com
    Redirect permanent / https://ceiba.tudominio.com/
</VirtualHost>

<VirtualHost *:443>
    ServerName ceiba.tudominio.com

    SSLEngine on
    SSLCertificateFile /etc/letsencrypt/live/ceiba.tudominio.com/fullchain.pem
    SSLCertificateKeyFile /etc/letsencrypt/live/ceiba.tudominio.com/privkey.pem

    # Headers
    RequestHeader set X-Forwarded-Proto "https"
    Header always set X-Frame-Options "SAMEORIGIN"
    Header always set X-Content-Type-Options "nosniff"

    # Proxy para WebSockets (Blazor SignalR)
    RewriteEngine On
    RewriteCond %{HTTP:Upgrade} websocket [NC]
    RewriteCond %{HTTP:Connection} upgrade [NC]
    RewriteRule ^/?(.*) ws://127.0.0.1:5000/$1 [P,L]

    # Proxy HTTP
    ProxyPreserveHost On
    ProxyPass / http://127.0.0.1:5000/
    ProxyPassReverse / http://127.0.0.1:5000/

    # Timeouts
    ProxyTimeout 300

    # Logs
    ErrorLog /var/log/httpd/ceiba_error.log
    CustomLog /var/log/httpd/ceiba_access.log combined
</VirtualHost>
```

### Activar en Ubuntu

```bash
sudo a2ensite ceiba
sudo systemctl reload apache2
```

## Opción 3: Traefik (Docker)

### docker-compose.yml

```yaml
services:
  traefik:
    image: traefik:v3.0
    command:
      - --api.dashboard=true
      - --providers.docker=true
      - --providers.docker.exposedbydefault=false
      - --entrypoints.web.address=:80
      - --entrypoints.websecure.address=:443
      - --certificatesresolvers.letsencrypt.acme.email=admin@org.com
      - --certificatesresolvers.letsencrypt.acme.storage=/letsencrypt/acme.json
      - --certificatesresolvers.letsencrypt.acme.httpchallenge.entrypoint=web
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock:ro
      - traefik-letsencrypt:/letsencrypt
    labels:
      # Dashboard (opcional, proteger en producción)
      - traefik.enable=true
      - traefik.http.routers.dashboard.rule=Host(`traefik.tudominio.com`)
      - traefik.http.routers.dashboard.service=api@internal
      - traefik.http.routers.dashboard.tls.certresolver=letsencrypt
      # Redirección HTTP → HTTPS
      - traefik.http.routers.http-catchall.rule=hostregexp(`{host:.+}`)
      - traefik.http.routers.http-catchall.entrypoints=web
      - traefik.http.routers.http-catchall.middlewares=redirect-to-https
      - traefik.http.middlewares.redirect-to-https.redirectscheme.scheme=https

  ceiba-web:
    build: .
    labels:
      - traefik.enable=true
      - traefik.http.routers.ceiba.rule=Host(`ceiba.tudominio.com`)
      - traefik.http.routers.ceiba.entrypoints=websecure
      - traefik.http.routers.ceiba.tls.certresolver=letsencrypt
      - traefik.http.services.ceiba.loadbalancer.server.port=8080
      # Headers de seguridad
      - traefik.http.routers.ceiba.middlewares=security-headers
      - traefik.http.middlewares.security-headers.headers.frameDeny=true
      - traefik.http.middlewares.security-headers.headers.contentTypeNosniff=true

volumes:
  traefik-letsencrypt:
```

## Configuración de ASP.NET Core

### Forwarded Headers

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Configurar forwarded headers
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // En producción, especificar proxies conocidos
    // options.KnownProxies.Add(IPAddress.Parse("10.0.0.1"));
});

var app = builder.Build();

// Usar forwarded headers ANTES de otros middlewares
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
app.UseHttpsRedirection();
```

## Load Balancing

### Nginx con Múltiples Instancias

```nginx
upstream ceiba_backend {
    least_conn;  # Algoritmo de balanceo
    server 127.0.0.1:5000 weight=3;
    server 127.0.0.1:5001 weight=2;
    server 127.0.0.1:5002 weight=1;
    keepalive 32;
}
```

### Sticky Sessions (para Blazor Server)

Blazor Server requiere sticky sessions:

```nginx
upstream ceiba_backend {
    ip_hash;  # Sticky sessions por IP
    server 127.0.0.1:5000;
    server 127.0.0.1:5001;
}
```

## Health Checks

### Nginx

```nginx
upstream ceiba_backend {
    server 127.0.0.1:5000 max_fails=3 fail_timeout=30s;
    server 127.0.0.1:5001 max_fails=3 fail_timeout=30s backup;
}
```

### Endpoint de Health Check

La aplicación expone `/health`:

```bash
curl http://localhost:5000/health
```

## Troubleshooting

### Error 502 Bad Gateway

El backend no responde.

```bash
# Verificar que la aplicación está corriendo
curl http://localhost:5000/health

# Verificar logs
sudo journalctl -u ceiba -f
```

### WebSockets No Funcionan

Blazor Server muestra "Attempting to reconnect".

**Verificar:**
1. Headers Upgrade y Connection configurados
2. Timeouts suficientemente largos
3. Proxy soporta WebSockets

### Error 413 Request Entity Too Large

```nginx
# Aumentar límite
client_max_body_size 50M;
```

### Conexión Lenta

```nginx
# Habilitar buffering
proxy_buffering on;
proxy_buffer_size 128k;
proxy_buffers 4 256k;
```

## Próximos Pasos

- [[Ops Config SSL HTTPS|Configurar SSL/HTTPS]]
- [[Ops Mant Monitoreo|Monitorear el sistema]]
- [[Ops Seguridad Firewall|Configurar firewall]]
