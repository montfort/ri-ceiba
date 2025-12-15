using Ceiba.Core.Entities;
using FluentAssertions;

namespace Ceiba.Core.Tests.Entities;

/// <summary>
/// Unit tests for Zona entity.
/// Zona is the highest level in the geographic hierarchy: Zona → Región → Sector → Cuadrante
/// </summary>
[Trait("Category", "Unit")]
public class ZonaTests
{
    #region Default Value Tests

    [Fact(DisplayName = "Zona should have Id default to 0")]
    public void Zona_Id_ShouldDefaultToZero()
    {
        // Arrange & Act
        var zona = new Zona();

        // Assert
        zona.Id.Should().Be(0);
    }

    [Fact(DisplayName = "Zona should have Nombre default to empty string")]
    public void Zona_Nombre_ShouldDefaultToEmptyString()
    {
        // Arrange & Act
        var zona = new Zona();

        // Assert
        zona.Nombre.Should().BeEmpty();
    }

    [Fact(DisplayName = "Zona should have Activo default to true (from BaseCatalogEntity)")]
    public void Zona_Activo_ShouldDefaultToTrue()
    {
        // Arrange & Act
        var zona = new Zona();

        // Assert
        zona.Activo.Should().BeTrue();
    }

    [Fact(DisplayName = "Zona should have UsuarioId default to empty Guid")]
    public void Zona_UsuarioId_ShouldDefaultToEmptyGuid()
    {
        // Arrange & Act
        var zona = new Zona();

        // Assert
        zona.UsuarioId.Should().Be(Guid.Empty);
    }

    [Fact(DisplayName = "Zona should have CreatedAt set to UTC now")]
    public void Zona_CreatedAt_ShouldDefaultToUtcNow()
    {
        // Arrange
        var before = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var zona = new Zona();

        // Assert
        zona.CreatedAt.Should().BeAfter(before);
        zona.CreatedAt.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));
    }

    #endregion

    #region Navigation Property Tests

    [Fact(DisplayName = "Zona Regiones collection should be initialized")]
    public void Zona_Regiones_ShouldBeInitialized()
    {
        // Arrange & Act
        var zona = new Zona();

        // Assert
        zona.Regiones.Should().NotBeNull();
        zona.Regiones.Should().BeEmpty();
    }

    [Fact(DisplayName = "Zona Reportes collection should be initialized")]
    public void Zona_Reportes_ShouldBeInitialized()
    {
        // Arrange & Act
        var zona = new Zona();

        // Assert
        zona.Reportes.Should().NotBeNull();
        zona.Reportes.Should().BeEmpty();
    }

    [Fact(DisplayName = "Zona should allow adding regions to collection")]
    public void Zona_Regiones_ShouldAllowAddingRegions()
    {
        // Arrange
        var zona = new Zona { Id = 1, Nombre = "Zona Norte" };
        var region = new Region { Id = 1, Nombre = "Región Centro", ZonaId = 1 };

        // Act
        zona.Regiones.Add(region);

        // Assert
        zona.Regiones.Should().HaveCount(1);
        zona.Regiones.Should().Contain(region);
    }

    [Fact(DisplayName = "Zona should allow adding reports to collection")]
    public void Zona_Reportes_ShouldAllowAddingReports()
    {
        // Arrange
        var zona = new Zona { Id = 1, Nombre = "Zona Norte" };
        var reporte = new ReporteIncidencia { Id = 1, ZonaId = 1 };

        // Act
        zona.Reportes.Add(reporte);

        // Assert
        zona.Reportes.Should().HaveCount(1);
        zona.Reportes.Should().Contain(reporte);
    }

    #endregion

    #region Property Assignment Tests

    [Fact(DisplayName = "Zona should allow setting all properties")]
    public void Zona_ShouldAllowSettingAllProperties()
    {
        // Arrange
        var zona = new Zona();
        var userId = Guid.NewGuid();
        var createdAt = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);

        // Act
        zona.Id = 5;
        zona.Nombre = "Zona Poniente";
        zona.Activo = false;
        zona.UsuarioId = userId;
        zona.CreatedAt = createdAt;

        // Assert
        zona.Id.Should().Be(5);
        zona.Nombre.Should().Be("Zona Poniente");
        zona.Activo.Should().BeFalse();
        zona.UsuarioId.Should().Be(userId);
        zona.CreatedAt.Should().Be(createdAt);
    }

    [Fact(DisplayName = "Zona should handle special characters in Nombre")]
    public void Zona_Nombre_ShouldHandleSpecialCharacters()
    {
        // Arrange
        var zona = new Zona();

        // Act
        zona.Nombre = "Zona Norte-Centro (Área 1)";

        // Assert
        zona.Nombre.Should().Be("Zona Norte-Centro (Área 1)");
    }

    [Fact(DisplayName = "Zona should handle Unicode characters in Nombre")]
    public void Zona_Nombre_ShouldHandleUnicodeCharacters()
    {
        // Arrange
        var zona = new Zona();

        // Act
        zona.Nombre = "Zona México-Tenochtitlán";

        // Assert
        zona.Nombre.Should().Be("Zona México-Tenochtitlán");
    }

    #endregion

    #region Inheritance Tests

    [Fact(DisplayName = "Zona should be assignable to BaseCatalogEntity")]
    public void Zona_ShouldBeAssignableToBaseCatalogEntity()
    {
        // Arrange & Act
        var zona = new Zona();

        // Assert
        zona.Should().BeAssignableTo<BaseCatalogEntity>();
    }

    [Fact(DisplayName = "Zona should be assignable to BaseEntityWithUser")]
    public void Zona_ShouldBeAssignableToBaseEntityWithUser()
    {
        // Arrange & Act
        var zona = new Zona();

        // Assert
        zona.Should().BeAssignableTo<BaseEntityWithUser>();
    }

    [Fact(DisplayName = "Zona should be assignable to BaseEntity")]
    public void Zona_ShouldBeAssignableToBaseEntity()
    {
        // Arrange & Act
        var zona = new Zona();

        // Assert
        zona.Should().BeAssignableTo<BaseEntity>();
    }

    #endregion
}
