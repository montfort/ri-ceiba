using Ceiba.Infrastructure.Caching;
using FluentAssertions;

namespace Ceiba.Infrastructure.Tests.Caching;

/// <summary>
/// Unit tests for CacheKeys - cache key generation and formatting.
/// </summary>
public class CacheKeysTests
{
    #region Constant Key Tests

    [Fact(DisplayName = "AllZonas key should have correct format")]
    public void AllZonas_ShouldHaveCorrectFormat()
    {
        // Assert
        CacheKeys.AllZonas.Should().Be("catalog:zonas:all");
    }

    [Fact(DisplayName = "AllSectores key should have correct format")]
    public void AllSectores_ShouldHaveCorrectFormat()
    {
        // Assert
        CacheKeys.AllSectores.Should().Be("catalog:sectores:all");
    }

    [Fact(DisplayName = "AllCuadrantes key should have correct format")]
    public void AllCuadrantes_ShouldHaveCorrectFormat()
    {
        // Assert
        CacheKeys.AllCuadrantes.Should().Be("catalog:cuadrantes:all");
    }

    [Fact(DisplayName = "Template keys should contain placeholders")]
    public void TemplateKeys_ShouldContainPlaceholders()
    {
        // Assert
        CacheKeys.SugerenciasByCampo.Should().Contain("{0}");
        CacheKeys.RegionesByZona.Should().Contain("{0}");
        CacheKeys.SectoresByRegion.Should().Contain("{0}");
        CacheKeys.CuadrantesBySector.Should().Contain("{0}");
        CacheKeys.ReportById.Should().Contain("{0}");
        CacheKeys.ReportsByUser.Should().Contain("{0}");
        CacheKeys.UserById.Should().Contain("{0}");
        CacheKeys.UserRoles.Should().Contain("{0}");
    }

    #endregion

    #region Format Method Tests

    [Fact(DisplayName = "Format should replace single placeholder")]
    public void Format_SinglePlaceholder_ShouldReplace()
    {
        // Act
        var result = CacheKeys.Format(CacheKeys.ReportById, 123);

        // Assert
        result.Should().Be("report:123");
    }

    [Fact(DisplayName = "Format should replace multiple placeholders")]
    public void Format_MultiplePlaceholders_ShouldReplace()
    {
        // Act
        var result = CacheKeys.Format(CacheKeys.ReportsByZona, 5, "2025-01-15");

        // Assert
        result.Should().Be("stats:reports:zona:5:2025-01-15");
    }

    [Fact(DisplayName = "Format with RegionesByZona should create correct key")]
    public void Format_RegionesByZona_ShouldCreateCorrectKey()
    {
        // Arrange
        var zonaId = 42;

        // Act
        var result = CacheKeys.Format(CacheKeys.RegionesByZona, zonaId);

        // Assert
        result.Should().Be("catalog:regiones:zona:42");
    }

    [Fact(DisplayName = "Format with SectoresByRegion should create correct key")]
    public void Format_SectoresByRegion_ShouldCreateCorrectKey()
    {
        // Arrange
        var regionId = 42;

        // Act
        var result = CacheKeys.Format(CacheKeys.SectoresByRegion, regionId);

        // Assert
        result.Should().Be("catalog:sectores:region:42");
    }

    [Fact(DisplayName = "Format with CuadrantesBySector should create correct key")]
    public void Format_CuadrantesBySector_ShouldCreateCorrectKey()
    {
        // Arrange
        var sectorId = 99;

        // Act
        var result = CacheKeys.Format(CacheKeys.CuadrantesBySector, sectorId);

        // Assert
        result.Should().Be("catalog:cuadrantes:sector:99");
    }

    [Fact(DisplayName = "Format with SugerenciasByCampo should create correct key")]
    public void Format_SugerenciasByCampo_ShouldCreateCorrectKey()
    {
        // Arrange
        var campo = "sexo";

        // Act
        var result = CacheKeys.Format(CacheKeys.SugerenciasByCampo, campo);

        // Assert
        result.Should().Be("catalog:sugerencias:sexo");
    }

    [Fact(DisplayName = "Format with UserById should create correct key")]
    public void Format_UserById_ShouldCreateCorrectKey()
    {
        // Arrange
        var userId = Guid.Parse("12345678-1234-1234-1234-123456789012");

        // Act
        var result = CacheKeys.Format(CacheKeys.UserById, userId);

        // Assert
        result.Should().Be("user:12345678-1234-1234-1234-123456789012");
    }

    [Fact(DisplayName = "Format with DashboardStats should create correct key")]
    public void Format_DashboardStats_ShouldCreateCorrectKey()
    {
        // Arrange
        var date = "2025-01-15";

        // Act
        var result = CacheKeys.Format(CacheKeys.DashboardStats, date);

        // Assert
        result.Should().Be("stats:dashboard:2025-01-15");
    }

    #endregion

    #region ForFilter Method Tests

    [Fact(DisplayName = "ForFilter should generate hash-based key")]
    public void ForFilter_ShouldGenerateHashBasedKey()
    {
        // Arrange
        var filter = new { Page = 1, PageSize = 20, Status = "active" };

        // Act
        var result = CacheKeys.ForFilter("reports:list", filter);

        // Assert
        result.Should().StartWith("reports:list:");
        result.Should().MatchRegex(@"reports:list:-?\d+");
    }

    [Fact(DisplayName = "ForFilter with same filter should generate same key")]
    public void ForFilter_SameFilter_ShouldGenerateSameKey()
    {
        // Arrange
        var filter1 = new { Page = 1, Status = "active" };
        var filter2 = new { Page = 1, Status = "active" };

        // Act
        var result1 = CacheKeys.ForFilter("test", filter1);
        var result2 = CacheKeys.ForFilter("test", filter2);

        // Assert
        result1.Should().Be(result2);
    }

    [Fact(DisplayName = "ForFilter with different filter should generate different key")]
    public void ForFilter_DifferentFilter_ShouldGenerateDifferentKey()
    {
        // Arrange
        var filter1 = new { Page = 1 };
        var filter2 = new { Page = 2 };

        // Act
        var result1 = CacheKeys.ForFilter("test", filter1);
        var result2 = CacheKeys.ForFilter("test", filter2);

        // Assert
        result1.Should().NotBe(result2);
    }

    [Fact(DisplayName = "ForFilter with different prefix should generate different key")]
    public void ForFilter_DifferentPrefix_ShouldGenerateDifferentKey()
    {
        // Arrange
        var filter = new { Page = 1 };

        // Act
        var result1 = CacheKeys.ForFilter("prefix1", filter);
        var result2 = CacheKeys.ForFilter("prefix2", filter);

        // Assert
        result1.Should().NotBe(result2);
        result1.Should().StartWith("prefix1:");
        result2.Should().StartWith("prefix2:");
    }

    #endregion
}
