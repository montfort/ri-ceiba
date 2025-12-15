using Bunit;
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
/// Component tests for SuggestionManager Blazor component.
/// Tests suggestion field management for reports.
/// </summary>
[Trait("Category", "Component")]
public class SuggestionManagerTests : TestContext
{
    private readonly Mock<ICatalogAdminService> _mockCatalogService;
    private readonly Guid _testUserId = Guid.NewGuid();

    public SuggestionManagerTests()
    {
        _mockCatalogService = new Mock<ICatalogAdminService>();

        Services.AddSingleton(_mockCatalogService.Object);
        Services.AddSingleton<AuthenticationStateProvider>(new TestAuthStateProvider(_testUserId, "ADMIN"));
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        SetupDefaultMocks();
    }

    #region Rendering Tests

    [Fact(DisplayName = "SuggestionManager should render page title")]
    public void SuggestionManager_ShouldRenderPageTitle()
    {
        // Act
        var cut = Render<SuggestionManager>();

        // Assert
        cut.Markup.Should().Contain("Gestión de Sugerencias");
    }

    [Fact(DisplayName = "SuggestionManager should render campo categories")]
    public void SuggestionManager_ShouldRenderCampoCategories()
    {
        // Act
        var cut = Render<SuggestionManager>();

        // Assert
        cut.Markup.Should().Contain("Datos de la Persona");
        cut.Markup.Should().Contain("Detalles Operativos");
    }

    [Fact(DisplayName = "SuggestionManager should render persona fields")]
    public void SuggestionManager_ShouldRenderPersonaFields()
    {
        // Act
        var cut = Render<SuggestionManager>();

        // Assert
        cut.Markup.Should().Contain("Sexo");
    }

    [Fact(DisplayName = "SuggestionManager should render operativos fields")]
    public void SuggestionManager_ShouldRenderOperativosFields()
    {
        // Act
        var cut = Render<SuggestionManager>();

        // Assert
        cut.Markup.Should().Contain("Delito");
        cut.Markup.Should().Contain("Tipo de Atención");
    }

    [Fact(DisplayName = "SuggestionManager should render new suggestion button")]
    public void SuggestionManager_ShouldRenderNewSuggestionButton()
    {
        // Act
        var cut = Render<SuggestionManager>();

        // Assert
        cut.Markup.Should().Contain("Nueva Sugerencia");
    }

    #endregion

    #region Loading State Tests

    [Fact(DisplayName = "SuggestionManager should show loading state initially")]
    public void SuggestionManager_ShouldShowLoadingStateInitially()
    {
        // Arrange - Make the service never complete
        _mockCatalogService.Setup(x => x.GetSugerenciasAsync(It.IsAny<string>()))
            .Returns(new TaskCompletionSource<List<SugerenciaDto>>().Task);

        // Act
        var cut = Render<SuggestionManager>();

        // Assert
        cut.Markup.Should().Contain("spinner-border");
    }

    [Fact(DisplayName = "SuggestionManager should show empty message when no suggestions")]
    public void SuggestionManager_ShouldShowEmptyMessageWhenNoSuggestions()
    {
        // Arrange
        _mockCatalogService.Setup(x => x.GetSugerenciasAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<SugerenciaDto>());

        // Act
        var cut = Render<SuggestionManager>();

        // Assert
        cut.Markup.Should().Contain("No hay sugerencias para este campo");
    }

    #endregion

    #region Suggestion List Tests

    [Fact(DisplayName = "SuggestionManager should display suggestions in table")]
    public void SuggestionManager_ShouldDisplaySuggestionsInTable()
    {
        // Arrange
        var suggestions = CreateTestSuggestions(3);
        _mockCatalogService.Setup(x => x.GetSugerenciasAsync(It.IsAny<string>()))
            .ReturnsAsync(suggestions);

        // Act
        var cut = Render<SuggestionManager>();

        // Assert
        cut.Markup.Should().Contain("<table");
        cut.Markup.Should().Contain("Sugerencia 1");
        cut.Markup.Should().Contain("Sugerencia 2");
        cut.Markup.Should().Contain("Sugerencia 3");
    }

    [Fact(DisplayName = "SuggestionManager should show table headers")]
    public void SuggestionManager_ShouldShowTableHeaders()
    {
        // Arrange
        var suggestions = CreateTestSuggestions(1);
        _mockCatalogService.Setup(x => x.GetSugerenciasAsync(It.IsAny<string>()))
            .ReturnsAsync(suggestions);

        // Act
        var cut = Render<SuggestionManager>();

        // Assert
        cut.Markup.Should().Contain("Orden");
        cut.Markup.Should().Contain("Valor");
        cut.Markup.Should().Contain("Estado");
        cut.Markup.Should().Contain("Acciones");
    }

    [Fact(DisplayName = "SuggestionManager should show active/inactive badges")]
    public void SuggestionManager_ShouldShowActiveInactiveBadges()
    {
        // Arrange
        var suggestions = new List<SugerenciaDto>
        {
            new() { Id = 1, Campo = "Sexo", Valor = "Active", Orden = 1, Activo = true },
            new() { Id = 2, Campo = "Sexo", Valor = "Inactive", Orden = 2, Activo = false }
        };
        _mockCatalogService.Setup(x => x.GetSugerenciasAsync(It.IsAny<string>()))
            .ReturnsAsync(suggestions);

        // Act
        var cut = Render<SuggestionManager>();

        // Assert
        cut.Markup.Should().Contain("bg-success"); // Active
        cut.Markup.Should().Contain("bg-secondary"); // Inactive
        cut.Markup.Should().Contain("Activo");
        cut.Markup.Should().Contain("Inactivo");
    }

    #endregion

    #region Campo Selection Tests

    [Fact(DisplayName = "SuggestionManager should load suggestions when campo selected")]
    public async Task SuggestionManager_ShouldLoadSuggestionsWhenCampoSelected()
    {
        // Arrange
        var suggestions = CreateTestSuggestions(2);
        _mockCatalogService.Setup(x => x.GetSugerenciasAsync(It.IsAny<string>()))
            .ReturnsAsync(suggestions);

        var cut = Render<SuggestionManager>();

        // Act - Click on a different campo
        var delitoButton = cut.FindAll("button.list-group-item").First(b => b.TextContent.Contains("Delito"));
        await cut.InvokeAsync(() => delitoButton.Click());

        // Assert - Note: campo names are lowercase in the component
        _mockCatalogService.Verify(x => x.GetSugerenciasAsync("delito", It.IsAny<bool?>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact(DisplayName = "SuggestionManager should highlight selected campo")]
    public async Task SuggestionManager_ShouldHighlightSelectedCampo()
    {
        // Arrange
        _mockCatalogService.Setup(x => x.GetSugerenciasAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<SugerenciaDto>());

        var cut = Render<SuggestionManager>();

        // Act - Click on Delito
        var delitoButton = cut.FindAll("button.list-group-item").First(b => b.TextContent.Contains("Delito"));
        await cut.InvokeAsync(() => delitoButton.Click());

        // Assert - Delito should have active class
        cut.FindAll("button.list-group-item.active")
            .Should().Contain(b => b.TextContent.Contains("Delito"));
    }

    #endregion

    #region Modal Tests

    [Fact(DisplayName = "SuggestionManager should open create modal")]
    public async Task SuggestionManager_ShouldOpenCreateModal()
    {
        // Arrange
        var cut = Render<SuggestionManager>();

        // Act
        var newButton = cut.FindAll("button").First(b => b.TextContent.Contains("Nueva Sugerencia"));
        await cut.InvokeAsync(() => newButton.Click());

        // Assert
        cut.Markup.Should().Contain("modal");
        cut.Markup.Should().Contain("Nueva Sugerencia");
        cut.Markup.Should().Contain("Valor *");
    }

    [Fact(DisplayName = "SuggestionManager should open edit modal")]
    public async Task SuggestionManager_ShouldOpenEditModal()
    {
        // Arrange
        var suggestions = CreateTestSuggestions(1);
        _mockCatalogService.Setup(x => x.GetSugerenciasAsync(It.IsAny<string>()))
            .ReturnsAsync(suggestions);

        var cut = Render<SuggestionManager>();

        // Act
        var editButton = cut.Find("button.btn-outline-primary");
        await cut.InvokeAsync(() => editButton.Click());

        // Assert
        cut.Markup.Should().Contain("Editar Sugerencia");
    }

    [Fact(DisplayName = "SuggestionManager should close modal on cancel")]
    public async Task SuggestionManager_ShouldCloseModalOnCancel()
    {
        // Arrange
        var cut = Render<SuggestionManager>();

        // Open modal
        var newButton = cut.FindAll("button").First(b => b.TextContent.Contains("Nueva Sugerencia"));
        await cut.InvokeAsync(() => newButton.Click());

        // Act
        var cancelButton = cut.FindAll("button").First(b => b.TextContent == "Cancelar");
        await cut.InvokeAsync(() => cancelButton.Click());

        // Assert
        cut.Markup.Should().NotContain("modal fade show d-block");
    }

    #endregion

    #region CRUD Tests

    [Fact(DisplayName = "SuggestionManager should create new suggestion", Skip = "Modal interaction requires component-specific selector")]
    public async Task SuggestionManager_ShouldCreateNewSuggestion()
    {
        // Arrange
        _mockCatalogService.Setup(x => x.CreateSugerenciaAsync(It.IsAny<CreateSugerenciaDto>(), It.IsAny<Guid>()))
            .ReturnsAsync(new SugerenciaDto { Id = 1, Campo = "Sexo", Valor = "Nuevo Valor", Orden = 1, Activo = true });

        var cut = Render<SuggestionManager>();

        // Open modal
        var newButton = cut.FindAll("button").First(b => b.TextContent.Contains("Nueva Sugerencia"));
        await cut.InvokeAsync(() => newButton.Click());

        // Fill form
        var valorInput = cut.FindAll("input.form-control").First();
        await cut.InvokeAsync(() => valorInput.Change("Nuevo Valor"));

        // Act - Save
        var saveButton = cut.FindAll("button").First(b => b.TextContent == "Guardar");
        await cut.InvokeAsync(() => saveButton.Click());

        // Assert
        _mockCatalogService.Verify(x => x.CreateSugerenciaAsync(
            It.Is<CreateSugerenciaDto>(dto => dto.Valor == "Nuevo Valor"),
            It.IsAny<Guid>()), Times.Once);
    }

    [Fact(DisplayName = "SuggestionManager should update existing suggestion")]
    public async Task SuggestionManager_ShouldUpdateExistingSuggestion()
    {
        // Arrange
        var suggestions = CreateTestSuggestions(1);
        _mockCatalogService.Setup(x => x.GetSugerenciasAsync(It.IsAny<string>()))
            .ReturnsAsync(suggestions);
        _mockCatalogService.Setup(x => x.UpdateSugerenciaAsync(It.IsAny<int>(), It.IsAny<CreateSugerenciaDto>(), It.IsAny<Guid>()))
            .ReturnsAsync(new SugerenciaDto { Id = 1, Campo = "Sexo", Valor = "Sugerencia 1", Orden = 1, Activo = true });

        var cut = Render<SuggestionManager>();

        // Open edit modal
        var editButton = cut.Find("button.btn-outline-primary");
        await cut.InvokeAsync(() => editButton.Click());

        // Act - Save
        var saveButton = cut.FindAll("button").First(b => b.TextContent == "Guardar");
        await cut.InvokeAsync(() => saveButton.Click());

        // Assert
        _mockCatalogService.Verify(x => x.UpdateSugerenciaAsync(
            1,
            It.IsAny<CreateSugerenciaDto>(),
            It.IsAny<Guid>()), Times.Once);
    }

    [Fact(DisplayName = "SuggestionManager should show delete confirmation")]
    public async Task SuggestionManager_ShouldShowDeleteConfirmation()
    {
        // Arrange
        var suggestions = CreateTestSuggestions(1);
        _mockCatalogService.Setup(x => x.GetSugerenciasAsync(It.IsAny<string>()))
            .ReturnsAsync(suggestions);

        var cut = Render<SuggestionManager>();

        // Act
        var deleteButton = cut.Find("button.btn-outline-danger");
        await cut.InvokeAsync(() => deleteButton.Click());

        // Assert
        cut.Markup.Should().Contain("Confirmar Eliminación");
        cut.Markup.Should().Contain("Sugerencia 1");
    }

    [Fact(DisplayName = "SuggestionManager should delete suggestion")]
    public async Task SuggestionManager_ShouldDeleteSuggestion()
    {
        // Arrange
        var suggestions = CreateTestSuggestions(1);
        _mockCatalogService.Setup(x => x.GetSugerenciasAsync(It.IsAny<string>()))
            .ReturnsAsync(suggestions);
        _mockCatalogService.Setup(x => x.DeleteSugerenciaAsync(It.IsAny<int>(), It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        var cut = Render<SuggestionManager>();

        // Open delete confirmation
        var deleteButton = cut.Find("button.btn-outline-danger");
        await cut.InvokeAsync(() => deleteButton.Click());

        // Act - Confirm delete
        var confirmButton = cut.FindAll("button.btn-danger").First(b => b.TextContent == "Eliminar");
        await cut.InvokeAsync(() => confirmButton.Click());

        // Assert
        _mockCatalogService.Verify(x => x.DeleteSugerenciaAsync(1, It.IsAny<Guid>()), Times.Once);
    }

    #endregion

    #region Reorder Tests

    [Fact(DisplayName = "SuggestionManager should show reorder buttons")]
    public void SuggestionManager_ShouldShowReorderButtons()
    {
        // Arrange
        var suggestions = CreateTestSuggestions(3);
        _mockCatalogService.Setup(x => x.GetSugerenciasAsync(It.IsAny<string>()))
            .ReturnsAsync(suggestions);

        // Act
        var cut = Render<SuggestionManager>();

        // Assert
        cut.Markup.Should().Contain("bi-arrow-up");
        cut.Markup.Should().Contain("bi-arrow-down");
    }

    [Fact(DisplayName = "SuggestionManager should move suggestion up")]
    public async Task SuggestionManager_ShouldMoveSuggestionUp()
    {
        // Arrange
        var suggestions = CreateTestSuggestions(3);
        _mockCatalogService.Setup(x => x.GetSugerenciasAsync(It.IsAny<string>()))
            .ReturnsAsync(suggestions);
        _mockCatalogService.Setup(x => x.ReorderSugerenciasAsync(It.IsAny<string>(), It.IsAny<int[]>(), It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        var cut = Render<SuggestionManager>();

        // Act - Click move up on second item (index 1)
        var upButtons = cut.FindAll("button").Where(b => b.InnerHtml.Contains("bi-arrow-up")).ToList();
        // Second row's up button (first row's is disabled)
        await cut.InvokeAsync(() => upButtons[1].Click());

        // Assert
        _mockCatalogService.Verify(x => x.ReorderSugerenciasAsync(
            It.IsAny<string>(),
            It.IsAny<int[]>(),
            It.IsAny<Guid>()), Times.Once);
    }

    [Fact(DisplayName = "SuggestionManager should disable move up on first item")]
    public void SuggestionManager_ShouldDisableMoveUpOnFirstItem()
    {
        // Arrange
        var suggestions = CreateTestSuggestions(3);
        _mockCatalogService.Setup(x => x.GetSugerenciasAsync(It.IsAny<string>()))
            .ReturnsAsync(suggestions);

        // Act
        var cut = Render<SuggestionManager>();

        // Assert - First up button should be disabled
        var upButtons = cut.FindAll("button").Where(b => b.InnerHtml.Contains("bi-arrow-up")).ToList();
        upButtons[0].HasAttribute("disabled").Should().BeTrue();
    }

    #endregion

    #region Validation Tests

    [Fact(DisplayName = "SuggestionManager should show validation error for empty value")]
    public async Task SuggestionManager_ShouldShowValidationErrorForEmptyValue()
    {
        // Arrange
        var cut = Render<SuggestionManager>();

        // Open modal
        var newButton = cut.FindAll("button").First(b => b.TextContent.Contains("Nueva Sugerencia"));
        await cut.InvokeAsync(() => newButton.Click());

        // Don't fill the value

        // Act - Try to save
        var saveButton = cut.FindAll("button").First(b => b.TextContent == "Guardar");
        await cut.InvokeAsync(() => saveButton.Click());

        // Assert
        cut.Markup.Should().Contain("El valor es requerido");
    }

    #endregion

    #region Error Handling Tests

    [Fact(DisplayName = "SuggestionManager should show error on load failure")]
    public void SuggestionManager_ShouldShowErrorOnLoadFailure()
    {
        // Arrange
        _mockCatalogService.Setup(x => x.GetSugerenciasAsync(It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Error de prueba"));

        // Act
        var cut = Render<SuggestionManager>();

        // Assert
        cut.Markup.Should().Contain("Error al cargar las sugerencias");
    }

    #endregion

    #region Helpers

    private void SetupDefaultMocks()
    {
        _mockCatalogService.Setup(x => x.GetSugerenciasAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<SugerenciaDto>());
    }

    private static List<SugerenciaDto> CreateTestSuggestions(int count)
    {
        return Enumerable.Range(1, count).Select(i => new SugerenciaDto
        {
            Id = i,
            Campo = "Sexo",
            Valor = $"Sugerencia {i}",
            Orden = i,
            Activo = true
        }).ToList();
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
