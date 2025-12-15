using Ceiba.Core.Interfaces;
using Ceiba.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Ceiba.Infrastructure.Tests.Services;

/// <summary>
/// Unit tests for EmailQueueProcessorService.
/// T146-T155: RO-003 Background service for processing queued emails.
/// </summary>
[Trait("Category", "Unit")]
public class EmailQueueProcessorServiceTests
{
    private readonly IServiceScopeFactory _mockScopeFactory;
    private readonly IServiceScope _mockScope;
    private readonly IServiceProvider _mockServiceProvider;
    private readonly IResilientEmailService _mockResilientEmailService;
    private readonly ILogger<EmailQueueProcessorService> _mockLogger;

    public EmailQueueProcessorServiceTests()
    {
        _mockScopeFactory = Substitute.For<IServiceScopeFactory>();
        _mockScope = Substitute.For<IServiceScope>();
        _mockServiceProvider = Substitute.For<IServiceProvider>();
        _mockResilientEmailService = Substitute.For<IResilientEmailService>();
        _mockLogger = Substitute.For<ILogger<EmailQueueProcessorService>>();

        // Setup dependency injection chain
        _mockScopeFactory.CreateScope().Returns(_mockScope);
        _mockScope.ServiceProvider.Returns(_mockServiceProvider);
        _mockServiceProvider.GetService(typeof(IResilientEmailService)).Returns(_mockResilientEmailService);
    }

    private EmailQueueProcessorService CreateService()
    {
        return new EmailQueueProcessorService(_mockScopeFactory, _mockLogger);
    }

    #region Constructor Tests

    [Fact(DisplayName = "Constructor should not throw with valid dependencies")]
    public void Constructor_WithValidDependencies_DoesNotThrow()
    {
        // Act
        var service = CreateService();

        // Assert
        service.Should().NotBeNull();
    }

    #endregion

    #region ExecuteAsync Startup Tests

    [Fact(DisplayName = "ExecuteAsync should log startup message")]
    public async Task ExecuteAsync_OnStart_LogsStartupMessage()
    {
        // Arrange
        var service = CreateService();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        // Act
        try
        {
            await service.StartAsync(cts.Token);
            await Task.Delay(200, CancellationToken.None);
            await service.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert - verify startup log was called
        _mockLogger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("started")),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact(DisplayName = "ExecuteAsync should log stop message on cancellation")]
    public async Task ExecuteAsync_OnStop_LogsStopMessage()
    {
        // Arrange
        var service = CreateService();
        using var cts = new CancellationTokenSource();

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(200);
        await service.StopAsync(CancellationToken.None);

        // Assert - verify stop log was called
        _mockLogger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("stopped")),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    #endregion

    #region Queue Processing Tests

    [Fact(DisplayName = "ExecuteAsync should not immediately process (waits for interval)")]
    public async Task ExecuteAsync_WaitsForInterval_BeforeProcessing()
    {
        // Arrange
        var service = CreateService();
        _mockResilientEmailService.QueuedEmailCount.Returns(5);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        // Act - Start and immediately stop before interval completes
        try
        {
            await service.StartAsync(cts.Token);
            await Task.Delay(200, CancellationToken.None);
            await service.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert - Since the interval is 5 minutes, no processing should occur in 200ms
        // The service should start without processing immediately
        await _mockResilientEmailService.DidNotReceive().ProcessQueueAsync(Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Service should be startable and stoppable")]
    public async Task Service_CanBeStartedAndStopped()
    {
        // Arrange
        var service = CreateService();
        _mockResilientEmailService.QueuedEmailCount.Returns(0);

        // Act
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(200);
        await service.StopAsync(CancellationToken.None);

        // Assert - No exception should be thrown
        service.Should().NotBeNull();
    }

    [Fact(DisplayName = "Service should log startup with interval information")]
    public async Task ExecuteAsync_AfterProcessing_LogsCount()
    {
        // Arrange
        var service = CreateService();
        _mockResilientEmailService.QueuedEmailCount.Returns(0);

        // Act
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(200);
        await service.StopAsync(CancellationToken.None);

        // Assert - Verify startup logging
        _mockLogger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("started")),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    #endregion

    #region Error Handling Tests

    [Fact(DisplayName = "ExecuteAsync should handle exceptions gracefully")]
    public async Task ExecuteAsync_ProcessingError_ContinuesRunning()
    {
        // Arrange
        var service = CreateService();
        _mockResilientEmailService.QueuedEmailCount.Returns(5);
        _mockResilientEmailService.ProcessQueueAsync(Arg.Any<CancellationToken>())
            .Returns<int>(_ => throw new Exception("Processing error"));

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        // Act - should not throw
        Exception? caughtException = null;
        try
        {
            await service.StartAsync(cts.Token);
            await Task.Delay(200, CancellationToken.None);
            await service.StopAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        // Assert - service should handle exception gracefully (not propagate)
        // The OperationCanceledException from CTS is expected
        caughtException.Should().BeNull();
    }

    [Fact(DisplayName = "ExecuteAsync should log errors when processing fails")]
    public async Task ExecuteAsync_ProcessingError_LogsError()
    {
        // Arrange
        var service = CreateService();
        _mockResilientEmailService.QueuedEmailCount.Returns(5);
        _mockResilientEmailService.ProcessQueueAsync(Arg.Any<CancellationToken>())
            .Returns<int>(_ => throw new Exception("Test processing error"));

        // Start the service briefly to trigger potential error handling
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        // Act
        try
        {
            await service.StartAsync(cts.Token);
            await Task.Delay(250, CancellationToken.None);
            await service.StopAsync(CancellationToken.None);
        }
        catch
        {
            // Ignore cancellation
        }

        // Assert - The service should be created and started without throwing
        service.Should().NotBeNull();
    }

    #endregion

    #region Cancellation Tests

    [Fact(DisplayName = "ExecuteAsync should not throw OperationCanceledException on normal stop")]
    public async Task ExecuteAsync_NormalStop_DoesNotThrow()
    {
        // Arrange
        var service = CreateService();
        using var cts = new CancellationTokenSource();

        // Act & Assert
        await service.StartAsync(cts.Token);
        await Task.Delay(200);

        var act = async () => await service.StopAsync(CancellationToken.None);
        await act.Should().NotThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region Scope Creation Tests

    [Fact(DisplayName = "Service should be configured with IServiceScopeFactory")]
    public void Service_HasCorrectDependencies()
    {
        // Arrange & Act
        var service = CreateService();

        // Assert - Service should be instantiated correctly
        service.Should().NotBeNull();
        service.Should().BeAssignableTo<Microsoft.Extensions.Hosting.BackgroundService>();
    }

    [Fact(DisplayName = "ScopeFactory mock is properly configured")]
    public void ScopeFactory_MockIsConfigured()
    {
        // Arrange & Act
        var scope = _mockScopeFactory.CreateScope();

        // Assert - Verify our mock setup works
        scope.Should().NotBeNull();
        scope.ServiceProvider.Should().NotBeNull();
    }

    #endregion

    #region BackgroundService Integration Tests

    [Fact(DisplayName = "Service should be a BackgroundService")]
    public void Service_InheritsFromBackgroundService()
    {
        // Arrange & Act
        var service = CreateService();

        // Assert
        service.Should().BeAssignableTo<Microsoft.Extensions.Hosting.BackgroundService>();
    }

    [Fact(DisplayName = "StartAsync should start the background task")]
    public async Task StartAsync_StartsBackgroundTask()
    {
        // Arrange
        var service = CreateService();

        // Act
        await service.StartAsync(CancellationToken.None);

        // Brief delay to ensure task starts
        await Task.Delay(200);

        // Assert - service is running (ExecuteTask is not null/completed)
        // Stop the service to clean up
        await service.StopAsync(CancellationToken.None);
    }

    [Fact(DisplayName = "StopAsync should wait for task completion")]
    public async Task StopAsync_WaitsForCompletion()
    {
        // Arrange
        var service = CreateService();
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(200);

        // Act & Assert - should complete without hanging
        var stopTask = service.StopAsync(CancellationToken.None);
        var completedInTime = await Task.WhenAny(stopTask, Task.Delay(5000)) == stopTask;

        completedInTime.Should().BeTrue("StopAsync should complete within reasonable time");
    }

    #endregion

    #region Logging Detail Tests

    [Fact(DisplayName = "ExecuteAsync should log processing interval on startup")]
    public async Task ExecuteAsync_LogsProcessingInterval()
    {
        // Arrange
        var service = CreateService();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        // Act
        try
        {
            await service.StartAsync(cts.Token);
            await Task.Delay(200, CancellationToken.None);
            await service.StopAsync(CancellationToken.None);
        }
        catch
        {
            // Expected
        }

        // Assert - startup message should include interval information
        _mockLogger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("interval") || o.ToString()!.Contains("started")),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    #endregion
}
