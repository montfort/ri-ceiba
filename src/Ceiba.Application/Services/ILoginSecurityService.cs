namespace Ceiba.Application.Services;

/// <summary>
/// Service for login security including rate limiting and progressive delays.
/// Implements OWASP recommendations for brute force protection.
/// </summary>
public interface ILoginSecurityService
{
    /// <summary>
    /// Checks if the IP address is currently blocked due to rate limiting.
    /// </summary>
    Task<bool> IsIpBlockedAsync(string ipAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a failed login attempt for the given IP and email.
    /// </summary>
    Task RecordFailedAttemptAsync(string ipAddress, string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a successful login, clearing failed attempts.
    /// </summary>
    Task RecordSuccessfulLoginAsync(string ipAddress, string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the progressive delay in milliseconds based on failed attempts.
    /// </summary>
    Task<int> GetProgressiveDelayAsync(string ipAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the remaining lockout time in seconds, or 0 if not locked.
    /// </summary>
    Task<int> GetRemainingLockoutSecondsAsync(string ipAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the number of failed attempts for an IP address.
    /// </summary>
    Task<int> GetFailedAttemptCountAsync(string ipAddress, CancellationToken cancellationToken = default);
}
