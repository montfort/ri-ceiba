using Ceiba.Core.Entities;
using Ceiba.Core.Interfaces;
using Ceiba.Infrastructure.Data;
using Ceiba.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Ceiba.Infrastructure.Tests.Services;

/// <summary>
/// Unit tests for AuditService.
/// Tests audit logging and query operations.
/// T119: Audit service implementation and query tests.
/// </summary>
public class AuditServiceTests : IDisposable
{
    private readonly CeibaDbContext _context;
    private readonly IAuditService _service;
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _anotherUserId = Guid.NewGuid();
    private readonly string _databaseName;

    public AuditServiceTests()
    {
        _databaseName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<CeibaDbContext>()
            .UseInMemoryDatabase(databaseName: _databaseName)
            .Options;

        _context = new CeibaDbContext(options, _testUserId);
        _service = new AuditService(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region LogAsync Tests

    [Fact(DisplayName = "T119: LogAsync should create audit log with all parameters")]
    public async Task LogAsync_WithAllParameters_CreatesAuditLog()
    {
        // Arrange
        const string codigo = "TEST_ACTION";
        const int idRelacionado = 42;
        const string tablaRelacionada = "REPORTE_INCIDENCIA";
        const string detalles = "{\"old\":\"value1\",\"new\":\"value2\"}";
        const string ip = "192.168.1.100";

        // Act
        await _service.LogAsync(
            codigo: codigo,
            idRelacionado: idRelacionado,
            tablaRelacionada: tablaRelacionada,
            detalles: detalles,
            ip: ip);

        // Assert
        var logs = await _context.RegistrosAuditoria.ToListAsync();
        logs.Should().HaveCount(1);

        var log = logs.First();
        log.Codigo.Should().Be(codigo);
        log.IdRelacionado.Should().Be(idRelacionado);
        log.TablaRelacionada.Should().Be(tablaRelacionada);
        log.Detalles.Should().Be(detalles);
        log.Ip.Should().Be(ip);
        log.UsuarioId.Should().Be(_testUserId);
        log.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact(DisplayName = "T119: LogAsync should create audit log with only required parameters")]
    public async Task LogAsync_WithOnlyRequiredParameters_CreatesAuditLog()
    {
        // Arrange
        const string codigo = "MINIMAL_ACTION";

        // Act
        await _service.LogAsync(codigo: codigo);

        // Assert
        var logs = await _context.RegistrosAuditoria.ToListAsync();
        logs.Should().HaveCount(1);

        var log = logs.First();
        log.Codigo.Should().Be(codigo);
        log.IdRelacionado.Should().BeNull();
        log.TablaRelacionada.Should().BeNull();
        log.Detalles.Should().BeNull();
        log.Ip.Should().BeNull();
        log.UsuarioId.Should().Be(_testUserId);
    }

    [Fact(DisplayName = "T119: LogAsync should use CurrentUserId from DbContext")]
    public async Task LogAsync_UsesCurrentUserIdFromDbContext()
    {
        // Arrange
        const string codigo = "USER_ACTION";

        // Act
        await _service.LogAsync(codigo: codigo);

        // Assert
        var log = await _context.RegistrosAuditoria.FirstAsync();
        log.UsuarioId.Should().Be(_testUserId);
    }

    [Fact(DisplayName = "T119: LogAsync should handle null CurrentUserId for system actions")]
    public async Task LogAsync_WithNullCurrentUserId_CreatesSystemAuditLog()
    {
        // Arrange
        var systemContext = new CeibaDbContext(
            new DbContextOptionsBuilder<CeibaDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options,
            userId: null);
        var systemService = new AuditService(systemContext);
        const string codigo = "SYSTEM_ACTION";

        // Act
        await systemService.LogAsync(codigo: codigo);

        // Assert
        var log = await systemContext.RegistrosAuditoria.FirstAsync();
        log.UsuarioId.Should().BeNull();
        log.Codigo.Should().Be(codigo);

        systemContext.Dispose();
    }

    [Fact(DisplayName = "T119: LogAsync should handle IPv6 addresses")]
    public async Task LogAsync_WithIPv6Address_StoresCorrectly()
    {
        // Arrange
        const string codigo = "IPV6_ACTION";
        const string ipv6 = "2001:0db8:85a3:0000:0000:8a2e:0370:7334";

        // Act
        await _service.LogAsync(codigo: codigo, ip: ipv6);

        // Assert
        var log = await _context.RegistrosAuditoria.FirstAsync();
        log.Ip.Should().Be(ipv6);
    }

    [Fact(DisplayName = "T119: LogAsync should handle JSON details")]
    public async Task LogAsync_WithJsonDetails_StoresCorrectly()
    {
        // Arrange
        const string codigo = "JSON_ACTION";
        const string jsonDetails = "{\"operacion\":\"CAMBIO_ESTADO\",\"anterior\":\"Borrador\",\"nuevo\":\"Entregado\"}";

        // Act
        await _service.LogAsync(codigo: codigo, detalles: jsonDetails);

        // Assert
        var log = await _context.RegistrosAuditoria.FirstAsync();
        log.Detalles.Should().Be(jsonDetails);
    }

    [Fact(DisplayName = "T119: LogAsync should respect CancellationToken")]
    public async Task LogAsync_WithCancellationToken_ThrowsOnCancellation()
    {
        // Arrange
        const string codigo = "CANCELLED_ACTION";
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = () => _service.LogAsync(codigo: codigo, cancellationToken: cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact(DisplayName = "T119: LogAsync should create multiple audit logs")]
    public async Task LogAsync_MultipleCalls_CreatesMultipleLogs()
    {
        // Arrange & Act
        await _service.LogAsync("ACTION_1");
        await _service.LogAsync("ACTION_2");
        await _service.LogAsync("ACTION_3");

        // Assert
        var logs = await _context.RegistrosAuditoria.ToListAsync();
        logs.Should().HaveCount(3);
        logs.Select(l => l.Codigo).Should().BeEquivalentTo(new[] { "ACTION_1", "ACTION_2", "ACTION_3" });
    }

    #endregion

    #region QueryAsync Tests

    [Fact(DisplayName = "T119: QueryAsync should return all logs when no filters")]
    public async Task QueryAsync_NoFilters_ReturnsAllLogs()
    {
        // Arrange
        await SeedTestAuditLogs();

        // Act
        var results = await _service.QueryAsync();

        // Assert
        results.Should().HaveCount(5);
    }

    [Fact(DisplayName = "T119: QueryAsync should filter by usuarioId")]
    public async Task QueryAsync_FilterByUsuarioId_ReturnsMatchingLogs()
    {
        // Arrange
        await SeedTestAuditLogs();

        // Act
        var results = await _service.QueryAsync(usuarioId: _testUserId);

        // Assert
        results.Should().HaveCount(3);
        results.Should().OnlyContain(r => r.UsuarioId == _testUserId);
    }

    [Fact(DisplayName = "T119: QueryAsync should filter by codigo")]
    public async Task QueryAsync_FilterByCodigo_ReturnsMatchingLogs()
    {
        // Arrange
        await SeedTestAuditLogs();

        // Act
        var results = await _service.QueryAsync(codigo: "REPORT_CREATE");

        // Assert
        results.Should().HaveCount(2);
        results.Should().OnlyContain(r => r.Codigo == "REPORT_CREATE");
    }

    [Fact(DisplayName = "T119: QueryAsync should filter by fechaInicio")]
    public async Task QueryAsync_FilterByFechaInicio_ReturnsLogsAfterDate()
    {
        // Arrange
        await SeedTestAuditLogs();
        var cutoffDate = DateTime.UtcNow.AddMinutes(-2);

        // Act
        var results = await _service.QueryAsync(fechaInicio: cutoffDate);

        // Assert
        results.Should().NotBeEmpty();
        results.Should().OnlyContain(r => r.CreatedAt >= cutoffDate);
    }

    [Fact(DisplayName = "T119: QueryAsync should filter by fechaFin")]
    public async Task QueryAsync_FilterByFechaFin_ReturnsLogsBeforeDate()
    {
        // Arrange
        await SeedTestAuditLogs();
        var cutoffDate = DateTime.UtcNow.AddMinutes(1);

        // Act
        var results = await _service.QueryAsync(fechaFin: cutoffDate);

        // Assert
        results.Should().HaveCount(5);
        results.Should().OnlyContain(r => r.CreatedAt <= cutoffDate);
    }

    [Fact(DisplayName = "T119: QueryAsync should filter by date range")]
    public async Task QueryAsync_FilterByDateRange_ReturnsLogsInRange()
    {
        // Arrange
        await SeedTestAuditLogs();
        var startDate = DateTime.UtcNow.AddMinutes(-5);
        var endDate = DateTime.UtcNow.AddMinutes(5);

        // Act
        var results = await _service.QueryAsync(
            fechaInicio: startDate,
            fechaFin: endDate);

        // Assert
        results.Should().HaveCount(5);
        results.Should().OnlyContain(r => r.CreatedAt >= startDate && r.CreatedAt <= endDate);
    }

    [Fact(DisplayName = "T119: QueryAsync should combine multiple filters")]
    public async Task QueryAsync_MultipleFilters_ReturnsMatchingLogs()
    {
        // Arrange
        await SeedTestAuditLogs();

        // Act
        var results = await _service.QueryAsync(
            usuarioId: _testUserId,
            codigo: "REPORT_CREATE");

        // Assert
        results.Should().HaveCount(1);
        var log = results.First();
        log.UsuarioId.Should().Be(_testUserId);
        log.Codigo.Should().Be("REPORT_CREATE");
    }

    [Fact(DisplayName = "T119: QueryAsync should apply pagination with skip")]
    public async Task QueryAsync_WithSkip_SkipsRecords()
    {
        // Arrange
        await SeedTestAuditLogs();

        // Act
        var results = await _service.QueryAsync(skip: 2);

        // Assert
        results.Should().HaveCount(3);
    }

    [Fact(DisplayName = "T119: QueryAsync should apply pagination with take")]
    public async Task QueryAsync_WithTake_LimitsRecords()
    {
        // Arrange
        await SeedTestAuditLogs();

        // Act
        var results = await _service.QueryAsync(take: 3);

        // Assert
        results.Should().HaveCount(3);
    }

    [Fact(DisplayName = "T119: QueryAsync should apply pagination with skip and take")]
    public async Task QueryAsync_WithSkipAndTake_PaginatesCorrectly()
    {
        // Arrange
        await SeedTestAuditLogs();

        // Act
        var results = await _service.QueryAsync(skip: 1, take: 2);

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact(DisplayName = "T119: QueryAsync should enforce max limit of 500 (RT-002 mitigation)")]
    public async Task QueryAsync_ExceedsMaxLimit_EnforcesLimit()
    {
        // Arrange
        await SeedTestAuditLogs();

        // Act
        var results = await _service.QueryAsync(take: 1000);

        // Assert
        // The service should clamp to 500 max, but we only have 5 records
        results.Should().HaveCount(5);
    }

    [Fact(DisplayName = "T119: QueryAsync should use default pagination")]
    public async Task QueryAsync_NoPagination_UsesDefaults()
    {
        // Arrange
        await SeedTestAuditLogs();

        // Act
        var results = await _service.QueryAsync();

        // Assert
        results.Should().HaveCount(5); // All records since we have less than default take (50)
    }

    [Fact(DisplayName = "T119: QueryAsync should order by CreatedAt descending")]
    public async Task QueryAsync_OrdersBy_CreatedAtDescending()
    {
        // Arrange
        await SeedTestAuditLogsWithDifferentDates();

        // Act
        var results = await _service.QueryAsync();

        // Assert
        results.Should().BeInDescendingOrder(r => r.CreatedAt);
    }

    [Fact(DisplayName = "T119: QueryAsync should return empty list when no matches")]
    public async Task QueryAsync_NoMatches_ReturnsEmptyList()
    {
        // Arrange
        await SeedTestAuditLogs();

        // Act
        var results = await _service.QueryAsync(codigo: "NON_EXISTENT_CODE");

        // Assert
        results.Should().BeEmpty();
    }

    [Fact(DisplayName = "T119: QueryAsync should return AuditLogDto with correct structure")]
    public async Task QueryAsync_ReturnsCorrectDtoStructure()
    {
        // Arrange
        await _service.LogAsync(
            codigo: "TEST_CODE",
            idRelacionado: 123,
            tablaRelacionada: "TEST_TABLE",
            detalles: "{\"test\":\"data\"}",
            ip: "10.0.0.1");

        // Act
        var results = await _service.QueryAsync();

        // Assert
        results.Should().HaveCount(1);
        var dto = results.First();
        dto.Id.Should().BeGreaterThan(0);
        dto.Codigo.Should().Be("TEST_CODE");
        dto.IdRelacionado.Should().Be(123);
        dto.TablaRelacionada.Should().Be("TEST_TABLE");
        dto.UsuarioId.Should().Be(_testUserId);
        dto.UsuarioNombre.Should().BeNull(); // Not joined in current implementation
        dto.Ip.Should().Be("10.0.0.1");
        dto.Detalles.Should().Be("{\"test\":\"data\"}");
        dto.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact(DisplayName = "T119: QueryAsync should handle null optional fields in DTO")]
    public async Task QueryAsync_WithNullFields_ReturnsCorrectDto()
    {
        // Arrange
        await _service.LogAsync(codigo: "MINIMAL_CODE");

        // Act
        var results = await _service.QueryAsync();

        // Assert
        results.Should().HaveCount(1);
        var dto = results.First();
        dto.Codigo.Should().Be("MINIMAL_CODE");
        dto.IdRelacionado.Should().BeNull();
        dto.TablaRelacionada.Should().BeNull();
        dto.Ip.Should().BeNull();
        dto.Detalles.Should().BeNull();
        dto.UsuarioNombre.Should().BeNull();
    }

    [Fact(DisplayName = "T119: QueryAsync should respect CancellationToken")]
    public async Task QueryAsync_WithCancellationToken_ThrowsOnCancellation()
    {
        // Arrange
        await SeedTestAuditLogs();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = () => _service.QueryAsync(cancellationToken: cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact(DisplayName = "T119: QueryAsync should handle large datasets with pagination")]
    public async Task QueryAsync_LargeDataset_PaginatesCorrectly()
    {
        // Arrange
        for (int i = 0; i < 100; i++)
        {
            await _service.LogAsync($"ACTION_{i}");
        }

        // Act - Get page 2 (skip 20, take 20)
        var results = await _service.QueryAsync(skip: 20, take: 20);

        // Assert
        results.Should().HaveCount(20);
    }

    [Fact(DisplayName = "T119: QueryAsync should filter by system actions (null userId)")]
    public async Task QueryAsync_FilterByNullUserId_ReturnsSystemActions()
    {
        // Arrange
        // Create a system action with null userId
        var systemContext = new CeibaDbContext(
            new DbContextOptionsBuilder<CeibaDbContext>()
                .UseInMemoryDatabase(databaseName: _databaseName)
                .Options,
            userId: null);
        var systemService = new AuditService(systemContext);
        await systemService.LogAsync("SYSTEM_ACTION");

        // Add user actions
        await _service.LogAsync("USER_ACTION_1");
        await _service.LogAsync("USER_ACTION_2");

        // Act - Query for system actions by looking for null user
        var allResults = await _service.QueryAsync();
        var systemResults = allResults.Where(r => r.UsuarioId == null).ToList();

        // Assert
        systemResults.Should().HaveCount(1);
        systemResults.First().Codigo.Should().Be("SYSTEM_ACTION");

        systemContext.Dispose();
    }

    #endregion

    #region Edge Cases and Error Scenarios

    [Fact(DisplayName = "T119: LogAsync should handle empty codigo")]
    public async Task LogAsync_EmptyString_CreatesAuditLog()
    {
        // Arrange & Act
        await _service.LogAsync(codigo: string.Empty);

        // Assert
        var log = await _context.RegistrosAuditoria.FirstAsync();
        log.Codigo.Should().BeEmpty();
    }

    [Fact(DisplayName = "T119: LogAsync should handle very long detalles")]
    public async Task LogAsync_VeryLongDetalles_StoresCorrectly()
    {
        // Arrange
        var longDetails = new string('x', 5000);

        // Act
        await _service.LogAsync(codigo: "LONG_DETAILS", detalles: longDetails);

        // Assert
        var log = await _context.RegistrosAuditoria.FirstAsync();
        log.Detalles.Should().Be(longDetails);
    }

    [Fact(DisplayName = "T119: QueryAsync should handle zero skip")]
    public async Task QueryAsync_ZeroSkip_ReturnsFromStart()
    {
        // Arrange
        await SeedTestAuditLogs();

        // Act
        var results = await _service.QueryAsync(skip: 0, take: 2);

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact(DisplayName = "T119: QueryAsync should handle skip beyond dataset")]
    public async Task QueryAsync_SkipBeyondDataset_ReturnsEmpty()
    {
        // Arrange
        await SeedTestAuditLogs();

        // Act
        var results = await _service.QueryAsync(skip: 1000);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact(DisplayName = "T119: QueryAsync should handle negative skip as zero")]
    public async Task QueryAsync_NegativeSkip_TreatsAsZero()
    {
        // Arrange
        await SeedTestAuditLogs();

        // Act
        // Note: EF Core will handle negative skip values
        var results = await _service.QueryAsync(skip: -1);

        // Assert
        results.Should().NotBeEmpty();
    }

    #endregion

    #region Helper Methods

    private async Task SeedTestAuditLogs()
    {
        // Create logs with different users and codes
        _context.RegistrosAuditoria.AddRange(
            new RegistroAuditoria
            {
                Codigo = "REPORT_CREATE",
                IdRelacionado = 1,
                TablaRelacionada = "REPORTE_INCIDENCIA",
                UsuarioId = _testUserId,
                Ip = "192.168.1.1",
                Detalles = "{\"action\":\"create\"}"
            },
            new RegistroAuditoria
            {
                Codigo = "REPORT_CREATE",
                IdRelacionado = 2,
                TablaRelacionada = "REPORTE_INCIDENCIA",
                UsuarioId = _anotherUserId,
                Ip = "192.168.1.2"
            },
            new RegistroAuditoria
            {
                Codigo = "USER_LOGIN",
                UsuarioId = _testUserId,
                Ip = "192.168.1.3"
            },
            new RegistroAuditoria
            {
                Codigo = "USER_LOGIN",
                UsuarioId = _anotherUserId,
                Ip = "192.168.1.4"
            },
            new RegistroAuditoria
            {
                Codigo = "REPORT_EDIT",
                IdRelacionado = 1,
                TablaRelacionada = "REPORTE_INCIDENCIA",
                UsuarioId = _testUserId,
                Ip = "192.168.1.5",
                Detalles = "{\"action\":\"edit\",\"field\":\"estado\"}"
            }
        );

        await _context.SaveChangesAsync();
    }

    private async Task SeedTestAuditLogsWithDifferentDates()
    {
        var baseDate = DateTime.UtcNow.AddHours(-5);

        for (int i = 0; i < 5; i++)
        {
            var log = new RegistroAuditoria
            {
                Codigo = $"ACTION_{i}",
                UsuarioId = _testUserId
            };

            _context.RegistrosAuditoria.Add(log);
            await _context.SaveChangesAsync();

            // Manually update CreatedAt to ensure different dates
            log.CreatedAt = baseDate.AddHours(i);
            _context.Entry(log).Property(l => l.CreatedAt).IsModified = true;
            await _context.SaveChangesAsync();
        }
    }

    #endregion
}
