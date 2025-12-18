# Configuración de SSL/HTTPS

Esta guía describe cómo configurar HTTPS para Ceiba en producción.

## Importancia de HTTPS

HTTPS es **obligatorio** en producción para:
- Proteger credenciales de usuarios
- Cifrar datos de reportes sensibles
- Cumplir con estándares de seguridad
- Evitar ataques man-in-the-middle

## Opción 1: Let's Encrypt con Certbot

### Instalación de Certbot

```bash
# Fedora/RHEL
sudo dnf install certbot python3-certbot-nginx

# Ubuntu/Debian
sudo apt install certbot python3-certbot-nginx
```

### Obtener Certificado

```bash
# Con Nginx
sudo certbot --nginx -d ceiba.tudominio.com

# Solo certificado (sin configurar Nginx automáticamente)
sudo certbot certonly --webroot -w /var/www/html -d ceiba.tudominio.com
```

### Renovación Automática

```bash
# Verificar renovación
sudo certbot renew --dry-run

# Certbot instala timer automáticamente
sudo systemctl status certbot.timer
```

### Ubicación de Certificados

```
/etc/letsencrypt/live/ceiba.tudominio.com/
├── fullchain.pem  # Certificado + intermedios
├── privkey.pem    # Clave privada
├── cert.pem       # Solo certificado
└── chain.pem      # Certificados intermedios
```

## Opción 2: Certificado Comercial

### Generar CSR

```bash
# Generar clave privada
openssl genrsa -out ceiba.key 2048

# Generar CSR
openssl req -new -key ceiba.key -out ceiba.csr \
  -subj "/C=MX/ST=Estado/L=Ciudad/O=Organizacion/CN=ceiba.tudominio.com"
```

### Instalar Certificado

```bash
# Copiar archivos recibidos
sudo cp ceiba.crt /etc/ssl/certs/
sudo cp ceiba.key /etc/ssl/private/
sudo cp ca-bundle.crt /etc/ssl/certs/
```

## Configuración de Nginx

### Configuración Básica HTTPS

```nginx
server {
    listen 80;
    server_name ceiba.tudominio.com;
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name ceiba.tudominio.com;

    # Certificados
    ssl_certificate /etc/letsencrypt/live/ceiba.tudominio.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/ceiba.tudominio.com/privkey.pem;

    # Configuración SSL moderna
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256:ECDHE-ECDSA-AES256-GCM-SHA384:ECDHE-RSA-AES256-GCM-SHA384;
    ssl_prefer_server_ciphers off;

    # HSTS
    add_header Strict-Transport-Security "max-age=63072000" always;

    # OCSP Stapling
    ssl_stapling on;
    ssl_stapling_verify on;
    ssl_trusted_certificate /etc/letsencrypt/live/ceiba.tudominio.com/chain.pem;

    # Proxy a la aplicación
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

### Aplicar Configuración

```bash
# Verificar sintaxis
sudo nginx -t

# Recargar
sudo systemctl reload nginx
```

## Configuración con Docker/Traefik

### docker-compose.yml con Traefik

```yaml
services:
  traefik:
    image: traefik:v3.0
    command:
      - --api.insecure=false
      - --providers.docker=true
      - --providers.docker.exposedbydefault=false
      - --entrypoints.web.address=:80
      - --entrypoints.websecure.address=:443
      - --certificatesresolvers.letsencrypt.acme.email=admin@tudominio.com
      - --certificatesresolvers.letsencrypt.acme.storage=/letsencrypt/acme.json
      - --certificatesresolvers.letsencrypt.acme.httpchallenge.entrypoint=web
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock:ro
      - ./letsencrypt:/letsencrypt
    labels:
      # Redirección HTTP a HTTPS
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
    depends_on:
      - ceiba-db
```

## Configuración de ASP.NET Core

### Forzar HTTPS

```csharp
// Program.cs
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
app.UseHttpsRedirection();
```

### Configurar Headers de Proxy

```csharp
// Program.cs
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
```

### appsettings.Production.json

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5000"
      }
    }
  }
}
```

## Verificación

### Comprobar Certificado

```bash
# Verificar certificado instalado
openssl s_client -connect ceiba.tudominio.com:443 -servername ceiba.tudominio.com

# Ver detalles del certificado
echo | openssl s_client -connect ceiba.tudominio.com:443 2>/dev/null | openssl x509 -noout -text

# Verificar fecha de expiración
echo | openssl s_client -connect ceiba.tudominio.com:443 2>/dev/null | openssl x509 -noout -dates
```

### Test con SSL Labs

Visita: https://www.ssllabs.com/ssltest/analyze.html?d=ceiba.tudominio.com

Objetivo: **A** o **A+**

### Test con curl

```bash
# Verificar HTTPS funciona
curl -I https://ceiba.tudominio.com

# Verificar redirección HTTP → HTTPS
curl -I http://ceiba.tudominio.com
```

## Headers de Seguridad

### Configuración Nginx Completa

```nginx
# Headers de seguridad
add_header X-Frame-Options "SAMEORIGIN" always;
add_header X-Content-Type-Options "nosniff" always;
add_header X-XSS-Protection "1; mode=block" always;
add_header Referrer-Policy "strict-origin-when-cross-origin" always;
add_header Content-Security-Policy "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:;" always;

# HSTS (solo si estás seguro de usar siempre HTTPS)
add_header Strict-Transport-Security "max-age=63072000; includeSubDomains; preload" always;
```

## Troubleshooting

### Error: Certificate Not Trusted

```
SSL certificate problem: unable to get local issuer certificate
```

**Solución:** Incluir certificados intermedios en fullchain.pem

### Error: Certificate Expired

```bash
# Renovar manualmente
sudo certbot renew --force-renewal

# Verificar timer
sudo systemctl status certbot.timer
```

### Error: Mixed Content

Contenido HTTP en página HTTPS.

**Solución:** Asegurar que todos los recursos usen HTTPS o URLs relativas.

### Error: HSTS Redirect Loop

**Solución:** Verificar que la aplicación responde correctamente en HTTPS antes de habilitar HSTS.

## Certificados Wildcard

Para múltiples subdominios:

```bash
sudo certbot certonly --manual --preferred-challenges dns \
  -d "*.tudominio.com" -d "tudominio.com"
```

Requiere agregar registro TXT en DNS.

## Próximos Pasos

- [[Ops Config Reverse Proxy|Configurar reverse proxy]]
- [[Ops Seguridad Hardening|Hardening del servidor]]
- [[Ops Seguridad Firewall|Configurar firewall]]
