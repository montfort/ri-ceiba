using System.Diagnostics;
using Ceiba.Core.Interfaces;
using Ceiba.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ceiba.Infrastructure.Services;

/// <summary>
/// T156-T165: RO-004 Database health check implementation.
/// Monitors PostgreSQL database connectivity and performance.
/// </summary>
public class DatabaseHealthCheck : IServiceHealthCheck
{
    private readonly CeibaDbContext _context;
    private readonly ILogger<DatabaseHealthCheck> _logger;
    private const int HealthyResponseTimeMs = 1000;
    private const int DegradedResponseTimeMs = 3000;

    public DatabaseHealthCheck(
        CeibaDbContext context,
        ILogger<DatabaseHealthCheck> logger)
    {
        _context = context;
        _logger = logger;
    }

    public string ServiceName => "PostgreSQL Database";

    public async Task<ServiceHealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Simple connectivity check
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);

            stopwatch.Stop();
            var responseTime = stopwatch.ElapsedMilliseconds;

            if (!canConnect)
            {
                _logger.LogWarning("Database health check failed: Cannot connect");
                return new ServiceHealthStatus
                {
                    ServiceName = ServiceName,
                    IsHealthy = false,
                    Status = ServiceStatus.Unhealthy,
                    Details = "Cannot establish connection to database",
                    ResponseTimeMs = responseTime,
                    ErrorMessage = "Database connection failed"
                };
            }

            // Determine status based on response time
            var status = responseTime switch
            {
                <= HealthyResponseTimeMs => ServiceStatus.Healthy,
                <= DegradedResponseTimeMs => ServiceStatus.Degraded,
                _ => ServiceStatus.Unhealthy // > DegradedResponseTimeMs
            };

            var details = status switch
            {
                ServiceStatus.Healthy => "Database responding normally",
                ServiceStatus.Degraded => $"Database responding slowly ({responseTime}ms)",
                ServiceStatus.Unhealthy => $"Database response time critical ({responseTime}ms)"
            };

            var isHealthy = status is ServiceStatus.Healthy or ServiceStatus.Degraded;

            _logger.LogDebug(
                "Database health check: {Status} in {ResponseTime}ms",
                status, responseTime);

            return new ServiceHealthStatus
            {
                ServiceName = ServiceName,
                IsHealthy = isHealthy,
                Status = status,
                Details = details,
                ResponseTimeMs = responseTime
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex, "Database health check failed with exception");

            return new ServiceHealthStatus
            {
                ServiceName = ServiceName,
                IsHealthy = false,
                Status = ServiceStatus.Unhealthy,
                Details = "Database health check failed",
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                ErrorMessage = ex.Message
            };
        }
    }
}
