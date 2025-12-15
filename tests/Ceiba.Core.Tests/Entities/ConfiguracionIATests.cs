using Ceiba.Core.Entities;
using FluentAssertions;

namespace Ceiba.Core.Tests.Entities;

/// <summary>
/// Unit tests for ConfiguracionIA entity.
/// Tests AI service configuration and validation logic.
/// US4: Reportes Automatizados Diarios con IA.
/// </summary>
[Trait("Category", "Unit")]
public class ConfiguracionIATests
{
    #region Default Value Tests

    [Fact(DisplayName = "ConfiguracionIA should have default provider as OpenAI")]
    public void ConfiguracionIA_Proveedor_ShouldDefaultToOpenAI()
    {
        // Arrange & Act
        var config = new ConfiguracionIA();

        // Assert
        config.Proveedor.Should().Be("OpenAI");
    }

    [Fact(DisplayName = "ConfiguracionIA should have default model as gpt-4o-mini")]
    public void ConfiguracionIA_Modelo_ShouldDefaultToGpt4oMini()
    {
        // Arrange & Act
        var config = new ConfiguracionIA();

        // Assert
        config.Modelo.Should().Be("gpt-4o-mini");
    }

    [Fact(DisplayName = "ConfiguracionIA should have default MaxTokens as 4000")]
    public void ConfiguracionIA_MaxTokens_ShouldDefaultTo4000()
    {
        // Arrange & Act
        var config = new ConfiguracionIA();

        // Assert
        config.MaxTokens.Should().Be(4000);
    }

    [Fact(DisplayName = "ConfiguracionIA should have default Temperature as 0.7")]
    public void ConfiguracionIA_Temperature_ShouldDefaultTo07()
    {
        // Arrange & Act
        var config = new ConfiguracionIA();

        // Assert
        config.Temperature.Should().Be(0.7);
    }

    [Fact(DisplayName = "ConfiguracionIA should have default MaxReportesParaNarrativa as 0")]
    public void ConfiguracionIA_MaxReportesParaNarrativa_ShouldDefaultToZero()
    {
        // Arrange & Act
        var config = new ConfiguracionIA();

        // Assert
        config.MaxReportesParaNarrativa.Should().Be(0);
    }

    [Fact(DisplayName = "ConfiguracionIA should have default Activo as true")]
    public void ConfiguracionIA_Activo_ShouldDefaultToTrue()
    {
        // Arrange & Act
        var config = new ConfiguracionIA();

        // Assert
        config.Activo.Should().BeTrue();
    }

    [Fact(DisplayName = "ConfiguracionIA should have nullable properties default to null")]
    public void ConfiguracionIA_NullableProperties_ShouldDefaultToNull()
    {
        // Arrange & Act
        var config = new ConfiguracionIA();

        // Assert
        config.ApiKey.Should().BeNull();
        config.Endpoint.Should().BeNull();
        config.AzureEndpoint.Should().BeNull();
        config.AzureApiVersion.Should().BeNull();
        config.LocalEndpoint.Should().BeNull();
        config.UpdatedAt.Should().BeNull();
        config.ModificadoPorId.Should().BeNull();
    }

    #endregion

    #region Validation Tests - Required Fields

    [Fact(DisplayName = "Validate should fail when Proveedor is empty")]
    public void Validate_EmptyProveedor_ShouldFail()
    {
        // Arrange
        var config = new ConfiguracionIA { Proveedor = "" };

        // Act
        var (isValid, errors) = config.Validate();

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("proveedor"));
    }

    [Fact(DisplayName = "Validate should fail when Modelo is empty")]
    public void Validate_EmptyModelo_ShouldFail()
    {
        // Arrange
        var config = new ConfiguracionIA { Modelo = "" };

        // Act
        var (isValid, errors) = config.Validate();

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("modelo"));
    }

    #endregion

    #region Validation Tests - OpenAI Provider

    [Fact(DisplayName = "Validate should pass for valid OpenAI configuration")]
    public void Validate_ValidOpenAIConfig_ShouldPass()
    {
        // Arrange
        var config = new ConfiguracionIA
        {
            Proveedor = "OpenAI",
            Modelo = "gpt-4o-mini",
            ApiKey = "sk-test-key"
        };

        // Act
        var (isValid, errors) = config.Validate();

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact(DisplayName = "Validate should fail for OpenAI without ApiKey")]
    public void Validate_OpenAIWithoutApiKey_ShouldFail()
    {
        // Arrange
        var config = new ConfiguracionIA
        {
            Proveedor = "OpenAI",
            Modelo = "gpt-4",
            ApiKey = null
        };

        // Act
        var (isValid, errors) = config.Validate();

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("API Key") && e.Contains("OpenAI"));
    }

    #endregion

    #region Validation Tests - Azure OpenAI Provider

    [Fact(DisplayName = "Validate should pass for valid Azure OpenAI configuration")]
    public void Validate_ValidAzureOpenAIConfig_ShouldPass()
    {
        // Arrange
        var config = new ConfiguracionIA
        {
            Proveedor = "AzureOpenAI",
            Modelo = "gpt-4",
            ApiKey = "azure-api-key",
            AzureEndpoint = "https://my-resource.openai.azure.com"
        };

        // Act
        var (isValid, errors) = config.Validate();

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact(DisplayName = "Validate should fail for Azure OpenAI without ApiKey")]
    public void Validate_AzureOpenAIWithoutApiKey_ShouldFail()
    {
        // Arrange
        var config = new ConfiguracionIA
        {
            Proveedor = "AzureOpenAI",
            Modelo = "gpt-4",
            AzureEndpoint = "https://my-resource.openai.azure.com"
        };

        // Act
        var (isValid, errors) = config.Validate();

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("API Key") && e.Contains("Azure"));
    }

    [Fact(DisplayName = "Validate should fail for Azure OpenAI without AzureEndpoint")]
    public void Validate_AzureOpenAIWithoutEndpoint_ShouldFail()
    {
        // Arrange
        var config = new ConfiguracionIA
        {
            Proveedor = "AzureOpenAI",
            Modelo = "gpt-4",
            ApiKey = "azure-api-key"
        };

        // Act
        var (isValid, errors) = config.Validate();

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("endpoint") && e.Contains("Azure"));
    }

    #endregion

    #region Validation Tests - Local/Ollama Provider

    [Fact(DisplayName = "Validate should pass for valid Local configuration")]
    public void Validate_ValidLocalConfig_ShouldPass()
    {
        // Arrange
        var config = new ConfiguracionIA
        {
            Proveedor = "Local",
            Modelo = "llama3.2",
            LocalEndpoint = "http://localhost:11434"
        };

        // Act
        var (isValid, errors) = config.Validate();

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact(DisplayName = "Validate should pass for valid Ollama configuration")]
    public void Validate_ValidOllamaConfig_ShouldPass()
    {
        // Arrange
        var config = new ConfiguracionIA
        {
            Proveedor = "Ollama",
            Modelo = "mistral",
            LocalEndpoint = "http://localhost:11434"
        };

        // Act
        var (isValid, errors) = config.Validate();

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact(DisplayName = "Validate should fail for Local without LocalEndpoint")]
    public void Validate_LocalWithoutEndpoint_ShouldFail()
    {
        // Arrange
        var config = new ConfiguracionIA
        {
            Proveedor = "Local",
            Modelo = "llama3.2"
        };

        // Act
        var (isValid, errors) = config.Validate();

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("endpoint") && e.Contains("local"));
    }

    #endregion

    #region Validation Tests - Numeric Ranges

    [Theory(DisplayName = "Validate should fail for MaxTokens outside valid range")]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(499)]
    [InlineData(128001)]
    [InlineData(200000)]
    public void Validate_InvalidMaxTokens_ShouldFail(int maxTokens)
    {
        // Arrange
        var config = new ConfiguracionIA
        {
            Proveedor = "OpenAI",
            Modelo = "gpt-4",
            ApiKey = "sk-test",
            MaxTokens = maxTokens
        };

        // Act
        var (isValid, errors) = config.Validate();

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("MaxTokens"));
    }

    [Theory(DisplayName = "Validate should pass for MaxTokens within valid range")]
    [InlineData(500)]
    [InlineData(4000)]
    [InlineData(8000)]
    [InlineData(128000)]
    public void Validate_ValidMaxTokens_ShouldPass(int maxTokens)
    {
        // Arrange
        var config = new ConfiguracionIA
        {
            Proveedor = "OpenAI",
            Modelo = "gpt-4",
            ApiKey = "sk-test",
            MaxTokens = maxTokens
        };

        // Act
        var (isValid, errors) = config.Validate();

        // Assert
        isValid.Should().BeTrue();
    }

    [Theory(DisplayName = "Validate should fail for Temperature outside valid range")]
    [InlineData(-0.1)]
    [InlineData(2.1)]
    [InlineData(5.0)]
    public void Validate_InvalidTemperature_ShouldFail(double temperature)
    {
        // Arrange
        var config = new ConfiguracionIA
        {
            Proveedor = "OpenAI",
            Modelo = "gpt-4",
            ApiKey = "sk-test",
            Temperature = temperature
        };

        // Act
        var (isValid, errors) = config.Validate();

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("Temperature"));
    }

    [Theory(DisplayName = "Validate should pass for Temperature within valid range")]
    [InlineData(0)]
    [InlineData(0.7)]
    [InlineData(1.0)]
    [InlineData(2.0)]
    public void Validate_ValidTemperature_ShouldPass(double temperature)
    {
        // Arrange
        var config = new ConfiguracionIA
        {
            Proveedor = "OpenAI",
            Modelo = "gpt-4",
            ApiKey = "sk-test",
            Temperature = temperature
        };

        // Act
        var (isValid, errors) = config.Validate();

        // Assert
        isValid.Should().BeTrue();
    }

    [Theory(DisplayName = "Validate should fail for MaxReportesParaNarrativa outside valid range")]
    [InlineData(-1)]
    [InlineData(1001)]
    [InlineData(5000)]
    public void Validate_InvalidMaxReportesParaNarrativa_ShouldFail(int maxReportes)
    {
        // Arrange
        var config = new ConfiguracionIA
        {
            Proveedor = "OpenAI",
            Modelo = "gpt-4",
            ApiKey = "sk-test",
            MaxReportesParaNarrativa = maxReportes
        };

        // Act
        var (isValid, errors) = config.Validate();

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("MaxReportesParaNarrativa"));
    }

    [Theory(DisplayName = "Validate should pass for MaxReportesParaNarrativa within valid range")]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(1000)]
    public void Validate_ValidMaxReportesParaNarrativa_ShouldPass(int maxReportes)
    {
        // Arrange
        var config = new ConfiguracionIA
        {
            Proveedor = "OpenAI",
            Modelo = "gpt-4",
            ApiKey = "sk-test",
            MaxReportesParaNarrativa = maxReportes
        };

        // Act
        var (isValid, errors) = config.Validate();

        // Assert
        isValid.Should().BeTrue();
    }

    #endregion

    #region Inheritance Tests

    [Fact(DisplayName = "ConfiguracionIA should inherit from BaseEntity")]
    public void ConfiguracionIA_ShouldInheritFromBaseEntity()
    {
        // Arrange & Act
        var config = new ConfiguracionIA();

        // Assert
        config.Should().BeAssignableTo<BaseEntity>();
        config.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    #endregion
}
