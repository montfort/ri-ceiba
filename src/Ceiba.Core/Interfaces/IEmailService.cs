using Ceiba.Shared.DTOs;

namespace Ceiba.Core.Interfaces;

/// <summary>
/// Email service abstraction for sending reports via SMTP.
/// US4: Reportes Automatizados Diarios con IA.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email with optional attachments.
    /// </summary>
    /// <param name="request">The email sending request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the send operation.</returns>
    Task<SendEmailResultDto> SendAsync(
        SendEmailRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the email service is available and properly configured.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the service is available.</returns>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}
