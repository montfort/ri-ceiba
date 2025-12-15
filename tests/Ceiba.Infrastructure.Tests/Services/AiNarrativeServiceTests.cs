using System.Net;
using System.Text.Json;
using Ceiba.Core.Entities;
using Ceiba.Core.Interfaces;
using Ceiba.Infrastructure.Services;
using Ceiba.Shared.DTOs;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RichardSzalay.MockHttp;

namespace Ceiba.Infrastructure.Tests.Services;

/// <summary>
/// Unit tests for AiNarrativeService.
/// Tests AI narrative generation with different providers.
/// </summary>
public class AiNarrativeServiceTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHttp;
    private readonly HttpClient _httpClient;
    private readonly IAiConfigurationService _configService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AiNarrativeService> _logger;
    private readonly AiNarrativeService _service;

    public AiNarrativeServiceTests()
    {
        _mockHttp = new MockHttpMessageHandler();
        _httpClient = _mockHttp.ToHttpClient();

        _configService = Substitute.For<IAiConfigurationService>();
        _logger = Substitute.For<ILogger<AiNarrativeService>>();

        var configData = new Dictionary<string, string?>
        {
            ["AI:Provider"] = "OpenAI",
            ["AI:ApiKey"] = "test-api-key",
            ["AI:Model"] = "gpt-4",
            ["AI:Endpoint"] = "https://api.openai.com/v1/chat/completions",
            ["AI:MaxTokens"] = "1000",
            ["AI:Temperature"] = "0.7"
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _service = new AiNarrativeService(
            _httpClient,
            _configService,
            _configuration,
            _logger);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _mockHttp.Dispose();
        GC.SuppressFinalize(this);
    }

    #region GenerateNarrativeAsync Tests

    [Fact(DisplayName = "GenerateNarrativeAsync should return fallback when no API key")]
    public async Task GenerateNarrativeAsync_ReturnsFallback_WhenNoApiKey()
    {
        // Arrange
        _configService.GetActiveConfigurationAsync(Arg.Any<CancellationToken>())
            .Returns((ConfiguracionIA?)null);

        var configData = new Dictionary<string, string?>
        {
            ["AI:Provider"] = "OpenAI",
            ["AI:ApiKey"] = "", // Empty API key
            ["AI:Model"] = "gpt-4"
        };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var service = new AiNarrativeService(
            _httpClient,
            _configService,
            config,
            _logger);

        var request = CreateTestNarrativeRequest();

        // Act
        var result = await service.GenerateNarrativeAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Narrativa.Should().Contain("Resumen del período");
        result.Error.Should().Contain("fallback");
    }

    [Fact(DisplayName = "GenerateNarrativeAsync should call OpenAI API successfully")]
    public async Task GenerateNarrativeAsync_CallsOpenAI_Successfully()
    {
        // Arrange
        SetupOpenAiConfig();
        SetupOpenAiMockResponse("This is the AI generated narrative.");

        var request = CreateTestNarrativeRequest();

        // Act
        var result = await _service.GenerateNarrativeAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Narrativa.Should().Be("This is the AI generated narrative.");
        result.TokensUsed.Should().Be(150);
    }

    [Fact(DisplayName = "GenerateNarrativeAsync should return fallback on OpenAI error")]
    public async Task GenerateNarrativeAsync_ReturnsFallback_OnOpenAiError()
    {
        // Arrange
        SetupOpenAiConfig();
        _mockHttp.When("https://api.openai.com/v1/chat/completions")
            .Respond(HttpStatusCode.InternalServerError);

        var request = CreateTestNarrativeRequest();

        // Act
        var result = await _service.GenerateNarrativeAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue(); // Fallback is successful
        result.Narrativa.Should().Contain("Resumen del período");
    }

    [Fact(DisplayName = "GenerateNarrativeAsync should call Gemini API successfully")]
    public async Task GenerateNarrativeAsync_CallsGemini_Successfully()
    {
        // Arrange
        SetupGeminiConfig();
        SetupGeminiMockResponse("Gemini generated narrative.");

        var request = CreateTestNarrativeRequest();

        // Act
        var result = await _service.GenerateNarrativeAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Narrativa.Should().Be("Gemini generated narrative.");
    }

    [Fact(DisplayName = "GenerateNarrativeAsync should call DeepSeek API successfully")]
    public async Task GenerateNarrativeAsync_CallsDeepSeek_Successfully()
    {
        // Arrange
        SetupDeepSeekConfig();
        SetupDeepSeekMockResponse("DeepSeek generated narrative.");

        var request = CreateTestNarrativeRequest();

        // Act
        var result = await _service.GenerateNarrativeAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Narrativa.Should().Be("DeepSeek generated narrative.");
    }

    [Fact(DisplayName = "GenerateNarrativeAsync should call Azure OpenAI API successfully")]
    public async Task GenerateNarrativeAsync_CallsAzureOpenAi_Successfully()
    {
        // Arrange
        SetupAzureOpenAiConfig();
        SetupAzureOpenAiMockResponse("Azure OpenAI generated narrative.");

        var request = CreateTestNarrativeRequest();

        // Act
        var result = await _service.GenerateNarrativeAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Narrativa.Should().Be("Azure OpenAI generated narrative.");
    }

    [Fact(DisplayName = "GenerateNarrativeAsync should call Ollama API successfully")]
    public async Task GenerateNarrativeAsync_CallsOllama_Successfully()
    {
        // Arrange
        SetupOllamaConfig();
        SetupOllamaMockResponse("Ollama generated narrative.");

        var request = CreateTestNarrativeRequest();

        // Act
        var result = await _service.GenerateNarrativeAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Narrativa.Should().Be("Ollama generated narrative.");
    }

    [Fact(DisplayName = "GenerateNarrativeAsync should include all incidents in prompt")]
    public async Task GenerateNarrativeAsync_IncludesAllIncidents_InPrompt()
    {
        // Arrange
        SetupOpenAiConfig();

        string capturedBody = string.Empty;
        _mockHttp.When("https://api.openai.com/v1/chat/completions")
            .With(request =>
            {
                capturedBody = request.Content?.ReadAsStringAsync().Result ?? "";
                return true;
            })
            .Respond("application/json", CreateOpenAiResponse("Test response"));

        var request = CreateTestNarrativeRequest(incidentCount: 5);

        // Act
        await _service.GenerateNarrativeAsync(request);

        // Assert
        capturedBody.Should().Contain("INC-000001");
        capturedBody.Should().Contain("INC-000005");
    }

    [Fact(DisplayName = "GenerateNarrativeAsync should return fallback on exception")]
    public async Task GenerateNarrativeAsync_ReturnsFallback_OnException()
    {
        // Arrange
        SetupOpenAiConfig();
        _mockHttp.When("https://api.openai.com/v1/chat/completions")
            .Throw(new HttpRequestException("Connection refused"));

        var request = CreateTestNarrativeRequest();

        // Act
        var result = await _service.GenerateNarrativeAsync(request);

        // Assert - service returns fallback on exception instead of throwing
        result.Should().NotBeNull();
        result.Success.Should().BeTrue(); // Fallback is successful
        result.Narrativa.Should().Contain("Resumen del período");
    }

    #endregion

    #region IsAvailableAsync Tests

    [Fact(DisplayName = "IsAvailableAsync should return false when no config")]
    public async Task IsAvailableAsync_ReturnsFalse_WhenNoConfig()
    {
        // Arrange
        _configService.GetActiveConfigurationAsync(Arg.Any<CancellationToken>())
            .Returns((ConfiguracionIA?)null);

        var configData = new Dictionary<string, string?>();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var service = new AiNarrativeService(
            _httpClient,
            _configService,
            config,
            _logger);

        // Act
        var result = await service.IsAvailableAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact(DisplayName = "IsAvailableAsync should return false when API key missing for cloud provider")]
    public async Task IsAvailableAsync_ReturnsFalse_WhenApiKeyMissing()
    {
        // Arrange
        var dbConfig = new ConfiguracionIA
        {
            Proveedor = "OpenAI",
            ApiKey = "", // Missing API key
            Modelo = "gpt-4"
        };
        _configService.GetActiveConfigurationAsync(Arg.Any<CancellationToken>())
            .Returns(dbConfig);

        // Act
        var result = await _service.IsAvailableAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact(DisplayName = "IsAvailableAsync should return true for local provider without API key")]
    public async Task IsAvailableAsync_ReturnsTrue_ForLocalProviderWithoutApiKey()
    {
        // Arrange
        var dbConfig = new ConfiguracionIA
        {
            Proveedor = "Local",
            ApiKey = "", // No API key needed for local
            Modelo = "llama2",
            LocalEndpoint = "http://localhost:11434/api/generate"
        };
        _configService.GetActiveConfigurationAsync(Arg.Any<CancellationToken>())
            .Returns(dbConfig);

        _mockHttp.When(HttpMethod.Head, "http://localhost:11434/api/generate")
            .Respond(HttpStatusCode.MethodNotAllowed);

        // Act
        var result = await _service.IsAvailableAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact(DisplayName = "IsAvailableAsync should return true when endpoint responds")]
    public async Task IsAvailableAsync_ReturnsTrue_WhenEndpointResponds()
    {
        // Arrange
        SetupOpenAiConfig();
        _mockHttp.When(HttpMethod.Head, "https://api.openai.com/v1/chat/completions")
            .Respond(HttpStatusCode.OK);

        // Act
        var result = await _service.IsAvailableAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact(DisplayName = "IsAvailableAsync should return false when endpoint fails")]
    public async Task IsAvailableAsync_ReturnsFalse_WhenEndpointFails()
    {
        // Arrange
        SetupOpenAiConfig();
        _mockHttp.When(HttpMethod.Head, "https://api.openai.com/v1/chat/completions")
            .Throw(new HttpRequestException("Connection refused"));

        // Act
        var result = await _service.IsAvailableAsync();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region ProviderName Tests

    [Fact(DisplayName = "ProviderName should return configured provider")]
    public async Task ProviderName_ReturnsConfiguredProvider()
    {
        // Arrange
        var dbConfig = new ConfiguracionIA
        {
            Proveedor = "Gemini",
            ApiKey = "test-key",
            Modelo = "gemini-pro"
        };
        _configService.GetActiveConfigurationAsync(Arg.Any<CancellationToken>())
            .Returns(dbConfig);

        // Force cache refresh
        await _service.IsAvailableAsync();

        // Act
        var result = _service.ProviderName;

        // Assert
        result.Should().Be("Gemini");
    }

    [Fact(DisplayName = "ProviderName should return default when no cached config")]
    public void ProviderName_ReturnsDefault_WhenNoCachedConfig()
    {
        // Arrange - default config has OpenAI
        _configService.GetActiveConfigurationAsync(Arg.Any<CancellationToken>())
            .Returns((ConfiguracionIA?)null);

        // Act
        var result = _service.ProviderName;

        // Assert
        result.Should().Be("OpenAI");
    }

    #endregion

    #region Fallback Narrative Tests

    [Fact(DisplayName = "Fallback narrative should include statistics")]
    public async Task FallbackNarrative_IncludesStatistics()
    {
        // Arrange
        _configService.GetActiveConfigurationAsync(Arg.Any<CancellationToken>())
            .Returns((ConfiguracionIA?)null);

        var configData = new Dictionary<string, string?>
        {
            ["AI:Provider"] = "OpenAI",
            ["AI:ApiKey"] = ""
        };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var service = new AiNarrativeService(
            _httpClient,
            _configService,
            config,
            _logger);

        var request = CreateTestNarrativeRequest();
        request.Statistics.TotalReportes = 25;
        request.Statistics.DelitoMasFrecuente = "Robo";
        request.Statistics.ZonaMasActiva = "Centro";

        // Act
        var result = await service.GenerateNarrativeAsync(request);

        // Assert
        result.Narrativa.Should().Contain("25 reportes");
        result.Narrativa.Should().Contain("Robo");
        result.Narrativa.Should().Contain("Centro");
    }

    #endregion

    #region Helper Methods

    private NarrativeRequestDto CreateTestNarrativeRequest(int incidentCount = 3)
    {
        var incidents = new List<IncidentSummaryDto>();
        for (int i = 1; i <= incidentCount; i++)
        {
            incidents.Add(new IncidentSummaryDto
            {
                Id = i,
                Folio = $"INC-{i:D6}",
                Delito = "Robo",
                HechosReportados = $"Hechos del caso {i}",
                AccionesRealizadas = $"Acciones del caso {i}",
                FechaReporte = DateTime.UtcNow.AddDays(-i)
            });
        }

        return new NarrativeRequestDto
        {
            FechaInicio = DateTime.UtcNow.AddDays(-7),
            FechaFin = DateTime.UtcNow,
            Statistics = new ReportStatisticsDto
            {
                TotalReportes = incidentCount,
                ReportesEntregados = incidentCount,
                DelitoMasFrecuente = "Robo",
                ZonaMasActiva = "Centro",
                PorDelito = new Dictionary<string, int> { { "Robo", incidentCount } },
                PorZona = new Dictionary<string, int> { { "Centro", incidentCount } },
                PorSexo = new Dictionary<string, int> { { "F", incidentCount } }
            },
            Incidents = incidents
        };
    }

    private void SetupOpenAiConfig()
    {
        var dbConfig = new ConfiguracionIA
        {
            Proveedor = "OpenAI",
            ApiKey = "test-api-key",
            Modelo = "gpt-4",
            Endpoint = "https://api.openai.com/v1/chat/completions",
            MaxTokens = 1000,
            Temperature = 0.7
        };
        _configService.GetActiveConfigurationAsync(Arg.Any<CancellationToken>())
            .Returns(dbConfig);
    }

    private void SetupGeminiConfig()
    {
        var dbConfig = new ConfiguracionIA
        {
            Proveedor = "Gemini",
            ApiKey = "test-gemini-key",
            Modelo = "gemini-pro",
            MaxTokens = 8000,
            Temperature = 0.7
        };
        _configService.GetActiveConfigurationAsync(Arg.Any<CancellationToken>())
            .Returns(dbConfig);
    }

    private void SetupDeepSeekConfig()
    {
        var dbConfig = new ConfiguracionIA
        {
            Proveedor = "DeepSeek",
            ApiKey = "test-deepseek-key",
            Modelo = "deepseek-chat",
            MaxTokens = 1000,
            Temperature = 0.7
        };
        _configService.GetActiveConfigurationAsync(Arg.Any<CancellationToken>())
            .Returns(dbConfig);
    }

    private void SetupAzureOpenAiConfig()
    {
        var dbConfig = new ConfiguracionIA
        {
            Proveedor = "AzureOpenAI",
            ApiKey = "test-azure-key",
            Modelo = "gpt-4",
            AzureEndpoint = "https://test.openai.azure.com/openai/deployments/gpt-4/chat/completions",
            AzureApiVersion = "2024-02-15-preview",
            MaxTokens = 1000,
            Temperature = 0.7
        };
        _configService.GetActiveConfigurationAsync(Arg.Any<CancellationToken>())
            .Returns(dbConfig);
    }

    private void SetupOllamaConfig()
    {
        var dbConfig = new ConfiguracionIA
        {
            Proveedor = "Ollama",
            ApiKey = "",
            Modelo = "llama2",
            LocalEndpoint = "http://localhost:11434/api/generate",
            MaxTokens = 1000,
            Temperature = 0.7
        };
        _configService.GetActiveConfigurationAsync(Arg.Any<CancellationToken>())
            .Returns(dbConfig);
    }

    private void SetupOpenAiMockResponse(string narrativeText)
    {
        _mockHttp.When("https://api.openai.com/v1/chat/completions")
            .Respond("application/json", CreateOpenAiResponse(narrativeText));
    }

    private void SetupGeminiMockResponse(string narrativeText)
    {
        _mockHttp.When("https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent")
            .Respond("application/json", CreateGeminiResponse(narrativeText));
    }

    private void SetupDeepSeekMockResponse(string narrativeText)
    {
        _mockHttp.When("https://api.deepseek.com/chat/completions")
            .Respond("application/json", CreateOpenAiResponse(narrativeText)); // DeepSeek uses OpenAI format
    }

    private void SetupAzureOpenAiMockResponse(string narrativeText)
    {
        _mockHttp.When("https://test.openai.azure.com/openai/deployments/gpt-4/chat/completions*")
            .Respond("application/json", CreateOpenAiResponse(narrativeText));
    }

    private void SetupOllamaMockResponse(string narrativeText)
    {
        _mockHttp.When("http://localhost:11434/api/generate")
            .Respond("application/json", CreateOllamaResponse(narrativeText));
    }

    private static string CreateOpenAiResponse(string narrativeText)
    {
        return JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new
                {
                    message = new { content = narrativeText }
                }
            },
            usage = new { total_tokens = 150 }
        });
    }

    private static string CreateGeminiResponse(string narrativeText)
    {
        return JsonSerializer.Serialize(new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new { text = narrativeText }
                        }
                    },
                    finishReason = "STOP"
                }
            },
            usageMetadata = new { totalTokenCount = 150 }
        });
    }

    private static string CreateOllamaResponse(string narrativeText)
    {
        return JsonSerializer.Serialize(new
        {
            response = narrativeText
        });
    }

    #endregion
}
