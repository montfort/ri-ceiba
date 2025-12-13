using Ceiba.Core.Interfaces;
using Ceiba.Infrastructure.Data;
using Ceiba.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Ceiba.Integration.Tests.Performance;

/// <summary>
/// T121: Search performance tests - Search operations must complete in under 10 seconds.
/// </summary>
[Trait("Category", "Performance")]
[Collection("Database")]
public class SearchPerformanceTests : PerformanceTestBase
{
    private static readonly TimeSpan SearchThreshold = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan FastSearchThreshold = TimeSpan.FromSeconds(2);

    public SearchPerformanceTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    [Trait("NFR", "T121")]
    public async Task SearchReports_WithNoFilters_CompletesUnder10Seconds()
    {
        // Arrange
        await using var context = CreateTestContext();
        var repository = new ReportRepository(context);

        // Act & Assert
        await MeasureAsync(
            "Search reports (no filters)",
            async () => await repository.SearchAsync(page: 1, pageSize: 20),
            SearchThreshold);
    }

    [Fact]
    [Trait("NFR", "T121")]
    public async Task SearchReports_WithMultipleFilters_CompletesUnder10Seconds()
    {
        // Arrange
        await using var context = CreateTestContext();
        var repository = new ReportRepository(context);

        // Act & Assert
        await MeasureAsync(
            "Search reports (with filters)",
            async () => await repository.SearchAsync(
                estado: 1,
                zonaId: 1,
                delito: "robo",
                fechaDesde: DateTime.UtcNow.AddMonths(-6),
                fechaHasta: DateTime.UtcNow,
                page: 1,
                pageSize: 20),
            SearchThreshold);
    }

    [Fact]
    [Trait("NFR", "T121")]
    public async Task SearchReports_WithPagination_CompletesUnder2Seconds()
    {
        // Arrange
        await using var context = CreateTestContext();
        var repository = new ReportRepository(context);

        // Act & Assert - Test multiple pages
        for (var page = 1; page <= 5; page++)
        {
            await MeasureAsync(
                $"Search reports (page {page})",
                async () => await repository.SearchAsync(page: page, pageSize: 20),
                FastSearchThreshold);
        }
    }

    [Fact]
    [Trait("NFR", "T121")]
    public async Task SearchReports_WithTextSearch_CompletesUnder10Seconds()
    {
        // Arrange
        await using var context = CreateTestContext();
        var repository = new ReportRepository(context);

        // Act & Assert
        await MeasureAsync(
            "Search reports (text search)",
            async () => await repository.SearchAsync(
                delito: "asalto",
                page: 1,
                pageSize: 20),
            SearchThreshold);
    }

    [Fact]
    [Trait("NFR", "T121")]
    public async Task SearchReports_KeysetPagination_CompletesUnder2Seconds()
    {
        // Arrange
        await using var context = CreateTestContext();
        var repository = new ReportRepository(context);

        // Act & Assert
        await MeasureAsync(
            "Search reports (keyset pagination)",
            async () => await repository.SearchWithKeysetAsync(
                lastCreatedAt: DateTime.UtcNow.AddDays(-30),
                lastId: 100,
                pageSize: 20),
            FastSearchThreshold);
    }

    [Fact]
    [Trait("NFR", "T121")]
    public async Task SearchReports_MultipleIterations_MaintainsPerformance()
    {
        // Arrange
        await using var context = CreateTestContext();
        var repository = new ReportRepository(context);

        // Act
        var stats = await RunIterationsAsync(
            "Search reports (10 iterations)",
            async () =>
            {
                await repository.SearchAsync(
                    estado: 1,
                    page: 1,
                    pageSize: 20);
            },
            iterations: 10,
            warmupDuration: TimeSpan.FromSeconds(1));

        // Assert
        Assert.True(stats.P95Ms < SearchThreshold.TotalMilliseconds,
            $"P95 response time ({stats.P95Ms:F2}ms) exceeds threshold ({SearchThreshold.TotalMilliseconds}ms)");
    }

    [Fact]
    [Trait("NFR", "T121")]
    public async Task GetReportById_CompletesUnder1Second()
    {
        // Arrange
        await using var context = CreateTestContext();
        var repository = new ReportRepository(context);

        // Act & Assert
        await MeasureAsync(
            "Get report by ID",
            async () => await repository.GetByIdAsync(1),
            TimeSpan.FromSeconds(1));
    }

    [Fact]
    [Trait("NFR", "T121")]
    public async Task GetReportWithRelations_CompletesUnder2Seconds()
    {
        // Arrange
        await using var context = CreateTestContext();
        var repository = new ReportRepository(context);

        // Act & Assert
        await MeasureAsync(
            "Get report with relations",
            async () => await repository.GetByIdWithRelationsAsync(1),
            TimeSpan.FromSeconds(2));
    }

    private static CeibaDbContext CreateTestContext()
    {
        var options = new DbContextOptionsBuilder<CeibaDbContext>()
            .UseInMemoryDatabase(databaseName: $"PerfTest_{Guid.NewGuid()}")
            .Options;

        var context = new CeibaDbContext(options, null);

        // Seed some test data for performance testing
        SeedTestData(context);

        return context;
    }

    private static void SeedTestData(CeibaDbContext context)
    {
        // Add zones
        var zona = new Core.Entities.Zona { Id = 1, Nombre = "Zona Norte", Activo = true };
        context.Zonas.Add(zona);

        // Add regions
        var region = new Core.Entities.Region { Id = 1, Nombre = "Región Norte", ZonaId = 1, Activo = true };
        context.Regiones.Add(region);

        // Add sectors
        var sector = new Core.Entities.Sector { Id = 1, Nombre = "Sector Norte", RegionId = 1, Activo = true };
        context.Sectores.Add(sector);

        // Add cuadrantes
        var cuadrante = new Core.Entities.Cuadrante { Id = 1, Nombre = "Cuadrante 1", SectorId = 1, Activo = true };
        context.Cuadrantes.Add(cuadrante);

        // Add sample reports for testing
        var userId = Guid.NewGuid();
        var random = new Random(42); // Fixed seed for reproducibility

        for (var i = 1; i <= 100; i++)
        {
            context.ReportesIncidencia.Add(new Core.Entities.ReporteIncidencia
            {
                Id = i,
                UsuarioId = userId,
                Estado = (short)random.Next(0, 3),
                Delito = GetRandomDelito(random),
                HechosReportados = $"Hechos del reporte {i}",
                DatetimeHechos = DateTime.UtcNow.AddDays(-random.Next(1, 365)),
                ZonaId = 1,
                RegionId = 1,
                SectorId = 1,
                CuadranteId = 1,
                CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 365))
            });
        }

        context.SaveChanges();
    }

    private static string GetRandomDelito(Random random)
    {
        var delitos = new[]
        {
            "Robo a transeúnte", "Robo a casa habitación", "Robo de vehículo",
            "Asalto", "Lesiones", "Daño a propiedad", "Amenazas",
            "Riña", "Portación de arma", "Narcomenudeo"
        };
        return delitos[random.Next(delitos.Length)];
    }
}
