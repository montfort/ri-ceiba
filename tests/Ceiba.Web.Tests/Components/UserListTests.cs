using Bunit;
using Ceiba.Core.Interfaces;
using Ceiba.Shared.DTOs;
using Ceiba.Web.Components.Pages.Admin;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Security.Claims;

namespace Ceiba.Web.Tests.Components;

/// <summary>
/// Component tests for UserList Blazor component.
/// Tests user management CRUD operations.
/// </summary>
public class UserListTests : TestContext
{
    private readonly Mock<IUserManagementService> _mockUserService;
    private readonly Guid _testUserId = Guid.NewGuid();

    public UserListTests()
    {
        _mockUserService = new Mock<IUserManagementService>();

        Services.AddSingleton(_mockUserService.Object);
        Services.AddSingleton<NavigationManager>(new FakeNavigationManager());
        Services.AddSingleton<AuthenticationStateProvider>(new TestAuthStateProvider(_testUserId, "ADMIN"));
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        SetupDefaultMocks();
    }

    #region Rendering Tests

    [Fact(DisplayName = "UserList should render page title")]
    public void UserList_ShouldRenderPageTitle()
    {
        // Act
        var cut = Render<UserList>();

        // Assert
        cut.Markup.Should().Contain("Gestión de Usuarios");
    }

    [Fact(DisplayName = "UserList should render users in table")]
    public void UserList_ShouldRenderUsersInTable()
    {
        // Act
        var cut = Render<UserList>();

        // Assert
        cut.Markup.Should().Contain("admin@ceiba.local");
        cut.Markup.Should().Contain("creador@ceiba.local");
    }

    [Fact(DisplayName = "UserList should display role badges")]
    public void UserList_ShouldDisplayRoleBadges()
    {
        // Act
        var cut = Render<UserList>();

        // Assert
        var adminBadge = cut.FindAll(".badge.bg-danger").FirstOrDefault();
        adminBadge.Should().NotBeNull();
        adminBadge!.TextContent.Should().Contain("ADMIN");
    }

    [Fact(DisplayName = "UserList should display active/suspended status")]
    public void UserList_ShouldDisplayActiveStatus()
    {
        // Act
        var cut = Render<UserList>();

        // Assert
        var activeBadge = cut.FindAll(".badge.bg-success").FirstOrDefault();
        activeBadge.Should().NotBeNull();
        activeBadge!.TextContent.Should().Contain("Activo");
    }

    [Fact(DisplayName = "UserList should display Nuevo Usuario button")]
    public void UserList_ShouldDisplayNewUserButton()
    {
        // Act
        var cut = Render<UserList>();

        // Assert
        var newUserButton = cut.FindAll("button.btn-primary").FirstOrDefault(b => b.TextContent.Contains("Nuevo Usuario"));
        newUserButton.Should().NotBeNull();
    }

    #endregion

    #region Filter Tests

    [Fact(DisplayName = "UserList should render filter controls")]
    public void UserList_ShouldRenderFilterControls()
    {
        // Act
        var cut = Render<UserList>();

        // Assert
        cut.Find("#filterSearch").Should().NotBeNull();
        cut.Find("#filterRole").Should().NotBeNull();
        cut.Find("#filterActivo").Should().NotBeNull();
    }

    [Fact(DisplayName = "UserList should filter by role")]
    public async Task UserList_ShouldFilterByRole()
    {
        // Arrange
        var cut = Render<UserList>();

        // Act
        var roleFilter = cut.Find("#filterRole");
        await cut.InvokeAsync(() => roleFilter.Change("CREADOR"));

        // Assert
        _mockUserService.Verify(
            s => s.ListUsersAsync(It.Is<UserFilterDto>(f => f.Role == "CREADOR")),
            Times.AtLeastOnce);
    }

    [Fact(DisplayName = "UserList should filter by active status")]
    public async Task UserList_ShouldFilterByActiveStatus()
    {
        // Arrange
        var cut = Render<UserList>();

        // Act
        var activoFilter = cut.Find("#filterActivo");
        await cut.InvokeAsync(() => activoFilter.Change("true"));

        // Assert
        _mockUserService.Verify(
            s => s.ListUsersAsync(It.Is<UserFilterDto>(f => f.Activo == true)),
            Times.AtLeastOnce);
    }

    [Fact(DisplayName = "UserList should clear filters when Limpiar clicked")]
    public async Task UserList_ShouldClearFilters()
    {
        // Arrange
        var cut = Render<UserList>();

        // Set a filter first
        var roleFilter = cut.Find("#filterRole");
        await cut.InvokeAsync(() => roleFilter.Change("ADMIN"));

        // Act
        var clearButton = cut.FindAll("button").First(b => b.TextContent.Contains("Limpiar"));
        await cut.InvokeAsync(() => clearButton.Click());

        // Assert
        _mockUserService.Verify(
            s => s.ListUsersAsync(It.Is<UserFilterDto>(f => f.Role == null)),
            Times.AtLeastOnce);
    }

    #endregion

    #region Create User Tests

    [Fact(DisplayName = "UserList should open create modal when Nuevo Usuario clicked")]
    public async Task UserList_ShouldOpenCreateModal()
    {
        // Arrange
        var cut = Render<UserList>();

        // Act
        var newUserButton = cut.FindAll("button.btn-primary").First(b => b.TextContent.Contains("Nuevo Usuario"));
        await cut.InvokeAsync(() => newUserButton.Click());

        // Assert
        cut.Markup.Should().Contain("Nuevo Usuario");
        cut.Find("#email").Should().NotBeNull();
        cut.Find("#password").Should().NotBeNull();
    }

    [Fact(DisplayName = "UserList should show validation error when email is empty")]
    public async Task UserList_ShouldShowValidationErrorForEmptyEmail()
    {
        // Arrange
        var cut = Render<UserList>();

        // Open modal
        var newUserButton = cut.FindAll("button.btn-primary").First(b => b.TextContent.Contains("Nuevo Usuario"));
        await cut.InvokeAsync(() => newUserButton.Click());

        // Act - try to save without email
        var saveButton = cut.FindAll(".modal button.btn-primary").First();
        await cut.InvokeAsync(() => saveButton.Click());

        // Assert
        cut.Markup.Should().Contain("El email es requerido");
    }

    [Fact(DisplayName = "UserList should show validation error when password is empty for new user")]
    public async Task UserList_ShouldShowValidationErrorForEmptyPassword()
    {
        // Arrange
        var cut = Render<UserList>();

        // Open modal and fill email
        var newUserButton = cut.FindAll("button.btn-primary").First(b => b.TextContent.Contains("Nuevo Usuario"));
        await cut.InvokeAsync(() => newUserButton.Click());

        var emailInput = cut.Find("#email");
        await cut.InvokeAsync(() => emailInput.Change("test@ceiba.local"));

        // Act - try to save without password
        var saveButton = cut.FindAll(".modal button.btn-primary").First();
        await cut.InvokeAsync(() => saveButton.Click());

        // Assert
        cut.Markup.Should().Contain("La contraseña es requerida");
    }

    [Fact(DisplayName = "UserList should show validation error when no roles selected")]
    public async Task UserList_ShouldShowValidationErrorForNoRoles()
    {
        // Arrange
        var cut = Render<UserList>();

        // Open modal and fill email + password
        var newUserButton = cut.FindAll("button.btn-primary").First(b => b.TextContent.Contains("Nuevo Usuario"));
        await cut.InvokeAsync(() => newUserButton.Click());

        var emailInput = cut.Find("#email");
        await cut.InvokeAsync(() => emailInput.Change("test@ceiba.local"));

        var passwordInput = cut.Find("#password");
        await cut.InvokeAsync(() => passwordInput.Change("Test123!"));

        // Act - try to save without roles
        var saveButton = cut.FindAll(".modal button.btn-primary").First();
        await cut.InvokeAsync(() => saveButton.Click());

        // Assert
        cut.Markup.Should().Contain("Debe seleccionar al menos un rol");
    }

    [Fact(DisplayName = "UserList should call CreateUserAsync when form is valid")]
    public async Task UserList_ShouldCallCreateUserAsync()
    {
        // Arrange
        var cut = Render<UserList>();

        // Open modal
        var newUserButton = cut.FindAll("button.btn-primary").First(b => b.TextContent.Contains("Nuevo Usuario"));
        await cut.InvokeAsync(() => newUserButton.Click());

        // Fill form
        var emailInput = cut.Find("#email");
        await cut.InvokeAsync(() => emailInput.Change("newuser@ceiba.local"));

        var passwordInput = cut.Find("#password");
        await cut.InvokeAsync(() => passwordInput.Change("Test123!"));

        // Select CREADOR role
        var roleCheckbox = cut.Find("#role_CREADOR");
        await cut.InvokeAsync(() => roleCheckbox.Change(true));

        // Act
        var saveButton = cut.FindAll(".modal button.btn-primary").First();
        await cut.InvokeAsync(() => saveButton.Click());

        // Assert
        _mockUserService.Verify(
            s => s.CreateUserAsync(
                It.Is<CreateUserDto>(d => d.Email == "newuser@ceiba.local" && d.Roles.Contains("CREADOR")),
                It.IsAny<Guid>()),
            Times.Once);
    }

    #endregion

    #region Edit User Tests

    [Fact(DisplayName = "UserList should open edit modal when edit button clicked")]
    public async Task UserList_ShouldOpenEditModal()
    {
        // Arrange
        var cut = Render<UserList>();

        // Act
        var editButton = cut.FindAll("button.btn-outline-primary").First();
        await cut.InvokeAsync(() => editButton.Click());

        // Assert
        cut.Markup.Should().Contain("Editar Usuario");
    }

    [Fact(DisplayName = "UserList should populate form when editing")]
    public async Task UserList_ShouldPopulateFormWhenEditing()
    {
        // Arrange
        var cut = Render<UserList>();

        // Act
        var editButton = cut.FindAll("button.btn-outline-primary").First();
        await cut.InvokeAsync(() => editButton.Click());

        // Assert
        var emailInput = cut.Find("#email");
        emailInput.GetAttribute("value").Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Suspend/Activate User Tests

    [Fact(DisplayName = "UserList should call SuspendUserAsync when suspend clicked")]
    public async Task UserList_ShouldCallSuspendUserAsync()
    {
        // Arrange
        var cut = Render<UserList>();

        // Act - click suspend button (warning button)
        var suspendButton = cut.FindAll("button.btn-outline-warning").FirstOrDefault();
        if (suspendButton != null)
        {
            await cut.InvokeAsync(() => suspendButton.Click());

            // Assert
            _mockUserService.Verify(
                s => s.SuspendUserAsync(It.IsAny<Guid>(), It.IsAny<Guid>()),
                Times.Once);
        }
    }

    [Fact(DisplayName = "UserList should not show suspend button for current user")]
    public void UserList_ShouldNotShowSuspendForCurrentUser()
    {
        // Arrange - setup mock to return current user
        var currentUserList = new UserListResponse
        {
            Items = new List<UserDto>
            {
                new() { Id = _testUserId, Email = "current@ceiba.local", Roles = new List<string> { "ADMIN" }, Activo = true }
            },
            TotalCount = 1,
            Page = 1,
            PageSize = 20
        };

        _mockUserService.Setup(s => s.ListUsersAsync(It.IsAny<UserFilterDto>()))
            .ReturnsAsync(currentUserList);

        // Act
        var cut = Render<UserList>();

        // Assert - should not have suspend button for current user's row
        var suspendButtons = cut.FindAll("button.btn-outline-warning");
        suspendButtons.Should().BeEmpty();
    }

    #endregion

    #region Delete User Tests

    [Fact(DisplayName = "UserList should show delete confirmation when delete clicked")]
    public async Task UserList_ShouldShowDeleteConfirmation()
    {
        // Arrange
        var cut = Render<UserList>();

        // Act
        var deleteButton = cut.FindAll("button.btn-outline-danger").FirstOrDefault();
        if (deleteButton != null)
        {
            await cut.InvokeAsync(() => deleteButton.Click());

            // Assert
            cut.Markup.Should().Contain("Confirmar Eliminación");
        }
    }

    [Fact(DisplayName = "UserList should call DeleteUserAsync when confirmed")]
    public async Task UserList_ShouldCallDeleteUserAsync()
    {
        // Arrange
        var cut = Render<UserList>();

        // Click delete button
        var deleteButton = cut.FindAll("button.btn-outline-danger").FirstOrDefault();
        if (deleteButton != null)
        {
            await cut.InvokeAsync(() => deleteButton.Click());

            // Confirm delete
            var confirmButton = cut.FindAll(".modal button.btn-danger").First();
            await cut.InvokeAsync(() => confirmButton.Click());

            // Assert
            _mockUserService.Verify(
                s => s.DeleteUserAsync(It.IsAny<Guid>(), It.IsAny<Guid>()),
                Times.Once);
        }
    }

    #endregion

    #region Pagination Tests

    [Fact(DisplayName = "UserList should display pagination when multiple pages")]
    public void UserList_ShouldDisplayPagination()
    {
        // Arrange - setup mock with many users
        var manyUsers = new UserListResponse
        {
            Items = Enumerable.Range(1, 20).Select(i => new UserDto
            {
                Id = Guid.NewGuid(),
                Email = $"user{i}@ceiba.local",
                Roles = new List<string> { "CREADOR" },
                Activo = true
            }).ToList(),
            TotalCount = 50,
            Page = 1,
            PageSize = 20
        };

        _mockUserService.Setup(s => s.ListUsersAsync(It.IsAny<UserFilterDto>()))
            .ReturnsAsync(manyUsers);

        // Act
        var cut = Render<UserList>();

        // Assert
        cut.Markup.Should().Contain("Siguiente");
        cut.Markup.Should().Contain("Anterior");
    }

    #endregion

    #region Error Handling Tests

    [Fact(DisplayName = "UserList should display error when load fails")]
    public void UserList_ShouldDisplayErrorWhenLoadFails()
    {
        // Arrange
        _mockUserService.Setup(s => s.ListUsersAsync(It.IsAny<UserFilterDto>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var cut = Render<UserList>();

        // Assert
        cut.Markup.Should().Contain("Error al cargar");
    }

    [Fact(DisplayName = "UserList should display empty state when no users")]
    public void UserList_ShouldDisplayEmptyState()
    {
        // Arrange
        _mockUserService.Setup(s => s.ListUsersAsync(It.IsAny<UserFilterDto>()))
            .ReturnsAsync(new UserListResponse
            {
                Items = new List<UserDto>(),
                TotalCount = 0,
                Page = 1,
                PageSize = 20
            });

        // Act
        var cut = Render<UserList>();

        // Assert
        cut.Markup.Should().Contain("No hay usuarios");
    }

    #endregion

    #region Helper Methods

    private void SetupDefaultMocks()
    {
        var users = new UserListResponse
        {
            Items = new List<UserDto>
            {
                new() { Id = Guid.NewGuid(), Email = "admin@ceiba.local", Roles = new List<string> { "ADMIN" }, Activo = true },
                new() { Id = Guid.NewGuid(), Email = "creador@ceiba.local", Roles = new List<string> { "CREADOR" }, Activo = true },
                new() { Id = Guid.NewGuid(), Email = "revisor@ceiba.local", Roles = new List<string> { "REVISOR" }, Activo = false }
            },
            TotalCount = 3,
            Page = 1,
            PageSize = 20
        };

        var roles = new List<string> { "ADMIN", "REVISOR", "CREADOR" };

        _mockUserService.Setup(s => s.ListUsersAsync(It.IsAny<UserFilterDto>())).ReturnsAsync(users);
        _mockUserService.Setup(s => s.GetAvailableRolesAsync()).ReturnsAsync(roles);
        _mockUserService.Setup(s => s.CreateUserAsync(It.IsAny<CreateUserDto>(), It.IsAny<Guid>()))
            .ReturnsAsync(new UserDto { Id = Guid.NewGuid(), Email = "new@ceiba.local", Roles = new List<string> { "CREADOR" }, Activo = true });
    }

    private class FakeNavigationManager : NavigationManager
    {
        public FakeNavigationManager()
        {
            Initialize("https://localhost:5001/", "https://localhost:5001/admin/users");
        }

        protected override void NavigateToCore(string uri, bool forceLoad) { }
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
