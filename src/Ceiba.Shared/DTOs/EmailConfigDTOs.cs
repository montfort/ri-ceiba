namespace Ceiba.Shared.DTOs;

/// <summary>
/// DTO for email configuration display.
/// </summary>
public class EmailConfigDto
{
    public int Id { get; set; }
    public string Proveedor { get; set; } = "SMTP";
    public bool Habilitado { get; set; }

    // SMTP Configuration
    public string? SmtpHost { get; set; }
    public int? SmtpPort { get; set; }
    public string? SmtpUsername { get; set; }
    public bool SmtpUseSsl { get; set; }

    // SendGrid Configuration (API key masked for security)
    public bool HasSendGridApiKey { get; set; }

    // Mailgun Configuration
    public bool HasMailgunApiKey { get; set; }
    public string? MailgunDomain { get; set; }
    public string? MailgunRegion { get; set; }

    // Common Configuration
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;

    // Test results
    public DateTime? LastTestedAt { get; set; }
    public bool? LastTestSuccess { get; set; }
    public string? LastTestError { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO for updating email configuration.
/// </summary>
public class EmailConfigUpdateDto
{
    public string Proveedor { get; set; } = "SMTP";
    public bool Habilitado { get; set; }

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
    public string? MailgunRegion { get; set; }

    // Common Configuration
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
}

/// <summary>
/// DTO for testing email configuration.
/// </summary>
public class TestEmailConfigDto
{
    public string TestRecipient { get; set; } = string.Empty;
}

/// <summary>
/// DTO for email configuration test result.
/// </summary>
public class EmailConfigTestResultDto
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public DateTime TestedAt { get; set; }
}
