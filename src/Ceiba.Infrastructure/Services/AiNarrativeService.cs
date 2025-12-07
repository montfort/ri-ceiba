using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Ceiba.Core.Interfaces;
using Ceiba.Shared.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Ceiba.Infrastructure.Services;

/// <summary>
/// Provider-agnostic AI narrative generation service.
/// Supports OpenAI, Azure OpenAI, and can be extended for local LLMs.
/// US4: Reportes Automatizados Diarios con IA.
/// </summary>
public class AiNarrativeService : IAiNarrativeService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AiNarrativeService> _logger;
    private readonly string _provider;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly string _endpoint;

    public AiNarrativeService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<AiNarrativeService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        // Load configuration
        _provider = configuration["AI:Provider"] ?? "OpenAI";
        _apiKey = configuration["AI:ApiKey"] ?? "";
        _model = configuration["AI:Model"] ?? "gpt-4";
        _endpoint = configuration["AI:Endpoint"] ?? "https://api.openai.com/v1/chat/completions";
    }

    public string ProviderName => _provider;

    public async Task<NarrativeResponseDto> GenerateNarrativeAsync(
        NarrativeRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("AI API key not configured. Returning fallback narrative.");
            return CreateFallbackNarrative(request);
        }

        try
        {
            var prompt = BuildPrompt(request);

            var response = _provider.ToLower() switch
            {
                "openai" => await CallOpenAiAsync(prompt, cancellationToken),
                "azureopenai" => await CallAzureOpenAiAsync(prompt, cancellationToken),
                "local" => await CallLocalLlmAsync(prompt, cancellationToken),
                _ => await CallOpenAiAsync(prompt, cancellationToken)
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
        if (string.IsNullOrEmpty(_apiKey))
            return false;

        try
        {
            // Simple health check - just verify we can reach the endpoint
            using var request = new HttpRequestMessage(HttpMethod.Head, _endpoint);
            request.Headers.Add("Authorization", $"Bearer {_apiKey}");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed;
        }
        catch
        {
            return false;
        }
    }

    private string BuildPrompt(NarrativeRequestDto request)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Genera un resumen narrativo profesional para un reporte de incidencias de género.");
        sb.AppendLine($"Período: {request.FechaInicio:dd/MM/yyyy} al {request.FechaFin:dd/MM/yyyy}");
        sb.AppendLine();
        sb.AppendLine("ESTADÍSTICAS:");
        sb.AppendLine($"- Total de reportes: {request.Statistics.TotalReportes}");
        sb.AppendLine($"- Reportes entregados: {request.Statistics.ReportesEntregados}");

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
        foreach (var item in request.Statistics.PorDelito.Take(5))
            sb.AppendLine($"- {item.Key}: {item.Value}");

        if (request.HechosReportados.Any())
        {
            sb.AppendLine();
            sb.AppendLine("RESUMEN DE HECHOS REPORTADOS (muestra):");
            foreach (var hecho in request.HechosReportados.Take(3))
                sb.AppendLine($"- {hecho.Substring(0, Math.Min(200, hecho.Length))}...");
        }

        sb.AppendLine();
        sb.AppendLine("Instrucciones:");
        sb.AppendLine("1. Redacta un resumen ejecutivo de 2-3 párrafos.");
        sb.AppendLine("2. Usa un tono formal y profesional.");
        sb.AppendLine("3. Destaca las tendencias más importantes.");
        sb.AppendLine("4. Incluye recomendaciones si es apropiado.");
        sb.AppendLine("5. El texto debe estar en español.");

        return sb.ToString();
    }

    private async Task<NarrativeResponseDto> CallOpenAiAsync(string prompt, CancellationToken cancellationToken)
    {
        var requestBody = new
        {
            model = _model,
            messages = new[]
            {
                new { role = "system", content = "Eres un analista de seguridad pública especializado en género que genera reportes ejecutivos." },
                new { role = "user", content = prompt }
            },
            max_tokens = 1000,
            temperature = 0.7
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint);
        request.Headers.Add("Authorization", $"Bearer {_apiKey}");
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

    private async Task<NarrativeResponseDto> CallAzureOpenAiAsync(string prompt, CancellationToken cancellationToken)
    {
        // Azure OpenAI uses a slightly different endpoint format
        var azureEndpoint = _configuration["AI:AzureEndpoint"] ?? _endpoint;
        var apiVersion = _configuration["AI:AzureApiVersion"] ?? "2024-02-15-preview";

        var requestBody = new
        {
            messages = new[]
            {
                new { role = "system", content = "Eres un analista de seguridad pública especializado en género que genera reportes ejecutivos." },
                new { role = "user", content = prompt }
            },
            max_tokens = 1000,
            temperature = 0.7
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{azureEndpoint}?api-version={apiVersion}");
        request.Headers.Add("api-key", _apiKey);
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

    private async Task<NarrativeResponseDto> CallLocalLlmAsync(string prompt, CancellationToken cancellationToken)
    {
        // Local LLM endpoint (e.g., Ollama, LM Studio)
        var localEndpoint = _configuration["AI:LocalEndpoint"] ?? "http://localhost:11434/api/generate";

        var requestBody = new
        {
            model = _model,
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
            _logger.LogError("Local LLM error: {StatusCode} - {Content}", response.StatusCode, responseContent);
            throw new Exception($"Local LLM error: {response.StatusCode}");
        }

        using var doc = JsonDocument.Parse(responseContent);
        var narrative = doc.RootElement.GetProperty("response").GetString() ?? "";

        return new NarrativeResponseDto
        {
            Narrativa = narrative,
            Success = true,
            TokensUsed = 0 // Local LLMs typically don't report tokens
        };
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
