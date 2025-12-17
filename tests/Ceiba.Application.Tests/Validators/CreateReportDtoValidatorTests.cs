using Ceiba.Application.Validators;
using Ceiba.Shared.DTOs;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace Ceiba.Application.Tests.Validators;

/// <summary>
/// Unit tests for CreateReportDtoValidator.
/// Tests all validation rules for CreateReportDto.
/// US1: T038
/// </summary>
public class CreateReportDtoValidatorTests
{
    private readonly CreateReportDtoValidator _validator;

    public CreateReportDtoValidatorTests()
    {
        _validator = new CreateReportDtoValidator();
    }

    #region DatetimeHechos Tests

    [Fact(DisplayName = "DatetimeHechos should be required")]
    public void DatetimeHechos_WhenEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreateReportDto { DatetimeHechos = default };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DatetimeHechos)
            .WithErrorMessage("La fecha y hora de los hechos es requerida");
    }

    [Fact(DisplayName = "DatetimeHechos should not be in the future")]
    public void DatetimeHechos_WhenFutureDate_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.DatetimeHechos = DateTime.UtcNow.AddDays(1);

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
        var dto = CreateValidDto();
        // Use a slightly past time to avoid timing issues with validation
        dto.DatetimeHechos = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DatetimeHechos);
    }

    [Fact(DisplayName = "DatetimeHechos should accept past dates")]
    public void DatetimeHechos_WhenPastDate_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.DatetimeHechos = DateTime.UtcNow.AddHours(-2);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DatetimeHechos);
    }

    #endregion

    #region Sexo Tests

    [Fact(DisplayName = "Sexo should be required")]
    public void Sexo_WhenEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.Sexo = string.Empty;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Sexo)
            .WithErrorMessage("El sexo es requerido");
    }

    [Fact(DisplayName = "Sexo should not exceed 50 characters")]
    public void Sexo_WhenTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.Sexo = new string('A', 51);

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
        var dto = CreateValidDto();
        dto.Sexo = "Femenino";

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Sexo);
    }

    [Fact(DisplayName = "Sexo should accept exactly 50 characters")]
    public void Sexo_WhenExactly50Characters_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.Sexo = new string('A', 50);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Sexo);
    }

    #endregion

    #region Edad Tests

    [Fact(DisplayName = "Edad should be between 1 and 149")]
    public void Edad_WhenZero_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.Edad = 0;

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
        var dto = CreateValidDto();
        dto.Edad = -1;

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
        var dto = CreateValidDto();
        dto.Edad = 150;

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
        var dto = CreateValidDto();
        dto.Edad = 1;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Edad);
    }

    [Fact(DisplayName = "Edad should accept maximum value of 149")]
    public void Edad_WhenMaximum_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.Edad = 149;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Edad);
    }

    [Fact(DisplayName = "Edad should accept typical values")]
    public void Edad_WhenTypicalValue_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.Edad = 35;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Edad);
    }

    #endregion

    #region Delito Tests

    [Fact(DisplayName = "Delito should be required")]
    public void Delito_WhenEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.Delito = string.Empty;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Delito)
            .WithErrorMessage("El delito es requerido");
    }

    [Fact(DisplayName = "Delito should not exceed 100 characters")]
    public void Delito_WhenTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.Delito = new string('A', 101);

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
        var dto = CreateValidDto();
        dto.Delito = "Violencia familiar";

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Delito);
    }

    [Fact(DisplayName = "Delito should accept exactly 100 characters")]
    public void Delito_WhenExactly100Characters_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.Delito = new string('A', 100);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Delito);
    }

    #endregion

    #region Geographic Catalog Tests (ZonaId, SectorId, CuadranteId)

    [Fact(DisplayName = "ZonaId should be greater than 0")]
    public void ZonaId_WhenZero_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.ZonaId = 0;

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
        var dto = CreateValidDto();
        dto.ZonaId = -1;

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
        var dto = CreateValidDto();
        dto.ZonaId = 1;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ZonaId);
    }

    [Fact(DisplayName = "SectorId should be greater than 0")]
    public void SectorId_WhenZero_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.SectorId = 0;

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
        var dto = CreateValidDto();
        dto.SectorId = -1;

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
        var dto = CreateValidDto();
        dto.SectorId = 1;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SectorId);
    }

    [Fact(DisplayName = "CuadranteId should be greater than 0")]
    public void CuadranteId_WhenZero_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.CuadranteId = 0;

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
        var dto = CreateValidDto();
        dto.CuadranteId = -1;

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
        var dto = CreateValidDto();
        dto.CuadranteId = 1;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CuadranteId);
    }

    #endregion

    #region TurnoCeiba Tests

    [Fact(DisplayName = "TurnoCeiba should be required")]
    public void TurnoCeiba_WhenEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.TurnoCeiba = "";

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TurnoCeiba)
            .WithErrorMessage("El turno CEIBA es requerido");
    }

    [Fact(DisplayName = "TurnoCeiba should not exceed max length")]
    public void TurnoCeiba_WhenTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.TurnoCeiba = new string('x', 101);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TurnoCeiba)
            .WithErrorMessage("El turno CEIBA no puede exceder 100 caracteres");
    }

    [Fact(DisplayName = "TurnoCeiba should accept valid values")]
    public void TurnoCeiba_WhenValid_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.TurnoCeiba = "Balderas 1";

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TurnoCeiba);
    }

    #endregion

    #region TipoDeAtencion Tests

    [Fact(DisplayName = "TipoDeAtencion should be required")]
    public void TipoDeAtencion_WhenEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.TipoDeAtencion = string.Empty;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TipoDeAtencion)
            .WithErrorMessage("El tipo de atención es requerido");
    }

    [Fact(DisplayName = "TipoDeAtencion should not exceed 100 characters")]
    public void TipoDeAtencion_WhenTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.TipoDeAtencion = new string('A', 101);

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
        var dto = CreateValidDto();
        dto.TipoDeAtencion = "Presencial";

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TipoDeAtencion);
    }

    #endregion

    #region TipoDeAccion Tests

    [Fact(DisplayName = "TipoDeAccion should be required")]
    public void TipoDeAccion_WhenEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.TipoDeAccion = string.Empty;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TipoDeAccion)
            .WithErrorMessage("El tipo de acción es requerido");
    }

    [Fact(DisplayName = "TipoDeAccion should not exceed 500 characters")]
    public void TipoDeAccion_WhenTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.TipoDeAccion = new string('x', 501);

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
    [InlineData("Orientación y apoyo a la víctima")]
    public void TipoDeAccion_WhenValidValue_ShouldNotHaveValidationError(string tipoDeAccion)
    {
        // Arrange
        var dto = CreateValidDto();
        dto.TipoDeAccion = tipoDeAccion;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TipoDeAccion);
    }

    #endregion

    #region HechosReportados Tests

    [Fact(DisplayName = "HechosReportados should be required")]
    public void HechosReportados_WhenEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.HechosReportados = string.Empty;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.HechosReportados)
            .WithErrorMessage("Los hechos reportados son requeridos");
    }

    [Fact(DisplayName = "HechosReportados should have minimum length of 10")]
    public void HechosReportados_WhenTooShort_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.HechosReportados = "Corto";

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
        var dto = CreateValidDto();
        dto.HechosReportados = new string('A', 10001);

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
        var dto = CreateValidDto();
        dto.HechosReportados = "Descripción válida de los hechos reportados con suficiente detalle.";

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.HechosReportados);
    }

    [Fact(DisplayName = "HechosReportados should accept exactly 10 characters")]
    public void HechosReportados_WhenExactlyMinimumLength_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.HechosReportados = new string('A', 10);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.HechosReportados);
    }

    [Fact(DisplayName = "HechosReportados should accept exactly 10000 characters")]
    public void HechosReportados_WhenExactlyMaximumLength_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.HechosReportados = new string('A', 10000);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.HechosReportados);
    }

    #endregion

    #region AccionesRealizadas Tests

    [Fact(DisplayName = "AccionesRealizadas should be required")]
    public void AccionesRealizadas_WhenEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.AccionesRealizadas = string.Empty;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AccionesRealizadas)
            .WithErrorMessage("Las acciones realizadas son requeridas");
    }

    [Fact(DisplayName = "AccionesRealizadas should have minimum length of 10")]
    public void AccionesRealizadas_WhenTooShort_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.AccionesRealizadas = "Corto";

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
        var dto = CreateValidDto();
        dto.AccionesRealizadas = new string('A', 10001);

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
        var dto = CreateValidDto();
        dto.AccionesRealizadas = "Descripción válida de las acciones realizadas por el oficial.";

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.AccionesRealizadas);
    }

    [Fact(DisplayName = "AccionesRealizadas should accept exactly 10 characters")]
    public void AccionesRealizadas_WhenExactlyMinimumLength_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.AccionesRealizadas = new string('A', 10);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.AccionesRealizadas);
    }

    [Fact(DisplayName = "AccionesRealizadas should accept exactly 10000 characters")]
    public void AccionesRealizadas_WhenExactlyMaximumLength_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.AccionesRealizadas = new string('A', 10000);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.AccionesRealizadas);
    }

    #endregion

    #region Traslados Tests

    [Fact(DisplayName = "Traslados should be required")]
    public void Traslados_WhenEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.Traslados = "";

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Traslados)
            .WithErrorMessage("El estado de traslados es requerido");
    }

    [Fact(DisplayName = "Traslados should not exceed max length")]
    public void Traslados_WhenTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.Traslados = new string('x', 101);

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
        var dto = CreateValidDto();
        dto.Traslados = traslados;

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
        var dto = CreateValidDto();
        dto.Observaciones = null;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Observaciones);
    }

    [Fact(DisplayName = "Observaciones should accept empty string")]
    public void Observaciones_WhenEmpty_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.Observaciones = string.Empty;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Observaciones);
    }

    [Fact(DisplayName = "Observaciones should not exceed 5000 characters")]
    public void Observaciones_WhenTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.Observaciones = new string('A', 5001);

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
        var dto = CreateValidDto();
        dto.Observaciones = "Observaciones adicionales del reporte";

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Observaciones);
    }

    [Fact(DisplayName = "Observaciones should accept exactly 5000 characters")]
    public void Observaciones_WhenExactlyMaximumLength_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.Observaciones = new string('A', 5000);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Observaciones);
    }

    #endregion

    #region Complete Object Validation

    [Fact(DisplayName = "Valid DTO should pass all validations")]
    public void CompleteDto_WhenAllFieldsValid_ShouldNotHaveAnyValidationErrors()
    {
        // Arrange
        var dto = CreateValidDto();

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
        var dto = new CreateReportDto
        {
            DatetimeHechos = default,
            Sexo = string.Empty,
            Edad = 0,
            Delito = string.Empty,
            ZonaId = 0,
            SectorId = 0,
            CuadranteId = 0,
            TurnoCeiba = string.Empty,
            TipoDeAtencion = string.Empty,
            TipoDeAccion = string.Empty,
            HechosReportados = string.Empty,
            AccionesRealizadas = string.Empty,
            Traslados = string.Empty
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThan(10);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a valid CreateReportDto for testing purposes.
    /// </summary>
    private static CreateReportDto CreateValidDto()
    {
        return new CreateReportDto
        {
            TipoReporte = "A",
            DatetimeHechos = DateTime.UtcNow.AddHours(-2),
            Sexo = "Femenino",
            Edad = 28,
            LgbtttiqPlus = false,
            SituacionCalle = false,
            Migrante = false,
            Discapacidad = false,
            Delito = "Violencia familiar",
            ZonaId = 1,
            RegionId = 1,
            SectorId = 1,
            CuadranteId = 1,
            TurnoCeiba = "Balderas 1",
            TipoDeAtencion = "Presencial",
            TipoDeAccion = "Preventiva",
            HechosReportados = "Descripción detallada de los hechos reportados con suficiente información.",
            AccionesRealizadas = "Acciones realizadas por el oficial durante la atención del caso.",
            Traslados = "No",
            Observaciones = "Observaciones adicionales"
        };
    }

    #endregion
}
