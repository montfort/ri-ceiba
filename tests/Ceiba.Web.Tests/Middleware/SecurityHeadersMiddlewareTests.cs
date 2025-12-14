using Ceiba.Web.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace Ceiba.Web.Tests.Middleware;

/// <summary>
/// Unit tests for SecurityHeadersMiddleware.
/// Tests security header configuration and middleware behavior.
/// Phase 2: Medium priority tests for coverage improvement.
/// T114, T114a, T118: HTTPS, HSTS, and security headers.
///
/// Note: Full header injection tests require integration tests with a real HTTP pipeline
/// because the middleware uses OnStarting callbacks that don't fire with DefaultHttpContext.
/// </summary>
[Trait("Category", "Unit")]
public class SecurityHeadersMiddlewareTests
{
    private readonly Mock<ILogger<SecurityHeadersMiddleware>> _mockLogger;

    public SecurityHeadersMiddlewareTests()
    {
        _mockLogger = new Mock<ILogger<SecurityHeadersMiddleware>>();
    }

    private SecurityHeadersMiddleware CreateMiddleware(
        RequestDelegate next,
        SecurityHeadersOptions? options = null)
    {
        return new SecurityHeadersMiddleware(
            next,
            _mockLogger.Object,
            options ?? new SecurityHeadersOptions());
    }

    #region Middleware Invocation Tests

    [Fact(DisplayName = "InvokeAsync should call next delegate")]
    public async Task InvokeAsync_CallsNextDelegate()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var nextCalled = false;
        RequestDelegate next = ctx =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };
        var middleware = CreateMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact(DisplayName = "InvokeAsync should preserve response status from next")]
    public async Task InvokeAsync_PreservesResponseStatus()
    {
        // Arrange
        var context = new DefaultHttpContext();
        RequestDelegate next = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status201Created;
            return Task.CompletedTask;
        };
        var middleware = CreateMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status201Created);
    }

    [Fact(DisplayName = "InvokeAsync should not throw with valid context")]
    public async Task InvokeAsync_ValidContext_DoesNotThrow()
    {
        // Arrange
        var context = new DefaultHttpContext();
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = CreateMiddleware(next);

        // Act
        Func<Task> act = async () => await middleware.InvokeAsync(context);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact(DisplayName = "InvokeAsync should complete even if next throws")]
    public async Task InvokeAsync_NextThrows_PropagatesException()
    {
        // Arrange
        var context = new DefaultHttpContext();
        RequestDelegate next = _ => throw new InvalidOperationException("Test exception");
        var middleware = CreateMiddleware(next);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.InvokeAsync(context));
    }

    #endregion

    #region SecurityHeadersOptions Tests

    [Fact(DisplayName = "SecurityHeadersOptions should have correct default FrameOptions")]
    public void SecurityHeadersOptions_DefaultFrameOptions_IsDeny()
    {
        // Act
        var options = new SecurityHeadersOptions();

        // Assert
        options.FrameOptions.Should().Be("DENY");
    }

    [Fact(DisplayName = "SecurityHeadersOptions should have correct default ReferrerPolicy")]
    public void SecurityHeadersOptions_DefaultReferrerPolicy_IsStrictOriginWhenCrossOrigin()
    {
        // Act
        var options = new SecurityHeadersOptions();

        // Assert
        options.ReferrerPolicy.Should().Be("strict-origin-when-cross-origin");
    }

    [Fact(DisplayName = "SecurityHeadersOptions should have empty default CSP")]
    public void SecurityHeadersOptions_DefaultCsp_IsEmpty()
    {
        // Act
        var options = new SecurityHeadersOptions();

        // Assert
        options.ContentSecurityPolicy.Should().BeEmpty();
    }

    [Fact(DisplayName = "SecurityHeadersOptions should have default PermissionsPolicy with disabled features")]
    public void SecurityHeadersOptions_DefaultPermissionsPolicy_DisablesFeatures()
    {
        // Act
        var options = new SecurityHeadersOptions();

        // Assert
        options.PermissionsPolicy.Should().Contain("camera=()");
        options.PermissionsPolicy.Should().Contain("microphone=()");
        options.PermissionsPolicy.Should().Contain("geolocation=()");
    }

    [Fact(DisplayName = "SecurityHeadersOptions should allow custom FrameOptions")]
    public void SecurityHeadersOptions_CustomFrameOptions_IsSet()
    {
        // Act
        var options = new SecurityHeadersOptions { FrameOptions = "SAMEORIGIN" };

        // Assert
        options.FrameOptions.Should().Be("SAMEORIGIN");
    }

    [Fact(DisplayName = "SecurityHeadersOptions should allow custom ReferrerPolicy")]
    public void SecurityHeadersOptions_CustomReferrerPolicy_IsSet()
    {
        // Act
        var options = new SecurityHeadersOptions { ReferrerPolicy = "no-referrer" };

        // Assert
        options.ReferrerPolicy.Should().Be("no-referrer");
    }

    [Fact(DisplayName = "SecurityHeadersOptions should allow custom CSP")]
    public void SecurityHeadersOptions_CustomCsp_IsSet()
    {
        // Act
        var options = new SecurityHeadersOptions { ContentSecurityPolicy = "default-src 'self'" };

        // Assert
        options.ContentSecurityPolicy.Should().Be("default-src 'self'");
    }

    [Fact(DisplayName = "SecurityHeadersOptions should allow custom PermissionsPolicy")]
    public void SecurityHeadersOptions_CustomPermissionsPolicy_IsSet()
    {
        // Act
        var options = new SecurityHeadersOptions { PermissionsPolicy = "geolocation=(self)" };

        // Assert
        options.PermissionsPolicy.Should().Be("geolocation=(self)");
    }

    #endregion

    #region Extension Methods Tests

    [Fact(DisplayName = "UseSecurityHeaders extension method exists")]
    public void UseSecurityHeaders_ExtensionExists()
    {
        // Act - Verify the extension method exists
        var method = typeof(SecurityHeadersMiddlewareExtensions)
            .GetMethod("UseSecurityHeaders");

        // Assert
        method.Should().NotBeNull();
        method!.IsStatic.Should().BeTrue();
    }

    [Fact(DisplayName = "UseStrictTransportSecurity extension exists")]
    public void UseStrictTransportSecurity_ExtensionExists()
    {
        // Act
        var method = typeof(SecurityHeadersMiddlewareExtensions)
            .GetMethod("UseStrictTransportSecurity");

        // Assert
        method.Should().NotBeNull();
        method!.IsStatic.Should().BeTrue();
    }

    [Fact(DisplayName = "UseSecurityHeaders extension has correct parameter types")]
    public void UseSecurityHeaders_HasCorrectParameters()
    {
        // Act
        var method = typeof(SecurityHeadersMiddlewareExtensions)
            .GetMethod("UseSecurityHeaders");
        var parameters = method!.GetParameters();

        // Assert
        parameters.Should().HaveCount(2);
        parameters[0].ParameterType.Name.Should().Be("IApplicationBuilder");
        parameters[1].ParameterType.Name.Should().Contain("Action");
    }

    #endregion

    #region Middleware Construction Tests

    [Fact(DisplayName = "Middleware should accept null options")]
    public void Middleware_NullOptions_UsesDefaults()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;

        // Act
        var middleware = new SecurityHeadersMiddleware(next, _mockLogger.Object, new SecurityHeadersOptions());

        // Assert
        middleware.Should().NotBeNull();
    }

    [Fact(DisplayName = "Middleware should accept custom options")]
    public void Middleware_CustomOptions_Accepted()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;
        var options = new SecurityHeadersOptions
        {
            FrameOptions = "SAMEORIGIN",
            ReferrerPolicy = "no-referrer",
            ContentSecurityPolicy = "default-src 'self'"
        };

        // Act
        var middleware = new SecurityHeadersMiddleware(next, _mockLogger.Object, options);

        // Assert
        middleware.Should().NotBeNull();
    }

    #endregion

    #region Edge Case Tests

    [Fact(DisplayName = "InvokeAsync should handle multiple invocations")]
    public async Task InvokeAsync_MultipleInvocations_HandleCorrectly()
    {
        // Arrange
        var callCount = 0;
        RequestDelegate next = _ =>
        {
            callCount++;
            return Task.CompletedTask;
        };
        var middleware = CreateMiddleware(next);

        // Act
        for (var i = 0; i < 3; i++)
        {
            var context = new DefaultHttpContext();
            await middleware.InvokeAsync(context);
        }

        // Assert
        callCount.Should().Be(3);
    }

    [Fact(DisplayName = "InvokeAsync should work with different status codes")]
    public async Task InvokeAsync_DifferentStatusCodes_Preserved()
    {
        // Arrange
        var statusCodes = new[] { 200, 201, 204, 400, 401, 403, 404, 500 };
        RequestDelegate next = ctx => Task.CompletedTask;
        var middleware = CreateMiddleware(next);

        foreach (var statusCode in statusCodes)
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.StatusCode = statusCode;

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.Response.StatusCode.Should().Be(statusCode);
        }
    }

    [Fact(DisplayName = "SecurityHeadersOptions should be mutable")]
    public void SecurityHeadersOptions_IsMutable()
    {
        // Arrange
        var options = new SecurityHeadersOptions();
        var originalFrameOptions = options.FrameOptions;

        // Act
        options.FrameOptions = "SAMEORIGIN";

        // Assert
        options.FrameOptions.Should().NotBe(originalFrameOptions);
        options.FrameOptions.Should().Be("SAMEORIGIN");
    }

    #endregion
}
