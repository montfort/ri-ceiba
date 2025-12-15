using Ceiba.Core.Entities;
using FluentAssertions;

namespace Ceiba.Core.Tests.Entities;

/// <summary>
/// Unit tests for CatalogoSugerencia entity.
/// Tests configurable suggestion catalog for text field autocomplete.
/// </summary>
[Trait("Category", "Unit")]
public class CatalogoSugerenciaTests
{
    #region Default Value Tests

    [Fact(DisplayName = "CatalogoSugerencia should have Id default to 0")]
    public void CatalogoSugerencia_Id_ShouldDefaultToZero()
    {
        // Arrange & Act
        var sugerencia = new CatalogoSugerencia();

        // Assert
        sugerencia.Id.Should().Be(0);
    }

    [Fact(DisplayName = "CatalogoSugerencia should have Campo default to empty string")]
    public void CatalogoSugerencia_Campo_ShouldDefaultToEmptyString()
    {
        // Arrange & Act
        var sugerencia = new CatalogoSugerencia();

        // Assert
        sugerencia.Campo.Should().BeEmpty();
    }

    [Fact(DisplayName = "CatalogoSugerencia should have Valor default to empty string")]
    public void CatalogoSugerencia_Valor_ShouldDefaultToEmptyString()
    {
        // Arrange & Act
        var sugerencia = new CatalogoSugerencia();

        // Assert
        sugerencia.Valor.Should().BeEmpty();
    }

    [Fact(DisplayName = "CatalogoSugerencia should have Orden default to 0")]
    public void CatalogoSugerencia_Orden_ShouldDefaultToZero()
    {
        // Arrange & Act
        var sugerencia = new CatalogoSugerencia();

        // Assert
        sugerencia.Orden.Should().Be(0);
    }

    [Fact(DisplayName = "CatalogoSugerencia should have Activo default to true")]
    public void CatalogoSugerencia_Activo_ShouldDefaultToTrue()
    {
        // Arrange & Act
        var sugerencia = new CatalogoSugerencia();

        // Assert
        sugerencia.Activo.Should().BeTrue();
    }

    [Fact(DisplayName = "CatalogoSugerencia should have UsuarioId default to empty Guid")]
    public void CatalogoSugerencia_UsuarioId_ShouldDefaultToEmptyGuid()
    {
        // Arrange & Act
        var sugerencia = new CatalogoSugerencia();

        // Assert
        sugerencia.UsuarioId.Should().Be(Guid.Empty);
    }

    [Fact(DisplayName = "CatalogoSugerencia should have CreatedAt set to UTC now")]
    public void CatalogoSugerencia_CreatedAt_ShouldDefaultToUtcNow()
    {
        // Arrange
        var before = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var sugerencia = new CatalogoSugerencia();

        // Assert
        sugerencia.CreatedAt.Should().BeAfter(before);
        sugerencia.CreatedAt.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));
    }

    #endregion

    #region Campo (Field Name) Tests

    [Theory(DisplayName = "CatalogoSugerencia should accept valid field names")]
    [InlineData("sexo")]
    [InlineData("delito")]
    [InlineData("tipo_de_atencion")]
    public void CatalogoSugerencia_ShouldAcceptValidFieldNames(string campo)
    {
        // Arrange
        var sugerencia = new CatalogoSugerencia();

        // Act
        sugerencia.Campo = campo;

        // Assert
        sugerencia.Campo.Should().Be(campo);
    }

    [Fact(DisplayName = "CatalogoSugerencia Campo should be case-sensitive")]
    public void CatalogoSugerencia_Campo_ShouldBeCaseSensitive()
    {
        // Arrange
        var sugerencia1 = new CatalogoSugerencia { Campo = "sexo" };
        var sugerencia2 = new CatalogoSugerencia { Campo = "Sexo" };

        // Assert
        sugerencia1.Campo.Should().NotBe(sugerencia2.Campo);
    }

    #endregion

    #region Sexo Field Suggestions Tests

    [Fact(DisplayName = "CatalogoSugerencia should store sexo suggestion: Masculino")]
    public void CatalogoSugerencia_ShouldStoreSexoSuggestion_Masculino()
    {
        // Arrange & Act
        var sugerencia = new CatalogoSugerencia
        {
            Campo = "sexo",
            Valor = "Masculino",
            Orden = 1
        };

        // Assert
        sugerencia.Campo.Should().Be("sexo");
        sugerencia.Valor.Should().Be("Masculino");
        sugerencia.Orden.Should().Be(1);
    }

    [Fact(DisplayName = "CatalogoSugerencia should store sexo suggestion: Femenino")]
    public void CatalogoSugerencia_ShouldStoreSexoSuggestion_Femenino()
    {
        // Arrange & Act
        var sugerencia = new CatalogoSugerencia
        {
            Campo = "sexo",
            Valor = "Femenino",
            Orden = 2
        };

        // Assert
        sugerencia.Campo.Should().Be("sexo");
        sugerencia.Valor.Should().Be("Femenino");
    }

    [Fact(DisplayName = "CatalogoSugerencia should store sexo suggestion: No Binario")]
    public void CatalogoSugerencia_ShouldStoreSexoSuggestion_NoBinario()
    {
        // Arrange & Act
        var sugerencia = new CatalogoSugerencia
        {
            Campo = "sexo",
            Valor = "No Binario",
            Orden = 3
        };

        // Assert
        sugerencia.Valor.Should().Be("No Binario");
    }

    #endregion

    #region Delito Field Suggestions Tests

    [Theory(DisplayName = "CatalogoSugerencia should accept valid delito values")]
    [InlineData("Robo")]
    [InlineData("Robo a Casa Habitación")]
    [InlineData("Robo de Vehículo")]
    [InlineData("Vandalismo")]
    [InlineData("Lesiones")]
    [InlineData("Homicidio")]
    [InlineData("Secuestro")]
    [InlineData("Extorsión")]
    public void CatalogoSugerencia_ShouldAcceptValidDelitoValues(string delito)
    {
        // Arrange & Act
        var sugerencia = new CatalogoSugerencia
        {
            Campo = "delito",
            Valor = delito
        };

        // Assert
        sugerencia.Campo.Should().Be("delito");
        sugerencia.Valor.Should().Be(delito);
    }

    [Fact(DisplayName = "CatalogoSugerencia should handle delito with special characters")]
    public void CatalogoSugerencia_ShouldHandleDelitoWithSpecialCharacters()
    {
        // Arrange & Act
        var sugerencia = new CatalogoSugerencia
        {
            Campo = "delito",
            Valor = "Robo a Casa Habitación (con violencia)"
        };

        // Assert
        sugerencia.Valor.Should().Contain("Casa Habitación");
        sugerencia.Valor.Should().Contain("violencia");
    }

    #endregion

    #region TipoDeAtencion Field Suggestions Tests

    [Theory(DisplayName = "CatalogoSugerencia should accept valid tipo_de_atencion values")]
    [InlineData("Orientación")]
    [InlineData("Canalización")]
    [InlineData("Detención")]
    [InlineData("Prevención")]
    [InlineData("Auxilio")]
    [InlineData("Investigación")]
    public void CatalogoSugerencia_ShouldAcceptValidTipoDeAtencionValues(string tipoAtencion)
    {
        // Arrange & Act
        var sugerencia = new CatalogoSugerencia
        {
            Campo = "tipo_de_atencion",
            Valor = tipoAtencion
        };

        // Assert
        sugerencia.Campo.Should().Be("tipo_de_atencion");
        sugerencia.Valor.Should().Be(tipoAtencion);
    }

    #endregion

    #region Orden (Display Order) Tests

    [Fact(DisplayName = "CatalogoSugerencia Orden should control display order")]
    public void CatalogoSugerencia_Orden_ShouldControlDisplayOrder()
    {
        // Arrange
        var sugerencias = new List<CatalogoSugerencia>
        {
            new() { Campo = "sexo", Valor = "Femenino", Orden = 2 },
            new() { Campo = "sexo", Valor = "Masculino", Orden = 1 },
            new() { Campo = "sexo", Valor = "Otro", Orden = 3 }
        };

        // Act
        var ordenadas = sugerencias.OrderBy(s => s.Orden).ToList();

        // Assert
        ordenadas[0].Valor.Should().Be("Masculino");
        ordenadas[1].Valor.Should().Be("Femenino");
        ordenadas[2].Valor.Should().Be("Otro");
    }

    [Theory(DisplayName = "CatalogoSugerencia should accept various Orden values")]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(999)]
    public void CatalogoSugerencia_ShouldAcceptVariousOrdenValues(int orden)
    {
        // Arrange
        var sugerencia = new CatalogoSugerencia();

        // Act
        sugerencia.Orden = orden;

        // Assert
        sugerencia.Orden.Should().Be(orden);
    }

    [Fact(DisplayName = "CatalogoSugerencia should allow negative Orden for special ordering")]
    public void CatalogoSugerencia_ShouldAllowNegativeOrden()
    {
        // Arrange
        var sugerencia = new CatalogoSugerencia();

        // Act
        sugerencia.Orden = -1;

        // Assert
        sugerencia.Orden.Should().Be(-1);
    }

    #endregion

    #region Activo Status Tests

    [Fact(DisplayName = "CatalogoSugerencia should allow deactivating suggestion")]
    public void CatalogoSugerencia_ShouldAllowDeactivatingSuggestion()
    {
        // Arrange
        var sugerencia = new CatalogoSugerencia { Activo = true };

        // Act
        sugerencia.Activo = false;

        // Assert
        sugerencia.Activo.Should().BeFalse();
    }

    [Fact(DisplayName = "Inactive CatalogoSugerencia should be filterable")]
    public void InactiveCatalogoSugerencia_ShouldBeFilterable()
    {
        // Arrange
        var sugerencias = new List<CatalogoSugerencia>
        {
            new() { Campo = "sexo", Valor = "Masculino", Activo = true },
            new() { Campo = "sexo", Valor = "Obsoleto", Activo = false },
            new() { Campo = "sexo", Valor = "Femenino", Activo = true }
        };

        // Act
        var activas = sugerencias.Where(s => s.Activo).ToList();

        // Assert
        activas.Should().HaveCount(2);
        activas.Should().NotContain(s => s.Valor == "Obsoleto");
    }

    #endregion

    #region Complete Suggestion Tests

    [Fact(DisplayName = "CatalogoSugerencia should allow setting all properties")]
    public void CatalogoSugerencia_ShouldAllowSettingAllProperties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var createdAt = new DateTime(2024, 7, 25, 10, 0, 0, DateTimeKind.Utc);

        // Act
        var sugerencia = new CatalogoSugerencia
        {
            Id = 50,
            Campo = "delito",
            Valor = "Robo de Vehículo con Violencia",
            Orden = 5,
            Activo = true,
            UsuarioId = userId,
            CreatedAt = createdAt
        };

        // Assert
        sugerencia.Id.Should().Be(50);
        sugerencia.Campo.Should().Be("delito");
        sugerencia.Valor.Should().Be("Robo de Vehículo con Violencia");
        sugerencia.Orden.Should().Be(5);
        sugerencia.Activo.Should().BeTrue();
        sugerencia.UsuarioId.Should().Be(userId);
        sugerencia.CreatedAt.Should().Be(createdAt);
    }

    #endregion

    #region Filtering by Campo Tests

    [Fact(DisplayName = "CatalogoSugerencia should be filterable by Campo")]
    public void CatalogoSugerencia_ShouldBeFilterableByCampo()
    {
        // Arrange
        var sugerencias = new List<CatalogoSugerencia>
        {
            new() { Campo = "sexo", Valor = "Masculino" },
            new() { Campo = "sexo", Valor = "Femenino" },
            new() { Campo = "delito", Valor = "Robo" },
            new() { Campo = "delito", Valor = "Vandalismo" },
            new() { Campo = "tipo_de_atencion", Valor = "Orientación" }
        };

        // Act
        var sexoSugerencias = sugerencias.Where(s => s.Campo == "sexo").ToList();
        var delitoSugerencias = sugerencias.Where(s => s.Campo == "delito").ToList();
        var tipoAtencionSugerencias = sugerencias.Where(s => s.Campo == "tipo_de_atencion").ToList();

        // Assert
        sexoSugerencias.Should().HaveCount(2);
        delitoSugerencias.Should().HaveCount(2);
        tipoAtencionSugerencias.Should().HaveCount(1);
    }

    #endregion

    #region Valor Length Tests

    [Fact(DisplayName = "CatalogoSugerencia Valor should handle long text")]
    public void CatalogoSugerencia_Valor_ShouldHandleLongText()
    {
        // Arrange
        var sugerencia = new CatalogoSugerencia();
        var longValue = "Robo a Casa Habitación con Violencia en Zona Residencial de Alta Plusvalía";

        // Act
        sugerencia.Valor = longValue;

        // Assert
        sugerencia.Valor.Should().Be(longValue);
        sugerencia.Valor.Length.Should().BeLessThanOrEqualTo(200);
    }

    #endregion

    #region Inheritance Tests

    [Fact(DisplayName = "CatalogoSugerencia should inherit from BaseCatalogEntity")]
    public void CatalogoSugerencia_ShouldInheritFromBaseCatalogEntity()
    {
        // Arrange & Act
        var sugerencia = new CatalogoSugerencia();

        // Assert
        sugerencia.Should().BeAssignableTo<BaseCatalogEntity>();
    }

    [Fact(DisplayName = "CatalogoSugerencia should allow setting UsuarioId")]
    public void CatalogoSugerencia_ShouldAllowSettingUsuarioId()
    {
        // Arrange
        var sugerencia = new CatalogoSugerencia();
        var userId = Guid.NewGuid();

        // Act
        sugerencia.UsuarioId = userId;

        // Assert
        sugerencia.UsuarioId.Should().Be(userId);
    }

    #endregion
}
