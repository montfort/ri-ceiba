using Serilog.Core;
using Serilog.Events;
using System.Text.RegularExpressions;

namespace Ceiba.Infrastructure.Logging;

/// <summary>
/// RS-003 Mitigation: Serilog enricher to redact PII from log messages.
/// Prevents accidental logging of sensitive information like passwords, emails, IPs.
/// </summary>
public partial class PIIRedactionEnricher : ILogEventEnricher
{
    // Regex patterns for common PII
    [GeneratedRegex(@"password[\""\s:=]+([^\""\s,}]+)", RegexOptions.IgnoreCase)]
    private static partial Regex PasswordRegex();

    [GeneratedRegex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"\b(?:\d{1,3}\.){3}\d{1,3}\b")]
    private static partial Regex IPv4Regex();

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (logEvent.MessageTemplate?.Text == null)
            return;

        var message = logEvent.RenderMessage();
        var redactedMessage = RedactSensitiveData(message);

        if (message != redactedMessage)
        {
            var redactedProperty = propertyFactory.CreateProperty("RedactedMessage", redactedMessage);
            logEvent.AddOrUpdateProperty(redactedProperty);
        }
    }

    private static string RedactSensitiveData(string message)
    {
        // Redact passwords
        message = PasswordRegex().Replace(message, "password: [REDACTED]");

        // Redact emails (partial)
        message = EmailRegex().Replace(message, m =>
        {
            var email = m.Value;
            var atIndex = email.IndexOf('@');
            if (atIndex > 2)
            {
                return email.Substring(0, 2) + "***" + email.Substring(atIndex);
            }
            return "***@" + email.Substring(atIndex + 1);
        });

        // Redact IPs (partial - keep first octet)
        message = IPv4Regex().Replace(message, m =>
        {
            var parts = m.Value.Split('.');
            return $"{parts[0]}.***.***.***";
        });

        return message;
    }
}
