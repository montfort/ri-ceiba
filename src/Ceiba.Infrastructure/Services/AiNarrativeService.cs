using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Ceiba.Core.Entities;
using Ceiba.Core.Interfaces;
using Ceiba.Shared.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Ceiba.Infrastructure.Services;

/// <summary>
/// Provider-agnostic AI narrative generation service.
/// Supports OpenAI, Azure OpenAI, Ollama, and local LLMs.
/// Configuration is loaded from database (via IAiConfigurationService) with fallback to appsettings.
/// US4: Reportes Automatizados Diarios con IA.
/// </summary>
public class AiNarrativeService : IAiNarrativeService
{
    private readonly HttpClient _httpClient;
    private readonly IAiConfigurationService _configService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AiNarrativeService> _logger;

    // Cached configuration for performance
    private ConfiguracionIA? _cachedConfig;
    private DateTime _configCacheExpiry = DateTime.MinValue;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public AiNarrativeService(
        HttpClient httpClient,
        IAiConfigurationService configService,
        IConfiguration configuration,
        ILogger<AiNarrativeService> logger)
    {
        _httpClient = httpClient;
        _configService = configService;
        _configuration = configuration;
        _logger = logger;
    }

    public string ProviderName => GetCachedConfigSync()?.Proveedor ?? _configuration["AI:Provider"] ?? "OpenAI";

    public async Task<NarrativeResponseDto> GenerateNarrativeAsync(
        NarrativeRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var config = await GetConfigurationAsync(cancellationToken);

        if (config == null || string.IsNullOrEmpty(config.ApiKey) && RequiresApiKey(config.Proveedor))
        {
            _logger.LogWarning("AI API key not configured. Returning fallback narrative.");
            return CreateFallbackNarrative(request);
        }

        try
        {
            var prompt = BuildPrompt(request);

            var response = config.Proveedor.ToLower() switch
            {
                "openai" => await CallOpenAiAsync(config, prompt, cancellationToken),
                "gemini" => await CallGeminiAsync(config, prompt, cancellationToken),
                "deepseek" => await CallDeepSeekAsync(config, prompt, cancellationToken),
                "azureopenai" => await CallAzureOpenAiAsync(config, prompt, cancellationToken),
                "local" or "ollama" => await CallLocalLlmAsync(config, prompt, cancellationToken),
                _ => await CallOpenAiAsync(config, prompt, cancellationToken)
            };

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI narrative. Using fallback.");
            return CreateFallbackNarrative(request);
        }
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        var config = await GetConfigurationAsync(cancellationToken);

        if (config == null)
            return false;

        if (RequiresApiKey(config.Proveedor) && string.IsNullOrEmpty(config.ApiKey))
            return false;

        try
        {
            var endpoint = GetEndpoint(config);
            using var request = new HttpRequestMessage(HttpMethod.Head, endpoint);

            if (!string.IsNullOrEmpty(config.ApiKey))
            {
                if (config.Proveedor.ToLower() == "azureopenai")
                    request.Headers.Add("api-key", config.ApiKey);
                else
                    request.Headers.Add("Authorization", $"Bearer {config.ApiKey}");
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed;
        }
        catch
        {
            return false;
        }
    }

    private async Task<ConfiguracionIA?> GetConfigurationAsync(CancellationToken cancellationToken)
    {
        // Check cache
        if (_cachedConfig != null && DateTime.UtcNow < _configCacheExpiry)
        {
            return _cachedConfig;
        }

        // Load from database
        var dbConfig = await _configService.GetActiveConfigurationAsync(cancellationToken);

        if (dbConfig != null)
        {
            _cachedConfig = dbConfig;
            _configCacheExpiry = DateTime.UtcNow.Add(CacheDuration);
            return dbConfig;
        }

        // Fallback to appsettings configuration
        var fallbackConfig = new ConfiguracionIA
        {
            Proveedor = _configuration["AI:Provider"] ?? "OpenAI",
            ApiKey = _configuration["AI:ApiKey"] ?? "",
            Modelo = _configuration["AI:Model"] ?? "gpt-4",
            Endpoint = _configuration["AI:Endpoint"] ?? "https://api.openai.com/v1/chat/completions",
            AzureEndpoint = _configuration["AI:AzureEndpoint"],
            AzureApiVersion = _configuration["AI:AzureApiVersion"] ?? "2024-02-15-preview",
            LocalEndpoint = _configuration["AI:LocalEndpoint"] ?? "http://localhost:11434/api/generate",
            MaxTokens = int.TryParse(_configuration["AI:MaxTokens"], out var tokens) ? tokens : 1000,
            Temperature = double.TryParse(_configuration["AI:Temperature"], out var temp) ? temp : 0.7
        };

        _cachedConfig = fallbackConfig;
        _configCacheExpiry = DateTime.UtcNow.Add(CacheDuration);
        return fallbackConfig;
    }

    private ConfiguracionIA? GetCachedConfigSync()
    {
        if (_cachedConfig != null && DateTime.UtcNow < _configCacheExpiry)
        {
            return _cachedConfig;
        }
        return null;
    }

    private static bool RequiresApiKey(string provider)
    {
        return provider.ToLower() switch
        {
            "openai" or "azureopenai" or "gemini" or "deepseek" => true,
            _ => false
        };
    }

    private static string GetEndpoint(ConfiguracionIA config)
    {
        return config.Proveedor.ToLower() switch
        {
            "openai" => config.Endpoint ?? "https://api.openai.com/v1/chat/completions",
            "gemini" => config.Endpoint ?? "https://generativelanguage.googleapis.com/v1beta/models",
            "deepseek" => config.Endpoint ?? "https://api.deepseek.com/chat/completions",
            "azureopenai" => config.AzureEndpoint ?? config.Endpoint ?? "",
            "local" or "ollama" => config.LocalEndpoint ?? "http://localhost:11434/api/generate",
            _ => config.Endpoint ?? "https://api.openai.com/v1/chat/completions"
        };
    }

    private string BuildPrompt(NarrativeRequestDto request)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Genera un resumen narrativo profesional para un reporte de incidencias de género.");
        sb.AppendLine($"Período: {request.FechaInicio:dd/MM/yyyy} al {request.FechaFin:dd/MM/yyyy}");
        sb.AppendLine();
        sb.AppendLine("ESTADÍSTICAS GENERALES:");
        sb.AppendLine($"- Total de reportes entregados: {request.Statistics.TotalReportes}");

        if (!string.IsNullOrEmpty(request.Statistics.DelitoMasFrecuente))
            sb.AppendLine($"- Delito más frecuente: {request.Statistics.DelitoMasFrecuente}");

        if (!string.IsNullOrEmpty(request.Statistics.ZonaMasActiva))
            sb.AppendLine($"- Zona con más incidencias: {request.Statistics.ZonaMasActiva}");

        if (request.Statistics.TotalLgbtttiq > 0)
            sb.AppendLine($"- Casos LGBTTTIQ+: {request.Statistics.TotalLgbtttiq}");

        if (request.Statistics.TotalMigrantes > 0)
            sb.AppendLine($"- Casos de migrantes: {request.Statistics.TotalMigrantes}");

        sb.AppendLine();
        sb.AppendLine("DISTRIBUCIÓN POR SEXO:");
        foreach (var item in request.Statistics.PorSexo)
            sb.AppendLine($"- {item.Key}: {item.Value}");

        sb.AppendLine();
        sb.AppendLine("DISTRIBUCIÓN POR TIPO DE DELITO:");
        foreach (var item in request.Statistics.PorDelito.OrderByDescending(x => x.Value).Take(10))
            sb.AppendLine($"- {item.Key}: {item.Value}");

        // Include ALL incidents with complete details
        if (request.Incidents.Any())
        {
            sb.AppendLine();
            sb.AppendLine($"DETALLE DE TODOS LOS CASOS REPORTADOS ({request.Incidents.Count} casos):");
            sb.AppendLine();

            int caseNumber = 1;
            foreach (var incident in request.Incidents.OrderByDescending(i => i.FechaReporte))
            {
                sb.AppendLine($"--- Caso #{caseNumber} ---");
                sb.AppendLine($"Folio: {incident.Folio}");
                sb.AppendLine($"Fecha: {incident.FechaReporte:dd/MM/yyyy HH:mm}");
                sb.AppendLine($"Tipo de delito: {incident.Delito}");
                sb.AppendLine($"Hechos reportados: {incident.HechosReportados}");
                sb.AppendLine($"Acciones realizadas: {incident.AccionesRealizadas}");
                sb.AppendLine();
                caseNumber++;
            }
        }

        sb.AppendLine();
        sb.AppendLine("INSTRUCCIONES PARA LA NARRATIVA:");
        sb.AppendLine("1. Redacta un resumen ejecutivo que incluya TODOS los casos reportados.");
        sb.AppendLine("2. Para cada caso, menciona el folio, tipo de delito y un breve resumen de los hechos.");
        sb.AppendLine("3. Organiza los casos por tipo de delito o cronológicamente según consideres más apropiado.");
        sb.AppendLine("4. Usa un tono formal y profesional.");
        sb.AppendLine("5. Destaca las tendencias más importantes observadas.");
        sb.AppendLine("6. Incluye recomendaciones basadas en los patrones identificados.");
        sb.AppendLine("7. El texto debe estar en español.");
        sb.AppendLine("8. IMPORTANTE: Asegúrate de mencionar explícitamente cada uno de los casos listados arriba.");
        sb.AppendLine("9. NO menciones diferencias entre 'reportes totales' y 'reportes entregados' - todos los reportes en este análisis ya fueron entregados.");
        sb.AppendLine("10. En el primer párrafo, usa solo 'reportes' o 'reportes entregados', no ambos términos juntos.");

        return sb.ToString();
    }

    private async Task<NarrativeResponseDto> CallOpenAiAsync(
        ConfiguracionIA config,
        string prompt,
        CancellationToken cancellationToken)
    {
        var endpoint = config.Endpoint ?? "https://api.openai.com/v1/chat/completions";

        var requestBody = new
        {
            model = config.Modelo,
            messages = new[]
            {
                new { role = "system", content = "Eres un analista de seguridad pública especializado en género que genera reportes ejecutivos." },
                new { role = "user", content = prompt }
            },
            max_tokens = config.MaxTokens,
            temperature = config.Temperature
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Add("Authorization", $"Bearer {config.ApiKey}");
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("OpenAI API error: {StatusCode} - {Content}", response.StatusCode, responseContent);
            throw new Exception($"OpenAI API error: {response.StatusCode}");
        }

        using var doc = JsonDocument.Parse(responseContent);
        var narrative = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "";

        var tokensUsed = doc.RootElement
            .GetProperty("usage")
            .GetProperty("total_tokens")
            .GetInt32();

        return new NarrativeResponseDto
        {
            Narrativa = narrative,
            Success = true,
            TokensUsed = tokensUsed
        };
    }

    private async Task<NarrativeResponseDto> CallDeepSeekAsync(
        ConfiguracionIA config,
        string prompt,
        CancellationToken cancellationToken)
    {
        // DeepSeek API - always use the correct endpoint
        const string endpoint = "https://api.deepseek.com/chat/completions";

        var requestBody = new
        {
            model = config.Modelo,
            messages = new[]
            {
                new { role = "system", content = "Eres un analista de seguridad pública especializado en género que genera reportes ejecutivos." },
                new { role = "user", content = prompt }
            },
            max_tokens = config.MaxTokens,
            temperature = config.Temperature
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Add("Authorization", $"Bearer {config.ApiKey}");
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("DeepSeek API error: {StatusCode} - {Content}", response.StatusCode, responseContent);
            throw new Exception($"DeepSeek API error: {response.StatusCode}");
        }

        using var doc = JsonDocument.Parse(responseContent);
        var narrative = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "";

        var tokensUsed = 0;
        if (doc.RootElement.TryGetProperty("usage", out var usage) &&
            usage.TryGetProperty("total_tokens", out var totalTokens))
        {
            tokensUsed = totalTokens.GetInt32();
        }

        return new NarrativeResponseDto
        {
            Narrativa = narrative,
            Success = true,
            TokensUsed = tokensUsed
        };
    }

    private async Task<NarrativeResponseDto> CallGeminiAsync(
        ConfiguracionIA config,
        string prompt,
        CancellationToken cancellationToken)
    {
        var baseEndpoint = config.Endpoint ?? "https://generativelanguage.googleapis.com/v1beta/models";
        var endpoint = $"{baseEndpoint}/{config.Modelo}:generateContent";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = $"Eres un analista de seguridad pública especializado en género que genera reportes ejecutivos.\n\n{prompt}" }
                    }
                }
            },
            generationConfig = new
            {
                maxOutputTokens = config.MaxTokens,
                temperature = config.Temperature
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Add("x-goog-api-key", config.ApiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Gemini API error: {StatusCode} - {Content}", response.StatusCode, responseContent);
            throw new Exception($"Gemini API error: {response.StatusCode}");
        }

        using var doc = JsonDocument.Parse(responseContent);

        // Gemini response format: { "candidates": [{ "content": { "parts": [{ "text": "..." }] } }] }
        var narrative = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? "";

        // Try to get token usage if available
        int tokensUsed = 0;
        if (doc.RootElement.TryGetProperty("usageMetadata", out var usage))
        {
            if (usage.TryGetProperty("totalTokenCount", out var totalTokens))
            {
                tokensUsed = totalTokens.GetInt32();
            }
        }

        return new NarrativeResponseDto
        {
            Narrativa = narrative,
            Success = true,
            TokensUsed = tokensUsed
        };
    }

    private async Task<NarrativeResponseDto> CallAzureOpenAiAsync(
        ConfiguracionIA config,
        string prompt,
        CancellationToken cancellationToken)
    {
        var azureEndpoint = config.AzureEndpoint ?? config.Endpoint ?? "";
        var apiVersion = config.AzureApiVersion ?? "2024-02-15-preview";

        var requestBody = new
        {
            messages = new[]
            {
                new { role = "system", content = "Eres un analista de seguridad pública especializado en género que genera reportes ejecutivos." },
                new { role = "user", content = prompt }
            },
            max_tokens = config.MaxTokens,
            temperature = config.Temperature
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{azureEndpoint}?api-version={apiVersion}");
        request.Headers.Add("api-key", config.ApiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Azure OpenAI API error: {StatusCode} - {Content}", response.StatusCode, responseContent);
            throw new Exception($"Azure OpenAI API error: {response.StatusCode}");
        }

        using var doc = JsonDocument.Parse(responseContent);
        var narrative = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "";

        var tokensUsed = doc.RootElement
            .GetProperty("usage")
            .GetProperty("total_tokens")
            .GetInt32();

        return new NarrativeResponseDto
        {
            Narrativa = narrative,
            Success = true,
            TokensUsed = tokensUsed
        };
    }

    private async Task<NarrativeResponseDto> CallLocalLlmAsync(
        ConfiguracionIA config,
        string prompt,
        CancellationToken cancellationToken)
    {
        var localEndpoint = config.LocalEndpoint ?? "http://localhost:11434/api/generate";

        // Detect if it's Ollama based on endpoint
        bool isOllama = localEndpoint.Contains("11434") || localEndpoint.Contains("ollama") ||
                        config.Proveedor.ToLower() == "ollama";

        if (isOllama)
        {
            var requestBody = new
            {
                model = config.Modelo,
                prompt = prompt,
                stream = false
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, localEndpoint);
            request.Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Ollama error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                throw new Exception($"Ollama error: {response.StatusCode}");
            }

            using var doc = JsonDocument.Parse(responseContent);
            var narrative = doc.RootElement.GetProperty("response").GetString() ?? "";

            return new NarrativeResponseDto
            {
                Narrativa = narrative,
                Success = true,
                TokensUsed = 0
            };
        }
        else
        {
            // Generic OpenAI-compatible local LLM
            var requestBody = new
            {
                model = config.Modelo,
                messages = new[]
                {
                    new { role = "system", content = "Eres un analista de seguridad pública especializado en género que genera reportes ejecutivos." },
                    new { role = "user", content = prompt }
                },
                max_tokens = config.MaxTokens,
                temperature = config.Temperature
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, localEndpoint);
            request.Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Local LLM error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                throw new Exception($"Local LLM error: {response.StatusCode}");
            }

            using var doc = JsonDocument.Parse(responseContent);
            var narrative = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "";

            return new NarrativeResponseDto
            {
                Narrativa = narrative,
                Success = true,
                TokensUsed = 0
            };
        }
    }

    private NarrativeResponseDto CreateFallbackNarrative(NarrativeRequestDto request)
    {
        var stats = request.Statistics;
        var sb = new StringBuilder();

        sb.AppendLine($"## Resumen del período {request.FechaInicio:dd/MM/yyyy} - {request.FechaFin:dd/MM/yyyy}");
        sb.AppendLine();
        sb.AppendLine($"Durante el período analizado se registraron un total de **{stats.TotalReportes} reportes de incidencias**, ");
        sb.AppendLine($"de los cuales {stats.ReportesEntregados} fueron formalmente entregados.");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(stats.DelitoMasFrecuente))
        {
            sb.AppendLine($"El tipo de delito más frecuente fue **{stats.DelitoMasFrecuente}**, ");
            sb.AppendLine("lo cual requiere atención prioritaria en las estrategias de prevención.");
        }

        if (!string.IsNullOrEmpty(stats.ZonaMasActiva))
        {
            sb.AppendLine($"La zona con mayor número de incidencias fue **{stats.ZonaMasActiva}**, ");
            sb.AppendLine("sugiriendo la necesidad de reforzar la presencia y acciones preventivas en dicha área.");
        }

        sb.AppendLine();
        sb.AppendLine("*Nota: Este resumen fue generado automáticamente sin asistencia de IA debido a configuración del sistema.*");

        return new NarrativeResponseDto
        {
            Narrativa = sb.ToString(),
            Success = true,
            Error = "AI service not configured - using fallback template"
        };
    }
}
