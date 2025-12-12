using System.Security.Claims;
using Ceiba.Core.Interfaces;
using Ceiba.Shared.DTOs;
using Ceiba.Web.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Ceiba.Web.Tests.Controllers;

/// <summary>
/// Unit tests for AuditController.
/// Tests audit log viewing and filtering operations.
/// </summary>
public class AuditControllerTests
{
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<ILogger<AuditController>> _loggerMock;
    private readonly AuditController _controller;
    private readonly Guid _adminUserId = Guid.NewGuid();

    public AuditControllerTests()
    {
        _auditServiceMock = new Mock<IAuditService>();
        _loggerMock = new Mock<ILogger<AuditController>>();

        _controller = new AuditController(
            _auditServiceMock.Object,
            _loggerMock.Object);

        SetupAuthenticatedAdmin();
    }

    private void SetupAuthenticatedAdmin()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, _adminUserId.ToString()),
            new(ClaimTypes.Role, "ADMIN")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    #region GetAuditLogs Tests

    [Fact]
    public async Task GetAuditLogs_Success_ReturnsOkWithLogs()
    {
        // Arrange
        var filter = new AuditFilterDto { Page = 1, PageSize = 10 };
        var logs = new List<AuditLogDto>
        {
            new(1, "AUTH_LOGIN", null, null, DateTime.UtcNow, _adminUserId, "test@test.com", "127.0.0.1", null),
            new(2, "REPORT_CREATE", 1, "ReporteIncidencia", DateTime.UtcNow, _adminUserId, "test@test.com", null, null)
        };

        _auditServiceMock.Setup(x => x.QueryAsync(
            It.IsAny<Guid?>(),
            It.IsAny<string?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(logs);

        // Act
        var result = await _controller.GetAuditLogs(filter);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AuditListResponse>(okResult.Value);
        Assert.Equal(2, response.Items.Count);
    }

    [Fact]
    public async Task GetAuditLogs_WithFilters_PassesCorrectParameters()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var filter = new AuditFilterDto
        {
            Page = 2,
            PageSize = 20,
            UsuarioId = userId,
            Codigo = "AUTH_LOGIN",
            FechaDesde = DateTime.Today.AddDays(-7),
            FechaHasta = DateTime.Today
        };

        _auditServiceMock.Setup(x => x.QueryAsync(
            userId,
            "AUTH_LOGIN",
            filter.FechaDesde,
            filter.FechaHasta,
            20, // skip = (page-1) * pageSize = (2-1) * 20 = 20
            20,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AuditLogDto>());

        // Act
        await _controller.GetAuditLogs(filter);

        // Assert
        _auditServiceMock.Verify(x => x.QueryAsync(
            userId,
            "AUTH_LOGIN",
            filter.FechaDesde,
            filter.FechaHasta,
            20,
            20,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAuditLogs_EmptyResult_ReturnsOkWithEmptyList()
    {
        // Arrange
        var filter = new AuditFilterDto { Page = 1, PageSize = 10 };
        _auditServiceMock.Setup(x => x.QueryAsync(
            It.IsAny<Guid?>(),
            It.IsAny<string?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AuditLogDto>());

        // Act
        var result = await _controller.GetAuditLogs(filter);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AuditListResponse>(okResult.Value);
        Assert.Empty(response.Items);
    }

    [Fact]
    public async Task GetAuditLogs_Exception_Returns500()
    {
        // Arrange
        var filter = new AuditFilterDto();
        _auditServiceMock.Setup(x => x.QueryAsync(
            It.IsAny<Guid?>(),
            It.IsAny<string?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetAuditLogs(filter);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task GetAuditLogs_MapsCodigoDescriptionCorrectly()
    {
        // Arrange
        var filter = new AuditFilterDto { Page = 1, PageSize = 10 };
        var logs = new List<AuditLogDto>
        {
            new(1, "AUTH_LOGIN", null, null, DateTime.UtcNow, _adminUserId, "test@test.com", null, null)
        };

        _auditServiceMock.Setup(x => x.QueryAsync(
            It.IsAny<Guid?>(),
            It.IsAny<string?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(logs);

        // Act
        var result = await _controller.GetAuditLogs(filter);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AuditListResponse>(okResult.Value);
        Assert.Single(response.Items);
        Assert.Equal("AUTH_LOGIN", response.Items[0].Codigo);
        // The description should be set by AuditCodes.GetDescription
        Assert.NotNull(response.Items[0].CodigoDescripcion);
    }

    #endregion

    #region GetAuditCodes Tests

    [Fact]
    public void GetAuditCodes_ReturnsAllAuditCodes()
    {
        // Act
        var result = _controller.GetAuditCodes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);

        // Cast to array to verify structure
        var codes = okResult.Value as IEnumerable<object>;
        Assert.NotNull(codes);
        Assert.NotEmpty(codes);
    }

    [Fact]
    public void GetAuditCodes_IncludesAuthenticationCodes()
    {
        // Act
        var result = _controller.GetAuditCodes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var codesJson = System.Text.Json.JsonSerializer.Serialize(okResult.Value);

        Assert.Contains("AUTH_LOGIN", codesJson);
        Assert.Contains("AUTH_LOGOUT", codesJson);
        Assert.Contains("AUTH_FAILED", codesJson);
        Assert.Contains("AUTH_LOCKED", codesJson);
    }

    [Fact]
    public void GetAuditCodes_IncludesUserManagementCodes()
    {
        // Act
        var result = _controller.GetAuditCodes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var codesJson = System.Text.Json.JsonSerializer.Serialize(okResult.Value);

        Assert.Contains("USER_CREATE", codesJson);
        Assert.Contains("USER_UPDATE", codesJson);
        Assert.Contains("USER_SUSPEND", codesJson);
        Assert.Contains("USER_DELETE", codesJson);
    }

    [Fact]
    public void GetAuditCodes_IncludesReportCodes()
    {
        // Act
        var result = _controller.GetAuditCodes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var codesJson = System.Text.Json.JsonSerializer.Serialize(okResult.Value);

        Assert.Contains("REPORT_CREATE", codesJson);
        Assert.Contains("REPORT_UPDATE", codesJson);
        Assert.Contains("REPORT_SUBMIT", codesJson);
        Assert.Contains("REPORT_EXPORT", codesJson);
    }

    [Fact]
    public void GetAuditCodes_IncludesCatalogCodes()
    {
        // Act
        var result = _controller.GetAuditCodes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var codesJson = System.Text.Json.JsonSerializer.Serialize(okResult.Value);

        Assert.Contains("CATALOG_CREATE", codesJson);
        Assert.Contains("CATALOG_UPDATE", codesJson);
        Assert.Contains("CATALOG_DELETE", codesJson);
    }

    [Fact]
    public void GetAuditCodes_IncludesSecurityCodes()
    {
        // Act
        var result = _controller.GetAuditCodes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var codesJson = System.Text.Json.JsonSerializer.Serialize(okResult.Value);

        Assert.Contains("ACCESS_DENIED", codesJson);
        Assert.Contains("SESSION_EXPIRED", codesJson);
    }

    #endregion

    #region GetRelatedTables Tests

    [Fact]
    public void GetRelatedTables_ReturnsAllTables()
    {
        // Act
        var result = _controller.GetRelatedTables();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var tables = Assert.IsType<string[]>(okResult.Value);

        Assert.Contains("Usuario", tables);
        Assert.Contains("ReporteIncidencia", tables);
        Assert.Contains("Zona", tables);
        Assert.Contains("Sector", tables);
        Assert.Contains("Cuadrante", tables);
        Assert.Contains("CatalogoSugerencia", tables);
    }

    [Fact]
    public void GetRelatedTables_Returns6Tables()
    {
        // Act
        var result = _controller.GetRelatedTables();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var tables = Assert.IsType<string[]>(okResult.Value);

        Assert.Equal(6, tables.Length);
    }

    #endregion
}
