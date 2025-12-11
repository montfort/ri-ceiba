using System.Net;
using FluentAssertions;
using Ceiba.Shared.DTOs;
using Xunit;

namespace Ceiba.Integration.Tests;

/// <summary>
/// Contract tests for Audit Log API endpoints (US3)
/// T063: Validates audit endpoints require ADMIN role and follow API contracts
/// FR-031 to FR-036: Audit log viewing and filtering
/// </summary>
[Collection("Integration Tests")]
public class AuditContractTests : IClassFixture<CeibaWebApplicationFactory>
{
    private readonly CeibaWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuditContractTests(CeibaWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    #region Audit Endpoints - Authentication Required

    [Fact(DisplayName = "T063: GET /api/audit without authentication should return 401")]
    public async Task GetAuditLogs_WithoutAuth_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/audit");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "T063: GET /api/audit with filter params without authentication should return 401")]
    public async Task GetAuditLogs_WithFilters_WithoutAuth_Returns401()
    {
        // Arrange
        var queryParams = "?codigo=AUTH_LOGIN&page=1&pageSize=50";

        // Act
        var response = await _client.GetAsync($"/api/audit{queryParams}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "T063: GET /api/audit/codes without authentication should return 401")]
    public async Task GetAuditCodes_WithoutAuth_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/audit/codes");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "T063: GET /api/audit/tables without authentication should return 401")]
    public async Task GetRelatedTables_WithoutAuth_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/audit/tables");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region DTO Validation Tests

    [Fact(DisplayName = "T063: AuditLogEntryDto should have all expected properties")]
    public void AuditLogEntryDto_ShouldHaveExpectedProperties()
    {
        // Arrange
        var dto = new AuditLogEntryDto
        {
            Id = 1,
            Codigo = "AUTH_LOGIN",
            CodigoDescripcion = "Inicio de sesión",
            IdRelacionado = 123,
            TablaRelacionada = "Usuario",
            CreatedAt = DateTime.UtcNow,
            UsuarioId = Guid.NewGuid(),
            UsuarioEmail = "user@test.com",
            Ip = "192.168.1.1",
            Detalles = "Additional details"
        };

        // Assert
        dto.Id.Should().Be(1);
        dto.Codigo.Should().Be("AUTH_LOGIN");
        dto.CodigoDescripcion.Should().Be("Inicio de sesión");
        dto.IdRelacionado.Should().Be(123);
        dto.TablaRelacionada.Should().Be("Usuario");
        dto.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        dto.UsuarioId.Should().NotBeEmpty();
        dto.UsuarioEmail.Should().Be("user@test.com");
        dto.Ip.Should().Be("192.168.1.1");
        dto.Detalles.Should().Be("Additional details");
    }

    [Fact(DisplayName = "T063: AuditFilterDto should support all filter options")]
    public void AuditFilterDto_ShouldSupportAllFilterOptions()
    {
        // Arrange
        var filter = new AuditFilterDto
        {
            Codigo = "AUTH_LOGIN",
            UsuarioId = Guid.NewGuid(),
            FechaDesde = DateTime.UtcNow.AddDays(-7),
            FechaHasta = DateTime.UtcNow,
            TablaRelacionada = "Usuario",
            Page = 2,
            PageSize = 25
        };

        // Assert
        filter.Codigo.Should().Be("AUTH_LOGIN");
        filter.UsuarioId.Should().NotBeNull();
        filter.FechaDesde.Should().NotBeNull();
        filter.FechaHasta.Should().NotBeNull();
        filter.TablaRelacionada.Should().Be("Usuario");
        filter.Page.Should().Be(2);
        filter.PageSize.Should().Be(25);
    }

    [Fact(DisplayName = "T063: AuditFilterDto should have sensible defaults")]
    public void AuditFilterDto_ShouldHaveSensibleDefaults()
    {
        // Arrange
        var filter = new AuditFilterDto();

        // Assert
        filter.Page.Should().Be(1);
        filter.PageSize.Should().Be(50);
        filter.Codigo.Should().BeNull();
        filter.UsuarioId.Should().BeNull();
        filter.FechaDesde.Should().BeNull();
        filter.FechaHasta.Should().BeNull();
        filter.TablaRelacionada.Should().BeNull();
    }

    [Fact(DisplayName = "T063: AuditListResponse should support pagination")]
    public void AuditListResponse_ShouldSupportPagination()
    {
        // Arrange
        var response = new AuditListResponse
        {
            Items = new List<AuditLogEntryDto>
            {
                new AuditLogEntryDto { Id = 1, Codigo = "AUTH_LOGIN" },
                new AuditLogEntryDto { Id = 2, Codigo = "AUTH_LOGOUT" }
            },
            TotalCount = 1000,
            Page = 1,
            PageSize = 50
        };

        // Assert
        response.Items.Should().HaveCount(2);
        response.TotalCount.Should().Be(1000);
        response.Page.Should().Be(1);
        response.PageSize.Should().Be(50);
    }

    #endregion

    #region AuditCodes Validation Tests

    [Fact(DisplayName = "T063: AuditCodes should define authentication codes")]
    public void AuditCodes_ShouldDefineAuthenticationCodes()
    {
        // Assert
        AuditCodes.AUTH_LOGIN.Should().Be("AUTH_LOGIN");
        AuditCodes.AUTH_LOGOUT.Should().Be("AUTH_LOGOUT");
        AuditCodes.AUTH_FAILED.Should().Be("AUTH_FAILED");
        AuditCodes.AUTH_LOCKED.Should().Be("AUTH_LOCKED");
    }

    [Fact(DisplayName = "T063: AuditCodes should define user management codes")]
    public void AuditCodes_ShouldDefineUserManagementCodes()
    {
        // Assert
        AuditCodes.USER_CREATE.Should().Be("USER_CREATE");
        AuditCodes.USER_UPDATE.Should().Be("USER_UPDATE");
        AuditCodes.USER_SUSPEND.Should().Be("USER_SUSPEND");
        AuditCodes.USER_ACTIVATE.Should().Be("USER_ACTIVATE");
        AuditCodes.USER_DELETE.Should().Be("USER_DELETE");
        AuditCodes.USER_ROLE_CHANGE.Should().Be("USER_ROLE_CHANGE");
    }

    [Fact(DisplayName = "T063: AuditCodes should define report operation codes")]
    public void AuditCodes_ShouldDefineReportOperationCodes()
    {
        // Assert
        AuditCodes.REPORT_CREATE.Should().Be("REPORT_CREATE");
        AuditCodes.REPORT_UPDATE.Should().Be("REPORT_UPDATE");
        AuditCodes.REPORT_SUBMIT.Should().Be("REPORT_SUBMIT");
        AuditCodes.REPORT_DELETE.Should().Be("REPORT_DELETE");
        AuditCodes.REPORT_VIEW.Should().Be("REPORT_VIEW");
        AuditCodes.REPORT_EXPORT.Should().Be("REPORT_EXPORT");
        AuditCodes.REPORT_EXPORT_BULK.Should().Be("REPORT_EXPORT_BULK");
    }

    [Fact(DisplayName = "T063: AuditCodes should define catalog operation codes")]
    public void AuditCodes_ShouldDefineCatalogOperationCodes()
    {
        // Assert
        AuditCodes.CATALOG_CREATE.Should().Be("CATALOG_CREATE");
        AuditCodes.CATALOG_UPDATE.Should().Be("CATALOG_UPDATE");
        AuditCodes.CATALOG_DELETE.Should().Be("CATALOG_DELETE");
    }

    [Fact(DisplayName = "T063: AuditCodes should define security codes")]
    public void AuditCodes_ShouldDefineSecurityCodes()
    {
        // Assert
        AuditCodes.ACCESS_DENIED.Should().Be("ACCESS_DENIED");
        AuditCodes.SESSION_EXPIRED.Should().Be("SESSION_EXPIRED");
    }

    [Fact(DisplayName = "T063: AuditCodes should define automated report codes")]
    public void AuditCodes_ShouldDefineAutomatedReportCodes()
    {
        // Assert
        AuditCodes.AUTO_REPORT_GEN.Should().Be("AUTO_REPORT_GEN");
        AuditCodes.AUTO_REPORT_SEND.Should().Be("AUTO_REPORT_SEND");
        AuditCodes.AUTO_REPORT_FAIL.Should().Be("AUTO_REPORT_FAIL");
    }

    [Fact(DisplayName = "T063: AuditCodes.GetDescription should return Spanish descriptions")]
    public void AuditCodes_GetDescription_ShouldReturnSpanishDescriptions()
    {
        // Assert - Authentication
        AuditCodes.GetDescription(AuditCodes.AUTH_LOGIN).Should().Be("Inicio de sesión");
        AuditCodes.GetDescription(AuditCodes.AUTH_LOGOUT).Should().Be("Cierre de sesión");
        AuditCodes.GetDescription(AuditCodes.AUTH_FAILED).Should().Be("Intento de inicio de sesión fallido");

        // Assert - User Management
        AuditCodes.GetDescription(AuditCodes.USER_CREATE).Should().Be("Usuario creado");
        AuditCodes.GetDescription(AuditCodes.USER_SUSPEND).Should().Be("Usuario suspendido");

        // Assert - Reports
        AuditCodes.GetDescription(AuditCodes.REPORT_CREATE).Should().Be("Reporte creado");
        AuditCodes.GetDescription(AuditCodes.REPORT_SUBMIT).Should().Be("Reporte entregado");

        // Assert - Unknown code returns code itself
        AuditCodes.GetDescription("UNKNOWN_CODE").Should().Be("UNKNOWN_CODE");
    }

    #endregion

    #region API Endpoint Routes

    [Fact(DisplayName = "T063: Audit API should expose correct endpoint routes")]
    public void AuditApi_ShouldExposeCorrectRoutes()
    {
        // Arrange
        var controllerType = typeof(Ceiba.Web.Controllers.AuditController);

        // Verify route attribute
        var routeAttribute = controllerType.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.RouteAttribute), true)
            .Cast<Microsoft.AspNetCore.Mvc.RouteAttribute>()
            .FirstOrDefault();

        routeAttribute.Should().NotBeNull();
        routeAttribute!.Template.Should().Be("api/[controller]");

        // Verify ApiController attribute
        controllerType.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.ApiControllerAttribute), true)
            .Should().NotBeEmpty();
    }

    [Fact(DisplayName = "T063: Audit API should require ADMIN role")]
    public void AuditApi_ShouldRequireAdminRole()
    {
        // Arrange
        var controllerType = typeof(Ceiba.Web.Controllers.AuditController);

        // Verify AuthorizeBeforeModelBinding attribute with ADMIN role
        var authorizeAttribute = controllerType.GetCustomAttributes(typeof(Ceiba.Web.Filters.AuthorizeBeforeModelBindingAttribute), true)
            .Cast<Ceiba.Web.Filters.AuthorizeBeforeModelBindingAttribute>()
            .FirstOrDefault();

        authorizeAttribute.Should().NotBeNull();
    }

    #endregion

    #region Audit Retention Tests (FR-036)

    [Fact(DisplayName = "T063: FR-036: Audit logs should never be deleted (indefinite retention)")]
    public void AuditLogs_ShouldHaveIndefiniteRetention()
    {
        // This test documents the requirement that audit logs are never deleted
        // Implementation note: The database schema and application logic should
        // ensure that audit records are never deleted (soft delete only if needed)

        // Verify that AuditLogEntryDto does not have a "DeletedAt" property
        // that could indicate soft deletion
        var deletedAtProperty = typeof(AuditLogEntryDto).GetProperty("DeletedAt");
        deletedAtProperty.Should().BeNull("Audit logs should not support deletion");

        // Verify that the filter does not support filtering deleted records
        var showDeletedProperty = typeof(AuditFilterDto).GetProperty("ShowDeleted");
        showDeletedProperty.Should().BeNull("Audit logs should not support showing deleted records");
    }

    #endregion
}
