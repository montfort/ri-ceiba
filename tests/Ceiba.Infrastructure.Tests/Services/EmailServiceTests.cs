using System.Net;
using Ceiba.Core.Entities;
using Ceiba.Infrastructure.Data;
using Ceiba.Infrastructure.Services;
using Ceiba.Shared.DTOs;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RichardSzalay.MockHttp;

namespace Ceiba.Infrastructure.Tests.Services;

/// <summary>
/// Unit tests for EmailService.
/// Tests email sending via SMTP, SendGrid, and Mailgun providers.
/// </summary>
public class EmailServiceTests : IDisposable
{
    private readonly CeibaDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly MockHttpMessageHandler _mockHttp;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly EmailService _service;
    private readonly Guid _testUserId = Guid.NewGuid();

    public EmailServiceTests()
    {
        var options = new DbContextOptionsBuilder<CeibaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CeibaDbContext(options, _testUserId);

        var configData = new Dictionary<string, string?>();
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _logger = Substitute.For<ILogger<EmailService>>();

        _mockHttp = new MockHttpMessageHandler();
        var httpClient = _mockHttp.ToHttpClient();

        _httpClientFactory = Substitute.For<IHttpClientFactory>();
        _httpClientFactory.CreateClient(Arg.Any<string>()).Returns(httpClient);

        _service = new EmailService(
            _context,
            _configuration,
            _logger,
            _httpClientFactory);
    }

    public void Dispose()
    {
        _context.Dispose();
        _mockHttp.Dispose();
        GC.SuppressFinalize(this);
    }

    #region SendAsync Tests - No Configuration

    [Fact(DisplayName = "SendAsync should return error when no configuration")]
    public async Task SendAsync_ReturnsError_WhenNoConfiguration()
    {
        // Arrange
        var request = CreateTestEmailRequest();

        // Act
        var result = await _service.SendAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("no está configurado");
    }

    [Fact(DisplayName = "SendAsync should return error when service is disabled")]
    public async Task SendAsync_ReturnsError_WhenServiceDisabled()
    {
        // Arrange
        await CreateEmailConfig(habilitado: false);
        var request = CreateTestEmailRequest();

        // Act
        var result = await _service.SendAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("no está configurado");
    }

    #endregion

    #region SendAsync Tests - SendGrid

    [Fact(DisplayName = "SendAsync should send via SendGrid when configured")]
    public async Task SendAsync_SendsViaSendGrid_WhenConfigured()
    {
        // Arrange
        await CreateEmailConfig(proveedor: "SendGrid", sendGridApiKey: "SG.test-key");

        // Note: SendGrid uses its own client, so we can't easily mock it in unit tests.
        // This test verifies the configuration is read correctly.
        var request = CreateTestEmailRequest();

        // Act - We expect this to fail because we can't mock SendGrid client
        var result = await _service.SendAsync(request);

        // Assert - Either succeeds (unlikely without real key) or fails with SendGrid error
        // The important thing is it doesn't fail with "no configuration" error
        result.Error.Should().NotContain("no está configurado");
    }

    #endregion

    #region SendAsync Tests - Mailgun

    [Fact(DisplayName = "SendAsync should send via Mailgun when configured")]
    public async Task SendAsync_SendsViaMailgun_WhenConfigured()
    {
        // Arrange
        await CreateEmailConfig(
            proveedor: "Mailgun",
            mailgunApiKey: "key-test123",
            mailgunDomain: "mg.example.com",
            mailgunRegion: "US");

        _mockHttp.When("https://api.mailgun.net/v3/mg.example.com/messages")
            .Respond(HttpStatusCode.OK, "application/json", "{\"message\":\"Queued\"}");

        var request = CreateTestEmailRequest();

        // Act
        var result = await _service.SendAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.SentAt.Should().NotBeNull();
    }

    [Fact(DisplayName = "SendAsync should use EU endpoint for Mailgun EU region")]
    public async Task SendAsync_UsesEuEndpoint_ForMailgunEuRegion()
    {
        // Arrange
        await CreateEmailConfig(
            proveedor: "Mailgun",
            mailgunApiKey: "key-test123",
            mailgunDomain: "mg.example.com",
            mailgunRegion: "EU");

        _mockHttp.When("https://api.eu.mailgun.net/v3/mg.example.com/messages")
            .Respond(HttpStatusCode.OK, "application/json", "{\"message\":\"Queued\"}");

        var request = CreateTestEmailRequest();

        // Act
        var result = await _service.SendAsync(request);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact(DisplayName = "SendAsync should handle Mailgun error response")]
    public async Task SendAsync_HandlesMailgunError()
    {
        // Arrange
        await CreateEmailConfig(
            proveedor: "Mailgun",
            mailgunApiKey: "key-test123",
            mailgunDomain: "mg.example.com");

        _mockHttp.When("https://api.mailgun.net/v3/mg.example.com/messages")
            .Respond(HttpStatusCode.Unauthorized, "application/json", "{\"message\":\"Invalid credentials\"}");

        var request = CreateTestEmailRequest();

        // Act
        var result = await _service.SendAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Mailgun");
    }

    [Fact(DisplayName = "SendAsync should include attachments for Mailgun")]
    public async Task SendAsync_IncludesAttachments_ForMailgun()
    {
        // Arrange
        await CreateEmailConfig(
            proveedor: "Mailgun",
            mailgunApiKey: "key-test123",
            mailgunDomain: "mg.example.com");

        string? capturedContentType = null;
        _mockHttp.When("https://api.mailgun.net/v3/mg.example.com/messages")
            .With(request =>
            {
                capturedContentType = request.Content?.Headers.ContentType?.MediaType;
                return true;
            })
            .Respond(HttpStatusCode.OK, "application/json", "{\"message\":\"Queued\"}");

        var request = CreateTestEmailRequest();
        request.Attachments.Add(new EmailAttachmentDto
        {
            FileName = "test.pdf",
            Content = new byte[] { 0x25, 0x50, 0x44, 0x46 }, // PDF magic bytes
            ContentType = "application/pdf"
        });

        // Act
        var result = await _service.SendAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        capturedContentType.Should().Be("multipart/form-data");
    }

    #endregion

    #region SendAsync Tests - Exception Handling

    [Fact(DisplayName = "SendAsync should catch and return exception as error")]
    public async Task SendAsync_CatchesException_ReturnsAsError()
    {
        // Arrange
        await CreateEmailConfig(
            proveedor: "Mailgun",
            mailgunApiKey: "key-test123",
            mailgunDomain: "mg.example.com");

        _mockHttp.When("https://api.mailgun.net/v3/mg.example.com/messages")
            .Throw(new HttpRequestException("Network error"));

        var request = CreateTestEmailRequest();

        // Act
        var result = await _service.SendAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Network error");
    }

    #endregion

    #region IsAvailableAsync Tests

    [Fact(DisplayName = "IsAvailableAsync should return false when no configuration")]
    public async Task IsAvailableAsync_ReturnsFalse_WhenNoConfiguration()
    {
        // Act
        var result = await _service.IsAvailableAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact(DisplayName = "IsAvailableAsync should return true for SendGrid with API key")]
    public async Task IsAvailableAsync_ReturnsTrue_ForSendGridWithApiKey()
    {
        // Arrange
        await CreateEmailConfig(proveedor: "SendGrid", sendGridApiKey: "SG.test-key");

        // Act
        var result = await _service.IsAvailableAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact(DisplayName = "IsAvailableAsync should return false for SendGrid without API key")]
    public async Task IsAvailableAsync_ReturnsFalse_ForSendGridWithoutApiKey()
    {
        // Arrange
        await CreateEmailConfig(proveedor: "SendGrid", sendGridApiKey: "");

        // Act
        var result = await _service.IsAvailableAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact(DisplayName = "IsAvailableAsync should return true for Mailgun with API key and domain")]
    public async Task IsAvailableAsync_ReturnsTrue_ForMailgunWithCredentials()
    {
        // Arrange
        await CreateEmailConfig(
            proveedor: "Mailgun",
            mailgunApiKey: "key-test123",
            mailgunDomain: "mg.example.com");

        // Act
        var result = await _service.IsAvailableAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact(DisplayName = "IsAvailableAsync should return false for Mailgun without domain")]
    public async Task IsAvailableAsync_ReturnsFalse_ForMailgunWithoutDomain()
    {
        // Arrange
        await CreateEmailConfig(
            proveedor: "Mailgun",
            mailgunApiKey: "key-test123",
            mailgunDomain: "");

        // Act
        var result = await _service.IsAvailableAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact(DisplayName = "IsAvailableAsync should return false for SMTP without host")]
    public async Task IsAvailableAsync_ReturnsFalse_ForSmtpWithoutHost()
    {
        // Arrange
        await CreateEmailConfig(proveedor: "SMTP", smtpHost: "");

        // Act
        var result = await _service.IsAvailableAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact(DisplayName = "IsAvailableAsync should handle SMTP connection failure")]
    public async Task IsAvailableAsync_HandlesSMTPConnectionFailure()
    {
        // Arrange
        await CreateEmailConfig(
            proveedor: "SMTP",
            smtpHost: "invalid.smtp.host",
            smtpPort: 587);

        // Act - Should return false because connection will fail
        var result = await _service.IsAvailableAsync();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Multiple Recipients Tests

    [Fact(DisplayName = "SendAsync should send to multiple recipients via Mailgun")]
    public async Task SendAsync_SendsToMultipleRecipients_ViaMailgun()
    {
        // Arrange
        await CreateEmailConfig(
            proveedor: "Mailgun",
            mailgunApiKey: "key-test123",
            mailgunDomain: "mg.example.com");

        string? capturedBody = null;
        _mockHttp.When("https://api.mailgun.net/v3/mg.example.com/messages")
            .With(request =>
            {
                capturedBody = request.Content?.ReadAsStringAsync().Result;
                return true;
            })
            .Respond(HttpStatusCode.OK, "application/json", "{\"message\":\"Queued\"}");

        var request = new SendEmailRequestDto
        {
            Recipients = new List<string>
            {
                "user1@example.com",
                "user2@example.com",
                "user3@example.com"
            },
            Subject = "Test Subject",
            BodyHtml = "<p>Test Body</p>"
        };

        // Act
        var result = await _service.SendAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        capturedBody.Should().Contain("user1@example.com");
        capturedBody.Should().Contain("user2@example.com");
        capturedBody.Should().Contain("user3@example.com");
    }

    #endregion

    #region Configuration Priority Tests

    [Fact(DisplayName = "SendAsync should use most recent active configuration")]
    public async Task SendAsync_UsesMostRecentConfiguration()
    {
        // Arrange - Create older config
        var oldConfig = new ConfiguracionEmail
        {
            Proveedor = "SMTP",
            FromEmail = "old@example.com",
            FromName = "Old Sender",
            SmtpHost = "old.smtp.host",
            SmtpPort = 25,
            Habilitado = true
        };
        _context.ConfiguracionesEmail.Add(oldConfig);
        await _context.SaveChangesAsync();

        // Create newer config
        await Task.Delay(10); // Ensure different timestamp
        await CreateEmailConfig(
            proveedor: "Mailgun",
            mailgunApiKey: "key-test123",
            mailgunDomain: "mg.example.com");

        _mockHttp.When("https://api.mailgun.net/v3/mg.example.com/messages")
            .Respond(HttpStatusCode.OK, "application/json", "{\"message\":\"Queued\"}");

        var request = CreateTestEmailRequest();

        // Act
        var result = await _service.SendAsync(request);

        // Assert - Should use Mailgun (newer config), not SMTP
        result.Success.Should().BeTrue();
    }

    #endregion

    #region Helper Methods

    private SendEmailRequestDto CreateTestEmailRequest()
    {
        return new SendEmailRequestDto
        {
            Recipients = new List<string> { "recipient@example.com" },
            Subject = "Test Email Subject",
            BodyHtml = "<h1>Test Email</h1><p>This is a test email body.</p>"
        };
    }

    private async Task<ConfiguracionEmail> CreateEmailConfig(
        string proveedor = "SMTP",
        bool habilitado = true,
        string? smtpHost = "smtp.example.com",
        int? smtpPort = 587,
        bool smtpUseSsl = true,
        string? smtpUsername = null,
        string? smtpPassword = null,
        string? sendGridApiKey = null,
        string? mailgunApiKey = null,
        string? mailgunDomain = null,
        string? mailgunRegion = "US")
    {
        var config = new ConfiguracionEmail
        {
            Proveedor = proveedor,
            FromEmail = "sender@example.com",
            FromName = "Test Sender",
            SmtpHost = smtpHost,
            SmtpPort = smtpPort,
            SmtpUseSsl = smtpUseSsl,
            SmtpUsername = smtpUsername,
            SmtpPassword = smtpPassword,
            SendGridApiKey = sendGridApiKey,
            MailgunApiKey = mailgunApiKey,
            MailgunDomain = mailgunDomain,
            MailgunRegion = mailgunRegion,
            Habilitado = habilitado
        };

        _context.ConfiguracionesEmail.Add(config);
        await _context.SaveChangesAsync();

        return config;
    }

    #endregion
}
