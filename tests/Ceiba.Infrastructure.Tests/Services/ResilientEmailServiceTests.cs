using Ceiba.Core.Interfaces;
using Ceiba.Infrastructure.Services;
using Ceiba.Shared.DTOs;
using Microsoft.Extensions.Logging;
using NSubstitute;
using FluentAssertions;

namespace Ceiba.Infrastructure.Tests.Services;

/// <summary>
/// T146-T155: RO-003 Email resilience tests.
/// Tests retry logic, circuit breaker, and email queue functionality.
/// </summary>
[Trait("Category", "Unit")]
public class ResilientEmailServiceTests
{
    private readonly IEmailService _mockEmailService;
    private readonly ILogger<ResilientEmailService> _mockLogger;

    public ResilientEmailServiceTests()
    {
        _mockEmailService = Substitute.For<IEmailService>();
        _mockLogger = Substitute.For<ILogger<ResilientEmailService>>();
    }

    private ResilientEmailService CreateService(EmailResilienceOptions? options = null)
    {
        return new ResilientEmailService(_mockEmailService, _mockLogger, options);
    }

    private static SendEmailRequestDto CreateRequest(string subject = "Test Email")
    {
        return new SendEmailRequestDto
        {
            Recipients = ["test@example.com"],
            Subject = subject,
            BodyHtml = "<p>Test body</p>"
        };
    }

    #region T146: Basic Retry Logic

    [Fact]
    [Trait("NFR", "T146")]
    public async Task SendWithRetryAsync_Success_ReturnsSuccessOnFirstAttempt()
    {
        // Arrange
        var service = CreateService();
        var request = CreateRequest();

        _mockEmailService.SendAsync(request, Arg.Any<CancellationToken>())
            .Returns(new SendEmailResultDto { Success = true });

        // Act
        var result = await service.SendWithRetryAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        await _mockEmailService.Received(1).SendAsync(request, Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("NFR", "T146")]
    public async Task SendWithRetryAsync_FailsThenSucceeds_RetriesAndReturnsSuccess()
    {
        // Arrange
        var options = new EmailResilienceOptions { MaxRetries = 3, RetryDelayMs = 10 };
        var service = CreateService(options);
        var request = CreateRequest();

        var callCount = 0;
        _mockEmailService.SendAsync(request, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                return callCount < 3
                    ? new SendEmailResultDto { Success = false, Error = "Temporary failure" }
                    : new SendEmailResultDto { Success = true };
            });

        // Act
        var result = await service.SendWithRetryAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        await _mockEmailService.Received(3).SendAsync(request, Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("NFR", "T146")]
    public async Task SendWithRetryAsync_AllAttemptsFail_ReturnsFailure()
    {
        // Arrange
        var options = new EmailResilienceOptions { MaxRetries = 3, RetryDelayMs = 10, QueueOnFailure = false };
        var service = CreateService(options);
        var request = CreateRequest();

        _mockEmailService.SendAsync(request, Arg.Any<CancellationToken>())
            .Returns(new SendEmailResultDto { Success = false, Error = "Permanent failure" });

        // Act
        var result = await service.SendWithRetryAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Permanent failure");
        await _mockEmailService.Received(3).SendAsync(request, Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("NFR", "T146")]
    public async Task SendWithRetryAsync_ExceptionOnFirstAttempt_Retries()
    {
        // Arrange
        var options = new EmailResilienceOptions { MaxRetries = 2, RetryDelayMs = 10 };
        var service = CreateService(options);
        var request = CreateRequest();

        var callCount = 0;
        _mockEmailService.SendAsync(request, Arg.Any<CancellationToken>())
            .Returns<SendEmailResultDto>(_ =>
            {
                callCount++;
                if (callCount == 1)
                    throw new Exception("Connection failed");
                return new SendEmailResultDto { Success = true };
            });

        // Act
        var result = await service.SendWithRetryAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        await _mockEmailService.Received(2).SendAsync(request, Arg.Any<CancellationToken>());
    }

    #endregion

    #region T147: Exponential Backoff

    [Fact]
    [Trait("NFR", "T147")]
    public async Task SendWithRetryAsync_UsesExponentialBackoff()
    {
        // Arrange
        var options = new EmailResilienceOptions { MaxRetries = 3, RetryDelayMs = 50 };
        var service = CreateService(options);
        var request = CreateRequest();

        _mockEmailService.SendAsync(request, Arg.Any<CancellationToken>())
            .Returns(new SendEmailResultDto { Success = false, Error = "Fail" });

        var startTime = DateTime.UtcNow;

        // Act
        await service.SendWithRetryAsync(request);

        var elapsed = DateTime.UtcNow - startTime;

        // Assert - Should have waited at least 50ms + 100ms = 150ms (exponential: 50, 100)
        elapsed.TotalMilliseconds.Should().BeGreaterThan(100); // Some tolerance
    }

    #endregion

    #region T148: Circuit Breaker Opens

    [Fact]
    [Trait("NFR", "T148")]
    public async Task CircuitBreaker_OpensAfterThresholdFailures()
    {
        // Arrange
        var options = new EmailResilienceOptions
        {
            MaxRetries = 1,
            RetryDelayMs = 1,
            FailureThreshold = 3,
            QueueOnFailure = false
        };
        var service = CreateService(options);

        _mockEmailService.SendAsync(Arg.Any<SendEmailRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(new SendEmailResultDto { Success = false, Error = "Fail" });

        // Act - Send enough to trigger threshold
        for (var i = 0; i < 3; i++)
        {
            await service.SendWithRetryAsync(CreateRequest($"Email {i}"));
        }

        // Assert
        service.CurrentCircuitState.Should().Be(CircuitState.Open);
    }

    [Fact]
    [Trait("NFR", "T148")]
    public async Task CircuitBreaker_WhenOpen_RejectsRequests()
    {
        // Arrange
        var options = new EmailResilienceOptions
        {
            MaxRetries = 1,
            RetryDelayMs = 1,
            FailureThreshold = 2,
            CircuitBreakerTimeout = TimeSpan.FromHours(1) // Long timeout so it doesn't recover
        };
        var service = CreateService(options);

        _mockEmailService.SendAsync(Arg.Any<SendEmailRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(new SendEmailResultDto { Success = false, Error = "Fail" });

        // Open the circuit
        for (var i = 0; i < 2; i++)
        {
            await service.SendWithRetryAsync(CreateRequest());
        }

        _mockEmailService.ClearReceivedCalls();

        // Act - Try to send when circuit is open
        var result = await service.SendWithRetryAsync(CreateRequest("New Email"));

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("temporarily unavailable");
        await _mockEmailService.DidNotReceive().SendAsync(Arg.Any<SendEmailRequestDto>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region T149: Circuit Breaker Half-Open

    [Fact]
    [Trait("NFR", "T149")]
    public async Task CircuitBreaker_MovesToHalfOpenAfterTimeout()
    {
        // Arrange
        var options = new EmailResilienceOptions
        {
            MaxRetries = 1,
            RetryDelayMs = 1,
            FailureThreshold = 2,
            CircuitBreakerTimeout = TimeSpan.FromMilliseconds(50)
        };
        var service = CreateService(options);

        _mockEmailService.SendAsync(Arg.Any<SendEmailRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(new SendEmailResultDto { Success = false, Error = "Fail" });

        // Open the circuit
        for (var i = 0; i < 2; i++)
        {
            await service.SendWithRetryAsync(CreateRequest());
        }

        service.CurrentCircuitState.Should().Be(CircuitState.Open);

        // Wait for timeout
        await Task.Delay(100);

        _mockEmailService.ClearReceivedCalls();
        _mockEmailService.SendAsync(Arg.Any<SendEmailRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(new SendEmailResultDto { Success = true });

        // Act - Next request should be allowed (half-open)
        var result = await service.SendWithRetryAsync(CreateRequest());

        // Assert
        await _mockEmailService.Received(1).SendAsync(Arg.Any<SendEmailRequestDto>(), Arg.Any<CancellationToken>());
        result.Success.Should().BeTrue();
        service.CurrentCircuitState.Should().Be(CircuitState.Closed);
    }

    [Fact]
    [Trait("NFR", "T149")]
    public async Task CircuitBreaker_FailureInHalfOpen_ReturnsToOpen()
    {
        // Arrange
        var options = new EmailResilienceOptions
        {
            MaxRetries = 1,
            RetryDelayMs = 1,
            FailureThreshold = 2,
            CircuitBreakerTimeout = TimeSpan.FromMilliseconds(50)
        };
        var service = CreateService(options);

        _mockEmailService.SendAsync(Arg.Any<SendEmailRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(new SendEmailResultDto { Success = false, Error = "Fail" });

        // Open the circuit
        for (var i = 0; i < 2; i++)
        {
            await service.SendWithRetryAsync(CreateRequest());
        }

        // Wait for timeout to enter half-open
        await Task.Delay(100);

        // Act - Fail in half-open state
        await service.SendWithRetryAsync(CreateRequest());

        // Assert - Should be back to open
        service.CurrentCircuitState.Should().Be(CircuitState.Open);
    }

    #endregion

    #region T150: Email Queue

    [Fact]
    [Trait("NFR", "T150")]
    public async Task SendWithRetryAsync_OnFailure_QueuesEmail()
    {
        // Arrange
        var options = new EmailResilienceOptions
        {
            MaxRetries = 1,
            RetryDelayMs = 1,
            QueueOnFailure = true,
            FailureThreshold = 100 // High so circuit doesn't open
        };
        var service = CreateService(options);
        var request = CreateRequest("Queued Email");

        _mockEmailService.SendAsync(Arg.Any<SendEmailRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(new SendEmailResultDto { Success = false, Error = "Fail" });

        // Act
        await service.SendWithRetryAsync(request);

        // Assert
        service.QueuedEmailCount.Should().Be(1);
    }

    [Fact]
    [Trait("NFR", "T150")]
    public async Task CircuitOpen_QueuesEmail()
    {
        // Arrange
        var options = new EmailResilienceOptions
        {
            MaxRetries = 1,
            RetryDelayMs = 1,
            FailureThreshold = 2,
            CircuitBreakerTimeout = TimeSpan.FromHours(1)
        };
        var service = CreateService(options);

        _mockEmailService.SendAsync(Arg.Any<SendEmailRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(new SendEmailResultDto { Success = false, Error = "Fail" });

        // Open the circuit
        for (var i = 0; i < 2; i++)
        {
            await service.SendWithRetryAsync(CreateRequest());
        }

        // Queue should have 2 emails from failed attempts
        var initialCount = service.QueuedEmailCount;

        // Act - Send when circuit is open
        await service.SendWithRetryAsync(CreateRequest("New Email"));

        // Assert
        service.QueuedEmailCount.Should().Be(initialCount + 1);
    }

    [Fact]
    [Trait("NFR", "T150")]
    public async Task Queue_RespectsMaxSize()
    {
        // Arrange
        var options = new EmailResilienceOptions
        {
            MaxRetries = 1,
            RetryDelayMs = 1,
            QueueOnFailure = true,
            MaxQueueSize = 3,
            FailureThreshold = 100
        };
        var service = CreateService(options);

        _mockEmailService.SendAsync(Arg.Any<SendEmailRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(new SendEmailResultDto { Success = false, Error = "Fail" });

        // Act - Try to queue more than max
        for (var i = 0; i < 5; i++)
        {
            await service.SendWithRetryAsync(CreateRequest($"Email {i}"));
        }

        // Assert - Queue should be at max size
        service.QueuedEmailCount.Should().Be(3);
    }

    #endregion

    #region T151: Queue Processing

    [Fact]
    [Trait("NFR", "T151")]
    public async Task ProcessQueueAsync_SendsQueuedEmails()
    {
        // Arrange
        var options = new EmailResilienceOptions
        {
            MaxRetries = 1,
            RetryDelayMs = 1,
            QueueOnFailure = true,
            FailureThreshold = 100
        };
        var service = CreateService(options);

        // First, fail some emails to queue them
        _mockEmailService.SendAsync(Arg.Any<SendEmailRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(new SendEmailResultDto { Success = false, Error = "Fail" });

        for (var i = 0; i < 3; i++)
        {
            await service.SendWithRetryAsync(CreateRequest($"Email {i}"));
        }

        service.QueuedEmailCount.Should().Be(3);

        // Now make email service succeed
        _mockEmailService.ClearReceivedCalls();
        _mockEmailService.SendAsync(Arg.Any<SendEmailRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(new SendEmailResultDto { Success = true });

        // Act
        var processed = await service.ProcessQueueAsync();

        // Assert
        processed.Should().Be(3);
        service.QueuedEmailCount.Should().Be(0);
    }

    [Fact]
    [Trait("NFR", "T151")]
    public async Task ProcessQueueAsync_RequeuesFailedEmails()
    {
        // Arrange
        var options = new EmailResilienceOptions
        {
            MaxRetries = 1,
            RetryDelayMs = 1,
            QueueOnFailure = true,
            MaxQueueRetries = 3, // Small number to test exhaustion
            MaxQueueBatchSize = 10,
            FailureThreshold = 100
        };
        var service = CreateService(options);

        _mockEmailService.SendAsync(Arg.Any<SendEmailRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(new SendEmailResultDto { Success = false, Error = "Fail" });

        // Queue one email - this queues it with Attempts = 0
        await service.SendWithRetryAsync(CreateRequest());
        service.QueuedEmailCount.Should().Be(1);

        // Act - Process will fail, increment attempts, and eventually discard
        // when attempts >= MaxQueueRetries (after 3 processing attempts)
        var processed = await service.ProcessQueueAsync();

        // Assert
        // The email should be processed (and re-queued) until attempts exceed MaxQueueRetries
        // With MaxQueueRetries = 3, after 3 failures the email is discarded
        processed.Should().Be(0);
        service.QueuedEmailCount.Should().Be(0); // Discarded after exceeding retries
    }

    [Fact]
    [Trait("NFR", "T151")]
    public async Task ProcessQueueAsync_SkipsWhenCircuitOpen()
    {
        // Arrange
        var options = new EmailResilienceOptions
        {
            MaxRetries = 1,
            RetryDelayMs = 1,
            FailureThreshold = 2,
            CircuitBreakerTimeout = TimeSpan.FromHours(1)
        };
        var service = CreateService(options);

        _mockEmailService.SendAsync(Arg.Any<SendEmailRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(new SendEmailResultDto { Success = false, Error = "Fail" });

        // Open circuit and queue emails
        for (var i = 0; i < 3; i++)
        {
            await service.SendWithRetryAsync(CreateRequest());
        }

        service.CurrentCircuitState.Should().Be(CircuitState.Open);
        _mockEmailService.ClearReceivedCalls();

        // Act
        var processed = await service.ProcessQueueAsync();

        // Assert
        processed.Should().Be(0);
        await _mockEmailService.DidNotReceive().SendAsync(Arg.Any<SendEmailRequestDto>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region T152: Queue Expiration

    [Fact]
    [Trait("NFR", "T152")]
    public async Task ProcessQueueAsync_DiscardsExpiredEmails()
    {
        // Arrange - Note: This test uses short timeouts to simulate expiration
        var options = new EmailResilienceOptions
        {
            MaxRetries = 1,
            RetryDelayMs = 1,
            QueueOnFailure = true,
            MaxQueueTime = TimeSpan.FromMilliseconds(50),
            FailureThreshold = 100
        };
        var service = CreateService(options);

        _mockEmailService.SendAsync(Arg.Any<SendEmailRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(new SendEmailResultDto { Success = false, Error = "Fail" });

        // Queue an email
        await service.SendWithRetryAsync(CreateRequest());
        service.QueuedEmailCount.Should().Be(1);

        // Wait for expiration
        await Task.Delay(100);

        // Now make service succeed
        _mockEmailService.SendAsync(Arg.Any<SendEmailRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(new SendEmailResultDto { Success = true });

        // Act
        await service.ProcessQueueAsync();

        // Assert - Email should have been discarded (expired)
        service.QueuedEmailCount.Should().Be(0);
    }

    #endregion

    #region T153: Health Status

    [Fact]
    [Trait("NFR", "T153")]
    public void GetHealth_ReturnsCorrectStatus()
    {
        // Arrange
        var service = CreateService();

        // Act
        var health = service.GetHealth();

        // Assert
        health.CircuitState.Should().Be(CircuitState.Closed);
        health.QueuedEmails.Should().Be(0);
        health.FailureCount.Should().Be(0);
        health.IsHealthy.Should().BeTrue();
    }

    [Fact]
    [Trait("NFR", "T153")]
    public async Task GetHealth_ReflectsCircuitState()
    {
        // Arrange
        var options = new EmailResilienceOptions
        {
            MaxRetries = 1,
            RetryDelayMs = 1,
            FailureThreshold = 2,
            CircuitBreakerTimeout = TimeSpan.FromHours(1)
        };
        var service = CreateService(options);

        _mockEmailService.SendAsync(Arg.Any<SendEmailRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(new SendEmailResultDto { Success = false, Error = "Fail" });

        // Open the circuit
        for (var i = 0; i < 2; i++)
        {
            await service.SendWithRetryAsync(CreateRequest());
        }

        // Act
        var health = service.GetHealth();

        // Assert
        health.CircuitState.Should().Be(CircuitState.Open);
        health.IsHealthy.Should().BeFalse();
        health.CircuitOpenedAt.Should().NotBeNull();
    }

    [Fact]
    [Trait("NFR", "T153")]
    public async Task GetHealth_TracksQueueSize()
    {
        // Arrange
        var options = new EmailResilienceOptions
        {
            MaxRetries = 1,
            RetryDelayMs = 1,
            QueueOnFailure = true,
            FailureThreshold = 100
        };
        var service = CreateService(options);

        _mockEmailService.SendAsync(Arg.Any<SendEmailRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(new SendEmailResultDto { Success = false, Error = "Fail" });

        // Queue some emails
        for (var i = 0; i < 5; i++)
        {
            await service.SendWithRetryAsync(CreateRequest());
        }

        // Act
        var health = service.GetHealth();

        // Assert
        health.QueuedEmails.Should().Be(5);
    }

    #endregion

    #region T154: Success Resets Circuit

    [Fact]
    [Trait("NFR", "T154")]
    public async Task SuccessfulSend_ResetsFailureCount()
    {
        // Arrange
        var options = new EmailResilienceOptions
        {
            MaxRetries = 1,
            RetryDelayMs = 1,
            FailureThreshold = 5
        };
        var service = CreateService(options);

        // Fail a few times (but not enough to open circuit)
        _mockEmailService.SendAsync(Arg.Any<SendEmailRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(new SendEmailResultDto { Success = false, Error = "Fail" });

        for (var i = 0; i < 3; i++)
        {
            await service.SendWithRetryAsync(CreateRequest());
        }

        service.GetHealth().FailureCount.Should().Be(3);

        // Now succeed
        _mockEmailService.SendAsync(Arg.Any<SendEmailRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(new SendEmailResultDto { Success = true });

        // Act
        await service.SendWithRetryAsync(CreateRequest());

        // Assert
        service.GetHealth().FailureCount.Should().Be(0);
    }

    #endregion

    #region T155: Last Success Tracking

    [Fact]
    [Trait("NFR", "T155")]
    public async Task LastSuccessfulSend_UpdatesOnSuccess()
    {
        // Arrange
        var service = CreateService();

        _mockEmailService.SendAsync(Arg.Any<SendEmailRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(new SendEmailResultDto { Success = true });

        var before = DateTime.UtcNow;

        // Act
        await service.SendWithRetryAsync(CreateRequest());

        // Assert
        service.LastSuccessfulSend.Should().NotBeNull();
        service.LastSuccessfulSend.Should().BeOnOrAfter(before);
    }

    #endregion
}
