using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Ceiba.Core.Entities;
using Ceiba.Core.Interfaces;
using Ceiba.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ceiba.Infrastructure.Services;

/// <summary>
/// Service for managing AI configuration stored in the database.
/// US4: Reportes Automatizados Diarios con IA.
/// </summary>
public class AiConfigurationService : IAiConfigurationService
{
    private readonly CeibaDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAuditService _auditService;
    private readonly ILogger<AiConfigurationService> _logger;

    public AiConfigurationService(
        CeibaDbContext context,
        IHttpClientFactory httpClientFactory,
        IAuditService auditService,
        ILogger<AiConfigurationService> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<ConfiguracionIA?> GetActiveConfigurationAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ConfiguracionesIA
            .AsNoTracking()
            .Where(c => c.Activo)
            .OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ConfiguracionIA> SaveConfigurationAsync(
        ConfiguracionIA configuration,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Validate configuration
        var (isValid, errors) = configuration.Validate();
        if (!isValid)
        {
            throw new InvalidOperationException($"Configuración inválida: {string.Join(", ", errors)}");
        }

        // Deactivate all existing configurations (exclude the one being updated)
        var existingConfigs = await _context.ConfiguracionesIA
            .Where(c => c.Activo && c.Id != configuration.Id)
            .ToListAsync(cancellationToken);

        foreach (var existing in existingConfigs)
        {
            existing.Activo = false;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        // Set the new configuration as active
        configuration.Activo = true;
        configuration.ModificadoPorId = userId;

        if (configuration.Id == 0)
        {
            // New configuration
            configuration.CreatedAt = DateTime.UtcNow;
            _context.ConfiguracionesIA.Add(configuration);
        }
        else
        {
            // Update existing - check if already tracked
            var trackedEntity = _context.ChangeTracker.Entries<ConfiguracionIA>()
                .FirstOrDefault(e => e.Entity.Id == configuration.Id);

            if (trackedEntity != null)
            {
                // Update the tracked entity's values
                trackedEntity.CurrentValues.SetValues(configuration);
                trackedEntity.Entity.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Attach and mark as modified
                configuration.UpdatedAt = DateTime.UtcNow;
                _context.ConfiguracionesIA.Attach(configuration);
                _context.Entry(configuration).State = EntityState.Modified;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Audit log
        await _auditService.LogAsync(
            "AI_CONFIG_UPDATE",
            configuration.Id,
            "ConfiguracionIA",
            JsonSerializer.Serialize(new
            {
                configuration.Proveedor,
                configuration.Modelo,
                HasApiKey = !string.IsNullOrEmpty(configuration.ApiKey),
                configuration.MaxTokens,
                configuration.Temperature
            }),
            null,
            cancellationToken);

        _logger.LogInformation(
            "AI configuration updated by user {UserId}: Provider={Provider}, Model={Model}",
            userId,
            configuration.Proveedor,
            configuration.Modelo);

        return configuration;
    }

    public async Task<(bool Success, string Message)> TestConnectionAsync(
        ConfiguracionIA configuration,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            bool success;
            string message;

            switch (configuration.Proveedor.ToLower())
            {
                case "openai":
                    (success, message) = await TestOpenAiAsync(httpClient, configuration, cancellationToken);
                    break;

                case "azureopenai":
                    (success, message) = await TestAzureOpenAiAsync(httpClient, configuration, cancellationToken);
                    break;

                case "local":
                case "ollama":
                    (success, message) = await TestLocalLlmAsync(httpClient, configuration, cancellationToken);
                    break;

                default:
                    return (false, $"Proveedor no soportado: {configuration.Proveedor}");
            }

            stopwatch.Stop();

            if (success)
            {
                message = $"{message} (Tiempo de respuesta: {stopwatch.ElapsedMilliseconds}ms)";
            }

            return (success, message);
        }
        catch (TaskCanceledException)
        {
            return (false, "La conexión tardó demasiado tiempo (timeout de 30 segundos).");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "HTTP error testing AI connection");
            return (false, $"Error de conexión: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing AI connection");
            return (false, $"Error inesperado: {ex.Message}");
        }
    }

    public async Task<List<ConfiguracionIA>> GetConfigurationHistoryAsync(
        int take = 10,
        CancellationToken cancellationToken = default)
    {
        return await _context.ConfiguracionesIA
            .AsNoTracking()
            .OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    private async Task<(bool Success, string Message)> TestOpenAiAsync(
        HttpClient httpClient,
        ConfiguracionIA config,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config.ApiKey))
        {
            return (false, "API Key no configurada.");
        }

        var endpoint = config.Endpoint ?? "https://api.openai.com/v1/chat/completions";

        var requestBody = new
        {
            model = config.Modelo,
            messages = new[]
            {
                new { role = "user", content = "Responde solo con 'OK'" }
            },
            max_tokens = 10
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Add("Authorization", $"Bearer {config.ApiKey}");
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        var response = await httpClient.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return (true, $"Conexión exitosa con OpenAI. Modelo: {config.Modelo}");
        }

        var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
        return (false, $"Error de OpenAI ({response.StatusCode}): {GetErrorMessage(errorContent)}");
    }

    private async Task<(bool Success, string Message)> TestAzureOpenAiAsync(
        HttpClient httpClient,
        ConfiguracionIA config,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config.ApiKey))
        {
            return (false, "API Key no configurada.");
        }

        if (string.IsNullOrWhiteSpace(config.AzureEndpoint))
        {
            return (false, "Azure Endpoint no configurado.");
        }

        var apiVersion = config.AzureApiVersion ?? "2024-02-15-preview";
        var endpoint = $"{config.AzureEndpoint}?api-version={apiVersion}";

        var requestBody = new
        {
            messages = new[]
            {
                new { role = "user", content = "Responde solo con 'OK'" }
            },
            max_tokens = 10
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Add("api-key", config.ApiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        var response = await httpClient.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return (true, $"Conexión exitosa con Azure OpenAI. Modelo: {config.Modelo}");
        }

        var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
        return (false, $"Error de Azure OpenAI ({response.StatusCode}): {GetErrorMessage(errorContent)}");
    }

    private async Task<(bool Success, string Message)> TestLocalLlmAsync(
        HttpClient httpClient,
        ConfiguracionIA config,
        CancellationToken cancellationToken)
    {
        var endpoint = config.LocalEndpoint ?? "http://localhost:11434/api/generate";

        // Detect if it's Ollama or another LLM based on endpoint
        bool isOllama = endpoint.Contains("11434") || endpoint.Contains("ollama");

        if (isOllama)
        {
            // Ollama API format
            var requestBody = new
            {
                model = config.Modelo,
                prompt = "Responde solo con 'OK'",
                stream = false
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            var response = await httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return (true, $"Conexión exitosa con Ollama. Modelo: {config.Modelo}");
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            return (false, $"Error de Ollama ({response.StatusCode}): {errorContent}");
        }
        else
        {
            // Generic OpenAI-compatible API format
            var requestBody = new
            {
                model = config.Modelo,
                messages = new[]
                {
                    new { role = "user", content = "Responde solo con 'OK'" }
                },
                max_tokens = 10
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            var response = await httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return (true, $"Conexión exitosa con LLM local. Modelo: {config.Modelo}");
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            return (false, $"Error de LLM local ({response.StatusCode}): {errorContent}");
        }
    }

    private static string GetErrorMessage(string jsonResponse)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonResponse);
            if (doc.RootElement.TryGetProperty("error", out var error))
            {
                if (error.TryGetProperty("message", out var message))
                {
                    return message.GetString() ?? jsonResponse;
                }
                return error.GetString() ?? jsonResponse;
            }
            return jsonResponse;
        }
        catch
        {
            return jsonResponse;
        }
    }
}
