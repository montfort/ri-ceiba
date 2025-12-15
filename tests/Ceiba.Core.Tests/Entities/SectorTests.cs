using Ceiba.Core.Entities;
using FluentAssertions;

namespace Ceiba.Core.Tests.Entities;

/// <summary>
/// Unit tests for Sector entity.
/// Sector is the third level in the geographic hierarchy: Zona → Región → Sector → Cuadrante
/// </summary>
[Trait("Category", "Unit")]
public class SectorTests
{
    #region Default Value Tests

    [Fact(DisplayName = "Sector should have Id default to 0")]
    public void Sector_Id_ShouldDefaultToZero()
    {
        // Arrange & Act
        var sector = new Sector();

        // Assert
        sector.Id.Should().Be(0);
    }

    [Fact(DisplayName = "Sector should have Nombre default to empty string")]
    public void Sector_Nombre_ShouldDefaultToEmptyString()
    {
        // Arrange & Act
        var sector = new Sector();

        // Assert
        sector.Nombre.Should().BeEmpty();
    }

    [Fact(DisplayName = "Sector should have RegionId default to 0")]
    public void Sector_RegionId_ShouldDefaultToZero()
    {
        // Arrange & Act
        var sector = new Sector();

        // Assert
        sector.RegionId.Should().Be(0);
    }

    [Fact(DisplayName = "Sector should have Activo default to true")]
    public void Sector_Activo_ShouldDefaultToTrue()
    {
        // Arrange & Act
        var sector = new Sector();

        // Assert
        sector.Activo.Should().BeTrue();
    }

    [Fact(DisplayName = "Sector should have UsuarioId default to empty Guid")]
    public void Sector_UsuarioId_ShouldDefaultToEmptyGuid()
    {
        // Arrange & Act
        var sector = new Sector();

        // Assert
        sector.UsuarioId.Should().Be(Guid.Empty);
    }

    [Fact(DisplayName = "Sector should have CreatedAt set to UTC now")]
    public void Sector_CreatedAt_ShouldDefaultToUtcNow()
    {
        // Arrange
        var before = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var sector = new Sector();

        // Assert
        sector.CreatedAt.Should().BeAfter(before);
        sector.CreatedAt.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));
    }

    #endregion

    #region Navigation Property Tests

    [Fact(DisplayName = "Sector Cuadrantes collection should be initialized")]
    public void Sector_Cuadrantes_ShouldBeInitialized()
    {
        // Arrange & Act
        var sector = new Sector();

        // Assert
        sector.Cuadrantes.Should().NotBeNull();
        sector.Cuadrantes.Should().BeEmpty();
    }

    [Fact(DisplayName = "Sector Reportes collection should be initialized")]
    public void Sector_Reportes_ShouldBeInitialized()
    {
        // Arrange & Act
        var sector = new Sector();

        // Assert
        sector.Reportes.Should().NotBeNull();
        sector.Reportes.Should().BeEmpty();
    }

    [Fact(DisplayName = "Sector should allow adding cuadrantes to collection")]
    public void Sector_Cuadrantes_ShouldAllowAddingCuadrantes()
    {
        // Arrange
        var sector = new Sector { Id = 1, Nombre = "Sector A", RegionId = 1 };
        var cuadrante = new Cuadrante { Id = 1, Nombre = "Cuadrante 1", SectorId = 1 };

        // Act
        sector.Cuadrantes.Add(cuadrante);

        // Assert
        sector.Cuadrantes.Should().HaveCount(1);
        sector.Cuadrantes.Should().Contain(cuadrante);
    }

    [Fact(DisplayName = "Sector should allow setting parent Region")]
    public void Sector_Region_ShouldAllowSettingParent()
    {
        // Arrange
        var region = new Region { Id = 1, Nombre = "Región Centro", ZonaId = 1 };
        var sector = new Sector { Id = 1, Nombre = "Sector A", RegionId = 1 };

        // Act
        sector.Region = region;

        // Assert
        sector.Region.Should().Be(region);
        sector.Region.Nombre.Should().Be("Región Centro");
    }

    #endregion

    #region Hierarchy Tests

    [Fact(DisplayName = "Sector RegionId should match parent Region Id")]
    public void Sector_RegionId_ShouldMatchParentRegionId()
    {
        // Arrange
        var region = new Region { Id = 7, Nombre = "Región Este", ZonaId = 1 };
        var sector = new Sector { Nombre = "Sector B" };

        // Act
        sector.RegionId = region.Id;
        sector.Region = region;

        // Assert
        sector.RegionId.Should().Be(region.Id);
        sector.RegionId.Should().Be(7);
    }

    [Fact(DisplayName = "Sector should be linkable to Region via navigation property")]
    public void Sector_ShouldBeLinkableToRegion()
    {
        // Arrange
        var region = new Region { Id = 1, Nombre = "Región Centro", ZonaId = 1 };
        var sector = new Sector { Id = 1, Nombre = "Sector A", RegionId = 1, Region = region };

        // Act
        region.Sectores.Add(sector);

        // Assert
        region.Sectores.Should().Contain(sector);
        sector.Region.Should().Be(region);
    }

    [Fact(DisplayName = "Sector should support multiple cuadrantes")]
    public void Sector_ShouldSupportMultipleCuadrantes()
    {
        // Arrange
        var sector = new Sector { Id = 1, Nombre = "Sector A", RegionId = 1 };
        var cuadrantes = Enumerable.Range(1, 5).Select(i => new Cuadrante
        {
            Id = i,
            Nombre = $"Cuadrante {i}",
            SectorId = 1
        }).ToList();

        // Act
        foreach (var cuadrante in cuadrantes)
        {
            sector.Cuadrantes.Add(cuadrante);
        }

        // Assert
        sector.Cuadrantes.Should().HaveCount(5);
    }

    #endregion

    #region Property Assignment Tests

    [Fact(DisplayName = "Sector should allow setting all properties")]
    public void Sector_ShouldAllowSettingAllProperties()
    {
        // Arrange
        var sector = new Sector();
        var userId = Guid.NewGuid();
        var createdAt = new DateTime(2024, 5, 10, 8, 15, 0, DateTimeKind.Utc);

        // Act
        sector.Id = 15;
        sector.Nombre = "Sector Industrial";
        sector.RegionId = 4;
        sector.Activo = false;
        sector.UsuarioId = userId;
        sector.CreatedAt = createdAt;

        // Assert
        sector.Id.Should().Be(15);
        sector.Nombre.Should().Be("Sector Industrial");
        sector.RegionId.Should().Be(4);
        sector.Activo.Should().BeFalse();
        sector.UsuarioId.Should().Be(userId);
        sector.CreatedAt.Should().Be(createdAt);
    }

    #endregion

    #region Inheritance Tests

    [Fact(DisplayName = "Sector should be assignable to BaseCatalogEntity")]
    public void Sector_ShouldBeAssignableToBaseCatalogEntity()
    {
        // Arrange & Act
        var sector = new Sector();

        // Assert
        sector.Should().BeAssignableTo<BaseCatalogEntity>();
    }

    #endregion
}
