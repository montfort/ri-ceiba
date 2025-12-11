using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace Ceiba.Infrastructure.Resilience;

/// <summary>
/// Polly resilience policies for AI service HTTP calls.
/// T092a: Configure Polly policies (30s timeout, circuit breaker after 5 failures).
/// </summary>
public static class AiServicePolicies
{
    /// <summary>
    /// Timeout for AI API calls in seconds (T092a)
    /// </summary>
    public const int TimeoutSeconds = 30;

    /// <summary>
    /// Number of consecutive failures before circuit opens (T092a)
    /// </summary>
    public const int CircuitBreakerFailuresBeforeOpen = 5;

    /// <summary>
    /// Duration the circuit stays open before allowing test requests
    /// </summary>
    public static readonly TimeSpan CircuitBreakerDuration = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Number of retry attempts for transient failures
    /// </summary>
    public const int RetryCount = 2;

    /// <summary>
    /// Creates the combined resilience policy for AI HTTP client.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetResiliencePolicy(ILogger logger)
    {
        var timeoutPolicy = GetTimeoutPolicy(logger);
        var retryPolicy = GetRetryPolicy(logger);
        var circuitBreakerPolicy = GetCircuitBreakerPolicy(logger);

        // Wrap policies: Timeout -> Retry -> Circuit Breaker -> HTTP call
        return Policy.WrapAsync(timeoutPolicy, retryPolicy, circuitBreakerPolicy);
    }

    /// <summary>
    /// T092a: 30 second timeout policy
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy(ILogger logger)
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(
            TimeSpan.FromSeconds(TimeoutSeconds),
            TimeoutStrategy.Pessimistic,
            onTimeoutAsync: (context, timespan, task) =>
            {
                logger.LogWarning(
                    "AI service timeout after {Timeout}s. Operation: {OperationKey}",
                    timespan.TotalSeconds,
                    context.OperationKey);
                return Task.CompletedTask;
            });
    }

    /// <summary>
    /// Retry policy for transient HTTP failures
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(ILogger logger)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TimeoutRejectedException>()
            .WaitAndRetryAsync(
                RetryCount,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    logger.LogWarning(
                        "AI service retry {RetryAttempt}/{MaxRetries} after {Delay}s. " +
                        "Reason: {Reason}. Operation: {OperationKey}",
                        retryAttempt,
                        RetryCount,
                        timespan.TotalSeconds,
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString(),
                        context.OperationKey);
                });
    }

    /// <summary>
    /// T092a: Circuit breaker after 5 consecutive failures
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(ILogger logger)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TimeoutRejectedException>()
            .CircuitBreakerAsync(
                CircuitBreakerFailuresBeforeOpen,
                CircuitBreakerDuration,
                onBreak: (outcome, duration) =>
                {
                    logger.LogError(
                        "AI service circuit breaker OPENED for {Duration}s. " +
                        "Reason: {Reason}. Consecutive failures: {FailureCount}",
                        duration.TotalSeconds,
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString(),
                        CircuitBreakerFailuresBeforeOpen);
                },
                onReset: () =>
                {
                    logger.LogInformation("AI service circuit breaker RESET - service recovered");
                },
                onHalfOpen: () =>
                {
                    logger.LogInformation("AI service circuit breaker HALF-OPEN - testing service");
                });
    }
}

/// <summary>
/// Metrics tracking for AI service calls.
/// T092e: Add AI call monitoring (latency, tokens, success rate).
/// </summary>
public class AiServiceMetrics
{
    private readonly ILogger _logger;
    private long _totalCalls;
    private long _successfulCalls;
    private long _failedCalls;
    private long _totalTokensUsed;
    private double _totalLatencyMs;
    private readonly object _lock = new();

    public AiServiceMetrics(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Records a successful AI call.
    /// </summary>
    public void RecordSuccess(TimeSpan latency, int tokensUsed, string provider)
    {
        lock (_lock)
        {
            _totalCalls++;
            _successfulCalls++;
            _totalTokensUsed += tokensUsed;
            _totalLatencyMs += latency.TotalMilliseconds;
        }

        _logger.LogInformation(
            "AI call SUCCESS - Provider: {Provider}, Latency: {LatencyMs}ms, Tokens: {Tokens}, " +
            "Success rate: {SuccessRate:P2}",
            provider,
            latency.TotalMilliseconds,
            tokensUsed,
            SuccessRate);

        // Alert for slow responses (>10s is considered slow for AI calls)
        if (latency.TotalSeconds > 10)
        {
            _logger.LogWarning(
                "ALERT: Slow AI response - {LatencySeconds:F1}s. Provider: {Provider}",
                latency.TotalSeconds, provider);
        }
    }

    /// <summary>
    /// Records a failed AI call.
    /// </summary>
    public void RecordFailure(TimeSpan latency, string provider, string error)
    {
        lock (_lock)
        {
            _totalCalls++;
            _failedCalls++;
            _totalLatencyMs += latency.TotalMilliseconds;
        }

        _logger.LogWarning(
            "AI call FAILED - Provider: {Provider}, Latency: {LatencyMs}ms, Error: {Error}, " +
            "Success rate: {SuccessRate:P2}",
            provider,
            latency.TotalMilliseconds,
            error,
            SuccessRate);

        // Alert if success rate drops below 90%
        if (SuccessRate < 0.9 && _totalCalls > 5)
        {
            _logger.LogWarning(
                "ALERT: AI service success rate dropped to {SuccessRate:P2} ({Failed}/{Total} failed)",
                SuccessRate, _failedCalls, _totalCalls);
        }
    }

    /// <summary>
    /// Records a fallback being used.
    /// </summary>
    public void RecordFallback(string reason)
    {
        _logger.LogInformation(
            "AI fallback used - Reason: {Reason}. Current success rate: {SuccessRate:P2}",
            reason, SuccessRate);
    }

    /// <summary>
    /// Gets the current success rate.
    /// </summary>
    public double SuccessRate => _totalCalls == 0 ? 1.0 : (double)_successfulCalls / _totalCalls;

    /// <summary>
    /// Gets the average latency in milliseconds.
    /// </summary>
    public double AverageLatencyMs => _totalCalls == 0 ? 0 : _totalLatencyMs / _totalCalls;

    /// <summary>
    /// Gets the total tokens used across all calls.
    /// </summary>
    public long TotalTokensUsed => _totalTokensUsed;

    /// <summary>
    /// Gets current metrics summary.
    /// </summary>
    public AiServiceMetricsSummary GetSummary()
    {
        lock (_lock)
        {
            return new AiServiceMetricsSummary
            {
                TotalCalls = _totalCalls,
                SuccessfulCalls = _successfulCalls,
                FailedCalls = _failedCalls,
                SuccessRate = SuccessRate,
                AverageLatencyMs = AverageLatencyMs,
                TotalTokensUsed = _totalTokensUsed
            };
        }
    }

    /// <summary>
    /// Resets all metrics.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _totalCalls = 0;
            _successfulCalls = 0;
            _failedCalls = 0;
            _totalTokensUsed = 0;
            _totalLatencyMs = 0;
        }

        _logger.LogInformation("AI service metrics reset");
    }
}

/// <summary>
/// Summary of AI service metrics.
/// </summary>
public record AiServiceMetricsSummary
{
    public long TotalCalls { get; init; }
    public long SuccessfulCalls { get; init; }
    public long FailedCalls { get; init; }
    public double SuccessRate { get; init; }
    public double AverageLatencyMs { get; init; }
    public long TotalTokensUsed { get; init; }
}
