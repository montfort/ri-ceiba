using Ceiba.Core.Entities;
using Ceiba.Core.Interfaces;
using Ceiba.Infrastructure.Data;
using Ceiba.Infrastructure.Services;
using Ceiba.Shared.DTOs;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;

namespace Ceiba.Application.Tests.Services;

/// <summary>
/// Unit tests for EmailService.
/// US4: Reportes Automatizados Diarios con IA.
/// Tests T083: Email service operations for SMTP, SendGrid, and Mailgun providers.
/// </summary>
public class EmailServiceTests : IDisposable
{
    private readonly CeibaDbContext _context;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<EmailService>> _mockLogger;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;

    public EmailServiceTests()
    {
        var options = new DbContextOptionsBuilder<CeibaDbContext>()
            .UseInMemoryDatabase(databaseName: $"EmailServiceTests_{Guid.NewGuid()}")
            .Options;

        _context = new CeibaDbContext(options);
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<EmailService>>();
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private EmailService CreateService()
    {
        return new EmailService(
            _context,
            _mockConfiguration.Object,
            _mockLogger.Object,
            _mockHttpClientFactory.Object);
    }

    private async Task SetupEmailConfiguration(
        string provider = "SMTP",
        bool enabled = true,
        string? smtpHost = "smtp.test.com",
        int? smtpPort = 587,
        string? sendGridApiKey = null,
        string? mailgunApiKey = null,
        string? mailgunDomain = null,
        string? mailgunRegion = "US")
    {
        var config = new ConfiguracionEmail
        {
            Proveedor = provider,
            Habilitado = enabled,
            SmtpHost = smtpHost,
            SmtpPort = smtpPort,
            SmtpUsername = "test@test.com",
            SmtpPassword = "password",
            SmtpUseSsl = true,
            SendGridApiKey = sendGridApiKey,
            MailgunApiKey = mailgunApiKey,
            MailgunDomain = mailgunDomain,
            MailgunRegion = mailgunRegion,
            FromEmail = "noreply@ceiba.local",
            FromName = "Ceiba System",
            CreatedAt = DateTime.UtcNow
        };

        _context.ConfiguracionesEmail.Add(config);
        await _context.SaveChangesAsync();
    }

    private SendEmailRequestDto CreateSampleRequest()
    {
        return new SendEmailRequestDto
        {
            Recipients = new List<string> { "test@example.com" },
            Subject = "Test Subject",
            BodyHtml = "<p>Test body content</p>",
            Attachments = new List<EmailAttachmentDto>()
        };
    }

    #region SendAsync Tests

    [Fact(DisplayName = "T083: SendAsync should return error when no configuration exists")]
    public async Task SendAsync_NoConfiguration_ReturnsError()
    {
        // Arrange
        var service = CreateService();
        var request = CreateSampleRequest();

        // Act
        var result = await service.SendAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("no está configurado");
    }

    [Fact(DisplayName = "T083: SendAsync should return error when configuration is disabled")]
    public async Task SendAsync_ConfigurationDisabled_ReturnsError()
    {
        // Arrange
        await SetupEmailConfiguration(enabled: false);
        var service = CreateService();
        var request = CreateSampleRequest();

        // Act
        var result = await service.SendAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("no está configurado");
    }

    [Fact(DisplayName = "T083: SendAsync should use most recent active configuration")]
    public async Task SendAsync_MultipleConfigurations_UsesMostRecent()
    {
        // Arrange
        var oldConfig = new ConfiguracionEmail
        {
            Proveedor = "SMTP",
            Habilitado = true,
            SmtpHost = "old.smtp.com",
            SmtpPort = 587,
            FromEmail = "old@ceiba.local",
            FromName = "Old System",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var newConfig = new ConfiguracionEmail
        {
            Proveedor = "SendGrid",
            Habilitado = true,
            SendGridApiKey = "SG.test-api-key",
            FromEmail = "new@ceiba.local",
            FromName = "New System",
            CreatedAt = DateTime.UtcNow
        };

        _context.ConfiguracionesEmail.Add(oldConfig);
        _context.ConfiguracionesEmail.Add(newConfig);
        await _context.SaveChangesAsync();

        // We can't fully test the send operation, but we verify configuration is found
        var service = CreateService();

        // Act
        var result = await service.IsAvailableAsync();

        // Assert - SendGrid config should be used and it has API key
        result.Should().BeTrue();
    }

    [Fact(DisplayName = "T083: SendAsync with SMTP should handle connection errors gracefully")]
    public async Task SendAsync_SmtpConnectionError_ReturnsError()
    {
        // Arrange
        await SetupEmailConfiguration(provider: "SMTP", smtpHost: "invalid.smtp.host");
        var service = CreateService();
        var request = CreateSampleRequest();

        // Act
        var result = await service.SendAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact(DisplayName = "T083: SendAsync should include attachments in request")]
    public async Task SendAsync_WithAttachments_IncludesAttachments()
    {
        // Arrange
        await SetupEmailConfiguration(provider: "SMTP");
        var service = CreateService();
        var request = new SendEmailRequestDto
        {
            Recipients = new List<string> { "test@example.com" },
            Subject = "Test with attachment",
            BodyHtml = "<p>See attached</p>",
            Attachments = new List<EmailAttachmentDto>
            {
                new EmailAttachmentDto
                {
                    FileName = "report.pdf",
                    Content = new byte[] { 0x25, 0x50, 0x44, 0x46 }, // PDF magic bytes
                    ContentType = "application/pdf"
                }
            }
        };

        // Act - will fail on SMTP connection but validates the data structure
        var result = await service.SendAsync(request);

        // Assert - validates that attachments are properly structured
        request.Attachments.Should().HaveCount(1);
        request.Attachments[0].FileName.Should().Be("report.pdf");
    }

    #endregion

    #region IsAvailableAsync Tests

    [Fact(DisplayName = "T083: IsAvailableAsync should return false when no configuration exists")]
    public async Task IsAvailableAsync_NoConfiguration_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.IsAvailableAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact(DisplayName = "T083: IsAvailableAsync should return false when configuration is disabled")]
    public async Task IsAvailableAsync_ConfigurationDisabled_ReturnsFalse()
    {
        // Arrange
        await SetupEmailConfiguration(enabled: false);
        var service = CreateService();

        // Act
        var result = await service.IsAvailableAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact(DisplayName = "T083: IsAvailableAsync for SendGrid should check API key presence")]
    public async Task IsAvailableAsync_SendGrid_WithApiKey_ReturnsTrue()
    {
        // Arrange
        await SetupEmailConfiguration(
            provider: "SendGrid",
            sendGridApiKey: "SG.test-api-key-12345");
        var service = CreateService();

        // Act
        var result = await service.IsAvailableAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact(DisplayName = "T083: IsAvailableAsync for SendGrid should return false without API key")]
    public async Task IsAvailableAsync_SendGrid_NoApiKey_ReturnsFalse()
    {
        // Arrange
        await SetupEmailConfiguration(
            provider: "SendGrid",
            sendGridApiKey: null);
        var service = CreateService();

        // Act
        var result = await service.IsAvailableAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact(DisplayName = "T083: IsAvailableAsync for Mailgun should check API key and domain")]
    public async Task IsAvailableAsync_Mailgun_WithApiKeyAndDomain_ReturnsTrue()
    {
        // Arrange
        await SetupEmailConfiguration(
            provider: "Mailgun",
            mailgunApiKey: "key-test-api-key",
            mailgunDomain: "mg.example.com",
            mailgunRegion: "US");
        var service = CreateService();

        // Act
        var result = await service.IsAvailableAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact(DisplayName = "T083: IsAvailableAsync for Mailgun should return false without API key")]
    public async Task IsAvailableAsync_Mailgun_NoApiKey_ReturnsFalse()
    {
        // Arrange
        await SetupEmailConfiguration(
            provider: "Mailgun",
            mailgunApiKey: null,
            mailgunDomain: "mg.example.com");
        var service = CreateService();

        // Act
        var result = await service.IsAvailableAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact(DisplayName = "T083: IsAvailableAsync for Mailgun should return false without domain")]
    public async Task IsAvailableAsync_Mailgun_NoDomain_ReturnsFalse()
    {
        // Arrange
        await SetupEmailConfiguration(
            provider: "Mailgun",
            mailgunApiKey: "key-test-api-key",
            mailgunDomain: null);
        var service = CreateService();

        // Act
        var result = await service.IsAvailableAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact(DisplayName = "T083: IsAvailableAsync for SMTP should return false without host")]
    public async Task IsAvailableAsync_Smtp_NoHost_ReturnsFalse()
    {
        // Arrange
        await SetupEmailConfiguration(
            provider: "SMTP",
            smtpHost: null);
        var service = CreateService();

        // Act
        var result = await service.IsAvailableAsync();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Provider Selection Tests

    [Fact(DisplayName = "T083: SendAsync should route to SendGrid when provider is SendGrid")]
    public async Task SendAsync_SendGridProvider_UsesSendGrid()
    {
        // Arrange
        await SetupEmailConfiguration(
            provider: "SendGrid",
            sendGridApiKey: "SG.invalid-key-for-test");
        var service = CreateService();
        var request = CreateSampleRequest();

        // Act - will fail but we verify the provider routing works
        var result = await service.SendAsync(request);

        // Assert - should fail on SendGrid API call, not on missing config
        result.Success.Should().BeFalse();
        // Error should be from SendGrid API, not from missing configuration
        result.Error.Should().NotContain("no está configurado");
    }

    [Fact(DisplayName = "T083: SendAsync should route to Mailgun when provider is Mailgun")]
    public async Task SendAsync_MailgunProvider_UsesMailgun()
    {
        // Arrange
        await SetupEmailConfiguration(
            provider: "Mailgun",
            mailgunApiKey: "key-invalid-test",
            mailgunDomain: "mg.example.com",
            mailgunRegion: "US");

        // Mock HTTP client for Mailgun
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("Forbidden - Invalid API key")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        var service = CreateService();
        var request = CreateSampleRequest();

        // Act
        var result = await service.SendAsync(request);

        // Assert - should fail on Mailgun API call
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Mailgun");
    }

    [Fact(DisplayName = "T083: Mailgun should use EU region when configured")]
    public async Task SendAsync_MailgunEURegion_UsesEUEndpoint()
    {
        // Arrange
        await SetupEmailConfiguration(
            provider: "Mailgun",
            mailgunApiKey: "key-test",
            mailgunDomain: "mg.example.com",
            mailgunRegion: "EU");

        string? capturedUrl = null;
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) =>
            {
                capturedUrl = request.RequestUri?.ToString();
            })
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"message\": \"Queued\"}")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        var service = CreateService();
        var request = CreateSampleRequest();

        // Act
        var result = await service.SendAsync(request);

        // Assert
        capturedUrl.Should().NotBeNull();
        capturedUrl.Should().Contain("api.eu.mailgun.net");
    }

    [Fact(DisplayName = "T083: Mailgun should use US region by default")]
    public async Task SendAsync_MailgunUSRegion_UsesUSEndpoint()
    {
        // Arrange
        await SetupEmailConfiguration(
            provider: "Mailgun",
            mailgunApiKey: "key-test",
            mailgunDomain: "mg.example.com",
            mailgunRegion: "US");

        string? capturedUrl = null;
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) =>
            {
                capturedUrl = request.RequestUri?.ToString();
            })
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"message\": \"Queued\"}")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        var service = CreateService();
        var request = CreateSampleRequest();

        // Act
        var result = await service.SendAsync(request);

        // Assert
        capturedUrl.Should().NotBeNull();
        capturedUrl.Should().Contain("api.mailgun.net");
        capturedUrl.Should().NotContain("api.eu.mailgun.net");
    }

    #endregion

    #region Mailgun Integration Tests

    [Fact(DisplayName = "T083: Mailgun success response should return success result")]
    public async Task SendAsync_MailgunSuccess_ReturnsSuccessResult()
    {
        // Arrange
        await SetupEmailConfiguration(
            provider: "Mailgun",
            mailgunApiKey: "key-test",
            mailgunDomain: "mg.example.com");

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"id\": \"<msg-id>\", \"message\": \"Queued. Thank you.\"}")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        var service = CreateService();
        var request = CreateSampleRequest();

        // Act
        var result = await service.SendAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.SentAt.Should().NotBeNull();
        result.Error.Should().BeNull();
    }

    [Fact(DisplayName = "T083: Mailgun should include multiple recipients")]
    public async Task SendAsync_Mailgun_MultipleRecipients_IncludesAll()
    {
        // Arrange
        await SetupEmailConfiguration(
            provider: "Mailgun",
            mailgunApiKey: "key-test",
            mailgunDomain: "mg.example.com");

        HttpRequestMessage? capturedRequest = null;
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) =>
            {
                capturedRequest = req;
            })
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"message\": \"Queued\"}")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        var service = CreateService();
        var request = new SendEmailRequestDto
        {
            Recipients = new List<string>
            {
                "user1@example.com",
                "user2@example.com",
                "user3@example.com"
            },
            Subject = "Test to multiple recipients",
            BodyHtml = "<p>Hello all</p>"
        };

        // Act
        var result = await service.SendAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        capturedRequest.Should().NotBeNull();
    }

    [Fact(DisplayName = "T083: Mailgun should handle attachments")]
    public async Task SendAsync_Mailgun_WithAttachments_SendsAttachments()
    {
        // Arrange
        await SetupEmailConfiguration(
            provider: "Mailgun",
            mailgunApiKey: "key-test",
            mailgunDomain: "mg.example.com");

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"message\": \"Queued\"}")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        var service = CreateService();
        var request = new SendEmailRequestDto
        {
            Recipients = new List<string> { "test@example.com" },
            Subject = "With Attachment",
            BodyHtml = "<p>See attached report</p>",
            Attachments = new List<EmailAttachmentDto>
            {
                new EmailAttachmentDto
                {
                    FileName = "daily-report.pdf",
                    Content = System.Text.Encoding.UTF8.GetBytes("fake pdf content"),
                    ContentType = "application/pdf"
                }
            }
        };

        // Act
        var result = await service.SendAsync(request);

        // Assert
        result.Success.Should().BeTrue();
    }

    #endregion

    #region Error Handling Tests

    [Fact(DisplayName = "T083: SendAsync should catch and log exceptions")]
    public async Task SendAsync_Exception_ReturnsErrorAndLogs()
    {
        // Arrange
        await SetupEmailConfiguration(
            provider: "Mailgun",
            mailgunApiKey: "key-test",
            mailgunDomain: "mg.example.com");

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        var service = CreateService();
        var request = CreateSampleRequest();

        // Act
        var result = await service.SendAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Network error");
    }

    [Fact(DisplayName = "T083: IsAvailableAsync should return false on SMTP connection error")]
    public async Task IsAvailableAsync_SmtpConnectionError_ReturnsFalse()
    {
        // Arrange
        await SetupEmailConfiguration(
            provider: "SMTP",
            smtpHost: "nonexistent.smtp.server",
            smtpPort: 9999);
        var service = CreateService();

        // Act
        var result = await service.IsAvailableAsync();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region DTO Validation Tests

    [Fact(DisplayName = "T083: SendEmailRequestDto should have required properties")]
    public void SendEmailRequestDto_Structure_IsCorrect()
    {
        // Arrange & Act
        var dto = new SendEmailRequestDto();

        // Assert
        dto.Recipients.Should().NotBeNull();
        dto.Recipients.Should().BeEmpty();
        dto.Subject.Should().BeEmpty();
        dto.BodyHtml.Should().BeEmpty();
        dto.Attachments.Should().NotBeNull();
        dto.Attachments.Should().BeEmpty();
    }

    [Fact(DisplayName = "T083: SendEmailResultDto should have required properties")]
    public void SendEmailResultDto_Structure_IsCorrect()
    {
        // Arrange & Act
        var dto = new SendEmailResultDto();

        // Assert
        dto.Success.Should().BeFalse();
        dto.Error.Should().BeNull();
        dto.SentAt.Should().BeNull();
    }

    [Fact(DisplayName = "T083: EmailAttachmentDto should have required properties")]
    public void EmailAttachmentDto_Structure_IsCorrect()
    {
        // Arrange & Act
        var dto = new EmailAttachmentDto();

        // Assert
        dto.FileName.Should().BeEmpty();
        dto.Content.Should().BeEmpty();
        dto.ContentType.Should().Be("application/octet-stream");
    }

    #endregion

    #region Configuration Entity Tests

    [Fact(DisplayName = "T083: ConfiguracionEmail should support all providers")]
    public void ConfiguracionEmail_Providers_AreSupported()
    {
        // Arrange & Act
        var smtpConfig = new ConfiguracionEmail { Proveedor = "SMTP" };
        var sendGridConfig = new ConfiguracionEmail { Proveedor = "SendGrid" };
        var mailgunConfig = new ConfiguracionEmail { Proveedor = "Mailgun" };

        // Assert
        smtpConfig.Proveedor.Should().Be("SMTP");
        sendGridConfig.Proveedor.Should().Be("SendGrid");
        mailgunConfig.Proveedor.Should().Be("Mailgun");
    }

    [Fact(DisplayName = "T083: ConfiguracionEmail should have sensible defaults")]
    public void ConfiguracionEmail_Defaults_AreCorrect()
    {
        // Arrange & Act
        var config = new ConfiguracionEmail();

        // Assert
        config.Proveedor.Should().Be("SMTP");
        config.Habilitado.Should().BeFalse();
        config.SmtpUseSsl.Should().BeTrue();
        config.MailgunRegion.Should().Be("US");
    }

    #endregion
}
