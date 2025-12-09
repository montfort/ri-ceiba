namespace Ceiba.Core.Entities;

/// <summary>
/// Configuration for email transactional service.
/// Supports SMTP and SendGrid providers.
/// </summary>
public class ConfiguracionEmail : BaseEntityWithUser
{
    public int Id { get; set; }

    /// <summary>
    /// Email provider type: SMTP, SendGrid, Mailgun
    /// </summary>
    public string Proveedor { get; set; } = "SMTP";

    /// <summary>
    /// Whether the email service is enabled
    /// </summary>
    public bool Habilitado { get; set; } = false;

    // SMTP Configuration
    public string? SmtpHost { get; set; }
    public int? SmtpPort { get; set; }
    public string? SmtpUsername { get; set; }
    public string? SmtpPassword { get; set; }
    public bool SmtpUseSsl { get; set; } = true;

    // SendGrid Configuration
    public string? SendGridApiKey { get; set; }

    // Mailgun Configuration
    public string? MailgunApiKey { get; set; }
    public string? MailgunDomain { get; set; }
    public string? MailgunRegion { get; set; } = "US"; // US or EU

    // Common Configuration
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;

    /// <summary>
    /// Last time configuration was tested successfully
    /// </summary>
    public DateTime? LastTestedAt { get; set; }

    /// <summary>
    /// Result of last connection test
    /// </summary>
    public bool? LastTestSuccess { get; set; }

    /// <summary>
    /// Error message from last test (if failed)
    /// </summary>
    public string? LastTestError { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
