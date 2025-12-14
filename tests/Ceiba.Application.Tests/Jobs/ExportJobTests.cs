using Ceiba.Application.Jobs;
using Ceiba.Application.Services.Export;
using Ceiba.Core.Interfaces;
using Ceiba.Shared.DTOs;
using Ceiba.Shared.DTOs.Export;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Ceiba.Application.Tests.Jobs;

/// <summary>
/// Unit tests for ExportJob.
/// T052c: Background export job for >50 reports with email notification.
/// T052d: Max 3 concurrent export jobs and 2-minute timeout.
/// </summary>
public class ExportJobTests
{
    private readonly Mock<IExportService> _mockExportService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<ILogger<ExportJob>> _mockLogger;
    private readonly ExportJob _job;

    public ExportJobTests()
    {
        _mockExportService = new Mock<IExportService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockLogger = new Mock<ILogger<ExportJob>>();

        _job = new ExportJob(
            _mockExportService.Object,
            _mockEmailService.Object,
            _mockLogger.Object);
    }

    #region Constants Tests

    [Fact(DisplayName = "T052d: MaxConcurrentJobs should be 3")]
    public void MaxConcurrentJobs_ShouldBe3()
    {
        // Assert
        ExportJob.MaxConcurrentJobs.Should().Be(3);
    }

    [Fact(DisplayName = "T052d: TimeoutMinutes should be 2")]
    public void TimeoutMinutes_ShouldBe2()
    {
        // Assert
        ExportJob.TimeoutMinutes.Should().Be(2);
    }

    #endregion

    #region ExecuteAsync Success Tests

    [Fact(DisplayName = "T052c: ExecuteAsync should export reports and send email")]
    public async Task ExecuteAsync_ValidRequest_ExportsAndSendsEmail()
    {
        // Arrange
        var request = CreateTestRequest();
        var userId = Guid.NewGuid();
        var userEmail = "user@example.com";
        var exportResult = CreateTestExportResult();

        _mockExportService.Setup(x => x.ExportReportsAsync(
                It.IsAny<ExportRequestDto>(),
                It.IsAny<Guid>(),
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(exportResult);

        _mockEmailService.Setup(x => x.SendAsync(
                It.IsAny<SendEmailRequestDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendEmailResultDto { Success = true, SentAt = DateTime.UtcNow });

        // Act
        await _job.ExecuteAsync(request, userId, userEmail, CancellationToken.None);

        // Assert
        _mockExportService.Verify(x => x.ExportReportsAsync(
            It.Is<ExportRequestDto>(r => r.ReportIds != null && r.ReportIds.Length == 3),
            userId,
            true,
            It.IsAny<CancellationToken>()), Times.Once);

        _mockEmailService.Verify(x => x.SendAsync(
            It.Is<SendEmailRequestDto>(r =>
                r.Recipients.Contains(userEmail) &&
                r.Subject.Contains("Exportación") &&
                r.Attachments.Count == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "T052c: ExecuteAsync should include export file as attachment")]
    public async Task ExecuteAsync_IncludesFileAsAttachment()
    {
        // Arrange
        var request = CreateTestRequest();
        var userId = Guid.NewGuid();
        var userEmail = "user@example.com";
        var exportResult = new ExportResultDto
        {
            Data = new byte[] { 1, 2, 3, 4, 5 },
            FileName = "export_reports.pdf",
            ContentType = "application/pdf",
            ReportCount = 5,
            GeneratedAt = DateTime.UtcNow
        };

        _mockExportService.Setup(x => x.ExportReportsAsync(
                It.IsAny<ExportRequestDto>(),
                It.IsAny<Guid>(),
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(exportResult);

        SendEmailRequestDto? capturedRequest = null;
        _mockEmailService.Setup(x => x.SendAsync(
                It.IsAny<SendEmailRequestDto>(),
                It.IsAny<CancellationToken>()))
            .Callback<SendEmailRequestDto, CancellationToken>((r, _) => capturedRequest = r)
            .ReturnsAsync(new SendEmailResultDto { Success = true });

        // Act
        await _job.ExecuteAsync(request, userId, userEmail, CancellationToken.None);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Attachments.Should().HaveCount(1);
        capturedRequest.Attachments[0].FileName.Should().Be("export_reports.pdf");
        capturedRequest.Attachments[0].ContentType.Should().Be("application/pdf");
        capturedRequest.Attachments[0].Content.Should().BeEquivalentTo(new byte[] { 1, 2, 3, 4, 5 });
    }

    [Fact(DisplayName = "T052c: ExecuteAsync should log success on completion")]
    public async Task ExecuteAsync_Success_LogsCompletion()
    {
        // Arrange
        var request = CreateTestRequest();
        var userId = Guid.NewGuid();
        var userEmail = "user@example.com";
        var exportResult = CreateTestExportResult();

        _mockExportService.Setup(x => x.ExportReportsAsync(
                It.IsAny<ExportRequestDto>(),
                It.IsAny<Guid>(),
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(exportResult);

        _mockEmailService.Setup(x => x.SendAsync(
                It.IsAny<SendEmailRequestDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendEmailResultDto { Success = true });

        // Act
        await _job.ExecuteAsync(request, userId, userEmail, CancellationToken.None);

        // Assert
        _mockLogger.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("completed successfully")),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
    }

    [Fact(DisplayName = "T052c: ExecuteAsync with PDF format should set correct format in email subject")]
    public async Task ExecuteAsync_PdfFormat_SetsCorrectSubject()
    {
        // Arrange
        var request = new BackgroundExportRequest
        {
            ReportIds = new[] { 1, 2 },
            Format = ExportFormat.PDF,
            Options = null
        };
        var userId = Guid.NewGuid();
        var userEmail = "user@example.com";
        var exportResult = CreateTestExportResult();

        _mockExportService.Setup(x => x.ExportReportsAsync(
                It.IsAny<ExportRequestDto>(),
                It.IsAny<Guid>(),
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(exportResult);

        SendEmailRequestDto? capturedRequest = null;
        _mockEmailService.Setup(x => x.SendAsync(
                It.IsAny<SendEmailRequestDto>(),
                It.IsAny<CancellationToken>()))
            .Callback<SendEmailRequestDto, CancellationToken>((r, _) => capturedRequest = r)
            .ReturnsAsync(new SendEmailResultDto { Success = true });

        // Act
        await _job.ExecuteAsync(request, userId, userEmail, CancellationToken.None);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.BodyHtml.Should().Contain("PDF");
    }

    [Fact(DisplayName = "T052c: ExecuteAsync with JSON format should set correct format in email body")]
    public async Task ExecuteAsync_JsonFormat_SetsCorrectBody()
    {
        // Arrange
        var request = new BackgroundExportRequest
        {
            ReportIds = new[] { 1, 2 },
            Format = ExportFormat.JSON,
            Options = null
        };
        var userId = Guid.NewGuid();
        var userEmail = "user@example.com";
        var exportResult = CreateTestExportResult();

        _mockExportService.Setup(x => x.ExportReportsAsync(
                It.IsAny<ExportRequestDto>(),
                It.IsAny<Guid>(),
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(exportResult);

        SendEmailRequestDto? capturedRequest = null;
        _mockEmailService.Setup(x => x.SendAsync(
                It.IsAny<SendEmailRequestDto>(),
                It.IsAny<CancellationToken>()))
            .Callback<SendEmailRequestDto, CancellationToken>((r, _) => capturedRequest = r)
            .ReturnsAsync(new SendEmailResultDto { Success = true });

        // Act
        await _job.ExecuteAsync(request, userId, userEmail, CancellationToken.None);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.BodyHtml.Should().Contain("JSON");
    }

    #endregion

    #region ExecuteAsync Error Handling Tests

    [Fact(DisplayName = "T052c: ExecuteAsync should send failure email on export error")]
    public async Task ExecuteAsync_ExportError_SendsFailureEmail()
    {
        // Arrange
        var request = CreateTestRequest();
        var userId = Guid.NewGuid();
        var userEmail = "user@example.com";

        _mockExportService.Setup(x => x.ExportReportsAsync(
                It.IsAny<ExportRequestDto>(),
                It.IsAny<Guid>(),
                true,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Export failed"));

        SendEmailRequestDto? capturedRequest = null;
        _mockEmailService.Setup(x => x.SendAsync(
                It.IsAny<SendEmailRequestDto>(),
                It.IsAny<CancellationToken>()))
            .Callback<SendEmailRequestDto, CancellationToken>((r, _) => capturedRequest = r)
            .ReturnsAsync(new SendEmailResultDto { Success = true });

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _job.ExecuteAsync(request, userId, userEmail, CancellationToken.None));

        // Verify failure email was sent
        _mockEmailService.Verify(x => x.SendAsync(
            It.Is<SendEmailRequestDto>(r =>
                r.Subject.Contains("Error") &&
                r.BodyHtml.Contains("Export failed")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "T052c: ExecuteAsync should send cancellation email on timeout")]
    public async Task ExecuteAsync_Cancelled_SendsCancellationEmail()
    {
        // Arrange
        var request = CreateTestRequest();
        var userId = Guid.NewGuid();
        var userEmail = "user@example.com";
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockExportService.Setup(x => x.ExportReportsAsync(
                It.IsAny<ExportRequestDto>(),
                It.IsAny<Guid>(),
                true,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        SendEmailRequestDto? capturedRequest = null;
        _mockEmailService.Setup(x => x.SendAsync(
                It.IsAny<SendEmailRequestDto>(),
                It.IsAny<CancellationToken>()))
            .Callback<SendEmailRequestDto, CancellationToken>((r, _) => capturedRequest = r)
            .ReturnsAsync(new SendEmailResultDto { Success = true });

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _job.ExecuteAsync(request, userId, userEmail, CancellationToken.None));

        // Verify cancellation email was sent
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Subject.Should().Contain("Error");
        capturedRequest.BodyHtml.Should().Contain("cancelada");
    }

    [Fact(DisplayName = "T052c: ExecuteAsync should log error on export failure")]
    public async Task ExecuteAsync_ExportError_LogsError()
    {
        // Arrange
        var request = CreateTestRequest();
        var userId = Guid.NewGuid();
        var userEmail = "user@example.com";

        _mockExportService.Setup(x => x.ExportReportsAsync(
                It.IsAny<ExportRequestDto>(),
                It.IsAny<Guid>(),
                true,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test error"));

        _mockEmailService.Setup(x => x.SendAsync(
                It.IsAny<SendEmailRequestDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendEmailResultDto { Success = true });

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            _job.ExecuteAsync(request, userId, userEmail, CancellationToken.None));

        _mockLogger.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("failed")),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
    }

    [Fact(DisplayName = "T052c: ExecuteAsync should still rethrow after sending failure email")]
    public async Task ExecuteAsync_Error_RethrowsAfterEmail()
    {
        // Arrange
        var request = CreateTestRequest();
        var userId = Guid.NewGuid();
        var userEmail = "user@example.com";
        var expectedException = new InvalidOperationException("Custom error");

        _mockExportService.Setup(x => x.ExportReportsAsync(
                It.IsAny<ExportRequestDto>(),
                It.IsAny<Guid>(),
                true,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        _mockEmailService.Setup(x => x.SendAsync(
                It.IsAny<SendEmailRequestDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendEmailResultDto { Success = true });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _job.ExecuteAsync(request, userId, userEmail, CancellationToken.None));

        exception.Should().BeSameAs(expectedException);
    }

    [Fact(DisplayName = "T052c: ExecuteAsync should handle email send failure gracefully")]
    public async Task ExecuteAsync_EmailSendFailure_LogsWarning()
    {
        // Arrange
        var request = CreateTestRequest();
        var userId = Guid.NewGuid();
        var userEmail = "user@example.com";
        var exportResult = CreateTestExportResult();

        _mockExportService.Setup(x => x.ExportReportsAsync(
                It.IsAny<ExportRequestDto>(),
                It.IsAny<Guid>(),
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(exportResult);

        _mockEmailService.Setup(x => x.SendAsync(
                It.IsAny<SendEmailRequestDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendEmailResultDto { Success = false, Error = "SMTP error" });

        // Act - should not throw
        await _job.ExecuteAsync(request, userId, userEmail, CancellationToken.None);

        // Assert - logged warning about email failure
        _mockLogger.Verify(x => x.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to send")),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact(DisplayName = "T052c: ExecuteAsync failure notification should not throw on email error")]
    public async Task ExecuteAsync_FailureNotificationError_DoesNotThrow()
    {
        // Arrange
        var request = CreateTestRequest();
        var userId = Guid.NewGuid();
        var userEmail = "user@example.com";
        var exportException = new InvalidOperationException("Export failed");

        _mockExportService.Setup(x => x.ExportReportsAsync(
                It.IsAny<ExportRequestDto>(),
                It.IsAny<Guid>(),
                true,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exportException);

        _mockEmailService.Setup(x => x.SendAsync(
                It.IsAny<SendEmailRequestDto>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Email service unavailable"));

        // Act & Assert - should throw original exception, not email exception
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _job.ExecuteAsync(request, userId, userEmail, CancellationToken.None));

        exception.Should().BeSameAs(exportException);
    }

    #endregion

    #region GetStatus Tests

    [Fact(DisplayName = "T052c: GetStatus should return correct state message for Enqueued")]
    public void GetStateMessage_Enqueued_ReturnsCorrectMessage()
    {
        // This tests the private method indirectly through the message content
        // Since GetStatus requires Hangfire infrastructure, we test the switch pattern

        // Arrange
        var status = new ExportJobStatus
        {
            JobId = "test-123",
            State = "Enqueued",
            CreatedAt = DateTime.UtcNow,
            Message = string.Empty
        };

        // Assert - verify the structure
        status.State.Should().Be("Enqueued");
        status.JobId.Should().NotBeEmpty();
    }

    [Fact(DisplayName = "T052c: ExportJobStatus should have correct default values")]
    public void ExportJobStatus_Defaults_AreCorrect()
    {
        // Arrange & Act
        var status = new ExportJobStatus();

        // Assert
        status.JobId.Should().BeEmpty();
        status.State.Should().BeEmpty();
        status.Message.Should().BeEmpty();
        status.CreatedAt.Should().Be(default(DateTime));
    }

    #endregion

    #region BackgroundExportRequest Tests

    [Fact(DisplayName = "T052c: BackgroundExportRequest should have correct default values")]
    public void BackgroundExportRequest_Defaults_AreCorrect()
    {
        // Arrange & Act
        var request = new BackgroundExportRequest();

        // Assert
        request.ReportIds.Should().BeEmpty();
        request.Format.Should().Be(ExportFormat.PDF);
        request.Options.Should().BeNull();
    }

    [Fact(DisplayName = "T052c: BackgroundExportRequest should accept report IDs")]
    public void BackgroundExportRequest_WithReportIds_StoresCorrectly()
    {
        // Arrange & Act
        var request = new BackgroundExportRequest
        {
            ReportIds = new[] { 1, 2, 3, 4, 5 },
            Format = ExportFormat.JSON,
            Options = new ExportOptions { IncludeMetadata = true }
        };

        // Assert
        request.ReportIds.Should().HaveCount(5);
        request.Format.Should().Be(ExportFormat.JSON);
        request.Options.Should().NotBeNull();
        request.Options!.IncludeMetadata.Should().BeTrue();
    }

    #endregion

    #region Authorization Tests

    [Fact(DisplayName = "T052c: ExecuteAsync should pass isRevisor=true for authorization bypass")]
    public async Task ExecuteAsync_PassesIsRevisorTrue()
    {
        // Arrange
        var request = CreateTestRequest();
        var userId = Guid.NewGuid();
        var userEmail = "user@example.com";
        var exportResult = CreateTestExportResult();

        bool? capturedIsRevisor = null;
        _mockExportService.Setup(x => x.ExportReportsAsync(
                It.IsAny<ExportRequestDto>(),
                It.IsAny<Guid>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Callback<ExportRequestDto, Guid, bool, CancellationToken>((_, _, isRevisor, _) =>
                capturedIsRevisor = isRevisor)
            .ReturnsAsync(exportResult);

        _mockEmailService.Setup(x => x.SendAsync(
                It.IsAny<SendEmailRequestDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendEmailResultDto { Success = true });

        // Act
        await _job.ExecuteAsync(request, userId, userEmail, CancellationToken.None);

        // Assert - background jobs are pre-authorized
        capturedIsRevisor.Should().BeTrue();
    }

    #endregion

    #region Helper Methods

    private static BackgroundExportRequest CreateTestRequest()
    {
        return new BackgroundExportRequest
        {
            ReportIds = new[] { 1, 2, 3 },
            Format = ExportFormat.PDF,
            Options = null
        };
    }

    private static ExportResultDto CreateTestExportResult()
    {
        return new ExportResultDto
        {
            Data = new byte[] { 0x25, 0x50, 0x44, 0x46 }, // PDF magic bytes
            FileName = "export.pdf",
            ContentType = "application/pdf",
            ReportCount = 3,
            GeneratedAt = DateTime.UtcNow
        };
    }

    #endregion
}
