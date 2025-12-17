using System.ComponentModel.DataAnnotations;
using Ceiba.Shared.DTOs;
using FluentAssertions;

namespace Ceiba.Core.Tests.DTOs;

/// <summary>
/// Unit tests for Report DTOs (CreateReportDto, UpdateReportDto, ReportDto, etc.).
/// </summary>
[Trait("Category", "Unit")]
public class ReportDTOsTests
{
    #region CreateReportDto Tests

    [Fact(DisplayName = "CreateReportDto should have default TipoReporte as A")]
    public void CreateReportDto_ShouldHaveDefaultTipoReporteAsA()
    {
        // Arrange & Act
        var dto = new CreateReportDto();

        // Assert
        dto.TipoReporte.Should().Be("A");
    }

    [Fact(DisplayName = "CreateReportDto should have default boolean flags as false")]
    public void CreateReportDto_ShouldHaveDefaultBooleanFlagsAsFalse()
    {
        // Arrange & Act
        var dto = new CreateReportDto();

        // Assert
        dto.LgbtttiqPlus.Should().BeFalse();
        dto.SituacionCalle.Should().BeFalse();
        dto.Migrante.Should().BeFalse();
        dto.Discapacidad.Should().BeFalse();
    }

    [Fact(DisplayName = "CreateReportDto DatetimeHechos should accept valid datetime")]
    public void CreateReportDto_DatetimeHechos_ShouldAcceptValidDatetime()
    {
        // Arrange
        var dto = CreateValidReportDto();
        var validDate = DateTime.UtcNow.AddDays(-1);
        dto.DatetimeHechos = validDate;

        // Act
        var results = ValidateModel(dto);

        // Assert
        // Note: Required attribute on DateTime value type doesn't prevent default value
        // Business validation should handle DateTime.MinValue if needed
        results.Should().NotContain(r => r.MemberNames.Contains("DatetimeHechos"));
        dto.DatetimeHechos.Should().Be(validDate);
    }

    [Fact(DisplayName = "CreateReportDto validation should require Sexo")]
    public void CreateReportDto_Validation_ShouldRequireSexo()
    {
        // Arrange
        var dto = CreateValidReportDto();
        dto.Sexo = "";

        // Act
        var results = ValidateModel(dto);

        // Assert
        results.Should().Contain(r => r.MemberNames.Contains("Sexo"));
    }

    [Theory(DisplayName = "CreateReportDto validation should enforce Edad range")]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(150)]
    [InlineData(200)]
    public void CreateReportDto_Validation_ShouldEnforceEdadRange(int invalidAge)
    {
        // Arrange
        var dto = CreateValidReportDto();
        dto.Edad = invalidAge;

        // Act
        var results = ValidateModel(dto);

        // Assert
        results.Should().Contain(r => r.MemberNames.Contains("Edad"));
    }

    [Theory(DisplayName = "CreateReportDto validation should accept valid Edad")]
    [InlineData(1)]
    [InlineData(25)]
    [InlineData(65)]
    [InlineData(149)]
    public void CreateReportDto_Validation_ShouldAcceptValidEdad(int validAge)
    {
        // Arrange
        var dto = CreateValidReportDto();
        dto.Edad = validAge;

        // Act
        var results = ValidateModel(dto);

        // Assert
        results.Should().NotContain(r => r.MemberNames.Contains("Edad"));
    }

    [Fact(DisplayName = "CreateReportDto validation should require geographic IDs")]
    public void CreateReportDto_Validation_ShouldRequireGeographicIds()
    {
        // Arrange
        var dto = CreateValidReportDto();
        dto.ZonaId = 0;
        dto.RegionId = 0;
        dto.SectorId = 0;
        dto.CuadranteId = 0;

        // Act
        var results = ValidateModel(dto);

        // Assert
        results.Should().Contain(r => r.MemberNames.Contains("ZonaId"));
        results.Should().Contain(r => r.MemberNames.Contains("RegionId"));
        results.Should().Contain(r => r.MemberNames.Contains("SectorId"));
        results.Should().Contain(r => r.MemberNames.Contains("CuadranteId"));
    }

    [Theory(DisplayName = "CreateReportDto validation should enforce TipoDeAccion is not empty")]
    [InlineData("")]
    [InlineData(null)]
    public void CreateReportDto_Validation_ShouldEnforceTipoDeAccionNotEmpty(string? invalidValue)
    {
        // Arrange
        var dto = CreateValidReportDto();
        dto.TipoDeAccion = invalidValue ?? string.Empty;

        // Act
        var results = ValidateModel(dto);

        // Assert
        results.Should().Contain(r => r.MemberNames.Contains("TipoDeAccion"));
    }

    [Fact(DisplayName = "CreateReportDto validation should require Traslados")]
    public void CreateReportDto_Validation_ShouldRequireTraslados()
    {
        // Arrange
        var dto = CreateValidReportDto();
        dto.Traslados = "";

        // Act
        var results = ValidateModel(dto);

        // Assert
        results.Should().Contain(r => r.MemberNames.Contains("Traslados"));
    }

    [Fact(DisplayName = "CreateReportDto validation should require minimum length for HechosReportados")]
    public void CreateReportDto_Validation_ShouldRequireMinLengthForHechosReportados()
    {
        // Arrange
        var dto = CreateValidReportDto();
        dto.HechosReportados = "short";

        // Act
        var results = ValidateModel(dto);

        // Assert
        results.Should().Contain(r => r.MemberNames.Contains("HechosReportados"));
    }

    [Fact(DisplayName = "CreateReportDto validation should require minimum length for AccionesRealizadas")]
    public void CreateReportDto_Validation_ShouldRequireMinLengthForAccionesRealizadas()
    {
        // Arrange
        var dto = CreateValidReportDto();
        dto.AccionesRealizadas = "short";

        // Act
        var results = ValidateModel(dto);

        // Assert
        results.Should().Contain(r => r.MemberNames.Contains("AccionesRealizadas"));
    }

    [Fact(DisplayName = "CreateReportDto Observaciones should be optional")]
    public void CreateReportDto_Observaciones_ShouldBeOptional()
    {
        // Arrange
        var dto = CreateValidReportDto();
        dto.Observaciones = null;

        // Act
        var results = ValidateModel(dto);

        // Assert
        results.Should().NotContain(r => r.MemberNames.Contains("Observaciones"));
    }

    [Fact(DisplayName = "CreateReportDto validation should pass for valid data")]
    public void CreateReportDto_Validation_ShouldPassForValidData()
    {
        // Arrange
        var dto = CreateValidReportDto();

        // Act
        var results = ValidateModel(dto);

        // Assert
        results.Should().BeEmpty();
    }

    #endregion

    #region UpdateReportDto Tests

    [Fact(DisplayName = "UpdateReportDto should have all nullable properties")]
    public void UpdateReportDto_ShouldHaveAllNullableProperties()
    {
        // Arrange & Act
        var dto = new UpdateReportDto();

        // Assert
        dto.DatetimeHechos.Should().BeNull();
        dto.Sexo.Should().BeNull();
        dto.Edad.Should().BeNull();
        dto.LgbtttiqPlus.Should().BeNull();
        dto.SituacionCalle.Should().BeNull();
        dto.Migrante.Should().BeNull();
        dto.Discapacidad.Should().BeNull();
        dto.Delito.Should().BeNull();
        dto.ZonaId.Should().BeNull();
        dto.RegionId.Should().BeNull();
        dto.SectorId.Should().BeNull();
        dto.CuadranteId.Should().BeNull();
        dto.TurnoCeiba.Should().BeNull();
        dto.TipoDeAtencion.Should().BeNull();
        dto.TipoDeAccion.Should().BeNull();
        dto.HechosReportados.Should().BeNull();
        dto.AccionesRealizadas.Should().BeNull();
        dto.Traslados.Should().BeNull();
        dto.Observaciones.Should().BeNull();
    }

    [Fact(DisplayName = "UpdateReportDto validation should enforce Edad range when provided")]
    public void UpdateReportDto_Validation_ShouldEnforceEdadRangeWhenProvided()
    {
        // Arrange
        var dto = new UpdateReportDto { Edad = 0 };

        // Act
        var results = ValidateModel(dto);

        // Assert
        results.Should().Contain(r => r.MemberNames.Contains("Edad"));
    }

    [Fact(DisplayName = "UpdateReportDto validation should pass when Edad is null")]
    public void UpdateReportDto_Validation_ShouldPassWhenEdadIsNull()
    {
        // Arrange
        var dto = new UpdateReportDto { Edad = null };

        // Act
        var results = ValidateModel(dto);

        // Assert
        results.Should().NotContain(r => r.MemberNames.Contains("Edad"));
    }

    [Fact(DisplayName = "UpdateReportDto should allow partial updates")]
    public void UpdateReportDto_ShouldAllowPartialUpdates()
    {
        // Arrange & Act
        var dto = new UpdateReportDto
        {
            Sexo = "Masculino",
            Edad = 30
        };

        // Assert
        dto.Sexo.Should().Be("Masculino");
        dto.Edad.Should().Be(30);
        dto.Delito.Should().BeNull();
        dto.ZonaId.Should().BeNull();
    }

    #endregion

    #region ReportDto Tests

    [Fact(DisplayName = "ReportDto should have default values")]
    public void ReportDto_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var dto = new ReportDto();

        // Assert
        dto.Id.Should().Be(0);
        dto.TipoReporte.Should().BeEmpty();
        dto.Estado.Should().Be(0);
        dto.UsuarioId.Should().Be(Guid.Empty);
        dto.Sexo.Should().BeEmpty();
        dto.Delito.Should().BeEmpty();
    }

    [Fact(DisplayName = "ReportDto should store complete report data")]
    public void ReportDto_ShouldStoreCompleteReportData()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var fechaHechos = DateTime.UtcNow.AddDays(-1);

        // Act
        var dto = new ReportDto
        {
            Id = 100,
            TipoReporte = "A",
            Estado = 1,
            UsuarioId = userId,
            UsuarioEmail = "officer@example.com",
            CreatedAt = createdAt,
            DatetimeHechos = fechaHechos,
            Sexo = "Masculino",
            Edad = 35,
            LgbtttiqPlus = false,
            Migrante = true,
            Delito = "Robo",
            Zona = new CatalogItemDto { Id = 1, Nombre = "Zona Norte" },
            Region = new CatalogItemDto { Id = 2, Nombre = "Región Centro" },
            Sector = new CatalogItemDto { Id = 3, Nombre = "Sector A" },
            Cuadrante = new CatalogItemDto { Id = 4, Nombre = "Cuadrante 1" },
            TurnoCeiba = "Balderas 1",
            TipoDeAtencion = "Orientación",
            TipoDeAccion = "Preventiva",
            HechosReportados = "Se reporta robo en vía pública",
            AccionesRealizadas = "Se brindó orientación a la víctima",
            Traslados = "No"
        };

        // Assert
        dto.Id.Should().Be(100);
        dto.Estado.Should().Be(1);
        dto.UsuarioId.Should().Be(userId);
        dto.Zona.Nombre.Should().Be("Zona Norte");
        dto.Migrante.Should().BeTrue();
    }

    #endregion

    #region CatalogItemDto Tests

    [Fact(DisplayName = "CatalogItemDto should have default values")]
    public void CatalogItemDto_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var dto = new CatalogItemDto();

        // Assert
        dto.Id.Should().Be(0);
        dto.Nombre.Should().BeEmpty();
    }

    [Fact(DisplayName = "CatalogItemDto should store catalog item data")]
    public void CatalogItemDto_ShouldStoreCatalogItemData()
    {
        // Arrange & Act
        var dto = new CatalogItemDto
        {
            Id = 5,
            Nombre = "Zona Norte"
        };

        // Assert
        dto.Id.Should().Be(5);
        dto.Nombre.Should().Be("Zona Norte");
    }

    #endregion

    #region ReportFilterDto Tests

    [Fact(DisplayName = "ReportFilterDto should have default pagination values")]
    public void ReportFilterDto_ShouldHaveDefaultPaginationValues()
    {
        // Arrange & Act
        var filter = new ReportFilterDto();

        // Assert
        filter.Page.Should().Be(1);
        filter.PageSize.Should().Be(20);
        filter.SortBy.Should().Be("createdAt");
        filter.SortDesc.Should().BeTrue();
    }

    [Fact(DisplayName = "ReportFilterDto should have nullable filter properties")]
    public void ReportFilterDto_ShouldHaveNullableFilterProperties()
    {
        // Arrange & Act
        var filter = new ReportFilterDto();

        // Assert
        filter.Estado.Should().BeNull();
        filter.ZonaId.Should().BeNull();
        filter.Delito.Should().BeNull();
        filter.FechaDesde.Should().BeNull();
        filter.FechaHasta.Should().BeNull();
    }

    [Fact(DisplayName = "ReportFilterDto should allow setting all filter options")]
    public void ReportFilterDto_ShouldAllowSettingAllFilterOptions()
    {
        // Arrange
        var fechaDesde = DateTime.UtcNow.AddDays(-30);
        var fechaHasta = DateTime.UtcNow;

        // Act
        var filter = new ReportFilterDto
        {
            Estado = 1,
            ZonaId = 5,
            Delito = "Robo",
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta,
            Page = 2,
            PageSize = 50,
            SortBy = "fechaHechos",
            SortDesc = false
        };

        // Assert
        filter.Estado.Should().Be(1);
        filter.ZonaId.Should().Be(5);
        filter.Delito.Should().Be("Robo");
        filter.FechaDesde.Should().Be(fechaDesde);
        filter.Page.Should().Be(2);
        filter.PageSize.Should().Be(50);
        filter.SortDesc.Should().BeFalse();
    }

    #endregion

    #region ReportListResponse Tests

    [Fact(DisplayName = "ReportListResponse should have default values")]
    public void ReportListResponse_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var response = new ReportListResponse();

        // Assert
        response.Items.Should().NotBeNull().And.BeEmpty();
        response.TotalCount.Should().Be(0);
        response.Page.Should().Be(0);
        response.PageSize.Should().Be(0);
    }

    [Fact(DisplayName = "ReportListResponse TotalPages should calculate correctly")]
    public void ReportListResponse_TotalPages_ShouldCalculateCorrectly()
    {
        // Arrange & Act
        var response = new ReportListResponse
        {
            TotalCount = 95,
            PageSize = 20
        };

        // Assert
        response.TotalPages.Should().Be(5);
    }

    [Fact(DisplayName = "ReportListResponse TotalPages should be 1 for count less than page size")]
    public void ReportListResponse_TotalPages_ShouldBe1ForCountLessThanPageSize()
    {
        // Arrange & Act
        var response = new ReportListResponse
        {
            TotalCount = 10,
            PageSize = 20
        };

        // Assert
        response.TotalPages.Should().Be(1);
    }

    [Fact(DisplayName = "ReportListResponse TotalPages should handle exact division")]
    public void ReportListResponse_TotalPages_ShouldHandleExactDivision()
    {
        // Arrange & Act
        var response = new ReportListResponse
        {
            TotalCount = 100,
            PageSize = 20
        };

        // Assert
        response.TotalPages.Should().Be(5);
    }

    #endregion

    #region Helper Methods

    private static CreateReportDto CreateValidReportDto()
    {
        return new CreateReportDto
        {
            DatetimeHechos = DateTime.UtcNow,
            Sexo = "Masculino",
            Edad = 30,
            Delito = "Robo",
            ZonaId = 1,
            RegionId = 1,
            SectorId = 1,
            CuadranteId = 1,
            TurnoCeiba = "Balderas 1",
            TipoDeAtencion = "Orientación",
            TipoDeAccion = "Preventiva",
            HechosReportados = "Se reporta un robo en vía pública cerca del parque central",
            AccionesRealizadas = "Se brindó orientación a la víctima y se canalizó al MP",
            Traslados = "No"
        };
    }

    private static List<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(model);
        Validator.TryValidateObject(model, context, validationResults, true);
        return validationResults;
    }

    #endregion
}
