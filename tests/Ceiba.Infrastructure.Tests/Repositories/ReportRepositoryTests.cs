using Ceiba.Core.Entities;
using Ceiba.Infrastructure.Data;
using Ceiba.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ceiba.Infrastructure.Tests.Repositories;

/// <summary>
/// Unit tests for ReportRepository.
/// Tests basic CRUD operations. Note: Tests using AsSplitQuery and AsNoTracking
/// are limited due to EF Core InMemory provider limitations.
/// Full repository functionality is tested via integration tests.
/// </summary>
public class ReportRepositoryTests : IDisposable
{
    private readonly CeibaDbContext _context;
    private readonly ReportRepository _repository;
    private readonly Guid _testUserId = Guid.NewGuid();

    public ReportRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<CeibaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CeibaDbContext(options);
        _repository = new ReportRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private async Task<(ReporteIncidencia Report, Zona Zona)> CreateTestReportWithZona(
        Guid? usuarioId = null,
        short estado = 0,
        string delito = "Robo")
    {
        var zona = new Zona { Nombre = "Test Zona" };
        _context.Zonas.Add(zona);
        await _context.SaveChangesAsync();

        var report = new ReporteIncidencia
        {
            UsuarioId = usuarioId ?? _testUserId,
            Estado = estado,
            ZonaId = zona.Id,
            Delito = delito,
            Sexo = "M",
            Edad = 30,
            TipoDeAtencion = "Inmediata",
            TipoDeAccion = (short)1,
            HechosReportados = "Test hechos",
            AccionesRealizadas = "Test acciones",
            DatetimeHechos = DateTime.UtcNow,
            TurnoCeiba = (short)1,
            CreatedAt = DateTime.UtcNow
        };

        _context.ReportesIncidencia.Add(report);
        await _context.SaveChangesAsync();
        return (report, zona);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_NonExistingReport_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(99999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingReport_ReturnsReport()
    {
        // Arrange
        var (report, _) = await CreateTestReportWithZona();
        _context.ChangeTracker.Clear();

        // Act
        var result = await _repository.GetByIdAsync(report.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(report.Id, result.Id);
        Assert.Equal(report.Delito, result.Delito);
    }

    #endregion

    #region GetByIdWithRelationsAsync Tests

    [Fact]
    public async Task GetByIdWithRelationsAsync_NonExistingReport_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdWithRelationsAsync(99999);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetByUsuarioIdAsync Tests

    [Fact]
    public async Task GetByUsuarioIdAsync_NoReports_ReturnsEmptyList()
    {
        // Act
        var result = await _repository.GetByUsuarioIdAsync(Guid.NewGuid());

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region SearchAsync Tests

    [Fact]
    public async Task SearchAsync_NoResults_ReturnsEmptyListAndZeroCount()
    {
        // Arrange
        await CreateTestReportWithZona(estado: 0);
        _context.ChangeTracker.Clear();

        // Act - Search for non-existent estado
        var (items, totalCount) = await _repository.SearchAsync(estado: 99);

        // Assert
        Assert.Equal(0, totalCount);
        Assert.Empty(items);
    }

    [Fact]
    public async Task SearchAsync_FilterByEstado_CountsCorrectly()
    {
        // Arrange
        await CreateTestReportWithZona(estado: 0);
        await CreateTestReportWithZona(estado: 0);
        await CreateTestReportWithZona(estado: 1);
        _context.ChangeTracker.Clear();

        // Act
        var (_, totalCount) = await _repository.SearchAsync(estado: 0);

        // Assert - Count works even if items don't due to InMemory limitations
        Assert.Equal(2, totalCount);
    }

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_ValidReport_AddsAndReturnsReport()
    {
        // Arrange
        var zona = new Zona { Nombre = "Test Zona" };
        _context.Zonas.Add(zona);
        await _context.SaveChangesAsync();

        var report = new ReporteIncidencia
        {
            UsuarioId = _testUserId,
            Estado = 0,
            ZonaId = zona.Id,
            Delito = "Test",
            Sexo = "M",
            Edad = 25,
            TipoDeAtencion = "Test",
            TipoDeAccion = (short)1,
            HechosReportados = "Test",
            AccionesRealizadas = "Test",
            DatetimeHechos = DateTime.UtcNow,
            TurnoCeiba = (short)1
        };

        // Act
        var result = await _repository.AddAsync(report);

        // Assert
        Assert.True(result.Id > 0);
        var count = await _context.ReportesIncidencia.CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task AddAsync_SetsIdOnNewReport()
    {
        // Arrange
        var zona = new Zona { Nombre = "Test Zona" };
        _context.Zonas.Add(zona);
        await _context.SaveChangesAsync();

        var report = new ReporteIncidencia
        {
            UsuarioId = _testUserId,
            ZonaId = zona.Id,
            Delito = "Test",
            Sexo = "M",
            Edad = 25,
            TipoDeAtencion = "Test",
            TipoDeAccion = (short)1,
            HechosReportados = "Test",
            AccionesRealizadas = "Test",
            DatetimeHechos = DateTime.UtcNow,
            TurnoCeiba = (short)1
        };
        var originalId = report.Id;

        // Act
        var result = await _repository.AddAsync(report);

        // Assert
        Assert.NotEqual(originalId, result.Id);
        Assert.True(result.Id > 0);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ExistingReport_UpdatesReport()
    {
        // Arrange
        var (report, _) = await CreateTestReportWithZona(delito: "Original");

        // Update directly (entity is tracked)
        report.Delito = "Updated";

        // Act
        var result = await _repository.UpdateAsync(report);

        // Assert
        _context.ChangeTracker.Clear();
        var updatedReport = await _context.ReportesIncidencia.FindAsync(report.Id);
        Assert.Equal("Updated", updatedReport!.Delito);
    }

    [Fact]
    public async Task UpdateAsync_DetachedEntity_AttachesAndUpdates()
    {
        // Arrange
        var (report, _) = await CreateTestReportWithZona(delito: "Original");
        _context.ChangeTracker.Clear();

        report.Delito = "Updated from detached";

        // Act
        var result = await _repository.UpdateAsync(report);

        // Assert
        _context.ChangeTracker.Clear();
        var updatedReport = await _context.ReportesIncidencia.FindAsync(report.Id);
        Assert.Equal("Updated from detached", updatedReport!.Delito);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_NonExistingReport_DoesNothing()
    {
        // Arrange
        var countBefore = await _context.ReportesIncidencia.CountAsync();

        // Act - Should not throw
        await _repository.DeleteAsync(99999);

        // Assert
        var countAfter = await _context.ReportesIncidencia.CountAsync();
        Assert.Equal(countBefore, countAfter);
    }

    [Fact]
    public async Task DeleteAsync_ExistingReport_DeletesReport()
    {
        // Arrange
        var (report, _) = await CreateTestReportWithZona();
        var id = report.Id;
        _context.ChangeTracker.Clear();

        // Act
        await _repository.DeleteAsync(id);

        // Assert
        var deletedReport = await _context.ReportesIncidencia.FindAsync(id);
        Assert.Null(deletedReport);
    }

    #endregion
}
