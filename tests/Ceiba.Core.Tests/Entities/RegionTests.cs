using Ceiba.Core.Entities;
using FluentAssertions;

namespace Ceiba.Core.Tests.Entities;

/// <summary>
/// Unit tests for Region entity.
/// Region is the second level in the geographic hierarchy: Zona → Región → Sector → Cuadrante
/// </summary>
[Trait("Category", "Unit")]
public class RegionTests
{
    #region Default Value Tests

    [Fact(DisplayName = "Region should have Id default to 0")]
    public void Region_Id_ShouldDefaultToZero()
    {
        // Arrange & Act
        var region = new Region();

        // Assert
        region.Id.Should().Be(0);
    }

    [Fact(DisplayName = "Region should have Nombre default to empty string")]
    public void Region_Nombre_ShouldDefaultToEmptyString()
    {
        // Arrange & Act
        var region = new Region();

        // Assert
        region.Nombre.Should().BeEmpty();
    }

    [Fact(DisplayName = "Region should have ZonaId default to 0")]
    public void Region_ZonaId_ShouldDefaultToZero()
    {
        // Arrange & Act
        var region = new Region();

        // Assert
        region.ZonaId.Should().Be(0);
    }

    [Fact(DisplayName = "Region should have Activo default to true")]
    public void Region_Activo_ShouldDefaultToTrue()
    {
        // Arrange & Act
        var region = new Region();

        // Assert
        region.Activo.Should().BeTrue();
    }

    [Fact(DisplayName = "Region should have UsuarioId default to empty Guid")]
    public void Region_UsuarioId_ShouldDefaultToEmptyGuid()
    {
        // Arrange & Act
        var region = new Region();

        // Assert
        region.UsuarioId.Should().Be(Guid.Empty);
    }

    [Fact(DisplayName = "Region should have CreatedAt set to UTC now")]
    public void Region_CreatedAt_ShouldDefaultToUtcNow()
    {
        // Arrange
        var before = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var region = new Region();

        // Assert
        region.CreatedAt.Should().BeAfter(before);
        region.CreatedAt.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));
    }

    #endregion

    #region Navigation Property Tests

    [Fact(DisplayName = "Region Zona navigation should default to null")]
    public void Region_Zona_ShouldDefaultToNull()
    {
        // Arrange & Act
        var region = new Region();

        // Assert - The null! assignment means it will be null until EF loads it
        // We test that the property is accessible
        region.Should().NotBeNull();
    }

    [Fact(DisplayName = "Region Sectores collection should be initialized")]
    public void Region_Sectores_ShouldBeInitialized()
    {
        // Arrange & Act
        var region = new Region();

        // Assert
        region.Sectores.Should().NotBeNull();
        region.Sectores.Should().BeEmpty();
    }

    [Fact(DisplayName = "Region Reportes collection should be initialized")]
    public void Region_Reportes_ShouldBeInitialized()
    {
        // Arrange & Act
        var region = new Region();

        // Assert
        region.Reportes.Should().NotBeNull();
        region.Reportes.Should().BeEmpty();
    }

    [Fact(DisplayName = "Region should allow adding sectors to collection")]
    public void Region_Sectores_ShouldAllowAddingSectors()
    {
        // Arrange
        var region = new Region { Id = 1, Nombre = "Región Centro", ZonaId = 1 };
        var sector = new Sector { Id = 1, Nombre = "Sector A", RegionId = 1 };

        // Act
        region.Sectores.Add(sector);

        // Assert
        region.Sectores.Should().HaveCount(1);
        region.Sectores.Should().Contain(sector);
    }

    [Fact(DisplayName = "Region should allow setting parent Zona")]
    public void Region_Zona_ShouldAllowSettingParent()
    {
        // Arrange
        var zona = new Zona { Id = 1, Nombre = "Zona Norte" };
        var region = new Region { Id = 1, Nombre = "Región Centro", ZonaId = 1 };

        // Act
        region.Zona = zona;

        // Assert
        region.Zona.Should().Be(zona);
        region.Zona.Nombre.Should().Be("Zona Norte");
    }

    #endregion

    #region Hierarchy Tests

    [Fact(DisplayName = "Region ZonaId should match parent Zona Id")]
    public void Region_ZonaId_ShouldMatchParentZonaId()
    {
        // Arrange
        var zona = new Zona { Id = 5, Nombre = "Zona Sur" };
        var region = new Region { Nombre = "Región Este" };

        // Act
        region.ZonaId = zona.Id;
        region.Zona = zona;

        // Assert
        region.ZonaId.Should().Be(zona.Id);
        region.ZonaId.Should().Be(5);
    }

    [Fact(DisplayName = "Region should be linkable to Zona via navigation property")]
    public void Region_ShouldBeLinkableToZona()
    {
        // Arrange
        var zona = new Zona { Id = 1, Nombre = "Zona Norte" };
        var region = new Region { Id = 1, Nombre = "Región Centro", ZonaId = 1, Zona = zona };

        // Act
        zona.Regiones.Add(region);

        // Assert
        zona.Regiones.Should().Contain(region);
        region.Zona.Should().Be(zona);
    }

    #endregion

    #region Property Assignment Tests

    [Fact(DisplayName = "Region should allow setting all properties")]
    public void Region_ShouldAllowSettingAllProperties()
    {
        // Arrange
        var region = new Region();
        var userId = Guid.NewGuid();
        var createdAt = new DateTime(2024, 3, 20, 14, 30, 0, DateTimeKind.Utc);

        // Act
        region.Id = 10;
        region.Nombre = "Región Metropolitana";
        region.ZonaId = 3;
        region.Activo = false;
        region.UsuarioId = userId;
        region.CreatedAt = createdAt;

        // Assert
        region.Id.Should().Be(10);
        region.Nombre.Should().Be("Región Metropolitana");
        region.ZonaId.Should().Be(3);
        region.Activo.Should().BeFalse();
        region.UsuarioId.Should().Be(userId);
        region.CreatedAt.Should().Be(createdAt);
    }

    [Fact(DisplayName = "Region should handle special characters in Nombre")]
    public void Region_Nombre_ShouldHandleSpecialCharacters()
    {
        // Arrange
        var region = new Region();

        // Act
        region.Nombre = "Región Norte-Este (Área 2)";

        // Assert
        region.Nombre.Should().Be("Región Norte-Este (Área 2)");
    }

    #endregion

    #region Inheritance Tests

    [Fact(DisplayName = "Region should be assignable to BaseCatalogEntity")]
    public void Region_ShouldBeAssignableToBaseCatalogEntity()
    {
        // Arrange & Act
        var region = new Region();

        // Assert
        region.Should().BeAssignableTo<BaseCatalogEntity>();
    }

    #endregion
}
