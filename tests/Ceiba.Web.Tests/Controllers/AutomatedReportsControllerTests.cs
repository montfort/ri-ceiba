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
/// Unit tests for AutomatedReportsController.
/// US4: Reportes Automatizados Diarios con IA.
/// Tests report listing, download, email sending, and generation.
/// </summary>
[Trait("Category", "Unit")]
public class AutomatedReportsControllerTests
{
    private readonly Mock<IAutomatedReportService> _mockReportService;
    private readonly Mock<ILogger<AutomatedReportsController>> _mockLogger;
    private readonly AutomatedReportsController _controller;
    private readonly Guid _testUserId = Guid.NewGuid();

    public AutomatedReportsControllerTests()
    {
        _mockReportService = new Mock<IAutomatedReportService>();
        _mockLogger = new Mock<ILogger<AutomatedReportsController>>();

        _controller = new AutomatedReportsController(
            _mockReportService.Object,
            _mockLogger.Object);

        SetupAuthenticatedUser(_testUserId, "REVISOR");
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

    #region GetReports Tests

    [Fact(DisplayName = "GetReports should return OK with list of reports")]
    public async Task GetReports_Success_ReturnsOkWithReports()
    {
        // Arrange
        var reports = new List<AutomatedReportListDto>
        {
            new() { Id = 1, FechaInicio = DateTime.UtcNow.AddDays(-1), FechaFin = DateTime.UtcNow, TotalReportes = 10 },
            new() { Id = 2, FechaInicio = DateTime.UtcNow.AddDays(-2), FechaFin = DateTime.UtcNow.AddDays(-1), TotalReportes = 15 }
        };

        _mockReportService.Setup(x => x.GetReportsAsync(0, 20, null, null, null))
            .ReturnsAsync(reports);

        // Act
        var result = await _controller.GetReports();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedReports = okResult.Value.Should().BeAssignableTo<List<AutomatedReportListDto>>().Subject;
        returnedReports.Should().HaveCount(2);
    }

    [Fact(DisplayName = "GetReports should pass filter parameters correctly")]
    public async Task GetReports_WithFilters_PassesParametersCorrectly()
    {
        // Arrange
        var fechaDesde = DateTime.UtcNow.AddDays(-7);
        var fechaHasta = DateTime.UtcNow;

        _mockReportService.Setup(x => x.GetReportsAsync(10, 5, fechaDesde, fechaHasta, true))
            .ReturnsAsync(new List<AutomatedReportListDto>());

        // Act
        var result = await _controller.GetReports(skip: 10, take: 5, fechaDesde: fechaDesde, fechaHasta: fechaHasta, enviado: true);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        _mockReportService.Verify(x => x.GetReportsAsync(10, 5, fechaDesde, fechaHasta, true), Times.Once);
    }

    [Fact(DisplayName = "GetReports should return 500 on exception")]
    public async Task GetReports_Exception_Returns500()
    {
        // Arrange
        _mockReportService.Setup(x => x.GetReportsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<bool?>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetReports();

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetReport Tests

    [Fact(DisplayName = "GetReport should return OK with report details")]
    public async Task GetReport_ExistingReport_ReturnsOkWithDetails()
    {
        // Arrange
        var report = new AutomatedReportDetailDto
        {
            Id = 1,
            FechaInicio = DateTime.UtcNow.AddDays(-1),
            FechaFin = DateTime.UtcNow,
            ContenidoMarkdown = "# Report Content",
            Enviado = true
        };

        _mockReportService.Setup(x => x.GetReportByIdAsync(1))
            .ReturnsAsync(report);

        // Act
        var result = await _controller.GetReport(1);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedReport = okResult.Value.Should().BeOfType<AutomatedReportDetailDto>().Subject;
        returnedReport.Id.Should().Be(1);
    }

    [Fact(DisplayName = "GetReport should return NotFound for non-existent report")]
    public async Task GetReport_NonExistentReport_ReturnsNotFound()
    {
        // Arrange
        _mockReportService.Setup(x => x.GetReportByIdAsync(999))
            .ReturnsAsync((AutomatedReportDetailDto?)null);

        // Act
        var result = await _controller.GetReport(999);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact(DisplayName = "GetReport should return 500 on exception")]
    public async Task GetReport_Exception_Returns500()
    {
        // Arrange
        _mockReportService.Setup(x => x.GetReportByIdAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("Error"));

        // Act
        var result = await _controller.GetReport(1);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region DownloadReport Tests

    [Fact(DisplayName = "DownloadReport should return NotFound for non-existent report")]
    public async Task DownloadReport_NonExistentReport_ReturnsNotFound()
    {
        // Arrange
        _mockReportService.Setup(x => x.GetReportByIdAsync(999))
            .ReturnsAsync((AutomatedReportDetailDto?)null);

        // Act
        var result = await _controller.DownloadReport(999, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact(DisplayName = "DownloadReport should return NotFound when no Word path exists")]
    public async Task DownloadReport_NoWordPath_ReturnsNotFound()
    {
        // Arrange
        var report = new AutomatedReportDetailDto
        {
            Id = 1,
            ContenidoWordPath = null
        };

        _mockReportService.Setup(x => x.GetReportByIdAsync(1))
            .ReturnsAsync(report);

        // Act
        var result = await _controller.DownloadReport(1, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact(DisplayName = "DownloadReport should return NotFound when file does not exist")]
    public async Task DownloadReport_FileDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var report = new AutomatedReportDetailDto
        {
            Id = 1,
            ContenidoWordPath = "/non/existent/path.docx"
        };

        _mockReportService.Setup(x => x.GetReportByIdAsync(1))
            .ReturnsAsync(report);

        // Act
        var result = await _controller.DownloadReport(1, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact(DisplayName = "DownloadReport should return 500 on exception")]
    public async Task DownloadReport_Exception_Returns500()
    {
        // Arrange
        _mockReportService.Setup(x => x.GetReportByIdAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("Error"));

        // Act
        var result = await _controller.DownloadReport(1, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region SendReport Tests

    [Fact(DisplayName = "SendReport should return OK on success")]
    public async Task SendReport_Success_ReturnsOk()
    {
        // Arrange
        _mockReportService.Setup(x => x.SendReportByEmailAsync(1, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.SendReport(1, null, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact(DisplayName = "SendReport should pass custom recipients")]
    public async Task SendReport_WithRecipients_PassesRecipients()
    {
        // Arrange
        var recipients = new List<string> { "test@example.com", "other@example.com" };
        _mockReportService.Setup(x => x.SendReportByEmailAsync(1, recipients, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.SendReport(1, recipients, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockReportService.Verify(x => x.SendReportByEmailAsync(1, recipients, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "SendReport should return BadRequest when send fails")]
    public async Task SendReport_SendFails_ReturnsBadRequest()
    {
        // Arrange
        _mockReportService.Setup(x => x.SendReportByEmailAsync(1, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.SendReport(1, null, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "SendReport should return 500 on exception")]
    public async Task SendReport_Exception_Returns500()
    {
        // Arrange
        _mockReportService.Setup(x => x.SendReportByEmailAsync(It.IsAny<int>(), It.IsAny<List<string>?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Email error"));

        // Act
        var result = await _controller.SendReport(1, null, CancellationToken.None);

        // Assert
        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region RegenerateWord Tests

    [Fact(DisplayName = "RegenerateWord should return OK on success")]
    public async Task RegenerateWord_Success_ReturnsOk()
    {
        // Arrange
        _mockReportService.Setup(x => x.RegenerateWordDocumentAsync(1))
            .ReturnsAsync("/path/to/word.docx");

        // Act
        var result = await _controller.RegenerateWord(1);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact(DisplayName = "RegenerateWord should return BadRequest when regeneration fails")]
    public async Task RegenerateWord_RegenerationFails_ReturnsBadRequest()
    {
        // Arrange
        _mockReportService.Setup(x => x.RegenerateWordDocumentAsync(1))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _controller.RegenerateWord(1);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact(DisplayName = "RegenerateWord should return 500 on exception")]
    public async Task RegenerateWord_Exception_Returns500()
    {
        // Arrange
        _mockReportService.Setup(x => x.RegenerateWordDocumentAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("Conversion error"));

        // Act
        var result = await _controller.RegenerateWord(1);

        // Assert
        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region GenerateReport Tests

    [Fact(DisplayName = "GenerateReport should return OK with generated report")]
    public async Task GenerateReport_Success_ReturnsOkWithReport()
    {
        // Arrange
        var request = new GenerateReportRequestDto
        {
            FechaInicio = DateTime.UtcNow.AddDays(-1),
            FechaFin = DateTime.UtcNow
        };

        var generatedReport = new AutomatedReportDetailDto
        {
            Id = 1,
            FechaInicio = request.FechaInicio,
            FechaFin = request.FechaFin,
            ContenidoMarkdown = "# Generated Report"
        };

        _mockReportService.Setup(x => x.GenerateReportAsync(request, _testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(generatedReport);

        // Act
        var result = await _controller.GenerateReport(request, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedReport = okResult.Value.Should().BeOfType<AutomatedReportDetailDto>().Subject;
        returnedReport.Id.Should().Be(1);
    }

    [Fact(DisplayName = "GenerateReport should pass user ID from claims")]
    public async Task GenerateReport_PassesUserIdFromClaims()
    {
        // Arrange
        var request = new GenerateReportRequestDto
        {
            FechaInicio = DateTime.UtcNow.AddDays(-1),
            FechaFin = DateTime.UtcNow
        };

        var generatedReport = new AutomatedReportDetailDto { Id = 1 };

        Guid? capturedUserId = null;
        _mockReportService.Setup(x => x.GenerateReportAsync(
                It.IsAny<GenerateReportRequestDto>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .Callback<GenerateReportRequestDto, Guid?, CancellationToken>((_, userId, _) => capturedUserId = userId)
            .ReturnsAsync(generatedReport);

        // Act
        await _controller.GenerateReport(request, CancellationToken.None);

        // Assert
        capturedUserId.Should().Be(_testUserId);
    }

    [Fact(DisplayName = "GenerateReport should return 500 on exception")]
    public async Task GenerateReport_Exception_Returns500()
    {
        // Arrange
        var request = new GenerateReportRequestDto
        {
            FechaInicio = DateTime.UtcNow.AddDays(-1),
            FechaFin = DateTime.UtcNow
        };

        _mockReportService.Setup(x => x.GenerateReportAsync(It.IsAny<GenerateReportRequestDto>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Generation error"));

        // Act
        var result = await _controller.GenerateReport(request, CancellationToken.None);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    [Fact(DisplayName = "GenerateReport should include error message in response")]
    public async Task GenerateReport_Exception_IncludesErrorMessage()
    {
        // Arrange
        var request = new GenerateReportRequestDto
        {
            FechaInicio = DateTime.UtcNow.AddDays(-1),
            FechaFin = DateTime.UtcNow
        };

        _mockReportService.Setup(x => x.GenerateReportAsync(It.IsAny<GenerateReportRequestDto>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Specific error message"));

        // Act
        var result = await _controller.GenerateReport(request, CancellationToken.None);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        var errorObject = statusResult.Value;
        errorObject.Should().NotBeNull();
    }

    #endregion

    #region Authorization Tests

    [Fact(DisplayName = "Controller should have Authorize attribute with REVISOR role")]
    public void Controller_HasAuthorizeAttribute()
    {
        // Arrange & Act
        var attributes = typeof(AutomatedReportsController)
            .GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true);

        // Assert
        attributes.Should().HaveCount(1);
        var authorizeAttribute = (Microsoft.AspNetCore.Authorization.AuthorizeAttribute)attributes[0];
        authorizeAttribute.Roles.Should().Contain("REVISOR");
    }

    [Fact(DisplayName = "Controller should have ApiController attribute")]
    public void Controller_HasApiControllerAttribute()
    {
        // Arrange & Act
        var attributes = typeof(AutomatedReportsController)
            .GetCustomAttributes(typeof(ApiControllerAttribute), true);

        // Assert
        attributes.Should().HaveCount(1);
    }

    [Fact(DisplayName = "Controller should have correct route prefix")]
    public void Controller_HasCorrectRoutePrefix()
    {
        // Arrange & Act
        var attributes = typeof(AutomatedReportsController)
            .GetCustomAttributes(typeof(RouteAttribute), true);

        // Assert
        attributes.Should().HaveCount(1);
        var routeAttribute = (RouteAttribute)attributes[0];
        routeAttribute.Template.Should().Be("api/automated-reports");
    }

    #endregion

    #region Logging Tests

    [Fact(DisplayName = "GetReports should log errors")]
    public async Task GetReports_OnError_LogsError()
    {
        // Arrange
        _mockReportService.Setup(x => x.GetReportsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<bool?>()))
            .ThrowsAsync(new Exception("Test error"));

        // Act
        await _controller.GetReports();

        // Assert
        _mockLogger.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => true),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact(DisplayName = "SendReport should log success")]
    public async Task SendReport_OnSuccess_LogsInfo()
    {
        // Arrange
        _mockReportService.Setup(x => x.SendReportByEmailAsync(1, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _controller.SendReport(1, null, CancellationToken.None);

        // Assert
        _mockLogger.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("sent successfully")),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    #endregion
}
