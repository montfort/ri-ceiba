using Ceiba.Core.Interfaces;
using Ceiba.Shared.DTOs;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Ceiba.Infrastructure.Services;

/// <summary>
/// Email service implementation using MailKit for SMTP.
/// US4: Reportes Automatizados Diarios con IA.
/// </summary>
public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    private readonly string _host;
    private readonly int _port;
    private readonly string _username;
    private readonly string _password;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly bool _useSsl;

    public EmailService(
        IConfiguration configuration,
        ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // Load configuration
        _host = configuration["Email:Host"] ?? "localhost";
        _port = int.TryParse(configuration["Email:Port"], out var port) ? port : 587;
        _username = configuration["Email:Username"] ?? "";
        _password = configuration["Email:Password"] ?? "";
        _fromEmail = configuration["Email:FromEmail"] ?? "noreply@ceiba.local";
        _fromName = configuration["Email:FromName"] ?? "Ceiba - Reportes de Incidencias";
        _useSsl = bool.TryParse(configuration["Email:UseSsl"], out var useSsl) && useSsl;
    }

    public async Task<SendEmailResultDto> SendAsync(
        SendEmailRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_fromName, _fromEmail));

            foreach (var recipient in request.Recipients)
            {
                message.To.Add(MailboxAddress.Parse(recipient));
            }

            message.Subject = request.Subject;

            var builder = new BodyBuilder
            {
                HtmlBody = request.BodyHtml
            };

            // Add attachments
            foreach (var attachment in request.Attachments)
            {
                builder.Attachments.Add(
                    attachment.FileName,
                    attachment.Content,
                    ContentType.Parse(attachment.ContentType));
            }

            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();

            var secureSocketOptions = _useSsl
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTlsWhenAvailable;

            await client.ConnectAsync(_host, _port, secureSocketOptions, cancellationToken);

            // Authenticate if credentials provided
            if (!string.IsNullOrEmpty(_username) && !string.IsNullOrEmpty(_password))
            {
                await client.AuthenticateAsync(_username, _password, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation(
                "Email sent successfully to {Recipients}. Subject: {Subject}",
                string.Join(", ", request.Recipients),
                request.Subject);

            return new SendEmailResultDto
            {
                Success = true,
                SentAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipients}", string.Join(", ", request.Recipients));

            return new SendEmailResultDto
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_host))
            return false;

        try
        {
            using var client = new SmtpClient();

            var secureSocketOptions = _useSsl
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTlsWhenAvailable;

            // Set a short timeout for the check
            client.Timeout = 5000;

            await client.ConnectAsync(_host, _port, secureSocketOptions, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Email service availability check failed");
            return false;
        }
    }
}
