using Ceiba.Core.Interfaces;
using Ceiba.Shared.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ceiba.Infrastructure.Services;

/// <summary>
/// Background service that generates automated daily reports at a configured time.
/// US4: Reportes Automatizados Diarios con IA.
/// </summary>
public class AutomatedReportBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AutomatedReportBackgroundService> _logger;

    private TimeSpan _generationTime;
    private bool _isEnabled;

    public AutomatedReportBackgroundService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<AutomatedReportBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;

        LoadConfiguration();
    }

    private void LoadConfiguration()
    {
        _generationTime = TimeSpan.TryParse(_configuration["AutomatedReports:GenerationTime"], out var time)
            ? time
            : new TimeSpan(6, 0, 0); // Default: 6:00 AM

        _isEnabled = bool.TryParse(_configuration["AutomatedReports:Enabled"], out var enabled) && enabled;

        _logger.LogInformation(
            "Automated report service configured. Enabled: {Enabled}, GenerationTime: {Time}",
            _isEnabled,
            _generationTime);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_isEnabled)
        {
            _logger.LogInformation("Automated report generation is disabled.");
            return;
        }

        _logger.LogInformation("Automated report background service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var delay = CalculateDelayUntilNextRun();
                _logger.LogDebug("Next automated report generation in {Delay}", delay);

                await Task.Delay(delay, stoppingToken);

                if (!stoppingToken.IsCancellationRequested)
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
                _logger.LogError(ex, "Error in automated report background service. Will retry in 1 hour.");
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        _logger.LogInformation("Automated report background service stopped.");
    }

    private TimeSpan CalculateDelayUntilNextRun()
    {
        var now = DateTime.Now;
        var nextRun = now.Date.Add(_generationTime);

        // If the time has already passed today, schedule for tomorrow
        if (nextRun <= now)
        {
            nextRun = nextRun.AddDays(1);
        }

        return nextRun - now;
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
