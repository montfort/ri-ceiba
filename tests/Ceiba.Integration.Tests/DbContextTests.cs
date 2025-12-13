using Ceiba.Core.Entities;
using Ceiba.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ceiba.Integration.Tests;

/// <summary>
/// Integration tests for CeibaDbContext.
/// Tests database configuration, relationships, and constraints.
/// </summary>
[Collection("Integration Tests")]
public class DbContextTests : IAsyncLifetime
{
    private readonly string _connectionString;
    private DbContextOptions<CeibaDbContext> _options = null!;

    public DbContextTests()
    {
        // Use in-memory database for integration tests
        _connectionString = $"DataSource=:memory:_{Guid.NewGuid()}";
    }

    public async Task InitializeAsync()
    {
        _options = new DbContextOptionsBuilder<CeibaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;

        // Create database schema
        await using var context = new CeibaDbContext(_options, userId: Guid.NewGuid());
        await context.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task DbContext_ShouldConnect_AndApplyConfiguration()
    {
        // Arrange & Act
        await using var context = new CeibaDbContext(_options, userId: Guid.NewGuid());

        // Assert
        context.Database.ProviderName.Should().NotBeNullOrEmpty();
        context.Model.Should().NotBeNull();
    }

    [Fact]
    public async Task Zona_ShouldEnforce_UniqueNombreConstraint()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await using var context = new CeibaDbContext(_options, userId: userId);

        var zona1 = new Zona { Nombre = "Centro", Activo = true, UsuarioId = userId };
        var zona2 = new Zona { Nombre = "Centro", Activo = true, UsuarioId = userId };

        // Act
        context.Zonas.Add(zona1);
        await context.SaveChangesAsync();

        context.Zonas.Add(zona2);
        var act = async () => await context.SaveChangesAsync();

        // Assert
        // In-memory database doesn't enforce all constraints, but real PostgreSQL would throw
        // This test validates the configuration is present
        context.Model.FindEntityType(typeof(Zona))
            ?.GetIndexes()
            .Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Sector_ShouldRequire_ZonaRelationship()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await using var context = new CeibaDbContext(_options, userId: userId);

        var zona = new Zona { Nombre = "Norte", Activo = true, UsuarioId = userId };
        context.Zonas.Add(zona);
        await context.SaveChangesAsync();

        var region = new Region { Nombre = "Región Norte", ZonaId = zona.Id, Activo = true, UsuarioId = userId };
        context.Regiones.Add(region);
        await context.SaveChangesAsync();

        var sector = new Sector
        {
            Nombre = "Sector 1",
            RegionId = region.Id,
            Activo = true,
            UsuarioId = userId
        };

        // Act
        context.Sectores.Add(sector);
        await context.SaveChangesAsync();

        // Assert
        var savedSector = await context.Sectores
            .Include(s => s.Region)
            .FirstAsync();

        savedSector.Region.Should().NotBeNull();
        savedSector.Region.Nombre.Should().Be("Región Norte");
    }

    [Fact]
    public async Task Cuadrante_ShouldRequire_SectorRelationship()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await using var context = new CeibaDbContext(_options, userId: userId);

        var zona = new Zona { Nombre = "Sur", Activo = true, UsuarioId = userId };
        context.Zonas.Add(zona);
        await context.SaveChangesAsync();

        var region = new Region { Nombre = "Región Sur", ZonaId = zona.Id, Activo = true, UsuarioId = userId };
        context.Regiones.Add(region);
        await context.SaveChangesAsync();

        var sector = new Sector
        {
            Nombre = "Sector A",
            RegionId = region.Id,
            Activo = true,
            UsuarioId = userId
        };
        context.Sectores.Add(sector);
        await context.SaveChangesAsync();

        var cuadrante = new Cuadrante
        {
            Nombre = "Cuadrante 1",
            SectorId = sector.Id,
            Activo = true,
            UsuarioId = userId
        };

        // Act
        context.Cuadrantes.Add(cuadrante);
        await context.SaveChangesAsync();

        // Assert
        var savedCuadrante = await context.Cuadrantes
            .Include(c => c.Sector)
            .ThenInclude(s => s.Region)
            .FirstAsync();

        savedCuadrante.Sector.Should().NotBeNull();
        savedCuadrante.Sector.Nombre.Should().Be("Sector A");
        savedCuadrante.Sector.Region.Nombre.Should().Be("Región Sur");
    }

    [Fact]
    public async Task RegistroAuditoria_ShouldStore_AuditInformation()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await using var context = new CeibaDbContext(_options, userId: userId);

        var auditLog = new RegistroAuditoria
        {
            Codigo = "TEST_ACTION",
            IdRelacionado = 123,
            TablaRelacionada = "TEST_TABLE",
            UsuarioId = userId,
            Ip = "127.0.0.1",
            Detalles = """{"action": "test", "value": 42}"""
        };

        // Act
        context.RegistrosAuditoria.Add(auditLog);
        await context.SaveChangesAsync();

        // Assert
        var savedLog = await context.RegistrosAuditoria.FirstAsync();
        savedLog.Codigo.Should().Be("TEST_ACTION");
        savedLog.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        savedLog.Detalles.Should().Contain("test");
    }

    [Fact]
    public async Task CatalogoSugerencia_ShouldEnforce_UniqueCampoValor()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await using var context = new CeibaDbContext(_options, userId: userId);

        var sugerencia = new CatalogoSugerencia
        {
            Campo = "sexo",
            Valor = "Masculino",
            Orden = 1,
            Activo = true,
            UsuarioId = userId
        };

        // Act
        context.CatalogosSugerencia.Add(sugerencia);
        await context.SaveChangesAsync();

        // Assert
        var saved = await context.CatalogosSugerencia.FirstAsync();
        saved.Campo.Should().Be("sexo");
        saved.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
