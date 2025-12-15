using System.Net;
using Ceiba.Core.Entities;
using Ceiba.Core.Interfaces;
using Ceiba.Infrastructure.Data;
using Ceiba.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace Ceiba.Infrastructure.Tests.Services;

/// <summary>
/// Unit tests for AiConfigurationService.
/// Tests AI provider configuration management and connection testing.
/// </summary>
public class AiConfigurationServiceTests : IDisposable
{
    private readonly CeibaDbContext _context;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<ILogger<AiConfigurationService>> _loggerMock;
    private readonly Guid _testUserId = Guid.NewGuid();

    public AiConfigurationServiceTests()
    {
        var options = new DbContextOptionsBuilder<CeibaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CeibaDbContext(options);
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _auditServiceMock = new Mock<IAuditService>();
        _loggerMock = new Mock<ILogger<AiConfigurationService>>();

        // Default HTTP client setup - returns empty client that can have Timeout set
        _httpClientFactoryMock
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient());
    }

    private AiConfigurationService CreateService()
    {
        return new AiConfigurationService(
            _context,
            _httpClientFactoryMock.Object,
            _auditServiceMock.Object,
            _loggerMock.Object);
    }

    private void SetupMockHttpClient(HttpStatusCode statusCode, string content = "{}")
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
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

        var httpClient = new HttpClient(mockHandler.Object);
        _httpClientFactoryMock
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region GetActiveConfigurationAsync Tests

    [Fact]
    public async Task GetActiveConfigurationAsync_NoConfiguration_ReturnsNull()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetActiveConfigurationAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetActiveConfigurationAsync_ActiveConfigExists_ReturnsConfig()
    {
        // Arrange
        var config = new ConfiguracionIA
        {
            Proveedor = "OpenAI",
            Modelo = "gpt-4",
            ApiKey = "test-api-key",
            Activo = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.ConfiguracionesIA.Add(config);
        await _context.SaveChangesAsync();

        var service = CreateService();

        // Act
        var result = await service.GetActiveConfigurationAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("OpenAI", result.Proveedor);
        Assert.Equal("gpt-4", result.Modelo);
    }

    [Fact]
    public async Task GetActiveConfigurationAsync_InactiveConfigOnly_ReturnsNull()
    {
        // Arrange
        var config = new ConfiguracionIA
        {
            Proveedor = "OpenAI",
            Modelo = "gpt-4",
            Activo = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.ConfiguracionesIA.Add(config);
        await _context.SaveChangesAsync();

        var service = CreateService();

        // Act
        var result = await service.GetActiveConfigurationAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetActiveConfigurationAsync_MultipleActiveConfigs_ReturnsMostRecent()
    {
        // Arrange
        var oldConfig = new ConfiguracionIA
        {
            Proveedor = "OpenAI",
            Modelo = "gpt-3.5",
            Activo = true,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        var newConfig = new ConfiguracionIA
        {
            Proveedor = "Gemini",
            Modelo = "gemini-pro",
            Activo = true,
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            UpdatedAt = DateTime.UtcNow
        };
        _context.ConfiguracionesIA.AddRange(oldConfig, newConfig);
        await _context.SaveChangesAsync();

        var service = CreateService();

        // Act
        var result = await service.GetActiveConfigurationAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Gemini", result.Proveedor);
    }

    #endregion

    #region SaveConfigurationAsync Tests

    [Fact]
    public async Task SaveConfigurationAsync_NewConfiguration_CreatesAndActivates()
    {
        // Arrange
        var service = CreateService();
        var config = new ConfiguracionIA
        {
            Proveedor = "OpenAI",
            Modelo = "gpt-4",
            ApiKey = "test-key"
        };

        // Act
        var result = await service.SaveConfigurationAsync(config, _testUserId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Activo);
        Assert.Equal(_testUserId, result.ModificadoPorId);
        Assert.NotEqual(0, result.Id);
    }

    [Fact]
    public async Task SaveConfigurationAsync_DeactivatesOtherConfigurations()
    {
        // Arrange
        var existingConfig = new ConfiguracionIA
        {
            Proveedor = "OpenAI",
            Modelo = "gpt-3.5",
            Activo = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _context.ConfiguracionesIA.Add(existingConfig);
        await _context.SaveChangesAsync();

        var service = CreateService();
        var newConfig = new ConfiguracionIA
        {
            Proveedor = "Gemini",
            Modelo = "gemini-pro",
            ApiKey = "new-key"
        };

        // Act
        await service.SaveConfigurationAsync(newConfig, _testUserId);

        // Assert
        var allConfigs = await _context.ConfiguracionesIA.ToListAsync();
        Assert.Equal(2, allConfigs.Count);
        Assert.Single(allConfigs, c => c.Activo);
        Assert.Equal("Gemini", allConfigs.First(c => c.Activo).Proveedor);
    }

    [Fact]
    public async Task SaveConfigurationAsync_InvalidConfiguration_ThrowsException()
    {
        // Arrange
        var service = CreateService();
        var config = new ConfiguracionIA
        {
            Proveedor = "", // Invalid - empty provider
            Modelo = "gpt-4"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SaveConfigurationAsync(config, _testUserId));
    }

    [Fact]
    public async Task SaveConfigurationAsync_CallsAuditService()
    {
        // Arrange
        var service = CreateService();
        var config = new ConfiguracionIA
        {
            Proveedor = "OpenAI",
            Modelo = "gpt-4",
            ApiKey = "test-key"
        };

        // Act
        await service.SaveConfigurationAsync(config, _testUserId);

        // Assert
        _auditServiceMock.Verify(
            x => x.LogAsync(
                "AI_CONFIG_UPDATE",
                It.IsAny<int>(),
                "ConfiguracionIA",
                It.IsAny<string>(),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SaveConfigurationAsync_UpdateExisting_UpdatesCorrectly()
    {
        // Arrange
        var existingConfig = new ConfiguracionIA
        {
            Proveedor = "OpenAI",
            Modelo = "gpt-3.5",
            ApiKey = "old-key",
            Activo = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _context.ConfiguracionesIA.Add(existingConfig);
        await _context.SaveChangesAsync();
        _context.Entry(existingConfig).State = EntityState.Detached;

        var service = CreateService();
        var updatedConfig = new ConfiguracionIA
        {
            Id = existingConfig.Id,
            Proveedor = "OpenAI",
            Modelo = "gpt-4",
            ApiKey = "new-key"
        };

        // Act
        var result = await service.SaveConfigurationAsync(updatedConfig, _testUserId);

        // Assert
        Assert.Equal(existingConfig.Id, result.Id);
        Assert.Equal("gpt-4", result.Modelo);
    }

    #endregion

    #region TestConnectionAsync Tests

    [Fact]
    public async Task TestConnectionAsync_OpenAI_NoApiKey_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var config = new ConfiguracionIA
        {
            Proveedor = "OpenAI",
            Modelo = "gpt-4",
            ApiKey = null
        };

        // Act
        var (success, message) = await service.TestConnectionAsync(config);

        // Assert
        Assert.False(success);
        Assert.Contains("API Key", message);
    }

    [Fact]
    public async Task TestConnectionAsync_OpenAI_Success_ReturnsSuccess()
    {
        // Arrange
        SetupMockHttpClient(HttpStatusCode.OK, @"{""choices"":[]}");
        var service = CreateService();
        var config = new ConfiguracionIA
        {
            Proveedor = "OpenAI",
            Modelo = "gpt-4",
            ApiKey = "test-key"
        };

        // Act
        var (success, message) = await service.TestConnectionAsync(config);

        // Assert
        Assert.True(success);
        Assert.Contains("OpenAI", message);
    }

    [Fact]
    public async Task TestConnectionAsync_OpenAI_Failure_ReturnsError()
    {
        // Arrange
        SetupMockHttpClient(HttpStatusCode.Unauthorized, @"{""error"":{""message"":""Invalid API key""}}");
        var service = CreateService();
        var config = new ConfiguracionIA
        {
            Proveedor = "OpenAI",
            Modelo = "gpt-4",
            ApiKey = "invalid-key"
        };

        // Act
        var (success, message) = await service.TestConnectionAsync(config);

        // Assert
        Assert.False(success);
        Assert.Contains("Invalid API key", message);
    }

    [Fact]
    public async Task TestConnectionAsync_Gemini_NoApiKey_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var config = new ConfiguracionIA
        {
            Proveedor = "Gemini",
            Modelo = "gemini-pro",
            ApiKey = ""
        };

        // Act
        var (success, message) = await service.TestConnectionAsync(config);

        // Assert
        Assert.False(success);
        Assert.Contains("API Key", message);
    }

    [Fact]
    public async Task TestConnectionAsync_Gemini_Success_ReturnsSuccess()
    {
        // Arrange
        SetupMockHttpClient(HttpStatusCode.OK, @"{""candidates"":[]}");
        var service = CreateService();
        var config = new ConfiguracionIA
        {
            Proveedor = "Gemini",
            Modelo = "gemini-pro",
            ApiKey = "test-key"
        };

        // Act
        var (success, message) = await service.TestConnectionAsync(config);

        // Assert
        Assert.True(success);
        Assert.Contains("Gemini", message);
    }

    [Fact]
    public async Task TestConnectionAsync_DeepSeek_Success_ReturnsSuccess()
    {
        // Arrange
        SetupMockHttpClient(HttpStatusCode.OK, @"{""choices"":[]}");
        var service = CreateService();
        var config = new ConfiguracionIA
        {
            Proveedor = "DeepSeek",
            Modelo = "deepseek-chat",
            ApiKey = "test-key"
        };

        // Act
        var (success, message) = await service.TestConnectionAsync(config);

        // Assert
        Assert.True(success);
        Assert.Contains("DeepSeek", message);
    }

    [Fact]
    public async Task TestConnectionAsync_AzureOpenAI_NoEndpoint_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var config = new ConfiguracionIA
        {
            Proveedor = "AzureOpenAI",
            Modelo = "gpt-4",
            ApiKey = "test-key",
            AzureEndpoint = null
        };

        // Act
        var (success, message) = await service.TestConnectionAsync(config);

        // Assert
        Assert.False(success);
        Assert.Contains("Endpoint", message);
    }

    [Fact]
    public async Task TestConnectionAsync_AzureOpenAI_Success_ReturnsSuccess()
    {
        // Arrange
        SetupMockHttpClient(HttpStatusCode.OK, @"{""choices"":[]}");
        var service = CreateService();
        var config = new ConfiguracionIA
        {
            Proveedor = "AzureOpenAI",
            Modelo = "gpt-4",
            ApiKey = "test-key",
            AzureEndpoint = "https://myresource.openai.azure.com/openai/deployments/gpt-4/chat/completions"
        };

        // Act
        var (success, message) = await service.TestConnectionAsync(config);

        // Assert
        Assert.True(success);
        Assert.Contains("Azure", message);
    }

    [Fact]
    public async Task TestConnectionAsync_LocalOllama_Success_ReturnsSuccess()
    {
        // Arrange
        SetupMockHttpClient(HttpStatusCode.OK, @"{""response"":""OK""}");
        var service = CreateService();
        var config = new ConfiguracionIA
        {
            Proveedor = "Ollama",
            Modelo = "llama2",
            LocalEndpoint = "http://localhost:11434/api/generate"
        };

        // Act
        var (success, message) = await service.TestConnectionAsync(config);

        // Assert
        Assert.True(success);
        Assert.Contains("Ollama", message);
    }

    [Fact]
    public async Task TestConnectionAsync_UnsupportedProvider_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var config = new ConfiguracionIA
        {
            Proveedor = "UnknownProvider",
            Modelo = "some-model"
        };

        // Act
        var (success, message) = await service.TestConnectionAsync(config);

        // Assert
        Assert.False(success);
        Assert.Contains("no soportado", message);
    }

    [Fact]
    public async Task TestConnectionAsync_HttpException_ReturnsConnectionError()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var httpClient = new HttpClient(mockHandler.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = CreateService();
        var config = new ConfiguracionIA
        {
            Proveedor = "OpenAI",
            Modelo = "gpt-4",
            ApiKey = "test-key"
        };

        // Act
        var (success, message) = await service.TestConnectionAsync(config);

        // Assert
        Assert.False(success);
        Assert.Contains("conexión", message);
    }

    #endregion

    #region GetConfigurationHistoryAsync Tests

    [Fact]
    public async Task GetConfigurationHistoryAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetConfigurationHistoryAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetConfigurationHistoryAsync_ReturnsOrderedByMostRecent()
    {
        // Arrange
        var configs = new[]
        {
            new ConfiguracionIA { Proveedor = "OpenAI", Modelo = "old", CreatedAt = DateTime.UtcNow.AddDays(-3) },
            new ConfiguracionIA { Proveedor = "Gemini", Modelo = "mid", CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new ConfiguracionIA { Proveedor = "DeepSeek", Modelo = "new", CreatedAt = DateTime.UtcNow.AddDays(-1) }
        };
        _context.ConfiguracionesIA.AddRange(configs);
        await _context.SaveChangesAsync();

        var service = CreateService();

        // Act
        var result = await service.GetConfigurationHistoryAsync();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("DeepSeek", result[0].Proveedor);
        Assert.Equal("Gemini", result[1].Proveedor);
        Assert.Equal("OpenAI", result[2].Proveedor);
    }

    [Fact]
    public async Task GetConfigurationHistoryAsync_RespectsTakeLimit()
    {
        // Arrange
        for (int i = 0; i < 15; i++)
        {
            _context.ConfiguracionesIA.Add(new ConfiguracionIA
            {
                Proveedor = $"Provider{i}",
                Modelo = "model",
                CreatedAt = DateTime.UtcNow.AddDays(-i)
            });
        }
        await _context.SaveChangesAsync();

        var service = CreateService();

        // Act
        var result = await service.GetConfigurationHistoryAsync(take: 5);

        // Assert
        Assert.Equal(5, result.Count);
    }

    #endregion

    #region Additional Coverage Tests

    [Fact]
    public async Task TestConnectionAsync_Timeout_ReturnsTimeoutMessage()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("The request was cancelled"));

        var httpClient = new HttpClient(mockHandler.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = CreateService();
        var config = new ConfiguracionIA
        {
            Proveedor = "OpenAI",
            Modelo = "gpt-4",
            ApiKey = "test-key"
        };

        // Act
        var (success, message) = await service.TestConnectionAsync(config);

        // Assert
        Assert.False(success);
        Assert.Contains("timeout", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TestConnectionAsync_GenericException_ReturnsUnexpectedError()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Unexpected error"));

        var httpClient = new HttpClient(mockHandler.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = CreateService();
        var config = new ConfiguracionIA
        {
            Proveedor = "OpenAI",
            Modelo = "gpt-4",
            ApiKey = "test-key"
        };

        // Act
        var (success, message) = await service.TestConnectionAsync(config);

        // Assert
        Assert.False(success);
        Assert.Contains("inesperado", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TestConnectionAsync_AzureOpenAI_NoApiKey_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var config = new ConfiguracionIA
        {
            Proveedor = "AzureOpenAI",
            Modelo = "gpt-4",
            ApiKey = "",
            AzureEndpoint = "https://myresource.openai.azure.com"
        };

        // Act
        var (success, message) = await service.TestConnectionAsync(config);

        // Assert
        Assert.False(success);
        Assert.Contains("API Key", message);
    }

    [Fact]
    public async Task TestConnectionAsync_AzureOpenAI_Failure_ReturnsError()
    {
        // Arrange
        SetupMockHttpClient(HttpStatusCode.Forbidden, @"{""error"":{""message"":""Access denied""}}");
        var service = CreateService();
        var config = new ConfiguracionIA
        {
            Proveedor = "AzureOpenAI",
            Modelo = "gpt-4",
            ApiKey = "test-key",
            AzureEndpoint = "https://myresource.openai.azure.com/openai/deployments/gpt-4/chat/completions"
        };

        // Act
        var (success, message) = await service.TestConnectionAsync(config);

        // Assert
        Assert.False(success);
        Assert.Contains("Azure", message);
    }

    [Fact]
    public async Task TestConnectionAsync_DeepSeek_NoApiKey_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var config = new ConfiguracionIA
        {
            Proveedor = "DeepSeek",
            Modelo = "deepseek-chat",
            ApiKey = null
        };

        // Act
        var (success, message) = await service.TestConnectionAsync(config);

        // Assert
        Assert.False(success);
        Assert.Contains("API Key", message);
    }

    [Fact]
    public async Task TestConnectionAsync_DeepSeek_Failure_ReturnsError()
    {
        // Arrange
        SetupMockHttpClient(HttpStatusCode.BadRequest, @"{""error"":{""message"":""Invalid model""}}");
        var service = CreateService();
        var config = new ConfiguracionIA
        {
            Proveedor = "DeepSeek",
            Modelo = "invalid-model",
            ApiKey = "test-key"
        };

        // Act
        var (success, message) = await service.TestConnectionAsync(config);

        // Assert
        Assert.False(success);
        Assert.Contains("DeepSeek", message);
    }

    [Fact]
    public async Task TestConnectionAsync_Gemini_Failure_ReturnsError()
    {
        // Arrange
        SetupMockHttpClient(HttpStatusCode.BadRequest, @"{""error"":{""message"":""Invalid API key"",""status"":""INVALID_ARGUMENT""}}");
        var service = CreateService();
        var config = new ConfiguracionIA
        {
            Proveedor = "Gemini",
            Modelo = "gemini-pro",
            ApiKey = "invalid-key"
        };

        // Act
        var (success, message) = await service.TestConnectionAsync(config);

        // Assert
        Assert.False(success);
        Assert.Contains("Gemini", message);
    }

    [Fact]
    public async Task TestConnectionAsync_Gemini_ErrorWithStatusOnly_ReturnsStatus()
    {
        // Arrange
        SetupMockHttpClient(HttpStatusCode.BadRequest, @"{""error"":{""status"":""PERMISSION_DENIED""}}");
        var service = CreateService();
        var config = new ConfiguracionIA
        {
            Proveedor = "Gemini",
            Modelo = "gemini-pro",
            ApiKey = "test-key"
        };

        // Act
        var (success, message) = await service.TestConnectionAsync(config);

        // Assert
        Assert.False(success);
        Assert.Contains("PERMISSION_DENIED", message);
    }

    [Fact]
    public async Task TestConnectionAsync_LocalLlm_NonOllama_Success()
    {
        // Arrange
        SetupMockHttpClient(HttpStatusCode.OK, @"{""choices"":[]}");
        var service = CreateService();
        var config = new ConfiguracionIA
        {
            Proveedor = "Local",
            Modelo = "llama2",
            LocalEndpoint = "http://localhost:8080/v1/chat/completions" // Non-Ollama endpoint
        };

        // Act
        var (success, message) = await service.TestConnectionAsync(config);

        // Assert
        Assert.True(success);
        Assert.Contains("LLM local", message);
    }

    [Fact]
    public async Task TestConnectionAsync_LocalLlm_NonOllama_Failure()
    {
        // Arrange
        SetupMockHttpClient(HttpStatusCode.ServiceUnavailable, @"Service unavailable");
        var service = CreateService();
        var config = new ConfiguracionIA
        {
            Proveedor = "Local",
            Modelo = "llama2",
            LocalEndpoint = "http://localhost:8080/v1/chat/completions"
        };

        // Act
        var (success, message) = await service.TestConnectionAsync(config);

        // Assert
        Assert.False(success);
        Assert.Contains("LLM local", message);
    }

    [Fact]
    public async Task TestConnectionAsync_Ollama_Failure_ReturnsError()
    {
        // Arrange
        SetupMockHttpClient(HttpStatusCode.NotFound, @"model not found");
        var service = CreateService();
        var config = new ConfiguracionIA
        {
            Proveedor = "Ollama",
            Modelo = "nonexistent-model",
            LocalEndpoint = "http://localhost:11434/api/generate"
        };

        // Act
        var (success, message) = await service.TestConnectionAsync(config);

        // Assert
        Assert.False(success);
        Assert.Contains("Ollama", message);
    }

    [Fact]
    public async Task TestConnectionAsync_OpenAI_CustomEndpoint_UsesIt()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(@"{""choices"":[]}")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = CreateService();
        var config = new ConfiguracionIA
        {
            Proveedor = "OpenAI",
            Modelo = "gpt-4",
            ApiKey = "test-key",
            Endpoint = "https://custom.openai-api.com/v1/chat/completions"
        };

        // Act
        await service.TestConnectionAsync(config);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal("https://custom.openai-api.com/v1/chat/completions", capturedRequest!.RequestUri?.ToString());
    }

    [Fact]
    public async Task SaveConfigurationAsync_UpdateTrackedEntity_UpdatesCorrectly()
    {
        // Arrange
        var existingConfig = new ConfiguracionIA
        {
            Proveedor = "OpenAI",
            Modelo = "gpt-3.5",
            ApiKey = "old-key",
            Activo = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _context.ConfiguracionesIA.Add(existingConfig);
        await _context.SaveChangesAsync();
        // Don't detach - keep it tracked

        var service = CreateService();

        // Modify the tracked entity directly
        existingConfig.Modelo = "gpt-4-turbo";
        existingConfig.ApiKey = "new-key";

        // Act
        var result = await service.SaveConfigurationAsync(existingConfig, _testUserId);

        // Assert
        Assert.Equal(existingConfig.Id, result.Id);
        Assert.Equal("gpt-4-turbo", result.Modelo);
        Assert.True(result.Activo);
    }

    [Fact]
    public async Task TestConnectionAsync_ErrorResponseWithInvalidJson_ReturnsRawResponse()
    {
        // Arrange
        SetupMockHttpClient(HttpStatusCode.InternalServerError, "Not a JSON response");
        var service = CreateService();
        var config = new ConfiguracionIA
        {
            Proveedor = "OpenAI",
            Modelo = "gpt-4",
            ApiKey = "test-key"
        };

        // Act
        var (success, message) = await service.TestConnectionAsync(config);

        // Assert
        Assert.False(success);
        Assert.Contains("Not a JSON response", message);
    }

    [Fact]
    public async Task TestConnectionAsync_ErrorWithNestedErrorObject_ExtractsMessage()
    {
        // Arrange
        SetupMockHttpClient(HttpStatusCode.BadRequest, @"{""error"":{""code"":""invalid_model"",""message"":""The model is invalid""}}");
        var service = CreateService();
        var config = new ConfiguracionIA
        {
            Proveedor = "OpenAI",
            Modelo = "invalid",
            ApiKey = "test-key"
        };

        // Act
        var (success, message) = await service.TestConnectionAsync(config);

        // Assert
        Assert.False(success);
        Assert.Contains("The model is invalid", message);
    }

    [Fact]
    public async Task TestConnectionAsync_ErrorObjectWithoutMessage_ReturnsErrorString()
    {
        // Arrange
        SetupMockHttpClient(HttpStatusCode.BadRequest, @"{""error"":""Simple error string""}");
        var service = CreateService();
        var config = new ConfiguracionIA
        {
            Proveedor = "OpenAI",
            Modelo = "gpt-4",
            ApiKey = "test-key"
        };

        // Act
        var (success, message) = await service.TestConnectionAsync(config);

        // Assert
        Assert.False(success);
        Assert.Contains("Simple error string", message);
    }

    [Fact]
    public async Task TestConnectionAsync_AzureOpenAI_UsesDefaultApiVersion()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(@"{""choices"":[]}")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = CreateService();
        var config = new ConfiguracionIA
        {
            Proveedor = "AzureOpenAI",
            Modelo = "gpt-4",
            ApiKey = "test-key",
            AzureEndpoint = "https://myresource.openai.azure.com/openai/deployments/gpt-4/chat/completions",
            AzureApiVersion = null // Should use default
        };

        // Act
        await service.TestConnectionAsync(config);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Contains("api-version=2024-02-15-preview", capturedRequest!.RequestUri?.ToString());
    }

    [Fact]
    public async Task TestConnectionAsync_Success_IncludesResponseTime()
    {
        // Arrange
        SetupMockHttpClient(HttpStatusCode.OK, @"{""choices"":[]}");
        var service = CreateService();
        var config = new ConfiguracionIA
        {
            Proveedor = "OpenAI",
            Modelo = "gpt-4",
            ApiKey = "test-key"
        };

        // Act
        var (success, message) = await service.TestConnectionAsync(config);

        // Assert
        Assert.True(success);
        Assert.Contains("Tiempo de respuesta", message);
        Assert.Contains("ms", message);
    }

    #endregion
}
