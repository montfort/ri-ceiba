# Variables de Entorno

Esta guía describe todas las variables de entorno disponibles para configurar Ceiba.

## Variables Requeridas

### Conexión a Base de Datos

```bash
ConnectionStrings__DefaultConnection="Host=localhost;Database=ceiba;Username=ceiba;Password=tu_password"
```

| Parámetro | Descripción |
|-----------|-------------|
| Host | Servidor PostgreSQL |
| Database | Nombre de la base de datos |
| Username | Usuario de la base de datos |
| Password | Contraseña del usuario |

### Entorno de Ejecución

```bash
ASPNETCORE_ENVIRONMENT=Production
```

Valores posibles:
- `Development` - Modo desarrollo con hot reload
- `Staging` - Ambiente de pruebas
- `Production` - Producción con optimizaciones

### URLs de la Aplicación

```bash
ASPNETCORE_URLS="http://localhost:5000;https://localhost:5001"
```

## Variables de Email

```bash
Email__Host=smtp.proveedor.com
Email__Port=587
Email__Username=notificaciones@org.com
Email__Password=password_seguro
Email__FromAddress=ceiba@org.com
Email__FromName="Sistema Ceiba"
Email__EnableSsl=true
```

| Variable | Descripción | Requerido |
|----------|-------------|-----------|
| Email__Host | Servidor SMTP | Sí |
| Email__Port | Puerto SMTP (587/465/25) | Sí |
| Email__Username | Usuario SMTP | Sí |
| Email__Password | Contraseña SMTP | Sí |
| Email__FromAddress | Dirección remitente | Sí |
| Email__FromName | Nombre remitente | No |
| Email__EnableSsl | Usar SSL/TLS | No (default: true) |

## Variables de IA

```bash
AI__Provider=OpenAI
AI__ApiKey=sk-...
AI__Model=gpt-4
AI__MaxTokens=4096
AI__Temperature=0.7
```

| Variable | Descripción | Valores |
|----------|-------------|---------|
| AI__Provider | Proveedor de IA | OpenAI, AzureOpenAI, Local |
| AI__ApiKey | Clave API | Según proveedor |
| AI__Model | Modelo a usar | gpt-4, gpt-3.5-turbo, etc. |
| AI__MaxTokens | Tokens máximos | 1024-8192 |
| AI__Temperature | Creatividad | 0.0-1.0 |

### Azure OpenAI (alternativa)

```bash
AI__Provider=AzureOpenAI
AI__ApiKey=...
AI__Endpoint=https://tu-recurso.openai.azure.com/
AI__DeploymentName=gpt-4-deployment
```

## Variables de Reportes Automatizados

```bash
AutomatedReports__GenerationTime=06:00:00
AutomatedReports__Recipients=["supervisor1@org.com","supervisor2@org.com"]
AutomatedReports__Enabled=true
AutomatedReports__RetryCount=3
AutomatedReports__RetryDelayMinutes=5
```

| Variable | Descripción | Default |
|----------|-------------|---------|
| GenerationTime | Hora de generación (UTC) | 06:00:00 |
| Recipients | Lista JSON de destinatarios | [] |
| Enabled | Activar generación automática | false |
| RetryCount | Reintentos en caso de fallo | 3 |
| RetryDelayMinutes | Minutos entre reintentos | 5 |

## Variables de Seguridad

```bash
Security__SessionTimeoutMinutes=30
Security__MaxLoginAttempts=5
Security__LockoutDurationMinutes=15
Security__RequireHttps=true
Security__AllowedOrigins=["https://ceiba.org.com"]
```

## Variables de Logging

```bash
Logging__LogLevel__Default=Information
Logging__LogLevel__Microsoft=Warning
Logging__LogLevel__Microsoft.EntityFrameworkCore=Warning
Serilog__MinimumLevel=Information
Serilog__WriteTo__File__Path=/var/log/ceiba/app.log
```

## Configuración por Ambiente

### Desarrollo (appsettings.Development.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=ceiba_dev;Username=ceiba;Password=dev_password"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
```

### Producción (variables de entorno)

```bash
# No usar archivos de configuración con secretos en producción
# Usar variables de entorno o gestor de secretos

export ConnectionStrings__DefaultConnection="Host=prod-db;..."
export Email__Password="$(vault read -field=password secret/email)"
export AI__ApiKey="$(vault read -field=key secret/openai)"
```

## Docker Compose

```yaml
services:
  ceiba-web:
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=${DB_CONNECTION}
      - Email__Host=${SMTP_HOST}
      - Email__Password=${SMTP_PASSWORD}
      - AI__ApiKey=${OPENAI_KEY}
```

Con archivo `.env`:

```bash
# .env
DB_CONNECTION=Host=ceiba-db;Database=ceiba;Username=ceiba;Password=secreto
SMTP_HOST=smtp.gmail.com
SMTP_PASSWORD=app_password
OPENAI_KEY=sk-...
```

## Validación de Configuración

```bash
# Verificar que todas las variables están configuradas
dotnet run --project src/Ceiba.Web -- --validate-config

# O en Docker
docker compose exec ceiba-web dotnet Ceiba.Web.dll --validate-config
```

## Secretos y Seguridad

### NO hacer:
- ❌ Hardcodear secretos en código
- ❌ Incluir `.env` en control de versiones
- ❌ Usar passwords en logs

### Sí hacer:
- ✅ Usar variables de entorno para secretos
- ✅ Usar gestores de secretos (Vault, AWS Secrets Manager)
- ✅ Rotar credenciales periódicamente
- ✅ Usar passwords únicos por ambiente

## Próximos Pasos

- [[Ops-Config-Base-de-Datos|Configurar base de datos]]
- [[Ops-Config-Email-SMTP|Configurar email]]
- [[Ops-Config-IA|Configurar IA]]
