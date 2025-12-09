using Ceiba.Shared.DTOs;

namespace Ceiba.Core.Interfaces;

/// <summary>
/// Service for managing email configuration.
/// Supports SMTP and transactional email providers like SendGrid.
/// </summary>
public interface IEmailConfigService
{
    /// <summary>
    /// Gets the current email configuration.
    /// </summary>
    Task<EmailConfigDto?> GetConfigurationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the email configuration.
    /// </summary>
    Task<EmailConfigDto> UpdateConfigurationAsync(
        EmailConfigUpdateDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures a default email configuration exists.
    /// </summary>
    Task<EmailConfigDto> EnsureConfigurationExistsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests the email configuration by sending a test email.
    /// </summary>
    Task<EmailConfigTestResultDto> TestConfigurationAsync(
        string testRecipient,
        CancellationToken cancellationToken = default);
}
