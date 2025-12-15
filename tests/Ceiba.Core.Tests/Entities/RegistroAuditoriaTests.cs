using Ceiba.Core.Entities;
using FluentAssertions;

namespace Ceiba.Core.Tests.Entities;

/// <summary>
/// Unit tests for RegistroAuditoria entity.
/// Tests audit log entry functionality for system operation tracking.
/// </summary>
[Trait("Category", "Unit")]
public class RegistroAuditoriaTests
{
    #region Default Value Tests

    [Fact(DisplayName = "RegistroAuditoria should have Id default to 0")]
    public void RegistroAuditoria_Id_ShouldDefaultToZero()
    {
        // Arrange & Act
        var registro = new RegistroAuditoria();

        // Assert
        registro.Id.Should().Be(0);
    }

    [Fact(DisplayName = "RegistroAuditoria should have Codigo default to empty string")]
    public void RegistroAuditoria_Codigo_ShouldDefaultToEmptyString()
    {
        // Arrange & Act
        var registro = new RegistroAuditoria();

        // Assert
        registro.Codigo.Should().BeEmpty();
    }

    [Fact(DisplayName = "RegistroAuditoria should have IdRelacionado default to null")]
    public void RegistroAuditoria_IdRelacionado_ShouldDefaultToNull()
    {
        // Arrange & Act
        var registro = new RegistroAuditoria();

        // Assert
        registro.IdRelacionado.Should().BeNull();
    }

    [Fact(DisplayName = "RegistroAuditoria should have TablaRelacionada default to null")]
    public void RegistroAuditoria_TablaRelacionada_ShouldDefaultToNull()
    {
        // Arrange & Act
        var registro = new RegistroAuditoria();

        // Assert
        registro.TablaRelacionada.Should().BeNull();
    }

    [Fact(DisplayName = "RegistroAuditoria should have UsuarioId default to null")]
    public void RegistroAuditoria_UsuarioId_ShouldDefaultToNull()
    {
        // Arrange & Act
        var registro = new RegistroAuditoria();

        // Assert
        registro.UsuarioId.Should().BeNull();
    }

    [Fact(DisplayName = "RegistroAuditoria should have Ip default to null")]
    public void RegistroAuditoria_Ip_ShouldDefaultToNull()
    {
        // Arrange & Act
        var registro = new RegistroAuditoria();

        // Assert
        registro.Ip.Should().BeNull();
    }

    [Fact(DisplayName = "RegistroAuditoria should have Detalles default to null")]
    public void RegistroAuditoria_Detalles_ShouldDefaultToNull()
    {
        // Arrange & Act
        var registro = new RegistroAuditoria();

        // Assert
        registro.Detalles.Should().BeNull();
    }

    [Fact(DisplayName = "RegistroAuditoria should have CreatedAt set to UTC now")]
    public void RegistroAuditoria_CreatedAt_ShouldDefaultToUtcNow()
    {
        // Arrange
        var before = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var registro = new RegistroAuditoria();

        // Assert
        registro.CreatedAt.Should().BeAfter(before);
        registro.CreatedAt.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));
    }

    #endregion

    #region Audit Action Code Tests

    [Theory(DisplayName = "RegistroAuditoria should accept valid action codes")]
    [InlineData("AUTH_LOGIN")]
    [InlineData("AUTH_LOGOUT")]
    [InlineData("AUTH_FAILED")]
    [InlineData("REPORT_CREATE")]
    [InlineData("REPORT_UPDATE")]
    [InlineData("REPORT_SUBMIT")]
    [InlineData("USER_CREATE")]
    [InlineData("USER_SUSPEND")]
    [InlineData("CATALOG_UPDATE")]
    public void RegistroAuditoria_ShouldAcceptValidActionCodes(string codigo)
    {
        // Arrange
        var registro = new RegistroAuditoria();

        // Act
        registro.Codigo = codigo;

        // Assert
        registro.Codigo.Should().Be(codigo);
    }

    #endregion

    #region Entity Reference Tests

    [Fact(DisplayName = "RegistroAuditoria should set related entity information")]
    public void RegistroAuditoria_ShouldSetRelatedEntityInformation()
    {
        // Arrange
        var registro = new RegistroAuditoria();

        // Act
        registro.IdRelacionado = 12345;
        registro.TablaRelacionada = "REPORTE_INCIDENCIA";

        // Assert
        registro.IdRelacionado.Should().Be(12345);
        registro.TablaRelacionada.Should().Be("REPORTE_INCIDENCIA");
    }

    [Theory(DisplayName = "RegistroAuditoria should accept valid table names")]
    [InlineData("REPORTE_INCIDENCIA")]
    [InlineData("USUARIO")]
    [InlineData("ZONA")]
    [InlineData("SECTOR")]
    [InlineData("CUADRANTE")]
    [InlineData("CATALOGO_SUGERENCIA")]
    public void RegistroAuditoria_ShouldAcceptValidTableNames(string tableName)
    {
        // Arrange
        var registro = new RegistroAuditoria();

        // Act
        registro.TablaRelacionada = tableName;

        // Assert
        registro.TablaRelacionada.Should().Be(tableName);
    }

    #endregion

    #region User Tracking Tests

    [Fact(DisplayName = "RegistroAuditoria should track user who performed action")]
    public void RegistroAuditoria_ShouldTrackUserWhoPerformedAction()
    {
        // Arrange
        var registro = new RegistroAuditoria();
        var userId = Guid.NewGuid();

        // Act
        registro.UsuarioId = userId;

        // Assert
        registro.UsuarioId.Should().Be(userId);
    }

    [Fact(DisplayName = "RegistroAuditoria should allow null UsuarioId for system operations")]
    public void RegistroAuditoria_ShouldAllowNullUsuarioIdForSystemOperations()
    {
        // Arrange
        var registro = new RegistroAuditoria
        {
            Codigo = "SYSTEM_CLEANUP",
            UsuarioId = null
        };

        // Assert
        registro.UsuarioId.Should().BeNull();
    }

    #endregion

    #region IP Address Tests

    [Theory(DisplayName = "RegistroAuditoria should accept valid IPv4 addresses")]
    [InlineData("192.168.1.1")]
    [InlineData("10.0.0.1")]
    [InlineData("172.16.0.1")]
    [InlineData("127.0.0.1")]
    public void RegistroAuditoria_ShouldAcceptValidIPv4Addresses(string ip)
    {
        // Arrange
        var registro = new RegistroAuditoria();

        // Act
        registro.Ip = ip;

        // Assert
        registro.Ip.Should().Be(ip);
    }

    [Theory(DisplayName = "RegistroAuditoria should accept valid IPv6 addresses")]
    [InlineData("::1")]
    [InlineData("fe80::1")]
    [InlineData("2001:db8::1")]
    [InlineData("2001:0db8:85a3:0000:0000:8a2e:0370:7334")]
    public void RegistroAuditoria_ShouldAcceptValidIPv6Addresses(string ip)
    {
        // Arrange
        var registro = new RegistroAuditoria();

        // Act
        registro.Ip = ip;

        // Assert
        registro.Ip.Should().Be(ip);
    }

    #endregion

    #region Details JSON Tests

    [Fact(DisplayName = "RegistroAuditoria should store JSON details")]
    public void RegistroAuditoria_ShouldStoreJsonDetails()
    {
        // Arrange
        var registro = new RegistroAuditoria();
        var detalles = "{\"old_value\": \"Borrador\", \"new_value\": \"Entregado\"}";

        // Act
        registro.Detalles = detalles;

        // Assert
        registro.Detalles.Should().Be(detalles);
        registro.Detalles.Should().Contain("old_value");
        registro.Detalles.Should().Contain("new_value");
    }

    [Fact(DisplayName = "RegistroAuditoria should handle complex JSON details")]
    public void RegistroAuditoria_ShouldHandleComplexJsonDetails()
    {
        // Arrange
        var registro = new RegistroAuditoria();
        var detalles = "{\"changes\": [{\"field\": \"titulo\", \"from\": \"A\", \"to\": \"B\"}, {\"field\": \"descripcion\", \"from\": \"X\", \"to\": \"Y\"}], \"timestamp\": \"2024-01-15T10:30:00Z\"}";

        // Act
        registro.Detalles = detalles;

        // Assert
        registro.Detalles.Should().Be(detalles);
    }

    #endregion

    #region Complete Audit Entry Tests

    [Fact(DisplayName = "RegistroAuditoria should allow setting complete audit entry")]
    public void RegistroAuditoria_ShouldAllowSettingCompleteAuditEntry()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var createdAt = new DateTime(2024, 7, 25, 14, 30, 0, DateTimeKind.Utc);

        // Act
        var registro = new RegistroAuditoria
        {
            Id = 123456789,
            Codigo = "REPORT_SUBMIT",
            IdRelacionado = 1000,
            TablaRelacionada = "REPORTE_INCIDENCIA",
            UsuarioId = userId,
            Ip = "192.168.1.100",
            Detalles = "{\"estado_anterior\": \"Borrador\", \"estado_nuevo\": \"Entregado\"}",
            CreatedAt = createdAt
        };

        // Assert
        registro.Id.Should().Be(123456789);
        registro.Codigo.Should().Be("REPORT_SUBMIT");
        registro.IdRelacionado.Should().Be(1000);
        registro.TablaRelacionada.Should().Be("REPORTE_INCIDENCIA");
        registro.UsuarioId.Should().Be(userId);
        registro.Ip.Should().Be("192.168.1.100");
        registro.Detalles.Should().Contain("estado_anterior");
        registro.CreatedAt.Should().Be(createdAt);
    }

    [Fact(DisplayName = "RegistroAuditoria should handle system-triggered audit entry without user")]
    public void RegistroAuditoria_ShouldHandleSystemTriggeredAuditEntryWithoutUser()
    {
        // Arrange & Act
        var registro = new RegistroAuditoria
        {
            Id = 999999,
            Codigo = "SYSTEM_REPORT_GENERATION",
            IdRelacionado = null,
            TablaRelacionada = null,
            UsuarioId = null,
            Ip = null,
            Detalles = "{\"reports_generated\": 5, \"execution_time_ms\": 1234}"
        };

        // Assert
        registro.Id.Should().Be(999999);
        registro.Codigo.Should().Be("SYSTEM_REPORT_GENERATION");
        registro.IdRelacionado.Should().BeNull();
        registro.TablaRelacionada.Should().BeNull();
        registro.UsuarioId.Should().BeNull();
        registro.Ip.Should().BeNull();
        registro.Detalles.Should().Contain("reports_generated");
    }

    #endregion

    #region Id Type Tests

    [Fact(DisplayName = "RegistroAuditoria Id should support large values (BIGINT)")]
    public void RegistroAuditoria_Id_ShouldSupportLargeValues()
    {
        // Arrange
        var registro = new RegistroAuditoria();

        // Act
        registro.Id = long.MaxValue;

        // Assert
        registro.Id.Should().Be(long.MaxValue);
    }

    [Theory(DisplayName = "RegistroAuditoria Id should accept various large values")]
    [InlineData(1_000_000_000L)]
    [InlineData(10_000_000_000L)]
    [InlineData(100_000_000_000L)]
    public void RegistroAuditoria_Id_ShouldAcceptVariousLargeValues(long id)
    {
        // Arrange
        var registro = new RegistroAuditoria();

        // Act
        registro.Id = id;

        // Assert
        registro.Id.Should().Be(id);
    }

    #endregion

    #region Inheritance Tests

    [Fact(DisplayName = "RegistroAuditoria should inherit from BaseEntity")]
    public void RegistroAuditoria_ShouldInheritFromBaseEntity()
    {
        // Arrange & Act
        var registro = new RegistroAuditoria();

        // Assert
        registro.Should().BeAssignableTo<BaseEntity>();
    }

    #endregion
}
