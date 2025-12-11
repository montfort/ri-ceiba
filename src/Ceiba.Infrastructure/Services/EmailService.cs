using Ceiba.Core.Interfaces;
using Ceiba.Infrastructure.Data;
using Ceiba.Shared.DTOs;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Ceiba.Infrastructure.Services;

/// <summary>
/// Email service implementation with support for SMTP, SendGrid, and Mailgun.
/// Reads configuration from database (ConfiguracionEmail table).
/// US4: Reportes Automatizados Diarios con IA.
/// </summary>
public class EmailService : IEmailService
{
    private readonly CeibaDbContext _context;
    private readonly ILogger<EmailService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public EmailService(
        CeibaDbContext context,
        IConfiguration configuration,
        ILogger<EmailService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _context = context;
        // Note: configuration parameter is kept for DI compatibility but currently unused
        // as email settings are loaded from database (ConfiguracionEmail table)
        _ = configuration; // Suppress unused parameter warning
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<SendEmailResultDto> SendAsync(
        SendEmailRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Load configuration from database
            var config = await GetActiveConfigurationAsync(cancellationToken);

            if (config == null)
            {
                _logger.LogWarning("No email configuration found or service is disabled");
                return new SendEmailResultDto
                {
                    Success = false,
                    Error = "El servicio de email no está configurado o está deshabilitado"
                };
            }

            // Send based on provider
            if (config.Proveedor == "SendGrid")
            {
                return await SendViaSendGridAsync(config, request, cancellationToken);
            }
            else if (config.Proveedor == "Mailgun")
            {
                return await SendViaMailgunAsync(config, request, cancellationToken);
            }
            else // Default to SMTP
            {
                return await SendViaSmtpAsync(config, request, cancellationToken);
            }
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
        try
        {
            var config = await GetActiveConfigurationAsync(cancellationToken);

            if (config == null)
                return false;

            if (config.Proveedor == "SendGrid")
            {
                // For SendGrid, just check if API key is configured
                return !string.IsNullOrEmpty(config.SendGridApiKey);
            }
            else if (config.Proveedor == "Mailgun")
            {
                // For Mailgun, check if API key and domain are configured
                return !string.IsNullOrEmpty(config.MailgunApiKey) &&
                       !string.IsNullOrEmpty(config.MailgunDomain);
            }
            else // SMTP
            {
                if (string.IsNullOrEmpty(config.SmtpHost))
                    return false;

                using var client = new SmtpClient();

                var secureSocketOptions = config.SmtpUseSsl
                    ? SecureSocketOptions.SslOnConnect
                    : SecureSocketOptions.StartTlsWhenAvailable;

                // Set a short timeout for the check
                client.Timeout = 5000;

                await client.ConnectAsync(
                    config.SmtpHost,
                    config.SmtpPort ?? 587,
                    secureSocketOptions,
                    cancellationToken);

                await client.DisconnectAsync(true, cancellationToken);

                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Email service availability check failed");
            return false;
        }
    }

    private async Task<Core.Entities.ConfiguracionEmail?> GetActiveConfigurationAsync(
        CancellationToken cancellationToken)
    {
        return await _context.ConfiguracionesEmail
            .AsNoTracking()
            .Where(c => c.Habilitado)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<SendEmailResultDto> SendViaSmtpAsync(
        Core.Entities.ConfiguracionEmail config,
        SendEmailRequestDto request,
        CancellationToken cancellationToken)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(config.FromName, config.FromEmail));

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

        var secureSocketOptions = config.SmtpUseSsl
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTlsWhenAvailable;

        await client.ConnectAsync(
            config.SmtpHost!,
            config.SmtpPort ?? 587,
            secureSocketOptions,
            cancellationToken);

        // Authenticate if credentials provided
        if (!string.IsNullOrEmpty(config.SmtpUsername) && !string.IsNullOrEmpty(config.SmtpPassword))
        {
            await client.AuthenticateAsync(config.SmtpUsername, config.SmtpPassword, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        _logger.LogInformation(
            "Email sent successfully via SMTP to {Recipients}. Subject: {Subject}",
            string.Join(", ", request.Recipients),
            request.Subject);

        return new SendEmailResultDto
        {
            Success = true,
            SentAt = DateTime.UtcNow
        };
    }

    private async Task<SendEmailResultDto> SendViaSendGridAsync(
        Core.Entities.ConfiguracionEmail config,
        SendEmailRequestDto request,
        CancellationToken cancellationToken)
    {
        var client = new SendGridClient(config.SendGridApiKey);

        var from = new EmailAddress(config.FromEmail, config.FromName);
        var tos = request.Recipients.Select(r => new EmailAddress(r)).ToList();

        var msg = MailHelper.CreateSingleEmailToMultipleRecipients(
            from,
            tos,
            request.Subject,
            plainTextContent: null, // We only send HTML
            htmlContent: request.BodyHtml);

        // Add attachments
        if (request.Attachments.Any())
        {
            msg.Attachments = request.Attachments.Select(a => new Attachment
            {
                Content = Convert.ToBase64String(a.Content),
                Filename = a.FileName,
                Type = a.ContentType,
                Disposition = "attachment"
            }).ToList();
        }

        var response = await client.SendEmailAsync(msg, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation(
                "Email sent successfully via SendGrid to {Recipients}. Subject: {Subject}",
                string.Join(", ", request.Recipients),
                request.Subject);

            return new SendEmailResultDto
            {
                Success = true,
                SentAt = DateTime.UtcNow
            };
        }
        else
        {
            var body = await response.Body.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "SendGrid API returned error. Status: {Status}, Body: {Body}",
                response.StatusCode,
                body);

            return new SendEmailResultDto
            {
                Success = false,
                Error = $"SendGrid API error: {response.StatusCode}"
            };
        }
    }

    private async Task<SendEmailResultDto> SendViaMailgunAsync(
        Core.Entities.ConfiguracionEmail config,
        SendEmailRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Determine API base URL based on region
            var baseUrl = config.MailgunRegion == "EU"
                ? "https://api.eu.mailgun.net"
                : "https://api.mailgun.net";

            var client = _httpClientFactory.CreateClient();

            // Set up Basic Authentication (api:YOUR_API_KEY)
            var credentials = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"api:{config.MailgunApiKey}"));
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", credentials);

            // Build multipart form data
            var formData = new MultipartFormDataContent();

            // Mailgun requires "Sender Name <email@domain.com>" format
            var fromField = $"{config.FromName} <{config.FromEmail}>";
            formData.Add(new StringContent(fromField), "from");

            // Add recipients
            foreach (var recipient in request.Recipients)
            {
                formData.Add(new StringContent(recipient), "to");
            }

            formData.Add(new StringContent(request.Subject), "subject");
            formData.Add(new StringContent(request.BodyHtml), "html");

            // Add attachments
            foreach (var attachment in request.Attachments)
            {
                var fileContent = new ByteArrayContent(attachment.Content);
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(attachment.ContentType);
                formData.Add(fileContent, "attachment", attachment.FileName);
            }

            // Send request
            var url = $"{baseUrl}/v3/{config.MailgunDomain}/messages";
            _logger.LogInformation("Sending email via Mailgun to URL: {Url}", url);

            var response = await client.PostAsync(url, formData, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Email sent successfully via Mailgun to {Recipients}. Subject: {Subject}",
                    string.Join(", ", request.Recipients),
                    request.Subject);

                return new SendEmailResultDto
                {
                    Success = true,
                    SentAt = DateTime.UtcNow
                };
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Mailgun API returned error. Status: {Status}, Body: {Body}",
                    response.StatusCode,
                    body);

                // Return detailed error message
                var errorMessage = $"Mailgun API error: {response.StatusCode}";
                if (!string.IsNullOrEmpty(body))
                {
                    errorMessage += $" - {body}";
                }

                return new SendEmailResultDto
                {
                    Success = false,
                    Error = errorMessage
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email via Mailgun");
            throw;
        }
    }
}
