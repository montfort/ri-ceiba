using Ceiba.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Ceiba.Infrastructure.Services;

/// <summary>
/// T156-T165: RO-004 Aggregated health check service.
/// Collects health status from all registered health checks.
/// </summary>
public class AggregatedHealthCheckService
{
    private readonly IEnumerable<IServiceHealthCheck> _healthChecks;
    private readonly ILogger<AggregatedHealthCheckService> _logger;

    public AggregatedHealthCheckService(
        IEnumerable<IServiceHealthCheck> healthChecks,
        ILogger<AggregatedHealthCheckService> logger)
    {
        _healthChecks = healthChecks;
        _logger = logger;
    }

    /// <summary>
    /// Checks the health of all registered services.
    /// </summary>
    public async Task<AggregatedHealthStatus> CheckAllAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<ServiceHealthStatus>();
        var startTime = DateTime.UtcNow;

        foreach (var healthCheck in _healthChecks)
        {
            try
            {
                var result = await healthCheck.CheckHealthAsync(cancellationToken);
                results.Add(result);

                _logger.LogDebug(
                    "Health check for {Service}: {Status}",
                    healthCheck.ServiceName, result.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Health check for {Service} threw exception",
                    healthCheck.ServiceName);

                results.Add(new ServiceHealthStatus
                {
                    ServiceName = healthCheck.ServiceName,
                    IsHealthy = false,
                    Status = ServiceStatus.Unhealthy,
                    ErrorMessage = ex.Message
                });
            }
        }

        var overallStatus = DetermineOverallStatus(results);

        return new AggregatedHealthStatus
        {
            OverallStatus = overallStatus,
            IsHealthy = overallStatus != ServiceStatus.Unhealthy,
            Services = results,
            CheckedAt = startTime,
            TotalServices = results.Count,
            HealthyServices = results.Count(r => r.Status == ServiceStatus.Healthy),
            DegradedServices = results.Count(r => r.Status == ServiceStatus.Degraded),
            UnhealthyServices = results.Count(r => r.Status == ServiceStatus.Unhealthy)
        };
    }

    /// <summary>
    /// Checks health of a specific service by name.
    /// </summary>
    public async Task<ServiceHealthStatus?> CheckServiceAsync(
        string serviceName,
        CancellationToken cancellationToken = default)
    {
        var healthCheck = _healthChecks.FirstOrDefault(
            h => h.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase));

        if (healthCheck == null)
        {
            _logger.LogWarning("Health check for {Service} not found", serviceName);
            return null;
        }

        return await healthCheck.CheckHealthAsync(cancellationToken);
    }

    private static ServiceStatus DetermineOverallStatus(List<ServiceHealthStatus> results)
    {
        if (results.Count == 0)
            return ServiceStatus.Healthy;

        if (results.Any(r => r.Status == ServiceStatus.Unhealthy))
            return ServiceStatus.Unhealthy;

        if (results.Any(r => r.Status == ServiceStatus.Degraded))
            return ServiceStatus.Degraded;

        return ServiceStatus.Healthy;
    }
}

/// <summary>
/// Aggregated health status across all services.
/// </summary>
public class AggregatedHealthStatus
{
    public ServiceStatus OverallStatus { get; init; }
    public bool IsHealthy { get; init; }
    public IReadOnlyList<ServiceHealthStatus> Services { get; init; } = Array.Empty<ServiceHealthStatus>();
    public DateTime CheckedAt { get; init; }
    public int TotalServices { get; init; }
    public int HealthyServices { get; init; }
    public int DegradedServices { get; init; }
    public int UnhealthyServices { get; init; }
}
