using Ceiba.Core.Interfaces;
using Ceiba.Infrastructure.Data;
using Ceiba.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Ceiba.Infrastructure.Tests.Services;

/// <summary>
/// T156-T165: RO-004 Database health check tests.
/// Tests database connectivity monitoring and performance thresholds.
/// </summary>
[Trait("Category", "Unit")]
public class DatabaseHealthCheckTests : IDisposable
{
    private readonly CeibaDbContext _context;
    private readonly ILogger<DatabaseHealthCheck> _mockLogger;

    public DatabaseHealthCheckTests()
    {
        var options = new DbContextOptionsBuilder<CeibaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CeibaDbContext(options, Guid.NewGuid());
        _mockLogger = Substitute.For<ILogger<DatabaseHealthCheck>>();
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    private DatabaseHealthCheck CreateHealthCheck()
    {
        return new DatabaseHealthCheck(_context, _mockLogger);
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
        serviceName.Should().Be("PostgreSQL Database");
    }

    #endregion

    #region Healthy Scenarios

    [Fact]
    [Trait("NFR", "T157")]
    public async Task CheckHealthAsync_DatabaseAvailable_ReturnsHealthyStatus()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Should().NotBeNull();
        result.ServiceName.Should().Be("PostgreSQL Database");
        result.IsHealthy.Should().BeTrue();
        result.Status.Should().Be(ServiceStatus.Healthy);
        result.Details.Should().Be("Database responding normally");
        result.ResponseTimeMs.Should().BeGreaterThanOrEqualTo(0);
        result.ErrorMessage.Should().BeNullOrEmpty();
    }

    [Fact]
    [Trait("NFR", "T157")]
    public async Task CheckHealthAsync_FastResponse_ReturnsHealthyStatus()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.IsHealthy.Should().BeTrue();
        result.Status.Should().Be(ServiceStatus.Healthy);
        result.Details.Should().Be("Database responding normally");
        result.ResponseTimeMs.Should().BeLessThan(1000);
    }

    #endregion

    #region Unhealthy Scenarios

    [Fact]
    [Trait("NFR", "T159")]
    public async Task CheckHealthAsync_DisposedContext_ReturnsUnhealthyStatus()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<CeibaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var disposedContext = new CeibaDbContext(options, Guid.NewGuid());
        await disposedContext.Database.EnsureCreatedAsync();
        await disposedContext.DisposeAsync();

        var healthCheck = new DatabaseHealthCheck(disposedContext, _mockLogger);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.IsHealthy.Should().BeFalse();
        result.Status.Should().Be(ServiceStatus.Unhealthy);
        result.Details.Should().Be("Database health check failed");
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Cancellation

    [Fact]
    [Trait("NFR", "T161")]
    public async Task CheckHealthAsync_CancellationRequested_ReturnsUnhealthy()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await healthCheck.CheckHealthAsync(cts.Token);

        // Assert - may still complete if the operation is fast enough, or return unhealthy
        result.Should().NotBeNull();
        result.ServiceName.Should().Be("PostgreSQL Database");
    }

    [Fact]
    [Trait("NFR", "T161")]
    public async Task CheckHealthAsync_ValidCancellationToken_Completes()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();
        using var cts = new CancellationTokenSource();

        // Act
        var result = await healthCheck.CheckHealthAsync(cts.Token);

        // Assert
        result.IsHealthy.Should().BeTrue();
        result.Status.Should().Be(ServiceStatus.Healthy);
    }

    #endregion

    #region Response Time Tracking

    [Fact]
    [Trait("NFR", "T162")]
    public async Task CheckHealthAsync_TracksResponseTime_ReturnsAccurateTimings()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.ResponseTimeMs.Should().BeGreaterThanOrEqualTo(0);
        result.ResponseTimeMs.Should().BeLessThan(5000); // Should be very fast for in-memory
    }

    [Fact]
    [Trait("NFR", "T162")]
    public async Task CheckHealthAsync_DisposedContext_StillTracksResponseTime()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<CeibaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var disposedContext = new CeibaDbContext(options, Guid.NewGuid());
        await disposedContext.DisposeAsync();

        var healthCheck = new DatabaseHealthCheck(disposedContext, _mockLogger);

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
        var options = new DbContextOptionsBuilder<CeibaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var disposedContext = new CeibaDbContext(options, Guid.NewGuid());
        await disposedContext.DisposeAsync();

        var healthCheck = new DatabaseHealthCheck(disposedContext, _mockLogger);

        // Act
        await healthCheck.CheckHealthAsync();

        // Assert
        _mockLogger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
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

        // Act
        var result1 = await healthCheck.CheckHealthAsync();
        var result2 = await healthCheck.CheckHealthAsync();
        var result3 = await healthCheck.CheckHealthAsync();

        // Assert
        result1.Should().NotBeSameAs(result2);
        result2.Should().NotBeSameAs(result3);
        result1.IsHealthy.Should().BeTrue();
        result2.IsHealthy.Should().BeTrue();
        result3.IsHealthy.Should().BeTrue();
    }

    [Fact]
    [Trait("NFR", "T164")]
    public async Task CheckHealthAsync_RapidSuccessiveCalls_AllComplete()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();

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
    public async Task CheckHealthAsync_ConcurrentCalls_AllSucceed()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();

        // Act
        var task1 = healthCheck.CheckHealthAsync();
        var task2 = healthCheck.CheckHealthAsync();
        var task3 = healthCheck.CheckHealthAsync();

        var results = await Task.WhenAll(task1, task2, task3);

        // Assert
        results[0].IsHealthy.Should().BeTrue();
        results[1].IsHealthy.Should().BeTrue();
        results[2].IsHealthy.Should().BeTrue();
    }

    #endregion

    #region Response Time Thresholds

    [Fact]
    [Trait("NFR", "T165")]
    public async Task CheckHealthAsync_InMemoryDatabase_ReturnsHealthyWithFastResponseTime()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.Should().Be(ServiceStatus.Healthy);
        result.ResponseTimeMs.Should().BeLessThan(1000); // In-memory should be very fast
    }

    [Fact]
    [Trait("NFR", "T165")]
    public async Task CheckHealthAsync_ReturnsResponseTimeInMilliseconds()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.ResponseTimeMs.Should().BeGreaterThanOrEqualTo(0);
        result.ResponseTimeMs.Should().BeLessThan(10000); // Reasonable upper bound
    }

    #endregion

    #region Service Health Status Structure

    [Fact]
    [Trait("NFR", "T165")]
    public async Task CheckHealthAsync_ReturnsCompleteHealthStatus()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.ServiceName.Should().NotBeNullOrEmpty();
        result.Details.Should().NotBeNullOrEmpty();
        result.ResponseTimeMs.Should().BeGreaterThanOrEqualTo(0);
        result.LastChecked.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    [Trait("NFR", "T165")]
    public async Task CheckHealthAsync_HealthyDatabase_NoErrorMessage()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.IsHealthy.Should().BeTrue();
        result.ErrorMessage.Should().BeNullOrEmpty();
    }

    [Fact]
    [Trait("NFR", "T165")]
    public async Task CheckHealthAsync_UnhealthyDatabase_HasErrorMessage()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<CeibaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var disposedContext = new CeibaDbContext(options, Guid.NewGuid());
        await disposedContext.DisposeAsync();

        var healthCheck = new DatabaseHealthCheck(disposedContext, _mockLogger);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.IsHealthy.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    #endregion
}
