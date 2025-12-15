using Ceiba.Infrastructure.Logging;
using FluentAssertions;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;

namespace Ceiba.Infrastructure.Tests.Logging;

/// <summary>
/// Unit tests for PIIRedactionEnricher.
/// RS-003 Mitigation: Tests PII redaction in log messages.
/// </summary>
[Trait("Category", "Unit")]
public class PIIRedactionEnricherTests
{
    private readonly PIIRedactionEnricher _enricher;

    public PIIRedactionEnricherTests()
    {
        _enricher = new PIIRedactionEnricher();
    }

    #region Password Redaction Tests

    [Theory(DisplayName = "Enrich should redact password values")]
    [InlineData("password: secret123")]
    [InlineData("password=mysecret")]
    [InlineData("Password: admin123")]
    [InlineData("PASSWORD: test")]
    public void Enrich_PasswordPatterns_Redacted(string input)
    {
        // Arrange
        var logEvent = CreateLogEvent(input);
        var factory = new TestPropertyFactory();

        // Act
        _enricher.Enrich(logEvent, factory);

        // Assert
        if (factory.CreatedProperty != null)
        {
            var redacted = factory.CreatedProperty.Value.ToString();
            redacted.Should().Contain("[REDACTED]");
        }
    }

    [Fact(DisplayName = "Enrich should redact password in JSON format")]
    public void Enrich_PasswordInJson_Redacted()
    {
        // Arrange
        var input = "{\"password\": \"secret123\", \"username\": \"admin\"}";
        var logEvent = CreateLogEvent(input);
        var factory = new TestPropertyFactory();

        // Act
        _enricher.Enrich(logEvent, factory);

        // Assert
        if (factory.CreatedProperty != null)
        {
            var redacted = factory.CreatedProperty.Value.ToString();
            redacted.Should().Contain("[REDACTED]");
            redacted.Should().NotContain("secret123");
        }
    }

    #endregion

    #region Email Redaction Tests

    [Theory(DisplayName = "Enrich should partially redact email addresses")]
    [InlineData("user@example.com", "us***@example.com")]
    [InlineData("john.doe@company.org", "jo***@company.org")]
    [InlineData("ab@test.com", "***@test.com")]
    public void Enrich_EmailAddresses_PartiallyRedacted(string email, string expected)
    {
        // Arrange
        var input = $"User email: {email}";
        var logEvent = CreateLogEvent(input);
        var factory = new TestPropertyFactory();

        // Act
        _enricher.Enrich(logEvent, factory);

        // Assert
        factory.CreatedProperty.Should().NotBeNull();
        var redacted = factory.CreatedProperty!.Value.ToString();
        redacted.Should().Contain(expected);
        redacted.Should().NotContain(email);
    }

    [Fact(DisplayName = "Enrich should handle multiple emails in message")]
    public void Enrich_MultipleEmails_AllRedacted()
    {
        // Arrange
        var input = "Contact: admin@test.com and support@company.org";
        var logEvent = CreateLogEvent(input);
        var factory = new TestPropertyFactory();

        // Act
        _enricher.Enrich(logEvent, factory);

        // Assert
        factory.CreatedProperty.Should().NotBeNull();
        var redacted = factory.CreatedProperty!.Value.ToString();
        redacted.Should().NotContain("admin@test.com");
        redacted.Should().NotContain("support@company.org");
        redacted.Should().Contain("***@");
    }

    #endregion

    #region IP Address Redaction Tests

    [Theory(DisplayName = "Enrich should partially redact IPv4 addresses")]
    [InlineData("192.168.1.100", "192.***.***.***")]
    [InlineData("10.0.0.1", "10.***.***.***")]
    [InlineData("172.16.0.50", "172.***.***.***")]
    public void Enrich_IPv4Addresses_PartiallyRedacted(string ip, string expected)
    {
        // Arrange
        var input = $"Client IP: {ip}";
        var logEvent = CreateLogEvent(input);
        var factory = new TestPropertyFactory();

        // Act
        _enricher.Enrich(logEvent, factory);

        // Assert
        factory.CreatedProperty.Should().NotBeNull();
        var redacted = factory.CreatedProperty!.Value.ToString();
        redacted.Should().Contain(expected);
        redacted.Should().NotContain(ip);
    }

    [Fact(DisplayName = "Enrich should handle multiple IPs in message")]
    public void Enrich_MultipleIPs_AllRedacted()
    {
        // Arrange
        var input = "Request from 192.168.1.1 forwarded to 10.0.0.5";
        var logEvent = CreateLogEvent(input);
        var factory = new TestPropertyFactory();

        // Act
        _enricher.Enrich(logEvent, factory);

        // Assert
        factory.CreatedProperty.Should().NotBeNull();
        var redacted = factory.CreatedProperty!.Value.ToString();
        redacted.Should().Contain("192.***.***.***");
        redacted.Should().Contain("10.***.***.***");
        redacted.Should().NotContain("192.168.1.1");
        redacted.Should().NotContain("10.0.0.5");
    }

    #endregion

    #region No Redaction Needed Tests

    [Fact(DisplayName = "Enrich should not modify message without PII")]
    public void Enrich_NoPII_NoModification()
    {
        // Arrange
        var input = "Normal log message without sensitive data";
        var logEvent = CreateLogEvent(input);
        var factory = new TestPropertyFactory();

        // Act
        _enricher.Enrich(logEvent, factory);

        // Assert
        factory.CreatedProperty.Should().BeNull();
    }

    [Fact(DisplayName = "Enrich should handle null message template")]
    public void Enrich_NullMessageTemplate_NoError()
    {
        // Arrange
        var logEvent = new LogEvent(
            DateTimeOffset.Now,
            LogEventLevel.Information,
            null,
            new MessageTemplate(new List<MessageTemplateToken>()),
            new List<LogEventProperty>());
        var factory = new TestPropertyFactory();

        // Act
        var act = () => _enricher.Enrich(logEvent, factory);

        // Assert
        act.Should().NotThrow();
    }

    [Fact(DisplayName = "Enrich should preserve safe data in message")]
    public void Enrich_MixedContent_PreservesSafeData()
    {
        // Arrange
        var input = "User login at 2024-01-15 from IP 192.168.1.1";
        var logEvent = CreateLogEvent(input);
        var factory = new TestPropertyFactory();

        // Act
        _enricher.Enrich(logEvent, factory);

        // Assert
        factory.CreatedProperty.Should().NotBeNull();
        var redacted = factory.CreatedProperty!.Value.ToString();
        redacted.Should().Contain("User login at 2024-01-15");
        redacted.Should().Contain("192.***.***.***");
    }

    #endregion

    #region Combined PII Tests

    [Fact(DisplayName = "Enrich should redact all PII types in single message")]
    public void Enrich_AllPIITypes_AllRedacted()
    {
        // Arrange
        var input = "User admin@test.com logged in from 192.168.1.1 with password: secret";
        var logEvent = CreateLogEvent(input);
        var factory = new TestPropertyFactory();

        // Act
        _enricher.Enrich(logEvent, factory);

        // Assert
        factory.CreatedProperty.Should().NotBeNull();
        var redacted = factory.CreatedProperty!.Value.ToString();
        redacted.Should().NotContain("admin@test.com");
        redacted.Should().NotContain("192.168.1.1");
        redacted.Should().NotContain("secret");
        redacted.Should().Contain("***@");
        redacted.Should().Contain(".***.***.***");
        redacted.Should().Contain("[REDACTED]");
    }

    #endregion

    #region Edge Cases

    [Fact(DisplayName = "Enrich should handle empty message")]
    public void Enrich_EmptyMessage_NoError()
    {
        // Arrange
        var logEvent = CreateLogEvent(string.Empty);
        var factory = new TestPropertyFactory();

        // Act
        var act = () => _enricher.Enrich(logEvent, factory);

        // Assert
        act.Should().NotThrow();
    }

    [Fact(DisplayName = "Enrich should handle very long message")]
    public void Enrich_VeryLongMessage_Processes()
    {
        // Arrange
        var longText = string.Join(" ", Enumerable.Repeat("text", 1000));
        var input = $"{longText} email: test@example.com {longText}";
        var logEvent = CreateLogEvent(input);
        var factory = new TestPropertyFactory();

        // Act
        var act = () => _enricher.Enrich(logEvent, factory);

        // Assert
        act.Should().NotThrow();
        factory.CreatedProperty.Should().NotBeNull();
    }

    [Fact(DisplayName = "Enrich should handle special characters in message")]
    public void Enrich_SpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var input = "Message with <tags> & \"quotes\" from 192.168.1.1";
        var logEvent = CreateLogEvent(input);
        var factory = new TestPropertyFactory();

        // Act
        var act = () => _enricher.Enrich(logEvent, factory);

        // Assert
        act.Should().NotThrow();
        factory.CreatedProperty.Should().NotBeNull();
    }

    #endregion

    #region Helpers

    private static LogEvent CreateLogEvent(string message)
    {
        var tokens = new List<MessageTemplateToken>
        {
            new TextToken(message)
        };
        var template = new MessageTemplate(tokens);

        return new LogEvent(
            DateTimeOffset.Now,
            LogEventLevel.Information,
            null,
            template,
            new List<LogEventProperty>());
    }

    /// <summary>
    /// Test implementation of ILogEventPropertyFactory to capture created properties.
    /// </summary>
    private class TestPropertyFactory : ILogEventPropertyFactory
    {
        public LogEventProperty? CreatedProperty { get; private set; }

        public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
        {
            CreatedProperty = new LogEventProperty(name, new ScalarValue(value));
            return CreatedProperty;
        }
    }

    #endregion
}
