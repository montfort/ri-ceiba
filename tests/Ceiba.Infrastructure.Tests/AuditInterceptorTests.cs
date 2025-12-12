using Ceiba.Core.Entities;
using Ceiba.Core.Enums;
using Ceiba.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ceiba.Infrastructure.Tests;

/// <summary>
/// Unit tests for audit logging save changes interceptor.
/// Tests audit trail creation for all entity modifications.
/// </summary>
public class AuditInterceptorTests
{
    [Fact]
    public async Task SaveChangesAsync_WithAddedEntity_ShouldCreateAuditLog()
    {
        // Arrange
        var interceptor = new AuditSaveChangesInterceptor();
        var options = new DbContextOptionsBuilder<CeibaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        await using var context = new CeibaDbContext(options, userId: Guid.NewGuid());

        var zona = new Zona
        {
            Nombre = "Zona Test",
            Activo = true,
            UsuarioId = context.CurrentUserId ?? Guid.NewGuid()
        };

        // Act
        context.Zonas.Add(zona);
        await context.SaveChangesAsync();

        // Assert
        var auditLogs = await context.RegistrosAuditoria.ToListAsync();
        auditLogs.Should().ContainSingle();
        auditLogs.First().Codigo.Should().Be(AuditActionCode.CONFIG_ZONA);
        auditLogs.First().UsuarioId.Should().Be(context.CurrentUserId);
    }

    [Fact]
    public async Task SaveChangesAsync_WithModifiedEntity_ShouldCreateAuditLog()
    {
        // Arrange
        var interceptor = new AuditSaveChangesInterceptor();
        var options = new DbContextOptionsBuilder<CeibaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        var userId = Guid.NewGuid();
        await using var context = new CeibaDbContext(options, userId: userId);

        var zona = new Zona
        {
            Nombre = "Zona Original",
            Activo = true,
            UsuarioId = userId
        };

        context.Zonas.Add(zona);
        await context.SaveChangesAsync();

        // Act
        zona.Nombre = "Zona Modificada";
        await context.SaveChangesAsync();

        // Assert
        var auditLogs = await context.RegistrosAuditoria.ToListAsync();
        auditLogs.Should().HaveCount(2); // One for Add, one for Modify
        auditLogs.Last().Codigo.Should().Be(AuditActionCode.CONFIG_ZONA);
    }

    [Fact]
    public async Task SaveChangesAsync_WithoutUserId_ShouldCreateAuditWithNullUser()
    {
        // Arrange
        var interceptor = new AuditSaveChangesInterceptor();
        var options = new DbContextOptionsBuilder<CeibaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        await using var context = new CeibaDbContext(options, userId: null);

        var zona = new Zona
        {
            Nombre = "Zona Sistema",
            Activo = true,
            UsuarioId = Guid.NewGuid() // System-created
        };

        // Act
        context.Zonas.Add(zona);
        await context.SaveChangesAsync();

        // Assert
        var auditLogs = await context.RegistrosAuditoria.ToListAsync();
        auditLogs.Should().ContainSingle();
        auditLogs.First().UsuarioId.Should().BeNull();
    }

    [Fact]
    public async Task SaveChangesAsync_WithMultipleChanges_ShouldCreateMultipleAuditLogs()
    {
        // Arrange
        var interceptor = new AuditSaveChangesInterceptor();
        var options = new DbContextOptionsBuilder<CeibaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        var userId = Guid.NewGuid();
        await using var context = new CeibaDbContext(options, userId: userId);

        var zona1 = new Zona { Nombre = "Zona 1", Activo = true, UsuarioId = userId };
        var zona2 = new Zona { Nombre = "Zona 2", Activo = true, UsuarioId = userId };

        // Act
        context.Zonas.AddRange(zona1, zona2);
        await context.SaveChangesAsync();

        // Assert
        var auditLogs = await context.RegistrosAuditoria.ToListAsync();
        auditLogs.Should().HaveCount(2);
        auditLogs.All(log => log.Codigo == AuditActionCode.CONFIG_ZONA).Should().BeTrue();
    }
}
