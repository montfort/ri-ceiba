using Ceiba.Shared.DTOs;

namespace Ceiba.Core.Interfaces;

/// <summary>
/// Service for managing initial application setup.
/// Used to detect if setup is required and create the first admin user.
/// </summary>
public interface ISetupService
{
    /// <summary>
    /// Gets the current setup status.
    /// Returns information about whether setup is complete or required.
    /// </summary>
    Task<SetupStatus> GetStatusAsync();

    /// <summary>
    /// Checks if the initial setup is required.
    /// Returns true if no users exist in the system.
    /// </summary>
    Task<bool> IsSetupRequiredAsync();

    /// <summary>
    /// Marks the setup as complete.
    /// Should be called after the first admin user is created.
    /// </summary>
    Task MarkSetupCompleteAsync();

    /// <summary>
    /// Creates the first administrator user.
    /// Also seeds suggestions and other initial data.
    /// </summary>
    /// <param name="dto">The admin user details.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<SetupResult> CreateFirstAdminAsync(CreateFirstAdminDto dto);
}
