using Ceiba.Core.Interfaces;
using Ceiba.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Ceiba.Infrastructure.Tests.Services;

/// <summary>
/// T156-T165: RO-004 Email health check tests.
/// Tests email service health monitoring via resilient email service.
/// </summary>
[Trait("Category", "Unit")]
public class EmailHealthCheckTests
{
    private readonly IResilientEmailService _mockResilientEmailService;
    private readonly ILogger<EmailHealthCheck> _mockLogger;

    public EmailHealthCheckTests()
    {
        _mockResilientEmailService = Substitute.For<IResilientEmailService>();
        _mockLogger = Substitute.For<ILogger<EmailHealthCheck>>();
    }

    private EmailHealthCheck CreateHealthCheck(IResilientEmailService? resilientEmailService = null)
    {
        return new EmailHealthCheck(
            resilientEmailService ?? _mockResilientEmailService,
            _mockLogger);
    }

    #region Service Name

    [Fact]
    [Trait("NFR", "T156")]
    public void ServiceName_ReturnsExpectedName()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();

        // Act
        var serviceName = healthCheck.ServiceName;

        // Assert
        serviceName.Should().Be("Email Service");
    }

    #endregion

    #region Healthy Scenarios

    [Fact]
    [Trait("NFR", "T157")]
    public async Task CheckHealthAsync_CircuitClosed_ReturnsHealthyStatus()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();
        var emailHealth = new EmailServiceHealth
        {
            CircuitState = CircuitState.Closed,
            IsHealthy = true,
            QueuedEmails = 0,
            FailureCount = 0,
            LastSuccessAt = DateTime.UtcNow
        };

        _mockResilientEmailService.GetHealth().Returns(emailHealth);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Should().NotBeNull();
        result.ServiceName.Should().Be("Email Service");
        result.IsHealthy.Should().BeTrue();
        result.Status.Should().Be(ServiceStatus.Healthy);
        result.Details.Should().Be("Email service healthy. Queue: 0");
        result.ResponseTimeMs.Should().BeGreaterThanOrEqualTo(0);
        result.ErrorMessage.Should().BeNullOrEmpty();
    }

    [Fact]
    [Trait("NFR", "T157")]
    public async Task CheckHealthAsync_CircuitClosedWithQueuedEmails_ReturnsHealthyWithQueueCount()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();
        var emailHealth = new EmailServiceHealth
        {
            CircuitState = CircuitState.Closed,
            IsHealthy = true,
            QueuedEmails = 5,
            FailureCount = 0,
            LastSuccessAt = DateTime.UtcNow
        };

        _mockResilientEmailService.GetHealth().Returns(emailHealth);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.IsHealthy.Should().BeTrue();
        result.Status.Should().Be(ServiceStatus.Healthy);
        result.Details.Should().Be("Email service healthy. Queue: 5");
    }

    #endregion

    #region Degraded Scenarios

    [Fact]
    [Trait("NFR", "T158")]
    public async Task CheckHealthAsync_CircuitHalfOpen_ReturnsDegradedStatus()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();
        var emailHealth = new EmailServiceHealth
        {
            CircuitState = CircuitState.HalfOpen,
            IsHealthy = false,
            QueuedEmails = 3,
            FailureCount = 2,
            LastSuccessAt = DateTime.UtcNow.AddMinutes(-5)
        };

        _mockResilientEmailService.GetHealth().Returns(emailHealth);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.IsHealthy.Should().BeFalse();
        result.Status.Should().Be(ServiceStatus.Degraded);
        result.Details.Should().Be("Email service recovering. Queue: 3");
    }

    [Fact]
    [Trait("NFR", "T158")]
    public async Task CheckHealthAsync_CircuitHalfOpenWithQueue_IncludesQueueInformation()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();
        var emailHealth = new EmailServiceHealth
        {
            CircuitState = CircuitState.HalfOpen,
            IsHealthy = false,
            QueuedEmails = 10,
            FailureCount = 3,
            LastSuccessAt = DateTime.UtcNow.AddMinutes(-2)
        };

        _mockResilientEmailService.GetHealth().Returns(emailHealth);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.Should().Be(ServiceStatus.Degraded);
        result.Details.Should().Contain("recovering");
        result.Details.Should().Contain("10");
    }

    #endregion

    #region Unhealthy Scenarios

    [Fact]
    [Trait("NFR", "T159")]
    public async Task CheckHealthAsync_CircuitOpen_ReturnsUnhealthyStatus()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();
        var emailHealth = new EmailServiceHealth
        {
            CircuitState = CircuitState.Open,
            IsHealthy = false,
            QueuedEmails = 15,
            FailureCount = 5,
            LastSuccessAt = DateTime.UtcNow.AddMinutes(-10),
            CircuitOpenedAt = DateTime.UtcNow.AddMinutes(-1)
        };

        _mockResilientEmailService.GetHealth().Returns(emailHealth);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.IsHealthy.Should().BeFalse();
        result.Status.Should().Be(ServiceStatus.Unhealthy);
        result.Details.Should().Be("Email service unavailable. Failures: 5");
    }

    [Fact]
    [Trait("NFR", "T159")]
    public async Task CheckHealthAsync_ServiceNotConfigured_ReturnsUnhealthyStatus()
    {
        // Arrange
        var healthCheck = new EmailHealthCheck(null, _mockLogger);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.IsHealthy.Should().BeFalse();
        result.Status.Should().Be(ServiceStatus.Unhealthy);
        result.Details.Should().Be("Email service not configured");
        result.ErrorMessage.Should().Be("IResilientEmailService not available");
    }

    #endregion

    #region Exception Handling

    [Fact]
    [Trait("NFR", "T160")]
    public async Task CheckHealthAsync_ServiceThrowsException_ReturnsUnhealthyWithError()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();
        var expectedException = new InvalidOperationException("Email service error");

        _mockResilientEmailService.GetHealth()
            .Throws(expectedException);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.IsHealthy.Should().BeFalse();
        result.Status.Should().Be(ServiceStatus.Unhealthy);
        result.Details.Should().Be("Email health check failed");
        result.ErrorMessage.Should().Be("Email service error");
    }

    [Fact]
    [Trait("NFR", "T160")]
    public async Task CheckHealthAsync_NullReferenceException_ReturnsUnhealthyWithError()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();
        var nullRefException = new NullReferenceException("Null reference in email service");

        _mockResilientEmailService.GetHealth()
            .Throws(nullRefException);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.IsHealthy.Should().BeFalse();
        result.Status.Should().Be(ServiceStatus.Unhealthy);
        result.ErrorMessage.Should().Be("Null reference in email service");
    }

    [Fact]
    [Trait("NFR", "T160")]
    public async Task CheckHealthAsync_TimeoutException_ReturnsUnhealthyWithTimeout()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();
        var timeoutException = new TimeoutException("Health check timeout");

        _mockResilientEmailService.GetHealth()
            .Throws(timeoutException);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.IsHealthy.Should().BeFalse();
        result.Status.Should().Be(ServiceStatus.Unhealthy);
        result.ErrorMessage.Should().Be("Health check timeout");
    }

    #endregion

    #region Cancellation

    [Fact]
    [Trait("NFR", "T161")]
    public async Task CheckHealthAsync_CancellationToken_PassedToCheck()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();
        using var cts = new CancellationTokenSource();
        var emailHealth = new EmailServiceHealth
        {
            CircuitState = CircuitState.Closed,
            IsHealthy = true,
            QueuedEmails = 0,
            FailureCount = 0,
            LastSuccessAt = DateTime.UtcNow
        };

        _mockResilientEmailService.GetHealth().Returns(emailHealth);

        // Act
        var result = await healthCheck.CheckHealthAsync(cts.Token);

        // Assert
        result.IsHealthy.Should().BeTrue();
        _mockResilientEmailService.Received(1).GetHealth();
    }

    [Fact]
    [Trait("NFR", "T161")]
    public async Task CheckHealthAsync_ServiceNotConfigured_CancellationHandledGracefully()
    {
        // Arrange
        var healthCheck = new EmailHealthCheck(null, _mockLogger);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await healthCheck.CheckHealthAsync(cts.Token);

        // Assert
        result.IsHealthy.Should().BeFalse();
        result.Status.Should().Be(ServiceStatus.Unhealthy);
        result.Details.Should().Be("Email service not configured");
    }

    #endregion

    #region Response Time Tracking

    [Fact]
    [Trait("NFR", "T162")]
    public async Task CheckHealthAsync_TracksResponseTime_ReturnsAccurateTimings()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();
        var emailHealth = new EmailServiceHealth
        {
            CircuitState = CircuitState.Closed,
            IsHealthy = true,
            QueuedEmails = 0,
            FailureCount = 0,
            LastSuccessAt = DateTime.UtcNow
        };

        _mockResilientEmailService.GetHealth().Returns(emailHealth);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.ResponseTimeMs.Should().BeGreaterThanOrEqualTo(0);
        result.ResponseTimeMs.Should().BeLessThan(1000); // Should be very fast for GetHealth
    }

    [Fact]
    [Trait("NFR", "T162")]
    public async Task CheckHealthAsync_ExceptionDuringCheck_StillTracksResponseTime()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();
        _mockResilientEmailService.GetHealth()
            .Throws(new InvalidOperationException("Test error"));

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.ResponseTimeMs.Should().BeGreaterThanOrEqualTo(0);
        result.IsHealthy.Should().BeFalse();
    }

    #endregion

    #region Logging Verification

    [Fact]
    [Trait("NFR", "T163")]
    public async Task CheckHealthAsync_SuccessfulCheck_LogsDebugMessage()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();
        var emailHealth = new EmailServiceHealth
        {
            CircuitState = CircuitState.Closed,
            IsHealthy = true,
            QueuedEmails = 0,
            FailureCount = 0,
            LastSuccessAt = DateTime.UtcNow
        };

        _mockResilientEmailService.GetHealth().Returns(emailHealth);

        // Act
        await healthCheck.CheckHealthAsync();

        // Assert
        _mockLogger.Received(1).Log(
            LogLevel.Debug,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    [Trait("NFR", "T163")]
    public async Task CheckHealthAsync_Exception_LogsError()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();
        var exception = new InvalidOperationException("Test error");

        _mockResilientEmailService.GetHealth()
            .Throws(exception);

        // Act
        await healthCheck.CheckHealthAsync();

        // Assert
        _mockLogger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            exception,
            Arg.Any<Func<object, Exception?, string>>());
    }

    #endregion

    #region Circuit State Scenarios

    [Theory]
    [Trait("NFR", "T164")]
    [InlineData(CircuitState.Closed, ServiceStatus.Healthy)]
    [InlineData(CircuitState.HalfOpen, ServiceStatus.Degraded)]
    [InlineData(CircuitState.Open, ServiceStatus.Unhealthy)]
    public async Task CheckHealthAsync_VariousCircuitStates_ReturnsCorrectStatus(
        CircuitState circuitState,
        ServiceStatus expectedStatus)
    {
        // Arrange
        var healthCheck = CreateHealthCheck();
        var emailHealth = new EmailServiceHealth
        {
            CircuitState = circuitState,
            IsHealthy = circuitState == CircuitState.Closed,
            QueuedEmails = 0,
            FailureCount = 0,
            LastSuccessAt = DateTime.UtcNow
        };

        _mockResilientEmailService.GetHealth().Returns(emailHealth);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.Should().Be(expectedStatus);
    }

    [Fact]
    [Trait("NFR", "T164")]
    public async Task CheckHealthAsync_HighQueueCount_ReflectedInDetails()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();
        var emailHealth = new EmailServiceHealth
        {
            CircuitState = CircuitState.Closed,
            IsHealthy = true,
            QueuedEmails = 50,
            FailureCount = 0,
            LastSuccessAt = DateTime.UtcNow
        };

        _mockResilientEmailService.GetHealth().Returns(emailHealth);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Details.Should().Contain("50");
        result.IsHealthy.Should().BeTrue();
    }

    #endregion

    #region Edge Cases

    [Fact]
    [Trait("NFR", "T165")]
    public async Task CheckHealthAsync_MultipleConsecutiveCalls_EachReturnsIndependentResults()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();
        var emailHealth = new EmailServiceHealth
        {
            CircuitState = CircuitState.Closed,
            IsHealthy = true,
            QueuedEmails = 0,
            FailureCount = 0,
            LastSuccessAt = DateTime.UtcNow
        };

        _mockResilientEmailService.GetHealth().Returns(emailHealth);

        // Act
        var result1 = await healthCheck.CheckHealthAsync();
        var result2 = await healthCheck.CheckHealthAsync();
        var result3 = await healthCheck.CheckHealthAsync();

        // Assert
        result1.Should().NotBeSameAs(result2);
        result2.Should().NotBeSameAs(result3);
        _mockResilientEmailService.Received(3).GetHealth();
    }

    [Fact]
    [Trait("NFR", "T165")]
    public async Task CheckHealthAsync_RapidSuccessiveCalls_AllComplete()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();
        var emailHealth = new EmailServiceHealth
        {
            CircuitState = CircuitState.Closed,
            IsHealthy = true,
            QueuedEmails = 0,
            FailureCount = 0,
            LastSuccessAt = DateTime.UtcNow
        };

        _mockResilientEmailService.GetHealth().Returns(emailHealth);

        // Act
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => healthCheck.CheckHealthAsync())
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(10);
        results.Should().OnlyContain(r => r.IsHealthy);
    }

    [Fact]
    [Trait("NFR", "T165")]
    public async Task CheckHealthAsync_ZeroFailuresOpenCircuit_ReturnsUnhealthy()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();
        var emailHealth = new EmailServiceHealth
        {
            CircuitState = CircuitState.Open,
            IsHealthy = false,
            QueuedEmails = 0,
            FailureCount = 0, // Edge case: open circuit with no failures
            LastSuccessAt = DateTime.UtcNow.AddHours(-1),
            CircuitOpenedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        _mockResilientEmailService.GetHealth().Returns(emailHealth);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.IsHealthy.Should().BeFalse();
        result.Status.Should().Be(ServiceStatus.Unhealthy);
        result.Details.Should().Contain("Failures: 0");
    }

    #endregion

    #region Synchronous Nature

    [Fact]
    [Trait("NFR", "T165")]
    public async Task CheckHealthAsync_ReturnsSynchronously_NoActualAsyncWork()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();
        var emailHealth = new EmailServiceHealth
        {
            CircuitState = CircuitState.Closed,
            IsHealthy = true,
            QueuedEmails = 0,
            FailureCount = 0,
            LastSuccessAt = DateTime.UtcNow
        };

        _mockResilientEmailService.GetHealth().Returns(emailHealth);

        // Act
        var task = healthCheck.CheckHealthAsync();

        // Assert
        task.IsCompleted.Should().BeTrue(); // Should complete synchronously
        var result = await task;
        result.IsHealthy.Should().BeTrue();
    }

    #endregion
}
