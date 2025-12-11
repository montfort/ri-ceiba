using Ceiba.Core.Interfaces;
using Ceiba.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using FluentAssertions;

namespace Ceiba.Infrastructure.Tests.Services;

/// <summary>
/// T156-T165: RO-004 Service resilience tests.
/// Tests health checks and graceful degradation functionality.
/// </summary>
[Trait("Category", "Unit")]
public class ServiceResilienceTests
{
    private readonly ILogger<GracefulDegradationService> _mockLogger;

    public ServiceResilienceTests()
    {
        _mockLogger = Substitute.For<ILogger<GracefulDegradationService>>();
    }

    #region T156: Graceful Degradation Basic

    [Fact]
    [Trait("NFR", "T156")]
    public void GracefulDegradation_InitialState_NotDegraded()
    {
        // Arrange
        var service = new GracefulDegradationService(_mockLogger);

        // Act & Assert
        service.IsServiceDegraded("TestService").Should().BeFalse();
    }

    [Fact]
    [Trait("NFR", "T156")]
    public void GracefulDegradation_RecordSuccess_StaysHealthy()
    {
        // Arrange
        var service = new GracefulDegradationService(_mockLogger);

        // Act
        service.RecordSuccess("TestService");
        service.RecordSuccess("TestService");

        // Assert
        service.IsServiceDegraded("TestService").Should().BeFalse();
    }

    #endregion

    #region T157: Degradation After Failures

    [Fact]
    [Trait("NFR", "T157")]
    public void GracefulDegradation_ThreeFailures_BecomesDegraded()
    {
        // Arrange
        var service = new GracefulDegradationService(_mockLogger);

        // Act
        service.RecordFailure("TestService");
        service.RecordFailure("TestService");
        service.RecordFailure("TestService");

        // Assert
        service.IsServiceDegraded("TestService").Should().BeTrue();
    }

    [Fact]
    [Trait("NFR", "T157")]
    public void GracefulDegradation_TwoFailures_NotYetDegraded()
    {
        // Arrange
        var service = new GracefulDegradationService(_mockLogger);

        // Act
        service.RecordFailure("TestService");
        service.RecordFailure("TestService");

        // Assert
        service.IsServiceDegraded("TestService").Should().BeFalse();
    }

    #endregion

    #region T158: Recovery From Degradation

    [Fact]
    [Trait("NFR", "T158")]
    public void GracefulDegradation_SuccessAfterDegradation_Recovers()
    {
        // Arrange
        var service = new GracefulDegradationService(_mockLogger);

        // Degrade the service
        service.RecordFailure("TestService");
        service.RecordFailure("TestService");
        service.RecordFailure("TestService");
        service.IsServiceDegraded("TestService").Should().BeTrue();

        // Act
        service.RecordSuccess("TestService");

        // Assert
        service.IsServiceDegraded("TestService").Should().BeFalse();
    }

    [Fact]
    [Trait("NFR", "T158")]
    public void GracefulDegradation_GetServiceState_ReturnsCorrectState()
    {
        // Arrange
        var service = new GracefulDegradationService(_mockLogger);

        // Act
        service.RecordFailure("TestService");
        service.RecordFailure("TestService");
        var state = service.GetServiceState("TestService");

        // Assert
        state.Should().NotBeNull();
        state!.ConsecutiveFailures.Should().Be(2);
        state.IsDegraded.Should().BeFalse();
        state.LastFailureAt.Should().NotBeNull();
    }

    #endregion

    #region T159: Execute With Fallback

    [Fact]
    [Trait("NFR", "T159")]
    public async Task ExecuteWithFallback_SuccessfulOperation_ReturnsResult()
    {
        // Arrange
        var service = new GracefulDegradationService(_mockLogger);

        // Act
        var result = await service.ExecuteWithFallbackAsync(
            "TestService",
            () => Task.FromResult("Success"),
            () => Task.FromResult("Fallback"));

        // Assert
        result.Should().Be("Success");
    }

    [Fact]
    [Trait("NFR", "T159")]
    public async Task ExecuteWithFallback_FailedOperation_ReturnsFallback()
    {
        // Arrange
        var service = new GracefulDegradationService(_mockLogger);

        // Act
        var result = await service.ExecuteWithFallbackAsync(
            "TestService",
            () => throw new Exception("Test failure"),
            () => Task.FromResult("Fallback"));

        // Assert
        result.Should().Be("Fallback");
    }

    [Fact]
    [Trait("NFR", "T159")]
    public async Task ExecuteWithFallback_DegradedService_UsesFallbackDirectly()
    {
        // Arrange
        var service = new GracefulDegradationService(_mockLogger);
        var operationCalled = false;

        // Degrade the service
        for (var i = 0; i < 3; i++)
            service.RecordFailure("TestService");

        // Act
        var result = await service.ExecuteWithFallbackAsync(
            "TestService",
            () =>
            {
                operationCalled = true;
                return Task.FromResult("Success");
            },
            () => Task.FromResult("Fallback"));

        // Assert
        result.Should().Be("Fallback");
        operationCalled.Should().BeFalse();
    }

    #endregion

    #region T160: Multiple Services

    [Fact]
    [Trait("NFR", "T160")]
    public void GracefulDegradation_MultipleServices_IndependentStates()
    {
        // Arrange
        var service = new GracefulDegradationService(_mockLogger);

        // Act - Degrade service A but not B
        for (var i = 0; i < 3; i++)
            service.RecordFailure("ServiceA");
        service.RecordSuccess("ServiceB");

        // Assert
        service.IsServiceDegraded("ServiceA").Should().BeTrue();
        service.IsServiceDegraded("ServiceB").Should().BeFalse();
    }

    [Fact]
    [Trait("NFR", "T160")]
    public void GracefulDegradation_GetAllStates_ReturnsAllServices()
    {
        // Arrange
        var service = new GracefulDegradationService(_mockLogger);

        // Act
        service.RecordFailure("ServiceA");
        service.RecordSuccess("ServiceB");
        var states = service.GetAllServiceStates();

        // Assert
        states.Should().HaveCount(2);
        states.Should().Contain(s => s.ServiceName == "ServiceA");
        states.Should().Contain(s => s.ServiceName == "ServiceB");
    }

    #endregion

    #region T161: Service State Tracking

    [Fact]
    [Trait("NFR", "T161")]
    public void GracefulDegradation_TracksLastFailureTime()
    {
        // Arrange
        var service = new GracefulDegradationService(_mockLogger);
        var before = DateTime.UtcNow;

        // Act
        service.RecordFailure("TestService");
        var state = service.GetServiceState("TestService");

        // Assert
        state!.LastFailureAt.Should().NotBeNull();
        state.LastFailureAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    [Trait("NFR", "T161")]
    public void GracefulDegradation_TracksLastSuccessTime()
    {
        // Arrange
        var service = new GracefulDegradationService(_mockLogger);
        var before = DateTime.UtcNow;

        // Act
        service.RecordSuccess("TestService");
        var state = service.GetServiceState("TestService");

        // Assert
        state!.LastSuccessAt.Should().NotBeNull();
        state.LastSuccessAt.Should().BeOnOrAfter(before);
    }

    #endregion

    #region T162: Synchronous Fallback

    [Fact]
    [Trait("NFR", "T162")]
    public void ExecuteWithFallback_Sync_SuccessfulOperation_ReturnsResult()
    {
        // Arrange
        var service = new GracefulDegradationService(_mockLogger);

        // Act
        var result = service.ExecuteWithFallback(
            "TestService",
            () => "Success",
            () => "Fallback");

        // Assert
        result.Should().Be("Success");
    }

    [Fact]
    [Trait("NFR", "T162")]
    public void ExecuteWithFallback_Sync_FailedOperation_ReturnsFallback()
    {
        // Arrange
        var service = new GracefulDegradationService(_mockLogger);

        // Act
        var result = service.ExecuteWithFallback<string>(
            "TestService",
            () => throw new Exception("Test failure"),
            () => "Fallback");

        // Assert
        result.Should().Be("Fallback");
    }

    #endregion

    #region T163: Service Health Status

    [Fact]
    [Trait("NFR", "T163")]
    public void ServiceHealthStatus_HealthyByDefault()
    {
        // Arrange & Act
        var status = new ServiceHealthStatus
        {
            ServiceName = "Test",
            IsHealthy = true,
            Status = ServiceStatus.Healthy
        };

        // Assert
        status.IsHealthy.Should().BeTrue();
        status.Status.Should().Be(ServiceStatus.Healthy);
    }

    [Fact]
    [Trait("NFR", "T163")]
    public void ServiceHealthStatus_RecordsResponseTime()
    {
        // Arrange & Act
        var status = new ServiceHealthStatus
        {
            ServiceName = "Test",
            IsHealthy = true,
            Status = ServiceStatus.Healthy,
            ResponseTimeMs = 150
        };

        // Assert
        status.ResponseTimeMs.Should().Be(150);
    }

    #endregion

    #region T164: Aggregated Health Check

    [Fact]
    [Trait("NFR", "T164")]
    public async Task AggregatedHealthCheck_AllHealthy_ReturnsHealthy()
    {
        // Arrange
        var healthCheck1 = Substitute.For<IServiceHealthCheck>();
        healthCheck1.ServiceName.Returns("Service1");
        healthCheck1.CheckHealthAsync(Arg.Any<CancellationToken>())
            .Returns(new ServiceHealthStatus
            {
                ServiceName = "Service1",
                IsHealthy = true,
                Status = ServiceStatus.Healthy
            });

        var healthCheck2 = Substitute.For<IServiceHealthCheck>();
        healthCheck2.ServiceName.Returns("Service2");
        healthCheck2.CheckHealthAsync(Arg.Any<CancellationToken>())
            .Returns(new ServiceHealthStatus
            {
                ServiceName = "Service2",
                IsHealthy = true,
                Status = ServiceStatus.Healthy
            });

        var logger = Substitute.For<ILogger<AggregatedHealthCheckService>>();
        var service = new AggregatedHealthCheckService(
            new[] { healthCheck1, healthCheck2 },
            logger);

        // Act
        var result = await service.CheckAllAsync();

        // Assert
        result.IsHealthy.Should().BeTrue();
        result.OverallStatus.Should().Be(ServiceStatus.Healthy);
        result.TotalServices.Should().Be(2);
        result.HealthyServices.Should().Be(2);
    }

    [Fact]
    [Trait("NFR", "T164")]
    public async Task AggregatedHealthCheck_OneUnhealthy_ReturnsUnhealthy()
    {
        // Arrange
        var healthCheck1 = Substitute.For<IServiceHealthCheck>();
        healthCheck1.ServiceName.Returns("Service1");
        healthCheck1.CheckHealthAsync(Arg.Any<CancellationToken>())
            .Returns(new ServiceHealthStatus
            {
                ServiceName = "Service1",
                IsHealthy = true,
                Status = ServiceStatus.Healthy
            });

        var healthCheck2 = Substitute.For<IServiceHealthCheck>();
        healthCheck2.ServiceName.Returns("Service2");
        healthCheck2.CheckHealthAsync(Arg.Any<CancellationToken>())
            .Returns(new ServiceHealthStatus
            {
                ServiceName = "Service2",
                IsHealthy = false,
                Status = ServiceStatus.Unhealthy
            });

        var logger = Substitute.For<ILogger<AggregatedHealthCheckService>>();
        var service = new AggregatedHealthCheckService(
            new[] { healthCheck1, healthCheck2 },
            logger);

        // Act
        var result = await service.CheckAllAsync();

        // Assert
        result.IsHealthy.Should().BeFalse();
        result.OverallStatus.Should().Be(ServiceStatus.Unhealthy);
        result.UnhealthyServices.Should().Be(1);
    }

    [Fact]
    [Trait("NFR", "T164")]
    public async Task AggregatedHealthCheck_OneDegraded_ReturnsDegraded()
    {
        // Arrange
        var healthCheck1 = Substitute.For<IServiceHealthCheck>();
        healthCheck1.ServiceName.Returns("Service1");
        healthCheck1.CheckHealthAsync(Arg.Any<CancellationToken>())
            .Returns(new ServiceHealthStatus
            {
                ServiceName = "Service1",
                IsHealthy = true,
                Status = ServiceStatus.Healthy
            });

        var healthCheck2 = Substitute.For<IServiceHealthCheck>();
        healthCheck2.ServiceName.Returns("Service2");
        healthCheck2.CheckHealthAsync(Arg.Any<CancellationToken>())
            .Returns(new ServiceHealthStatus
            {
                ServiceName = "Service2",
                IsHealthy = true,
                Status = ServiceStatus.Degraded
            });

        var logger = Substitute.For<ILogger<AggregatedHealthCheckService>>();
        var service = new AggregatedHealthCheckService(
            new[] { healthCheck1, healthCheck2 },
            logger);

        // Act
        var result = await service.CheckAllAsync();

        // Assert
        result.IsHealthy.Should().BeTrue(); // Degraded is still "healthy" enough
        result.OverallStatus.Should().Be(ServiceStatus.Degraded);
        result.DegradedServices.Should().Be(1);
    }

    #endregion

    #region T165: Check Specific Service

    [Fact]
    [Trait("NFR", "T165")]
    public async Task AggregatedHealthCheck_CheckService_ReturnsServiceStatus()
    {
        // Arrange
        var healthCheck = Substitute.For<IServiceHealthCheck>();
        healthCheck.ServiceName.Returns("TestService");
        healthCheck.CheckHealthAsync(Arg.Any<CancellationToken>())
            .Returns(new ServiceHealthStatus
            {
                ServiceName = "TestService",
                IsHealthy = true,
                Status = ServiceStatus.Healthy,
                Details = "All good"
            });

        var logger = Substitute.For<ILogger<AggregatedHealthCheckService>>();
        var service = new AggregatedHealthCheckService(
            new[] { healthCheck },
            logger);

        // Act
        var result = await service.CheckServiceAsync("TestService");

        // Assert
        result.Should().NotBeNull();
        result!.ServiceName.Should().Be("TestService");
        result.IsHealthy.Should().BeTrue();
        result.Details.Should().Be("All good");
    }

    [Fact]
    [Trait("NFR", "T165")]
    public async Task AggregatedHealthCheck_CheckUnknownService_ReturnsNull()
    {
        // Arrange
        var logger = Substitute.For<ILogger<AggregatedHealthCheckService>>();
        var service = new AggregatedHealthCheckService(
            Array.Empty<IServiceHealthCheck>(),
            logger);

        // Act
        var result = await service.CheckServiceAsync("UnknownService");

        // Assert
        result.Should().BeNull();
    }

    #endregion
}
