using System.Security.Claims;
using Ceiba.Application.Services;
using Ceiba.Web.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Ceiba.Web.Tests.Controllers;

/// <summary>
/// Unit tests for AccountController.
/// Tests authentication endpoints including login, logout, and security features.
/// </summary>
public class AccountControllerTests
{
    private readonly Mock<SignInManager<IdentityUser<Guid>>> _signInManagerMock;
    private readonly Mock<ILoginSecurityService> _loginSecurityMock;
    private readonly Mock<ILogger<AccountController>> _loggerMock;
    private readonly AccountController _controller;

    public AccountControllerTests()
    {
        // Setup SignInManager mock (complex setup required)
        var userStoreMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var contextAccessorMock = new Mock<IHttpContextAccessor>();
        var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>();

        _signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(
            userManagerMock.Object,
            contextAccessorMock.Object,
            claimsFactoryMock.Object,
            null!, null!, null!, null!);

        _loginSecurityMock = new Mock<ILoginSecurityService>();
        _loggerMock = new Mock<ILogger<AccountController>>();

        _controller = new AccountController(
            _signInManagerMock.Object,
            _loginSecurityMock.Object,
            _loggerMock.Object);

        SetupHttpContext();
    }

    private void SetupHttpContext(string? ipAddress = "127.0.0.1", string? userName = null)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(ipAddress ?? "127.0.0.1");

        if (userName != null)
        {
            var claims = new List<Claim> { new(ClaimTypes.Name, userName) };
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        }

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    #region Login (JSON API) Tests

    [Fact]
    public async Task Login_InvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        _controller.ModelState.AddModelError("Email", "Required");
        var request = new AccountController.LoginRequest();

        // Act
        var result = await _controller.Login(request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task Login_IpBlocked_Returns429()
    {
        // Arrange
        _loginSecurityMock.Setup(x => x.IsIpBlockedAsync(It.IsAny<string>())).ReturnsAsync(true);
        _loginSecurityMock.Setup(x => x.GetRemainingLockoutSecondsAsync(It.IsAny<string>())).ReturnsAsync(300);

        var request = new AccountController.LoginRequest
        {
            Email = "test@test.com",
            Password = "Password123!"
        };

        // Act
        var result = await _controller.Login(request);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(429, statusResult.StatusCode);
    }

    [Fact]
    public async Task Login_Success_ReturnsOk()
    {
        // Arrange
        _loginSecurityMock.Setup(x => x.IsIpBlockedAsync(It.IsAny<string>())).ReturnsAsync(false);
        _loginSecurityMock.Setup(x => x.GetProgressiveDelayAsync(It.IsAny<string>())).ReturnsAsync(0);
        _signInManagerMock.Setup(x => x.PasswordSignInAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        var request = new AccountController.LoginRequest
        {
            Email = "test@test.com",
            Password = "Password123!"
        };

        // Act
        var result = await _controller.Login(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        _loginSecurityMock.Verify(x => x.RecordSuccessfulLoginAsync(It.IsAny<string>(), "test@test.com"), Times.Once);
    }

    [Fact]
    public async Task Login_Failure_ReturnsBadRequestAndRecordsAttempt()
    {
        // Arrange
        _loginSecurityMock.Setup(x => x.IsIpBlockedAsync(It.IsAny<string>())).ReturnsAsync(false);
        _loginSecurityMock.Setup(x => x.GetProgressiveDelayAsync(It.IsAny<string>())).ReturnsAsync(0);
        _loginSecurityMock.Setup(x => x.GetFailedAttemptCountAsync(It.IsAny<string>())).ReturnsAsync(1);
        _signInManagerMock.Setup(x => x.PasswordSignInAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        var request = new AccountController.LoginRequest
        {
            Email = "test@test.com",
            Password = "WrongPassword"
        };

        // Act
        var result = await _controller.Login(request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
        _loginSecurityMock.Verify(x => x.RecordFailedAttemptAsync(It.IsAny<string>(), "test@test.com"), Times.Once);
    }

    [Fact]
    public async Task Login_LockedOut_ReturnsBadRequestWithMessage()
    {
        // Arrange
        _loginSecurityMock.Setup(x => x.IsIpBlockedAsync(It.IsAny<string>())).ReturnsAsync(false);
        _loginSecurityMock.Setup(x => x.GetProgressiveDelayAsync(It.IsAny<string>())).ReturnsAsync(0);
        _signInManagerMock.Setup(x => x.PasswordSignInAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);

        var request = new AccountController.LoginRequest
        {
            Email = "test@test.com",
            Password = "Password123!"
        };

        // Act
        var result = await _controller.Login(request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task Login_RequiresTwoFactor_ReturnsBadRequest()
    {
        // Arrange
        _loginSecurityMock.Setup(x => x.IsIpBlockedAsync(It.IsAny<string>())).ReturnsAsync(false);
        _loginSecurityMock.Setup(x => x.GetProgressiveDelayAsync(It.IsAny<string>())).ReturnsAsync(0);
        _signInManagerMock.Setup(x => x.PasswordSignInAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.TwoFactorRequired);

        var request = new AccountController.LoginRequest
        {
            Email = "test@test.com",
            Password = "Password123!"
        };

        // Act
        var result = await _controller.Login(request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task Login_Exception_Returns500()
    {
        // Arrange
        _loginSecurityMock.Setup(x => x.IsIpBlockedAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Database error"));

        var request = new AccountController.LoginRequest
        {
            Email = "test@test.com",
            Password = "Password123!"
        };

        // Act
        var result = await _controller.Login(request);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task Login_WithProgressiveDelay_AppliesDelay()
    {
        // Arrange
        _loginSecurityMock.Setup(x => x.IsIpBlockedAsync(It.IsAny<string>())).ReturnsAsync(false);
        _loginSecurityMock.Setup(x => x.GetProgressiveDelayAsync(It.IsAny<string>())).ReturnsAsync(100); // 100ms delay
        _signInManagerMock.Setup(x => x.PasswordSignInAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        var request = new AccountController.LoginRequest
        {
            Email = "test@test.com",
            Password = "Password123!"
        };

        // Act
        var startTime = DateTime.UtcNow;
        await _controller.Login(request);
        var elapsed = DateTime.UtcNow - startTime;

        // Assert
        Assert.True(elapsed.TotalMilliseconds >= 50); // Allow some tolerance
    }

    #endregion

    #region Logout Tests

    [Fact]
    public async Task Logout_Success_ReturnsOk()
    {
        // Arrange
        _signInManagerMock.Setup(x => x.SignOutAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Logout();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        _signInManagerMock.Verify(x => x.SignOutAsync(), Times.Once);
    }

    [Fact]
    public async Task Logout_Exception_Returns500()
    {
        // Arrange
        _signInManagerMock.Setup(x => x.SignOutAsync()).ThrowsAsync(new Exception("SignOut error"));

        // Act
        var result = await _controller.Logout();

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    #endregion

    #region LogoutForm Tests

    [Fact]
    public async Task LogoutForm_Success_RedirectsToLogin()
    {
        // Arrange
        SetupHttpContext(userName: "testuser");
        _signInManagerMock.Setup(x => x.SignOutAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.LogoutForm();

        // Assert
        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Contains("logout=success", redirect.Url);
    }

    [Fact]
    public async Task LogoutForm_Exception_RedirectsWithError()
    {
        // Arrange
        _signInManagerMock.Setup(x => x.SignOutAsync()).ThrowsAsync(new Exception("Error"));

        // Act
        var result = await _controller.LogoutForm();

        // Assert
        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Contains("error=logout", redirect.Url);
    }

    #endregion

    #region PerformLogout Tests

    [Fact]
    public async Task PerformLogout_Success_RedirectsToLogin()
    {
        // Arrange
        SetupHttpContext(userName: "testuser");
        _signInManagerMock.Setup(x => x.SignOutAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.PerformLogout();

        // Assert
        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Contains("logout=success", redirect.Url);
    }

    [Fact]
    public async Task PerformLogout_Exception_RedirectsWithError()
    {
        // Arrange
        _signInManagerMock.Setup(x => x.SignOutAsync()).ThrowsAsync(new Exception("Error"));

        // Act
        var result = await _controller.PerformLogout();

        // Assert
        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Contains("error=logout", redirect.Url);
    }

    #endregion

    #region LoginForm Tests

    [Fact]
    public async Task LoginForm_Success_RedirectsToReturnUrl()
    {
        // Arrange
        _loginSecurityMock.Setup(x => x.IsIpBlockedAsync(It.IsAny<string>())).ReturnsAsync(false);
        _loginSecurityMock.Setup(x => x.GetProgressiveDelayAsync(It.IsAny<string>())).ReturnsAsync(0);
        _signInManagerMock.Setup(x => x.PasswordSignInAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        // Act
        var result = await _controller.LoginForm("test@test.com", "Password123!", false, "/reports");

        // Assert
        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/reports", redirect.Url);
    }

    [Fact]
    public async Task LoginForm_Success_NoReturnUrl_RedirectsToHome()
    {
        // Arrange
        _loginSecurityMock.Setup(x => x.IsIpBlockedAsync(It.IsAny<string>())).ReturnsAsync(false);
        _loginSecurityMock.Setup(x => x.GetProgressiveDelayAsync(It.IsAny<string>())).ReturnsAsync(0);
        _signInManagerMock.Setup(x => x.PasswordSignInAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        // Act
        var result = await _controller.LoginForm("test@test.com", "Password123!", false, null);

        // Assert
        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/", redirect.Url);
    }

    [Fact]
    public async Task LoginForm_IpBlocked_RedirectsWithError()
    {
        // Arrange
        _loginSecurityMock.Setup(x => x.IsIpBlockedAsync(It.IsAny<string>())).ReturnsAsync(true);
        _loginSecurityMock.Setup(x => x.GetRemainingLockoutSecondsAsync(It.IsAny<string>())).ReturnsAsync(300);

        // Act
        var result = await _controller.LoginForm("test@test.com", "Password123!", false, null);

        // Assert
        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Contains("error=blocked", redirect.Url);
        Assert.Contains("seconds=300", redirect.Url);
    }

    [Fact]
    public async Task LoginForm_InvalidCredentials_RedirectsWithError()
    {
        // Arrange
        _loginSecurityMock.Setup(x => x.IsIpBlockedAsync(It.IsAny<string>())).ReturnsAsync(false);
        _loginSecurityMock.Setup(x => x.GetProgressiveDelayAsync(It.IsAny<string>())).ReturnsAsync(0);
        _signInManagerMock.Setup(x => x.PasswordSignInAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        // Act
        var result = await _controller.LoginForm("test@test.com", "WrongPass", false, null);

        // Assert
        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Contains("error=invalid", redirect.Url);
    }

    [Fact]
    public async Task LoginForm_LockedOut_RedirectsWithError()
    {
        // Arrange
        _loginSecurityMock.Setup(x => x.IsIpBlockedAsync(It.IsAny<string>())).ReturnsAsync(false);
        _loginSecurityMock.Setup(x => x.GetProgressiveDelayAsync(It.IsAny<string>())).ReturnsAsync(0);
        _signInManagerMock.Setup(x => x.PasswordSignInAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);

        // Act
        var result = await _controller.LoginForm("test@test.com", "Password123!", false, null);

        // Assert
        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Contains("error=locked", redirect.Url);
    }

    [Fact]
    public async Task LoginForm_ServerError_RedirectsWithError()
    {
        // Arrange
        _loginSecurityMock.Setup(x => x.IsIpBlockedAsync(It.IsAny<string>())).ThrowsAsync(new Exception("DB error"));

        // Act
        var result = await _controller.LoginForm("test@test.com", "Password123!", false, null);

        // Assert
        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Contains("error=server", redirect.Url);
    }

    #endregion

    #region IP Address Detection Tests

    [Fact]
    public async Task Login_WithXForwardedFor_UsesForwardedIp()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
        httpContext.Request.Headers["X-Forwarded-For"] = "192.168.1.100, 10.0.0.1";
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        _loginSecurityMock.Setup(x => x.IsIpBlockedAsync("192.168.1.100")).ReturnsAsync(true);
        _loginSecurityMock.Setup(x => x.GetRemainingLockoutSecondsAsync(It.IsAny<string>())).ReturnsAsync(60);

        var request = new AccountController.LoginRequest
        {
            Email = "test@test.com",
            Password = "Password123!"
        };

        // Act
        var result = await _controller.Login(request);

        // Assert
        _loginSecurityMock.Verify(x => x.IsIpBlockedAsync("192.168.1.100"), Times.Once);
    }

    [Fact]
    public async Task Login_WithXRealIp_UsesRealIp()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
        httpContext.Request.Headers["X-Real-IP"] = "203.0.113.50";
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        _loginSecurityMock.Setup(x => x.IsIpBlockedAsync("203.0.113.50")).ReturnsAsync(true);
        _loginSecurityMock.Setup(x => x.GetRemainingLockoutSecondsAsync(It.IsAny<string>())).ReturnsAsync(60);

        var request = new AccountController.LoginRequest
        {
            Email = "test@test.com",
            Password = "Password123!"
        };

        // Act
        await _controller.Login(request);

        // Assert
        _loginSecurityMock.Verify(x => x.IsIpBlockedAsync("203.0.113.50"), Times.Once);
    }

    #endregion

    #region LoginRequest Validation Tests

    [Fact]
    public void LoginRequest_ValidData_PassesValidation()
    {
        // Arrange
        var request = new AccountController.LoginRequest
        {
            Email = "valid@email.com",
            Password = "Password123!",
            RememberMe = true
        };

        // Act
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
            request,
            new System.ComponentModel.DataAnnotations.ValidationContext(request),
            validationResults,
            true);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void LoginRequest_EmptyEmail_FailsValidation()
    {
        // Arrange
        var request = new AccountController.LoginRequest
        {
            Email = "",
            Password = "Password123!"
        };

        // Act
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
            request,
            new System.ComponentModel.DataAnnotations.ValidationContext(request),
            validationResults,
            true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains("Email"));
    }

    [Fact]
    public void LoginRequest_InvalidEmail_FailsValidation()
    {
        // Arrange
        var request = new AccountController.LoginRequest
        {
            Email = "not-an-email",
            Password = "Password123!"
        };

        // Act
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
            request,
            new System.ComponentModel.DataAnnotations.ValidationContext(request),
            validationResults,
            true);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void LoginRequest_ShortPassword_FailsValidation()
    {
        // Arrange
        var request = new AccountController.LoginRequest
        {
            Email = "valid@email.com",
            Password = "short"
        };

        // Act
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
            request,
            new System.ComponentModel.DataAnnotations.ValidationContext(request),
            validationResults,
            true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains("Password"));
    }

    #endregion
}
