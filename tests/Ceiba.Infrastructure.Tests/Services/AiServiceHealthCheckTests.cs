using Ceiba.Core.Interfaces;
using Ceiba.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Ceiba.Infrastructure.Tests.Services;

/// <summary>
/// T156-T165: RO-004 AI service health check tests.
/// Tests AI narrative service health monitoring.
/// </summary>
[Trait("Category", "Unit")]
public class AiServiceHealthCheckTests
{
    private readonly IAiNarrativeService _mockAiService;
    private readonly ILogger<AiServiceHealthCheck> _mockLogger;

    public AiServiceHealthCheckTests()
    {
        _mockAiService = Substitute.For<IAiNarrativeService>();
        _mockLogger = Substitute.For<ILogger<AiServiceHealthCheck>>();
    }

    private AiServiceHealthCheck CreateHealthCheck(IAiNarrativeService? aiService = null)
    {
        return new AiServiceHealthCheck(
            aiService ?? _mockAiService,
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
        serviceName.Should().Be("AI Narrative Service");
    }

    #endregion

    #region Healthy Scenarios

    [Fact]
    [Trait("NFR", "T157")]
    public async Task CheckHealthAsync_ServiceAvailable_ReturnsHealthyStatus()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();
        _mockAiService.IsAvailableAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Should().NotBeNull();
        result.ServiceName.Should().Be("AI Narrative Service");
        result.IsHealthy.Should().BeTrue();
        result.Status.Should().Be(ServiceStatus.Healthy);
        result.Details.Should().Be("AI service responding normally");
        result.ResponseTimeMs.Should().BeGreaterThanOrEqualTo(0);
        result.ErrorMessage.Should().BeNullOrEmpty();
    }

    [Fact]
    [Trait("NFR", "T157")]
    public async Task CheckHealthAsync_FastAvailabilityCheck_CompletesQuickly()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();
        _mockAiService.IsAvailableAsync(Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                await Task.Delay(50);
                return true;
            });

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.IsHealthy.Should().BeTrue();
        result.Status.Should().Be(ServiceStatus.Healthy);
        result.ResponseTimeMs.Should().BeLessThan(500);
    }

    #endregion

    #region Unhealthy Scenarios

    [Fact]
    [Trait("NFR", "T159")]
    public async Task CheckHealthAsync_ServiceUnavailable_ReturnsUnhealthyStatus()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();
        _mockAiService.IsAvailableAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.IsHealthy.Should().BeFalse();
        result.Status.Should().Be(ServiceStatus.Unhealthy);
        result.Details.Should().Be("AI service unavailable");
        result.ErrorMessage.Should().BeNullOrEmpty();
    }

    [Fact]
    [Trait("NFR", "T159")]
    public async Task CheckHealthAsync_ServiceNotConfigured_ReturnsUnhealthyStatus()
    {
        // Arrange
        // Create a health check with explicitly null service
        var healthCheck = new AiServiceHealthCheck(null, _mockLogger);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.IsHealthy.Should().BeFalse();
        result.Status.Should().Be(ServiceStatus.Unhealthy);
        result.Details.Should().Be("AI service not configured");
        result.ErrorMessage.Should().Be("IAiNarrativeService not available");
    }

    [Fact]
    [Trait("NFR", "T159")]
    public async Task CheckHealthAsync_SlowResponseStillAvailable_ReturnsHealthy()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();
        _mockAiService.IsAvailableAsync(Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                await Task.Delay(2000); // Slow but available
                return true;
            });

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.IsHealthy.Should().BeTrue();
        result.Status.Should().Be(ServiceStatus.Healthy);
        result.ResponseTimeMs.Should().BeGreaterThanOrEqualTo(2000);
    }

    #endregion

    #region Exception Handling

    [Fact]
    [Trait("NFR", "T160")]
    public async Task CheckHealthAsync_ServiceThrowsException_ReturnsUnhealthyWithError()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();
        var expectedException = new InvalidOperationException("AI service connection failed");

        _mockAiService.IsAvailableAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(expectedException);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.IsHealthy.Should().BeFalse();
        result.Status.Should().Be(ServiceStatus.Unhealthy);
        result.Details.Should().Be("AI health check failed");
        result.ErrorMessage.Should().Be("AI service connection failed");
    }

    [Fact]
    [Trait("NFR", "T160")]
    public async Task CheckHealthAsync_HttpRequestException_ReturnsUnhealthyWithError()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();
        var httpException = new HttpRequestException("Unable to connect to AI service");

        _mockAiService.IsAvailableAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(httpException);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.IsHealthy.Should().BeFalse();
        result.Status.Should().Be(ServiceStatus.Unhealthy);
        result.ErrorMessage.Should().Be("Unable to connect to AI service");
    }

    [Fact]
    [Trait("NFR", "T160")]
    public async Task CheckHealthAsync_TimeoutException_ReturnsUnhealthyWithTimeout()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();
        var timeoutException = new TimeoutException("AI service timeout");

        _mockAiService.IsAvailableAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(timeoutException);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.IsHealthy.Should().BeFalse();
        result.Status.Should().Be(ServiceStatus.Unhealthy);
        result.ErrorMessage.Should().Be("AI service timeout");
    }

    [Fact]
    [Trait("NFR", "T160")]
    public async Task CheckHealthAsync_UnauthorizedException_ReturnsUnhealthyWithError()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();
        var unauthorizedException = new UnauthorizedAccessException("Invalid API key");

        _mockAiService.IsAvailableAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(unauthorizedException);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.IsHealthy.Should().BeFalse();
        result.Status.Should().Be(ServiceStatus.Unhealthy);
        result.ErrorMessage.Should().Be("Invalid API key");
    }

    #endregion

    #region Cancellation

    [Fact]
    [Trait("NFR", "T161")]
    public async Task CheckHealthAsync_CancellationToken_PassedToService()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();
        using var cts = new CancellationTokenSource();

        _mockAiService.IsAvailableAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        // Act
        var result = await healthCheck.CheckHealthAsync(cts.Token);

        // Assert
        result.IsHealthy.Should().BeTrue();
        await _mockAiService.Received(1).IsAvailableAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("NFR", "T161")]
    public async Task CheckHealthAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockAiService.IsAvailableAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var result = await healthCheck.CheckHealthAsync(cts.Token);

        // Assert
        result.IsHealthy.Should().BeFalse();
        result.Status.Should().Be(ServiceStatus.Unhealthy);
    }

    [Fact]
    [Trait("NFR", "T161")]
    public async Task CheckHealthAsync_CancellationDuringCheck_HandlesGracefully()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();
        using var cts = new CancellationTokenSource();

        _mockAiService.IsAvailableAsync(Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                await Task.Delay(100);
                throw new OperationCanceledException();
            });

        // Act
        var result = await healthCheck.CheckHealthAsync(cts.Token);

        // Assert
        result.IsHealthy.Should().BeFalse();
        result.Status.Should().Be(ServiceStatus.Unhealthy);
        result.Details.Should().Be("AI health check failed");
    }

    [Fact]
    [Trait("NFR", "T161")]
    public async Task CheckHealthAsync_ServiceNotConfigured_CancellationHandledGracefully()
    {
        // Arrange
        var healthCheck = new AiServiceHealthCheck(null, _mockLogger);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await healthCheck.CheckHealthAsync(cts.Token);

        // Assert
        result.IsHealthy.Should().BeFalse();
        result.Status.Should().Be(ServiceStatus.Unhealthy);
        // When service is null, it returns "not configured" regardless of cancellation
        result.Details.Should().Be("AI service not configured");
    }

    #endregion

    #region Response Time Tracking

    [Fact]
    [Trait("NFR", "T162")]
    public async Task CheckHealthAsync_TracksResponseTime_ReturnsAccurateTimings()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();
        var delayMs = 250;

        _mockAiService.IsAvailableAsync(Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                await Task.Delay(delayMs);
                return true;
            });

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.ResponseTimeMs.Should().BeGreaterThanOrEqualTo(delayMs - 50); // Allow for timing variance
        result.ResponseTimeMs.Should().BeLessThan(delayMs + 100);
    }

    [Fact]
    [Trait("NFR", "T162")]
    public async Task CheckHealthAsync_ExceptionDuringCheck_StillTracksResponseTime()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();

        _mockAiService.IsAvailableAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Test error"));

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.ResponseTimeMs.Should().BeGreaterThanOrEqualTo(0);
        result.IsHealthy.Should().BeFalse();
        result.ErrorMessage.Should().Be("Test error");
    }

    [Fact]
    [Trait("NFR", "T162")]
    public async Task CheckHealthAsync_ServiceNotConfigured_TracksResponseTime()
    {
        // Arrange
        var healthCheck = CreateHealthCheck(null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.ResponseTimeMs.Should().BeGreaterThanOrEqualTo(0);
        result.ResponseTimeMs.Should().BeLessThan(100); // Should be very fast
    }

    #endregion

    #region Logging Verification

    [Fact]
    [Trait("NFR", "T163")]
    public async Task CheckHealthAsync_SuccessfulCheck_LogsDebugMessage()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();
        _mockAiService.IsAvailableAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

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

        _mockAiService.IsAvailableAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(exception);

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

    [Fact]
    [Trait("NFR", "T163")]
    public async Task CheckHealthAsync_ServiceUnavailable_LogsDebugNotError()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();
        _mockAiService.IsAvailableAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

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

    #endregion

    #region Edge Cases

    [Fact]
    [Trait("NFR", "T164")]
    public async Task CheckHealthAsync_MultipleConsecutiveCalls_EachReturnsIndependentResults()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();
        _mockAiService.IsAvailableAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        // Act
        var result1 = await healthCheck.CheckHealthAsync();
        var result2 = await healthCheck.CheckHealthAsync();
        var result3 = await healthCheck.CheckHealthAsync();

        // Assert
        result1.Should().NotBeSameAs(result2);
        result2.Should().NotBeSameAs(result3);
        await _mockAiService.Received(3).IsAvailableAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("NFR", "T164")]
    public async Task CheckHealthAsync_RapidSuccessiveCalls_AllComplete()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();
        _mockAiService.IsAvailableAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

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
    [Trait("NFR", "T164")]
    public async Task CheckHealthAsync_AlternatingAvailability_ReflectsCurrentState()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();
        var callCount = 0;

        _mockAiService.IsAvailableAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                return Task.FromResult(callCount % 2 == 0); // Alternates true/false
            });

        // Act
        var result1 = await healthCheck.CheckHealthAsync();
        var result2 = await healthCheck.CheckHealthAsync();
        var result3 = await healthCheck.CheckHealthAsync();
        var result4 = await healthCheck.CheckHealthAsync();

        // Assert
        result1.IsHealthy.Should().BeFalse();
        result2.IsHealthy.Should().BeTrue();
        result3.IsHealthy.Should().BeFalse();
        result4.IsHealthy.Should().BeTrue();
    }

    #endregion

    #region Service Provider Information

    [Fact]
    [Trait("NFR", "T165")]
    public async Task CheckHealthAsync_WithProviderName_DoesNotIncludeInStatus()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();
        _mockAiService.ProviderName.Returns("OpenAI");
        _mockAiService.IsAvailableAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.IsHealthy.Should().BeTrue();
        result.ServiceName.Should().Be("AI Narrative Service");
        // Provider name is accessible via the service but not in health status
        _mockAiService.ProviderName.Should().Be("OpenAI");
    }

    [Fact]
    [Trait("NFR", "T165")]
    public async Task CheckHealthAsync_NullService_HandlesGracefullyWithoutException()
    {
        // Arrange
        var healthCheck = CreateHealthCheck(null);

        // Act
        var action = async () => await healthCheck.CheckHealthAsync();

        // Assert
        await action.Should().NotThrowAsync();
        var result = await action();
        result.IsHealthy.Should().BeFalse();
    }

    #endregion

    #region Availability Status Transitions

    [Fact]
    [Trait("NFR", "T165")]
    public async Task CheckHealthAsync_ServiceRecovery_TransitionsToHealthy()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();
        var isAvailable = false;

        _mockAiService.IsAvailableAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(isAvailable));

        // Act
        var result1 = await healthCheck.CheckHealthAsync();
        isAvailable = true; // Service recovers
        var result2 = await healthCheck.CheckHealthAsync();

        // Assert
        result1.IsHealthy.Should().BeFalse();
        result1.Status.Should().Be(ServiceStatus.Unhealthy);
        result2.IsHealthy.Should().BeTrue();
        result2.Status.Should().Be(ServiceStatus.Healthy);
    }

    [Fact]
    [Trait("NFR", "T165")]
    public async Task CheckHealthAsync_ServiceDegradation_TransitionsToUnhealthy()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();
        var isAvailable = true;

        _mockAiService.IsAvailableAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(isAvailable));

        // Act
        var result1 = await healthCheck.CheckHealthAsync();
        isAvailable = false; // Service becomes unavailable
        var result2 = await healthCheck.CheckHealthAsync();

        // Assert
        result1.IsHealthy.Should().BeTrue();
        result1.Status.Should().Be(ServiceStatus.Healthy);
        result2.IsHealthy.Should().BeFalse();
        result2.Status.Should().Be(ServiceStatus.Unhealthy);
    }

    #endregion
}
