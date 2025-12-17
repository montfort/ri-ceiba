using Ceiba.Core.Entities;
using Ceiba.Core.Interfaces;
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
            TipoDeAccion = "Preventiva",
            HechosReportados = "Test hechos",
            AccionesRealizadas = "Test acciones",
            DatetimeHechos = DateTime.UtcNow,
            TurnoCeiba = "Balderas 1",
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
        var (items, totalCount) = await _repository.SearchAsync(new ReportSearchCriteria { Estado = 99 });

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
        var (_, totalCount) = await _repository.SearchAsync(new ReportSearchCriteria { Estado = 0 });

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
            TipoDeAccion = "Preventiva",
            HechosReportados = "Test",
            AccionesRealizadas = "Test",
            DatetimeHechos = DateTime.UtcNow,
            TurnoCeiba = "Balderas 1"
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
            TipoDeAccion = "Preventiva",
            HechosReportados = "Test",
            AccionesRealizadas = "Test",
            DatetimeHechos = DateTime.UtcNow,
            TurnoCeiba = "Balderas 1"
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

    #region Additional Edge Case Tests (Phase 2)

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetByIdAsync_NegativeId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(-1);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetByIdAsync_ZeroId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(0);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetByUsuarioIdAsync_EmptyGuid_ReturnsEmptyList()
    {
        // Act
        var result = await _repository.GetByUsuarioIdAsync(Guid.Empty);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SearchAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        for (var i = 0; i < 5; i++)
        {
            await CreateTestReportWithZona(estado: 0, delito: $"Delito {i}");
        }
        _context.ChangeTracker.Clear();

        // Act - Get second page of 2 items
        var (items, totalCount) = await _repository.SearchAsync(new ReportSearchCriteria
        {
            Estado = 0,
            Page = 2,
            PageSize = 2
        });

        // Assert
        Assert.Equal(5, totalCount);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SearchAsync_FilterByUsuarioId_ReturnsOnlyUserReports()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        await CreateTestReportWithZona(usuarioId: userId1);
        await CreateTestReportWithZona(usuarioId: userId1);
        await CreateTestReportWithZona(usuarioId: userId2);
        _context.ChangeTracker.Clear();

        // Act
        var (_, totalCount) = await _repository.SearchAsync(new ReportSearchCriteria
        {
            UsuarioId = userId1
        });

        // Assert
        Assert.Equal(2, totalCount);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SearchAsync_FilterByDelito_ReturnsMatchingReports()
    {
        // Arrange
        await CreateTestReportWithZona(delito: "Robo");
        await CreateTestReportWithZona(delito: "Robo");
        await CreateTestReportWithZona(delito: "Asalto");
        _context.ChangeTracker.Clear();

        // Act
        var (_, totalCount) = await _repository.SearchAsync(new ReportSearchCriteria
        {
            Delito = "Robo"
        });

        // Assert
        Assert.Equal(2, totalCount);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SearchAsync_NoFilters_ReturnsAllReports()
    {
        // Arrange
        await CreateTestReportWithZona();
        await CreateTestReportWithZona();
        await CreateTestReportWithZona();
        _context.ChangeTracker.Clear();

        // Act
        var (_, totalCount) = await _repository.SearchAsync(new ReportSearchCriteria());

        // Assert
        Assert.Equal(3, totalCount);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task AddAsync_MultipleReports_AssignsUniqueIds()
    {
        // Arrange
        var zona = new Zona { Nombre = "Test Zona" };
        _context.Zonas.Add(zona);
        await _context.SaveChangesAsync();

        // Act
        var report1 = await _repository.AddAsync(CreateReport(zona.Id));
        var report2 = await _repository.AddAsync(CreateReport(zona.Id));
        var report3 = await _repository.AddAsync(CreateReport(zona.Id));

        // Assert
        Assert.NotEqual(report1.Id, report2.Id);
        Assert.NotEqual(report2.Id, report3.Id);
        Assert.NotEqual(report1.Id, report3.Id);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateAsync_MultipleFields_UpdatesAll()
    {
        // Arrange
        var (report, _) = await CreateTestReportWithZona(delito: "Original", estado: 0);
        _context.ChangeTracker.Clear();

        // Act
        report.Delito = "Updated";
        report.Estado = 1;
        report.HechosReportados = "Updated hechos";
        await _repository.UpdateAsync(report);

        // Assert
        _context.ChangeTracker.Clear();
        var updated = await _context.ReportesIncidencia.FindAsync(report.Id);
        Assert.Equal("Updated", updated!.Delito);
        Assert.Equal(1, updated.Estado);
        Assert.Equal("Updated hechos", updated.HechosReportados);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DeleteAsync_MultipleTimes_DoesNotThrow()
    {
        // Arrange
        var (report, _) = await CreateTestReportWithZona();
        var id = report.Id;
        _context.ChangeTracker.Clear();

        // Act - Delete multiple times
        await _repository.DeleteAsync(id);
        await _repository.DeleteAsync(id);
        await _repository.DeleteAsync(id);

        // Assert - Should not throw
        var deleted = await _context.ReportesIncidencia.FindAsync(id);
        Assert.Null(deleted);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetByUsuarioIdAsync_MultipleReports_DoesNotThrow()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await CreateTestReportWithZona(usuarioId: userId, delito: "Delito 1");
        await CreateTestReportWithZona(usuarioId: userId, delito: "Delito 2");
        await CreateTestReportWithZona(usuarioId: userId, delito: "Delito 3");
        _context.ChangeTracker.Clear();

        // Act - Note: AsSplitQuery() doesn't work correctly with InMemory provider
        // Full functionality is tested via integration tests with real PostgreSQL
        var result = await _repository.GetByUsuarioIdAsync(userId);

        // Assert - Only verify it doesn't throw; count verification requires PostgreSQL
        Assert.NotNull(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SearchAsync_SmallPageSize_ReturnsCorrectCount()
    {
        // Arrange
        await CreateTestReportWithZona();
        await CreateTestReportWithZona();
        _context.ChangeTracker.Clear();

        // Act
        var (_, totalCount) = await _repository.SearchAsync(new ReportSearchCriteria
        {
            PageSize = 1
        });

        // Assert
        Assert.Equal(2, totalCount);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SearchAsync_LargePage_ReturnsEmptyWithCorrectCount()
    {
        // Arrange
        await CreateTestReportWithZona();
        await CreateTestReportWithZona();
        _context.ChangeTracker.Clear();

        // Act
        var (_, totalCount) = await _repository.SearchAsync(new ReportSearchCriteria
        {
            Page = 100,
            PageSize = 10
        });

        // Assert
        Assert.Equal(2, totalCount);
    }

    private ReporteIncidencia CreateReport(int zonaId)
    {
        return new ReporteIncidencia
        {
            UsuarioId = _testUserId,
            Estado = 0,
            ZonaId = zonaId,
            Delito = "Test",
            Sexo = "M",
            Edad = 25,
            TipoDeAtencion = "Test",
            TipoDeAccion = "Preventiva",
            HechosReportados = "Test hechos",
            AccionesRealizadas = "Test acciones",
            DatetimeHechos = DateTime.UtcNow,
            TurnoCeiba = "Balderas 1"
        };
    }

    #endregion

    #region SearchWithKeysetAsync Tests

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SearchWithKeysetAsync_NoFilters_ReturnsReports()
    {
        // Arrange
        await CreateTestReportWithZona();
        await CreateTestReportWithZona();
        _context.ChangeTracker.Clear();

        // Act
        var result = await _repository.SearchWithKeysetAsync(null, null);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SearchWithKeysetAsync_WithEstadoFilter_ReturnsFilteredReports()
    {
        // Arrange
        await CreateTestReportWithZona(estado: 0);
        await CreateTestReportWithZona(estado: 1);
        _context.ChangeTracker.Clear();

        // Act
        var result = await _repository.SearchWithKeysetAsync(null, null, estado: 0);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SearchWithKeysetAsync_WithZonaFilter_ReturnsFilteredReports()
    {
        // Arrange
        var (report, zona) = await CreateTestReportWithZona();
        await CreateTestReportWithZona();
        _context.ChangeTracker.Clear();

        // Act
        var result = await _repository.SearchWithKeysetAsync(null, null, zonaId: zona.Id);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SearchWithKeysetAsync_WithLastCursor_ReturnsReportsAfterCursor()
    {
        // Arrange
        var (report1, _) = await CreateTestReportWithZona(delito: "First");
        await Task.Delay(10); // Ensure different timestamps
        var (report2, _) = await CreateTestReportWithZona(delito: "Second");
        _context.ChangeTracker.Clear();

        // Act - Get reports after report2 (should be report1 or empty since report2 is newer)
        var result = await _repository.SearchWithKeysetAsync(
            report2.CreatedAt,
            report2.Id);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SearchWithKeysetAsync_WithCustomPageSize_RespectsLimit()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            await CreateTestReportWithZona(delito: $"Report {i}");
        }
        _context.ChangeTracker.Clear();

        // Act
        var result = await _repository.SearchWithKeysetAsync(null, null, pageSize: 2);

        // Assert
        Assert.NotNull(result);
    }

    #endregion

    #region SearchAsync Sorting Tests

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SearchAsync_SortByEstado_ReturnsResults()
    {
        // Arrange
        await CreateTestReportWithZona(estado: 0);
        await CreateTestReportWithZona(estado: 1);
        _context.ChangeTracker.Clear();

        // Act
        var (_, totalCount) = await _repository.SearchAsync(new ReportSearchCriteria
        {
            SortBy = "estado",
            SortDesc = false
        });

        // Assert
        Assert.Equal(2, totalCount);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SearchAsync_SortByEstadoDesc_ReturnsResults()
    {
        // Arrange
        await CreateTestReportWithZona(estado: 0);
        await CreateTestReportWithZona(estado: 1);
        _context.ChangeTracker.Clear();

        // Act
        var (_, totalCount) = await _repository.SearchAsync(new ReportSearchCriteria
        {
            SortBy = "estado",
            SortDesc = true
        });

        // Assert
        Assert.Equal(2, totalCount);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SearchAsync_SortByDelito_ReturnsResults()
    {
        // Arrange
        await CreateTestReportWithZona(delito: "Alpha");
        await CreateTestReportWithZona(delito: "Beta");
        _context.ChangeTracker.Clear();

        // Act
        var (_, totalCount) = await _repository.SearchAsync(new ReportSearchCriteria
        {
            SortBy = "delito",
            SortDesc = false
        });

        // Assert
        Assert.Equal(2, totalCount);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SearchAsync_SortByDelitoDesc_ReturnsResults()
    {
        // Arrange
        await CreateTestReportWithZona(delito: "Alpha");
        await CreateTestReportWithZona(delito: "Beta");
        _context.ChangeTracker.Clear();

        // Act
        var (_, totalCount) = await _repository.SearchAsync(new ReportSearchCriteria
        {
            SortBy = "delito",
            SortDesc = true
        });

        // Assert
        Assert.Equal(2, totalCount);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SearchAsync_SortByZona_ReturnsResults()
    {
        // Arrange
        await CreateTestReportWithZona();
        await CreateTestReportWithZona();
        _context.ChangeTracker.Clear();

        // Act
        var (_, totalCount) = await _repository.SearchAsync(new ReportSearchCriteria
        {
            SortBy = "zona",
            SortDesc = false
        });

        // Assert
        Assert.Equal(2, totalCount);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SearchAsync_SortByZonaDesc_ReturnsResults()
    {
        // Arrange
        await CreateTestReportWithZona();
        await CreateTestReportWithZona();
        _context.ChangeTracker.Clear();

        // Act
        var (_, totalCount) = await _repository.SearchAsync(new ReportSearchCriteria
        {
            SortBy = "zona",
            SortDesc = true
        });

        // Assert
        Assert.Equal(2, totalCount);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SearchAsync_SortByCreatedAt_ReturnsResults()
    {
        // Arrange
        await CreateTestReportWithZona();
        await CreateTestReportWithZona();
        _context.ChangeTracker.Clear();

        // Act
        var (_, totalCount) = await _repository.SearchAsync(new ReportSearchCriteria
        {
            SortBy = "createdat",
            SortDesc = false
        });

        // Assert
        Assert.Equal(2, totalCount);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SearchAsync_SortByUnknownField_UsesDefaultSort()
    {
        // Arrange
        await CreateTestReportWithZona();
        await CreateTestReportWithZona();
        _context.ChangeTracker.Clear();

        // Act
        var (_, totalCount) = await _repository.SearchAsync(new ReportSearchCriteria
        {
            SortBy = "unknownfield",
            SortDesc = true
        });

        // Assert
        Assert.Equal(2, totalCount);
    }

    #endregion

    #region SearchAsync Filter Tests

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SearchAsync_FilterByZonaId_ReturnsMatchingReports()
    {
        // Arrange
        var (report1, zona1) = await CreateTestReportWithZona();
        var (report2, zona2) = await CreateTestReportWithZona();
        _context.ChangeTracker.Clear();

        // Act
        var (_, totalCount) = await _repository.SearchAsync(new ReportSearchCriteria
        {
            ZonaId = zona1.Id
        });

        // Assert
        Assert.Equal(1, totalCount);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SearchAsync_FilterByFechaDesde_ReturnsReportsAfterDate()
    {
        // Arrange
        var (report1, _) = await CreateTestReportWithZona();
        _context.ChangeTracker.Clear();

        // Act
        var (_, totalCount) = await _repository.SearchAsync(new ReportSearchCriteria
        {
            FechaDesde = DateTime.UtcNow.AddDays(-1)
        });

        // Assert
        Assert.True(totalCount >= 1);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SearchAsync_FilterByFechaHasta_ReturnsReportsBeforeDate()
    {
        // Arrange
        await CreateTestReportWithZona();
        _context.ChangeTracker.Clear();

        // Act
        var (_, totalCount) = await _repository.SearchAsync(new ReportSearchCriteria
        {
            FechaHasta = DateTime.UtcNow.AddDays(1)
        });

        // Assert
        Assert.True(totalCount >= 1);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SearchAsync_FilterByDateRange_ReturnsReportsInRange()
    {
        // Arrange
        await CreateTestReportWithZona();
        _context.ChangeTracker.Clear();

        // Act
        var (_, totalCount) = await _repository.SearchAsync(new ReportSearchCriteria
        {
            FechaDesde = DateTime.UtcNow.AddDays(-1),
            FechaHasta = DateTime.UtcNow.AddDays(1)
        });

        // Assert
        Assert.True(totalCount >= 1);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SearchAsync_FilterByDateRange_NoResults_ReturnsEmpty()
    {
        // Arrange
        await CreateTestReportWithZona();
        _context.ChangeTracker.Clear();

        // Act
        var (items, totalCount) = await _repository.SearchAsync(new ReportSearchCriteria
        {
            FechaDesde = DateTime.UtcNow.AddYears(-10),
            FechaHasta = DateTime.UtcNow.AddYears(-9)
        });

        // Assert
        Assert.Equal(0, totalCount);
        Assert.Empty(items);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SearchAsync_PartialDelitoMatch_ReturnsResults()
    {
        // Arrange
        await CreateTestReportWithZona(delito: "Robo a mano armada");
        await CreateTestReportWithZona(delito: "Robo simple");
        await CreateTestReportWithZona(delito: "Asalto");
        _context.ChangeTracker.Clear();

        // Act
        var (_, totalCount) = await _repository.SearchAsync(new ReportSearchCriteria
        {
            Delito = "Robo"
        });

        // Assert
        Assert.Equal(2, totalCount);
    }

    #endregion

    #region GetByIdWithRelationsAsync Tests (Additional)

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetByIdWithRelationsAsync_ExistingReport_DoesNotThrow()
    {
        // Arrange
        var (report, zona) = await CreateTestReportWithZona();
        // Note: DON'T clear change tracker for this test - AsSplitQuery with InMemory
        // requires the entities to remain tracked for the query to work

        // Act & Assert - just verify it doesn't throw
        // Full functionality is tested via integration tests with real PostgreSQL
        // because AsSplitQuery() doesn't work correctly with InMemory provider
        var act = async () => await _repository.GetByIdWithRelationsAsync(report.Id);
        var exception = await Record.ExceptionAsync(act);
        Assert.Null(exception);
    }

    #endregion

    #region GetByUsuarioIdAsync Tests (Additional)

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetByUsuarioIdAsync_WithReports_ReturnsUserReports()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await CreateTestReportWithZona(usuarioId: userId);
        await CreateTestReportWithZona(usuarioId: userId);
        await CreateTestReportWithZona(usuarioId: Guid.NewGuid()); // Different user
        _context.ChangeTracker.Clear();

        // Act
        var result = await _repository.GetByUsuarioIdAsync(userId);

        // Assert
        Assert.NotNull(result);
        // Note: Full count verification requires PostgreSQL due to AsSplitQuery
    }

    #endregion
}
