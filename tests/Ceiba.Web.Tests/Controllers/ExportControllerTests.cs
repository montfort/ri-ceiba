using System.Security.Claims;
using Ceiba.Application.Services.Export;
using Ceiba.Core.Interfaces;
using Ceiba.Shared.DTOs;
using Ceiba.Shared.DTOs.Export;
using Ceiba.Web.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Ceiba.Web.Tests.Controllers;

/// <summary>
/// Unit tests for ExportController.
/// Tests report export operations (PDF and JSON).
/// </summary>
public class ExportControllerTests
{
    private readonly Mock<IExportService> _exportServiceMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<ILogger<ExportController>> _loggerMock;
    private readonly ExportController _controller;
    private readonly Guid _revisorUserId = Guid.NewGuid();

    public ExportControllerTests()
    {
        _exportServiceMock = new Mock<IExportService>();
        _auditServiceMock = new Mock<IAuditService>();
        _loggerMock = new Mock<ILogger<ExportController>>();

        _controller = new ExportController(
            _exportServiceMock.Object,
            _auditServiceMock.Object,
            _loggerMock.Object);

        SetupAuthenticatedRevisor();
    }

    private void SetupAuthenticatedRevisor()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, _revisorUserId.ToString()),
            new(ClaimTypes.Role, "REVISOR")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    private void SetupUnauthenticatedUser()
    {
        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    #region ExportReport Tests

    [Fact]
    public async Task ExportReport_PDFFormat_ReturnsFileResult()
    {
        // Arrange
        var reportId = 1;
        var exportResult = new ExportResultDto
        {
            Data = new byte[] { 0x25, 0x50, 0x44, 0x46 }, // PDF magic bytes
            FileName = "report_1.pdf",
            ContentType = "application/pdf"
        };

        _exportServiceMock.Setup(x => x.ExportSingleReportAsync(reportId, ExportFormat.PDF, _revisorUserId, true))
            .ReturnsAsync(exportResult);

        _auditServiceMock.Setup(x => x.LogAsync(
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ExportReport(reportId, ExportFormat.PDF);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", fileResult.ContentType);
        Assert.Equal("report_1.pdf", fileResult.FileDownloadName);
    }

    [Fact]
    public async Task ExportReport_JSONFormat_ReturnsFileResult()
    {
        // Arrange
        var reportId = 1;
        var exportResult = new ExportResultDto
        {
            Data = System.Text.Encoding.UTF8.GetBytes("{}"),
            FileName = "report_1.json",
            ContentType = "application/json"
        };

        _exportServiceMock.Setup(x => x.ExportSingleReportAsync(reportId, ExportFormat.JSON, _revisorUserId, true))
            .ReturnsAsync(exportResult);

        _auditServiceMock.Setup(x => x.LogAsync(
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ExportReport(reportId, ExportFormat.JSON);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/json", fileResult.ContentType);
    }

    [Fact]
    public async Task ExportReport_NotFound_ReturnsNotFound()
    {
        // Arrange
        var reportId = 999;
        _exportServiceMock.Setup(x => x.ExportSingleReportAsync(reportId, ExportFormat.PDF, _revisorUserId, true))
            .ThrowsAsync(new KeyNotFoundException("Report not found"));

        // Act
        var result = await _controller.ExportReport(reportId, ExportFormat.PDF);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task ExportReport_Unauthorized_Returns403()
    {
        // Arrange
        var reportId = 1;
        _exportServiceMock.Setup(x => x.ExportSingleReportAsync(reportId, ExportFormat.PDF, _revisorUserId, true))
            .ThrowsAsync(new UnauthorizedAccessException("Access denied"));

        // Act
        var result = await _controller.ExportReport(reportId, ExportFormat.PDF);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, statusResult.StatusCode);
    }

    [Fact]
    public async Task ExportReport_Exception_Returns500()
    {
        // Arrange
        var reportId = 1;
        _exportServiceMock.Setup(x => x.ExportSingleReportAsync(reportId, ExportFormat.PDF, _revisorUserId, true))
            .ThrowsAsync(new Exception("Export failed"));

        // Act
        var result = await _controller.ExportReport(reportId, ExportFormat.PDF);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task ExportReport_LogsAuditEntry()
    {
        // Arrange
        var reportId = 1;
        var exportResult = new ExportResultDto
        {
            Data = new byte[] { 1, 2, 3 },
            FileName = "report_1.pdf",
            ContentType = "application/pdf"
        };

        _exportServiceMock.Setup(x => x.ExportSingleReportAsync(reportId, ExportFormat.PDF, _revisorUserId, true))
            .ReturnsAsync(exportResult);

        _auditServiceMock.Setup(x => x.LogAsync(
            "REPORT_EXPORT",
            reportId,
            "ReporteIncidencia",
            It.Is<string>(s => s.Contains("PDF") && s.Contains("report_1.pdf")),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _controller.ExportReport(reportId, ExportFormat.PDF);

        // Assert
        _auditServiceMock.Verify(x => x.LogAsync(
            "REPORT_EXPORT",
            reportId,
            "ReporteIncidencia",
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region ExportBulk Tests

    [Fact]
    public async Task ExportBulk_ValidRequest_ReturnsFileResult()
    {
        // Arrange
        var request = new ExportRequestDto
        {
            ReportIds = new[] { 1, 2, 3 },
            Format = ExportFormat.PDF
        };

        var exportResult = new ExportResultDto
        {
            Data = new byte[] { 0x50, 0x4B, 0x03, 0x04 }, // ZIP magic bytes
            FileName = "reports_20241211.zip",
            ContentType = "application/zip",
            ReportCount = 3
        };

        _exportServiceMock.Setup(x => x.ExportReportsAsync(request, _revisorUserId, true))
            .ReturnsAsync(exportResult);

        _auditServiceMock.Setup(x => x.LogAsync(
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ExportBulk(request);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/zip", fileResult.ContentType);
    }

    [Fact]
    public async Task ExportBulk_InvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var request = new ExportRequestDto { ReportIds = Array.Empty<int>() };
        _exportServiceMock.Setup(x => x.ExportReportsAsync(request, _revisorUserId, true))
            .ThrowsAsync(new ArgumentException("No reports specified"));

        // Act
        var result = await _controller.ExportBulk(request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task ExportBulk_NoReportsFound_ReturnsNotFound()
    {
        // Arrange
        var request = new ExportRequestDto { ReportIds = new[] { 999, 1000 } };
        _exportServiceMock.Setup(x => x.ExportReportsAsync(request, _revisorUserId, true))
            .ThrowsAsync(new KeyNotFoundException("No reports found"));

        // Act
        var result = await _controller.ExportBulk(request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task ExportBulk_Unauthorized_Returns403()
    {
        // Arrange
        var request = new ExportRequestDto { ReportIds = new[] { 1 } };
        _exportServiceMock.Setup(x => x.ExportReportsAsync(request, _revisorUserId, true))
            .ThrowsAsync(new UnauthorizedAccessException("Access denied"));

        // Act
        var result = await _controller.ExportBulk(request);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, statusResult.StatusCode);
    }

    [Fact]
    public async Task ExportBulk_Exception_Returns500()
    {
        // Arrange
        var request = new ExportRequestDto { ReportIds = new[] { 1 } };
        _exportServiceMock.Setup(x => x.ExportReportsAsync(request, _revisorUserId, true))
            .ThrowsAsync(new Exception("Export failed"));

        // Act
        var result = await _controller.ExportBulk(request);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task ExportBulk_LogsBulkAuditEntry()
    {
        // Arrange
        var request = new ExportRequestDto
        {
            ReportIds = new[] { 1, 2 },
            Format = ExportFormat.JSON
        };

        var exportResult = new ExportResultDto
        {
            Data = new byte[] { 1, 2, 3 },
            FileName = "reports.zip",
            ContentType = "application/zip",
            ReportCount = 2
        };

        _exportServiceMock.Setup(x => x.ExportReportsAsync(request, _revisorUserId, true))
            .ReturnsAsync(exportResult);

        _auditServiceMock.Setup(x => x.LogAsync(
            "REPORT_EXPORT_BULK",
            null,
            "ReporteIncidencia",
            It.Is<string>(s => s.Contains("2 reportes")),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _controller.ExportBulk(request);

        // Assert
        _auditServiceMock.Verify(x => x.LogAsync(
            "REPORT_EXPORT_BULK",
            null,
            "ReporteIncidencia",
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetFormats Tests

    [Fact]
    public void GetFormats_ReturnsAvailableFormats()
    {
        // Act
        var result = _controller.GetFormats();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public void GetFormats_IncludesPDFFormat()
    {
        // Act
        var result = _controller.GetFormats();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
        Assert.Contains("PDF", json);
    }

    [Fact]
    public void GetFormats_IncludesJSONFormat()
    {
        // Act
        var result = _controller.GetFormats();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
        Assert.Contains("JSON", json);
    }

    #endregion

    #region Unauthenticated User Tests

    [Fact]
    public async Task ExportReport_UnauthenticatedUser_Returns500()
    {
        // Arrange
        SetupUnauthenticatedUser();

        // Act
        var result = await _controller.ExportReport(1, ExportFormat.PDF);

        // Assert - Controller catches UnauthorizedAccessException and returns 403
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, statusResult.StatusCode);
    }

    [Fact]
    public async Task ExportBulk_UnauthenticatedUser_Returns500()
    {
        // Arrange
        SetupUnauthenticatedUser();
        var request = new ExportRequestDto { ReportIds = new[] { 1 } };

        // Act
        var result = await _controller.ExportBulk(request);

        // Assert - Controller catches UnauthorizedAccessException and returns 403
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, statusResult.StatusCode);
    }

    #endregion
}
