using Ceiba.Core.Interfaces;
using Ceiba.Shared.DTOs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ceiba.Infrastructure.Services;

/// <summary>
/// Background service that generates automated daily reports at a configured time.
/// US4: Reportes Automatizados Diarios con IA (Enhanced with DB configuration).
/// </summary>
public class AutomatedReportBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AutomatedReportBackgroundService> _logger;

    private TimeSpan _generationTime;
    private bool _isEnabled;

    public AutomatedReportBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<AutomatedReportBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    private async Task<bool> LoadConfigurationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var configService = scope.ServiceProvider.GetRequiredService<IAutomatedReportConfigService>();

            var config = await configService.GetConfigurationAsync(cancellationToken);

            if (config == null)
            {
                // Create default configuration
                config = await configService.EnsureConfigurationExistsAsync(cancellationToken);
            }

            _generationTime = config.HoraGeneracion;
            _isEnabled = config.Habilitado;

            _logger.LogInformation(
                "Automated report service configured from database. Enabled: {Enabled}, GenerationTime: {Time}",
                _isEnabled,
                _generationTime);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading automated report configuration from database. Service will be disabled.");
            _isEnabled = false;
            return false;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Automated report background service starting...");

        // Initial configuration load
        await LoadConfigurationAsync(stoppingToken);

        _logger.LogInformation(
            "Automated report background service started. Initial state: Enabled={Enabled}, Time={Time}",
            _isEnabled,
            _generationTime);

        // Configuration check interval when disabled (check every 1 minute for quick testing, 5 minutes in production)
        var disabledCheckInterval = TimeSpan.FromMinutes(1);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Reload configuration to pick up changes from UI
                await LoadConfigurationAsync(stoppingToken);

                if (!_isEnabled)
                {
                    _logger.LogInformation(
                        "Automated report generation is disabled. Will check again in {Interval}.",
                        disabledCheckInterval);
                    await Task.Delay(disabledCheckInterval, stoppingToken);
                    continue;
                }

                var delay = CalculateDelayUntilNextRun();

                // If delay is very long (> 1 min), check configuration more frequently
                // to pick up changes from the UI (every 1 minute)
                var maxWait = TimeSpan.FromMinutes(1);
                if (delay > maxWait)
                {
                    _logger.LogDebug(
                        "Next run in {Hours}h {Minutes}m, will recheck config in {MaxWait}",
                        (int)delay.TotalHours, delay.Minutes, maxWait);
                    await Task.Delay(maxWait, stoppingToken);
                    continue;
                }

                _logger.LogInformation(
                    "Waiting {Minutes}m {Seconds}s until next automated report generation",
                    (int)delay.TotalMinutes, delay.Seconds);

                await Task.Delay(delay, stoppingToken);

                // Verify configuration is still enabled before running
                await LoadConfigurationAsync(stoppingToken);
                if (!stoppingToken.IsCancellationRequested && _isEnabled)
                {
                    await GenerateDailyReportAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when service is stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in automated report background service. Will retry in 1 minute.");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        _logger.LogInformation("Automated report background service stopped.");
    }

    private TimeSpan CalculateDelayUntilNextRun()
    {
        // Use UTC since _generationTime is stored in UTC
        var now = DateTime.UtcNow;
        var nextRun = now.Date.Add(_generationTime);

        // If the time has already passed today, schedule for tomorrow
        if (nextRun <= now)
        {
            nextRun = nextRun.AddDays(1);
        }

        var delay = nextRun - now;

        _logger.LogInformation(
            "Calculated next run: {NextRun} UTC (in {Hours}h {Minutes}m). Current UTC: {Now}",
            nextRun.ToString("yyyy-MM-dd HH:mm:ss"),
            (int)delay.TotalHours,
            delay.Minutes,
            now.ToString("yyyy-MM-dd HH:mm:ss"));

        return delay;
    }

    private async Task GenerateDailyReportAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting automated daily report generation.");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var reportService = scope.ServiceProvider.GetRequiredService<IAutomatedReportService>();

            // Generate report for the previous 24 hours
            var now = DateTime.UtcNow;
            var fechaFin = now.Date; // Start of today (UTC)
            var fechaInicio = fechaFin.AddDays(-1); // Start of yesterday

            var request = new GenerateReportRequestDto
            {
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                EnviarEmail = true
            };

            var report = await reportService.GenerateReportAsync(request, null, cancellationToken);

            _logger.LogInformation(
                "Automated daily report {Id} generated successfully. Total reports: {Total}, Sent: {Sent}",
                report.Id,
                report.Estadisticas.TotalReportes,
                report.Enviado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate automated daily report.");
            // Don't rethrow - we'll try again tomorrow
        }
    }
}
