using Ceiba.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Ceiba.Infrastructure.Services;

/// <summary>
/// T156-T165: RO-004 Graceful degradation service.
/// Provides fallback behavior when services are unavailable.
/// </summary>
public class GracefulDegradationService
{
    private readonly ILogger<GracefulDegradationService> _logger;
    private readonly Dictionary<string, ServiceDegradationState> _serviceStates = new();
    private readonly object _lockObject = new();

    public GracefulDegradationService(ILogger<GracefulDegradationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Records a service failure and updates degradation state.
    /// </summary>
    public void RecordFailure(string serviceName)
    {
        lock (_lockObject)
        {
            if (!_serviceStates.TryGetValue(serviceName, out var state))
            {
                state = new ServiceDegradationState { ServiceName = serviceName };
                _serviceStates[serviceName] = state;
            }

            state.ConsecutiveFailures++;
            state.LastFailureAt = DateTime.UtcNow;
            state.IsDegraded = state.ConsecutiveFailures >= 3;

            if (state.IsDegraded)
            {
                _logger.LogWarning(
                    "Service {Service} is now degraded after {Failures} consecutive failures",
                    serviceName, state.ConsecutiveFailures);
            }
        }
    }

    /// <summary>
    /// Records a service success and resets degradation state.
    /// </summary>
    public void RecordSuccess(string serviceName)
    {
        lock (_lockObject)
        {
            if (!_serviceStates.TryGetValue(serviceName, out var state))
            {
                state = new ServiceDegradationState { ServiceName = serviceName };
                _serviceStates[serviceName] = state;
            }

            var wasDegraded = state.IsDegraded;
            state.ConsecutiveFailures = 0;
            state.IsDegraded = false;
            state.LastSuccessAt = DateTime.UtcNow;

            if (wasDegraded)
            {
                _logger.LogInformation(
                    "Service {Service} recovered from degraded state",
                    serviceName);
            }
        }
    }

    /// <summary>
    /// Checks if a service is currently in a degraded state.
    /// </summary>
    public bool IsServiceDegraded(string serviceName)
    {
        lock (_lockObject)
        {
            return _serviceStates.TryGetValue(serviceName, out var state) && state.IsDegraded;
        }
    }

    /// <summary>
    /// Gets the current state of a service.
    /// </summary>
    public ServiceDegradationState? GetServiceState(string serviceName)
    {
        lock (_lockObject)
        {
            return _serviceStates.TryGetValue(serviceName, out var state)
                ? state.Clone()
                : null;
        }
    }

    /// <summary>
    /// Gets all service states.
    /// </summary>
    public IReadOnlyList<ServiceDegradationState> GetAllServiceStates()
    {
        lock (_lockObject)
        {
            return _serviceStates.Values.Select(s => s.Clone()).ToList();
        }
    }

    /// <summary>
    /// Executes an operation with fallback support.
    /// </summary>
    public async Task<T> ExecuteWithFallbackAsync<T>(
        string serviceName,
        Func<Task<T>> operation,
        Func<Task<T>> fallback)
    {
        if (IsServiceDegraded(serviceName))
        {
            _logger.LogDebug(
                "Service {Service} is degraded, using fallback",
                serviceName);
            return await fallback();
        }

        try
        {
            var result = await operation();
            RecordSuccess(serviceName);
            return result;
        }
        catch (Exception ex)
        {
            RecordFailure(serviceName);
            _logger.LogWarning(ex,
                "Service {Service} operation failed, using fallback",
                serviceName);
            return await fallback();
        }
    }

    /// <summary>
    /// Executes an operation with fallback support (synchronous version).
    /// </summary>
    public T ExecuteWithFallback<T>(
        string serviceName,
        Func<T> operation,
        Func<T> fallback)
    {
        if (IsServiceDegraded(serviceName))
        {
            _logger.LogDebug(
                "Service {Service} is degraded, using fallback",
                serviceName);
            return fallback();
        }

        try
        {
            var result = operation();
            RecordSuccess(serviceName);
            return result;
        }
        catch (Exception ex)
        {
            RecordFailure(serviceName);
            _logger.LogWarning(ex,
                "Service {Service} operation failed, using fallback",
                serviceName);
            return fallback();
        }
    }
}

/// <summary>
/// State of a service for graceful degradation.
/// </summary>
public class ServiceDegradationState
{
    public string ServiceName { get; init; } = string.Empty;
    public int ConsecutiveFailures { get; set; }
    public bool IsDegraded { get; set; }
    public DateTime? LastFailureAt { get; set; }
    public DateTime? LastSuccessAt { get; set; }

    public ServiceDegradationState Clone()
    {
        return new ServiceDegradationState
        {
            ServiceName = ServiceName,
            ConsecutiveFailures = ConsecutiveFailures,
            IsDegraded = IsDegraded,
            LastFailureAt = LastFailureAt,
            LastSuccessAt = LastSuccessAt
        };
    }
}
