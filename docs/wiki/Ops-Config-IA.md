# Configuración de IA

Esta guía describe cómo configurar la integración de IA para la generación automática de reportes narrativos.

## Descripción General

Ceiba utiliza IA para:
- Generar narrativas de reportes automatizados
- Resumir múltiples incidencias en un texto coherente
- Crear análisis de tendencias

## Proveedores Soportados

| Proveedor | Modelo | Uso Recomendado |
|-----------|--------|-----------------|
| OpenAI | GPT-4, GPT-3.5 | Producción |
| Azure OpenAI | GPT-4 | Producción corporativa |
| Local (Ollama) | Llama, Mistral | Desarrollo/Testing |

## Configuración OpenAI

### Variables de Entorno

```bash
AI__Provider=OpenAI
AI__ApiKey=sk-proj-xxxxxxxxxxxx
AI__Model=gpt-4
AI__MaxTokens=4096
AI__Temperature=0.7
```

### Obtener API Key

1. Visita [platform.openai.com](https://platform.openai.com)
2. Inicia sesión o crea cuenta
3. Ve a API Keys → Create new secret key
4. Copia y guarda la clave (solo se muestra una vez)

### Modelos Disponibles

| Modelo | Costo | Calidad | Velocidad |
|--------|-------|---------|-----------|
| gpt-4 | Alto | Excelente | Medio |
| gpt-4-turbo | Medio | Excelente | Rápido |
| gpt-3.5-turbo | Bajo | Buena | Muy rápido |

### Ejemplo de Configuración

```json
{
  "AI": {
    "Provider": "OpenAI",
    "ApiKey": "sk-proj-...",
    "Model": "gpt-4-turbo",
    "MaxTokens": 4096,
    "Temperature": 0.7,
    "Timeout": 60
  }
}
```

## Configuración Azure OpenAI

### Variables de Entorno

```bash
AI__Provider=AzureOpenAI
AI__ApiKey=xxxxxxxxxxxxxxxxxxxxxxxxx
AI__Endpoint=https://tu-recurso.openai.azure.com/
AI__DeploymentName=gpt-4-deployment
AI__ApiVersion=2024-02-15-preview
```

### Crear Recurso en Azure

1. Portal de Azure → Crear recurso → Azure OpenAI
2. Configurar nombre, región, pricing tier
3. Ir a Azure OpenAI Studio
4. Deployments → Create deployment
5. Seleccionar modelo y nombre de deployment

### Ejemplo de Configuración

```json
{
  "AI": {
    "Provider": "AzureOpenAI",
    "Endpoint": "https://mi-recurso.openai.azure.com/",
    "ApiKey": "...",
    "DeploymentName": "gpt-4",
    "ApiVersion": "2024-02-15-preview",
    "MaxTokens": 4096,
    "Temperature": 0.7
  }
}
```

## Configuración Local (Ollama)

Para desarrollo y testing sin costos de API.

### Instalar Ollama

```bash
# Linux
curl -fsSL https://ollama.com/install.sh | sh

# Descargar modelo
ollama pull llama2
```

### Variables de Entorno

```bash
AI__Provider=Local
AI__Endpoint=http://localhost:11434
AI__Model=llama2
```

### Docker Compose con Ollama

```yaml
services:
  ollama:
    image: ollama/ollama
    volumes:
      - ollama-data:/root/.ollama
    ports:
      - "11434:11434"

  ceiba-web:
    environment:
      - AI__Provider=Local
      - AI__Endpoint=http://ollama:11434
      - AI__Model=llama2

volumes:
  ollama-data:
```

## Parámetros de Generación

### Temperature

Controla la creatividad de las respuestas:

| Valor | Comportamiento |
|-------|----------------|
| 0.0 | Determinístico, siempre igual |
| 0.3 | Conservador, poco variado |
| 0.7 | Balanceado (recomendado) |
| 1.0 | Muy creativo, puede ser inconsistente |

### MaxTokens

Límite de tokens en la respuesta:

| Uso | Tokens Recomendados |
|-----|---------------------|
| Resumen corto | 500-1000 |
| Reporte diario | 2000-4000 |
| Análisis detallado | 4000-8000 |

### Ejemplo de Prompt

El sistema usa prompts optimizados para reportes:

```
Genera un reporte narrativo de incidencias policiales para el día {fecha}.

Datos de entrada:
- Total de incidencias: {count}
- Zonas afectadas: {zonas}
- Tipos de delitos: {delitos}

Instrucciones:
1. Escribir en español formal
2. Incluir estadísticas relevantes
3. Destacar tendencias
4. Mantener tono profesional
```

## Costos y Límites

### OpenAI Pricing (aproximado)

| Modelo | Input | Output |
|--------|-------|--------|
| GPT-4 | $0.03/1K tokens | $0.06/1K tokens |
| GPT-4 Turbo | $0.01/1K tokens | $0.03/1K tokens |
| GPT-3.5 Turbo | $0.0005/1K tokens | $0.0015/1K tokens |

### Estimación de Costos

Un reporte automatizado típico:
- Input: ~2000 tokens (datos de incidencias)
- Output: ~1500 tokens (narrativa)

Con GPT-4 Turbo:
- Costo por reporte: ~$0.065
- Costo mensual (30 reportes): ~$2

### Rate Limits

Configurar para respetar límites del proveedor:

```json
{
  "AI": {
    "RateLimitRequestsPerMinute": 60,
    "RateLimitTokensPerMinute": 90000,
    "RetryCount": 3,
    "RetryDelaySeconds": 5
  }
}
```

## Verificar Configuración

### Probar Conexión

```bash
# OpenAI
curl https://api.openai.com/v1/models \
  -H "Authorization: Bearer $AI__ApiKey"

# Azure OpenAI
curl "$AI__Endpoint/openai/deployments?api-version=2024-02-15-preview" \
  -H "api-key: $AI__ApiKey"

# Local Ollama
curl http://localhost:11434/api/tags
```

### Desde la Aplicación

```bash
# Generar reporte de prueba
curl -X POST http://localhost:5000/api/automated-reports/test \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"date": "2025-01-15"}'
```

## Troubleshooting

### Error: Invalid API Key

```
OpenAI.OpenAIException: Invalid API Key
```

**Solución:** Verificar que la API key es válida y no ha expirado.

### Error: Rate Limit Exceeded

```
OpenAI.OpenAIException: Rate limit reached
```

**Solución:** Reducir frecuencia de llamadas o aumentar tier en OpenAI.

### Error: Model Not Found

```
OpenAI.OpenAIException: The model does not exist
```

**Solución:** Verificar nombre del modelo. En Azure, usar el nombre del deployment.

### Error: Context Length Exceeded

```
OpenAI.OpenAIException: maximum context length exceeded
```

**Solución:** Reducir el tamaño del prompt o usar modelo con más contexto.

## Seguridad

### Proteger API Key

```bash
# Usar variables de entorno, nunca hardcodear
export AI__ApiKey="$(vault read -field=key secret/openai)"
```

### Filtrar Datos Sensibles

El sistema automáticamente:
- No envía datos personales identificables a la IA
- Anonimiza información sensible antes del procesamiento
- Registra uso en auditoría sin incluir prompts completos

## Fallback y Resiliencia

Configurar comportamiento cuando IA no está disponible:

```json
{
  "AI": {
    "Enabled": true,
    "FallbackEnabled": true,
    "FallbackMessage": "Narrativa no disponible. Consulte los datos estructurados."
  }
}
```

## Próximos Pasos

- [Configurar email para envío](Ops-Config-Email-SMTP)
- [Usar reportes automatizados](Usuario-Revisor-Reportes-Automatizados)
- [Monitorear uso de IA](Ops-Mant-Monitoreo)
