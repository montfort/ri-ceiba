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
/// Unit tests for AutomatedReportConfigController.
/// Tests automated report configuration CRUD operations.
/// </summary>
[Trait("Category", "Unit")]
public class AutomatedReportConfigControllerTests
{
    private readonly Mock<IAutomatedReportConfigService> _mockConfigService;
    private readonly Mock<ILogger<AutomatedReportConfigController>> _mockLogger;
    private readonly AutomatedReportConfigController _controller;
    private readonly Guid _testUserId = Guid.NewGuid();

    public AutomatedReportConfigControllerTests()
    {
        _mockConfigService = new Mock<IAutomatedReportConfigService>();
        _mockLogger = new Mock<ILogger<AutomatedReportConfigController>>();

        _controller = new AutomatedReportConfigController(
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
        var config = new AutomatedReportConfigDto
        {
            Id = 1,
            Habilitado = true,
            HoraGeneracion = TimeSpan.FromHours(6),
            Destinatarios = new[] { "test@example.com" }
        };

        _mockConfigService.Setup(x => x.GetConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        // Act
        var result = await _controller.GetConfiguration(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedConfig = okResult.Value.Should().BeOfType<AutomatedReportConfigDto>().Subject;
        returnedConfig.Habilitado.Should().BeTrue();
        returnedConfig.Destinatarios.Should().Contain("test@example.com");
    }

    [Fact(DisplayName = "GetConfiguration should create default when none exists")]
    public async Task GetConfiguration_NoConfig_CreatesDefault()
    {
        // Arrange
        var defaultConfig = new AutomatedReportConfigDto
        {
            Id = 1,
            Habilitado = false,
            HoraGeneracion = TimeSpan.FromHours(6)
        };

        _mockConfigService.Setup(x => x.GetConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((AutomatedReportConfigDto?)null);
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

    #endregion

    #region UpdateConfiguration Tests

    [Fact(DisplayName = "UpdateConfiguration should return OK on success")]
    public async Task UpdateConfiguration_ValidDto_ReturnsOk()
    {
        // Arrange
        var updateDto = new AutomatedReportConfigUpdateDto
        {
            Habilitado = true,
            HoraGeneracion = TimeSpan.FromHours(8),
            Destinatarios = new[] { "admin@example.com" },
            RutaSalida = "/reports"
        };

        var updatedConfig = new AutomatedReportConfigDto
        {
            Id = 1,
            Habilitado = true,
            HoraGeneracion = TimeSpan.FromHours(8),
            Destinatarios = new[] { "admin@example.com" }
        };

        _mockConfigService.Setup(x => x.UpdateConfigurationAsync(updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedConfig);

        // Act
        var result = await _controller.UpdateConfiguration(updateDto, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedConfig = okResult.Value.Should().BeOfType<AutomatedReportConfigDto>().Subject;
        returnedConfig.Habilitado.Should().BeTrue();
    }

    [Fact(DisplayName = "UpdateConfiguration should return BadRequest for invalid time")]
    public async Task UpdateConfiguration_InvalidTime_ReturnsBadRequest()
    {
        // Arrange
        var updateDto = new AutomatedReportConfigUpdateDto
        {
            Habilitado = true,
            HoraGeneracion = TimeSpan.FromDays(1), // Invalid - 24:00:00
            Destinatarios = new[] { "test@example.com" }
        };

        // Act
        var result = await _controller.UpdateConfiguration(updateDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "UpdateConfiguration should return BadRequest for negative time")]
    public async Task UpdateConfiguration_NegativeTime_ReturnsBadRequest()
    {
        // Arrange
        var updateDto = new AutomatedReportConfigUpdateDto
        {
            Habilitado = true,
            HoraGeneracion = TimeSpan.FromHours(-1), // Invalid - negative
            Destinatarios = new[] { "test@example.com" }
        };

        // Act
        var result = await _controller.UpdateConfiguration(updateDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "UpdateConfiguration should return BadRequest when enabled without recipients")]
    public async Task UpdateConfiguration_EnabledNoRecipients_ReturnsBadRequest()
    {
        // Arrange
        var updateDto = new AutomatedReportConfigUpdateDto
        {
            Habilitado = true,
            HoraGeneracion = TimeSpan.FromHours(6),
            Destinatarios = Array.Empty<string>() // No recipients when enabled
        };

        // Act
        var result = await _controller.UpdateConfiguration(updateDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "UpdateConfiguration should allow disabled without recipients")]
    public async Task UpdateConfiguration_DisabledNoRecipients_ReturnsOk()
    {
        // Arrange
        var updateDto = new AutomatedReportConfigUpdateDto
        {
            Habilitado = false,
            HoraGeneracion = TimeSpan.FromHours(6),
            Destinatarios = Array.Empty<string>()
        };

        var updatedConfig = new AutomatedReportConfigDto
        {
            Id = 1,
            Habilitado = false
        };

        _mockConfigService.Setup(x => x.UpdateConfigurationAsync(updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedConfig);

        // Act
        var result = await _controller.UpdateConfiguration(updateDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact(DisplayName = "UpdateConfiguration should return 401 on UnauthorizedAccessException")]
    public async Task UpdateConfiguration_Unauthorized_Returns401()
    {
        // Arrange
        var updateDto = new AutomatedReportConfigUpdateDto
        {
            Habilitado = false,
            HoraGeneracion = TimeSpan.FromHours(6),
            Destinatarios = Array.Empty<string>()
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
        var updateDto = new AutomatedReportConfigUpdateDto
        {
            Habilitado = false,
            HoraGeneracion = TimeSpan.FromHours(6),
            Destinatarios = Array.Empty<string>()
        };

        _mockConfigService.Setup(x => x.UpdateConfigurationAsync(updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.UpdateConfiguration(updateDto, CancellationToken.None);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region Authorization Tests

    [Fact(DisplayName = "Controller should have Authorize attribute with ADMIN role")]
    public void Controller_HasAuthorizeAttribute()
    {
        // Arrange & Act
        var attributes = typeof(AutomatedReportConfigController)
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
        var attributes = typeof(AutomatedReportConfigController)
            .GetCustomAttributes(typeof(ApiControllerAttribute), true);

        // Assert
        attributes.Should().HaveCount(1);
    }

    [Fact(DisplayName = "Controller should have correct route prefix")]
    public void Controller_HasCorrectRoutePrefix()
    {
        // Arrange & Act
        var attributes = typeof(AutomatedReportConfigController)
            .GetCustomAttributes(typeof(RouteAttribute), true);

        // Assert
        attributes.Should().HaveCount(1);
        var routeAttribute = (RouteAttribute)attributes[0];
        routeAttribute.Template.Should().Be("api/automated-report-config");
    }

    #endregion

    #region Logging Tests

    [Fact(DisplayName = "UpdateConfiguration should log on success")]
    public async Task UpdateConfiguration_Success_LogsInfo()
    {
        // Arrange
        var updateDto = new AutomatedReportConfigUpdateDto
        {
            Habilitado = true,
            HoraGeneracion = TimeSpan.FromHours(8),
            Destinatarios = new[] { "test@example.com" }
        };

        _mockConfigService.Setup(x => x.UpdateConfigurationAsync(updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AutomatedReportConfigDto());

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

    [Fact(DisplayName = "UpdateConfiguration should log warning on invalid time")]
    public async Task UpdateConfiguration_InvalidTime_LogsWarning()
    {
        // Arrange
        var updateDto = new AutomatedReportConfigUpdateDto
        {
            Habilitado = true,
            HoraGeneracion = TimeSpan.FromDays(2),
            Destinatarios = new[] { "test@example.com" }
        };

        // Act
        await _controller.UpdateConfiguration(updateDto, CancellationToken.None);

        // Assert
        _mockLogger.Verify(x => x.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Invalid time")),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact(DisplayName = "UpdateConfiguration should log warning when no recipients specified")]
    public async Task UpdateConfiguration_NoRecipients_LogsWarning()
    {
        // Arrange
        var updateDto = new AutomatedReportConfigUpdateDto
        {
            Habilitado = true,
            HoraGeneracion = TimeSpan.FromHours(6),
            Destinatarios = Array.Empty<string>()
        };

        // Act
        await _controller.UpdateConfiguration(updateDto, CancellationToken.None);

        // Assert
        _mockLogger.Verify(x => x.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No recipients")),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact(DisplayName = "GetConfiguration should log error on exception")]
    public async Task GetConfiguration_Error_LogsError()
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

    #region Validation Edge Cases

    [Fact(DisplayName = "UpdateConfiguration should accept time at midnight")]
    public async Task UpdateConfiguration_MidnightTime_IsValid()
    {
        // Arrange
        var updateDto = new AutomatedReportConfigUpdateDto
        {
            Habilitado = false,
            HoraGeneracion = TimeSpan.Zero, // 00:00:00
            Destinatarios = Array.Empty<string>()
        };

        _mockConfigService.Setup(x => x.UpdateConfigurationAsync(updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AutomatedReportConfigDto());

        // Act
        var result = await _controller.UpdateConfiguration(updateDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact(DisplayName = "UpdateConfiguration should accept time at 23:59:59")]
    public async Task UpdateConfiguration_EndOfDay_IsValid()
    {
        // Arrange
        var updateDto = new AutomatedReportConfigUpdateDto
        {
            Habilitado = false,
            HoraGeneracion = new TimeSpan(23, 59, 59),
            Destinatarios = Array.Empty<string>()
        };

        _mockConfigService.Setup(x => x.UpdateConfigurationAsync(updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AutomatedReportConfigDto());

        // Act
        var result = await _controller.UpdateConfiguration(updateDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact(DisplayName = "UpdateConfiguration with multiple recipients should work")]
    public async Task UpdateConfiguration_MultipleRecipients_Works()
    {
        // Arrange
        var updateDto = new AutomatedReportConfigUpdateDto
        {
            Habilitado = true,
            HoraGeneracion = TimeSpan.FromHours(6),
            Destinatarios = new[]
            {
                "admin1@example.com",
                "admin2@example.com",
                "supervisor@example.com"
            }
        };

        _mockConfigService.Setup(x => x.UpdateConfigurationAsync(updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AutomatedReportConfigDto { Destinatarios = updateDto.Destinatarios });

        // Act
        var result = await _controller.UpdateConfiguration(updateDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    #endregion
}
