using Ceiba.Core.Interfaces;
using Ceiba.Shared.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ceiba.Infrastructure.Services;

/// <summary>
/// User management service for ADMIN operations.
/// US3: FR-021 to FR-026
/// </summary>
public class UserManagementService : IUserManagementService
{
    private readonly UserManager<IdentityUser<Guid>> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly IAuditService _auditService;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(
        UserManager<IdentityUser<Guid>> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IAuditService auditService,
        ILogger<UserManagementService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _auditService = auditService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<UserListResponse> ListUsersAsync(
        UserFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var query = _userManager.Users.AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.ToLower();
            query = query.Where(u =>
                u.Email != null && u.Email.ToLower().Contains(search) ||
                u.UserName != null && u.UserName.ToLower().Contains(search));
        }

        if (filter.Activo.HasValue)
        {
            query = query.Where(u => u.LockoutEnd == null == filter.Activo.Value);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var users = await query
            .OrderBy(u => u.Email)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        // Map to DTOs with roles
        var userDtos = new List<UserDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userDtos.Add(MapToDto(user, roles.ToList()));
        }

        // Filter by role if specified (done after getting roles)
        if (!string.IsNullOrWhiteSpace(filter.Role))
        {
            userDtos = userDtos.Where(u => u.Roles.Contains(filter.Role)).ToList();
            totalCount = userDtos.Count;
        }

        return new UserListResponse
        {
            Items = userDtos,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    /// <inheritdoc />
    public async Task<UserDto?> GetUserByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return null;

        var roles = await _userManager.GetRolesAsync(user);
        return MapToDto(user, roles.ToList());
    }

    /// <inheritdoc />
    public async Task<UserDto> CreateUserAsync(
        CreateUserDto createDto,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        // Check email uniqueness
        if (await IsEmailInUseAsync(createDto.Email, null, cancellationToken))
        {
            throw new InvalidOperationException($"El email '{createDto.Email}' ya está en uso");
        }

        // Create user
        var user = new IdentityUser<Guid>
        {
            Id = Guid.NewGuid(),
            UserName = createDto.Email,
            Email = createDto.Email,
            EmailConfirmed = true // Admin-created users are pre-confirmed
        };

        var result = await _userManager.CreateAsync(user, createDto.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Error al crear usuario: {errors}");
        }

        // Assign roles
        foreach (var role in createDto.Roles)
        {
            if (await _roleManager.RoleExistsAsync(role))
            {
                await _userManager.AddToRoleAsync(user, role);
            }
            else
            {
                _logger.LogWarning("Role {Role} does not exist, skipping", role);
            }
        }

        // Audit log
        await _auditService.LogAsync(
            AuditCodes.USER_CREATE,
            null,
            "Usuario",
            $"Usuario creado: {user.Email}, Roles: {string.Join(", ", createDto.Roles)}",
            null,
            cancellationToken);

        _logger.LogInformation("User {Email} created by admin {AdminId}", user.Email, adminUserId);

        var roles = await _userManager.GetRolesAsync(user);
        return MapToDto(user, roles.ToList());
    }

    /// <inheritdoc />
    public async Task<UserDto> UpdateUserAsync(
        Guid userId,
        UpdateUserDto updateDto,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new KeyNotFoundException($"Usuario con ID {userId} no encontrado");

        // Check email uniqueness if changed
        if (!string.Equals(user.Email, updateDto.Email, StringComparison.OrdinalIgnoreCase))
        {
            if (await IsEmailInUseAsync(updateDto.Email, userId, cancellationToken))
            {
                throw new InvalidOperationException($"El email '{updateDto.Email}' ya está en uso");
            }
            user.Email = updateDto.Email;
            user.UserName = updateDto.Email;
        }

        // Update password if provided
        if (!string.IsNullOrWhiteSpace(updateDto.NewPassword))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, updateDto.NewPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Error al cambiar contraseña: {errors}");
            }
        }

        // Update active status
        if (!updateDto.Activo && user.LockoutEnd == null)
        {
            user.LockoutEnd = DateTimeOffset.MaxValue;
        }
        else if (updateDto.Activo && user.LockoutEnd != null)
        {
            user.LockoutEnd = null;
        }

        await _userManager.UpdateAsync(user);

        // Update roles
        var currentRoles = await _userManager.GetRolesAsync(user);
        var rolesToRemove = currentRoles.Except(updateDto.Roles);
        var rolesToAdd = updateDto.Roles.Except(currentRoles);

        if (rolesToRemove.Any())
        {
            await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
        }

        foreach (var role in rolesToAdd)
        {
            if (await _roleManager.RoleExistsAsync(role))
            {
                await _userManager.AddToRoleAsync(user, role);
            }
        }

        // Audit log
        await _auditService.LogAsync(
            AuditCodes.USER_UPDATE,
            null,
            "Usuario",
            $"Usuario actualizado: {user.Email}",
            null,
            cancellationToken);

        _logger.LogInformation("User {Email} updated by admin {AdminId}", user.Email, adminUserId);

        var roles = await _userManager.GetRolesAsync(user);
        return MapToDto(user, roles.ToList());
    }

    /// <inheritdoc />
    public async Task<UserDto> SuspendUserAsync(
        Guid userId,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new KeyNotFoundException($"Usuario con ID {userId} no encontrado");

        // Prevent self-suspension
        if (userId == adminUserId)
        {
            throw new InvalidOperationException("No puede suspender su propia cuenta");
        }

        user.LockoutEnd = DateTimeOffset.MaxValue;
        user.LockoutEnabled = true;
        await _userManager.UpdateAsync(user);

        // Audit log
        await _auditService.LogAsync(
            AuditCodes.USER_SUSPEND,
            null,
            "Usuario",
            $"Usuario suspendido: {user.Email}",
            null,
            cancellationToken);

        _logger.LogInformation("User {Email} suspended by admin {AdminId}", user.Email, adminUserId);

        var roles = await _userManager.GetRolesAsync(user);
        return MapToDto(user, roles.ToList());
    }

    /// <inheritdoc />
    public async Task<UserDto> ActivateUserAsync(
        Guid userId,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new KeyNotFoundException($"Usuario con ID {userId} no encontrado");

        user.LockoutEnd = null;
        await _userManager.UpdateAsync(user);

        // Audit log
        await _auditService.LogAsync(
            AuditCodes.USER_ACTIVATE,
            null,
            "Usuario",
            $"Usuario activado: {user.Email}",
            null,
            cancellationToken);

        _logger.LogInformation("User {Email} activated by admin {AdminId}", user.Email, adminUserId);

        var roles = await _userManager.GetRolesAsync(user);
        return MapToDto(user, roles.ToList());
    }

    /// <inheritdoc />
    public async Task DeleteUserAsync(
        Guid userId,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new KeyNotFoundException($"Usuario con ID {userId} no encontrado");

        // Prevent self-deletion
        if (userId == adminUserId)
        {
            throw new InvalidOperationException("No puede eliminar su propia cuenta");
        }

        // Soft delete: Lock the account permanently and mark email as deleted
        user.LockoutEnd = DateTimeOffset.MaxValue;
        user.LockoutEnabled = true;
        user.Email = $"DELETED_{user.Id}_{user.Email}";
        user.NormalizedEmail = user.Email.ToUpperInvariant();
        user.UserName = user.Email;
        user.NormalizedUserName = user.Email.ToUpperInvariant();
        await _userManager.UpdateAsync(user);

        // Audit log
        await _auditService.LogAsync(
            AuditCodes.USER_DELETE,
            null,
            "Usuario",
            $"Usuario eliminado (soft delete): {user.Email}",
            null,
            cancellationToken);

        _logger.LogInformation("User {UserId} soft-deleted by admin {AdminId}", userId, adminUserId);
    }

    /// <inheritdoc />
    public async Task<List<string>> GetAvailableRolesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _roleManager.Roles
            .Select(r => r.Name!)
            .Where(n => n != null)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> IsEmailInUseAsync(
        string email,
        Guid? excludeUserId = null,
        CancellationToken cancellationToken = default)
    {
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser == null)
            return false;

        if (excludeUserId.HasValue && existingUser.Id == excludeUserId.Value)
            return false;

        return true;
    }

    private static UserDto MapToDto(IdentityUser<Guid> user, List<string> roles)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            Nombre = user.UserName ?? string.Empty,
            Roles = roles,
            Activo = user.LockoutEnd == null || user.LockoutEnd <= DateTimeOffset.UtcNow,
            CreatedAt = DateTime.UtcNow, // Identity doesn't track this by default
            LastLogin = user.LockoutEnd == null ? null : null // Would need custom tracking
        };
    }
}
