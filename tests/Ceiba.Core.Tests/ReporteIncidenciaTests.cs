using Ceiba.Core.Entities;
using FluentAssertions;
using Xunit;

namespace Ceiba.Core.Tests;

/// <summary>
/// Unit tests for ReporteIncidencia entity (US1)
/// Tests state transitions and validation logic per T025
/// </summary>
public class ReporteIncidenciaTests
{
    #region T025: State Transition Tests

    [Fact(DisplayName = "T025: New report should be created with estado=0 (Borrador)")]
    public void NewReport_ShouldHaveEstadoBorrador()
    {
        // Arrange & Act
        var report = new ReporteIncidencia
        {
            UsuarioId = Guid.NewGuid(),
            TipoReporte = "A",
            DatetimeHechos = DateTime.UtcNow,
            Sexo = "Femenino",
            Edad = 28,
            Delito = "Violencia familiar",
            ZonaId = 1,
            SectorId = 1,
            CuadranteId = 1,
            TurnoCeiba = 1,
            TipoDeAtencion = "Presencial",
            TipoDeAccion = 1,
            HechosReportados = "Descripción de los hechos",
            AccionesRealizadas = "Acciones realizadas",
            Traslados = 0
        };

        // Assert
        report.Estado.Should().Be(0); // Default is Borrador
        report.IsBorrador().Should().BeTrue();
        report.IsEntregado().Should().BeFalse();
    }

    [Fact(DisplayName = "T025: Submit() should change estado from 0 to 1")]
    public void Submit_OnBorradorReport_ShouldChangeEstadoToEntregado()
    {
        // Arrange
        var report = new ReporteIncidencia
        {
            Estado = 0, // Borrador
            UsuarioId = Guid.NewGuid()
        };

        // Act
        report.Submit();

        // Assert
        report.Estado.Should().Be(1); // Entregado
        report.IsBorrador().Should().BeFalse();
        report.IsEntregado().Should().BeTrue();
    }

    [Fact(DisplayName = "T025: Submit() on already entregado report should throw")]
    public void Submit_OnEntregadoReport_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var report = new ReporteIncidencia
        {
            Estado = 1, // Already Entregado
            UsuarioId = Guid.NewGuid()
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => report.Submit());
    }

    [Fact(DisplayName = "T025: CanBeEditedByCreador should return true for borrador only")]
    public void CanBeEditedByCreador_ShouldBeTrueForBorradorOnly()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();
        var borradorReport = new ReporteIncidencia
        {
            Estado = 0,
            UsuarioId = usuarioId
        };
        var entregadoReport = new ReporteIncidencia
        {
            Estado = 1,
            UsuarioId = usuarioId
        };

        // Act & Assert
        borradorReport.CanBeEditedByCreador(usuarioId).Should().BeTrue();
        entregadoReport.CanBeEditedByCreador(usuarioId).Should().BeFalse();
    }

    [Fact(DisplayName = "T025: CanBeEditedByCreador should return false for other users")]
    public void CanBeEditedByCreador_ShouldBeFalseForOtherUsers()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var report = new ReporteIncidencia
        {
            Estado = 0,
            UsuarioId = ownerUserId
        };

        // Act & Assert
        report.CanBeEditedByCreador(otherUserId).Should().BeFalse();
    }

    [Fact(DisplayName = "T025: CanBeSubmittedByCreador should return true for own borrador")]
    public void CanBeSubmittedByCreador_ShouldBeTrueForOwnBorrador()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();
        var report = new ReporteIncidencia
        {
            Estado = 0,
            UsuarioId = usuarioId
        };

        // Act & Assert
        report.CanBeSubmittedByCreador(usuarioId).Should().BeTrue();
    }

    [Fact(DisplayName = "T025: CanBeSubmittedByCreador should return false for entregado")]
    public void CanBeSubmittedByCreador_ShouldBeFalseForEntregado()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();
        var report = new ReporteIncidencia
        {
            Estado = 1,
            UsuarioId = usuarioId
        };

        // Act & Assert
        report.CanBeSubmittedByCreador(usuarioId).Should().BeFalse();
    }

    #endregion

    #region T025: Validation Tests

    [Theory(DisplayName = "T025: Edad should be between 1 and 149")]
    [InlineData(0)] // Invalid: too low
    [InlineData(150)] // Invalid: too high
    [InlineData(-5)] // Invalid: negative
    public void Validate_WithInvalidEdad_ShouldFail(int edad)
    {
        // Arrange
        var report = new ReporteIncidencia
        {
            Edad = edad
        };

        // Act
        var validationResult = report.Validate();

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.Contains("edad"));
    }

    [Theory(DisplayName = "T025: Edad should accept valid range 1-149")]
    [InlineData(1)]
    [InlineData(28)]
    [InlineData(100)]
    [InlineData(149)]
    public void Validate_WithValidEdad_ShouldPass(int edad)
    {
        // Arrange
        var report = new ReporteIncidencia
        {
            UsuarioId = Guid.NewGuid(),
            TipoReporte = "A",
            DatetimeHechos = DateTime.UtcNow,
            Sexo = "Femenino",
            Edad = edad,
            Delito = "Test",
            ZonaId = 1,
            SectorId = 1,
            CuadranteId = 1,
            TurnoCeiba = 1,
            TipoDeAtencion = "Presencial",
            TipoDeAccion = 1,
            HechosReportados = "Test",
            AccionesRealizadas = "Test",
            Traslados = 0
        };

        // Act
        var validationResult = report.Validate();

        // Assert
        validationResult.IsValid.Should().BeTrue();
    }

    [Theory(DisplayName = "T025: TipoDeAccion should be 1, 2, or 3")]
    [InlineData(1)] // ATOS
    [InlineData(2)] // Capacitación
    [InlineData(3)] // Prevención
    public void Validate_WithValidTipoDeAccion_ShouldPass(int tipoDeAccion)
    {
        // Arrange
        var report = new ReporteIncidencia
        {
            TipoDeAccion = tipoDeAccion
        };

        // Act
        var validationResult = report.ValidateTipoDeAccion();

        // Assert
        validationResult.Should().BeTrue();
    }

    [Theory(DisplayName = "T025: TipoDeAccion with invalid values should fail")]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(-1)]
    public void Validate_WithInvalidTipoDeAccion_ShouldFail(int tipoDeAccion)
    {
        // Arrange
        var report = new ReporteIncidencia
        {
            TipoDeAccion = tipoDeAccion
        };

        // Act
        var validationResult = report.ValidateTipoDeAccion();

        // Assert
        validationResult.Should().BeFalse();
    }

    [Theory(DisplayName = "T025: Traslados should be 0, 1, or 2")]
    [InlineData(0)] // Sin traslados
    [InlineData(1)] // Con traslados
    [InlineData(2)] // No aplica
    public void Validate_WithValidTraslados_ShouldPass(int traslados)
    {
        // Arrange
        var report = new ReporteIncidencia
        {
            Traslados = traslados
        };

        // Act
        var validationResult = report.ValidateTraslados();

        // Assert
        validationResult.Should().BeTrue();
    }

    [Theory(DisplayName = "T025: Traslados with invalid values should fail")]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(10)]
    public void Validate_WithInvalidTraslados_ShouldFail(int traslados)
    {
        // Arrange
        var report = new ReporteIncidencia
        {
            Traslados = traslados
        };

        // Act
        var validationResult = report.ValidateTraslados();

        // Assert
        validationResult.Should().BeFalse();
    }

    [Fact(DisplayName = "T025: Required fields should not be null or empty")]
    public void Validate_WithMissingRequiredFields_ShouldFail()
    {
        // Arrange
        var report = new ReporteIncidencia
        {
            // Missing required fields
        };

        // Act
        var validationResult = report.Validate();

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().NotBeEmpty();
    }

    [Fact(DisplayName = "T025: Schema version should default to 1.0")]
    public void NewReport_ShouldHaveDefaultSchemaVersion()
    {
        // Arrange & Act
        var report = new ReporteIncidencia();

        // Assert
        report.SchemaVersion.Should().Be("1.0");
    }

    [Fact(DisplayName = "T025: CamposAdicionales should support extensibility (JSONB)")]
    public void CamposAdicionales_ShouldSupportExtensibility()
    {
        // Arrange
        var report = new ReporteIncidencia();
        var additionalData = new Dictionary<string, object>
        {
            { "campo_custom_1", "valor1" },
            { "campo_custom_2", 123 },
            { "campo_custom_3", true }
        };

        // Act
        report.SetCamposAdicionales(additionalData);

        // Assert
        report.CamposAdicionales.Should().NotBeNull();
        report.GetCamposAdicionales().Should().ContainKey("campo_custom_1");
        report.GetCamposAdicionales()["campo_custom_1"].Should().Be("valor1");
    }

    #endregion
}
