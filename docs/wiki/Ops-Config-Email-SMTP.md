# Configuración de Email SMTP

Esta guía describe cómo configurar el envío de emails en Ceiba usando MailKit.

## Descripción General

Ceiba utiliza email para:
- Notificaciones de cuenta (bienvenida, recuperación de contraseña)
- Envío de reportes automatizados diarios
- Alertas del sistema

## Configuración Básica

### Variables de Entorno

```bash
Email__Host=smtp.proveedor.com
Email__Port=587
Email__Username=notificaciones@org.com
Email__Password=password_seguro
Email__FromAddress=ceiba@org.com
Email__FromName="Sistema Ceiba"
Email__EnableSsl=true
```

### appsettings.json

```json
{
  "Email": {
    "Host": "smtp.proveedor.com",
    "Port": 587,
    "Username": "notificaciones@org.com",
    "Password": "password_seguro",
    "FromAddress": "ceiba@org.com",
    "FromName": "Sistema Ceiba",
    "EnableSsl": true
  }
}
```

## Proveedores Comunes

### Gmail

```bash
Email__Host=smtp.gmail.com
Email__Port=587
Email__Username=tu_cuenta@gmail.com
Email__Password=app_password  # Contraseña de aplicación
Email__EnableSsl=true
```

> **Nota:** Gmail requiere una "Contraseña de aplicación" si tienes 2FA activado.
> Genera una en: Google Account → Security → App passwords

### Microsoft 365 / Outlook

```bash
Email__Host=smtp.office365.com
Email__Port=587
Email__Username=tu_cuenta@dominio.com
Email__Password=tu_password
Email__EnableSsl=true
```

### Amazon SES

```bash
Email__Host=email-smtp.us-east-1.amazonaws.com
Email__Port=587
Email__Username=AKIAIOSFODNN7EXAMPLE
Email__Password=wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY
Email__EnableSsl=true
```

### SendGrid

```bash
Email__Host=smtp.sendgrid.net
Email__Port=587
Email__Username=apikey
Email__Password=SG.xxxx...  # API Key
Email__EnableSsl=true
```

### Servidor SMTP Propio

```bash
Email__Host=mail.tudominio.com
Email__Port=587
Email__Username=ceiba@tudominio.com
Email__Password=password
Email__EnableSsl=true
```

## Puertos y Seguridad

| Puerto | Protocolo | Uso |
|--------|-----------|-----|
| 25 | SMTP | No recomendado (sin cifrado) |
| 465 | SMTPS | SSL implícito |
| 587 | Submission | TLS explícito (STARTTLS) - Recomendado |

### Configuración por Puerto

#### Puerto 587 (Recomendado)

```bash
Email__Port=587
Email__EnableSsl=true  # Usa STARTTLS
```

#### Puerto 465

```bash
Email__Port=465
Email__EnableSsl=true  # SSL implícito
```

## Configuración de Reportes Automatizados

Los reportes automatizados se envían diariamente a los destinatarios configurados.

```bash
AutomatedReports__Recipients=["supervisor1@org.com","supervisor2@org.com"]
AutomatedReports__GenerationTime=06:00:00
AutomatedReports__Enabled=true
```

### Plantilla de Email

Los emails de reportes automatizados incluyen:
- Asunto con fecha del reporte
- Resumen en HTML
- Archivo PDF adjunto
- Archivo JSON adjunto (opcional)

## Prueba de Configuración

### Verificar Conexión SMTP

```bash
# Usando telnet
telnet smtp.proveedor.com 587

# Usando openssl
openssl s_client -connect smtp.proveedor.com:587 -starttls smtp
```

### Enviar Email de Prueba

```bash
# Desde la aplicación en modo debug
curl -X POST http://localhost:5000/api/admin/test-email \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"to": "test@example.com"}'
```

## Troubleshooting

### Error: Authentication Failed

```
MailKit.Security.AuthenticationException: Authentication failed
```

**Soluciones:**
1. Verificar usuario y contraseña
2. Para Gmail: usar contraseña de aplicación
3. Verificar que la cuenta no esté bloqueada

### Error: Connection Refused

```
System.Net.Sockets.SocketException: Connection refused
```

**Soluciones:**
1. Verificar host y puerto
2. Verificar firewall permite salida al puerto
3. Verificar DNS resuelve el host

### Error: Certificate Validation

```
MailKit.Security.SslHandshakeException: An error occurred
```

**Soluciones:**
1. Actualizar certificados del sistema
2. Verificar que el servidor SMTP tiene certificado válido

### Error: Timeout

```
System.TimeoutException: The operation has timed out
```

**Soluciones:**
1. Aumentar timeout de conexión
2. Verificar conectividad de red
3. Verificar que el servidor SMTP responde

## Configuración Avanzada

### Pool de Conexiones

```json
{
  "Email": {
    "MaxConnections": 5,
    "ConnectionTimeout": 30,
    "SendTimeout": 60
  }
}
```

### Reintentos

```json
{
  "Email": {
    "RetryCount": 3,
    "RetryDelaySeconds": 10
  }
}
```

### Rate Limiting

Algunos proveedores limitan la cantidad de emails:

| Proveedor | Límite |
|-----------|--------|
| Gmail | 500/día |
| Microsoft 365 | 10,000/día |
| Amazon SES | Según plan |
| SendGrid | Según plan |

## Logs de Email

Habilitar logs detallados de SMTP:

```json
{
  "Logging": {
    "LogLevel": {
      "MailKit": "Debug"
    }
  }
}
```

Ver logs:

```bash
# En Docker
docker compose logs ceiba-web | grep -i "email\|smtp\|mail"

# En Linux nativo
journalctl -u ceiba | grep -i "email\|smtp\|mail"
```

## Seguridad

### Proteger Credenciales

```bash
# No incluir en archivos de configuración
# Usar variables de entorno

export Email__Password="$(vault read -field=password secret/smtp)"
```

### SPF, DKIM, DMARC

Para mejorar la entregabilidad, configurar en el DNS:

```
# SPF
TXT @ "v=spf1 include:_spf.google.com ~all"

# DKIM
TXT selector._domainkey "v=DKIM1; k=rsa; p=..."

# DMARC
TXT _dmarc "v=DMARC1; p=quarantine; rua=mailto:dmarc@tudominio.com"
```

## Próximos Pasos

- [[Ops Config IA|Configurar IA para reportes]]
- [[Ops Config Variables Entorno|Todas las variables de entorno]]
- [[Usuario Revisor Reportes Automatizados|Usar reportes automatizados]]
