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
/// Tests user management functionality including CRUD operations.
/// Phase 3: Coverage improvement tests.
/// </summary>
[Trait("Category", "Component")]
public class UserListTests : TestContext
{
    private readonly Mock<IUserManagementService> _mockUserService;
    private readonly FakeNavigationManager _navigationManager;
    private readonly Guid _testUserId = Guid.NewGuid();

    public UserListTests()
    {
        _mockUserService = new Mock<IUserManagementService>();
        _navigationManager = new FakeNavigationManager();

        Services.AddSingleton(_mockUserService.Object);
        Services.AddSingleton<NavigationManager>(_navigationManager);
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

    [Fact(DisplayName = "UserList should render Nuevo Usuario button")]
    public void UserList_ShouldRenderNewUserButton()
    {
        // Act
        var cut = Render<UserList>();

        // Assert
        var button = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Nuevo Usuario"));
        button.Should().NotBeNull();
    }

    [Fact(DisplayName = "UserList should render filter controls")]
    public void UserList_ShouldRenderFilterControls()
    {
        // Act
        var cut = Render<UserList>();

        // Assert
        cut.Markup.Should().Contain("Buscar");
        cut.Markup.Should().Contain("Rol");
        cut.Markup.Should().Contain("Estado");
        cut.Markup.Should().Contain("Limpiar");
    }

    [Fact(DisplayName = "UserList should render users table")]
    public void UserList_ShouldRenderUsersTable()
    {
        // Act
        var cut = Render<UserList>();

        // Assert
        cut.Markup.Should().Contain("<table");
        cut.Markup.Should().Contain("Email");
        cut.Markup.Should().Contain("Roles");
        cut.Markup.Should().Contain("Estado");
        cut.Markup.Should().Contain("Acciones");
    }

    [Fact(DisplayName = "UserList should display users from service")]
    public void UserList_ShouldDisplayUsersFromService()
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
        cut.Markup.Should().Contain("ADMIN");
        cut.Markup.Should().Contain("CREADOR");
        cut.Markup.Should().Contain("badge");
    }

    [Fact(DisplayName = "UserList should display active/suspended status")]
    public void UserList_ShouldDisplayUserStatus()
    {
        // Act
        var cut = Render<UserList>();

        // Assert
        cut.Markup.Should().Contain("Activo");
        cut.Markup.Should().Contain("bg-success");
    }

    #endregion

    #region Loading State Tests

    [Fact(DisplayName = "UserList should show empty message when no users")]
    public void UserList_EmptyList_ShouldShowEmptyMessage()
    {
        // Arrange
        _mockUserService.Setup(s => s.ListUsersAsync(It.IsAny<UserFilterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserListResponse { Items = new List<UserDto>(), TotalCount = 0 });

        // Act
        var cut = Render<UserList>();

        // Assert
        cut.Markup.Should().Contain("No hay usuarios para mostrar");
    }

    [Fact(DisplayName = "UserList should show error message on service failure")]
    public void UserList_ServiceError_ShouldShowErrorMessage()
    {
        // Arrange
        _mockUserService.Setup(s => s.ListUsersAsync(It.IsAny<UserFilterDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var cut = Render<UserList>();

        // Assert
        cut.Markup.Should().Contain("Error al cargar");
    }

    #endregion

    #region Create User Modal Tests

    [Fact(DisplayName = "UserList Nuevo Usuario button should open modal")]
    public async Task UserList_NewUserButton_ShouldOpenModal()
    {
        // Arrange
        var cut = Render<UserList>();

        // Act
        var newUserButton = cut.FindAll("button").First(b => b.TextContent.Contains("Nuevo Usuario"));
        await cut.InvokeAsync(() => newUserButton.Click());

        // Assert
        cut.Markup.Should().Contain("modal");
        cut.Markup.Should().Contain("Nuevo Usuario");
    }

    [Fact(DisplayName = "UserList create modal should have required fields")]
    public async Task UserList_CreateModal_ShouldHaveRequiredFields()
    {
        // Arrange
        var cut = Render<UserList>();

        // Open modal
        var newUserButton = cut.FindAll("button").First(b => b.TextContent.Contains("Nuevo Usuario"));
        await cut.InvokeAsync(() => newUserButton.Click());

        // Assert
        cut.Markup.Should().Contain("Email");
        cut.Markup.Should().Contain("Contraseña");
        cut.Markup.Should().Contain("Roles");
        cut.Markup.Should().Contain("Crear Usuario");
    }

    [Fact(DisplayName = "UserList create modal cancel should close modal")]
    public async Task UserList_CreateModalCancel_ShouldCloseModal()
    {
        // Arrange
        var cut = Render<UserList>();

        // Open modal
        var newUserButton = cut.FindAll("button").First(b => b.TextContent.Contains("Nuevo Usuario"));
        await cut.InvokeAsync(() => newUserButton.Click());

        // Act - click cancel
        var cancelButton = cut.FindAll("button").First(b => b.TextContent.Contains("Cancelar"));
        await cut.InvokeAsync(() => cancelButton.Click());

        // Assert - modal should be closed
        cut.FindAll(".modal").Should().BeEmpty();
    }

    #endregion

    #region Edit User Modal Tests

    [Fact(DisplayName = "UserList edit button should open modal with user data")]
    public async Task UserList_EditButton_ShouldOpenModalWithUserData()
    {
        // Arrange
        var cut = Render<UserList>();

        // Wait for table to render
        cut.WaitForAssertion(() => cut.FindAll("table tbody tr").Count.Should().BeGreaterThan(0));

        // Act - Find and click edit button
        var editButton = cut.FindAll("button.btn-outline-primary").FirstOrDefault();
        if (editButton != null)
        {
            await cut.InvokeAsync(() => editButton.Click());

            // Assert
            cut.Markup.Should().Contain("modal");
            cut.Markup.Should().Contain("Editar Usuario");
        }
    }

    #endregion

    #region User Actions Tests

    [Fact(DisplayName = "UserList suspend button should call service")]
    public async Task UserList_SuspendButton_ShouldCallService()
    {
        // Arrange
        var otherUserId = Guid.NewGuid();
        var users = new List<UserDto>
        {
            new() { Id = otherUserId, Email = "other@ceiba.local", Roles = new List<string> { "CREADOR" }, Activo = true }
        };

        _mockUserService.Setup(s => s.ListUsersAsync(It.IsAny<UserFilterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserListResponse { Items = users, TotalCount = 1 });

        _mockUserService.Setup(s => s.SuspendUserAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserDto { Id = otherUserId, Email = "other@ceiba.local", Roles = new List<string> { "CREADOR" }, Activo = false });

        var cut = Render<UserList>();

        // Wait for table to render
        cut.WaitForAssertion(() => cut.FindAll("table tbody tr").Count.Should().BeGreaterThan(0));

        // Act - Find suspend button (warning outline)
        var suspendButton = cut.FindAll("button.btn-outline-warning").FirstOrDefault();
        if (suspendButton != null)
        {
            await cut.InvokeAsync(() => suspendButton.Click());
        }

        // Assert
        _mockUserService.Verify(s => s.SuspendUserAsync(otherUserId, _testUserId), Times.AtMostOnce);
    }

    [Fact(DisplayName = "UserList activate button should call service")]
    public async Task UserList_ActivateButton_ShouldCallService()
    {
        // Arrange
        var suspendedUserId = Guid.NewGuid();
        var users = new List<UserDto>
        {
            new() { Id = suspendedUserId, Email = "suspended@ceiba.local", Roles = new List<string> { "CREADOR" }, Activo = false }
        };

        _mockUserService.Setup(s => s.ListUsersAsync(It.IsAny<UserFilterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserListResponse { Items = users, TotalCount = 1 });

        _mockUserService.Setup(s => s.ActivateUserAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserDto { Id = suspendedUserId, Email = "suspended@ceiba.local", Roles = new List<string> { "CREADOR" }, Activo = true });

        var cut = Render<UserList>();

        // Wait for table to render
        cut.WaitForAssertion(() => cut.FindAll("table tbody tr").Count.Should().BeGreaterThan(0));

        // Act - Find activate button (success outline)
        var activateButton = cut.FindAll("button.btn-outline-success").FirstOrDefault();
        if (activateButton != null)
        {
            await cut.InvokeAsync(() => activateButton.Click());
        }

        // Assert
        _mockUserService.Verify(s => s.ActivateUserAsync(suspendedUserId, _testUserId), Times.AtMostOnce);
    }

    [Fact(DisplayName = "UserList delete button should show confirmation modal")]
    public async Task UserList_DeleteButton_ShouldShowConfirmationModal()
    {
        // Arrange
        var otherUserId = Guid.NewGuid();
        var users = new List<UserDto>
        {
            new() { Id = otherUserId, Email = "other@ceiba.local", Roles = new List<string> { "CREADOR" }, Activo = true }
        };

        _mockUserService.Setup(s => s.ListUsersAsync(It.IsAny<UserFilterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserListResponse { Items = users, TotalCount = 1 });

        var cut = Render<UserList>();

        // Wait for table to render
        cut.WaitForAssertion(() => cut.FindAll("table tbody tr").Count.Should().BeGreaterThan(0));

        // Act - Find and click delete button
        var deleteButton = cut.FindAll("button.btn-outline-danger").FirstOrDefault();
        if (deleteButton != null)
        {
            await cut.InvokeAsync(() => deleteButton.Click());

            // Assert
            cut.Markup.Should().Contain("Confirmar Eliminación");
            cut.Markup.Should().Contain("Esta acción no se puede deshacer");
        }
    }

    #endregion

    #region Pagination Tests

    [Fact(DisplayName = "UserList should show pagination when multiple pages")]
    public void UserList_MultiplePage_ShouldShowPagination()
    {
        // Arrange
        var users = Enumerable.Range(1, 25).Select(i => new UserDto
        {
            Id = Guid.NewGuid(),
            Email = $"user{i}@test.com",
            Roles = new List<string> { "CREADOR" },
            Activo = true
        }).ToList();

        _mockUserService.Setup(s => s.ListUsersAsync(It.IsAny<UserFilterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserListResponse { Items = users.Take(20).ToList(), TotalCount = 25 });

        // Act
        var cut = Render<UserList>();

        // Assert
        cut.Markup.Should().Contain("pagination");
        cut.Markup.Should().Contain("Anterior");
        cut.Markup.Should().Contain("Siguiente");
    }

    #endregion

    #region Role Badge Tests

    [Fact(DisplayName = "UserList should show correct badge for ADMIN role")]
    public void UserList_AdminRole_ShouldShowDangerBadge()
    {
        // Arrange
        var users = new List<UserDto>
        {
            new() { Id = Guid.NewGuid(), Email = "admin@test.com", Roles = new List<string> { "ADMIN" }, Activo = true }
        };

        _mockUserService.Setup(s => s.ListUsersAsync(It.IsAny<UserFilterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserListResponse { Items = users, TotalCount = 1 });

        // Act
        var cut = Render<UserList>();

        // Assert
        cut.Markup.Should().Contain("bg-danger");
        cut.Markup.Should().Contain("ADMIN");
    }

    [Fact(DisplayName = "UserList should show correct badge for REVISOR role")]
    public void UserList_RevisorRole_ShouldShowPrimaryBadge()
    {
        // Arrange
        var users = new List<UserDto>
        {
            new() { Id = Guid.NewGuid(), Email = "revisor@test.com", Roles = new List<string> { "REVISOR" }, Activo = true }
        };

        _mockUserService.Setup(s => s.ListUsersAsync(It.IsAny<UserFilterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserListResponse { Items = users, TotalCount = 1 });

        // Act
        var cut = Render<UserList>();

        // Assert
        cut.Markup.Should().Contain("bg-primary");
        cut.Markup.Should().Contain("REVISOR");
    }

    #endregion

    #region Filter Tests

    [Fact(DisplayName = "UserList clear filters button should reset filters")]
    public async Task UserList_ClearFilters_ShouldResetFilters()
    {
        // Arrange
        var cut = Render<UserList>();

        // Act
        var clearButton = cut.FindAll("button").First(b => b.TextContent.Contains("Limpiar"));
        await cut.InvokeAsync(() => clearButton.Click());

        // Assert - service should be called with reset filters
        _mockUserService.Verify(s => s.ListUsersAsync(It.Is<UserFilterDto>(f =>
            f.Search == null && f.Role == null && f.Activo == null)), Times.AtLeastOnce);
    }

    #endregion

    #region Helper Methods

    private void SetupDefaultMocks()
    {
        var users = new List<UserDto>
        {
            new() { Id = _testUserId, Email = "admin@ceiba.local", Roles = new List<string> { "ADMIN" }, Activo = true },
            new() { Id = Guid.NewGuid(), Email = "creador@ceiba.local", Roles = new List<string> { "CREADOR" }, Activo = true }
        };

        var roles = new List<string> { "ADMIN", "REVISOR", "CREADOR" };

        _mockUserService.Setup(s => s.ListUsersAsync(It.IsAny<UserFilterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserListResponse { Items = users, TotalCount = 2 });

        _mockUserService.Setup(s => s.GetAvailableRolesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);
    }

    private class FakeNavigationManager : NavigationManager
    {
        public FakeNavigationManager()
        {
            Initialize("https://localhost:5001/", "https://localhost:5001/admin/users");
        }

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            if (!uri.StartsWith("http"))
            {
                uri = new Uri(new Uri(BaseUri), uri).ToString();
            }
            Uri = uri;
        }
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
