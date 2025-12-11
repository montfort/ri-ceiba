using System.Diagnostics;
using Ceiba.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Ceiba.Infrastructure.Services;

/// <summary>
/// T156-T165: RO-004 Email service health check implementation.
/// Monitors email service availability via the resilient email service.
/// </summary>
public class EmailHealthCheck : IServiceHealthCheck
{
    private readonly IResilientEmailService? _resilientEmailService;
    private readonly ILogger<EmailHealthCheck> _logger;

    public EmailHealthCheck(
        IResilientEmailService? resilientEmailService,
        ILogger<EmailHealthCheck> logger)
    {
        _resilientEmailService = resilientEmailService;
        _logger = logger;
    }

    public string ServiceName => "Email Service";

    public Task<ServiceHealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (_resilientEmailService == null)
            {
                stopwatch.Stop();
                return Task.FromResult(new ServiceHealthStatus
                {
                    ServiceName = ServiceName,
                    IsHealthy = false,
                    Status = ServiceStatus.Unhealthy,
                    Details = "Email service not configured",
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    ErrorMessage = "IResilientEmailService not available"
                });
            }

            var health = _resilientEmailService.GetHealth();
            stopwatch.Stop();

            var status = health.CircuitState switch
            {
                CircuitState.Closed => ServiceStatus.Healthy,
                CircuitState.HalfOpen => ServiceStatus.Degraded,
                CircuitState.Open => ServiceStatus.Unhealthy,
                _ => ServiceStatus.Unhealthy
            };

            var details = health.CircuitState switch
            {
                CircuitState.Closed => $"Email service healthy. Queue: {health.QueuedEmails}",
                CircuitState.HalfOpen => $"Email service recovering. Queue: {health.QueuedEmails}",
                CircuitState.Open => $"Email service unavailable. Failures: {health.FailureCount}",
                _ => "Unknown state"
            };

            _logger.LogDebug(
                "Email health check: {Status}, Circuit: {CircuitState}",
                status, health.CircuitState);

            return Task.FromResult(new ServiceHealthStatus
            {
                ServiceName = ServiceName,
                IsHealthy = health.IsHealthy,
                Status = status,
                Details = details,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex, "Email health check failed with exception");

            return Task.FromResult(new ServiceHealthStatus
            {
                ServiceName = ServiceName,
                IsHealthy = false,
                Status = ServiceStatus.Unhealthy,
                Details = "Email health check failed",
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                ErrorMessage = ex.Message
            });
        }
    }
}
