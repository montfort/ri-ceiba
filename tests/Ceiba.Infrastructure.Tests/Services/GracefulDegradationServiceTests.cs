using Ceiba.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Ceiba.Infrastructure.Tests.Services;

/// <summary>
/// Unit tests for GracefulDegradationService.
/// Tests service state tracking, failure recording, and fallback execution.
/// </summary>
public class GracefulDegradationServiceTests
{
    private readonly Mock<ILogger<GracefulDegradationService>> _loggerMock;
    private readonly GracefulDegradationService _service;

    public GracefulDegradationServiceTests()
    {
        _loggerMock = new Mock<ILogger<GracefulDegradationService>>();
        _service = new GracefulDegradationService(_loggerMock.Object);
    }

    #region RecordFailure Tests

    [Fact]
    public void RecordFailure_FirstFailure_ServiceNotDegraded()
    {
        // Act
        _service.RecordFailure("TestService");

        // Assert
        Assert.False(_service.IsServiceDegraded("TestService"));
    }

    [Fact]
    public void RecordFailure_TwoFailures_ServiceNotDegraded()
    {
        // Act
        _service.RecordFailure("TestService");
        _service.RecordFailure("TestService");

        // Assert
        Assert.False(_service.IsServiceDegraded("TestService"));
    }

    [Fact]
    public void RecordFailure_ThreeConsecutiveFailures_ServiceBecomesDegraded()
    {
        // Act
        _service.RecordFailure("TestService");
        _service.RecordFailure("TestService");
        _service.RecordFailure("TestService");

        // Assert
        Assert.True(_service.IsServiceDegraded("TestService"));
    }

    [Fact]
    public void RecordFailure_UpdatesLastFailureTime()
    {
        // Act
        var beforeRecord = DateTime.UtcNow;
        _service.RecordFailure("TestService");
        var afterRecord = DateTime.UtcNow;

        // Assert
        var state = _service.GetServiceState("TestService");
        Assert.NotNull(state);
        Assert.NotNull(state.LastFailureAt);
        Assert.True(state.LastFailureAt >= beforeRecord && state.LastFailureAt <= afterRecord);
    }

    [Fact]
    public void RecordFailure_IncrementsConsecutiveFailures()
    {
        // Act
        _service.RecordFailure("TestService");
        var state1 = _service.GetServiceState("TestService");

        _service.RecordFailure("TestService");
        var state2 = _service.GetServiceState("TestService");

        // Assert
        Assert.Equal(1, state1!.ConsecutiveFailures);
        Assert.Equal(2, state2!.ConsecutiveFailures);
    }

    [Fact]
    public void RecordFailure_MultipleServices_IndependentTracking()
    {
        // Act
        _service.RecordFailure("ServiceA");
        _service.RecordFailure("ServiceA");
        _service.RecordFailure("ServiceA");
        _service.RecordFailure("ServiceB");

        // Assert
        Assert.True(_service.IsServiceDegraded("ServiceA"));
        Assert.False(_service.IsServiceDegraded("ServiceB"));
    }

    #endregion

    #region RecordSuccess Tests

    [Fact]
    public void RecordSuccess_ResetsConsecutiveFailures()
    {
        // Arrange
        _service.RecordFailure("TestService");
        _service.RecordFailure("TestService");

        // Act
        _service.RecordSuccess("TestService");

        // Assert
        var state = _service.GetServiceState("TestService");
        Assert.Equal(0, state!.ConsecutiveFailures);
    }

    [Fact]
    public void RecordSuccess_ResetsDegradedState()
    {
        // Arrange
        _service.RecordFailure("TestService");
        _service.RecordFailure("TestService");
        _service.RecordFailure("TestService");
        Assert.True(_service.IsServiceDegraded("TestService"));

        // Act
        _service.RecordSuccess("TestService");

        // Assert
        Assert.False(_service.IsServiceDegraded("TestService"));
    }

    [Fact]
    public void RecordSuccess_UpdatesLastSuccessTime()
    {
        // Act
        var beforeRecord = DateTime.UtcNow;
        _service.RecordSuccess("TestService");
        var afterRecord = DateTime.UtcNow;

        // Assert
        var state = _service.GetServiceState("TestService");
        Assert.NotNull(state);
        Assert.NotNull(state.LastSuccessAt);
        Assert.True(state.LastSuccessAt >= beforeRecord && state.LastSuccessAt <= afterRecord);
    }

    [Fact]
    public void RecordSuccess_NewService_CreatesState()
    {
        // Act
        _service.RecordSuccess("NewService");

        // Assert
        var state = _service.GetServiceState("NewService");
        Assert.NotNull(state);
        Assert.Equal("NewService", state.ServiceName);
        Assert.Equal(0, state.ConsecutiveFailures);
        Assert.False(state.IsDegraded);
    }

    #endregion

    #region IsServiceDegraded Tests

    [Fact]
    public void IsServiceDegraded_UnknownService_ReturnsFalse()
    {
        // Act
        var result = _service.IsServiceDegraded("UnknownService");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsServiceDegraded_DegradedService_ReturnsTrue()
    {
        // Arrange
        _service.RecordFailure("TestService");
        _service.RecordFailure("TestService");
        _service.RecordFailure("TestService");

        // Act & Assert
        Assert.True(_service.IsServiceDegraded("TestService"));
    }

    #endregion

    #region GetServiceState Tests

    [Fact]
    public void GetServiceState_UnknownService_ReturnsNull()
    {
        // Act
        var result = _service.GetServiceState("UnknownService");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetServiceState_ReturnsClone()
    {
        // Arrange
        _service.RecordFailure("TestService");

        // Act
        var state1 = _service.GetServiceState("TestService");
        var state2 = _service.GetServiceState("TestService");

        // Assert
        Assert.NotSame(state1, state2);
        Assert.Equal(state1!.ConsecutiveFailures, state2!.ConsecutiveFailures);
    }

    #endregion

    #region GetAllServiceStates Tests

    [Fact]
    public void GetAllServiceStates_NoServices_ReturnsEmptyList()
    {
        // Act
        var result = _service.GetAllServiceStates();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetAllServiceStates_MultipleServices_ReturnsAll()
    {
        // Arrange
        _service.RecordFailure("ServiceA");
        _service.RecordSuccess("ServiceB");
        _service.RecordFailure("ServiceC");

        // Act
        var result = _service.GetAllServiceStates();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(result, s => s.ServiceName == "ServiceA");
        Assert.Contains(result, s => s.ServiceName == "ServiceB");
        Assert.Contains(result, s => s.ServiceName == "ServiceC");
    }

    [Fact]
    public void GetAllServiceStates_ReturnsClones()
    {
        // Arrange
        _service.RecordFailure("TestService");

        // Act
        var result1 = _service.GetAllServiceStates();
        var result2 = _service.GetAllServiceStates();

        // Assert
        Assert.NotSame(result1[0], result2[0]);
    }

    #endregion

    #region ExecuteWithFallbackAsync Tests

    [Fact]
    public async Task ExecuteWithFallbackAsync_ServiceNotDegraded_ExecutesOperation()
    {
        // Arrange
        var operationCalled = false;
        var fallbackCalled = false;

        // Act
        var result = await _service.ExecuteWithFallbackAsync(
            "TestService",
            async () => { operationCalled = true; return await Task.FromResult("operation"); },
            async () => { fallbackCalled = true; return await Task.FromResult("fallback"); });

        // Assert
        Assert.True(operationCalled);
        Assert.False(fallbackCalled);
        Assert.Equal("operation", result);
    }

    [Fact]
    public async Task ExecuteWithFallbackAsync_ServiceDegraded_ExecutesFallback()
    {
        // Arrange
        _service.RecordFailure("TestService");
        _service.RecordFailure("TestService");
        _service.RecordFailure("TestService");

        var operationCalled = false;
        var fallbackCalled = false;

        // Act
        var result = await _service.ExecuteWithFallbackAsync(
            "TestService",
            async () => { operationCalled = true; return await Task.FromResult("operation"); },
            async () => { fallbackCalled = true; return await Task.FromResult("fallback"); });

        // Assert
        Assert.False(operationCalled);
        Assert.True(fallbackCalled);
        Assert.Equal("fallback", result);
    }

    [Fact]
    public async Task ExecuteWithFallbackAsync_OperationThrows_ExecutesFallbackAndRecordsFailure()
    {
        // Arrange
        var fallbackCalled = false;

        // Act
        var result = await _service.ExecuteWithFallbackAsync(
            "TestService",
            () => throw new InvalidOperationException("Test error"),
            async () => { fallbackCalled = true; return await Task.FromResult("fallback"); });

        // Assert
        Assert.True(fallbackCalled);
        Assert.Equal("fallback", result);

        var state = _service.GetServiceState("TestService");
        Assert.Equal(1, state!.ConsecutiveFailures);
    }

    [Fact]
    public async Task ExecuteWithFallbackAsync_OperationSucceeds_RecordsSuccess()
    {
        // Arrange
        _service.RecordFailure("TestService");
        _service.RecordFailure("TestService");

        // Act
        await _service.ExecuteWithFallbackAsync(
            "TestService",
            async () => await Task.FromResult("success"),
            async () => await Task.FromResult("fallback"));

        // Assert
        var state = _service.GetServiceState("TestService");
        Assert.Equal(0, state!.ConsecutiveFailures);
    }

    #endregion

    #region ExecuteWithFallback (Synchronous) Tests

    [Fact]
    public void ExecuteWithFallback_ServiceNotDegraded_ExecutesOperation()
    {
        // Arrange
        var operationCalled = false;
        var fallbackCalled = false;

        // Act
        var result = _service.ExecuteWithFallback(
            "TestService",
            () => { operationCalled = true; return "operation"; },
            () => { fallbackCalled = true; return "fallback"; });

        // Assert
        Assert.True(operationCalled);
        Assert.False(fallbackCalled);
        Assert.Equal("operation", result);
    }

    [Fact]
    public void ExecuteWithFallback_ServiceDegraded_ExecutesFallback()
    {
        // Arrange
        _service.RecordFailure("TestService");
        _service.RecordFailure("TestService");
        _service.RecordFailure("TestService");

        var operationCalled = false;
        var fallbackCalled = false;

        // Act
        var result = _service.ExecuteWithFallback(
            "TestService",
            () => { operationCalled = true; return "operation"; },
            () => { fallbackCalled = true; return "fallback"; });

        // Assert
        Assert.False(operationCalled);
        Assert.True(fallbackCalled);
        Assert.Equal("fallback", result);
    }

    [Fact]
    public void ExecuteWithFallback_OperationThrows_ExecutesFallbackAndRecordsFailure()
    {
        // Arrange
        var fallbackCalled = false;

        // Act
        var result = _service.ExecuteWithFallback<string>(
            "TestService",
            () => throw new InvalidOperationException("Test error"),
            () => { fallbackCalled = true; return "fallback"; });

        // Assert
        Assert.True(fallbackCalled);
        Assert.Equal("fallback", result);

        var state = _service.GetServiceState("TestService");
        Assert.Equal(1, state!.ConsecutiveFailures);
    }

    [Fact]
    public void ExecuteWithFallback_OperationSucceeds_RecordsSuccess()
    {
        // Arrange
        _service.RecordFailure("TestService");
        _service.RecordFailure("TestService");

        // Act
        _service.ExecuteWithFallback(
            "TestService",
            () => "success",
            () => "fallback");

        // Assert
        var state = _service.GetServiceState("TestService");
        Assert.Equal(0, state!.ConsecutiveFailures);
    }

    #endregion

    #region ServiceDegradationState Tests

    [Fact]
    public void ServiceDegradationState_Clone_CreatesIndependentCopy()
    {
        // Arrange
        var original = new ServiceDegradationState
        {
            ServiceName = "TestService",
            ConsecutiveFailures = 5,
            IsDegraded = true,
            LastFailureAt = DateTime.UtcNow,
            LastSuccessAt = DateTime.UtcNow.AddHours(-1)
        };

        // Act
        var clone = original.Clone();
        original.ConsecutiveFailures = 10;

        // Assert
        Assert.Equal(5, clone.ConsecutiveFailures);
        Assert.NotSame(original, clone);
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task RecordFailure_ConcurrentCalls_ThreadSafe()
    {
        // Arrange
        var tasks = new List<Task>();
        var iterations = 100;

        // Act
        for (int i = 0; i < iterations; i++)
        {
            tasks.Add(Task.Run(() => _service.RecordFailure("ConcurrentService")));
        }

        await Task.WhenAll(tasks);

        // Assert
        var state = _service.GetServiceState("ConcurrentService");
        Assert.Equal(iterations, state!.ConsecutiveFailures);
    }

    [Fact]
    public async Task MixedOperations_ConcurrentCalls_ThreadSafe()
    {
        // Arrange
        var tasks = new List<Task>();

        // Act - Mix of failures and successes
        for (int i = 0; i < 50; i++)
        {
            tasks.Add(Task.Run(() => _service.RecordFailure("MixedService")));
            tasks.Add(Task.Run(() => _service.RecordSuccess("MixedService")));
        }

        await Task.WhenAll(tasks);

        // Assert - Should not throw and state should be accessible
        var state = _service.GetServiceState("MixedService");
        Assert.NotNull(state);
    }

    #endregion
}
