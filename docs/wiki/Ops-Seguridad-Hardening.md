# Hardening del Sistema

Esta guía describe las mejores prácticas de seguridad para endurecer el servidor de Ceiba.

## Principios de Seguridad

1. **Mínimo Privilegio**: Solo permisos necesarios
2. **Defensa en Profundidad**: Múltiples capas de seguridad
3. **Fail Secure**: En caso de fallo, denegar acceso
4. **Auditoría Completa**: Registrar toda actividad

## Hardening del Sistema Operativo

### Actualizar Sistema

```bash
# Fedora/RHEL
sudo dnf update -y
sudo dnf autoremove

# Ubuntu/Debian
sudo apt update && sudo apt upgrade -y
sudo apt autoremove
```

### Configurar Actualizaciones Automáticas

```bash
# Fedora
sudo dnf install dnf-automatic
sudo systemctl enable --now dnf-automatic.timer

# Ubuntu
sudo apt install unattended-upgrades
sudo dpkg-reconfigure unattended-upgrades
```

### Deshabilitar Servicios Innecesarios

```bash
# Listar servicios activos
systemctl list-units --type=service --state=running

# Deshabilitar servicios no necesarios
sudo systemctl disable --now cups
sudo systemctl disable --now avahi-daemon
sudo systemctl disable --now bluetooth
```

### Configurar SSH

```bash
# /etc/ssh/sshd_config
PermitRootLogin no
PasswordAuthentication no
PubkeyAuthentication yes
MaxAuthTries 3
ClientAliveInterval 300
ClientAliveCountMax 2
AllowUsers ceiba-admin
Protocol 2

# Reiniciar SSH
sudo systemctl restart sshd
```

### Configurar Sudo

```bash
# /etc/sudoers.d/ceiba-admin
ceiba-admin ALL=(ALL) NOPASSWD: /usr/bin/docker, /usr/bin/docker-compose
ceiba-admin ALL=(ALL) PASSWD: ALL
Defaults timestamp_timeout=5
Defaults logfile="/var/log/sudo.log"
```

## Hardening de Docker

### Ejecutar como No-Root

```yaml
# docker-compose.yml
services:
  ceiba-web:
    user: "1000:1000"
    read_only: true
    security_opt:
      - no-new-privileges:true
```

### Limitar Recursos

```yaml
services:
  ceiba-web:
    deploy:
      resources:
        limits:
          cpus: '2'
          memory: 2G
        reservations:
          cpus: '0.5'
          memory: 512M
```

### Red Aislada

```yaml
networks:
  ceiba-internal:
    driver: bridge
    internal: true  # Sin acceso a internet

services:
  ceiba-db:
    networks:
      - ceiba-internal  # Solo red interna

  ceiba-web:
    networks:
      - ceiba-internal
      - ceiba-external  # Con acceso externo
```

### Montar Volúmenes como Solo Lectura

```yaml
services:
  ceiba-web:
    volumes:
      - ./config:/app/config:ro  # Solo lectura
      - logs:/app/logs:rw  # Lectura/escritura solo donde necesario
```

## Hardening de PostgreSQL

### Configuración de Seguridad

```ini
# postgresql.conf
ssl = on
ssl_cert_file = '/etc/ssl/certs/server.crt'
ssl_key_file = '/etc/ssl/private/server.key'
password_encryption = scram-sha-256
log_connections = on
log_disconnections = on
log_statement = 'ddl'
```

### Restringir Conexiones

```
# pg_hba.conf
# TYPE  DATABASE    USER    ADDRESS        METHOD
local   all         postgres               peer
host    ceiba       ceiba   127.0.0.1/32   scram-sha-256
host    ceiba       ceiba   10.0.0.0/8     scram-sha-256
hostssl ceiba       ceiba   0.0.0.0/0      scram-sha-256

# Rechazar todo lo demás
host    all         all     0.0.0.0/0      reject
```

### Usuario con Mínimos Privilegios

```sql
-- Crear usuario solo para la aplicación
CREATE USER ceiba_app WITH PASSWORD 'password';

-- Otorgar solo permisos necesarios
GRANT CONNECT ON DATABASE ceiba TO ceiba_app;
GRANT USAGE ON SCHEMA public TO ceiba_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO ceiba_app;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO ceiba_app;

-- Revocar permisos peligrosos
REVOKE CREATE ON SCHEMA public FROM ceiba_app;
```

## Hardening de la Aplicación

### Headers de Seguridad HTTP

```csharp
// Program.cs
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Add("Content-Security-Policy",
        "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'");
    context.Response.Headers.Add("Permissions-Policy",
        "geolocation=(), microphone=(), camera=()");
    await next();
});
```

### Configuración de Cookies

```csharp
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
});
```

### Rate Limiting

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("login", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
    });
});

// En el endpoint de login
app.MapPost("/login", ...).RequireRateLimiting("login");
```

### Validación de Entrada

```csharp
// Usar DataAnnotations
public class ReporteDto
{
    [Required]
    [StringLength(1000, MinimumLength = 10)]
    public string Descripcion { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int ZonaId { get; set; }
}

// Sanitizar HTML si se permite
public string SanitizeHtml(string input)
{
    var sanitizer = new HtmlSanitizer();
    return sanitizer.Sanitize(input);
}
```

## Hardening de Nginx

```nginx
# /etc/nginx/nginx.conf

# Ocultar versión
server_tokens off;

# Limitar métodos HTTP
if ($request_method !~ ^(GET|HEAD|POST|PUT|DELETE)$) {
    return 405;
}

# Limitar tamaño de cuerpo
client_max_body_size 10M;
client_body_buffer_size 128k;

# Timeouts
client_body_timeout 10s;
client_header_timeout 10s;
send_timeout 10s;

# Protección contra clickjacking
add_header X-Frame-Options "SAMEORIGIN" always;

# SSL
ssl_protocols TLSv1.2 TLSv1.3;
ssl_ciphers ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256;
ssl_prefer_server_ciphers off;
ssl_session_timeout 1d;
ssl_session_cache shared:SSL:50m;
```

## Protección contra Ataques

### Fuerza Bruta

```csharp
// Implementar lockout de cuenta
services.Configure<IdentityOptions>(options =>
{
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
});
```

### SQL Injection

```csharp
// SIEMPRE usar parámetros, NUNCA concatenar
// MAL:
var query = $"SELECT * FROM usuarios WHERE email = '{email}'";

// BIEN:
var usuario = await context.Usuarios
    .Where(u => u.Email == email)
    .FirstOrDefaultAsync();
```

### XSS

```csharp
// Blazor escapa automáticamente, pero cuidado con MarkupString
// MAL:
@((MarkupString)userInput)

// BIEN:
@userInput
```

## Checklist de Seguridad

### Sistema Operativo
- [ ] Sistema actualizado
- [ ] Servicios innecesarios deshabilitados
- [ ] SSH solo con llaves
- [ ] Firewall configurado
- [ ] Fail2ban activo

### Docker
- [ ] Contenedores sin root
- [ ] Recursos limitados
- [ ] Redes aisladas
- [ ] Imágenes actualizadas

### Base de Datos
- [ ] SSL habilitado
- [ ] Usuario con mínimos privilegios
- [ ] Conexiones restringidas
- [ ] Logging activo

### Aplicación
- [ ] HTTPS obligatorio
- [ ] Headers de seguridad
- [ ] Rate limiting
- [ ] Validación de entrada
- [ ] Auditoría completa

## Próximos Pasos

- [[Ops Seguridad Firewall|Configurar firewall]]
- [[Ops Seguridad Auditoria|Auditoría de seguridad]]
- [[Ops Config SSL HTTPS|Configurar HTTPS]]
