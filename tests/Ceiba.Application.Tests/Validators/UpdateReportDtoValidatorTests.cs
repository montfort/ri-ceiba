using Ceiba.Application.Validators;
using Ceiba.Shared.DTOs;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace Ceiba.Application.Tests.Validators;

/// <summary>
/// Unit tests for UpdateReportDtoValidator.
/// Tests all validation rules for UpdateReportDto.
/// US1: T038
/// </summary>
public class UpdateReportDtoValidatorTests
{
    private readonly UpdateReportDtoValidator _validator;

    public UpdateReportDtoValidatorTests()
    {
        _validator = new UpdateReportDtoValidator();
    }

    #region DatetimeHechos Tests

    [Fact(DisplayName = "DatetimeHechos should be optional")]
    public void DatetimeHechos_WhenNull_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { DatetimeHechos = null };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DatetimeHechos);
    }

    [Fact(DisplayName = "DatetimeHechos should not be in the future when provided")]
    public void DatetimeHechos_WhenFutureDate_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { DatetimeHechos = DateTime.UtcNow.AddDays(1) };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DatetimeHechos)
            .WithErrorMessage("La fecha de los hechos no puede ser futura");
    }

    [Fact(DisplayName = "DatetimeHechos should accept current time")]
    public void DatetimeHechos_WhenCurrentTime_ShouldNotHaveValidationError()
    {
        // Arrange
        // Use a slightly past time to avoid timing issues with validation
        var dto = new UpdateReportDto { DatetimeHechos = DateTime.UtcNow.AddSeconds(-1) };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DatetimeHechos);
    }

    [Fact(DisplayName = "DatetimeHechos should accept past dates")]
    public void DatetimeHechos_WhenPastDate_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { DatetimeHechos = DateTime.UtcNow.AddHours(-2) };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DatetimeHechos);
    }

    #endregion

    #region Sexo Tests

    [Fact(DisplayName = "Sexo should be optional")]
    public void Sexo_WhenNull_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { Sexo = null };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Sexo);
    }

    [Fact(DisplayName = "Sexo should accept empty string")]
    public void Sexo_WhenEmpty_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { Sexo = string.Empty };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Sexo);
    }

    [Fact(DisplayName = "Sexo should accept whitespace")]
    public void Sexo_WhenWhitespace_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { Sexo = "   " };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Sexo);
    }

    [Fact(DisplayName = "Sexo should not exceed 50 characters when provided")]
    public void Sexo_WhenTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { Sexo = new string('A', 51) };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Sexo)
            .WithErrorMessage("El sexo no puede exceder 50 caracteres");
    }

    [Fact(DisplayName = "Sexo should accept valid values")]
    public void Sexo_WhenValid_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { Sexo = "Masculino" };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Sexo);
    }

    [Fact(DisplayName = "Sexo should accept exactly 50 characters")]
    public void Sexo_WhenExactly50Characters_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { Sexo = new string('A', 50) };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Sexo);
    }

    #endregion

    #region Edad Tests

    [Fact(DisplayName = "Edad should be optional")]
    public void Edad_WhenNull_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { Edad = null };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Edad);
    }

    [Fact(DisplayName = "Edad should be between 1 and 149 when provided")]
    public void Edad_WhenZero_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { Edad = 0 };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Edad)
            .WithErrorMessage("La edad debe estar entre 1 y 149");
    }

    [Fact(DisplayName = "Edad should not be negative")]
    public void Edad_WhenNegative_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { Edad = -1 };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Edad)
            .WithErrorMessage("La edad debe estar entre 1 y 149");
    }

    [Fact(DisplayName = "Edad should not exceed 149")]
    public void Edad_WhenTooHigh_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { Edad = 150 };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Edad)
            .WithErrorMessage("La edad debe estar entre 1 y 149");
    }

    [Fact(DisplayName = "Edad should accept minimum value of 1")]
    public void Edad_WhenOne_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { Edad = 1 };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Edad);
    }

    [Fact(DisplayName = "Edad should accept maximum value of 149")]
    public void Edad_WhenMaximum_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { Edad = 149 };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Edad);
    }

    [Fact(DisplayName = "Edad should accept typical values")]
    public void Edad_WhenTypicalValue_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { Edad = 42 };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Edad);
    }

    #endregion

    #region Delito Tests

    [Fact(DisplayName = "Delito should be optional")]
    public void Delito_WhenNull_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { Delito = null };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Delito);
    }

    [Fact(DisplayName = "Delito should accept empty string")]
    public void Delito_WhenEmpty_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { Delito = string.Empty };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Delito);
    }

    [Fact(DisplayName = "Delito should not exceed 100 characters when provided")]
    public void Delito_WhenTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { Delito = new string('A', 101) };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Delito)
            .WithErrorMessage("El delito no puede exceder 100 caracteres");
    }

    [Fact(DisplayName = "Delito should accept valid values")]
    public void Delito_WhenValid_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { Delito = "Robo con violencia" };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Delito);
    }

    [Fact(DisplayName = "Delito should accept exactly 100 characters")]
    public void Delito_WhenExactly100Characters_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { Delito = new string('A', 100) };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Delito);
    }

    #endregion

    #region Geographic Catalog Tests (ZonaId, SectorId, CuadranteId)

    [Fact(DisplayName = "ZonaId should be optional")]
    public void ZonaId_WhenNull_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { ZonaId = null };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ZonaId);
    }

    [Fact(DisplayName = "ZonaId should be greater than 0 when provided")]
    public void ZonaId_WhenZero_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { ZonaId = 0 };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ZonaId)
            .WithErrorMessage("Debe seleccionar una zona válida");
    }

    [Fact(DisplayName = "ZonaId should not be negative")]
    public void ZonaId_WhenNegative_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { ZonaId = -1 };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ZonaId)
            .WithErrorMessage("Debe seleccionar una zona válida");
    }

    [Fact(DisplayName = "ZonaId should accept positive values")]
    public void ZonaId_WhenPositive_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { ZonaId = 1 };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ZonaId);
    }

    [Fact(DisplayName = "SectorId should be optional")]
    public void SectorId_WhenNull_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { SectorId = null };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SectorId);
    }

    [Fact(DisplayName = "SectorId should be greater than 0 when provided")]
    public void SectorId_WhenZero_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { SectorId = 0 };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SectorId)
            .WithErrorMessage("Debe seleccionar un sector válido");
    }

    [Fact(DisplayName = "SectorId should not be negative")]
    public void SectorId_WhenNegative_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { SectorId = -1 };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SectorId)
            .WithErrorMessage("Debe seleccionar un sector válido");
    }

    [Fact(DisplayName = "SectorId should accept positive values")]
    public void SectorId_WhenPositive_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { SectorId = 1 };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SectorId);
    }

    [Fact(DisplayName = "CuadranteId should be optional")]
    public void CuadranteId_WhenNull_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { CuadranteId = null };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CuadranteId);
    }

    [Fact(DisplayName = "CuadranteId should be greater than 0 when provided")]
    public void CuadranteId_WhenZero_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { CuadranteId = 0 };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CuadranteId)
            .WithErrorMessage("Debe seleccionar un cuadrante válido");
    }

    [Fact(DisplayName = "CuadranteId should not be negative")]
    public void CuadranteId_WhenNegative_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { CuadranteId = -1 };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CuadranteId)
            .WithErrorMessage("Debe seleccionar un cuadrante válido");
    }

    [Fact(DisplayName = "CuadranteId should accept positive values")]
    public void CuadranteId_WhenPositive_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { CuadranteId = 1 };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CuadranteId);
    }

    #endregion

    #region TurnoCeiba Tests

    [Fact(DisplayName = "TurnoCeiba should be optional")]
    public void TurnoCeiba_WhenNull_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { TurnoCeiba = null };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TurnoCeiba);
    }

    [Fact(DisplayName = "TurnoCeiba should not exceed max length when provided")]
    public void TurnoCeiba_WhenTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { TurnoCeiba = new string('x', 101) };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TurnoCeiba)
            .WithErrorMessage("El turno CEIBA no puede exceder 100 caracteres");
    }

    [Fact(DisplayName = "TurnoCeiba should accept valid value when provided")]
    public void TurnoCeiba_WhenValid_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { TurnoCeiba = "Balderas 1" };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TurnoCeiba);
    }

    #endregion

    #region TipoDeAtencion Tests

    [Fact(DisplayName = "TipoDeAtencion should be optional")]
    public void TipoDeAtencion_WhenNull_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { TipoDeAtencion = null };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TipoDeAtencion);
    }

    [Fact(DisplayName = "TipoDeAtencion should accept empty string")]
    public void TipoDeAtencion_WhenEmpty_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { TipoDeAtencion = string.Empty };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TipoDeAtencion);
    }

    [Fact(DisplayName = "TipoDeAtencion should not exceed 100 characters when provided")]
    public void TipoDeAtencion_WhenTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { TipoDeAtencion = new string('A', 101) };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TipoDeAtencion)
            .WithErrorMessage("El tipo de atención no puede exceder 100 caracteres");
    }

    [Fact(DisplayName = "TipoDeAtencion should accept valid values")]
    public void TipoDeAtencion_WhenValid_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { TipoDeAtencion = "Telefónica" };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TipoDeAtencion);
    }

    #endregion

    #region TipoDeAccion Tests

    [Fact(DisplayName = "TipoDeAccion should be optional")]
    public void TipoDeAccion_WhenNull_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { TipoDeAccion = null };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TipoDeAccion);
    }

    [Fact(DisplayName = "TipoDeAccion should not exceed 500 characters when provided")]
    public void TipoDeAccion_WhenTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { TipoDeAccion = new string('x', 501) };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TipoDeAccion)
            .WithErrorMessage("El tipo de acción no puede exceder 500 caracteres");
    }

    [Theory(DisplayName = "TipoDeAccion should accept valid text values")]
    [InlineData("Preventiva")]
    [InlineData("Reactiva")]
    [InlineData("Seguimiento")]
    [InlineData("Orientación y apoyo")]
    public void TipoDeAccion_WhenValidValue_ShouldNotHaveValidationError(string tipoDeAccion)
    {
        // Arrange
        var dto = new UpdateReportDto { TipoDeAccion = tipoDeAccion };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TipoDeAccion);
    }

    #endregion

    #region HechosReportados Tests

    [Fact(DisplayName = "HechosReportados should be optional")]
    public void HechosReportados_WhenNull_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { HechosReportados = null };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.HechosReportados);
    }

    [Fact(DisplayName = "HechosReportados should accept empty string")]
    public void HechosReportados_WhenEmpty_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { HechosReportados = string.Empty };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.HechosReportados);
    }

    [Fact(DisplayName = "HechosReportados should have minimum length of 10 when provided")]
    public void HechosReportados_WhenTooShort_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { HechosReportados = "Corto" };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.HechosReportados)
            .WithErrorMessage("Los hechos reportados deben tener al menos 10 caracteres");
    }

    [Fact(DisplayName = "HechosReportados should not exceed 10000 characters")]
    public void HechosReportados_WhenTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { HechosReportados = new string('A', 10001) };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.HechosReportados)
            .WithErrorMessage("Los hechos reportados no pueden exceder 10,000 caracteres");
    }

    [Fact(DisplayName = "HechosReportados should accept valid length")]
    public void HechosReportados_WhenValidLength_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto
        {
            HechosReportados = "Descripción actualizada de los hechos con más detalles."
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.HechosReportados);
    }

    [Fact(DisplayName = "HechosReportados should accept exactly 10 characters")]
    public void HechosReportados_WhenExactlyMinimumLength_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { HechosReportados = new string('A', 10) };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.HechosReportados);
    }

    [Fact(DisplayName = "HechosReportados should accept exactly 10000 characters")]
    public void HechosReportados_WhenExactlyMaximumLength_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { HechosReportados = new string('A', 10000) };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.HechosReportados);
    }

    #endregion

    #region AccionesRealizadas Tests

    [Fact(DisplayName = "AccionesRealizadas should be optional")]
    public void AccionesRealizadas_WhenNull_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { AccionesRealizadas = null };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.AccionesRealizadas);
    }

    [Fact(DisplayName = "AccionesRealizadas should accept empty string")]
    public void AccionesRealizadas_WhenEmpty_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { AccionesRealizadas = string.Empty };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.AccionesRealizadas);
    }

    [Fact(DisplayName = "AccionesRealizadas should have minimum length of 10 when provided")]
    public void AccionesRealizadas_WhenTooShort_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { AccionesRealizadas = "Breve" };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AccionesRealizadas)
            .WithErrorMessage("Las acciones realizadas deben tener al menos 10 caracteres");
    }

    [Fact(DisplayName = "AccionesRealizadas should not exceed 10000 characters")]
    public void AccionesRealizadas_WhenTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { AccionesRealizadas = new string('A', 10001) };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AccionesRealizadas)
            .WithErrorMessage("Las acciones realizadas no pueden exceder 10,000 caracteres");
    }

    [Fact(DisplayName = "AccionesRealizadas should accept valid length")]
    public void AccionesRealizadas_WhenValidLength_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto
        {
            AccionesRealizadas = "Descripción actualizada de las acciones realizadas."
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.AccionesRealizadas);
    }

    [Fact(DisplayName = "AccionesRealizadas should accept exactly 10 characters")]
    public void AccionesRealizadas_WhenExactlyMinimumLength_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { AccionesRealizadas = new string('A', 10) };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.AccionesRealizadas);
    }

    [Fact(DisplayName = "AccionesRealizadas should accept exactly 10000 characters")]
    public void AccionesRealizadas_WhenExactlyMaximumLength_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { AccionesRealizadas = new string('A', 10000) };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.AccionesRealizadas);
    }

    #endregion

    #region Traslados Tests

    [Fact(DisplayName = "Traslados should be optional")]
    public void Traslados_WhenNull_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { Traslados = null };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Traslados);
    }

    [Fact(DisplayName = "Traslados should not exceed max length when provided")]
    public void Traslados_WhenTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { Traslados = new string('x', 101) };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Traslados)
            .WithErrorMessage("Traslados no puede exceder 100 caracteres");
    }

    [Theory(DisplayName = "Traslados should accept valid values")]
    [InlineData("Sí")]
    [InlineData("No")]
    [InlineData("No aplica")]
    public void Traslados_WhenValidValue_ShouldNotHaveValidationError(string traslados)
    {
        // Arrange
        var dto = new UpdateReportDto { Traslados = traslados };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Traslados);
    }

    #endregion

    #region Observaciones Tests

    [Fact(DisplayName = "Observaciones should be optional")]
    public void Observaciones_WhenNull_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { Observaciones = null };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Observaciones);
    }

    [Fact(DisplayName = "Observaciones should accept empty string")]
    public void Observaciones_WhenEmpty_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { Observaciones = string.Empty };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Observaciones);
    }

    [Fact(DisplayName = "Observaciones should not exceed 5000 characters when provided")]
    public void Observaciones_WhenTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { Observaciones = new string('A', 5001) };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Observaciones)
            .WithErrorMessage("Las observaciones no pueden exceder 5,000 caracteres");
    }

    [Fact(DisplayName = "Observaciones should accept valid length")]
    public void Observaciones_WhenValidLength_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { Observaciones = "Observaciones actualizadas del reporte" };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Observaciones);
    }

    [Fact(DisplayName = "Observaciones should accept exactly 5000 characters")]
    public void Observaciones_WhenExactlyMaximumLength_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto { Observaciones = new string('A', 5000) };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Observaciones);
    }

    #endregion

    #region Boolean Fields Tests

    [Fact(DisplayName = "Boolean fields should be optional")]
    public void BooleanFields_WhenNull_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto
        {
            LgbtttiqPlus = null,
            SituacionCalle = null,
            Migrante = null,
            Discapacidad = null
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact(DisplayName = "Boolean fields should accept true values")]
    public void BooleanFields_WhenTrue_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto
        {
            LgbtttiqPlus = true,
            SituacionCalle = true,
            Migrante = true,
            Discapacidad = true
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact(DisplayName = "Boolean fields should accept false values")]
    public void BooleanFields_WhenFalse_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new UpdateReportDto
        {
            LgbtttiqPlus = false,
            SituacionCalle = false,
            Migrante = false,
            Discapacidad = false
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region Complete Object Validation

    [Fact(DisplayName = "Empty DTO should pass all validations")]
    public void CompleteDto_WhenAllFieldsNull_ShouldNotHaveAnyValidationErrors()
    {
        // Arrange
        var dto = new UpdateReportDto();

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact(DisplayName = "Valid partial update should pass validations")]
    public void CompleteDto_WhenPartiallyFilled_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var dto = new UpdateReportDto
        {
            Sexo = "Masculino",
            Edad = 35,
            Observaciones = "Modificado por supervisor"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact(DisplayName = "Valid complete update should pass all validations")]
    public void CompleteDto_WhenAllFieldsValid_ShouldNotHaveAnyValidationErrors()
    {
        // Arrange
        var dto = new UpdateReportDto
        {
            DatetimeHechos = DateTime.UtcNow.AddHours(-1),
            Sexo = "Masculino",
            Edad = 42,
            LgbtttiqPlus = true,
            SituacionCalle = false,
            Migrante = false,
            Discapacidad = false,
            Delito = "Robo con violencia",
            ZonaId = 2,
            SectorId = 3,
            CuadranteId = 5,
            TurnoCeiba = "Balderas 2",
            TipoDeAtencion = "Telefónica",
            TipoDeAccion = "Reactiva",
            HechosReportados = "Descripción completa actualizada de los hechos reportados.",
            AccionesRealizadas = "Acciones actualizadas realizadas por el oficial.",
            Traslados = "Sí",
            Observaciones = "Observaciones actualizadas"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact(DisplayName = "Invalid DTO should report all validation errors")]
    public void CompleteDto_WhenMultipleFieldsInvalid_ShouldHaveMultipleValidationErrors()
    {
        // Arrange
        var dto = new UpdateReportDto
        {
            DatetimeHechos = DateTime.UtcNow.AddDays(1),
            Sexo = new string('A', 51),
            Edad = 200,
            Delito = new string('B', 101),
            ZonaId = 0,
            SectorId = -1,
            CuadranteId = 0,
            TurnoCeiba = new string('T', 101),
            TipoDeAtencion = new string('C', 101),
            TipoDeAccion = new string('x', 501),
            HechosReportados = "Corto",
            AccionesRealizadas = "Breve",
            Traslados = new string('R', 101),
            Observaciones = new string('D', 5001)
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThan(10);
    }

    #endregion
}
