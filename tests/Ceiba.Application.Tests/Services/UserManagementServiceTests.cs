using Ceiba.Core.Entities;
using Ceiba.Core.Interfaces;
using Ceiba.Infrastructure.Services;
using Ceiba.Shared.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Ceiba.Application.Tests.Services;

/// <summary>
/// Unit tests for UserManagementService (US3: T064)
/// FR-021 to FR-026: User management operations
/// </summary>
public class UserManagementServiceTests
{
    private readonly Mock<UserManager<Usuario>> _mockUserManager;
    private readonly Mock<RoleManager<IdentityRole<Guid>>> _mockRoleManager;
    private readonly Mock<ILogger<UserManagementService>> _mockLogger;
    private readonly UserManagementService _service;

    public UserManagementServiceTests()
    {
        // Setup UserManager mock
        var userStore = new Mock<IUserStore<Usuario>>();
        _mockUserManager = new Mock<UserManager<Usuario>>(
            userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        // Setup RoleManager mock
        var roleStore = new Mock<IRoleStore<IdentityRole<Guid>>>();
        _mockRoleManager = new Mock<RoleManager<IdentityRole<Guid>>>(
            roleStore.Object, null!, null!, null!, null!);

        _mockLogger = new Mock<ILogger<UserManagementService>>();

        _service = new UserManagementService(
            _mockUserManager.Object,
            _mockRoleManager.Object,
            _mockLogger.Object);
    }

    #region GetUserByIdAsync Tests

    [Fact(DisplayName = "T064: GetUserByIdAsync should return user when exists")]
    public async Task GetUserByIdAsync_UserExists_ReturnsUserDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new Usuario
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "test@example.com",
            Nombre = "Test User",
            CreatedAt = DateTime.UtcNow
        };
        var roles = new List<string> { "CREADOR" };

        _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        _mockUserManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(roles);

        // Act
        var result = await _service.GetUserByIdAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(userId);
        result.Email.Should().Be("test@example.com");
        result.Roles.Should().Contain("CREADOR");
    }

    [Fact(DisplayName = "T064: GetUserByIdAsync should return null when user not found")]
    public async Task GetUserByIdAsync_UserNotFound_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((Usuario?)null);

        // Act
        var result = await _service.GetUserByIdAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CreateUserAsync Tests

    [Fact(DisplayName = "T064: CreateUserAsync should create user with valid data")]
    public async Task CreateUserAsync_ValidData_CreatesUser()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var createDto = new CreateUserDto
        {
            Nombre = "New User",
            Email = "newuser@example.com",
            Password = "SecurePassword123!",
            Roles = new List<string> { "CREADOR" }
        };

        _mockUserManager.Setup(x => x.FindByEmailAsync(createDto.Email))
            .ReturnsAsync((Usuario?)null);
        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<Usuario>(), createDto.Password))
            .ReturnsAsync(IdentityResult.Success);
        _mockRoleManager.Setup(x => x.RoleExistsAsync("CREADOR"))
            .ReturnsAsync(true);
        _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<Usuario>(), "CREADOR"))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(x => x.GetRolesAsync(It.IsAny<Usuario>()))
            .ReturnsAsync(new List<string> { "CREADOR" });

        // Act
        var result = await _service.CreateUserAsync(createDto, adminId);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be(createDto.Email);
        result.Roles.Should().Contain("CREADOR");
        _mockUserManager.Verify(x => x.CreateAsync(It.IsAny<Usuario>(), createDto.Password), Times.Once);
    }

    [Fact(DisplayName = "T064: CreateUserAsync should throw when email already in use")]
    public async Task CreateUserAsync_EmailInUse_ThrowsInvalidOperationException()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var createDto = new CreateUserDto
        {
            Nombre = "New User",
            Email = "existing@example.com",
            Password = "SecurePassword123!",
            Roles = new List<string> { "CREADOR" }
        };

        var existingUser = new Usuario { Id = Guid.NewGuid(), Email = createDto.Email, Nombre = "Existing", CreatedAt = DateTime.UtcNow };
        _mockUserManager.Setup(x => x.FindByEmailAsync(createDto.Email))
            .ReturnsAsync(existingUser);

        // Act
        var act = async () => await _service.CreateUserAsync(createDto, adminId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{createDto.Email}*ya está en uso*");
    }

    [Fact(DisplayName = "T064: CreateUserAsync should throw when Identity fails")]
    public async Task CreateUserAsync_IdentityFails_ThrowsInvalidOperationException()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var createDto = new CreateUserDto
        {
            Nombre = "New User",
            Email = "newuser@example.com",
            Password = "weak",
            Roles = new List<string> { "CREADOR" }
        };

        _mockUserManager.Setup(x => x.FindByEmailAsync(createDto.Email))
            .ReturnsAsync((Usuario?)null);
        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<Usuario>(), createDto.Password))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password too weak" }));

        // Act
        var act = async () => await _service.CreateUserAsync(createDto, adminId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Error al crear usuario*Password too weak*");
    }

    [Fact(DisplayName = "T064: CreateUserAsync should skip non-existent roles")]
    public async Task CreateUserAsync_NonExistentRole_SkipsRole()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var createDto = new CreateUserDto
        {
            Nombre = "New User",
            Email = "newuser@example.com",
            Password = "SecurePassword123!",
            Roles = new List<string> { "CREADOR", "INVALID_ROLE" }
        };

        _mockUserManager.Setup(x => x.FindByEmailAsync(createDto.Email))
            .ReturnsAsync((Usuario?)null);
        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<Usuario>(), createDto.Password))
            .ReturnsAsync(IdentityResult.Success);
        _mockRoleManager.Setup(x => x.RoleExistsAsync("CREADOR"))
            .ReturnsAsync(true);
        _mockRoleManager.Setup(x => x.RoleExistsAsync("INVALID_ROLE"))
            .ReturnsAsync(false);
        _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<Usuario>(), "CREADOR"))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(x => x.GetRolesAsync(It.IsAny<Usuario>()))
            .ReturnsAsync(new List<string> { "CREADOR" });

        // Act
        var result = await _service.CreateUserAsync(createDto, adminId);

        // Assert
        result.Should().NotBeNull();
        _mockUserManager.Verify(x => x.AddToRoleAsync(It.IsAny<Usuario>(), "INVALID_ROLE"), Times.Never);
    }

    #endregion

    #region UpdateUserAsync Tests

    [Fact(DisplayName = "T064: UpdateUserAsync should update user data")]
    public async Task UpdateUserAsync_ValidData_UpdatesUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var user = new Usuario
        {
            Id = userId,
            Email = "original@example.com",
            UserName = "original@example.com",
            Nombre = "Original User",
            CreatedAt = DateTime.UtcNow
        };

        var updateDto = new UpdateUserDto
        {
            Nombre = "Updated Name",
            Email = "updated@example.com",
            Roles = new List<string> { "REVISOR" },
            Activo = true
        };

        _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        _mockUserManager.Setup(x => x.FindByEmailAsync(updateDto.Email))
            .ReturnsAsync((Usuario?)null);
        _mockUserManager.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "CREADOR" });
        _mockRoleManager.Setup(x => x.RoleExistsAsync("REVISOR"))
            .ReturnsAsync(true);
        _mockUserManager.Setup(x => x.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(x => x.AddToRoleAsync(user, "REVISOR"))
            .ReturnsAsync(IdentityResult.Success);

        // Setup final GetRolesAsync to return updated roles
        _mockUserManager.SetupSequence(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "CREADOR" })
            .ReturnsAsync(new List<string> { "REVISOR" });

        // Act
        var result = await _service.UpdateUserAsync(userId, updateDto, adminId);

        // Assert
        result.Should().NotBeNull();
        _mockUserManager.Verify(x => x.UpdateAsync(user), Times.Once);
    }

    [Fact(DisplayName = "T064: UpdateUserAsync should throw when user not found")]
    public async Task UpdateUserAsync_UserNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var updateDto = new UpdateUserDto
        {
            Nombre = "Updated Name",
            Email = "updated@example.com",
            Roles = new List<string> { "CREADOR" },
            Activo = true
        };

        _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((Usuario?)null);

        // Act
        var act = async () => await _service.UpdateUserAsync(userId, updateDto, adminId);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{userId}*no encontrado*");
    }

    [Fact(DisplayName = "T064: UpdateUserAsync should update password when provided")]
    public async Task UpdateUserAsync_WithNewPassword_UpdatesPassword()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var user = new Usuario
        {
            Id = userId,
            Email = "user@example.com",
            UserName = "user@example.com",
            Nombre = "User",
            CreatedAt = DateTime.UtcNow
        };

        var updateDto = new UpdateUserDto
        {
            Nombre = "User",
            Email = "user@example.com",
            NewPassword = "NewSecurePassword123!",
            Roles = new List<string> { "CREADOR" },
            Activo = true
        };

        _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        _mockUserManager.Setup(x => x.GeneratePasswordResetTokenAsync(user))
            .ReturnsAsync("reset-token");
        _mockUserManager.Setup(x => x.ResetPasswordAsync(user, "reset-token", updateDto.NewPassword))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "CREADOR" });

        // Act
        await _service.UpdateUserAsync(userId, updateDto, adminId);

        // Assert
        _mockUserManager.Verify(x => x.ResetPasswordAsync(user, "reset-token", updateDto.NewPassword), Times.Once);
    }

    #endregion

    #region SuspendUserAsync Tests

    [Fact(DisplayName = "T064: SuspendUserAsync should lock user account")]
    public async Task SuspendUserAsync_ValidUser_LocksAccount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var user = new Usuario
        {
            Id = userId,
            Email = "user@example.com",
            UserName = "user@example.com",
            LockoutEnabled = false,
            Nombre = "User",
            CreatedAt = DateTime.UtcNow
        };

        _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        _mockUserManager.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "CREADOR" });

        // Act
        var result = await _service.SuspendUserAsync(userId, adminId);

        // Assert
        result.Should().NotBeNull();
        user.Activo.Should().BeFalse();
        _mockUserManager.Verify(x => x.UpdateAsync(user), Times.Once);
    }

    [Fact(DisplayName = "T064: SuspendUserAsync should throw when suspending self")]
    public async Task SuspendUserAsync_SelfSuspension_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new Usuario
        {
            Id = userId,
            Email = "admin@example.com",
            Nombre = "Admin User",
            CreatedAt = DateTime.UtcNow
        };

        _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        // Act
        var act = async () => await _service.SuspendUserAsync(userId, userId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No puede suspender su propia cuenta*");
    }

    [Fact(DisplayName = "T064: SuspendUserAsync should throw when user not found")]
    public async Task SuspendUserAsync_UserNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((Usuario?)null);

        // Act
        var act = async () => await _service.SuspendUserAsync(userId, adminId);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    #endregion

    #region ActivateUserAsync Tests

    [Fact(DisplayName = "T064: ActivateUserAsync should unlock user account")]
    public async Task ActivateUserAsync_LockedUser_UnlocksAccount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var user = new Usuario
        {
            Id = userId,
            Email = "user@example.com",
            UserName = "user@example.com",
            Activo = false, // User is currently suspended
            Nombre = "User",
            CreatedAt = DateTime.UtcNow
        };

        _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        _mockUserManager.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "CREADOR" });

        // Act
        var result = await _service.ActivateUserAsync(userId, adminId);

        // Assert
        result.Should().NotBeNull();
        user.Activo.Should().BeTrue();
        _mockUserManager.Verify(x => x.UpdateAsync(user), Times.Once);
    }

    [Fact(DisplayName = "T064: ActivateUserAsync should throw when user not found")]
    public async Task ActivateUserAsync_UserNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((Usuario?)null);

        // Act
        var act = async () => await _service.ActivateUserAsync(userId, adminId);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    #endregion

    #region DeleteUserAsync Tests

    [Fact(DisplayName = "T064: DeleteUserAsync should soft-delete user")]
    public async Task DeleteUserAsync_ValidUser_SoftDeletesUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var originalEmail = "user@example.com";
        var user = new Usuario
        {
            Id = userId,
            Email = originalEmail,
            UserName = originalEmail,
            Nombre = "User",
            CreatedAt = DateTime.UtcNow
        };

        _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        _mockUserManager.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        await _service.DeleteUserAsync(userId, adminId);

        // Assert
        user.Activo.Should().BeFalse();
        user.Email.Should().StartWith("DELETED_");
        user.Email.Should().Contain(userId.ToString());
        _mockUserManager.Verify(x => x.UpdateAsync(user), Times.Once);
    }

    [Fact(DisplayName = "T064: DeleteUserAsync should throw when deleting self")]
    public async Task DeleteUserAsync_SelfDeletion_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new Usuario
        {
            Id = userId,
            Email = "admin@example.com",
            Nombre = "Admin User",
            CreatedAt = DateTime.UtcNow
        };

        _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        // Act
        var act = async () => await _service.DeleteUserAsync(userId, userId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No puede eliminar su propia cuenta*");
    }

    [Fact(DisplayName = "T064: DeleteUserAsync should throw when user not found")]
    public async Task DeleteUserAsync_UserNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((Usuario?)null);

        // Act
        var act = async () => await _service.DeleteUserAsync(userId, adminId);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    #endregion

    #region IsEmailInUseAsync Tests

    [Fact(DisplayName = "T064: IsEmailInUseAsync should return false when email not in use")]
    public async Task IsEmailInUseAsync_EmailNotInUse_ReturnsFalse()
    {
        // Arrange
        var email = "new@example.com";
        _mockUserManager.Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync((Usuario?)null);

        // Act
        var result = await _service.IsEmailInUseAsync(email);

        // Assert
        result.Should().BeFalse();
    }

    [Fact(DisplayName = "T064: IsEmailInUseAsync should return true when email is in use")]
    public async Task IsEmailInUseAsync_EmailInUse_ReturnsTrue()
    {
        // Arrange
        var email = "existing@example.com";
        var existingUser = new Usuario { Id = Guid.NewGuid(), Email = email, Nombre = "Existing", CreatedAt = DateTime.UtcNow };
        _mockUserManager.Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _service.IsEmailInUseAsync(email);

        // Assert
        result.Should().BeTrue();
    }

    [Fact(DisplayName = "T064: IsEmailInUseAsync should exclude specified user")]
    public async Task IsEmailInUseAsync_ExcludeUser_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "user@example.com";
        var existingUser = new Usuario { Id = userId, Email = email, Nombre = "Existing", CreatedAt = DateTime.UtcNow };
        _mockUserManager.Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _service.IsEmailInUseAsync(email, userId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
