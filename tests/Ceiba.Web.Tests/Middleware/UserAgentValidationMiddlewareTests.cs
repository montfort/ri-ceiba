using Ceiba.Web.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace Ceiba.Web.Tests.Middleware;

/// <summary>
/// Unit tests for UserAgentValidationMiddleware.
/// RS-005 Mitigation: Tests session hijacking detection via User-Agent validation.
/// Phase 3: Coverage improvement tests.
/// </summary>
[Trait("Category", "Unit")]
public class UserAgentValidationMiddlewareTests
{
    private readonly Mock<ILogger<UserAgentValidationMiddleware>> _mockLogger;
    private const string UserAgentSessionKey = "Ceiba_UserAgent";

    public UserAgentValidationMiddlewareTests()
    {
        _mockLogger = new Mock<ILogger<UserAgentValidationMiddleware>>();
    }

    private UserAgentValidationMiddleware CreateMiddleware(RequestDelegate next)
    {
        return new UserAgentValidationMiddleware(next, _mockLogger.Object);
    }

    private static DefaultHttpContext CreateHttpContext(
        bool isAuthenticated = false,
        string? userName = null,
        string? userAgent = null)
    {
        var context = new DefaultHttpContext();

        // Setup session
        var mockSession = new MockSession();
        context.Features.Set<ISessionFeature>(new MockSessionFeature(mockSession));

        if (isAuthenticated)
        {
            var claims = new List<Claim>();
            if (!string.IsNullOrEmpty(userName))
            {
                claims.Add(new Claim(ClaimTypes.Name, userName));
            }
            var identity = new ClaimsIdentity(claims, "TestAuth");
            context.User = new ClaimsPrincipal(identity);
        }

        if (!string.IsNullOrEmpty(userAgent))
        {
            context.Request.Headers.UserAgent = userAgent;
        }

        return context;
    }

    #region Constructor Tests

    [Fact(DisplayName = "Constructor should not throw with valid parameters")]
    public void Constructor_ValidParameters_DoesNotThrow()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;

        // Act
        var middleware = CreateMiddleware(next);

        // Assert
        middleware.Should().NotBeNull();
    }

    #endregion

    #region Unauthenticated User Tests

    [Fact(DisplayName = "InvokeAsync should call next for unauthenticated users")]
    public async Task InvokeAsync_UnauthenticatedUser_CallsNext()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };
        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext(isAuthenticated: false);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact(DisplayName = "InvokeAsync should not store User-Agent for unauthenticated users")]
    public async Task InvokeAsync_UnauthenticatedUser_DoesNotStoreUserAgent()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext(isAuthenticated: false, userAgent: "TestBrowser/1.0");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Session.Keys.Should().NotContain(UserAgentSessionKey);
    }

    [Fact(DisplayName = "InvokeAsync should handle null User")]
    public async Task InvokeAsync_NullUser_CallsNext()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };
        var middleware = CreateMiddleware(next);
        var context = new DefaultHttpContext { User = null! };

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact(DisplayName = "InvokeAsync should handle null Identity")]
    public async Task InvokeAsync_NullIdentity_CallsNext()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };
        var middleware = CreateMiddleware(next);
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(); // No identity

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
    }

    #endregion

    #region First Request Tests (Store User-Agent)

    [Fact(DisplayName = "InvokeAsync should store User-Agent on first authenticated request")]
    public async Task InvokeAsync_FirstAuthenticatedRequest_StoresUserAgent()
    {
        // Arrange
        var userAgent = "Mozilla/5.0 TestBrowser";
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext(isAuthenticated: true, userName: "testuser", userAgent: userAgent);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Session.GetString(UserAgentSessionKey).Should().Be(userAgent);
    }

    [Fact(DisplayName = "InvokeAsync should call next after storing User-Agent")]
    public async Task InvokeAsync_FirstAuthenticatedRequest_CallsNext()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };
        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext(isAuthenticated: true, userName: "testuser", userAgent: "TestBrowser");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact(DisplayName = "InvokeAsync should store empty User-Agent if header is empty")]
    public async Task InvokeAsync_EmptyUserAgent_StoresEmptyString()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext(isAuthenticated: true, userName: "testuser", userAgent: "");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Session.GetString(UserAgentSessionKey).Should().Be("");
    }

    #endregion

    #region Subsequent Request Tests (Match User-Agent)

    [Fact(DisplayName = "InvokeAsync should allow subsequent request with same User-Agent")]
    public async Task InvokeAsync_SameUserAgent_CallsNext()
    {
        // Arrange
        var userAgent = "Mozilla/5.0 TestBrowser";
        var nextCallCount = 0;
        RequestDelegate next = _ =>
        {
            nextCallCount++;
            return Task.CompletedTask;
        };
        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext(isAuthenticated: true, userName: "testuser", userAgent: userAgent);

        // First request - stores User-Agent
        await middleware.InvokeAsync(context);

        // Second request - same User-Agent
        var context2 = CreateHttpContext(isAuthenticated: true, userName: "testuser", userAgent: userAgent);
        // Copy the session
        context2.Features.Set<ISessionFeature>(context.Features.Get<ISessionFeature>());

        // Act
        await middleware.InvokeAsync(context2);

        // Assert
        nextCallCount.Should().Be(2);
    }

    [Fact(DisplayName = "InvokeAsync should not log warning for matching User-Agent")]
    public async Task InvokeAsync_SameUserAgent_NoWarning()
    {
        // Arrange
        var userAgent = "Mozilla/5.0 TestBrowser";
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext(isAuthenticated: true, userName: "testuser", userAgent: userAgent);

        // First request
        await middleware.InvokeAsync(context);

        // Second request with same context/session
        context.Request.Headers.UserAgent = userAgent;

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    #endregion

    #region User-Agent Mismatch Tests (Session Hijacking Detection)

    [Fact(DisplayName = "InvokeAsync should detect User-Agent mismatch")]
    public async Task InvokeAsync_UserAgentMismatch_LogsWarning()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;

        // Setup auth service mock
        var servicesMock = new Mock<IServiceProvider>();
        var authServiceMock = new Mock<IAuthenticationService>();
        authServiceMock.Setup(x => x.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string?>(), It.IsAny<AuthenticationProperties?>()))
            .Returns(Task.CompletedTask);
        servicesMock.Setup(x => x.GetService(typeof(IAuthenticationService)))
            .Returns(authServiceMock.Object);

        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext(isAuthenticated: true, userName: "testuser", userAgent: "OriginalBrowser/1.0");
        context.RequestServices = servicesMock.Object;

        // First request - stores User-Agent
        await middleware.InvokeAsync(context);

        // Change User-Agent for second request
        context.Request.Headers.UserAgent = "HijackedBrowser/2.0";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("mismatch")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact(DisplayName = "InvokeAsync should not call next on User-Agent mismatch")]
    public async Task InvokeAsync_UserAgentMismatch_DoesNotCallNext()
    {
        // Arrange
        var nextCallCount = 0;
        RequestDelegate next = _ =>
        {
            nextCallCount++;
            return Task.CompletedTask;
        };

        // Use mock for services
        var servicesMock = new Mock<IServiceProvider>();
        var authServiceMock = new Mock<IAuthenticationService>();
        authServiceMock.Setup(x => x.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string?>(), It.IsAny<AuthenticationProperties?>()))
            .Returns(Task.CompletedTask);
        servicesMock.Setup(x => x.GetService(typeof(IAuthenticationService)))
            .Returns(authServiceMock.Object);

        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext(isAuthenticated: true, userName: "testuser", userAgent: "OriginalBrowser/1.0");
        context.RequestServices = servicesMock.Object;

        // First request - stores User-Agent
        await middleware.InvokeAsync(context);
        var firstCallCount = nextCallCount;

        // Change User-Agent
        context.Request.Headers.UserAgent = "HijackedBrowser/2.0";

        // Act
        await middleware.InvokeAsync(context);

        // Assert - next should have been called only on first request
        nextCallCount.Should().Be(firstCallCount);
    }

    [Fact(DisplayName = "InvokeAsync should redirect to login on User-Agent mismatch")]
    public async Task InvokeAsync_UserAgentMismatch_RedirectsToLogin()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;

        // Setup auth service mock
        var servicesMock = new Mock<IServiceProvider>();
        var authServiceMock = new Mock<IAuthenticationService>();
        authServiceMock.Setup(x => x.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string?>(), It.IsAny<AuthenticationProperties?>()))
            .Returns(Task.CompletedTask);
        servicesMock.Setup(x => x.GetService(typeof(IAuthenticationService)))
            .Returns(authServiceMock.Object);

        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext(isAuthenticated: true, userName: "testuser", userAgent: "OriginalBrowser/1.0");
        context.RequestServices = servicesMock.Object;

        // First request - stores User-Agent
        await middleware.InvokeAsync(context);

        // Change User-Agent
        context.Request.Headers.UserAgent = "HijackedBrowser/2.0";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.Location.ToString().Should().Contain("/Auth/Login");
    }

    [Fact(DisplayName = "InvokeAsync should sign out user on User-Agent mismatch")]
    public async Task InvokeAsync_UserAgentMismatch_SignsOut()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;

        // Setup auth service mock
        var servicesMock = new Mock<IServiceProvider>();
        var authServiceMock = new Mock<IAuthenticationService>();
        authServiceMock.Setup(x => x.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string?>(), It.IsAny<AuthenticationProperties?>()))
            .Returns(Task.CompletedTask);
        servicesMock.Setup(x => x.GetService(typeof(IAuthenticationService)))
            .Returns(authServiceMock.Object);

        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext(isAuthenticated: true, userName: "testuser", userAgent: "OriginalBrowser/1.0");
        context.RequestServices = servicesMock.Object;

        // First request - stores User-Agent
        await middleware.InvokeAsync(context);

        // Change User-Agent
        context.Request.Headers.UserAgent = "HijackedBrowser/2.0";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        authServiceMock.Verify(
            x => x.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string?>(), It.IsAny<AuthenticationProperties?>()),
            Times.Once);
    }

    #endregion

    #region Edge Cases

    [Fact(DisplayName = "InvokeAsync should handle various User-Agent formats")]
    public async Task InvokeAsync_VariousUserAgentFormats_StoresCorrectly()
    {
        // Arrange
        var userAgents = new[]
        {
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
            "Mozilla/5.0 (iPhone; CPU iPhone OS 14_0 like Mac OS X)",
            "curl/7.68.0",
            "PostmanRuntime/7.26.8",
            "CustomApp/1.0 (API Client)"
        };

        foreach (var ua in userAgents)
        {
            RequestDelegate next = _ => Task.CompletedTask;
            var middleware = CreateMiddleware(next);
            var context = CreateHttpContext(isAuthenticated: true, userName: "testuser", userAgent: ua);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.Session.GetString(UserAgentSessionKey).Should().Be(ua);
        }
    }

    [Fact(DisplayName = "InvokeAsync should handle long User-Agent strings")]
    public async Task InvokeAsync_LongUserAgent_HandlesCorrectly()
    {
        // Arrange
        var longUserAgent = new string('x', 1000);
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext(isAuthenticated: true, userName: "testuser", userAgent: longUserAgent);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Session.GetString(UserAgentSessionKey).Should().Be(longUserAgent);
    }

    [Fact(DisplayName = "InvokeAsync should handle special characters in User-Agent")]
    public async Task InvokeAsync_SpecialCharactersInUserAgent_HandlesCorrectly()
    {
        // Arrange
        var specialUserAgent = "Mozilla/5.0 <script>alert('xss')</script> (Test)";
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext(isAuthenticated: true, userName: "testuser", userAgent: specialUserAgent);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Session.GetString(UserAgentSessionKey).Should().Be(specialUserAgent);
    }

    [Fact(DisplayName = "InvokeAsync should be case-sensitive for User-Agent comparison")]
    public async Task InvokeAsync_CaseSensitiveComparison_DetectsMismatch()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;

        var servicesMock = new Mock<IServiceProvider>();
        var authServiceMock = new Mock<IAuthenticationService>();
        authServiceMock.Setup(x => x.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string?>(), It.IsAny<AuthenticationProperties?>()))
            .Returns(Task.CompletedTask);
        servicesMock.Setup(x => x.GetService(typeof(IAuthenticationService)))
            .Returns(authServiceMock.Object);

        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext(isAuthenticated: true, userName: "testuser", userAgent: "Mozilla/5.0");
        context.RequestServices = servicesMock.Object;

        // First request
        await middleware.InvokeAsync(context);

        // Change to different case
        context.Request.Headers.UserAgent = "mozilla/5.0";

        // Act
        await middleware.InvokeAsync(context);

        // Assert - Should detect as mismatch
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Helper Classes

    /// <summary>
    /// Mock session implementation for testing.
    /// </summary>
    private class MockSession : ISession
    {
        private readonly Dictionary<string, byte[]> _data = new();

        public string Id => Guid.NewGuid().ToString();
        public bool IsAvailable => true;
        public IEnumerable<string> Keys => _data.Keys;

        public void Clear() => _data.Clear();

        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Remove(string key) => _data.Remove(key);

        public void Set(string key, byte[] value) => _data[key] = value;

        public bool TryGetValue(string key, out byte[] value)
        {
            if (_data.TryGetValue(key, out var result))
            {
                value = result;
                return true;
            }
            value = Array.Empty<byte>();
            return false;
        }
    }

    /// <summary>
    /// Mock session feature for injecting into HttpContext.
    /// </summary>
    private class MockSessionFeature : ISessionFeature
    {
        public MockSessionFeature(ISession session)
        {
            Session = session;
        }

        public ISession Session { get; set; }
    }

    #endregion
}
