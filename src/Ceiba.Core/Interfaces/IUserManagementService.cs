using Ceiba.Shared.DTOs;

namespace Ceiba.Core.Interfaces;

/// <summary>
/// Service interface for user management operations.
/// Only accessible by ADMIN role.
/// US3: FR-021 to FR-026
/// </summary>
public interface IUserManagementService
{
    /// <summary>
    /// Gets a paginated list of users with optional filtering.
    /// FR-021: List all system users
    /// </summary>
    Task<UserListResponse> ListUsersAsync(
        UserFilterDto filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by ID.
    /// </summary>
    Task<UserDto?> GetUserByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new user with specified roles.
    /// FR-022: Create users with name, email, password, roles
    /// FR-026: Validates unique email
    /// </summary>
    Task<UserDto> CreateUserAsync(
        CreateUserDto createDto,
        Guid adminUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing user.
    /// FR-025: Assign or modify roles
    /// </summary>
    Task<UserDto> UpdateUserAsync(
        Guid userId,
        UpdateUserDto updateDto,
        Guid adminUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Suspends a user (prevents login).
    /// FR-023: Suspend existing users
    /// </summary>
    Task<UserDto> SuspendUserAsync(
        Guid userId,
        Guid adminUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates a suspended user.
    /// </summary>
    Task<UserDto> ActivateUserAsync(
        Guid userId,
        Guid adminUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes a user (marks as inactive).
    /// FR-024: Preserve referential integrity
    /// </summary>
    Task DeleteUserAsync(
        Guid userId,
        Guid adminUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available roles for assignment.
    /// </summary>
    Task<List<string>> GetAvailableRolesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an email is already in use.
    /// FR-026: Unique email constraint
    /// </summary>
    Task<bool> IsEmailInUseAsync(
        string email,
        Guid? excludeUserId = null,
        CancellationToken cancellationToken = default);
}
