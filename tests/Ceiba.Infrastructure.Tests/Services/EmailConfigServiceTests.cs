using System.Security.Claims;
using Ceiba.Core.Entities;
using Ceiba.Core.Interfaces;
using Ceiba.Infrastructure.Data;
using Ceiba.Infrastructure.Services;
using Ceiba.Shared.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Ceiba.Infrastructure.Tests.Services;

/// <summary>
/// Unit tests for EmailConfigService.
/// Tests email configuration management, provider switching, and testing.
/// </summary>
public class EmailConfigServiceTests : IDisposable
{
    private readonly CeibaDbContext _context;
    private readonly Mock<ILogger<EmailConfigService>> _loggerMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Guid _testUserId = Guid.NewGuid();

    public EmailConfigServiceTests()
    {
        var options = new DbContextOptionsBuilder<CeibaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CeibaDbContext(options);
        _loggerMock = new Mock<ILogger<EmailConfigService>>();
        _emailServiceMock = new Mock<IEmailService>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

        SetupAuthenticatedUser(_testUserId);
    }

    private void SetupAuthenticatedUser(Guid userId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
    }

    private void SetupUnauthenticatedUser()
    {
        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
    }

    private EmailConfigService CreateService()
    {
        return new EmailConfigService(
            _context,
            _loggerMock.Object,
            _emailServiceMock.Object,
            _httpContextAccessorMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region GetConfigurationAsync Tests

    [Fact]
    public async Task GetConfigurationAsync_NoConfiguration_ReturnsNull()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetConfigurationAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetConfigurationAsync_ConfigurationExists_ReturnsDto()
    {
        // Arrange
        var config = new ConfiguracionEmail
        {
            Proveedor = "SMTP",
            Habilitado = true,
            SmtpHost = "smtp.test.com",
            SmtpPort = 587,
            FromEmail = "test@test.com",
            FromName = "Test",
            UsuarioId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };
        _context.ConfiguracionesEmail.Add(config);
        await _context.SaveChangesAsync();

        var service = CreateService();

        // Act
        var result = await service.GetConfigurationAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("SMTP", result.Proveedor);
        Assert.True(result.Habilitado);
        Assert.Equal("smtp.test.com", result.SmtpHost);
    }

    [Fact]
    public async Task GetConfigurationAsync_MultipleConfigurations_ReturnsMostRecent()
    {
        // Arrange
        var oldConfig = new ConfiguracionEmail
        {
            Proveedor = "SMTP",
            Habilitado = false,
            FromEmail = "old@test.com",
            UsuarioId = _testUserId,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        var newConfig = new ConfiguracionEmail
        {
            Proveedor = "SendGrid",
            Habilitado = true,
            FromEmail = "new@test.com",
            UsuarioId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };
        _context.ConfiguracionesEmail.AddRange(oldConfig, newConfig);
        await _context.SaveChangesAsync();

        var service = CreateService();

        // Act
        var result = await service.GetConfigurationAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("SendGrid", result.Proveedor);
        Assert.Equal("new@test.com", result.FromEmail);
    }

    #endregion

    #region UpdateConfigurationAsync Tests

    [Fact]
    public async Task UpdateConfigurationAsync_UnauthenticatedUser_ThrowsUnauthorized()
    {
        // Arrange
        SetupUnauthenticatedUser();
        var service = CreateService();
        var dto = new EmailConfigUpdateDto { Proveedor = "SMTP" };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.UpdateConfigurationAsync(dto));
    }

    [Fact]
    public async Task UpdateConfigurationAsync_NewConfiguration_CreatesConfig()
    {
        // Arrange
        var service = CreateService();
        var dto = new EmailConfigUpdateDto
        {
            Proveedor = "SMTP",
            Habilitado = true,
            SmtpHost = "smtp.new.com",
            SmtpPort = 587,
            SmtpUsername = "user",
            SmtpPassword = "pass",
            SmtpUseSsl = true,
            FromEmail = "from@test.com",
            FromName = "Test Sender"
        };

        // Act
        var result = await service.UpdateConfigurationAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("SMTP", result.Proveedor);
        Assert.Equal("smtp.new.com", result.SmtpHost);
        Assert.Equal(587, result.SmtpPort);
    }

    [Fact]
    public async Task UpdateConfigurationAsync_ExistingConfiguration_UpdatesConfig()
    {
        // Arrange
        var existingConfig = new ConfiguracionEmail
        {
            Proveedor = "SMTP",
            Habilitado = false,
            SmtpHost = "old.smtp.com",
            FromEmail = "old@test.com",
            UsuarioId = _testUserId,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _context.ConfiguracionesEmail.Add(existingConfig);
        await _context.SaveChangesAsync();

        var service = CreateService();
        var dto = new EmailConfigUpdateDto
        {
            Proveedor = "SMTP",
            Habilitado = true,
            SmtpHost = "new.smtp.com",
            SmtpPort = 465,
            FromEmail = "new@test.com",
            FromName = "New Sender"
        };

        // Act
        var result = await service.UpdateConfigurationAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Habilitado);
        Assert.Equal("new.smtp.com", result.SmtpHost);
        Assert.Equal("new@test.com", result.FromEmail);
    }

    [Fact]
    public async Task UpdateConfigurationAsync_SwitchToSendGrid_ClearsSmtpSettings()
    {
        // Arrange
        var existingConfig = new ConfiguracionEmail
        {
            Proveedor = "SMTP",
            SmtpHost = "smtp.test.com",
            SmtpPort = 587,
            SmtpUsername = "user",
            SmtpPassword = "pass",
            FromEmail = "test@test.com",
            UsuarioId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };
        _context.ConfiguracionesEmail.Add(existingConfig);
        await _context.SaveChangesAsync();

        var service = CreateService();
        var dto = new EmailConfigUpdateDto
        {
            Proveedor = "SendGrid",
            Habilitado = true,
            SendGridApiKey = "sg-api-key",
            FromEmail = "test@test.com",
            FromName = "Test"
        };

        // Act
        var result = await service.UpdateConfigurationAsync(dto);

        // Assert
        Assert.Equal("SendGrid", result.Proveedor);
        Assert.Null(result.SmtpHost);
        Assert.Null(result.SmtpPort);
        Assert.True(result.HasSendGridApiKey);
    }

    [Fact]
    public async Task UpdateConfigurationAsync_SwitchToMailgun_ClearsOtherProviderSettings()
    {
        // Arrange
        var service = CreateService();
        var dto = new EmailConfigUpdateDto
        {
            Proveedor = "Mailgun",
            Habilitado = true,
            MailgunApiKey = "mg-api-key",
            MailgunDomain = "mg.test.com",
            MailgunRegion = "US",
            FromEmail = "test@test.com",
            FromName = "Test"
        };

        // Act
        var result = await service.UpdateConfigurationAsync(dto);

        // Assert
        Assert.Equal("Mailgun", result.Proveedor);
        Assert.True(result.HasMailgunApiKey);
        Assert.Equal("mg.test.com", result.MailgunDomain);
        Assert.Equal("US", result.MailgunRegion);
    }

    [Fact]
    public async Task UpdateConfigurationAsync_PasswordNotProvided_KeepsExisting()
    {
        // Arrange
        var existingConfig = new ConfiguracionEmail
        {
            Proveedor = "SMTP",
            SmtpHost = "smtp.test.com",
            SmtpPassword = "existing-password",
            FromEmail = "test@test.com",
            UsuarioId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };
        _context.ConfiguracionesEmail.Add(existingConfig);
        await _context.SaveChangesAsync();
        _context.Entry(existingConfig).State = EntityState.Detached;

        var service = CreateService();
        var dto = new EmailConfigUpdateDto
        {
            Proveedor = "SMTP",
            Habilitado = true,
            SmtpHost = "smtp.test.com",
            SmtpPassword = null, // Not provided
            FromEmail = "test@test.com"
        };

        // Act
        await service.UpdateConfigurationAsync(dto);

        // Assert
        var updatedConfig = await _context.ConfiguracionesEmail.FirstAsync();
        Assert.Equal("existing-password", updatedConfig.SmtpPassword);
    }

    #endregion

    #region EnsureConfigurationExistsAsync Tests

    [Fact]
    public async Task EnsureConfigurationExistsAsync_NoConfiguration_CreatesDefault()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.EnsureConfigurationExistsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("SMTP", result.Proveedor);
        Assert.False(result.Habilitado);
        Assert.Equal("localhost", result.SmtpHost);
    }

    [Fact]
    public async Task EnsureConfigurationExistsAsync_ConfigurationExists_ReturnsExisting()
    {
        // Arrange
        var existingConfig = new ConfiguracionEmail
        {
            Proveedor = "SendGrid",
            Habilitado = true,
            FromEmail = "existing@test.com",
            UsuarioId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };
        _context.ConfiguracionesEmail.Add(existingConfig);
        await _context.SaveChangesAsync();

        var service = CreateService();

        // Act
        var result = await service.EnsureConfigurationExistsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("SendGrid", result.Proveedor);
        Assert.Equal("existing@test.com", result.FromEmail);
    }

    #endregion

    #region TestConfigurationAsync Tests

    [Fact]
    public async Task TestConfigurationAsync_NoConfiguration_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.TestConfigurationAsync("test@recipient.com");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("No hay configuración", result.Error);
    }

    [Fact]
    public async Task TestConfigurationAsync_DisabledConfiguration_ReturnsFailure()
    {
        // Arrange
        var config = new ConfiguracionEmail
        {
            Proveedor = "SMTP",
            Habilitado = false,
            FromEmail = "test@test.com",
            UsuarioId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };
        _context.ConfiguracionesEmail.Add(config);
        await _context.SaveChangesAsync();

        var service = CreateService();

        // Act
        var result = await service.TestConfigurationAsync("test@recipient.com");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("deshabilitado", result.Error);
    }

    [Fact]
    public async Task TestConfigurationAsync_EmailSendSuccess_ReturnsSuccess()
    {
        // Arrange
        var config = new ConfiguracionEmail
        {
            Proveedor = "SMTP",
            Habilitado = true,
            SmtpHost = "smtp.test.com",
            FromEmail = "test@test.com",
            UsuarioId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };
        _context.ConfiguracionesEmail.Add(config);
        await _context.SaveChangesAsync();

        _emailServiceMock
            .Setup(x => x.SendAsync(It.IsAny<SendEmailRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendEmailResultDto { Success = true });

        var service = CreateService();

        // Act
        var result = await service.TestConfigurationAsync("test@recipient.com");

        // Assert
        Assert.True(result.Success);
        Assert.True(result.TestedAt != default);
    }

    [Fact]
    public async Task TestConfigurationAsync_EmailSendFailure_ReturnsFailure()
    {
        // Arrange
        var config = new ConfiguracionEmail
        {
            Proveedor = "SMTP",
            Habilitado = true,
            SmtpHost = "smtp.test.com",
            FromEmail = "test@test.com",
            UsuarioId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };
        _context.ConfiguracionesEmail.Add(config);
        await _context.SaveChangesAsync();

        _emailServiceMock
            .Setup(x => x.SendAsync(It.IsAny<SendEmailRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendEmailResultDto { Success = false, Error = "Connection failed" });

        var service = CreateService();

        // Act
        var result = await service.TestConfigurationAsync("test@recipient.com");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Connection failed", result.Error);
    }

    [Fact]
    public async Task TestConfigurationAsync_EmailServiceThrows_ReturnsFailure()
    {
        // Arrange
        var config = new ConfiguracionEmail
        {
            Proveedor = "SMTP",
            Habilitado = true,
            SmtpHost = "smtp.test.com",
            FromEmail = "test@test.com",
            UsuarioId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };
        _context.ConfiguracionesEmail.Add(config);
        await _context.SaveChangesAsync();

        _emailServiceMock
            .Setup(x => x.SendAsync(It.IsAny<SendEmailRequestDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("SMTP connection error"));

        var service = CreateService();

        // Act
        var result = await service.TestConfigurationAsync("test@recipient.com");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("SMTP connection error", result.Error);
    }

    [Fact]
    public async Task TestConfigurationAsync_UpdatesConfigWithTestResults()
    {
        // Arrange
        var config = new ConfiguracionEmail
        {
            Proveedor = "SMTP",
            Habilitado = true,
            SmtpHost = "smtp.test.com",
            FromEmail = "test@test.com",
            UsuarioId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };
        _context.ConfiguracionesEmail.Add(config);
        await _context.SaveChangesAsync();

        _emailServiceMock
            .Setup(x => x.SendAsync(It.IsAny<SendEmailRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendEmailResultDto { Success = true });

        var service = CreateService();

        // Act
        await service.TestConfigurationAsync("test@recipient.com");

        // Assert
        var updatedConfig = await _context.ConfiguracionesEmail.FirstAsync();
        Assert.NotNull(updatedConfig.LastTestedAt);
        Assert.True(updatedConfig.LastTestSuccess);
        Assert.Null(updatedConfig.LastTestError);
    }

    #endregion

    #region DTO Mapping Tests

    [Fact]
    public async Task GetConfigurationAsync_MapsAllPropertiesToDto()
    {
        // Arrange
        var config = new ConfiguracionEmail
        {
            Proveedor = "SMTP",
            Habilitado = true,
            SmtpHost = "smtp.test.com",
            SmtpPort = 587,
            SmtpUsername = "user",
            SmtpUseSsl = true,
            SendGridApiKey = "sg-key",
            MailgunApiKey = "mg-key",
            MailgunDomain = "mg.domain.com",
            MailgunRegion = "EU",
            FromEmail = "from@test.com",
            FromName = "Test Sender",
            LastTestedAt = DateTime.UtcNow,
            LastTestSuccess = true,
            UsuarioId = _testUserId,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };
        _context.ConfiguracionesEmail.Add(config);
        await _context.SaveChangesAsync();

        var service = CreateService();

        // Act
        var result = await service.GetConfigurationAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(config.Id, result.Id);
        Assert.Equal("SMTP", result.Proveedor);
        Assert.True(result.Habilitado);
        Assert.Equal("smtp.test.com", result.SmtpHost);
        Assert.Equal(587, result.SmtpPort);
        Assert.True(result.SmtpUseSsl);
        Assert.True(result.HasSendGridApiKey);
        Assert.True(result.HasMailgunApiKey);
        Assert.Equal("mg.domain.com", result.MailgunDomain);
        Assert.Equal("EU", result.MailgunRegion);
        Assert.Equal("from@test.com", result.FromEmail);
        Assert.Equal("Test Sender", result.FromName);
        Assert.True(result.LastTestedAt != default);
        Assert.True(result.LastTestSuccess);
        Assert.True(result.CreatedAt != default);
        Assert.True(result.UpdatedAt != default);
    }

    #endregion
}
