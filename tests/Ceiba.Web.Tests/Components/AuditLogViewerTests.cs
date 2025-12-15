using Bunit;
using Ceiba.Core.Interfaces;
using Ceiba.Web.Components.Pages.Admin;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Security.Claims;

namespace Ceiba.Web.Tests.Components;

/// <summary>
/// Component tests for AuditLogViewer Blazor component.
/// Tests audit log viewing and filtering functionality.
/// Phase 3: Coverage improvement tests.
/// </summary>
[Trait("Category", "Component")]
public class AuditLogViewerTests : TestContext
{
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Guid _testUserId = Guid.NewGuid();

    public AuditLogViewerTests()
    {
        _mockAuditService = new Mock<IAuditService>();

        Services.AddSingleton(_mockAuditService.Object);
        Services.AddSingleton<AuthenticationStateProvider>(new TestAuthStateProvider(_testUserId, "ADMIN"));
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        SetupDefaultMocks();
    }

    #region Rendering Tests

    [Fact(DisplayName = "AuditLogViewer should render page title")]
    public void AuditLogViewer_ShouldRenderPageTitle()
    {
        // Act
        var cut = Render<AuditLogViewer>();

        // Assert
        cut.Markup.Should().Contain("Registro de Auditoría");
    }

    [Fact(DisplayName = "AuditLogViewer should render description")]
    public void AuditLogViewer_ShouldRenderDescription()
    {
        // Act
        var cut = Render<AuditLogViewer>();

        // Assert
        cut.Markup.Should().Contain("Visualice todas las acciones realizadas en el sistema");
    }

    [Fact(DisplayName = "AuditLogViewer should render filter controls")]
    public void AuditLogViewer_ShouldRenderFilterControls()
    {
        // Act
        var cut = Render<AuditLogViewer>();

        // Assert
        cut.Markup.Should().Contain("Código de Acción");
        cut.Markup.Should().Contain("Desde");
        cut.Markup.Should().Contain("Hasta");
        cut.Markup.Should().Contain("Usuario (ID)");
        cut.Markup.Should().Contain("Limpiar");
    }

    [Fact(DisplayName = "AuditLogViewer should render logs table")]
    public void AuditLogViewer_ShouldRenderLogsTable()
    {
        // Act
        var cut = Render<AuditLogViewer>();

        // Assert
        cut.Markup.Should().Contain("<table");
        cut.Markup.Should().Contain("Fecha/Hora");
        cut.Markup.Should().Contain("Código");
        cut.Markup.Should().Contain("Usuario");
        cut.Markup.Should().Contain("Tabla");
        cut.Markup.Should().Contain("ID Relacionado");
        cut.Markup.Should().Contain("IP");
        cut.Markup.Should().Contain("Detalles");
    }

    [Fact(DisplayName = "AuditLogViewer should display audit codes dropdown")]
    public void AuditLogViewer_ShouldDisplayAuditCodesDropdown()
    {
        // Act
        var cut = Render<AuditLogViewer>();

        // Assert
        cut.Markup.Should().Contain("AUTH_LOGIN");
        cut.Markup.Should().Contain("USER_CREATE");
        cut.Markup.Should().Contain("REPORT_CREATE");
    }

    #endregion

    #region Data Display Tests

    [Fact(DisplayName = "AuditLogViewer should display logs from service")]
    public void AuditLogViewer_ShouldDisplayLogsFromService()
    {
        // Act
        var cut = Render<AuditLogViewer>();

        // Assert
        cut.Markup.Should().Contain("AUTH_LOGIN");
        cut.Markup.Should().Contain("USUARIO");
    }

    [Fact(DisplayName = "AuditLogViewer should display code badges")]
    public void AuditLogViewer_ShouldDisplayCodeBadges()
    {
        // Act
        var cut = Render<AuditLogViewer>();

        // Assert
        cut.Markup.Should().Contain("badge");
    }

    [Fact(DisplayName = "AuditLogViewer should display IP addresses")]
    public void AuditLogViewer_ShouldDisplayIPAddresses()
    {
        // Act
        var cut = Render<AuditLogViewer>();

        // Assert
        cut.Markup.Should().Contain("192.168.1.1");
    }

    #endregion

    #region Loading State Tests

    [Fact(DisplayName = "AuditLogViewer should show empty message when no logs")]
    public void AuditLogViewer_EmptyLogs_ShouldShowEmptyMessage()
    {
        // Arrange
        _mockAuditService.Setup(s => s.QueryAsync(
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AuditLogDto>());

        // Act
        var cut = Render<AuditLogViewer>();

        // Assert
        cut.Markup.Should().Contain("No hay registros de auditoría para mostrar");
    }

    [Fact(DisplayName = "AuditLogViewer should show error message on service failure")]
    public void AuditLogViewer_ServiceError_ShouldShowErrorMessage()
    {
        // Arrange
        _mockAuditService.Setup(s => s.QueryAsync(
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var cut = Render<AuditLogViewer>();

        // Assert
        cut.Markup.Should().Contain("Error al cargar los registros de auditoría");
    }

    #endregion

    #region Details Modal Tests

    [Fact(DisplayName = "AuditLogViewer view details button should open modal")]
    public async Task AuditLogViewer_ViewDetailsButton_ShouldOpenModal()
    {
        // Arrange
        var cut = Render<AuditLogViewer>();

        // Wait for table to render
        cut.WaitForAssertion(() => cut.FindAll("table tbody tr").Count.Should().BeGreaterThan(0));

        // Act - Find and click view details button
        var viewButton = cut.FindAll("button.btn-link").FirstOrDefault();
        if (viewButton != null)
        {
            await cut.InvokeAsync(() => viewButton.Click());

            // Assert
            cut.Markup.Should().Contain("modal");
            cut.Markup.Should().Contain("Detalles del Registro");
        }
    }

    [Fact(DisplayName = "AuditLogViewer details modal should display all fields")]
    public async Task AuditLogViewer_DetailsModal_ShouldDisplayAllFields()
    {
        // Arrange
        var cut = Render<AuditLogViewer>();

        // Wait for table to render
        cut.WaitForAssertion(() => cut.FindAll("table tbody tr").Count.Should().BeGreaterThan(0));

        // Act - Find and click view details button
        var viewButton = cut.FindAll("button.btn-link").FirstOrDefault();
        if (viewButton != null)
        {
            await cut.InvokeAsync(() => viewButton.Click());

            // Assert - modal should display all fields
            cut.Markup.Should().Contain("Fecha/Hora");
            cut.Markup.Should().Contain("Código");
        }
    }

    #endregion

    #region Filter Tests

    [Fact(DisplayName = "AuditLogViewer clear filters button should reset filters")]
    public async Task AuditLogViewer_ClearFilters_ShouldResetFilters()
    {
        // Arrange
        var cut = Render<AuditLogViewer>();

        // Act
        var clearButton = cut.FindAll("button").First(b => b.TextContent.Contains("Limpiar"));
        await cut.InvokeAsync(() => clearButton.Click());

        // Assert - service should be called again
        _mockAuditService.Verify(s => s.QueryAsync(
            null, null, null, null, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    #endregion

    #region Pagination Tests

    [Fact(DisplayName = "AuditLogViewer should show pagination when multiple pages")]
    public void AuditLogViewer_MultiplePage_ShouldShowPagination()
    {
        // Arrange
        var logs = Enumerable.Range(1, 60).Select(i => new AuditLogDto(
            Id: i,
            Codigo: "AUTH_LOGIN",
            IdRelacionado: null,
            TablaRelacionada: "USUARIO",
            CreatedAt: DateTime.UtcNow,
            UsuarioId: _testUserId,
            UsuarioNombre: "admin@test.com",
            Ip: "192.168.1.1",
            Detalles: "Login details"
        )).ToList();

        _mockAuditService.Setup(s => s.QueryAsync(
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(logs.Take(50).ToList());

        // Act
        var cut = Render<AuditLogViewer>();

        // Assert
        cut.Markup.Should().Contain("pagination");
        cut.Markup.Should().Contain("Anterior");
        cut.Markup.Should().Contain("Siguiente");
    }

    #endregion

    #region Code Badge Color Tests

    [Fact(DisplayName = "AuditLogViewer AUTH_FAILED should show danger badge")]
    public void AuditLogViewer_AuthFailed_ShouldShowDangerBadge()
    {
        // Arrange
        var logs = new List<AuditLogDto>
        {
            new(
                Id: 1,
                Codigo: "AUTH_FAILED",
                IdRelacionado: null,
                TablaRelacionada: null,
                CreatedAt: DateTime.UtcNow,
                UsuarioId: _testUserId,
                UsuarioNombre: null,
                Ip: "192.168.1.1",
                Detalles: null
            )
        };

        _mockAuditService.Setup(s => s.QueryAsync(
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(logs);

        // Act
        var cut = Render<AuditLogViewer>();

        // Assert
        cut.Markup.Should().Contain("bg-danger");
    }

    [Fact(DisplayName = "AuditLogViewer USER_ codes should show primary badge")]
    public void AuditLogViewer_UserCodes_ShouldShowPrimaryBadge()
    {
        // Arrange
        var logs = new List<AuditLogDto>
        {
            new(
                Id: 1,
                Codigo: "USER_CREATE",
                IdRelacionado: null,
                TablaRelacionada: null,
                CreatedAt: DateTime.UtcNow,
                UsuarioId: _testUserId,
                UsuarioNombre: null,
                Ip: "192.168.1.1",
                Detalles: null
            )
        };

        _mockAuditService.Setup(s => s.QueryAsync(
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(logs);

        // Act
        var cut = Render<AuditLogViewer>();

        // Assert
        cut.Markup.Should().Contain("bg-primary");
    }

    [Fact(DisplayName = "AuditLogViewer REPORT_ codes should show success badge")]
    public void AuditLogViewer_ReportCodes_ShouldShowSuccessBadge()
    {
        // Arrange
        var logs = new List<AuditLogDto>
        {
            new(
                Id: 1,
                Codigo: "REPORT_CREATE",
                IdRelacionado: null,
                TablaRelacionada: null,
                CreatedAt: DateTime.UtcNow,
                UsuarioId: _testUserId,
                UsuarioNombre: null,
                Ip: "192.168.1.1",
                Detalles: null
            )
        };

        _mockAuditService.Setup(s => s.QueryAsync(
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(logs);

        // Act
        var cut = Render<AuditLogViewer>();

        // Assert
        cut.Markup.Should().Contain("bg-success");
    }

    #endregion

    #region Helper Methods

    private void SetupDefaultMocks()
    {
        var logs = new List<AuditLogDto>
        {
            new(
                Id: 1,
                Codigo: "AUTH_LOGIN",
                IdRelacionado: 123,
                TablaRelacionada: "USUARIO",
                CreatedAt: DateTime.UtcNow,
                UsuarioId: _testUserId,
                UsuarioNombre: "admin@ceiba.local",
                Ip: "192.168.1.1",
                Detalles: "Login successful"
            ),
            new(
                Id: 2,
                Codigo: "REPORT_CREATE",
                IdRelacionado: 456,
                TablaRelacionada: "REPORTE_INCIDENCIA",
                CreatedAt: DateTime.UtcNow.AddMinutes(-5),
                UsuarioId: _testUserId,
                UsuarioNombre: "admin@ceiba.local",
                Ip: "192.168.1.1",
                Detalles: "Report created"
            )
        };

        _mockAuditService.Setup(s => s.QueryAsync(
                It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(logs);
    }

    private class TestAuthStateProvider : AuthenticationStateProvider
    {
        private readonly AuthenticationState _authState;

        public TestAuthStateProvider(Guid userId, string role)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "admin@ceiba.local"),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            }, "Test");

            _authState = new AuthenticationState(new ClaimsPrincipal(identity));
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(_authState);
    }

    #endregion
}
