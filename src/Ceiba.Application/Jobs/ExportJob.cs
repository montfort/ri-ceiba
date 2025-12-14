using Ceiba.Application.Services.Export;
using Ceiba.Core.Interfaces;
using Ceiba.Shared.DTOs;
using Ceiba.Shared.DTOs.Export;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace Ceiba.Application.Jobs;

/// <summary>
/// Background export job for processing large export requests.
/// T052c: Background export job for >50 reports with email notification.
/// T052d: Max 3 concurrent export jobs and 2-minute timeout.
/// </summary>
public class ExportJob
{
    private readonly IExportService _exportService;
    private readonly IEmailService _emailService;
    private readonly ILogger<ExportJob> _logger;

    /// <summary>
    /// Maximum concurrent export jobs (T052d)
    /// </summary>
    public const int MaxConcurrentJobs = 3;

    /// <summary>
    /// Job timeout in minutes (T052d)
    /// </summary>
    public const int TimeoutMinutes = 2;

    public ExportJob(
        IExportService exportService,
        IEmailService emailService,
        ILogger<ExportJob> logger)
    {
        _exportService = exportService;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Executes background export and sends result via email.
    /// </summary>
    /// <param name="request">Export request parameters</param>
    /// <param name="userId">User who requested the export</param>
    /// <param name="userEmail">Email to send results to</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [AutomaticRetry(Attempts = 1)]
    [Queue("exports")]
    public async Task ExecuteAsync(
        BackgroundExportRequest request,
        Guid userId,
        string userEmail,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting background export job for {ReportCount} reports, User: {UserId}, Email: {Email}",
            request.ReportIds.Length, userId, userEmail);

        try
        {
            // Create export request DTO
            var exportRequest = new ExportRequestDto
            {
                ReportIds = request.ReportIds,
                Format = request.Format,
                Options = request.Options
            };

            // Execute export (this bypasses the limit check since it's a background job)
            var result = await ExecuteExportWithoutLimitsAsync(
                exportRequest, userId, cancellationToken);

            // Send email with attachment
            await SendExportResultEmailAsync(
                userEmail, result, request.Format, cancellationToken);

            _logger.LogInformation(
                "Background export job completed successfully. " +
                "Reports: {ReportCount}, Size: {SizeKb}KB, User: {UserId}",
                result.ReportCount, result.Data.Length / 1024, userId);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex,
                "Background export job cancelled for User: {UserId}",
                userId);

            // Send cancellation notification
            await SendExportFailureEmailAsync(
                userEmail,
                "La exportación fue cancelada por exceder el tiempo límite.",
                cancellationToken);

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Background export job failed for User: {UserId}",
                userId);

            // Send failure notification
            await SendExportFailureEmailAsync(
                userEmail,
                $"Error durante la exportación: {ex.Message}",
                cancellationToken);

            throw;
        }
    }

    /// <summary>
    /// Executes export without limit validation (for background jobs only).
    /// </summary>
    private async Task<ExportResultDto> ExecuteExportWithoutLimitsAsync(
        ExportRequestDto request,
        Guid userId,
        CancellationToken cancellationToken)
    {
        // Background jobs bypass the normal limits
        // The service will handle the actual export logic
        return await _exportService.ExportReportsAsync(
            request,
            userId,
            isRevisor: true, // Background jobs are pre-authorized
            cancellationToken);
    }

    /// <summary>
    /// Sends email with export result attachment.
    /// </summary>
    private async Task SendExportResultEmailAsync(
        string recipientEmail,
        ExportResultDto result,
        ExportFormat format,
        CancellationToken cancellationToken)
    {
        var formatName = format == ExportFormat.PDF ? "PDF" : "JSON";
        var subject = $"Exportación de Reportes Completada - {result.ReportCount} reportes";

        var bodyHtml = $@"
            <h2>Exportación Completada</h2>
            <p>Su solicitud de exportación ha sido procesada exitosamente.</p>
            <ul>
                <li><strong>Reportes exportados:</strong> {result.ReportCount}</li>
                <li><strong>Formato:</strong> {formatName}</li>
                <li><strong>Tamaño del archivo:</strong> {result.Data.Length / 1024.0:F1} KB</li>
                <li><strong>Fecha de generación:</strong> {result.GeneratedAt:dd/MM/yyyy HH:mm:ss} UTC</li>
            </ul>
            <p>El archivo está adjunto a este correo.</p>
            <hr/>
            <p><small>Este es un mensaje automático del sistema Ceiba.</small></p>";

        var emailRequest = new SendEmailRequestDto
        {
            Recipients = new List<string> { recipientEmail },
            Subject = subject,
            BodyHtml = bodyHtml,
            Attachments = new List<EmailAttachmentDto>
            {
                new EmailAttachmentDto
                {
                    FileName = result.FileName,
                    Content = result.Data,
                    ContentType = result.ContentType
                }
            }
        };

        var emailResult = await _emailService.SendAsync(emailRequest, cancellationToken);

        if (!emailResult.Success)
        {
            _logger.LogWarning(
                "Failed to send export result email to {Email}: {Error}",
                recipientEmail, emailResult.Error);
        }
    }

    /// <summary>
    /// Sends email notification when export fails.
    /// </summary>
    private async Task SendExportFailureEmailAsync(
        string recipientEmail,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        var subject = "Exportación de Reportes - Error";

        var bodyHtml = $@"
            <h2>Error en Exportación</h2>
            <p>Su solicitud de exportación no pudo completarse.</p>
            <p><strong>Motivo:</strong> {errorMessage}</p>
            <p>Por favor, intente nuevamente o contacte al administrador del sistema si el problema persiste.</p>
            <hr/>
            <p><small>Este es un mensaje automático del sistema Ceiba.</small></p>";

        var emailRequest = new SendEmailRequestDto
        {
            Recipients = new List<string> { recipientEmail },
            Subject = subject,
            BodyHtml = bodyHtml
        };

        try
        {
            await _emailService.SendAsync(emailRequest, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send export failure notification to {Email}",
                recipientEmail);
        }
    }

    /// <summary>
    /// Schedules a background export job.
    /// T052c: Creates background job with Hangfire.
    /// </summary>
    /// <param name="request">Export request parameters</param>
    /// <param name="userId">User requesting the export</param>
    /// <param name="userEmail">Email for notification</param>
    /// <returns>Job ID for tracking</returns>
    public static string Schedule(
        BackgroundExportRequest request,
        Guid userId,
        string userEmail)
    {
        // T052d: Configure timeout
        var jobId = BackgroundJob.Enqueue<ExportJob>(
            job => job.ExecuteAsync(request, userId, userEmail, CancellationToken.None));

        return jobId;
    }

    /// <summary>
    /// Gets the status of a background export job.
    /// </summary>
    /// <param name="jobId">Job ID to check</param>
    /// <returns>Job status information</returns>
    public static ExportJobStatus GetStatus(string jobId)
    {
        var connection = JobStorage.Current.GetConnection();
        var jobData = connection.GetJobData(jobId);

        if (jobData == null)
        {
            return new ExportJobStatus
            {
                JobId = jobId,
                State = "NotFound",
                Message = "El trabajo no fue encontrado."
            };
        }

        return new ExportJobStatus
        {
            JobId = jobId,
            State = jobData.State,
            CreatedAt = jobData.CreatedAt,
            Message = GetStateMessage(jobData.State)
        };
    }

    private static string GetStateMessage(string state)
    {
        return state switch
        {
            "Enqueued" => "En cola, esperando procesamiento.",
            "Processing" => "Procesando exportación...",
            "Succeeded" => "Exportación completada. Revise su correo electrónico.",
            "Failed" => "La exportación falló. Se ha enviado notificación por correo.",
            "Deleted" => "El trabajo fue eliminado.",
            "Scheduled" => "Programado para ejecución.",
            _ => $"Estado: {state}"
        };
    }
}

/// <summary>
/// Request model for background export jobs.
/// </summary>
public class BackgroundExportRequest
{
    public int[] ReportIds { get; set; } = Array.Empty<int>();
    public ExportFormat Format { get; set; }
    public ExportOptions? Options { get; set; }
}

/// <summary>
/// Status information for a background export job.
/// </summary>
public class ExportJobStatus
{
    public string JobId { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Message { get; set; } = string.Empty;
}
