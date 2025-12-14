using System.Diagnostics;
using Ceiba.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Ceiba.Infrastructure.Services;

/// <summary>
/// T156-T165: RO-004 AI service health check implementation.
/// Monitors AI narrative service availability.
/// </summary>
public class AiServiceHealthCheck : IServiceHealthCheck
{
    private readonly IAiNarrativeService? _aiService;
    private readonly ILogger<AiServiceHealthCheck> _logger;

    public AiServiceHealthCheck(
        IAiNarrativeService? aiService,
        ILogger<AiServiceHealthCheck> logger)
    {
        _aiService = aiService;
        _logger = logger;
    }

    public string ServiceName => "AI Narrative Service";

    public async Task<ServiceHealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (_aiService == null)
            {
                stopwatch.Stop();
                return new ServiceHealthStatus
                {
                    ServiceName = ServiceName,
                    IsHealthy = false,
                    Status = ServiceStatus.Unhealthy,
                    Details = "AI service not configured",
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    ErrorMessage = "IAiNarrativeService not available"
                };
            }

            var isAvailable = await _aiService.IsAvailableAsync(cancellationToken);
            stopwatch.Stop();

            var status = isAvailable ? ServiceStatus.Healthy : ServiceStatus.Unhealthy;
            var details = isAvailable
                ? "AI service responding normally"
                : "AI service unavailable";

            _logger.LogDebug(
                "AI health check: {Status} in {ResponseTime}ms",
                status, stopwatch.ElapsedMilliseconds);

            return new ServiceHealthStatus
            {
                ServiceName = ServiceName,
                IsHealthy = isAvailable,
                Status = status,
                Details = details,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex, "AI health check failed with exception");

            return new ServiceHealthStatus
            {
                ServiceName = ServiceName,
                IsHealthy = false,
                Status = ServiceStatus.Unhealthy,
                Details = "AI health check failed",
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                ErrorMessage = ex.Message
            };
        }
    }
}
