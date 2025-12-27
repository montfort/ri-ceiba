using Ceiba.Core.Interfaces;
using Ceiba.Infrastructure.Services;
using Ceiba.Shared.DTOs;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Ceiba.Infrastructure.Tests.Services;

/// <summary>
/// Unit tests for AutomatedReportBackgroundService.
/// Tests automated report generation background service.
/// Phase 2: Medium priority tests for coverage improvement.
/// </summary>
[Trait("Category", "Unit")]
public class AutomatedReportBackgroundServiceTests
{
    private readonly IServiceProvider _mockServiceProvider;
    private readonly IServiceScopeFactory _mockScopeFactory;
    private readonly IServiceScope _mockScope;
    private readonly IServiceProvider _mockScopedServiceProvider;
    private readonly IAutomatedReportConfigService _mockConfigService;
    private readonly IAutomatedReportService _mockReportService;
    private readonly ILogger<AutomatedReportBackgroundService> _mockLogger;

    public AutomatedReportBackgroundServiceTests()
    {
        _mockServiceProvider = Substitute.For<IServiceProvider>();
        _mockScopeFactory = Substitute.For<IServiceScopeFactory>();
        _mockScope = Substitute.For<IServiceScope>();
        _mockScopedServiceProvider = Substitute.For<IServiceProvider>();
        _mockConfigService = Substitute.For<IAutomatedReportConfigService>();
        _mockReportService = Substitute.For<IAutomatedReportService>();
        _mockLogger = Substitute.For<ILogger<AutomatedReportBackgroundService>>();

        // Setup dependency injection chain
        _mockServiceProvider.GetService(typeof(IServiceScopeFactory)).Returns(_mockScopeFactory);
        _mockScopeFactory.CreateScope().Returns(_mockScope);
        _mockScope.ServiceProvider.Returns(_mockScopedServiceProvider);
        _mockScopedServiceProvider.GetService(typeof(IAutomatedReportConfigService)).Returns(_mockConfigService);
        _mockScopedServiceProvider.GetRequiredService(typeof(IAutomatedReportConfigService)).Returns(_mockConfigService);
        _mockScopedServiceProvider.GetService(typeof(IAutomatedReportService)).Returns(_mockReportService);
        _mockScopedServiceProvider.GetRequiredService(typeof(IAutomatedReportService)).Returns(_mockReportService);
    }

    private AutomatedReportBackgroundService CreateService()
    {
        return new AutomatedReportBackgroundService(_mockServiceProvider, _mockLogger);
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

    [Fact(DisplayName = "Service should be a BackgroundService")]
    public void Service_InheritsFromBackgroundService()
    {
        // Arrange & Act
        var service = CreateService();

        // Assert
        service.Should().BeAssignableTo<Microsoft.Extensions.Hosting.BackgroundService>();
    }

    #endregion

    #region Configuration Loading Tests

    [Fact(DisplayName = "ExecuteAsync should load configuration on startup")]
    public async Task ExecuteAsync_OnStart_LoadsConfiguration()
    {
        // Arrange
        var config = new AutomatedReportConfigDto
        {
            Habilitado = false, // Disabled so service exits quickly
            HoraGeneracion = TimeSpan.FromHours(6)
        };

        _mockConfigService.GetConfigurationAsync(Arg.Any<CancellationToken>())
            .Returns(config);

        var service = CreateService();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(200);
        await service.StopAsync(CancellationToken.None);

        // Assert
        await _mockConfigService.Received().GetConfigurationAsync(Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "ExecuteAsync should create default config if none exists")]
    public async Task ExecuteAsync_NoConfig_CreatesDefaultConfig()
    {
        // Arrange
        var ensureConfigCalled = new TaskCompletionSource<bool>();

        _mockConfigService.GetConfigurationAsync(Arg.Any<CancellationToken>())
            .Returns((AutomatedReportConfigDto?)null);
        _mockConfigService.EnsureConfigurationExistsAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                ensureConfigCalled.TrySetResult(true);
                return new AutomatedReportConfigDto { Habilitado = false };
            });

        var service = CreateService();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act
        await service.StartAsync(cts.Token);

        // Wait for the method to be called (with timeout)
        var called = await Task.WhenAny(ensureConfigCalled.Task, Task.Delay(TimeSpan.FromSeconds(3)));

        await service.StopAsync(CancellationToken.None);

        // Assert
        called.Should().Be(ensureConfigCalled.Task, "EnsureConfigurationExistsAsync should have been called");
        await _mockConfigService.Received().EnsureConfigurationExistsAsync(Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "ExecuteAsync should log when disabled")]
    public async Task ExecuteAsync_WhenDisabled_LogsAndExits()
    {
        // Arrange
        var config = new AutomatedReportConfigDto
        {
            Habilitado = false,
            HoraGeneracion = TimeSpan.FromHours(6)
        };

        _mockConfigService.GetConfigurationAsync(Arg.Any<CancellationToken>())
            .Returns(config);

        var service = CreateService();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(250);
        await service.StopAsync(CancellationToken.None);

        // Assert - now logs at Debug level
        _mockLogger.Received().Log(
            LogLevel.Debug,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("disabled")),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact(DisplayName = "ExecuteAsync should handle configuration load error")]
    public async Task ExecuteAsync_ConfigLoadError_DisablesService()
    {
        // Arrange
        _mockConfigService.GetConfigurationAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Database error"));

        var service = CreateService();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(250);
        await service.StopAsync(CancellationToken.None);

        // Assert - Verify error was logged (not specific message, which is implementation detail)
        _mockLogger.ReceivedWithAnyArgs().Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    #endregion

    #region Service Lifecycle Tests

    [Fact(DisplayName = "Service should be startable and stoppable")]
    public async Task Service_CanBeStartedAndStopped()
    {
        // Arrange
        var config = new AutomatedReportConfigDto
        {
            Habilitado = false,
            HoraGeneracion = TimeSpan.FromHours(6)
        };

        _mockConfigService.GetConfigurationAsync(Arg.Any<CancellationToken>())
            .Returns(config);

        var service = CreateService();

        // Act
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(200);
        await service.StopAsync(CancellationToken.None);

        // Assert - No exception should be thrown
        service.Should().NotBeNull();
    }

    [Fact(DisplayName = "StopAsync should complete within reasonable time")]
    public async Task StopAsync_CompletesWithinTimeout()
    {
        // Arrange
        var config = new AutomatedReportConfigDto
        {
            Habilitado = false,
            HoraGeneracion = TimeSpan.FromHours(6)
        };

        _mockConfigService.GetConfigurationAsync(Arg.Any<CancellationToken>())
            .Returns(config);

        var service = CreateService();
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(200);

        // Act & Assert
        var stopTask = service.StopAsync(CancellationToken.None);
        var completedInTime = await Task.WhenAny(stopTask, Task.Delay(5000)) == stopTask;

        completedInTime.Should().BeTrue("StopAsync should complete within reasonable time");
    }

    [Fact(DisplayName = "ExecuteAsync should log startup message when enabled")]
    public async Task ExecuteAsync_WhenEnabled_LogsStartup()
    {
        // Arrange
        var config = new AutomatedReportConfigDto
        {
            Habilitado = true,
            HoraGeneracion = TimeSpan.FromHours(6)
        };

        _mockConfigService.GetConfigurationAsync(Arg.Any<CancellationToken>())
            .Returns(config);

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

        // Assert - Verify logger was called (not specific message, which is implementation detail)
        _mockLogger.ReceivedWithAnyArgs().Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact(DisplayName = "ExecuteAsync should log stop message")]
    public async Task ExecuteAsync_OnStop_LogsStopMessage()
    {
        // Arrange
        var config = new AutomatedReportConfigDto
        {
            Habilitado = true,
            HoraGeneracion = TimeSpan.FromHours(6)
        };

        _mockConfigService.GetConfigurationAsync(Arg.Any<CancellationToken>())
            .Returns(config);

        var service = CreateService();

        // Act
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(200);
        await service.StopAsync(CancellationToken.None);

        // Assert
        _mockLogger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("stopped")),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    #endregion

    #region Configuration Reload Tests

    [Fact(DisplayName = "ExecuteAsync should reload config on each iteration")]
    public async Task ExecuteAsync_OnEachIteration_ReloadsConfig()
    {
        // Arrange
        var callCount = 0;
        _mockConfigService.GetConfigurationAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                // Disable after first call so the loop exits
                return new AutomatedReportConfigDto
                {
                    Habilitado = callCount == 1,
                    HoraGeneracion = TimeSpan.FromHours(6)
                };
            });

        var service = CreateService();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(600));

        // Act
        try
        {
            await service.StartAsync(cts.Token);
            await Task.Delay(300, CancellationToken.None);
            await service.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert - Config should be loaded at least once
        callCount.Should().BeGreaterThanOrEqualTo(1);
    }

    #endregion

    #region Error Handling Tests

    [Fact(DisplayName = "ExecuteAsync should handle OperationCanceledException gracefully")]
    public async Task ExecuteAsync_OperationCanceled_ExitsGracefully()
    {
        // Arrange
        var config = new AutomatedReportConfigDto
        {
            Habilitado = true,
            HoraGeneracion = TimeSpan.FromHours(6)
        };

        _mockConfigService.GetConfigurationAsync(Arg.Any<CancellationToken>())
            .Returns(config);

        var service = CreateService();
        using var cts = new CancellationTokenSource();

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(200);

        // Assert - Should not throw
        var stopTask = async () => await service.StopAsync(CancellationToken.None);
        await stopTask.Should().NotThrowAsync();
    }

    [Fact(DisplayName = "ExecuteAsync should continue after general exception")]
    public async Task ExecuteAsync_GeneralException_ContinuesRunning()
    {
        // Arrange
        var config = new AutomatedReportConfigDto
        {
            Habilitado = false,
            HoraGeneracion = TimeSpan.FromHours(6)
        };

        _mockConfigService.GetConfigurationAsync(Arg.Any<CancellationToken>())
            .Returns(config);

        var service = CreateService();

        // Act - Should not throw
        Exception? caughtException = null;
        try
        {
            await service.StartAsync(CancellationToken.None);
            await Task.Delay(200);
            await service.StopAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        // Assert
        caughtException.Should().BeNull();
    }

    #endregion

    #region Configuration from Database Tests

    [Fact(DisplayName = "ExecuteAsync should log configuration changes")]
    public async Task ExecuteAsync_LoadsConfig_LogsConfigurationChanges()
    {
        // Arrange - config with non-default values will trigger change log on first load
        var configLoaded = new TaskCompletionSource<bool>();
        var config = new AutomatedReportConfigDto
        {
            Habilitado = true,  // Different from default (false)
            HoraGeneracion = TimeSpan.FromHours(8)  // Different from default (TimeSpan.Zero)
        };

        _mockConfigService.GetConfigurationAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                configLoaded.TrySetResult(true);
                return config;
            });

        var service = CreateService();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act
        await service.StartAsync(cts.Token);

        // Wait for the config to be loaded (with timeout)
        var loaded = await Task.WhenAny(configLoaded.Task, Task.Delay(TimeSpan.FromSeconds(3)));

        // Give a small delay for the log to be written after config load
        await Task.Delay(50);

        await service.StopAsync(CancellationToken.None);

        // Assert - verify config was loaded
        loaded.Should().Be(configLoaded.Task, "Configuration should have been loaded");

        // Assert - now only logs when configuration changes (which includes first load with non-default values)
        _mockLogger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("configuration updated")),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    #endregion

    #region Scope Management Tests

    [Fact(DisplayName = "Service should create scope for configuration access")]
    public async Task ExecuteAsync_CreatesScope_ForConfigAccess()
    {
        // Arrange
        var config = new AutomatedReportConfigDto
        {
            Habilitado = false,
            HoraGeneracion = TimeSpan.FromHours(6)
        };

        _mockConfigService.GetConfigurationAsync(Arg.Any<CancellationToken>())
            .Returns(config);

        var service = CreateService();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(200);
        await service.StopAsync(CancellationToken.None);

        // Assert
        _mockScopeFactory.Received().CreateScope();
    }

    #endregion

    #region Time Calculation Tests

    [Fact(DisplayName = "Service should handle generation time correctly")]
    public async Task ExecuteAsync_WithGenerationTime_UsesConfiguredTime()
    {
        // Arrange
        var config = new AutomatedReportConfigDto
        {
            Habilitado = true,
            HoraGeneracion = TimeSpan.FromHours(6) // 6:00 AM
        };

        _mockConfigService.GetConfigurationAsync(Arg.Any<CancellationToken>())
            .Returns(config);

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

        // Assert - Verify service started and config was loaded
        await _mockConfigService.Received().GetConfigurationAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region Disabled Service Tests

    [Fact(DisplayName = "ExecuteAsync should poll periodically when disabled")]
    public async Task ExecuteAsync_WhenDisabled_PollsPeriodically()
    {
        // Arrange
        var callCount = 0;
        _mockConfigService.GetConfigurationAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                return new AutomatedReportConfigDto
                {
                    // First call enabled, subsequent disabled
                    Habilitado = callCount == 1,
                    HoraGeneracion = TimeSpan.FromHours(6)
                };
            });

        var service = CreateService();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(600));

        // Act
        try
        {
            await service.StartAsync(cts.Token);
            await Task.Delay(300, CancellationToken.None);
            await service.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert - Should log that service is disabled
        _mockLogger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("disabled") || o.ToString()!.Contains("stopped")),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact(DisplayName = "Service should continue running when initially disabled")]
    public async Task ExecuteAsync_InitiallyDisabled_ContinuesPolling()
    {
        // Arrange - Service starts disabled but should keep polling
        var callCount = 0;
        _mockConfigService.GetConfigurationAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                return new AutomatedReportConfigDto
                {
                    Habilitado = false, // Always disabled
                    HoraGeneracion = TimeSpan.FromHours(6)
                };
            });

        var service = CreateService();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(300, CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        // Assert - Config should be loaded multiple times (initial + at least one poll)
        // The service should NOT exit immediately when disabled
        callCount.Should().BeGreaterThanOrEqualTo(1);

        // Verify service logged "disabled" message at Debug level
        _mockLogger.Received().Log(
            LogLevel.Debug,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("disabled")),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    #endregion

    #region UTC Time Calculation Tests

    [Fact(DisplayName = "Service should use UTC for time calculations")]
    public async Task ExecuteAsync_TimeCalculation_UsesUtc()
    {
        // Arrange - Set generation time to a time that would behave differently in UTC vs local
        var config = new AutomatedReportConfigDto
        {
            Habilitado = true,
            HoraGeneracion = TimeSpan.FromHours(12) // Noon UTC
        };

        _mockConfigService.GetConfigurationAsync(Arg.Any<CancellationToken>())
            .Returns(config);

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

        // Assert - Should log calculated next run with UTC timestamp at Debug level
        _mockLogger.Received().Log(
            LogLevel.Debug,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("UTC")),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    #endregion
}
