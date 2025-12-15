using Bunit;
using Ceiba.Core.Entities;
using Ceiba.Core.Interfaces;
using Ceiba.Shared.DTOs;
using Ceiba.Web.Components.Pages.Admin;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Security.Claims;

namespace Ceiba.Web.Tests.Components;

/// <summary>
/// Component tests for AiConfigManager Blazor component.
/// Tests AI configuration management for automated reports.
/// </summary>
[Trait("Category", "Component")]
public class AiConfigManagerTests : TestContext
{
    private readonly Mock<IAiConfigurationService> _mockConfigService;
    private readonly Guid _testUserId = Guid.NewGuid();

    public AiConfigManagerTests()
    {
        _mockConfigService = new Mock<IAiConfigurationService>();

        Services.AddSingleton(_mockConfigService.Object);
        Services.AddSingleton<AuthenticationStateProvider>(new TestAuthStateProvider(_testUserId, "ADMIN"));
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        SetupDefaultMocks();
    }

    #region Rendering Tests

    [Fact(DisplayName = "AiConfigManager should render page title")]
    public void AiConfigManager_ShouldRenderPageTitle()
    {
        // Act
        var cut = Render<AiConfigManager>();

        // Assert
        cut.Markup.Should().Contain("Configuracion de Inteligencia Artificial");
    }

    [Fact(DisplayName = "AiConfigManager should render provider selection")]
    public void AiConfigManager_ShouldRenderProviderSelection()
    {
        // Act
        var cut = Render<AiConfigManager>();

        // Assert
        cut.Markup.Should().Contain("Proveedor de IA");
        cut.Markup.Should().Contain("OpenAI");
    }

    [Fact(DisplayName = "AiConfigManager should render back button")]
    public void AiConfigManager_ShouldRenderBackButton()
    {
        // Act
        var cut = Render<AiConfigManager>();

        // Assert
        cut.Markup.Should().Contain("Volver");
    }

    [Fact(DisplayName = "AiConfigManager should render save button")]
    public void AiConfigManager_ShouldRenderSaveButton()
    {
        // Act
        var cut = Render<AiConfigManager>();

        // Assert
        cut.Markup.Should().Contain("Guardar Configuracion");
    }

    [Fact(DisplayName = "AiConfigManager should render test connection button")]
    public void AiConfigManager_ShouldRenderTestConnectionButton()
    {
        // Act
        var cut = Render<AiConfigManager>();

        // Assert
        cut.Markup.Should().Contain("Probar Conexion");
    }

    [Fact(DisplayName = "AiConfigManager should render current status panel")]
    public void AiConfigManager_ShouldRenderCurrentStatusPanel()
    {
        // Act
        var cut = Render<AiConfigManager>();

        // Assert
        cut.Markup.Should().Contain("Estado Actual");
    }

    [Fact(DisplayName = "AiConfigManager should render help panel")]
    public void AiConfigManager_ShouldRenderHelpPanel()
    {
        // Act
        var cut = Render<AiConfigManager>();

        // Assert
        cut.Markup.Should().Contain("Ayuda");
        cut.Markup.Should().Contain("platform.openai.com");
    }

    #endregion

    #region Loading State Tests

    [Fact(DisplayName = "AiConfigManager should show loading state initially")]
    public void AiConfigManager_ShouldShowLoadingStateInitially()
    {
        // Arrange - Make the service never complete to keep loading state
        _mockConfigService.Setup(x => x.GetActiveConfigurationAsync())
            .Returns(new TaskCompletionSource<ConfiguracionIA?>().Task);

        // Act
        var cut = Render<AiConfigManager>();

        // Assert
        cut.Markup.Should().Contain("Cargando configuracion...");
    }

    [Fact(DisplayName = "AiConfigManager should display existing configuration")]
    public void AiConfigManager_ShouldDisplayExistingConfiguration()
    {
        // Arrange
        var existingConfig = new ConfiguracionIA
        {
            Id = 1,
            Proveedor = "OpenAI",
            Modelo = "gpt-4o",
            ApiKey = "sk-test12345678901234567890",
            MaxTokens = 2000,
            Temperature = 0.7,
            CreatedAt = DateTime.UtcNow.AddDays(-7),
            UpdatedAt = DateTime.UtcNow
        };
        _mockConfigService.Setup(x => x.GetActiveConfigurationAsync())
            .ReturnsAsync(existingConfig);

        // Act
        var cut = Render<AiConfigManager>();

        // Assert
        cut.Markup.Should().Contain("OpenAI");
        cut.Markup.Should().Contain("gpt-4o");
        cut.Markup.Should().Contain("Configurada");
    }

    [Fact(DisplayName = "AiConfigManager should show no config message when none exists")]
    public void AiConfigManager_ShouldShowNoConfigMessageWhenNoneExists()
    {
        // Arrange
        _mockConfigService.Setup(x => x.GetActiveConfigurationAsync())
            .ReturnsAsync((ConfiguracionIA?)null);

        // Act
        var cut = Render<AiConfigManager>();

        // Assert
        cut.Markup.Should().Contain("No hay configuracion de IA activa");
    }

    #endregion

    #region Provider Selection Tests

    [Fact(DisplayName = "AiConfigManager should display all AI providers")]
    public void AiConfigManager_ShouldDisplayAllAiProviders()
    {
        // Act
        var cut = Render<AiConfigManager>();

        // Assert
        cut.Markup.Should().Contain("OpenAI");
        cut.Markup.Should().Contain("Google Gemini");
        cut.Markup.Should().Contain("DeepSeek");
        cut.Markup.Should().Contain("Azure OpenAI");
        cut.Markup.Should().Contain("Ollama");
    }

    [Fact(DisplayName = "AiConfigManager should show API key field for OpenAI")]
    public void AiConfigManager_ShouldShowApiKeyFieldForOpenAI()
    {
        // Act
        var cut = Render<AiConfigManager>();

        // Assert
        cut.Markup.Should().Contain("API Key");
    }

    #endregion

    #region Model Selection Tests

    [Fact(DisplayName = "AiConfigManager should display model selection")]
    public void AiConfigManager_ShouldDisplayModelSelection()
    {
        // Act
        var cut = Render<AiConfigManager>();

        // Assert
        cut.Markup.Should().Contain("Modelo");
        cut.Markup.Should().Contain("<select");
    }

    #endregion

    #region Advanced Settings Tests

    [Fact(DisplayName = "AiConfigManager should have collapsed advanced settings by default")]
    public void AiConfigManager_ShouldHaveCollapsedAdvancedSettingsByDefault()
    {
        // Act
        var cut = Render<AiConfigManager>();

        // Assert
        cut.Markup.Should().Contain("Configuración Avanzada");
        // Max Tokens should not be visible when collapsed
    }

    [Fact(DisplayName = "AiConfigManager should expand advanced settings on click")]
    public async Task AiConfigManager_ShouldExpandAdvancedSettingsOnClick()
    {
        // Arrange
        var cut = Render<AiConfigManager>();

        // Act
        var advancedButton = cut.FindAll("button").First(b => b.TextContent.Contains("Configuración Avanzada"));
        await cut.InvokeAsync(() => advancedButton.Click());

        // Assert
        cut.Markup.Should().Contain("Max Tokens");
        cut.Markup.Should().Contain("Temperature");
    }

    #endregion

    #region Save Configuration Tests

    [Fact(DisplayName = "AiConfigManager should save configuration on submit")]
    public async Task AiConfigManager_ShouldSaveConfigurationOnSubmit()
    {
        // Arrange
        var cut = Render<AiConfigManager>();

        // Act - Find and click the save button
        var form = cut.Find("form");
        await cut.InvokeAsync(() => form.Submit());

        // Assert
        _mockConfigService.Verify(x => x.SaveConfigurationAsync(
            It.IsAny<ConfiguracionIA>(),
            It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "AiConfigManager should show success message after save")]
    public async Task AiConfigManager_ShouldShowSuccessMessageAfterSave()
    {
        // Arrange
        _mockConfigService.Setup(x => x.SaveConfigurationAsync(It.IsAny<ConfiguracionIA>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConfiguracionIA { Id = 1, Proveedor = "OpenAI", Modelo = "gpt-4o" });

        var cut = Render<AiConfigManager>();

        // Act
        var form = cut.Find("form");
        await cut.InvokeAsync(() => form.Submit());

        // Assert
        cut.Markup.Should().Contain("Configuracion guardada exitosamente");
    }

    [Fact(DisplayName = "AiConfigManager should show error message on save failure")]
    public async Task AiConfigManager_ShouldShowErrorMessageOnSaveFailure()
    {
        // Arrange
        _mockConfigService.Setup(x => x.SaveConfigurationAsync(It.IsAny<ConfiguracionIA>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Error de prueba"));

        var cut = Render<AiConfigManager>();

        // Act
        var form = cut.Find("form");
        await cut.InvokeAsync(() => form.Submit());

        // Assert
        cut.Markup.Should().Contain("Error al guardar la configuracion");
    }

    #endregion

    #region Test Connection Tests

    [Fact(DisplayName = "AiConfigManager should test connection when button clicked")]
    public async Task AiConfigManager_ShouldTestConnectionWhenButtonClicked()
    {
        // Arrange
        _mockConfigService.Setup(x => x.TestConnectionAsync(It.IsAny<ConfiguracionIA>()))
            .ReturnsAsync((true, "Conexión exitosa"));

        var cut = Render<AiConfigManager>();

        // Act
        var testButton = cut.FindAll("button").First(b => b.TextContent.Contains("Probar Conexion"));
        await cut.InvokeAsync(() => testButton.Click());

        // Assert
        _mockConfigService.Verify(x => x.TestConnectionAsync(It.IsAny<ConfiguracionIA>()), Times.Once);
    }

    [Fact(DisplayName = "AiConfigManager should show success result after successful connection test")]
    public async Task AiConfigManager_ShouldShowSuccessResultAfterSuccessfulConnectionTest()
    {
        // Arrange
        _mockConfigService.Setup(x => x.TestConnectionAsync(It.IsAny<ConfiguracionIA>()))
            .ReturnsAsync((true, "Conexión exitosa con el proveedor"));

        var cut = Render<AiConfigManager>();

        // Act
        var testButton = cut.FindAll("button").First(b => b.TextContent.Contains("Probar Conexion"));
        await cut.InvokeAsync(() => testButton.Click());

        // Assert
        cut.Markup.Should().Contain("Resultado de Prueba");
        cut.Markup.Should().Contain("Conexión exitosa con el proveedor");
    }

    [Fact(DisplayName = "AiConfigManager should show failure result after failed connection test")]
    public async Task AiConfigManager_ShouldShowFailureResultAfterFailedConnectionTest()
    {
        // Arrange
        _mockConfigService.Setup(x => x.TestConnectionAsync(It.IsAny<ConfiguracionIA>()))
            .ReturnsAsync((false, "API Key inválida"));

        var cut = Render<AiConfigManager>();

        // Act
        var testButton = cut.FindAll("button").First(b => b.TextContent.Contains("Probar Conexion"));
        await cut.InvokeAsync(() => testButton.Click());

        // Assert
        cut.Markup.Should().Contain("API Key inválida");
    }

    #endregion

    #region API Key Handling Tests

    [Fact(DisplayName = "AiConfigManager should mask existing API key")]
    public void AiConfigManager_ShouldMaskExistingApiKey()
    {
        // Arrange
        var existingConfig = new ConfiguracionIA
        {
            Id = 1,
            Proveedor = "OpenAI",
            Modelo = "gpt-4o",
            ApiKey = "sk-test12345678901234567890",
            MaxTokens = 2000,
            Temperature = 0.7,
            CreatedAt = DateTime.UtcNow
        };
        _mockConfigService.Setup(x => x.GetActiveConfigurationAsync())
            .ReturnsAsync(existingConfig);

        // Act
        var cut = Render<AiConfigManager>();

        // Assert - Should show masked version
        cut.Markup.Should().Contain("sk-t");
        cut.Markup.Should().Contain("...");
        cut.Markup.Should().NotContain("sk-test12345678901234567890");
    }

    [Fact(DisplayName = "AiConfigManager should toggle API key visibility")]
    public async Task AiConfigManager_ShouldToggleApiKeyVisibility()
    {
        // Arrange
        var cut = Render<AiConfigManager>();

        // Find the toggle button (the eye icon button)
        var toggleButton = cut.FindAll("button.btn-outline-secondary").FirstOrDefault(b => b.InnerHtml.Contains("bi-eye"));
        toggleButton.Should().NotBeNull();

        // Act
        await cut.InvokeAsync(() => toggleButton!.Click());

        // Assert - Password field type should change
        cut.Markup.Should().Contain("type=\"text\"");
    }

    #endregion

    #region Error Handling Tests

    [Fact(DisplayName = "AiConfigManager should handle load error gracefully")]
    public void AiConfigManager_ShouldHandleLoadErrorGracefully()
    {
        // Arrange
        _mockConfigService.Setup(x => x.GetActiveConfigurationAsync())
            .ThrowsAsync(new InvalidOperationException("Error de conexión"));

        // Act
        var cut = Render<AiConfigManager>();

        // Assert
        cut.Markup.Should().Contain("Error al cargar la configuracion de IA");
    }

    #endregion

    #region Helpers

    private void SetupDefaultMocks()
    {
        _mockConfigService.Setup(x => x.GetActiveConfigurationAsync())
            .ReturnsAsync((ConfiguracionIA?)null);

        _mockConfigService.Setup(x => x.SaveConfigurationAsync(It.IsAny<ConfiguracionIA>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConfiguracionIA { Id = 1, Proveedor = "OpenAI", Modelo = "gpt-4o" });

        _mockConfigService.Setup(x => x.TestConnectionAsync(It.IsAny<ConfiguracionIA>()))
            .ReturnsAsync((true, "Conexión exitosa"));
    }

    private class TestAuthStateProvider : AuthenticationStateProvider
    {
        private readonly AuthenticationState _authState;

        public TestAuthStateProvider(Guid userId, string role)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "admin@ceiba.local"),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            }, "Test");

            _authState = new AuthenticationState(new ClaimsPrincipal(identity));
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(_authState);
    }

    #endregion
}
