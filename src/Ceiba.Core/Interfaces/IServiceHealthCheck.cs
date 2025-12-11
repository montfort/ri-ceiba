namespace Ceiba.Core.Interfaces;

/// <summary>
/// T156-T165: RO-004 Service health check interface.
/// Provides health status for services with circuit breaker patterns.
/// </summary>
public interface IServiceHealthCheck
{
    /// <summary>
    /// Gets the name of the service being monitored.
    /// </summary>
    string ServiceName { get; }

    /// <summary>
    /// Checks if the service is healthy and available.
    /// </summary>
    Task<ServiceHealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Health status for a service.
/// </summary>
public class ServiceHealthStatus
{
    /// <summary>
    /// Name of the service.
    /// </summary>
    public string ServiceName { get; init; } = string.Empty;

    /// <summary>
    /// Whether the service is healthy.
    /// </summary>
    public bool IsHealthy { get; init; }

    /// <summary>
    /// Current service status.
    /// </summary>
    public ServiceStatus Status { get; init; }

    /// <summary>
    /// Additional details about the service health.
    /// </summary>
    public string? Details { get; init; }

    /// <summary>
    /// When the health was last checked.
    /// </summary>
    public DateTime LastChecked { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Response time of the health check in milliseconds.
    /// </summary>
    public long ResponseTimeMs { get; init; }

    /// <summary>
    /// Error message if the service is unhealthy.
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Service availability status levels.
/// </summary>
public enum ServiceStatus
{
    /// <summary>Service is fully operational.</summary>
    Healthy,
    /// <summary>Service is operational but may have degraded performance.</summary>
    Degraded,
    /// <summary>Service is unavailable.</summary>
    Unhealthy
}
