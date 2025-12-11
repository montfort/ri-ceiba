using Ceiba.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ceiba.Infrastructure.Services;

/// <summary>
/// T146-T155: RO-003 Background service for processing queued emails.
/// Runs periodically to send emails that were queued due to failures.
/// </summary>
public class EmailQueueProcessorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailQueueProcessorService> _logger;
    private readonly TimeSpan _processingInterval = TimeSpan.FromMinutes(5);

    public EmailQueueProcessorService(
        IServiceScopeFactory scopeFactory,
        ILogger<EmailQueueProcessorService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Email queue processor started. Processing interval: {Interval}",
            _processingInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_processingInterval, stoppingToken);

                using var scope = _scopeFactory.CreateScope();
                var resilientEmailService = scope.ServiceProvider.GetRequiredService<IResilientEmailService>();

                if (resilientEmailService.QueuedEmailCount > 0)
                {
                    _logger.LogDebug(
                        "Processing email queue. Queued emails: {Count}",
                        resilientEmailService.QueuedEmailCount);

                    var processed = await resilientEmailService.ProcessQueueAsync(stoppingToken);

                    if (processed > 0)
                    {
                        _logger.LogInformation(
                            "Processed {Count} queued emails. Remaining: {Remaining}",
                            processed, resilientEmailService.QueuedEmailCount);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing email queue");
            }
        }

        _logger.LogInformation("Email queue processor stopped");
    }
}
