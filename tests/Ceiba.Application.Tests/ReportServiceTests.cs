using Ceiba.Application.Services;
using Ceiba.Core.Entities;
using Ceiba.Core.Exceptions;
using Ceiba.Core.Interfaces;
using Ceiba.Shared.DTOs;
using FluentAssertions;
using Moq;
using System.Text.Json;
using Xunit;

namespace Ceiba.Application.Tests;

/// <summary>
/// Unit tests for ReportService (US1)
/// Tests create/edit functionality per T024
/// </summary>
public class ReportServiceTests
{
    private readonly Mock<IReportRepository> _mockRepository;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<ICatalogService> _mockCatalogService;
    private readonly Mock<IUserManagementService> _mockUserService;
    private readonly ReportService _sut;

    public ReportServiceTests()
    {
        _mockRepository = new Mock<IReportRepository>();
        _mockAuditService = new Mock<IAuditService>();
        _mockCatalogService = new Mock<ICatalogService>();
        _mockUserService = new Mock<IUserManagementService>();
        _sut = new ReportService(
            _mockRepository.Object,
            _mockAuditService.Object,
            _mockCatalogService.Object,
            _mockUserService.Object
        );
    }

    #region T024: Create Report Tests

    [Fact(DisplayName = "T024: CreateReportAsync should create report with estado=0 (Borrador)")]
    public async Task CreateReportAsync_WithValidData_CreatesReportAsBorrador()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();
        var createDto = new CreateReportDto
        {
            TipoReporte = "A",
            DatetimeHechos = DateTime.UtcNow.AddHours(-2),
            Sexo = "Femenino",
            Edad = 28,
            LgbtttiqPlus = false,
            SituacionCalle = false,
            Migrante = false,
            Discapacidad = false,
            Delito = "Violencia familiar",
            ZonaId = 1,
            RegionId = 1,
            SectorId = 1,
            CuadranteId = 1,
            TurnoCeiba = 1,
            TipoDeAtencion = "Presencial",
            TipoDeAccion = 1,
            HechosReportados = "Descripción detallada de los hechos reportados.",
            AccionesRealizadas = "Acciones realizadas por el oficial.",
            Traslados = 0,
            Observaciones = "Observaciones adicionales"
        };

        var savedReport = new ReporteIncidencia
        {
            Id = 1,
            UsuarioId = usuarioId,
            Estado = 0,
            TipoReporte = "A",
            DatetimeHechos = createDto.DatetimeHechos.ToUniversalTime(),
            Sexo = "Femenino",
            Edad = 28,
            Delito = "Violencia familiar",
            ZonaId = 1,
            RegionId = 1,
            SectorId = 1,
            CuadranteId = 1,
            TurnoCeiba = 1,
            TipoDeAtencion = "Presencial",
            TipoDeAccion = 1,
            HechosReportados = "Descripción detallada de los hechos reportados.",
            AccionesRealizadas = "Acciones realizadas por el oficial.",
            Traslados = 0,
            Observaciones = "Observaciones adicionales",
            Zona = new Zona { Id = 1, Nombre = "Zona Centro" },
            Region = new Region { Id = 1, Nombre = "Región Centro", ZonaId = 1 },
            Sector = new Sector { Id = 1, Nombre = "Sector 1", RegionId = 1 },
            Cuadrante = new Cuadrante { Id = 1, Nombre = "Cuadrante 1-A", SectorId = 1 }
        };

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<ReporteIncidencia>()))
            .ReturnsAsync((ReporteIncidencia r) => { r.Id = 1; return r; });

        _mockRepository
            .Setup(r => r.GetByIdWithRelationsAsync(1))
            .ReturnsAsync(savedReport);

        _mockCatalogService
            .Setup(c => c.ValidateHierarchyAsync(1, 1, 1, 1))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.CreateReportAsync(createDto, usuarioId);

        // Assert
        result.Should().NotBeNull();
        result.Estado.Should().Be(0); // Borrador
        result.UsuarioId.Should().Be(usuarioId);
        result.TipoReporte.Should().Be("A");
        result.Sexo.Should().Be("Femenino");
        result.Edad.Should().Be(28);
        result.Delito.Should().Be("Violencia familiar");

        // Verify audit log was created
        _mockAuditService.Verify(
            a => a.LogAsync(
                "REPORT_CREATE",
                It.IsAny<int>(),
                "REPORTE_INCIDENCIA",
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    [Fact(DisplayName = "T024: CreateReportAsync should validate zona-sector-cuadrante hierarchy")]
    public async Task CreateReportAsync_WithInvalidHierarchy_ThrowsValidationException()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();
        var createDto = new CreateReportDto
        {
            TipoReporte = "A",
            DatetimeHechos = DateTime.UtcNow,
            Sexo = "Femenino",
            Edad = 28,
            Delito = "Test",
            ZonaId = 1,
            RegionId = 1,
            SectorId = 99, // Invalid sector not belonging to region 1
            CuadranteId = 1,
            TurnoCeiba = 1,
            TipoDeAtencion = "Presencial",
            TipoDeAccion = 1,
            HechosReportados = "Test",
            AccionesRealizadas = "Test",
            Traslados = 0
        };

        _mockCatalogService
            .Setup(c => c.ValidateHierarchyAsync(1, 1, 99, 1))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _sut.CreateReportAsync(createDto, usuarioId)
        );
    }

    [Fact(DisplayName = "T024: CreateReportAsync should validate edad range (1-149)")]
    public async Task CreateReportAsync_WithInvalidEdad_ThrowsValidationException()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();
        var createDto = new CreateReportDto
        {
            TipoReporte = "A",
            DatetimeHechos = DateTime.UtcNow,
            Sexo = "Femenino",
            Edad = 200, // Invalid edad > 149
            Delito = "Test",
            ZonaId = 1,
            RegionId = 1,
            SectorId = 1,
            CuadranteId = 1,
            TurnoCeiba = 1,
            TipoDeAtencion = "Presencial",
            TipoDeAccion = 1,
            HechosReportados = "Test",
            AccionesRealizadas = "Test",
            Traslados = 0
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _sut.CreateReportAsync(createDto, usuarioId)
        );
    }

    #endregion

    #region T024: Edit Report Tests

    [Fact(DisplayName = "T024: UpdateReportAsync should update report fields")]
    public async Task UpdateReportAsync_WithValidData_UpdatesReport()
    {
        // Arrange
        var reportId = 1;
        var usuarioId = Guid.NewGuid();
        var existingReport = new ReporteIncidencia
        {
            Id = reportId,
            UsuarioId = usuarioId,
            Estado = 0, // Borrador
            TipoReporte = "A",
            DatetimeHechos = DateTime.UtcNow.AddHours(-3),
            Sexo = "Femenino",
            Edad = 28,
            Delito = "Violencia familiar",
            ZonaId = 1,
            RegionId = 1,
            SectorId = 1,
            CuadranteId = 1,
            TurnoCeiba = 1,
            TipoDeAtencion = "Presencial",
            TipoDeAccion = 1,
            HechosReportados = "Original",
            AccionesRealizadas = "Original",
            Traslados = 0
        };

        var updateDto = new UpdateReportDto
        {
            Sexo = "Masculino",
            Edad = 35,
            Delito = "Robo con violencia",
            ZonaId = 1,
            RegionId = 1,
            SectorId = 1,
            CuadranteId = 1,
            TipoDeAccion = 1,
            HechosReportados = "Actualizado con más detalles",
            AccionesRealizadas = "Acciones actualizadas"
        };

        var updatedReport = new ReporteIncidencia
        {
            Id = reportId,
            UsuarioId = usuarioId,
            Estado = 0,
            TipoReporte = "A",
            DatetimeHechos = DateTime.UtcNow.AddHours(-3),
            Sexo = "Masculino",
            Edad = 35,
            Delito = "Robo con violencia",
            ZonaId = 1,
            RegionId = 1,
            SectorId = 1,
            CuadranteId = 1,
            TurnoCeiba = 1,
            TipoDeAtencion = "Presencial",
            TipoDeAccion = 1,
            HechosReportados = "Actualizado con más detalles",
            AccionesRealizadas = "Acciones actualizadas",
            Traslados = 0,
            Zona = new Zona { Id = 1, Nombre = "Zona Centro" },
            Region = new Region { Id = 1, Nombre = "Región Centro", ZonaId = 1 },
            Sector = new Sector { Id = 1, Nombre = "Sector 1", RegionId = 1 },
            Cuadrante = new Cuadrante { Id = 1, Nombre = "Cuadrante 1-A", SectorId = 1 }
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(reportId))
            .ReturnsAsync(existingReport);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<ReporteIncidencia>()))
            .ReturnsAsync((ReporteIncidencia r) => r);

        _mockRepository
            .Setup(r => r.GetByIdWithRelationsAsync(reportId))
            .ReturnsAsync(updatedReport);

        _mockCatalogService
            .Setup(c => c.ValidateHierarchyAsync(1, 1, 1, 1))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.UpdateReportAsync(reportId, updateDto, usuarioId);

        // Assert
        result.Should().NotBeNull();
        result.Sexo.Should().Be("Masculino");
        result.Edad.Should().Be(35);
        result.Delito.Should().Be("Robo con violencia");
        result.HechosReportados.Should().Be("Actualizado con más detalles");
        result.AccionesRealizadas.Should().Be("Acciones actualizadas");

        // Verify audit log
        _mockAuditService.Verify(
            a => a.LogAsync(
                "REPORT_UPDATE",
                reportId,
                "REPORTE_INCIDENCIA",
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    [Fact(DisplayName = "T024: UpdateReportAsync as CREADOR on entregado report should throw")]
    public async Task UpdateReportAsync_AsCreadorOnEntregadoReport_ThrowsForbiddenException()
    {
        // Arrange
        var reportId = 1;
        var usuarioId = Guid.NewGuid();
        var existingReport = new ReporteIncidencia
        {
            Id = reportId,
            UsuarioId = usuarioId,
            Estado = 1, // Entregado - cannot edit
            TipoReporte = "A",
            Sexo = "Femenino",
            Edad = 28
        };

        var updateDto = new UpdateReportDto { Sexo = "Masculino" };

        _mockRepository
            .Setup(r => r.GetByIdAsync(reportId))
            .ReturnsAsync(existingReport);

        // Act & Assert: CREADOR cannot edit submitted reports
        await Assert.ThrowsAsync<ForbiddenException>(
            () => _sut.UpdateReportAsync(reportId, updateDto, usuarioId, isRevisor: false)
        );
    }

    [Fact(DisplayName = "T024: UpdateReportAsync as REVISOR on entregado report should succeed")]
    public async Task UpdateReportAsync_AsRevisorOnEntregadoReport_Succeeds()
    {
        // Arrange
        var reportId = 1;
        var revisorId = Guid.NewGuid();
        var existingReport = new ReporteIncidencia
        {
            Id = reportId,
            UsuarioId = Guid.NewGuid(), // Different user
            Estado = 1, // Entregado
            TipoReporte = "A",
            Sexo = "Femenino",
            Edad = 28
        };

        var updateDto = new UpdateReportDto
        {
            Sexo = "Masculino",
            Edad = 28,
            Delito = "Violencia familiar",
            ZonaId = 1,
            RegionId = 1,
            SectorId = 1,
            CuadranteId = 1,
            TipoDeAccion = 1,
            HechosReportados = "Hechos reportados",
            AccionesRealizadas = "Acciones realizadas",
            Observaciones = "Modificado por supervisor"
        };

        var updatedReport = new ReporteIncidencia
        {
            Id = reportId,
            UsuarioId = existingReport.UsuarioId,
            Estado = 1,
            TipoReporte = "A",
            DatetimeHechos = DateTime.UtcNow,
            Sexo = "Masculino",
            Edad = 28,
            Delito = "Violencia familiar",
            ZonaId = 1,
            RegionId = 1,
            SectorId = 1,
            CuadranteId = 1,
            TurnoCeiba = 1,
            TipoDeAtencion = "Presencial",
            TipoDeAccion = 1,
            HechosReportados = "Hechos reportados",
            AccionesRealizadas = "Acciones realizadas",
            Traslados = 0,
            Observaciones = "Modificado por supervisor",
            Zona = new Zona { Id = 1, Nombre = "Zona Centro" },
            Region = new Region { Id = 1, Nombre = "Región Centro", ZonaId = 1 },
            Sector = new Sector { Id = 1, Nombre = "Sector 1", RegionId = 1 },
            Cuadrante = new Cuadrante { Id = 1, Nombre = "Cuadrante 1-A", SectorId = 1 }
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(reportId))
            .ReturnsAsync(existingReport);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<ReporteIncidencia>()))
            .ReturnsAsync((ReporteIncidencia r) => r);

        _mockRepository
            .Setup(r => r.GetByIdWithRelationsAsync(reportId))
            .ReturnsAsync(updatedReport);

        _mockCatalogService
            .Setup(c => c.ValidateHierarchyAsync(1, 1, 1, 1))
            .ReturnsAsync(true);

        // Act: REVISOR can edit any report
        var result = await _sut.UpdateReportAsync(reportId, updateDto, revisorId, isRevisor: true);

        // Assert
        result.Should().NotBeNull();
        result.Sexo.Should().Be("Masculino");
        result.Observaciones.Should().Be("Modificado por supervisor");
    }

    #endregion

    #region T024: Submit Report Tests

    [Fact(DisplayName = "T024: SubmitReportAsync should change estado from 0 to 1")]
    public async Task SubmitReportAsync_WithValidDraft_ChangesEstadoToEntregado()
    {
        // Arrange
        var reportId = 1;
        var usuarioId = Guid.NewGuid();
        var draftReport = new ReporteIncidencia
        {
            Id = reportId,
            UsuarioId = usuarioId,
            Estado = 0, // Borrador
            TipoReporte = "A",
            Sexo = "Femenino",
            Edad = 28
        };

        var submittedReport = new ReporteIncidencia
        {
            Id = reportId,
            UsuarioId = usuarioId,
            Estado = 1, // Entregado
            TipoReporte = "A",
            DatetimeHechos = DateTime.UtcNow,
            Sexo = "Femenino",
            Edad = 28,
            Delito = "Test",
            ZonaId = 1,
            RegionId = 1,
            SectorId = 1,
            CuadranteId = 1,
            TurnoCeiba = 1,
            TipoDeAtencion = "Presencial",
            TipoDeAccion = 1,
            HechosReportados = "Test hechos",
            AccionesRealizadas = "Test acciones",
            Traslados = 0,
            Zona = new Zona { Id = 1, Nombre = "Zona Centro" },
            Region = new Region { Id = 1, Nombre = "Región Centro", ZonaId = 1 },
            Sector = new Sector { Id = 1, Nombre = "Sector 1", RegionId = 1 },
            Cuadrante = new Cuadrante { Id = 1, Nombre = "Cuadrante 1-A", SectorId = 1 }
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(reportId))
            .ReturnsAsync(draftReport);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<ReporteIncidencia>()))
            .ReturnsAsync((ReporteIncidencia r) => { r.Estado = 1; return r; });

        _mockRepository
            .Setup(r => r.GetByIdWithRelationsAsync(reportId))
            .ReturnsAsync(submittedReport);

        // Act
        var result = await _sut.SubmitReportAsync(reportId, usuarioId);

        // Assert
        result.Should().NotBeNull();
        result.Estado.Should().Be(1); // Entregado

        // Verify audit log
        _mockAuditService.Verify(
            a => a.LogAsync(
                "REPORT_SUBMIT",
                reportId,
                "REPORTE_INCIDENCIA",
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    [Fact(DisplayName = "T024: SubmitReportAsync on already submitted report should throw")]
    public async Task SubmitReportAsync_OnAlreadySubmittedReport_ThrowsForbiddenException()
    {
        // Arrange
        var reportId = 1;
        var usuarioId = Guid.NewGuid();
        var submittedReport = new ReporteIncidencia
        {
            Id = reportId,
            UsuarioId = usuarioId,
            Estado = 1 // Already entregado
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(reportId))
            .ReturnsAsync(submittedReport);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(
            () => _sut.SubmitReportAsync(reportId, usuarioId)
        );
    }

    [Fact(DisplayName = "T024: SubmitReportAsync on another user's report should throw")]
    public async Task SubmitReportAsync_OnOtherUsersReport_ThrowsForbiddenException()
    {
        // Arrange
        var reportId = 1;
        var ownerUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var report = new ReporteIncidencia
        {
            Id = reportId,
            UsuarioId = ownerUserId, // Different user
            Estado = 0
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(reportId))
            .ReturnsAsync(report);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(
            () => _sut.SubmitReportAsync(reportId, otherUserId)
        );
    }

    #endregion
}
