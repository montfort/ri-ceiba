using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Ceiba.Core.Enums;
using Ceiba.Core.Interfaces;
using Ceiba.Web.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Ceiba.Web.Tests.Middleware
{
    public class AuthorizationLoggingMiddlewareTests
    {
        [Fact]
        public async Task InvokeAsync_WithMalformedNameIdentifierClaim_DoesNotThrow_AndLogsAudit()
        {
            // Arrange
            var context = new DefaultHttpContext();

            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "not-a-guid"),
                new Claim(ClaimTypes.Name, "testuser")
            }, "TestAuth");

            context.User = new ClaimsPrincipal(identity);

            RequestDelegate next = ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };

            var mockLogger = new Mock<ILogger<AuthorizationLoggingMiddleware>>();
            var mockAudit = new Mock<IAuditService>();
            mockAudit
                .Setup(a => a.LogAsync(
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var middleware = new AuthorizationLoggingMiddleware(next, mockLogger.Object);

            // Act
            var ex = await Record.ExceptionAsync(() => middleware.InvokeAsync(context, mockAudit.Object));

            // Assert
            Assert.Null(ex);
            mockAudit.Verify(a => a.LogAsync(
                AuditActionCode.SECURITY_UNAUTHORIZED_ACCESS,
                null,
                null,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_WhenAuditServiceThrows_DoesNotPropagateException_AndErrorIsLogged()
        {
            // Arrange
            var context = new DefaultHttpContext();

            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, "testuser")
            }, "TestAuth");

            context.User = new ClaimsPrincipal(identity);

            RequestDelegate next = ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };

            var mockLogger = new Mock<ILogger<AuthorizationLoggingMiddleware>>();
            var mockAudit = new Mock<IAuditService>();
            mockAudit
                .Setup(a => a.LogAsync(
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("boom"));

            var middleware = new AuthorizationLoggingMiddleware(next, mockLogger.Object);

            // Act
            var ex = await Record.ExceptionAsync(() => middleware.InvokeAsync(context, mockAudit.Object));

            // Assert
            Assert.Null(ex);

            // Verificar que se intentó llamar al servicio de auditoría
            mockAudit.Verify(a => a.LogAsync(
                AuditActionCode.SECURITY_UNAUTHORIZED_ACCESS,
                null,
                null,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Once);

            // Verificar que se registró un error en el logger
            // Note: ILogger.Log uses FormattedLogValues, so we use It.IsAnyType for the state parameter
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),  // Changed from It.Is<It.IsAnyType>((v, t) => true)
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),  // Changed Func parameter types
                Times.AtLeastOnce);
        }
    }
}
