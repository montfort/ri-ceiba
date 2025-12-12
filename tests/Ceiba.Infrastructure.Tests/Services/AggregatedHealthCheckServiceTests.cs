using Ceiba.Core.Interfaces;
using Ceiba.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Ceiba.Infrastructure.Tests.Services;

/// <summary>
/// Unit tests for AggregatedHealthCheckService.
/// Tests health check aggregation across multiple services.
/// </summary>
public class AggregatedHealthCheckServiceTests
{
    private readonly Mock<ILogger<AggregatedHealthCheckService>> _loggerMock;

    public AggregatedHealthCheckServiceTests()
    {
        _loggerMock = new Mock<ILogger<AggregatedHealthCheckService>>();
    }

    private AggregatedHealthCheckService CreateService(IEnumerable<IServiceHealthCheck> healthChecks)
    {
        return new AggregatedHealthCheckService(healthChecks, _loggerMock.Object);
    }

    private Mock<IServiceHealthCheck> CreateHealthCheckMock(
        string serviceName,
        ServiceStatus status,
        bool isHealthy = true,
        string? errorMessage = null)
    {
        var mock = new Mock<IServiceHealthCheck>();
        mock.Setup(x => x.ServiceName).Returns(serviceName);
        mock.Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ServiceHealthStatus
            {
                ServiceName = serviceName,
                IsHealthy = isHealthy,
                Status = status,
                ErrorMessage = errorMessage
            });
        return mock;
    }

    #region CheckAllAsync Tests

    [Fact]
    public async Task CheckAllAsync_NoHealthChecks_ReturnsHealthyStatus()
    {
        // Arrange
        var service = CreateService(Array.Empty<IServiceHealthCheck>());

        // Act
        var result = await service.CheckAllAsync();

        // Assert
        Assert.Equal(ServiceStatus.Healthy, result.OverallStatus);
        Assert.True(result.IsHealthy);
        Assert.Empty(result.Services);
        Assert.Equal(0, result.TotalServices);
    }

    [Fact]
    public async Task CheckAllAsync_AllHealthy_ReturnsHealthyStatus()
    {
        // Arrange
        var healthChecks = new[]
        {
            CreateHealthCheckMock("Database", ServiceStatus.Healthy).Object,
            CreateHealthCheckMock("Email", ServiceStatus.Healthy).Object,
            CreateHealthCheckMock("AI", ServiceStatus.Healthy).Object
        };
        var service = CreateService(healthChecks);

        // Act
        var result = await service.CheckAllAsync();

        // Assert
        Assert.Equal(ServiceStatus.Healthy, result.OverallStatus);
        Assert.True(result.IsHealthy);
        Assert.Equal(3, result.TotalServices);
        Assert.Equal(3, result.HealthyServices);
        Assert.Equal(0, result.DegradedServices);
        Assert.Equal(0, result.UnhealthyServices);
    }

    [Fact]
    public async Task CheckAllAsync_OneDegraded_ReturnsDegradedStatus()
    {
        // Arrange
        var healthChecks = new[]
        {
            CreateHealthCheckMock("Database", ServiceStatus.Healthy).Object,
            CreateHealthCheckMock("Email", ServiceStatus.Degraded, true).Object,
            CreateHealthCheckMock("AI", ServiceStatus.Healthy).Object
        };
        var service = CreateService(healthChecks);

        // Act
        var result = await service.CheckAllAsync();

        // Assert
        Assert.Equal(ServiceStatus.Degraded, result.OverallStatus);
        Assert.True(result.IsHealthy); // Degraded is still considered healthy
        Assert.Equal(3, result.TotalServices);
        Assert.Equal(2, result.HealthyServices);
        Assert.Equal(1, result.DegradedServices);
        Assert.Equal(0, result.UnhealthyServices);
    }

    [Fact]
    public async Task CheckAllAsync_OneUnhealthy_ReturnsUnhealthyStatus()
    {
        // Arrange
        var healthChecks = new[]
        {
            CreateHealthCheckMock("Database", ServiceStatus.Healthy).Object,
            CreateHealthCheckMock("Email", ServiceStatus.Unhealthy, false, "Connection failed").Object,
            CreateHealthCheckMock("AI", ServiceStatus.Healthy).Object
        };
        var service = CreateService(healthChecks);

        // Act
        var result = await service.CheckAllAsync();

        // Assert
        Assert.Equal(ServiceStatus.Unhealthy, result.OverallStatus);
        Assert.False(result.IsHealthy);
        Assert.Equal(3, result.TotalServices);
        Assert.Equal(2, result.HealthyServices);
        Assert.Equal(0, result.DegradedServices);
        Assert.Equal(1, result.UnhealthyServices);
    }

    [Fact]
    public async Task CheckAllAsync_UnhealthyTakesPrecedenceOverDegraded()
    {
        // Arrange
        var healthChecks = new[]
        {
            CreateHealthCheckMock("Database", ServiceStatus.Degraded).Object,
            CreateHealthCheckMock("Email", ServiceStatus.Unhealthy, false).Object,
            CreateHealthCheckMock("AI", ServiceStatus.Healthy).Object
        };
        var service = CreateService(healthChecks);

        // Act
        var result = await service.CheckAllAsync();

        // Assert
        Assert.Equal(ServiceStatus.Unhealthy, result.OverallStatus);
        Assert.False(result.IsHealthy);
    }

    [Fact]
    public async Task CheckAllAsync_HealthCheckThrows_ReturnsUnhealthyForThatService()
    {
        // Arrange
        var throwingMock = new Mock<IServiceHealthCheck>();
        throwingMock.Setup(x => x.ServiceName).Returns("FailingService");
        throwingMock.Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Service unavailable"));

        var healthChecks = new[]
        {
            CreateHealthCheckMock("Database", ServiceStatus.Healthy).Object,
            throwingMock.Object
        };
        var service = CreateService(healthChecks);

        // Act
        var result = await service.CheckAllAsync();

        // Assert
        Assert.Equal(ServiceStatus.Unhealthy, result.OverallStatus);
        Assert.Equal(2, result.TotalServices);
        Assert.Equal(1, result.UnhealthyServices);

        var failingService = result.Services.FirstOrDefault(s => s.ServiceName == "FailingService");
        Assert.NotNull(failingService);
        Assert.False(failingService.IsHealthy);
        Assert.Contains("Service unavailable", failingService.ErrorMessage);
    }

    [Fact]
    public async Task CheckAllAsync_SetsCheckedAtTimestamp()
    {
        // Arrange
        var healthChecks = new[] { CreateHealthCheckMock("Test", ServiceStatus.Healthy).Object };
        var service = CreateService(healthChecks);

        // Act
        var beforeCheck = DateTime.UtcNow;
        var result = await service.CheckAllAsync();
        var afterCheck = DateTime.UtcNow;

        // Assert
        Assert.True(result.CheckedAt >= beforeCheck);
        Assert.True(result.CheckedAt <= afterCheck);
    }

    [Fact]
    public async Task CheckAllAsync_PassesCancellationToken()
    {
        // Arrange
        CancellationToken receivedToken = default;
        var healthCheck = new Mock<IServiceHealthCheck>();
        healthCheck.Setup(x => x.ServiceName).Returns("TestService");
        healthCheck.Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .Callback<CancellationToken>(ct => receivedToken = ct)
            .ReturnsAsync(new ServiceHealthStatus { ServiceName = "TestService", IsHealthy = true, Status = ServiceStatus.Healthy });

        var healthChecks = new[] { healthCheck.Object };
        var service = CreateService(healthChecks);

        using var cts = new CancellationTokenSource();

        // Act
        await service.CheckAllAsync(cts.Token);

        // Assert - Verify the cancellation token was passed through
        Assert.Equal(cts.Token, receivedToken);
    }

    #endregion

    #region CheckServiceAsync Tests

    [Fact]
    public async Task CheckServiceAsync_ServiceExists_ReturnsStatus()
    {
        // Arrange
        var healthChecks = new[]
        {
            CreateHealthCheckMock("Database", ServiceStatus.Healthy).Object,
            CreateHealthCheckMock("Email", ServiceStatus.Degraded).Object
        };
        var service = CreateService(healthChecks);

        // Act
        var result = await service.CheckServiceAsync("Database");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Database", result.ServiceName);
        Assert.Equal(ServiceStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckServiceAsync_ServiceNotFound_ReturnsNull()
    {
        // Arrange
        var healthChecks = new[]
        {
            CreateHealthCheckMock("Database", ServiceStatus.Healthy).Object
        };
        var service = CreateService(healthChecks);

        // Act
        var result = await service.CheckServiceAsync("NonExistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CheckServiceAsync_CaseInsensitiveSearch()
    {
        // Arrange
        var healthChecks = new[]
        {
            CreateHealthCheckMock("Database", ServiceStatus.Healthy).Object
        };
        var service = CreateService(healthChecks);

        // Act
        var result = await service.CheckServiceAsync("DATABASE");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Database", result.ServiceName);
    }

    #endregion

    #region AggregatedHealthStatus Tests

    [Fact]
    public void AggregatedHealthStatus_DefaultValues()
    {
        // Arrange & Act
        var status = new AggregatedHealthStatus();

        // Assert
        Assert.Equal(ServiceStatus.Healthy, status.OverallStatus);
        Assert.False(status.IsHealthy);
        Assert.Empty(status.Services);
        Assert.Equal(default, status.CheckedAt);
        Assert.Equal(0, status.TotalServices);
        Assert.Equal(0, status.HealthyServices);
        Assert.Equal(0, status.DegradedServices);
        Assert.Equal(0, status.UnhealthyServices);
    }

    [Fact]
    public async Task CheckAllAsync_AllUnhealthy_ReturnsCorrectCounts()
    {
        // Arrange
        var healthChecks = new[]
        {
            CreateHealthCheckMock("Service1", ServiceStatus.Unhealthy, false).Object,
            CreateHealthCheckMock("Service2", ServiceStatus.Unhealthy, false).Object
        };
        var service = CreateService(healthChecks);

        // Act
        var result = await service.CheckAllAsync();

        // Assert
        Assert.Equal(2, result.UnhealthyServices);
        Assert.Equal(0, result.HealthyServices);
        Assert.Equal(0, result.DegradedServices);
    }

    #endregion
}
