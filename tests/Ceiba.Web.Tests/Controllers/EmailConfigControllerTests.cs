using System.Security.Claims;
using Ceiba.Core.Interfaces;
using Ceiba.Shared.DTOs;
using Ceiba.Web.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Ceiba.Web.Tests.Controllers;

/// <summary>
/// Unit tests for EmailConfigController.
/// Tests email configuration CRUD operations and test email functionality.
/// Phase 2: Medium priority tests for coverage improvement.
/// </summary>
[Trait("Category", "Unit")]
public class EmailConfigControllerTests
{
    private readonly Mock<IEmailConfigService> _mockConfigService;
    private readonly Mock<ILogger<EmailConfigController>> _mockLogger;
    private readonly EmailConfigController _controller;
    private readonly Guid _testUserId = Guid.NewGuid();

    public EmailConfigControllerTests()
    {
        _mockConfigService = new Mock<IEmailConfigService>();
        _mockLogger = new Mock<ILogger<EmailConfigController>>();

        _controller = new EmailConfigController(
            _mockConfigService.Object,
            _mockLogger.Object);

        SetupAuthenticatedUser(_testUserId, "ADMIN");
    }

    private void SetupAuthenticatedUser(Guid userId, string role)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    #region GetConfiguration Tests

    [Fact(DisplayName = "GetConfiguration should return OK with existing configuration")]
    public async Task GetConfiguration_ConfigExists_ReturnsOkWithConfig()
    {
        // Arrange
        var config = new EmailConfigDto
        {
            Id = 1,
            Proveedor = "SMTP",
            Habilitado = true,
            SmtpHost = "smtp.example.com",
            SmtpPort = 587,
            FromEmail = "noreply@example.com",
            FromName = "Test System"
        };

        _mockConfigService.Setup(x => x.GetConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        // Act
        var result = await _controller.GetConfiguration(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedConfig = okResult.Value.Should().BeOfType<EmailConfigDto>().Subject;
        returnedConfig.Habilitado.Should().BeTrue();
        returnedConfig.SmtpHost.Should().Be("smtp.example.com");
    }

    [Fact(DisplayName = "GetConfiguration should create default when none exists")]
    public async Task GetConfiguration_NoConfig_CreatesDefault()
    {
        // Arrange
        var defaultConfig = new EmailConfigDto
        {
            Id = 1,
            Proveedor = "SMTP",
            Habilitado = false
        };

        _mockConfigService.Setup(x => x.GetConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailConfigDto?)null);
        _mockConfigService.Setup(x => x.EnsureConfigurationExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultConfig);

        // Act
        var result = await _controller.GetConfiguration(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        _mockConfigService.Verify(x => x.EnsureConfigurationExistsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "GetConfiguration should return 500 on exception")]
    public async Task GetConfiguration_Exception_Returns500()
    {
        // Arrange
        _mockConfigService.Setup(x => x.GetConfigurationAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetConfiguration(CancellationToken.None);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    [Fact(DisplayName = "GetConfiguration should log error on exception")]
    public async Task GetConfiguration_Exception_LogsError()
    {
        // Arrange
        _mockConfigService.Setup(x => x.GetConfigurationAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test error"));

        // Act
        await _controller.GetConfiguration(CancellationToken.None);

        // Assert
        _mockLogger.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => true),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    #endregion

    #region UpdateConfiguration Tests

    [Fact(DisplayName = "UpdateConfiguration should return OK on success")]
    public async Task UpdateConfiguration_ValidDto_ReturnsOk()
    {
        // Arrange
        var updateDto = new EmailConfigUpdateDto
        {
            Proveedor = "SMTP",
            Habilitado = true,
            SmtpHost = "smtp.newhost.com",
            SmtpPort = 587,
            FromEmail = "test@example.com",
            FromName = "Test Sender"
        };

        var updatedConfig = new EmailConfigDto
        {
            Id = 1,
            Proveedor = "SMTP",
            Habilitado = true,
            SmtpHost = "smtp.newhost.com",
            SmtpPort = 587
        };

        _mockConfigService.Setup(x => x.UpdateConfigurationAsync(updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedConfig);

        // Act
        var result = await _controller.UpdateConfiguration(updateDto, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedConfig = okResult.Value.Should().BeOfType<EmailConfigDto>().Subject;
        returnedConfig.Habilitado.Should().BeTrue();
    }

    [Fact(DisplayName = "UpdateConfiguration should return BadRequest when enabled without from email")]
    public async Task UpdateConfiguration_EnabledNoFromEmail_ReturnsBadRequest()
    {
        // Arrange
        var updateDto = new EmailConfigUpdateDto
        {
            Proveedor = "SMTP",
            Habilitado = true,
            SmtpHost = "smtp.example.com",
            SmtpPort = 587,
            FromEmail = "", // Missing
            FromName = "Test"
        };

        // Act
        var result = await _controller.UpdateConfiguration(updateDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "UpdateConfiguration should return BadRequest when enabled without from name")]
    public async Task UpdateConfiguration_EnabledNoFromName_ReturnsBadRequest()
    {
        // Arrange
        var updateDto = new EmailConfigUpdateDto
        {
            Proveedor = "SMTP",
            Habilitado = true,
            SmtpHost = "smtp.example.com",
            SmtpPort = 587,
            FromEmail = "test@example.com",
            FromName = "" // Missing
        };

        // Act
        var result = await _controller.UpdateConfiguration(updateDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "UpdateConfiguration with SMTP should require host and port")]
    public async Task UpdateConfiguration_SmtpMissingHost_ReturnsBadRequest()
    {
        // Arrange
        var updateDto = new EmailConfigUpdateDto
        {
            Proveedor = "SMTP",
            Habilitado = true,
            SmtpHost = "", // Missing
            SmtpPort = 587,
            FromEmail = "test@example.com",
            FromName = "Test"
        };

        // Act
        var result = await _controller.UpdateConfiguration(updateDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "UpdateConfiguration with SMTP should require port")]
    public async Task UpdateConfiguration_SmtpMissingPort_ReturnsBadRequest()
    {
        // Arrange
        var updateDto = new EmailConfigUpdateDto
        {
            Proveedor = "SMTP",
            Habilitado = true,
            SmtpHost = "smtp.example.com",
            SmtpPort = null, // Missing
            FromEmail = "test@example.com",
            FromName = "Test"
        };

        // Act
        var result = await _controller.UpdateConfiguration(updateDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "UpdateConfiguration with SendGrid should require API key for new config")]
    public async Task UpdateConfiguration_SendGridMissingApiKey_ReturnsBadRequest()
    {
        // Arrange
        var updateDto = new EmailConfigUpdateDto
        {
            Proveedor = "SendGrid",
            Habilitado = true,
            SendGridApiKey = "", // Missing
            FromEmail = "test@example.com",
            FromName = "Test"
        };

        _mockConfigService.Setup(x => x.GetConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailConfigDto?)null);

        // Act
        var result = await _controller.UpdateConfiguration(updateDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "UpdateConfiguration with SendGrid should allow update without API key when existing")]
    public async Task UpdateConfiguration_SendGridExistingConfig_AllowsUpdateWithoutApiKey()
    {
        // Arrange
        var existingConfig = new EmailConfigDto
        {
            Id = 1,
            Proveedor = "SendGrid",
            HasSendGridApiKey = true
        };

        var updateDto = new EmailConfigUpdateDto
        {
            Proveedor = "SendGrid",
            Habilitado = true,
            SendGridApiKey = null, // Not provided, but existing config has one
            FromEmail = "test@example.com",
            FromName = "Test"
        };

        _mockConfigService.Setup(x => x.GetConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingConfig);
        _mockConfigService.Setup(x => x.UpdateConfigurationAsync(updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailConfigDto { Id = 1, Habilitado = true });

        // Act
        var result = await _controller.UpdateConfiguration(updateDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact(DisplayName = "UpdateConfiguration should allow disabled without validation")]
    public async Task UpdateConfiguration_Disabled_SkipsValidation()
    {
        // Arrange
        var updateDto = new EmailConfigUpdateDto
        {
            Proveedor = "SMTP",
            Habilitado = false,
            SmtpHost = "", // Would fail if enabled
            SmtpPort = null,
            FromEmail = "",
            FromName = ""
        };

        _mockConfigService.Setup(x => x.UpdateConfigurationAsync(updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailConfigDto { Id = 1, Habilitado = false });

        // Act
        var result = await _controller.UpdateConfiguration(updateDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact(DisplayName = "UpdateConfiguration should return 401 on UnauthorizedAccessException")]
    public async Task UpdateConfiguration_Unauthorized_Returns401()
    {
        // Arrange
        var updateDto = new EmailConfigUpdateDto
        {
            Proveedor = "SMTP",
            Habilitado = false
        };

        _mockConfigService.Setup(x => x.UpdateConfigurationAsync(updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Not authorized"));

        // Act
        var result = await _controller.UpdateConfiguration(updateDto, CancellationToken.None);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(401);
    }

    [Fact(DisplayName = "UpdateConfiguration should return 500 on exception")]
    public async Task UpdateConfiguration_Exception_Returns500()
    {
        // Arrange
        var updateDto = new EmailConfigUpdateDto
        {
            Proveedor = "SMTP",
            Habilitado = false
        };

        _mockConfigService.Setup(x => x.UpdateConfigurationAsync(updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.UpdateConfiguration(updateDto, CancellationToken.None);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    [Fact(DisplayName = "UpdateConfiguration should log success")]
    public async Task UpdateConfiguration_Success_LogsInfo()
    {
        // Arrange
        var updateDto = new EmailConfigUpdateDto
        {
            Proveedor = "SMTP",
            Habilitado = false
        };

        _mockConfigService.Setup(x => x.UpdateConfigurationAsync(updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailConfigDto { Id = 1 });

        // Act
        await _controller.UpdateConfiguration(updateDto, CancellationToken.None);

        // Assert
        _mockLogger.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("updated successfully")),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    #endregion

    #region TestConfiguration Tests

    [Fact(DisplayName = "TestConfiguration should return OK with test result")]
    public async Task TestConfiguration_ValidRecipient_ReturnsOkWithResult()
    {
        // Arrange
        var testDto = new TestEmailConfigDto { TestRecipient = "test@example.com" };
        var testResult = new EmailConfigTestResultDto
        {
            Success = true,
            TestedAt = DateTime.UtcNow
        };

        _mockConfigService.Setup(x => x.TestConfigurationAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(testResult);

        // Act
        var result = await _controller.TestConfiguration(testDto, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResult = okResult.Value.Should().BeOfType<EmailConfigTestResultDto>().Subject;
        returnedResult.Success.Should().BeTrue();
    }

    [Fact(DisplayName = "TestConfiguration should return BadRequest for empty recipient")]
    public async Task TestConfiguration_EmptyRecipient_ReturnsBadRequest()
    {
        // Arrange
        var testDto = new TestEmailConfigDto { TestRecipient = "" };

        // Act
        var result = await _controller.TestConfiguration(testDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "TestConfiguration should return BadRequest for whitespace recipient")]
    public async Task TestConfiguration_WhitespaceRecipient_ReturnsBadRequest()
    {
        // Arrange
        var testDto = new TestEmailConfigDto { TestRecipient = "   " };

        // Act
        var result = await _controller.TestConfiguration(testDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "TestConfiguration should return 500 on exception")]
    public async Task TestConfiguration_Exception_Returns500()
    {
        // Arrange
        var testDto = new TestEmailConfigDto { TestRecipient = "test@example.com" };

        _mockConfigService.Setup(x => x.TestConfigurationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("SMTP error"));

        // Act
        var result = await _controller.TestConfiguration(testDto, CancellationToken.None);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    [Fact(DisplayName = "TestConfiguration should log test attempt")]
    public async Task TestConfiguration_ValidRecipient_LogsInfo()
    {
        // Arrange
        var testDto = new TestEmailConfigDto { TestRecipient = "test@example.com" };

        _mockConfigService.Setup(x => x.TestConfigurationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailConfigTestResultDto { Success = true });

        // Act
        await _controller.TestConfiguration(testDto, CancellationToken.None);

        // Assert
        _mockLogger.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Testing email configuration")),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact(DisplayName = "TestConfiguration should return failed result when service fails")]
    public async Task TestConfiguration_ServiceFails_ReturnsFailedResult()
    {
        // Arrange
        var testDto = new TestEmailConfigDto { TestRecipient = "test@example.com" };
        var testResult = new EmailConfigTestResultDto
        {
            Success = false,
            Error = "Connection refused",
            TestedAt = DateTime.UtcNow
        };

        _mockConfigService.Setup(x => x.TestConfigurationAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(testResult);

        // Act
        var result = await _controller.TestConfiguration(testDto, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResult = okResult.Value.Should().BeOfType<EmailConfigTestResultDto>().Subject;
        returnedResult.Success.Should().BeFalse();
        returnedResult.Error.Should().Contain("Connection refused");
    }

    #endregion

    #region Authorization Tests

    [Fact(DisplayName = "Controller should have Authorize attribute with ADMIN role")]
    public void Controller_HasAuthorizeAttribute()
    {
        // Arrange & Act
        var attributes = typeof(EmailConfigController)
            .GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true);

        // Assert
        attributes.Should().HaveCount(1);
        var authorizeAttribute = (Microsoft.AspNetCore.Authorization.AuthorizeAttribute)attributes[0];
        authorizeAttribute.Roles.Should().Contain("ADMIN");
    }

    [Fact(DisplayName = "Controller should have ApiController attribute")]
    public void Controller_HasApiControllerAttribute()
    {
        // Arrange & Act
        var attributes = typeof(EmailConfigController)
            .GetCustomAttributes(typeof(ApiControllerAttribute), true);

        // Assert
        attributes.Should().HaveCount(1);
    }

    [Fact(DisplayName = "Controller should have correct route prefix")]
    public void Controller_HasCorrectRoutePrefix()
    {
        // Arrange & Act
        var attributes = typeof(EmailConfigController)
            .GetCustomAttributes(typeof(RouteAttribute), true);

        // Assert
        attributes.Should().HaveCount(1);
        var routeAttribute = (RouteAttribute)attributes[0];
        routeAttribute.Template.Should().Be("api/email-config");
    }

    [Fact(DisplayName = "Controller should have AutoValidateAntiforgeryToken attribute")]
    public void Controller_HasAutoValidateAntiforgeryTokenAttribute()
    {
        // Arrange & Act
        var attributes = typeof(EmailConfigController)
            .GetCustomAttributes(typeof(AutoValidateAntiforgeryTokenAttribute), true);

        // Assert
        attributes.Should().HaveCount(1);
    }

    #endregion

    #region Edge Cases

    [Fact(DisplayName = "UpdateConfiguration should handle unknown provider gracefully")]
    public async Task UpdateConfiguration_UnknownProvider_PassesValidation()
    {
        // Arrange
        var updateDto = new EmailConfigUpdateDto
        {
            Proveedor = "UnknownProvider",
            Habilitado = true,
            FromEmail = "test@example.com",
            FromName = "Test"
        };

        _mockConfigService.Setup(x => x.UpdateConfigurationAsync(updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailConfigDto { Id = 1 });

        // Act
        var result = await _controller.UpdateConfiguration(updateDto, CancellationToken.None);

        // Assert - Unknown providers pass validation (no specific requirements)
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact(DisplayName = "GetConfiguration should return config with test status")]
    public async Task GetConfiguration_WithTestStatus_ReturnsFullConfig()
    {
        // Arrange
        var config = new EmailConfigDto
        {
            Id = 1,
            Habilitado = true,
            LastTestedAt = DateTime.UtcNow.AddHours(-1),
            LastTestSuccess = true,
            LastTestError = null
        };

        _mockConfigService.Setup(x => x.GetConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        // Act
        var result = await _controller.GetConfiguration(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedConfig = okResult.Value.Should().BeOfType<EmailConfigDto>().Subject;
        returnedConfig.LastTestedAt.Should().NotBeNull();
        returnedConfig.LastTestSuccess.Should().BeTrue();
    }

    #endregion
}
