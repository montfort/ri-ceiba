using Ceiba.Core.Entities;
using Ceiba.Core.Interfaces;
using Ceiba.Infrastructure.Services;
using Ceiba.Shared.DTOs;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using Xunit;

namespace Ceiba.Application.Tests.Services;

/// <summary>
/// Unit tests for AiNarrativeService (US4: T082)
/// Tests AI narrative generation with multiple providers.
/// </summary>
public class AiNarrativeServiceTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpHandler;
    private readonly Mock<IAiConfigurationService> _mockConfigService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<AiNarrativeService>> _mockLogger;
    private readonly HttpClient _httpClient;

    public AiNarrativeServiceTests()
    {
        _mockHttpHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpHandler.Object);
        _mockConfigService = new Mock<IAiConfigurationService>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<AiNarrativeService>>();

        // Setup default configuration
        _mockConfiguration.Setup(c => c["AI:Provider"]).Returns("OpenAI");
        _mockConfiguration.Setup(c => c["AI:ApiKey"]).Returns((string?)null);
        _mockConfiguration.Setup(c => c["AI:Model"]).Returns("gpt-4");
    }

    private AiNarrativeService CreateService()
    {
        return new AiNarrativeService(
            _httpClient,
            _mockConfigService.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);
    }

    private NarrativeRequestDto CreateTestRequest()
    {
        return new NarrativeRequestDto
        {
            FechaInicio = DateTime.UtcNow.AddDays(-7),
            FechaFin = DateTime.UtcNow,
            Statistics = new ReportStatisticsDto
            {
                TotalReportes = 15,
                ReportesEntregados = 12,
                ReportesBorrador = 3,
                DelitoMasFrecuente = "Violencia familiar",
                ZonaMasActiva = "Zona Centro",
                PorSexo = new Dictionary<string, int> { { "Femenino", 10 }, { "Masculino", 5 } },
                PorDelito = new Dictionary<string, int>
                {
                    { "Violencia familiar", 8 },
                    { "Robo", 4 },
                    { "Acoso", 3 }
                }
            },
            Incidents = new List<IncidentSummaryDto>
            {
                new()
                {
                    Id = 1,
                    Folio = "INC-2024-001",
                    Delito = "Violencia familiar",
                    HechosReportados = "Víctima reporta agresión física por parte de su pareja.",
                    AccionesRealizadas = "Se brindó apoyo psicológico y se canalizó a refugio.",
                    FechaReporte = DateTime.UtcNow.AddDays(-5)
                },
                new()
                {
                    Id = 2,
                    Folio = "INC-2024-002",
                    Delito = "Robo",
                    HechosReportados = "Robo de pertenencias en transporte público.",
                    AccionesRealizadas = "Se levantó denuncia ante MP.",
                    FechaReporte = DateTime.UtcNow.AddDays(-3)
                }
            }
        };
    }

    #region Fallback Narrative Tests

    [Fact(DisplayName = "T082: GenerateNarrativeAsync should return fallback when API key not configured")]
    public async Task GenerateNarrativeAsync_NoApiKey_ReturnsFallback()
    {
        // Arrange
        _mockConfigService.Setup(x => x.GetActiveConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConfiguracionIA?)null);
        _mockConfiguration.Setup(c => c["AI:ApiKey"]).Returns((string?)null);

        var service = CreateService();
        var request = CreateTestRequest();

        // Act
        var result = await service.GenerateNarrativeAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Narrativa.Should().Contain("15 reportes de incidencias");
        result.Narrativa.Should().Contain("Violencia familiar");
        result.Error.Should().Contain("fallback");
    }

    [Fact(DisplayName = "T082: Fallback narrative should include statistics")]
    public async Task GenerateNarrativeAsync_Fallback_IncludesStatistics()
    {
        // Arrange
        _mockConfigService.Setup(x => x.GetActiveConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConfiguracionIA?)null);

        var service = CreateService();
        var request = CreateTestRequest();

        // Act
        var result = await service.GenerateNarrativeAsync(request);

        // Assert
        result.Narrativa.Should().Contain(request.Statistics.DelitoMasFrecuente);
        result.Narrativa.Should().Contain(request.Statistics.ZonaMasActiva);
    }

    #endregion

    #region OpenAI Provider Tests

    [Fact(DisplayName = "T082: GenerateNarrativeAsync with OpenAI should call correct endpoint")]
    public async Task GenerateNarrativeAsync_OpenAI_CallsCorrectEndpoint()
    {
        // Arrange
        var config = new ConfiguracionIA
        {
            Proveedor = "OpenAI",
            ApiKey = "test-api-key",
            Modelo = "gpt-4",
            Endpoint = "https://api.openai.com/v1/chat/completions",
            MaxTokens = 1000,
            Temperature = 0.7
        };

        _mockConfigService.Setup(x => x.GetActiveConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var openAiResponse = new
        {
            choices = new[]
            {
                new
                {
                    message = new { content = "Resumen ejecutivo generado por IA..." }
                }
            },
            usage = new { total_tokens = 500 }
        };

        SetupHttpResponse(HttpStatusCode.OK, JsonSerializer.Serialize(openAiResponse));

        var service = CreateService();
        var request = CreateTestRequest();

        // Act
        var result = await service.GenerateNarrativeAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Narrativa.Should().Contain("Resumen ejecutivo generado por IA");
        result.TokensUsed.Should().Be(500);

        // Verify HTTP call was made
        _mockHttpHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.RequestUri!.ToString().Contains("openai.com")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact(DisplayName = "T082: GenerateNarrativeAsync with OpenAI error should return fallback")]
    public async Task GenerateNarrativeAsync_OpenAIError_ReturnsFallback()
    {
        // Arrange
        var config = new ConfiguracionIA
        {
            Proveedor = "OpenAI",
            ApiKey = "test-api-key",
            Modelo = "gpt-4",
            MaxTokens = 1000
        };

        _mockConfigService.Setup(x => x.GetActiveConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        SetupHttpResponse(HttpStatusCode.TooManyRequests, "Rate limit exceeded");

        var service = CreateService();
        var request = CreateTestRequest();

        // Act
        var result = await service.GenerateNarrativeAsync(request);

        // Assert
        result.Success.Should().BeTrue(); // Fallback is still successful
        result.Narrativa.Should().NotBeEmpty();
    }

    #endregion

    #region DeepSeek Provider Tests

    [Fact(DisplayName = "T082: GenerateNarrativeAsync with DeepSeek should use correct endpoint")]
    public async Task GenerateNarrativeAsync_DeepSeek_UsesCorrectEndpoint()
    {
        // Arrange
        var config = new ConfiguracionIA
        {
            Proveedor = "DeepSeek",
            ApiKey = "test-deepseek-key",
            Modelo = "deepseek-chat",
            MaxTokens = 2000,
            Temperature = 0.7
        };

        _mockConfigService.Setup(x => x.GetActiveConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var deepSeekResponse = new
        {
            choices = new[]
            {
                new
                {
                    message = new { content = "Análisis generado por DeepSeek..." }
                }
            },
            usage = new { total_tokens = 750 }
        };

        SetupHttpResponse(HttpStatusCode.OK, JsonSerializer.Serialize(deepSeekResponse));

        var service = CreateService();
        var request = CreateTestRequest();

        // Act
        var result = await service.GenerateNarrativeAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Narrativa.Should().Contain("Análisis generado por DeepSeek");

        _mockHttpHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.RequestUri!.ToString().Contains("deepseek.com")),
            ItExpr.IsAny<CancellationToken>());
    }

    #endregion

    #region Gemini Provider Tests

    [Fact(DisplayName = "T082: GenerateNarrativeAsync with Gemini should use correct API format")]
    public async Task GenerateNarrativeAsync_Gemini_UsesCorrectFormat()
    {
        // Arrange
        var config = new ConfiguracionIA
        {
            Proveedor = "Gemini",
            ApiKey = "test-gemini-key",
            Modelo = "gemini-pro",
            MaxTokens = 8000,
            Temperature = 0.7
        };

        _mockConfigService.Setup(x => x.GetActiveConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var geminiResponse = new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new { text = "Análisis narrativo generado por Gemini..." }
                        }
                    },
                    finishReason = "STOP"
                }
            },
            usageMetadata = new { totalTokenCount = 600 }
        };

        SetupHttpResponse(HttpStatusCode.OK, JsonSerializer.Serialize(geminiResponse));

        var service = CreateService();
        var request = CreateTestRequest();

        // Act
        var result = await service.GenerateNarrativeAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Narrativa.Should().Contain("Análisis narrativo generado por Gemini");
        result.TokensUsed.Should().Be(600);

        _mockHttpHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.RequestUri!.ToString().Contains("generativelanguage.googleapis.com")),
            ItExpr.IsAny<CancellationToken>());
    }

    #endregion

    #region Local LLM/Ollama Tests

    [Fact(DisplayName = "T082: GenerateNarrativeAsync with Ollama should use correct endpoint")]
    public async Task GenerateNarrativeAsync_Ollama_UsesCorrectEndpoint()
    {
        // Arrange
        var config = new ConfiguracionIA
        {
            Proveedor = "Ollama",
            ApiKey = "", // No API key needed for local
            Modelo = "llama2",
            LocalEndpoint = "http://localhost:11434/api/generate",
            MaxTokens = 1000
        };

        _mockConfigService.Setup(x => x.GetActiveConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var ollamaResponse = new
        {
            response = "Reporte generado localmente con Ollama..."
        };

        SetupHttpResponse(HttpStatusCode.OK, JsonSerializer.Serialize(ollamaResponse));

        var service = CreateService();
        var request = CreateTestRequest();

        // Act
        var result = await service.GenerateNarrativeAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Narrativa.Should().Contain("Reporte generado localmente con Ollama");
        result.TokensUsed.Should().Be(0); // Ollama doesn't report token usage

        _mockHttpHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.RequestUri!.ToString().Contains("localhost:11434")),
            ItExpr.IsAny<CancellationToken>());
    }

    #endregion

    #region IsAvailableAsync Tests

    [Fact(DisplayName = "T082: IsAvailableAsync should return false when no config")]
    public async Task IsAvailableAsync_NoConfig_ReturnsFalse()
    {
        // Arrange
        _mockConfigService.Setup(x => x.GetActiveConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConfiguracionIA?)null);
        _mockConfiguration.Setup(c => c["AI:ApiKey"]).Returns((string?)null);

        var service = CreateService();

        // Act
        var result = await service.IsAvailableAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact(DisplayName = "T082: IsAvailableAsync should return false when API key missing for cloud providers")]
    public async Task IsAvailableAsync_NoApiKeyForCloudProvider_ReturnsFalse()
    {
        // Arrange
        var config = new ConfiguracionIA
        {
            Proveedor = "OpenAI",
            ApiKey = "", // Empty API key
            Modelo = "gpt-4"
        };

        _mockConfigService.Setup(x => x.GetActiveConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var service = CreateService();

        // Act
        var result = await service.IsAvailableAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact(DisplayName = "T082: IsAvailableAsync should return true for local LLM without API key")]
    public async Task IsAvailableAsync_LocalLlmNoApiKey_ReturnsTrue()
    {
        // Arrange
        var config = new ConfiguracionIA
        {
            Proveedor = "Local",
            ApiKey = "",
            Modelo = "llama2",
            LocalEndpoint = "http://localhost:11434/api/generate"
        };

        _mockConfigService.Setup(x => x.GetActiveConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        // Setup successful HEAD request
        SetupHttpResponse(HttpStatusCode.OK, "");

        var service = CreateService();

        // Act
        var result = await service.IsAvailableAsync();

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region ProviderName Tests

    [Fact(DisplayName = "T082: ProviderName should return configured provider")]
    public async Task ProviderName_WithConfig_ReturnsConfiguredProvider()
    {
        // Arrange
        var config = new ConfiguracionIA
        {
            Proveedor = "Gemini",
            ApiKey = "test-key"
        };

        _mockConfigService.Setup(x => x.GetActiveConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var service = CreateService();

        // Trigger config load
        await service.IsAvailableAsync();

        // Act
        var providerName = service.ProviderName;

        // Assert
        providerName.Should().Be("Gemini");
    }

    [Fact(DisplayName = "T082: ProviderName should fallback to config when no DB config")]
    public void ProviderName_NoDbConfig_ReturnsFallbackProvider()
    {
        // Arrange
        _mockConfigService.Setup(x => x.GetActiveConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConfiguracionIA?)null);
        _mockConfiguration.Setup(c => c["AI:Provider"]).Returns("AzureOpenAI");

        var service = CreateService();

        // Act
        var providerName = service.ProviderName;

        // Assert
        providerName.Should().Be("AzureOpenAI");
    }

    #endregion

    #region Configuration Caching Tests

    [Fact(DisplayName = "T082: Configuration should be cached for 5 minutes")]
    public async Task GetConfiguration_ShouldBeCached()
    {
        // Arrange
        var config = new ConfiguracionIA
        {
            Proveedor = "OpenAI",
            ApiKey = "test-key",
            Modelo = "gpt-4"
        };

        _mockConfigService.Setup(x => x.GetActiveConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var service = CreateService();

        // Act - Call multiple times
        await service.IsAvailableAsync();
        await service.IsAvailableAsync();
        await service.IsAvailableAsync();

        // Assert - Config service should only be called once due to caching
        _mockConfigService.Verify(
            x => x.GetActiveConfigurationAsync(It.IsAny<CancellationToken>()),
            Times.Once());
    }

    #endregion

    #region Helper Methods

    private void SetupHttpResponse(HttpStatusCode statusCode, string content)
    {
        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content)
            });
    }

    #endregion
}
