using Ceiba.Shared.DTOs;

namespace Ceiba.Core.Interfaces;

/// <summary>
/// T146-T155: RO-003 Resilient email service interface.
/// Provides retry logic, circuit breaker, and email queue functionality.
/// </summary>
public interface IResilientEmailService
{
    /// <summary>
    /// Current state of the circuit breaker.
    /// </summary>
    CircuitState CurrentCircuitState { get; }

    /// <summary>
    /// Number of emails currently in the retry queue.
    /// </summary>
    int QueuedEmailCount { get; }

    /// <summary>
    /// Timestamp of the last successful email send.
    /// </summary>
    DateTime? LastSuccessfulSend { get; }

    /// <summary>
    /// Sends an email with automatic retry logic and circuit breaker protection.
    /// </summary>
    /// <param name="request">The email request to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<SendEmailResultDto> SendWithRetryAsync(
        SendEmailRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes queued emails that failed to send.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of successfully sent emails.</returns>
    Task<int> ProcessQueueAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current health status of the email service.
    /// </summary>
    /// <returns>Health status information.</returns>
    EmailServiceHealth GetHealth();
}

/// <summary>
/// Circuit breaker states.
/// </summary>
public enum CircuitState
{
    /// <summary>Normal operation - requests pass through.</summary>
    Closed,
    /// <summary>Failing - requests are rejected.</summary>
    Open,
    /// <summary>Testing if service recovered.</summary>
    HalfOpen
}

/// <summary>
/// Email service health status.
/// </summary>
public class EmailServiceHealth
{
    public CircuitState CircuitState { get; init; }
    public int QueuedEmails { get; init; }
    public int FailureCount { get; init; }
    public DateTime LastSuccessAt { get; init; }
    public DateTime? CircuitOpenedAt { get; init; }
    public bool IsHealthy { get; init; }
}

/// <summary>
/// Configuration options for email resilience.
/// </summary>
public class EmailResilienceOptions
{
    /// <summary>
    /// Maximum number of retry attempts for a single email.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Initial delay between retries in milliseconds.
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Whether to queue emails when sending fails.
    /// </summary>
    public bool QueueOnFailure { get; set; } = true;

    /// <summary>
    /// Maximum number of emails in the queue.
    /// </summary>
    public int MaxQueueSize { get; set; } = 100;

    /// <summary>
    /// Maximum time an email can stay in the queue.
    /// </summary>
    public TimeSpan MaxQueueTime { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Maximum retry attempts for queued emails.
    /// </summary>
    public int MaxQueueRetries { get; set; } = 5;

    /// <summary>
    /// Maximum emails to process per queue batch.
    /// </summary>
    public int MaxQueueBatchSize { get; set; } = 10;

    /// <summary>
    /// Number of consecutive failures before opening circuit.
    /// </summary>
    public int FailureThreshold { get; set; } = 5;

    /// <summary>
    /// How long to wait before testing circuit recovery.
    /// </summary>
    public TimeSpan CircuitBreakerTimeout { get; set; } = TimeSpan.FromMinutes(1);
}
