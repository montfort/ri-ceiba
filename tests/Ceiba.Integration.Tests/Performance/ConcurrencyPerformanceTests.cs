using Ceiba.Core.Entities;
using Ceiba.Core.Interfaces;
using Ceiba.Infrastructure.Data;
using Ceiba.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace Ceiba.Integration.Tests.Performance;

/// <summary>
/// T123: Concurrent user performance tests - System must support 50 concurrent users.
/// T124: SLA monitoring - 99.5% availability target.
/// </summary>
[Trait("Category", "Performance")]
public class ConcurrencyPerformanceTests : PerformanceTestBase
{
    private const int TargetConcurrentUsers = 50;
    private const double MaxErrorRatePercent = 0.5; // 99.5% SLA

    public ConcurrencyPerformanceTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    [Trait("NFR", "T123")]
    public async Task SearchReports_With10ConcurrentUsers_MaintainsPerformance()
    {
        // Arrange
        var dbName = $"ConcurrencyTest_{Guid.NewGuid()}";

        // Act
        var stats = await RunConcurrentAsync(
            "Search with 10 concurrent users",
            async userId =>
            {
                await using var context = CreateTestContext(dbName);
                var repository = new ReportRepository(context);
                await repository.SearchAsync(new ReportSearchCriteria { Page = 1, PageSize = 20 });
            },
            concurrentUsers: 10,
            duration: TimeSpan.FromSeconds(5));

        // Assert
        Assert.True(stats.ErrorRate <= MaxErrorRatePercent,
            $"Error rate ({stats.ErrorRate:F2}%) exceeds SLA threshold ({MaxErrorRatePercent}%)");
        Assert.True(stats.P95ResponseMs < 10000,
            $"P95 response time ({stats.P95ResponseMs:F2}ms) exceeds 10s threshold");
    }

    [Fact]
    [Trait("NFR", "T123")]
    public async Task SearchReports_With25ConcurrentUsers_MaintainsPerformance()
    {
        // Arrange
        var dbName = $"ConcurrencyTest_{Guid.NewGuid()}";

        // Act
        var stats = await RunConcurrentAsync(
            "Search with 25 concurrent users",
            async userId =>
            {
                await using var context = CreateTestContext(dbName);
                var repository = new ReportRepository(context);
                await repository.SearchAsync(new ReportSearchCriteria
                {
                    Estado = userId % 3,
                    Page = 1,
                    PageSize = 20
                });
            },
            concurrentUsers: 25,
            duration: TimeSpan.FromSeconds(5));

        // Assert
        Assert.True(stats.ErrorRate <= MaxErrorRatePercent,
            $"Error rate ({stats.ErrorRate:F2}%) exceeds SLA threshold ({MaxErrorRatePercent}%)");
    }

    [Fact]
    [Trait("NFR", "T123")]
    public async Task MixedOperations_With50ConcurrentUsers_MaintainsPerformance()
    {
        // Arrange
        var dbName = $"ConcurrencyTest_{Guid.NewGuid()}";
        SeedSharedTestData(dbName);

        // Act
        var stats = await RunConcurrentAsync(
            "Mixed operations with 50 concurrent users",
            async userId =>
            {
                await using var context = CreateTestContext(dbName);
                var repository = new ReportRepository(context);

                // Mix of operations based on user ID
                var operation = userId % 4;
                switch (operation)
                {
                    case 0:
                        await repository.SearchAsync(new ReportSearchCriteria { Page = 1, PageSize = 20 });
                        break;
                    case 1:
                        await repository.GetByIdAsync((userId % 100) + 1);
                        break;
                    case 2:
                        await repository.GetByIdWithRelationsAsync((userId % 100) + 1);
                        break;
                    case 3:
                        await repository.SearchAsync(new ReportSearchCriteria
                        {
                            Delito = "robo",
                            Page = 1,
                            PageSize = 10
                        });
                        break;
                }
            },
            concurrentUsers: TargetConcurrentUsers,
            duration: TimeSpan.FromSeconds(10));

        // Assert
        Output.WriteLine($"Operations completed: {stats.TotalOperations}");
        Output.WriteLine($"Operations per second: {stats.OperationsPerSecond:F2}");

        Assert.True(stats.ErrorRate <= MaxErrorRatePercent,
            $"Error rate ({stats.ErrorRate:F2}%) exceeds SLA threshold ({MaxErrorRatePercent}%)");
        Assert.True(stats.TotalOperations > 0, "No operations completed");
    }

    [Fact]
    [Trait("NFR", "T124")]
    public async Task SlaMonitoring_ErrorRateBelowThreshold()
    {
        // Arrange
        var dbName = $"SlaTest_{Guid.NewGuid()}";
        SeedSharedTestData(dbName);

        // Act - Run for longer duration to get meaningful SLA metrics
        var stats = await RunConcurrentAsync(
            "SLA monitoring test",
            async userId =>
            {
                await using var context = CreateTestContext(dbName);
                var repository = new ReportRepository(context);
                await repository.SearchAsync(new ReportSearchCriteria { Page = 1, PageSize = 20 });
            },
            concurrentUsers: 20,
            duration: TimeSpan.FromSeconds(15));

        // Assert - 99.5% SLA = max 0.5% error rate
        var successRate = 100 - stats.ErrorRate;
        Output.WriteLine($"Success rate: {successRate:F2}%");
        Output.WriteLine($"SLA target: 99.5%");

        Assert.True(successRate >= 99.5,
            $"Success rate ({successRate:F2}%) is below 99.5% SLA target");
    }

    [Fact]
    [Trait("NFR", "T123")]
    public async Task ReadOperations_HighThroughput()
    {
        // Arrange
        var dbName = $"ThroughputTest_{Guid.NewGuid()}";
        SeedSharedTestData(dbName);

        // Act
        var stats = await RunConcurrentAsync(
            "Read throughput test",
            async userId =>
            {
                await using var context = CreateTestContext(dbName);
                var repository = new ReportRepository(context);
                await repository.GetByIdAsync((userId % 100) + 1);
            },
            concurrentUsers: 30,
            duration: TimeSpan.FromSeconds(5));

        // Assert
        Output.WriteLine($"Throughput: {stats.OperationsPerSecond:F2} ops/sec");
        Assert.True(stats.OperationsPerSecond > 10, // At least 10 ops/sec
            $"Throughput ({stats.OperationsPerSecond:F2} ops/sec) is too low");
    }

    [Fact]
    [Trait("NFR", "T123")]
    public async Task WriteOperations_UnderLoad()
    {
        // Arrange
        var dbName = $"WriteTest_{Guid.NewGuid()}";
        var reportIdCounter = 1000;
        var lockObj = new object();

        // Act
        var stats = await RunConcurrentAsync(
            "Write operations under load",
            async userId =>
            {
                int reportId;
                lock (lockObj)
                {
                    reportId = reportIdCounter++;
                }

                await using var context = CreateTestContext(dbName);
                var repository = new ReportRepository(context);

                var report = new ReporteIncidencia
                {
                    Id = reportId,
                    UsuarioId = Guid.NewGuid(),
                    Estado = 0,
                    Delito = $"Test delito {userId}",
                    HechosReportados = $"Test hechos from user {userId}",
                    DatetimeHechos = DateTime.UtcNow,
                    ZonaId = 1,
                    SectorId = 1,
                    CuadranteId = 1,
                    CreatedAt = DateTime.UtcNow
                };

                await repository.AddAsync(report);
            },
            concurrentUsers: 10,
            duration: TimeSpan.FromSeconds(5));

        // Assert
        Assert.True(stats.ErrorRate <= 5, // Allow slightly higher error rate for writes
            $"Write error rate ({stats.ErrorRate:F2}%) is too high");
    }

    private static CeibaDbContext CreateTestContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<CeibaDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new CeibaDbContext(options, null);
    }

    private static void SeedSharedTestData(string dbName)
    {
        using var context = CreateTestContext(dbName);

        // Check if already seeded
        if (context.Zonas.Any())
            return;

        // Add zones
        context.Zonas.Add(new Zona { Id = 1, Nombre = "Zona Norte", Activo = true });

        // Add sectors
        context.Sectores.Add(new Sector { Id = 1, Nombre = "Sector A", RegionId = 1, Activo = true });

        // Add cuadrantes
        context.Cuadrantes.Add(new Cuadrante { Id = 1, Nombre = "Cuadrante 1", SectorId = 1, Activo = true });

        // Add sample reports
        var userId = Guid.NewGuid();
        for (var i = 1; i <= 100; i++)
        {
            context.ReportesIncidencia.Add(new ReporteIncidencia
            {
                Id = i,
                UsuarioId = userId,
                Estado = (short)(i % 3),
                Delito = $"Delito {i % 10}",
                HechosReportados = $"Hechos del reporte {i}",
                DatetimeHechos = DateTime.UtcNow.AddDays(-i),
                ZonaId = 1,
                SectorId = 1,
                CuadranteId = 1,
                CreatedAt = DateTime.UtcNow.AddDays(-i)
            });
        }

        context.SaveChanges();
    }
}
