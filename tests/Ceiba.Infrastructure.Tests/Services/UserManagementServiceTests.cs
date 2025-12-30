using Ceiba.Core.Entities;
using Ceiba.Infrastructure.Services;
using Ceiba.Shared.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MockQueryable;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace Ceiba.Infrastructure.Tests.Services;

/// <summary>
/// Unit tests for UserManagementService.
/// Tests user CRUD operations for ADMIN role.
/// US3: FR-021 to FR-026
/// </summary>
public class UserManagementServiceTests
{
    private readonly UserManager<Usuario> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ILogger<UserManagementService> _logger;
    private readonly UserManagementService _service;
    private readonly Guid _adminUserId = Guid.NewGuid();

    public UserManagementServiceTests()
    {
        // Create mock user store
        var userStore = Substitute.For<IUserStore<Usuario>>();
        userStore.As<IUserEmailStore<Usuario>>();
        userStore.As<IUserPasswordStore<Usuario>>();
        userStore.As<IUserLockoutStore<Usuario>>();
        userStore.As<IQueryableUserStore<Usuario>>();

        _userManager = Substitute.For<UserManager<Usuario>>(
            userStore,
            null, null, null, null, null, null, null, null);

        // Create mock role store
        var roleStore = Substitute.For<IRoleStore<IdentityRole<Guid>>>();
        roleStore.As<IQueryableRoleStore<IdentityRole<Guid>>>();

        _roleManager = Substitute.For<RoleManager<IdentityRole<Guid>>>(
            roleStore, null, null, null, null);

        _logger = Substitute.For<ILogger<UserManagementService>>();

        _service = new UserManagementService(
            _userManager,
            _roleManager,
            _logger);
    }

    #region ListUsersAsync Tests

    [Fact(DisplayName = "ListUsersAsync should return paginated users")]
    public async Task ListUsersAsync_ReturnsPaginatedUsers()
    {
        // Arrange
        var users = CreateTestUsers(10);
        SetupUserManagerUsers(users);
        SetupUserRoles(users);

        var filter = new UserFilterDto { Page = 1, PageSize = 5 };

        // Act
        var result = await _service.ListUsersAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(5);
        result.TotalCount.Should().Be(10);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(5);
    }

    [Fact(DisplayName = "ListUsersAsync should filter by search term")]
    public async Task ListUsersAsync_FiltersBySearch()
    {
        // Arrange
        var users = new List<Usuario>
        {
            CreateUser("john@example.com"),
            CreateUser("jane@example.com"),
            CreateUser("admin@test.com")
        };
        SetupUserManagerUsers(users);
        SetupUserRoles(users);

        var filter = new UserFilterDto { Search = "example", Page = 1, PageSize = 10 };

        // Act
        var result = await _service.ListUsersAsync(filter);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(u => u.Email.Contains("example"));
    }

    [Fact(DisplayName = "ListUsersAsync should filter by active status")]
    public async Task ListUsersAsync_FiltersByActiveStatus()
    {
        // Arrange
        var activeUser = CreateUser("active@example.com");
        var suspendedUser = CreateUser("suspended@example.com");
        suspendedUser.Activo = false; // User is suspended

        var users = new List<Usuario> { activeUser, suspendedUser };
        SetupUserManagerUsers(users);
        SetupUserRoles(users);

        var filter = new UserFilterDto { Activo = true, Page = 1, PageSize = 10 };

        // Act
        var result = await _service.ListUsersAsync(filter);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.Should().OnlyContain(u => u.Activo);
    }

    [Fact(DisplayName = "ListUsersAsync should filter by role")]
    public async Task ListUsersAsync_FiltersByRole()
    {
        // Arrange
        var adminUser = CreateUser("admin@example.com");
        var creadorUser = CreateUser("creador@example.com");

        var users = new List<Usuario> { adminUser, creadorUser };
        SetupUserManagerUsers(users);

        _userManager.GetRolesAsync(adminUser).Returns(new List<string> { "ADMIN" });
        _userManager.GetRolesAsync(creadorUser).Returns(new List<string> { "CREADOR" });

        var filter = new UserFilterDto { Role = "ADMIN", Page = 1, PageSize = 10 };

        // Act
        var result = await _service.ListUsersAsync(filter);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().Roles.Should().Contain("ADMIN");
    }

    [Fact(DisplayName = "ListUsersAsync should order by email")]
    public async Task ListUsersAsync_OrdersByEmail()
    {
        // Arrange
        var users = new List<Usuario>
        {
            CreateUser("charlie@example.com"),
            CreateUser("alice@example.com"),
            CreateUser("bob@example.com")
        };
        SetupUserManagerUsers(users);
        SetupUserRoles(users);

        var filter = new UserFilterDto { Page = 1, PageSize = 10 };

        // Act
        var result = await _service.ListUsersAsync(filter);

        // Assert
        result.Items.Should().BeInAscendingOrder(u => u.Email);
    }

    #endregion

    #region GetUserByIdAsync Tests

    [Fact(DisplayName = "GetUserByIdAsync should return user when found")]
    public async Task GetUserByIdAsync_ReturnsUser_WhenFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUser("test@example.com", userId);

        _userManager.FindByIdAsync(userId.ToString()).Returns(user);
        _userManager.GetRolesAsync(user).Returns(new List<string> { "CREADOR" });

        // Act
        var result = await _service.GetUserByIdAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(userId);
        result.Email.Should().Be("test@example.com");
        result.Roles.Should().Contain("CREADOR");
    }

    [Fact(DisplayName = "GetUserByIdAsync should return null when not found")]
    public async Task GetUserByIdAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userManager.FindByIdAsync(userId.ToString()).ReturnsNull();

        // Act
        var result = await _service.GetUserByIdAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CreateUserAsync Tests

    [Fact(DisplayName = "CreateUserAsync should create user with roles")]
    public async Task CreateUserAsync_CreatesUserWithRoles()
    {
        // Arrange
        var createDto = new CreateUserDto
        {
            Email = "newuser@example.com",
            Password = "StrongPassword123!",
            Roles = new List<string> { "CREADOR" }
        };

        _userManager.FindByEmailAsync(createDto.Email).ReturnsNull();
        _userManager.CreateAsync(Arg.Any<Usuario>(), createDto.Password)
            .Returns(IdentityResult.Success);
        _roleManager.RoleExistsAsync("CREADOR").Returns(true);
        _userManager.AddToRoleAsync(Arg.Any<Usuario>(), "CREADOR")
            .Returns(IdentityResult.Success);
        _userManager.GetRolesAsync(Arg.Any<Usuario>())
            .Returns(new List<string> { "CREADOR" });

        // Act
        var result = await _service.CreateUserAsync(createDto, _adminUserId);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be(createDto.Email);
        result.Roles.Should().Contain("CREADOR");
    }

    [Fact(DisplayName = "CreateUserAsync should throw when email in use")]
    public async Task CreateUserAsync_ThrowsWhenEmailInUse()
    {
        // Arrange
        var existingUser = CreateUser("existing@example.com");
        var createDto = new CreateUserDto
        {
            Email = "existing@example.com",
            Password = "Password123!",
            Roles = new List<string> { "CREADOR" }
        };

        _userManager.FindByEmailAsync(createDto.Email).Returns(existingUser);

        // Act & Assert
        var act = () => _service.CreateUserAsync(createDto, _adminUserId);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ya está en uso*");
    }

    [Fact(DisplayName = "CreateUserAsync should throw on Identity failure")]
    public async Task CreateUserAsync_ThrowsOnIdentityFailure()
    {
        // Arrange
        var createDto = new CreateUserDto
        {
            Email = "newuser@example.com",
            Password = "weak",
            Roles = new List<string> { "CREADOR" }
        };

        _userManager.FindByEmailAsync(createDto.Email).ReturnsNull();
        _userManager.CreateAsync(Arg.Any<Usuario>(), createDto.Password)
            .Returns(IdentityResult.Failed(new IdentityError
            {
                Code = "PasswordTooWeak",
                Description = "Password is too weak"
            }));

        // Act & Assert
        var act = () => _service.CreateUserAsync(createDto, _adminUserId);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Password is too weak*");
    }

    [Fact(DisplayName = "CreateUserAsync should skip non-existent roles")]
    public async Task CreateUserAsync_SkipsNonExistentRoles()
    {
        // Arrange
        var createDto = new CreateUserDto
        {
            Email = "newuser@example.com",
            Password = "StrongPassword123!",
            Roles = new List<string> { "CREADOR", "NONEXISTENT" }
        };

        _userManager.FindByEmailAsync(createDto.Email).ReturnsNull();
        _userManager.CreateAsync(Arg.Any<Usuario>(), createDto.Password)
            .Returns(IdentityResult.Success);
        _roleManager.RoleExistsAsync("CREADOR").Returns(true);
        _roleManager.RoleExistsAsync("NONEXISTENT").Returns(false);
        _userManager.AddToRoleAsync(Arg.Any<Usuario>(), "CREADOR")
            .Returns(IdentityResult.Success);
        _userManager.GetRolesAsync(Arg.Any<Usuario>())
            .Returns(new List<string> { "CREADOR" });

        // Act
        var result = await _service.CreateUserAsync(createDto, _adminUserId);

        // Assert
        await _userManager.Received(1).AddToRoleAsync(Arg.Any<Usuario>(), "CREADOR");
        await _userManager.DidNotReceive().AddToRoleAsync(Arg.Any<Usuario>(), "NONEXISTENT");
    }

    #endregion

    #region UpdateUserAsync Tests

    [Fact(DisplayName = "UpdateUserAsync should update user details")]
    public async Task UpdateUserAsync_UpdatesUserDetails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUser("old@example.com", userId);
        var updateDto = new UpdateUserDto
        {
            Email = "new@example.com",
            Activo = true,
            Roles = new List<string> { "REVISOR" }
        };

        _userManager.FindByIdAsync(userId.ToString()).Returns(user);
        _userManager.FindByEmailAsync(updateDto.Email).ReturnsNull();
        _userManager.UpdateAsync(user).Returns(IdentityResult.Success);
        _userManager.GetRolesAsync(user).Returns(
            new List<string> { "CREADOR" },
            new List<string> { "REVISOR" });
        _userManager.RemoveFromRolesAsync(user, Arg.Any<IEnumerable<string>>())
            .Returns(IdentityResult.Success);
        _roleManager.RoleExistsAsync("REVISOR").Returns(true);
        _userManager.AddToRoleAsync(user, "REVISOR").Returns(IdentityResult.Success);

        // Act
        var result = await _service.UpdateUserAsync(userId, updateDto, _adminUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("new@example.com");
    }

    [Fact(DisplayName = "UpdateUserAsync should throw when user not found")]
    public async Task UpdateUserAsync_ThrowsWhenNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateDto = new UpdateUserDto
        {
            Email = "test@example.com",
            Activo = true,
            Roles = new List<string>()
        };

        _userManager.FindByIdAsync(userId.ToString()).ReturnsNull();

        // Act & Assert
        var act = () => _service.UpdateUserAsync(userId, updateDto, _adminUserId);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact(DisplayName = "UpdateUserAsync should update password when provided")]
    public async Task UpdateUserAsync_UpdatesPassword_WhenProvided()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUser("test@example.com", userId);
        var updateDto = new UpdateUserDto
        {
            Email = "test@example.com",
            NewPassword = "NewPassword123!",
            Activo = true,
            Roles = new List<string> { "CREADOR" }
        };

        _userManager.FindByIdAsync(userId.ToString()).Returns(user);
        _userManager.GeneratePasswordResetTokenAsync(user).Returns("reset-token");
        _userManager.ResetPasswordAsync(user, "reset-token", updateDto.NewPassword)
            .Returns(IdentityResult.Success);
        _userManager.UpdateAsync(user).Returns(IdentityResult.Success);
        _userManager.GetRolesAsync(user).Returns(new List<string> { "CREADOR" });

        // Act
        var result = await _service.UpdateUserAsync(userId, updateDto, _adminUserId);

        // Assert
        await _userManager.Received(1).ResetPasswordAsync(user, "reset-token", updateDto.NewPassword);
    }

    [Fact(DisplayName = "UpdateUserAsync should suspend user when Activo is false")]
    public async Task UpdateUserAsync_SuspendsUser_WhenActivoIsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUser("test@example.com", userId);
        user.LockoutEnd = null; // User is currently active

        var updateDto = new UpdateUserDto
        {
            Email = "test@example.com",
            Activo = false,
            Roles = new List<string> { "CREADOR" }
        };

        _userManager.FindByIdAsync(userId.ToString()).Returns(user);
        _userManager.UpdateAsync(user).Returns(IdentityResult.Success);
        _userManager.GetRolesAsync(user).Returns(new List<string> { "CREADOR" });

        // Act
        await _service.UpdateUserAsync(userId, updateDto, _adminUserId);

        // Assert
        user.Activo.Should().BeFalse();
    }

    #endregion

    #region SuspendUserAsync Tests

    [Fact(DisplayName = "SuspendUserAsync should lock out user")]
    public async Task SuspendUserAsync_LocksOutUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUser("test@example.com", userId);

        _userManager.FindByIdAsync(userId.ToString()).Returns(user);
        _userManager.UpdateAsync(user).Returns(IdentityResult.Success);
        _userManager.GetRolesAsync(user).Returns(new List<string> { "CREADOR" });

        // Act
        var result = await _service.SuspendUserAsync(userId, _adminUserId);

        // Assert
        result.Should().NotBeNull();
        user.Activo.Should().BeFalse();
    }

    [Fact(DisplayName = "SuspendUserAsync should throw when suspending self")]
    public async Task SuspendUserAsync_ThrowsWhenSuspendingSelf()
    {
        // Arrange
        var user = CreateUser("admin@example.com", _adminUserId);
        _userManager.FindByIdAsync(_adminUserId.ToString()).Returns(user);

        // Act & Assert
        var act = () => _service.SuspendUserAsync(_adminUserId, _adminUserId);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*suspender su propia cuenta*");
    }

    [Fact(DisplayName = "SuspendUserAsync should throw when user not found")]
    public async Task SuspendUserAsync_ThrowsWhenNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userManager.FindByIdAsync(userId.ToString()).ReturnsNull();

        // Act & Assert
        var act = () => _service.SuspendUserAsync(userId, _adminUserId);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    #endregion

    #region ActivateUserAsync Tests

    [Fact(DisplayName = "ActivateUserAsync should activate user")]
    public async Task ActivateUserAsync_ActivatesUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUser("test@example.com", userId);
        user.Activo = false; // User is currently suspended

        _userManager.FindByIdAsync(userId.ToString()).Returns(user);
        _userManager.UpdateAsync(user).Returns(IdentityResult.Success);
        _userManager.GetRolesAsync(user).Returns(new List<string> { "CREADOR" });

        // Act
        var result = await _service.ActivateUserAsync(userId, _adminUserId);

        // Assert
        result.Should().NotBeNull();
        user.Activo.Should().BeTrue();
        result.Activo.Should().BeTrue();
    }

    [Fact(DisplayName = "ActivateUserAsync should throw when user not found")]
    public async Task ActivateUserAsync_ThrowsWhenNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userManager.FindByIdAsync(userId.ToString()).ReturnsNull();

        // Act & Assert
        var act = () => _service.ActivateUserAsync(userId, _adminUserId);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    #endregion

    #region DeleteUserAsync Tests

    [Fact(DisplayName = "DeleteUserAsync should soft delete user")]
    public async Task DeleteUserAsync_SoftDeletesUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUser("test@example.com", userId);

        _userManager.FindByIdAsync(userId.ToString()).Returns(user);
        _userManager.UpdateAsync(user).Returns(IdentityResult.Success);

        // Act
        await _service.DeleteUserAsync(userId, _adminUserId);

        // Assert
        user.Activo.Should().BeFalse();
        user.Email.Should().StartWith("DELETED_");
    }

    [Fact(DisplayName = "DeleteUserAsync should throw when deleting self")]
    public async Task DeleteUserAsync_ThrowsWhenDeletingSelf()
    {
        // Arrange
        var user = CreateUser("admin@example.com", _adminUserId);
        _userManager.FindByIdAsync(_adminUserId.ToString()).Returns(user);

        // Act & Assert
        var act = () => _service.DeleteUserAsync(_adminUserId, _adminUserId);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*eliminar su propia cuenta*");
    }

    [Fact(DisplayName = "DeleteUserAsync should throw when user not found")]
    public async Task DeleteUserAsync_ThrowsWhenNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userManager.FindByIdAsync(userId.ToString()).ReturnsNull();

        // Act & Assert
        var act = () => _service.DeleteUserAsync(userId, _adminUserId);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    #endregion

    #region GetAvailableRolesAsync Tests

    [Fact(DisplayName = "GetAvailableRolesAsync should return all roles")]
    public async Task GetAvailableRolesAsync_ReturnsAllRoles()
    {
        // Arrange
        var roles = new List<IdentityRole<Guid>>
        {
            new() { Id = Guid.NewGuid(), Name = "ADMIN" },
            new() { Id = Guid.NewGuid(), Name = "REVISOR" },
            new() { Id = Guid.NewGuid(), Name = "CREADOR" }
        };

        var mockRoles = roles.BuildMock();
        _roleManager.Roles.Returns(mockRoles);

        // Act
        var result = await _service.GetAvailableRolesAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain("ADMIN");
        result.Should().Contain("REVISOR");
        result.Should().Contain("CREADOR");
    }

    #endregion

    #region IsEmailInUseAsync Tests

    [Fact(DisplayName = "IsEmailInUseAsync should return true when email exists")]
    public async Task IsEmailInUseAsync_ReturnsTrue_WhenEmailExists()
    {
        // Arrange
        var existingUser = CreateUser("existing@example.com");
        _userManager.FindByEmailAsync("existing@example.com").Returns(existingUser);

        // Act
        var result = await _service.IsEmailInUseAsync("existing@example.com");

        // Assert
        result.Should().BeTrue();
    }

    [Fact(DisplayName = "IsEmailInUseAsync should return false when email not found")]
    public async Task IsEmailInUseAsync_ReturnsFalse_WhenEmailNotFound()
    {
        // Arrange
        _userManager.FindByEmailAsync("new@example.com").ReturnsNull();

        // Act
        var result = await _service.IsEmailInUseAsync("new@example.com");

        // Assert
        result.Should().BeFalse();
    }

    [Fact(DisplayName = "IsEmailInUseAsync should return false when email belongs to excluded user")]
    public async Task IsEmailInUseAsync_ReturnsFalse_WhenEmailBelongsToExcludedUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingUser = CreateUser("existing@example.com", userId);
        _userManager.FindByEmailAsync("existing@example.com").Returns(existingUser);

        // Act
        var result = await _service.IsEmailInUseAsync("existing@example.com", userId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    private static Usuario CreateUser(string email, Guid? id = null)
    {
        return new Usuario
        {
            Id = id ?? Guid.NewGuid(),
            Email = email,
            UserName = email,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = email.ToUpperInvariant(),
            EmailConfirmed = true,
            Nombre = email.Split('@')[0],
            CreatedAt = DateTime.UtcNow
        };
    }

    private static List<Usuario> CreateTestUsers(int count)
    {
        var users = new List<Usuario>();
        for (int i = 0; i < count; i++)
        {
            users.Add(CreateUser($"user{i}@example.com"));
        }
        return users;
    }

    private void SetupUserManagerUsers(List<Usuario> users)
    {
        var mockUsers = users.BuildMock();
        _userManager.Users.Returns(mockUsers);
    }

    private void SetupUserRoles(List<Usuario> users)
    {
        foreach (var user in users)
        {
            _userManager.GetRolesAsync(user).Returns(new List<string> { "CREADOR" });
        }
    }

    #endregion
}
