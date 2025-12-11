using System.Collections.Concurrent;
using Ceiba.Core.Interfaces;
using Ceiba.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace Ceiba.Infrastructure.Services;

/// <summary>
/// T146-T155: RO-003 Resilient email service wrapper.
/// Adds retry logic, circuit breaker, and email queue functionality.
/// </summary>
public class ResilientEmailService : IResilientEmailService
{
    private readonly IEmailService _innerEmailService;
    private readonly ILogger<ResilientEmailService> _logger;
    private readonly EmailResilienceOptions _options;

    // Circuit breaker state
    private CircuitState _circuitState = CircuitState.Closed;
    private int _failureCount;
    private DateTime _circuitOpenedAt;
    private DateTime _lastSuccessAt = DateTime.UtcNow;

    // Email queue for retry
    private readonly ConcurrentQueue<QueuedEmail> _emailQueue = new();
    private readonly object _lockObject = new();

    public ResilientEmailService(
        IEmailService innerEmailService,
        ILogger<ResilientEmailService> logger,
        EmailResilienceOptions? options = null)
    {
        _innerEmailService = innerEmailService;
        _logger = logger;
        _options = options ?? new EmailResilienceOptions();
    }

    public CircuitState CurrentCircuitState => _circuitState;
    public int QueuedEmailCount => _emailQueue.Count;
    public DateTime? LastSuccessfulSend => _lastSuccessAt;

    public async Task<SendEmailResultDto> SendWithRetryAsync(
        SendEmailRequestDto request,
        CancellationToken cancellationToken = default)
    {
        // Check circuit breaker
        if (!ShouldAttemptSend())
        {
            _logger.LogWarning("Circuit breaker is open. Queuing email for later delivery.");
            QueueEmail(request);

            return new SendEmailResultDto
            {
                Success = false,
                Error = "Email service temporarily unavailable. Message queued for later delivery."
            };
        }

        // Attempt to send with retries
        Exception? lastException = null;

        for (var attempt = 1; attempt <= _options.MaxRetries; attempt++)
        {
            try
            {
                var result = await _innerEmailService.SendAsync(request, cancellationToken);

                if (result.Success)
                {
                    RecordSuccess();
                    return result;
                }

                lastException = new Exception(result.Error);
                _logger.LogWarning(
                    "Email send attempt {Attempt}/{Max} failed: {Error}",
                    attempt, _options.MaxRetries, result.Error);
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning(ex,
                    "Email send attempt {Attempt}/{Max} threw exception",
                    attempt, _options.MaxRetries);
            }

            if (attempt < _options.MaxRetries)
            {
                // Wait before retry with exponential backoff
                var delay = TimeSpan.FromMilliseconds(
                    _options.RetryDelayMs * Math.Pow(2, attempt - 1));

                _logger.LogDebug("Waiting {Delay}ms before retry", delay.TotalMilliseconds);
                await Task.Delay(delay, cancellationToken);
            }
        }

        // All retries failed
        RecordFailure();

        // Queue the email for later if configured
        if (_options.QueueOnFailure)
        {
            QueueEmail(request);
            _logger.LogWarning("Email failed after {Retries} retries. Queued for later delivery.",
                _options.MaxRetries);
        }

        return new SendEmailResultDto
        {
            Success = false,
            Error = lastException?.Message ?? "Email failed after all retry attempts"
        };
    }

    public async Task<int> ProcessQueueAsync(CancellationToken cancellationToken = default)
    {
        if (_circuitState == CircuitState.Open)
        {
            _logger.LogDebug("Circuit breaker is open. Skipping queue processing.");
            return 0;
        }

        var processedCount = 0;
        var maxToProcess = _options.MaxQueueBatchSize;

        while (processedCount < maxToProcess && !cancellationToken.IsCancellationRequested)
        {
            if (!_emailQueue.TryDequeue(out var queuedEmail))
            {
                break;
            }

            var processResult = await ProcessSingleQueuedEmailAsync(queuedEmail, cancellationToken);
            if (processResult)
            {
                processedCount++;
            }

            if (_circuitState == CircuitState.Open)
            {
                break;
            }
        }

        return processedCount;
    }

    /// <summary>
    /// Processes a single queued email, returning true if successfully sent.
    /// </summary>
    private async Task<bool> ProcessSingleQueuedEmailAsync(QueuedEmail queuedEmail, CancellationToken cancellationToken)
    {
        if (ShouldDiscardQueuedEmail(queuedEmail))
        {
            return false;
        }

        return await TrySendQueuedEmailAsync(queuedEmail, cancellationToken);
    }

    /// <summary>
    /// Checks if a queued email should be discarded due to age or retry limits.
    /// </summary>
    private bool ShouldDiscardQueuedEmail(QueuedEmail queuedEmail)
    {
        if (DateTime.UtcNow - queuedEmail.QueuedAt > _options.MaxQueueTime)
        {
            _logger.LogWarning(
                "Discarding queued email (exceeded max queue time). Subject: {Subject}",
                queuedEmail.Request.Subject);
            return true;
        }

        if (queuedEmail.Attempts >= _options.MaxQueueRetries)
        {
            _logger.LogWarning(
                "Discarding queued email (exceeded max retries). Subject: {Subject}",
                queuedEmail.Request.Subject);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to send a queued email, handling success and failure.
    /// </summary>
    private async Task<bool> TrySendQueuedEmailAsync(QueuedEmail queuedEmail, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _innerEmailService.SendAsync(queuedEmail.Request, cancellationToken);

            if (result.Success)
            {
                RecordSuccess();
                _logger.LogInformation(
                    "Successfully sent queued email. Subject: {Subject}",
                    queuedEmail.Request.Subject);
                return true;
            }

            RequeueEmailAfterFailure(queuedEmail, "Failed to send queued email");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception sending queued email. Attempt {Attempt}. Re-queuing.", queuedEmail.Attempts + 1);
            RequeueEmailAfterFailure(queuedEmail, null);
            return false;
        }
    }

    /// <summary>
    /// Re-queues an email after a failed send attempt.
    /// </summary>
    private void RequeueEmailAfterFailure(QueuedEmail queuedEmail, string? warningMessage)
    {
        queuedEmail.Attempts++;
        _emailQueue.Enqueue(queuedEmail);
        RecordFailure();

        if (warningMessage != null)
        {
            _logger.LogWarning("{Message}. Attempt {Attempt}. Re-queuing.", warningMessage, queuedEmail.Attempts);
        }
    }

    public EmailServiceHealth GetHealth()
    {
        return new EmailServiceHealth
        {
            CircuitState = _circuitState,
            QueuedEmails = _emailQueue.Count,
            FailureCount = _failureCount,
            LastSuccessAt = _lastSuccessAt,
            CircuitOpenedAt = _circuitState == CircuitState.Open ? _circuitOpenedAt : null,
            IsHealthy = _circuitState != CircuitState.Open && _emailQueue.Count < _options.MaxQueueSize
        };
    }

    private bool ShouldAttemptSend()
    {
        lock (_lockObject)
        {
            switch (_circuitState)
            {
                case CircuitState.Closed:
                    return true;

                case CircuitState.Open:
                    // Check if we should move to half-open
                    if (DateTime.UtcNow - _circuitOpenedAt >= _options.CircuitBreakerTimeout)
                    {
                        _circuitState = CircuitState.HalfOpen;
                        _logger.LogInformation("Circuit breaker moving to half-open state");
                        return true;
                    }
                    return false;

                case CircuitState.HalfOpen:
                    // Allow one test request in half-open state
                    return true;

                default:
                    return false;
            }
        }
    }

    private void RecordSuccess()
    {
        lock (_lockObject)
        {
            _failureCount = 0;
            _lastSuccessAt = DateTime.UtcNow;

            if (_circuitState == CircuitState.HalfOpen)
            {
                _circuitState = CircuitState.Closed;
                _logger.LogInformation("Circuit breaker closed after successful request");
            }
        }
    }

    private void RecordFailure()
    {
        lock (_lockObject)
        {
            _failureCount++;

            if (_circuitState == CircuitState.HalfOpen)
            {
                // Single failure in half-open returns to open
                _circuitState = CircuitState.Open;
                _circuitOpenedAt = DateTime.UtcNow;
                _logger.LogWarning("Circuit breaker re-opened after failure in half-open state");
            }
            else if (_circuitState == CircuitState.Closed &&
                     _failureCount >= _options.FailureThreshold)
            {
                _circuitState = CircuitState.Open;
                _circuitOpenedAt = DateTime.UtcNow;
                _logger.LogWarning(
                    "Circuit breaker opened after {Failures} consecutive failures",
                    _failureCount);
            }
        }
    }

    private void QueueEmail(SendEmailRequestDto request)
    {
        if (_emailQueue.Count >= _options.MaxQueueSize)
        {
            _logger.LogError(
                "Email queue is full ({Size}). Discarding email. Subject: {Subject}",
                _options.MaxQueueSize, request.Subject);
            return;
        }

        _emailQueue.Enqueue(new QueuedEmail
        {
            Request = request,
            QueuedAt = DateTime.UtcNow,
            Attempts = 0
        });

        _logger.LogInformation(
            "Email queued for later delivery. Queue size: {Size}. Subject: {Subject}",
            _emailQueue.Count, request.Subject);
    }

    private class QueuedEmail
    {
        public required SendEmailRequestDto Request { get; init; }
        public DateTime QueuedAt { get; init; }
        public int Attempts { get; set; }
    }
}
