using System.Security.Claims;
using Ceiba.Core.Exceptions;
using Ceiba.Core.Interfaces;
using Ceiba.Shared.DTOs;
using Ceiba.Web.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Ceiba.Web.Tests.Controllers;

/// <summary>
/// Unit tests for ReportsController.
/// Tests report CRUD operations and authorization logic.
/// </summary>
public class ReportsControllerTests
{
    private readonly Mock<IReportService> _reportServiceMock;
    private readonly Mock<ILogger<ReportsController>> _loggerMock;
    private readonly ReportsController _controller;
    private readonly Guid _testUserId = Guid.NewGuid();

    public ReportsControllerTests()
    {
        _reportServiceMock = new Mock<IReportService>();
        _loggerMock = new Mock<ILogger<ReportsController>>();

        _controller = new ReportsController(
            _reportServiceMock.Object,
            _loggerMock.Object);

        SetupAuthenticatedUser(_testUserId, "CREADOR");
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

    private void SetupUnauthenticatedUser()
    {
        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    #region ListReports Tests

    [Fact]
    public async Task ListReports_Success_ReturnsOkWithReports()
    {
        // Arrange
        var filter = new ReportFilterDto { Page = 1, PageSize = 10 };
        var response = new ReportListResponse
        {
            Items = new List<ReportDto>
            {
                new() { Id = 1, TipoReporte = "A" },
                new() { Id = 2, TipoReporte = "A" }
            },
            TotalCount = 2,
            Page = 1,
            PageSize = 10
        };

        _reportServiceMock.Setup(x => x.ListReportsAsync(filter, _testUserId, false))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.ListReports(filter);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedResponse = Assert.IsType<ReportListResponse>(okResult.Value);
        Assert.Equal(2, returnedResponse.TotalCount);
    }

    [Fact]
    public async Task ListReports_AsRevisor_PassesRevisorFlag()
    {
        // Arrange
        SetupAuthenticatedUser(_testUserId, "REVISOR");
        var filter = new ReportFilterDto();

        _reportServiceMock.Setup(x => x.ListReportsAsync(filter, _testUserId, true))
            .ReturnsAsync(new ReportListResponse());

        // Act
        await _controller.ListReports(filter);

        // Assert
        _reportServiceMock.Verify(x => x.ListReportsAsync(filter, _testUserId, true), Times.Once);
    }

    [Fact]
    public async Task ListReports_Exception_Returns500()
    {
        // Arrange
        var filter = new ReportFilterDto();
        _reportServiceMock.Setup(x => x.ListReportsAsync(It.IsAny<ReportFilterDto>(), It.IsAny<Guid>(), It.IsAny<bool>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.ListReports(filter);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    #endregion

    #region GetReport Tests

    [Fact]
    public async Task GetReport_ExistingReport_ReturnsOk()
    {
        // Arrange
        var report = new ReportDto { Id = 1, TipoReporte = "A" };
        _reportServiceMock.Setup(x => x.GetReportByIdAsync(1, _testUserId, false))
            .ReturnsAsync(report);

        // Act
        var result = await _controller.GetReport(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<ReportDto>(okResult.Value);
        Assert.Equal(1, returnedReport.Id);
    }

    [Fact]
    public async Task GetReport_NotFound_ReturnsNotFound()
    {
        // Arrange
        _reportServiceMock.Setup(x => x.GetReportByIdAsync(999, _testUserId, false))
            .ThrowsAsync(new NotFoundException("Reporte no encontrado"));

        // Act
        var result = await _controller.GetReport(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal(404, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task GetReport_Forbidden_ReturnsForbid()
    {
        // Arrange
        _reportServiceMock.Setup(x => x.GetReportByIdAsync(1, _testUserId, false))
            .ThrowsAsync(new ForbiddenException("No tiene permiso"));

        // Act
        var result = await _controller.GetReport(1);

        // Assert
        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task GetReport_Exception_Returns500()
    {
        // Arrange
        _reportServiceMock.Setup(x => x.GetReportByIdAsync(1, _testUserId, false))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _controller.GetReport(1);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    #endregion

    #region CreateReport Tests

    [Fact]
    public async Task CreateReport_ValidData_ReturnsCreated()
    {
        // Arrange
        var createDto = new CreateReportDto
        {
            DatetimeHechos = DateTime.Today,
            TurnoCeiba = "Balderas 1",
            Sexo = "Masculino",
            Edad = 30,
            Delito = "Robo",
            ZonaId = 1,
            SectorId = 1,
            CuadranteId = 1,
            TipoDeAtencion = "Inmediata",
            TipoDeAccion = "Preventiva",
            HechosReportados = "Descripción de los hechos reportados",
            AccionesRealizadas = "Descripción de las acciones realizadas"
        };
        var createdReport = new ReportDto { Id = 1, TipoReporte = "A" };

        _reportServiceMock.Setup(x => x.CreateReportAsync(createDto, _testUserId))
            .ReturnsAsync(createdReport);

        // Act
        var result = await _controller.CreateReport(createDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(201, createdResult.StatusCode);
        Assert.Equal("GetReport", createdResult.ActionName);
        Assert.Equal(1, createdResult.RouteValues!["id"]);
    }

    [Fact]
    public async Task CreateReport_ValidationError_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateReportDto();
        _reportServiceMock.Setup(x => x.CreateReportAsync(createDto, _testUserId))
            .ThrowsAsync(new ValidationException("Fecha es requerida"));

        // Act
        var result = await _controller.CreateReport(createDto);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task CreateReport_Exception_Returns500()
    {
        // Arrange
        var createDto = new CreateReportDto();
        _reportServiceMock.Setup(x => x.CreateReportAsync(createDto, _testUserId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.CreateReport(createDto);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    #endregion

    #region UpdateReport Tests

    [Fact]
    public async Task UpdateReport_ValidData_ReturnsOk()
    {
        // Arrange
        var updateDto = new UpdateReportDto { TurnoCeiba = "Balderas 2" };
        var updatedReport = new ReportDto { Id = 1, TurnoCeiba = "Balderas 2" };

        _reportServiceMock.Setup(x => x.UpdateReportAsync(1, updateDto, _testUserId, false))
            .ReturnsAsync(updatedReport);

        // Act
        var result = await _controller.UpdateReport(1, updateDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<ReportDto>(okResult.Value);
        Assert.Equal("Balderas 2", returnedReport.TurnoCeiba);
    }

    [Fact]
    public async Task UpdateReport_NotFound_ReturnsNotFound()
    {
        // Arrange
        var updateDto = new UpdateReportDto();
        _reportServiceMock.Setup(x => x.UpdateReportAsync(999, updateDto, _testUserId, false))
            .ThrowsAsync(new NotFoundException("Reporte no encontrado"));

        // Act
        var result = await _controller.UpdateReport(999, updateDto);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal(404, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task UpdateReport_Forbidden_Returns403()
    {
        // Arrange
        var updateDto = new UpdateReportDto();
        _reportServiceMock.Setup(x => x.UpdateReportAsync(1, updateDto, _testUserId, false))
            .ThrowsAsync(new ForbiddenException("No puede editar este reporte"));

        // Act
        var result = await _controller.UpdateReport(1, updateDto);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, statusResult.StatusCode);
    }

    [Fact]
    public async Task UpdateReport_ValidationError_ReturnsBadRequest()
    {
        // Arrange
        var updateDto = new UpdateReportDto();
        _reportServiceMock.Setup(x => x.UpdateReportAsync(1, updateDto, _testUserId, false))
            .ThrowsAsync(new ValidationException("Datos inválidos"));

        // Act
        var result = await _controller.UpdateReport(1, updateDto);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task UpdateReport_AsRevisor_PassesRevisorFlag()
    {
        // Arrange
        SetupAuthenticatedUser(_testUserId, "REVISOR");
        var updateDto = new UpdateReportDto();

        _reportServiceMock.Setup(x => x.UpdateReportAsync(1, updateDto, _testUserId, true))
            .ReturnsAsync(new ReportDto());

        // Act
        await _controller.UpdateReport(1, updateDto);

        // Assert
        _reportServiceMock.Verify(x => x.UpdateReportAsync(1, updateDto, _testUserId, true), Times.Once);
    }

    #endregion

    #region SubmitReport Tests

    [Fact]
    public async Task SubmitReport_Success_ReturnsOk()
    {
        // Arrange
        var submittedReport = new ReportDto { Id = 1, Estado = 1 }; // 1 = Entregado
        _reportServiceMock.Setup(x => x.SubmitReportAsync(1, _testUserId))
            .ReturnsAsync(submittedReport);

        // Act
        var result = await _controller.SubmitReport(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReport = Assert.IsType<ReportDto>(okResult.Value);
        Assert.Equal(1, returnedReport.Estado);
    }

    [Fact]
    public async Task SubmitReport_NotFound_ReturnsNotFound()
    {
        // Arrange
        _reportServiceMock.Setup(x => x.SubmitReportAsync(999, _testUserId))
            .ThrowsAsync(new NotFoundException("Reporte no encontrado"));

        // Act
        var result = await _controller.SubmitReport(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task SubmitReport_Forbidden_Returns403()
    {
        // Arrange
        _reportServiceMock.Setup(x => x.SubmitReportAsync(1, _testUserId))
            .ThrowsAsync(new ForbiddenException("No es su reporte"));

        // Act
        var result = await _controller.SubmitReport(1);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, statusResult.StatusCode);
    }

    [Fact]
    public async Task SubmitReport_AlreadySubmitted_ReturnsBadRequest()
    {
        // Arrange
        _reportServiceMock.Setup(x => x.SubmitReportAsync(1, _testUserId))
            .ThrowsAsync(new BadRequestException("Reporte ya entregado"));

        // Act
        var result = await _controller.SubmitReport(1);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    #endregion

    #region DeleteReport Tests

    [Fact]
    public async Task DeleteReport_Success_ReturnsOk()
    {
        // Arrange
        _reportServiceMock.Setup(x => x.DeleteReportAsync(1, _testUserId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteReport(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public async Task DeleteReport_NotFound_ReturnsNotFound()
    {
        // Arrange
        _reportServiceMock.Setup(x => x.DeleteReportAsync(999, _testUserId))
            .ThrowsAsync(new NotFoundException("Reporte no encontrado"));

        // Act
        var result = await _controller.DeleteReport(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteReport_Forbidden_Returns403()
    {
        // Arrange
        _reportServiceMock.Setup(x => x.DeleteReportAsync(1, _testUserId))
            .ThrowsAsync(new ForbiddenException("No es su reporte"));

        // Act
        var result = await _controller.DeleteReport(1);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, statusResult.StatusCode);
    }

    [Fact]
    public async Task DeleteReport_AlreadySubmitted_ReturnsBadRequest()
    {
        // Arrange
        _reportServiceMock.Setup(x => x.DeleteReportAsync(1, _testUserId))
            .ThrowsAsync(new BadRequestException("No se puede eliminar reporte entregado"));

        // Act
        var result = await _controller.DeleteReport(1);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task DeleteReport_Exception_Returns500()
    {
        // Arrange
        _reportServiceMock.Setup(x => x.DeleteReportAsync(1, _testUserId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.DeleteReport(1);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    #endregion

    #region Authorization Tests

    [Fact]
    public async Task CreateReport_UnauthenticatedUser_Returns500()
    {
        // Arrange
        SetupUnauthenticatedUser();
        var createDto = new CreateReportDto();

        // Note: In real application, the [AuthorizeBeforeModelBinding] would prevent this
        // For unit testing, we test that GetUsuarioId throws and controller catches it

        // Act
        var result = await _controller.CreateReport(createDto);

        // Assert - Controller catches UnauthorizedAccessException and returns 500
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    #endregion
}
