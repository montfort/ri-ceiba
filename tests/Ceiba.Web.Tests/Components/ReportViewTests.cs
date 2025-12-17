using Bunit;
using Ceiba.Core.Exceptions;
using Ceiba.Core.Interfaces;
using Ceiba.Shared.DTOs;
using Ceiba.Web.Components.Pages.Reports;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Security.Claims;

namespace Ceiba.Web.Tests.Components;

/// <summary>
/// Component tests for ReportView Blazor component.
/// Tests report viewing with detail display, submit, and delete functionality.
/// Phase 3: Coverage improvement tests.
/// </summary>
[Trait("Category", "Component")]
public class ReportViewTests : TestContext
{
    private readonly Mock<IReportService> _mockReportService;
    private readonly FakeNavigationManager _navigationManager;
    private readonly Guid _testUserId = Guid.NewGuid();
    private const int TestReportId = 123;

    public ReportViewTests()
    {
        _mockReportService = new Mock<IReportService>();
        _navigationManager = new FakeNavigationManager();

        Services.AddSingleton(_mockReportService.Object);
        Services.AddSingleton<NavigationManager>(_navigationManager);
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
    }

    private void SetupAuth(string role = "CREADOR")
    {
        Services.AddSingleton<AuthenticationStateProvider>(new TestAuthStateProvider(_testUserId, role));
    }

    #region Loading State Tests

    [Fact(DisplayName = "ReportView should display loading spinner initially")]
    public void ReportView_ShouldDisplayLoadingSpinner()
    {
        // Arrange
        SetupAuth();
        _mockReportService.Setup(s => s.GetReportByIdAsync(It.IsAny<int>(), It.IsAny<Guid>(), false))
            .Returns(async () =>
            {
                await Task.Delay(5000);
                return CreateTestReport(_testUserId);
            });

        // Act
        var cut = Render<ReportView>(parameters => parameters.Add(p => p.ReportId, TestReportId));

        // Assert
        cut.Markup.Should().Contain("spinner-border");
        cut.Markup.Should().Contain("Cargando reporte");
    }

    #endregion

    #region Rendering Tests

    [Fact(DisplayName = "ReportView should display report details")]
    public void ReportView_ShouldDisplayReportDetails()
    {
        // Arrange
        SetupAuth();
        var report = CreateTestReport(_testUserId);
        _mockReportService.Setup(s => s.GetReportByIdAsync(TestReportId, _testUserId, false))
            .ReturnsAsync(report);

        // Act
        var cut = Render<ReportView>(parameters => parameters.Add(p => p.ReportId, TestReportId));

        // Assert
        cut.Markup.Should().Contain($"Reporte de Incidencia #{report.Id}");
        cut.Markup.Should().Contain(report.Delito);
        cut.Markup.Should().Contain(report.Zona.Nombre);
        cut.Markup.Should().Contain(report.HechosReportados);
    }

    [Fact(DisplayName = "ReportView should display estado badge for Borrador")]
    public void ReportView_ShouldDisplayBorradorBadge()
    {
        // Arrange
        SetupAuth();
        var report = CreateTestReport(_testUserId, estado: 0);
        _mockReportService.Setup(s => s.GetReportByIdAsync(TestReportId, _testUserId, false))
            .ReturnsAsync(report);

        // Act
        var cut = Render<ReportView>(parameters => parameters.Add(p => p.ReportId, TestReportId));

        // Assert
        cut.Markup.Should().Contain("Borrador");
        cut.Markup.Should().Contain("bg-warning");
    }

    [Fact(DisplayName = "ReportView should display estado badge for Entregado")]
    public void ReportView_ShouldDisplayEntregadoBadge()
    {
        // Arrange
        SetupAuth();
        var report = CreateTestReport(_testUserId, estado: 1);
        _mockReportService.Setup(s => s.GetReportByIdAsync(TestReportId, _testUserId, false))
            .ReturnsAsync(report);

        // Act
        var cut = Render<ReportView>(parameters => parameters.Add(p => p.ReportId, TestReportId));

        // Assert
        cut.Markup.Should().Contain("Entregado");
        cut.Markup.Should().Contain("bg-success");
    }

    [Fact(DisplayName = "ReportView should display victim data")]
    public void ReportView_ShouldDisplayVictimData()
    {
        // Arrange
        SetupAuth();
        var report = CreateTestReport(_testUserId);
        _mockReportService.Setup(s => s.GetReportByIdAsync(TestReportId, _testUserId, false))
            .ReturnsAsync(report);

        // Act
        var cut = Render<ReportView>(parameters => parameters.Add(p => p.ReportId, TestReportId));

        // Assert
        cut.Markup.Should().Contain(report.Sexo);
        cut.Markup.Should().Contain(report.Edad.ToString());
        cut.Markup.Should().Contain("LGBTTTIQ+");
    }

    [Fact(DisplayName = "ReportView should display geographic location")]
    public void ReportView_ShouldDisplayGeographicLocation()
    {
        // Arrange
        SetupAuth();
        var report = CreateTestReport(_testUserId);
        _mockReportService.Setup(s => s.GetReportByIdAsync(TestReportId, _testUserId, false))
            .ReturnsAsync(report);

        // Act
        var cut = Render<ReportView>(parameters => parameters.Add(p => p.ReportId, TestReportId));

        // Assert
        cut.Markup.Should().Contain(report.Zona.Nombre);
        cut.Markup.Should().Contain(report.Sector.Nombre);
        cut.Markup.Should().Contain(report.Cuadrante.Nombre);
    }

    [Fact(DisplayName = "ReportView should display narrative sections")]
    public void ReportView_ShouldDisplayNarrativeSections()
    {
        // Arrange
        SetupAuth();
        var report = CreateTestReport(_testUserId);
        _mockReportService.Setup(s => s.GetReportByIdAsync(TestReportId, _testUserId, false))
            .ReturnsAsync(report);

        // Act
        var cut = Render<ReportView>(parameters => parameters.Add(p => p.ReportId, TestReportId));

        // Assert
        cut.Markup.Should().Contain("Hechos Reportados");
        cut.Markup.Should().Contain(report.HechosReportados);
        cut.Markup.Should().Contain("Acciones Realizadas");
        cut.Markup.Should().Contain(report.AccionesRealizadas);
    }

    #endregion

    #region Error Handling Tests

    [Fact(DisplayName = "ReportView should display error when report not found")]
    public void ReportView_ShouldDisplayNotFoundError()
    {
        // Arrange
        SetupAuth();
        _mockReportService.Setup(s => s.GetReportByIdAsync(TestReportId, _testUserId, false))
            .ThrowsAsync(new NotFoundException("Report not found"));

        // Act
        var cut = Render<ReportView>(parameters => parameters.Add(p => p.ReportId, TestReportId));

        // Assert
        cut.Markup.Should().Contain("alert-danger");
        cut.Markup.Should().Contain("El reporte solicitado no existe");
    }

    [Fact(DisplayName = "ReportView should display error when forbidden")]
    public void ReportView_ShouldDisplayForbiddenError()
    {
        // Arrange
        SetupAuth();
        _mockReportService.Setup(s => s.GetReportByIdAsync(TestReportId, _testUserId, false))
            .ThrowsAsync(new ForbiddenException("Access denied"));

        // Act
        var cut = Render<ReportView>(parameters => parameters.Add(p => p.ReportId, TestReportId));

        // Assert
        cut.Markup.Should().Contain("alert-danger");
        cut.Markup.Should().Contain("No tiene permisos para ver este reporte");
    }

    [Fact(DisplayName = "ReportView should display generic error on exception")]
    public void ReportView_ShouldDisplayGenericError()
    {
        // Arrange
        SetupAuth();
        _mockReportService.Setup(s => s.GetReportByIdAsync(TestReportId, _testUserId, false))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var cut = Render<ReportView>(parameters => parameters.Add(p => p.ReportId, TestReportId));

        // Assert
        cut.Markup.Should().Contain("alert-danger");
        cut.Markup.Should().Contain("Error al cargar el reporte");
    }

    [Fact(DisplayName = "ReportView error state should display Volver button")]
    public void ReportView_ErrorState_ShouldDisplayVolverButton()
    {
        // Arrange
        SetupAuth();
        _mockReportService.Setup(s => s.GetReportByIdAsync(TestReportId, _testUserId, false))
            .ThrowsAsync(new NotFoundException("Not found"));

        // Act
        var cut = Render<ReportView>(parameters => parameters.Add(p => p.ReportId, TestReportId));

        // Assert
        var volverButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Volver"));
        volverButton.Should().NotBeNull();
    }

    #endregion

    #region Action Button Visibility Tests

    [Fact(DisplayName = "ReportView should show Edit button for own Borrador report")]
    public void ReportView_OwnBorrador_ShouldShowEditButton()
    {
        // Arrange
        SetupAuth();
        var report = CreateTestReport(_testUserId, estado: 0);
        _mockReportService.Setup(s => s.GetReportByIdAsync(TestReportId, _testUserId, false))
            .ReturnsAsync(report);

        // Act
        var cut = Render<ReportView>(parameters => parameters.Add(p => p.ReportId, TestReportId));

        // Assert
        var editButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Editar"));
        editButton.Should().NotBeNull();
    }

    [Fact(DisplayName = "ReportView should show Entregar button for own Borrador report")]
    public void ReportView_OwnBorrador_ShouldShowEntregarButton()
    {
        // Arrange
        SetupAuth();
        var report = CreateTestReport(_testUserId, estado: 0);
        _mockReportService.Setup(s => s.GetReportByIdAsync(TestReportId, _testUserId, false))
            .ReturnsAsync(report);

        // Act
        var cut = Render<ReportView>(parameters => parameters.Add(p => p.ReportId, TestReportId));

        // Assert
        var submitButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Entregar"));
        submitButton.Should().NotBeNull();
    }

    [Fact(DisplayName = "ReportView should show Eliminar button for own Borrador report")]
    public void ReportView_OwnBorrador_ShouldShowEliminarButton()
    {
        // Arrange
        SetupAuth();
        var report = CreateTestReport(_testUserId, estado: 0);
        _mockReportService.Setup(s => s.GetReportByIdAsync(TestReportId, _testUserId, false))
            .ReturnsAsync(report);

        // Act
        var cut = Render<ReportView>(parameters => parameters.Add(p => p.ReportId, TestReportId));

        // Assert
        var deleteButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Eliminar"));
        deleteButton.Should().NotBeNull();
    }

    [Fact(DisplayName = "ReportView should NOT show Edit/Submit/Delete for Entregado report")]
    public void ReportView_Entregado_ShouldNotShowActionButtons()
    {
        // Arrange
        SetupAuth();
        var report = CreateTestReport(_testUserId, estado: 1);
        _mockReportService.Setup(s => s.GetReportByIdAsync(TestReportId, _testUserId, false))
            .ReturnsAsync(report);

        // Act
        var cut = Render<ReportView>(parameters => parameters.Add(p => p.ReportId, TestReportId));

        // Assert
        var editButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Editar"));
        var submitButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Entregar"));
        var deleteButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Eliminar"));

        editButton.Should().BeNull();
        submitButton.Should().BeNull();
        deleteButton.Should().BeNull();
    }

    [Fact(DisplayName = "ReportView REVISOR should see Edit button for any report")]
    public void ReportView_Revisor_ShouldShowEditForAnyReport()
    {
        // Arrange
        SetupAuth("REVISOR");
        var otherUserId = Guid.NewGuid();
        var report = CreateTestReport(otherUserId, estado: 1);
        _mockReportService.Setup(s => s.GetReportByIdAsync(TestReportId, _testUserId, true))
            .ReturnsAsync(report);

        // Act
        var cut = Render<ReportView>(parameters => parameters.Add(p => p.ReportId, TestReportId));

        // Assert
        var editButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Editar"));
        editButton.Should().NotBeNull();
    }

    #endregion

    #region Navigation Tests

    [Fact(DisplayName = "ReportView Volver button should navigate back for CREADOR")]
    public async Task ReportView_VolverButton_ShouldNavigateBackCreador()
    {
        // Arrange
        SetupAuth();
        var report = CreateTestReport(_testUserId);
        _mockReportService.Setup(s => s.GetReportByIdAsync(TestReportId, _testUserId, false))
            .ReturnsAsync(report);

        var cut = Render<ReportView>(parameters => parameters.Add(p => p.ReportId, TestReportId));

        // Act
        var volverButton = cut.FindAll("button").First(b => b.TextContent.Contains("Volver"));
        await cut.InvokeAsync(() => volverButton.Click());

        // Assert
        _navigationManager.Uri.Should().Contain("/reports");
    }

    [Fact(DisplayName = "ReportView Volver button should navigate to supervisor for REVISOR")]
    public async Task ReportView_VolverButton_ShouldNavigateBackRevisor()
    {
        // Arrange
        SetupAuth("REVISOR");
        var report = CreateTestReport(_testUserId);
        _mockReportService.Setup(s => s.GetReportByIdAsync(TestReportId, _testUserId, true))
            .ReturnsAsync(report);

        var cut = Render<ReportView>(parameters => parameters.Add(p => p.ReportId, TestReportId));

        // Act
        var volverButton = cut.FindAll("button").First(b => b.TextContent.Contains("Volver"));
        await cut.InvokeAsync(() => volverButton.Click());

        // Assert
        _navigationManager.Uri.Should().Contain("/supervisor/reports");
    }

    [Fact(DisplayName = "ReportView Edit button should navigate to edit page")]
    public async Task ReportView_EditButton_ShouldNavigateToEdit()
    {
        // Arrange
        SetupAuth();
        var report = CreateTestReport(_testUserId, estado: 0);
        _mockReportService.Setup(s => s.GetReportByIdAsync(TestReportId, _testUserId, false))
            .ReturnsAsync(report);

        var cut = Render<ReportView>(parameters => parameters.Add(p => p.ReportId, TestReportId));

        // Act
        var editButton = cut.FindAll("button").First(b => b.TextContent.Contains("Editar"));
        await cut.InvokeAsync(() => editButton.Click());

        // Assert
        _navigationManager.Uri.Should().Contain($"/reports/edit/{TestReportId}");
    }

    #endregion

    #region Submit Report Tests

    [Fact(DisplayName = "ReportView Submit should call service")]
    public async Task ReportView_Submit_ShouldCallService()
    {
        // Arrange
        SetupAuth();
        var report = CreateTestReport(_testUserId, estado: 0);
        _mockReportService.Setup(s => s.GetReportByIdAsync(TestReportId, _testUserId, false))
            .ReturnsAsync(report);
        _mockReportService.Setup(s => s.SubmitReportAsync(TestReportId, _testUserId))
            .ReturnsAsync(CreateTestReport(_testUserId, estado: 1));

        var cut = Render<ReportView>(parameters => parameters.Add(p => p.ReportId, TestReportId));

        // Act
        var submitButton = cut.FindAll("button").First(b => b.TextContent.Contains("Entregar"));
        await cut.InvokeAsync(() => submitButton.Click());

        // Assert
        _mockReportService.Verify(s => s.SubmitReportAsync(TestReportId, _testUserId), Times.Once);
    }

    [Fact(DisplayName = "ReportView Submit should display success message")]
    public async Task ReportView_Submit_ShouldDisplaySuccessMessage()
    {
        // Arrange
        SetupAuth();
        var report = CreateTestReport(_testUserId, estado: 0);
        _mockReportService.Setup(s => s.GetReportByIdAsync(TestReportId, _testUserId, false))
            .ReturnsAsync(report);
        _mockReportService.Setup(s => s.SubmitReportAsync(TestReportId, _testUserId))
            .ReturnsAsync(CreateTestReport(_testUserId, estado: 1));

        var cut = Render<ReportView>(parameters => parameters.Add(p => p.ReportId, TestReportId));

        // Act
        var submitButton = cut.FindAll("button").First(b => b.TextContent.Contains("Entregar"));
        await cut.InvokeAsync(() => submitButton.Click());

        // Assert
        cut.Markup.Should().Contain("alert-success");
        cut.Markup.Should().Contain("exitosamente");
    }

    #endregion

    #region Delete Modal Tests

    [Fact(DisplayName = "ReportView Delete button should show confirmation modal")]
    public async Task ReportView_DeleteButton_ShouldShowModal()
    {
        // Arrange
        SetupAuth();
        var report = CreateTestReport(_testUserId, estado: 0);
        _mockReportService.Setup(s => s.GetReportByIdAsync(TestReportId, _testUserId, false))
            .ReturnsAsync(report);

        var cut = Render<ReportView>(parameters => parameters.Add(p => p.ReportId, TestReportId));

        // Act
        var deleteButton = cut.FindAll("button.btn-outline-danger").First();
        await cut.InvokeAsync(() => deleteButton.Click());

        // Assert
        cut.Markup.Should().Contain("Confirmar Eliminacion");
        cut.Markup.Should().Contain("modal");
    }

    [Fact(DisplayName = "ReportView Delete confirmation should call service")]
    public async Task ReportView_DeleteConfirmation_ShouldCallService()
    {
        // Arrange
        SetupAuth();
        var report = CreateTestReport(_testUserId, estado: 0);
        _mockReportService.Setup(s => s.GetReportByIdAsync(TestReportId, _testUserId, false))
            .ReturnsAsync(report);
        _mockReportService.Setup(s => s.DeleteReportAsync(TestReportId, _testUserId))
            .Returns(Task.CompletedTask);

        var cut = Render<ReportView>(parameters => parameters.Add(p => p.ReportId, TestReportId));

        // Show modal
        var deleteButton = cut.FindAll("button.btn-outline-danger").First();
        await cut.InvokeAsync(() => deleteButton.Click());

        // Act - click confirm delete
        var confirmButton = cut.FindAll("button.btn-danger").First();
        await cut.InvokeAsync(() => confirmButton.Click());

        // Assert
        _mockReportService.Verify(s => s.DeleteReportAsync(TestReportId, _testUserId), Times.Once);
    }

    [Fact(DisplayName = "ReportView Delete cancel should close modal")]
    public async Task ReportView_DeleteCancel_ShouldCloseModal()
    {
        // Arrange
        SetupAuth();
        var report = CreateTestReport(_testUserId, estado: 0);
        _mockReportService.Setup(s => s.GetReportByIdAsync(TestReportId, _testUserId, false))
            .ReturnsAsync(report);

        var cut = Render<ReportView>(parameters => parameters.Add(p => p.ReportId, TestReportId));

        // Show modal
        var deleteButton = cut.FindAll("button.btn-outline-danger").First();
        await cut.InvokeAsync(() => deleteButton.Click());

        // Act - click cancel
        var cancelButton = cut.FindAll("button.btn-secondary").First(b => b.TextContent.Contains("Cancelar"));
        await cut.InvokeAsync(() => cancelButton.Click());

        // Assert - modal should be hidden
        cut.Markup.Should().NotContain("Confirmar Eliminacion");
    }

    #endregion

    #region Helper Methods

    private ReportDto CreateTestReport(Guid usuarioId, int estado = 0)
    {
        return new ReportDto
        {
            Id = TestReportId,
            UsuarioId = usuarioId,
            Delito = "Robo a casa habitacion",
            Estado = estado,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            DatetimeHechos = DateTime.UtcNow.AddDays(-1).AddHours(-2),
            Zona = new CatalogItemDto { Id = 1, Nombre = "Zona Norte" },
            Sector = new CatalogItemDto { Id = 1, Nombre = "Sector 1" },
            Cuadrante = new CatalogItemDto { Id = 1, Nombre = "Cuadrante A" },
            Sexo = "Femenino",
            Edad = 25,
            TurnoCeiba = "Balderas 1",
            TipoDeAtencion = "Presencial",
            TipoDeAccion = "Preventiva",
            TipoReporte = "A",
            HechosReportados = "Descripcion detallada de los hechos ocurridos durante el incidente.",
            AccionesRealizadas = "Se brindo atencion a la victima y se levanto el reporte correspondiente."
        };
    }

    private class FakeNavigationManager : NavigationManager
    {
        public FakeNavigationManager()
        {
            Initialize("https://localhost:5001/", "https://localhost:5001/reports/view/123");
        }

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            if (!uri.StartsWith("http"))
            {
                uri = new Uri(new Uri(BaseUri), uri).ToString();
            }
            Uri = uri;
        }
    }

    private class TestAuthStateProvider : AuthenticationStateProvider
    {
        private readonly AuthenticationState _authState;

        public TestAuthStateProvider(Guid userId, string role)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "testuser@ceiba.local"),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            }, "Test");

            _authState = new AuthenticationState(new ClaimsPrincipal(identity));
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(_authState);
    }

    #endregion
}
