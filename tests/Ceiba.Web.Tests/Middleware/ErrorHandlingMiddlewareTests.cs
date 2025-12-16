using System.Net;
using System.Text.Json;
using Ceiba.Web.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace Ceiba.Web.Tests.Middleware;

/// <summary>
/// Unit tests for ErrorHandlingMiddleware.
/// Tests global exception handling, error responses, and environment-specific behavior.
/// </summary>
public class ErrorHandlingMiddlewareTests
{
    private readonly Mock<ILogger<ErrorHandlingMiddleware>> _mockLogger;
    private readonly Mock<IHostEnvironment> _mockEnvironment;

    public ErrorHandlingMiddlewareTests()
    {
        _mockLogger = new Mock<ILogger<ErrorHandlingMiddleware>>();
        _mockEnvironment = new Mock<IHostEnvironment>();
    }

    #region Successful Request Tests

    [Fact(DisplayName = "InvokeAsync should pass through when no exception occurs")]
    public async Task InvokeAsync_NoException_PassesThrough()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var responseCompleted = false;

        RequestDelegate next = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            responseCompleted = true;
            return Task.CompletedTask;
        };

        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var middleware = new ErrorHandlingMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        responseCompleted.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact(DisplayName = "InvokeAsync should not modify response when request succeeds")]
    public async Task InvokeAsync_Success_DoesNotModifyResponse()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        RequestDelegate next = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status201Created;
            return Task.CompletedTask;
        };

        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var middleware = new ErrorHandlingMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status201Created);
    }

    #endregion

    #region Exception Handling Tests

    [Fact(DisplayName = "InvokeAsync should handle UnauthorizedAccessException with 403 status")]
    public async Task InvokeAsync_UnauthorizedAccessException_Returns403()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = "/api/reports";

        RequestDelegate next = _ => throw new UnauthorizedAccessException("Access denied");

        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var middleware = new ErrorHandlingMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.Forbidden);
        var response = await ReadResponseBody(context);
        response.Should().Contain("No tiene permisos");
    }

    [Fact(DisplayName = "InvokeAsync should handle ArgumentException with 400 status")]
    public async Task InvokeAsync_ArgumentException_Returns400()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = "/api/reports";

        RequestDelegate next = _ => throw new ArgumentException("Invalid parameter value");

        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var middleware = new ErrorHandlingMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        var response = await ReadResponseBody(context);
        response.Should().Contain("Invalid parameter value");
    }

    [Fact(DisplayName = "InvokeAsync should handle KeyNotFoundException with 404 status")]
    public async Task InvokeAsync_KeyNotFoundException_Returns404()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = "/api/reports/999";

        RequestDelegate next = _ => throw new KeyNotFoundException("Report not found");

        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var middleware = new ErrorHandlingMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
        var response = await ReadResponseBody(context);
        response.Should().Contain("Recurso no encontrado");
    }

    [Fact(DisplayName = "InvokeAsync should handle generic Exception with 500 status")]
    public async Task InvokeAsync_GenericException_Returns500()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = "/api/reports";

        RequestDelegate next = _ => throw new Exception("Database connection failed");

        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var middleware = new ErrorHandlingMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        var response = await ReadResponseBody(context);
        response.Should().Contain("Ha ocurrido un error interno");
    }

    [Fact(DisplayName = "InvokeAsync should handle NullReferenceException with 500 status")]
    public async Task InvokeAsync_NullReferenceException_Returns500()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = "/api/reports";

        RequestDelegate next = _ => throw new NullReferenceException("Object reference not set");

        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var middleware = new ErrorHandlingMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
    }

    #endregion

    #region Response Format Tests

    [Fact(DisplayName = "InvokeAsync should set JSON content type on error")]
    public async Task InvokeAsync_OnError_SetsJsonContentType()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        RequestDelegate next = _ => throw new Exception("Test error");

        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var middleware = new ErrorHandlingMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.ContentType.Should().Be("application/json");
    }

    [Fact(DisplayName = "InvokeAsync should return valid JSON error response")]
    public async Task InvokeAsync_OnError_ReturnsValidJson()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = "/api/test";

        RequestDelegate next = _ => throw new Exception("Test error");

        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var middleware = new ErrorHandlingMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var responseBody = await ReadResponseBody(context);
        var json = JsonDocument.Parse(responseBody);

        json.RootElement.TryGetProperty("statusCode", out var statusCode).Should().BeTrue();
        statusCode.GetInt32().Should().Be(500);

        json.RootElement.TryGetProperty("message", out var message).Should().BeTrue();
        message.GetString().Should().NotBeNullOrEmpty();
    }

    [Fact(DisplayName = "InvokeAsync should use camelCase JSON naming")]
    public async Task InvokeAsync_OnError_UsesCamelCaseNaming()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        RequestDelegate next = _ => throw new Exception("Test");

        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var middleware = new ErrorHandlingMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var response = await ReadResponseBody(context);
        response.Should().Contain("statusCode");
        response.Should().NotContain("StatusCode");
    }

    #endregion

    #region Environment-Specific Behavior Tests

    [Fact(DisplayName = "InvokeAsync in Development should include exception details")]
    public async Task InvokeAsync_Development_IncludesExceptionDetails()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = "/api/test";

        var exceptionMessage = "Unique error message for testing";
        RequestDelegate next = _ => throw new Exception(exceptionMessage);

        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Development");
        var middleware = new ErrorHandlingMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var response = await ReadResponseBody(context);
        var json = JsonDocument.Parse(response);

        json.RootElement.TryGetProperty("details", out var details).Should().BeTrue();
        details.GetString().Should().Contain(exceptionMessage);
    }

    [Fact(DisplayName = "InvokeAsync in Production should NOT include exception details")]
    public async Task InvokeAsync_Production_DoesNotIncludeExceptionDetails()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = "/api/test";

        var sensitiveMessage = "Sensitive database error with connection string";
        RequestDelegate next = _ => throw new Exception(sensitiveMessage);

        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var middleware = new ErrorHandlingMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var response = await ReadResponseBody(context);
        var json = JsonDocument.Parse(response);

        json.RootElement.TryGetProperty("details", out var details).Should().BeTrue();
        details.ValueKind.Should().Be(JsonValueKind.Null);
        response.Should().NotContain(sensitiveMessage);
    }

    [Fact(DisplayName = "InvokeAsync in Staging should NOT include exception details")]
    public async Task InvokeAsync_Staging_DoesNotIncludeExceptionDetails()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        RequestDelegate next = _ => throw new Exception("Staging error");

        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Staging");
        var middleware = new ErrorHandlingMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var response = await ReadResponseBody(context);
        var json = JsonDocument.Parse(response);

        json.RootElement.TryGetProperty("details", out var details).Should().BeTrue();
        details.ValueKind.Should().Be(JsonValueKind.Null);
    }

    #endregion

    #region Logging Tests

    [Fact(DisplayName = "InvokeAsync should log error with exception details")]
    public async Task InvokeAsync_OnError_LogsError()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = "/api/reports";

        var testException = new Exception("Test logging exception");
        RequestDelegate next = _ => throw testException;

        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var middleware = new ErrorHandlingMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("/api/reports")),
                testException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact(DisplayName = "InvokeAsync should log request path in error message")]
    public async Task InvokeAsync_OnError_LogsRequestPath()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = "/api/specific/endpoint";

        RequestDelegate next = _ => throw new Exception("Error");

        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var middleware = new ErrorHandlingMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("/api/specific/endpoint")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Edge Cases

    [Fact(DisplayName = "InvokeAsync should handle nested exceptions")]
    public async Task InvokeAsync_NestedExceptions_HandlesCorrectly()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var innerException = new InvalidOperationException("Inner error");
        var outerException = new Exception("Outer error", innerException);
        RequestDelegate next = _ => throw outerException;

        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Development");
        var middleware = new ErrorHandlingMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var response = await ReadResponseBody(context);
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        response.Should().Contain("Outer error");
    }

    [Fact(DisplayName = "InvokeAsync should handle ArgumentNullException as BadRequest")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "S3928:Parameter names used into ArgumentException constructors should match an existing one", Justification = "Test intentionally uses arbitrary parameter name")]
    public async Task InvokeAsync_ArgumentNullException_Returns400()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        RequestDelegate next = _ => throw new ArgumentNullException("paramName", "Parameter is required");

        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var middleware = new ErrorHandlingMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "InvokeAsync should handle empty request path")]
    public async Task InvokeAsync_EmptyPath_HandlesCorrectly()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = "";

        RequestDelegate next = _ => throw new Exception("Error on root");

        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var middleware = new ErrorHandlingMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
    }

    #endregion

    #region Helper Methods

    private static async Task<string> ReadResponseBody(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync();
    }

    #endregion
}
