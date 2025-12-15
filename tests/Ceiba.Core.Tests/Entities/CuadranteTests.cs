using Ceiba.Core.Entities;
using FluentAssertions;

namespace Ceiba.Core.Tests.Entities;

/// <summary>
/// Unit tests for Cuadrante entity.
/// Cuadrante is the lowest level in the geographic hierarchy: Zona → Región → Sector → Cuadrante
/// </summary>
[Trait("Category", "Unit")]
public class CuadranteTests
{
    #region Default Value Tests

    [Fact(DisplayName = "Cuadrante should have Id default to 0")]
    public void Cuadrante_Id_ShouldDefaultToZero()
    {
        // Arrange & Act
        var cuadrante = new Cuadrante();

        // Assert
        cuadrante.Id.Should().Be(0);
    }

    [Fact(DisplayName = "Cuadrante should have Nombre default to empty string")]
    public void Cuadrante_Nombre_ShouldDefaultToEmptyString()
    {
        // Arrange & Act
        var cuadrante = new Cuadrante();

        // Assert
        cuadrante.Nombre.Should().BeEmpty();
    }

    [Fact(DisplayName = "Cuadrante should have SectorId default to 0")]
    public void Cuadrante_SectorId_ShouldDefaultToZero()
    {
        // Arrange & Act
        var cuadrante = new Cuadrante();

        // Assert
        cuadrante.SectorId.Should().Be(0);
    }

    [Fact(DisplayName = "Cuadrante should have Activo default to true")]
    public void Cuadrante_Activo_ShouldDefaultToTrue()
    {
        // Arrange & Act
        var cuadrante = new Cuadrante();

        // Assert
        cuadrante.Activo.Should().BeTrue();
    }

    [Fact(DisplayName = "Cuadrante should have UsuarioId default to empty Guid")]
    public void Cuadrante_UsuarioId_ShouldDefaultToEmptyGuid()
    {
        // Arrange & Act
        var cuadrante = new Cuadrante();

        // Assert
        cuadrante.UsuarioId.Should().Be(Guid.Empty);
    }

    [Fact(DisplayName = "Cuadrante should have CreatedAt set to UTC now")]
    public void Cuadrante_CreatedAt_ShouldDefaultToUtcNow()
    {
        // Arrange
        var before = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var cuadrante = new Cuadrante();

        // Assert
        cuadrante.CreatedAt.Should().BeAfter(before);
        cuadrante.CreatedAt.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));
    }

    #endregion

    #region Navigation Property Tests

    [Fact(DisplayName = "Cuadrante Reportes collection should be initialized")]
    public void Cuadrante_Reportes_ShouldBeInitialized()
    {
        // Arrange & Act
        var cuadrante = new Cuadrante();

        // Assert
        cuadrante.Reportes.Should().NotBeNull();
        cuadrante.Reportes.Should().BeEmpty();
    }

    [Fact(DisplayName = "Cuadrante should allow adding reports to collection")]
    public void Cuadrante_Reportes_ShouldAllowAddingReports()
    {
        // Arrange
        var cuadrante = new Cuadrante { Id = 1, Nombre = "Cuadrante 1", SectorId = 1 };
        var reporte = new ReporteIncidencia { Id = 1, CuadranteId = 1 };

        // Act
        cuadrante.Reportes.Add(reporte);

        // Assert
        cuadrante.Reportes.Should().HaveCount(1);
        cuadrante.Reportes.Should().Contain(reporte);
    }

    [Fact(DisplayName = "Cuadrante should allow setting parent Sector")]
    public void Cuadrante_Sector_ShouldAllowSettingParent()
    {
        // Arrange
        var sector = new Sector { Id = 1, Nombre = "Sector A", RegionId = 1 };
        var cuadrante = new Cuadrante { Id = 1, Nombre = "Cuadrante 1", SectorId = 1 };

        // Act
        cuadrante.Sector = sector;

        // Assert
        cuadrante.Sector.Should().Be(sector);
        cuadrante.Sector.Nombre.Should().Be("Sector A");
    }

    #endregion

    #region Hierarchy Tests

    [Fact(DisplayName = "Cuadrante SectorId should match parent Sector Id")]
    public void Cuadrante_SectorId_ShouldMatchParentSectorId()
    {
        // Arrange
        var sector = new Sector { Id = 12, Nombre = "Sector X", RegionId = 1 };
        var cuadrante = new Cuadrante { Nombre = "Cuadrante Alpha" };

        // Act
        cuadrante.SectorId = sector.Id;
        cuadrante.Sector = sector;

        // Assert
        cuadrante.SectorId.Should().Be(sector.Id);
        cuadrante.SectorId.Should().Be(12);
    }

    [Fact(DisplayName = "Cuadrante should be linkable to Sector via navigation property")]
    public void Cuadrante_ShouldBeLinkableToSector()
    {
        // Arrange
        var sector = new Sector { Id = 1, Nombre = "Sector A", RegionId = 1 };
        var cuadrante = new Cuadrante { Id = 1, Nombre = "Cuadrante 1", SectorId = 1, Sector = sector };

        // Act
        sector.Cuadrantes.Add(cuadrante);

        // Assert
        sector.Cuadrantes.Should().Contain(cuadrante);
        cuadrante.Sector.Should().Be(sector);
    }

    [Fact(DisplayName = "Cuadrante should be at the lowest level of hierarchy")]
    public void Cuadrante_ShouldBeAtLowestLevelOfHierarchy()
    {
        // Arrange - Build complete hierarchy
        var zona = new Zona { Id = 1, Nombre = "Zona Norte" };
        var region = new Region { Id = 1, Nombre = "Región Centro", ZonaId = 1, Zona = zona };
        var sector = new Sector { Id = 1, Nombre = "Sector A", RegionId = 1, Region = region };
        var cuadrante = new Cuadrante { Id = 1, Nombre = "Cuadrante 1-A", SectorId = 1, Sector = sector };

        // Act & Assert - Verify complete chain
        cuadrante.Sector.Should().Be(sector);
        cuadrante.Sector.Region.Should().Be(region);
        cuadrante.Sector.Region.Zona.Should().Be(zona);
    }

    [Fact(DisplayName = "Cuadrante should support multiple reports")]
    public void Cuadrante_ShouldSupportMultipleReports()
    {
        // Arrange
        var cuadrante = new Cuadrante { Id = 1, Nombre = "Cuadrante 1", SectorId = 1 };
        var reportes = Enumerable.Range(1, 10).Select(i => new ReporteIncidencia
        {
            Id = i,
            CuadranteId = 1
        }).ToList();

        // Act
        foreach (var reporte in reportes)
        {
            cuadrante.Reportes.Add(reporte);
        }

        // Assert
        cuadrante.Reportes.Should().HaveCount(10);
    }

    #endregion

    #region Property Assignment Tests

    [Fact(DisplayName = "Cuadrante should allow setting all properties")]
    public void Cuadrante_ShouldAllowSettingAllProperties()
    {
        // Arrange
        var cuadrante = new Cuadrante();
        var userId = Guid.NewGuid();
        var createdAt = new DateTime(2024, 7, 25, 16, 45, 0, DateTimeKind.Utc);

        // Act
        cuadrante.Id = 100;
        cuadrante.Nombre = "Cuadrante Norte-1A";
        cuadrante.SectorId = 20;
        cuadrante.Activo = false;
        cuadrante.UsuarioId = userId;
        cuadrante.CreatedAt = createdAt;

        // Assert
        cuadrante.Id.Should().Be(100);
        cuadrante.Nombre.Should().Be("Cuadrante Norte-1A");
        cuadrante.SectorId.Should().Be(20);
        cuadrante.Activo.Should().BeFalse();
        cuadrante.UsuarioId.Should().Be(userId);
        cuadrante.CreatedAt.Should().Be(createdAt);
    }

    [Fact(DisplayName = "Cuadrante should handle naming conventions with numbers")]
    public void Cuadrante_Nombre_ShouldHandleNamingConventionsWithNumbers()
    {
        // Arrange
        var cuadrante = new Cuadrante();

        // Act
        cuadrante.Nombre = "C-123-A";

        // Assert
        cuadrante.Nombre.Should().Be("C-123-A");
    }

    #endregion

    #region Inheritance Tests

    [Fact(DisplayName = "Cuadrante should be assignable to BaseCatalogEntity")]
    public void Cuadrante_ShouldBeAssignableToBaseCatalogEntity()
    {
        // Arrange & Act
        var cuadrante = new Cuadrante();

        // Assert
        cuadrante.Should().BeAssignableTo<BaseCatalogEntity>();
    }

    #endregion
}
