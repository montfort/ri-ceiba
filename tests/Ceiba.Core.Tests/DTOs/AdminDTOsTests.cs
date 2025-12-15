using System.ComponentModel.DataAnnotations;
using Ceiba.Shared.DTOs;
using FluentAssertions;

namespace Ceiba.Core.Tests.DTOs;

/// <summary>
/// Unit tests for Admin DTOs (User Management, Catalogs, Audit).
/// </summary>
[Trait("Category", "Unit")]
public class AdminDTOsTests
{
    #region UserDto Tests

    [Fact(DisplayName = "UserDto should have default values")]
    public void UserDto_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var dto = new UserDto();

        // Assert
        dto.Id.Should().Be(Guid.Empty);
        dto.Email.Should().BeEmpty();
        dto.Nombre.Should().BeEmpty();
        dto.Roles.Should().NotBeNull().And.BeEmpty();
        dto.Activo.Should().BeFalse();
        dto.LastLogin.Should().BeNull();
    }

    [Fact(DisplayName = "UserDto should allow setting all properties")]
    public void UserDto_ShouldAllowSettingAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var lastLogin = DateTime.UtcNow.AddHours(-1);

        // Act
        var dto = new UserDto
        {
            Id = id,
            Email = "admin@example.com",
            Nombre = "Admin User",
            Roles = new List<string> { "ADMIN", "REVISOR" },
            Activo = true,
            CreatedAt = createdAt,
            LastLogin = lastLogin
        };

        // Assert
        dto.Id.Should().Be(id);
        dto.Email.Should().Be("admin@example.com");
        dto.Nombre.Should().Be("Admin User");
        dto.Roles.Should().HaveCount(2);
        dto.Activo.Should().BeTrue();
        dto.LastLogin.Should().Be(lastLogin);
    }

    #endregion

    #region CreateUserDto Tests

    [Fact(DisplayName = "CreateUserDto should have default values")]
    public void CreateUserDto_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var dto = new CreateUserDto();

        // Assert
        dto.Nombre.Should().BeEmpty();
        dto.Email.Should().BeEmpty();
        dto.Password.Should().BeEmpty();
        dto.Roles.Should().NotBeNull().And.BeEmpty();
    }

    [Fact(DisplayName = "CreateUserDto validation should fail for empty Nombre")]
    public void CreateUserDto_Validation_ShouldFailForEmptyNombre()
    {
        // Arrange
        var dto = new CreateUserDto
        {
            Nombre = "",
            Email = "test@example.com",
            Password = "ValidPassword123",
            Roles = new List<string> { "CREADOR" }
        };

        // Act
        var results = ValidateModel(dto);

        // Assert
        results.Should().Contain(r => r.MemberNames.Contains("Nombre"));
    }

    [Fact(DisplayName = "CreateUserDto validation should fail for invalid email")]
    public void CreateUserDto_Validation_ShouldFailForInvalidEmail()
    {
        // Arrange
        var dto = new CreateUserDto
        {
            Nombre = "Test User",
            Email = "invalid-email",
            Password = "ValidPassword123",
            Roles = new List<string> { "CREADOR" }
        };

        // Act
        var results = ValidateModel(dto);

        // Assert
        results.Should().Contain(r => r.MemberNames.Contains("Email"));
    }

    [Fact(DisplayName = "CreateUserDto validation should fail for short password")]
    public void CreateUserDto_Validation_ShouldFailForShortPassword()
    {
        // Arrange
        var dto = new CreateUserDto
        {
            Nombre = "Test User",
            Email = "test@example.com",
            Password = "short",
            Roles = new List<string> { "CREADOR" }
        };

        // Act
        var results = ValidateModel(dto);

        // Assert
        results.Should().Contain(r => r.MemberNames.Contains("Password"));
    }

    [Fact(DisplayName = "CreateUserDto validation should fail for empty roles")]
    public void CreateUserDto_Validation_ShouldFailForEmptyRoles()
    {
        // Arrange
        var dto = new CreateUserDto
        {
            Nombre = "Test User",
            Email = "test@example.com",
            Password = "ValidPassword123",
            Roles = new List<string>()
        };

        // Act
        var results = ValidateModel(dto);

        // Assert
        results.Should().Contain(r => r.MemberNames.Contains("Roles"));
    }

    [Fact(DisplayName = "CreateUserDto validation should pass for valid data")]
    public void CreateUserDto_Validation_ShouldPassForValidData()
    {
        // Arrange
        var dto = new CreateUserDto
        {
            Nombre = "Test User",
            Email = "test@example.com",
            Password = "ValidPassword123",
            Roles = new List<string> { "CREADOR" }
        };

        // Act
        var results = ValidateModel(dto);

        // Assert
        results.Should().BeEmpty();
    }

    #endregion

    #region UpdateUserDto Tests

    [Fact(DisplayName = "UpdateUserDto should have default Activo as true")]
    public void UpdateUserDto_ShouldHaveDefaultActivoAsTrue()
    {
        // Arrange & Act
        var dto = new UpdateUserDto();

        // Assert
        dto.Activo.Should().BeTrue();
    }

    [Fact(DisplayName = "UpdateUserDto NewPassword should be optional")]
    public void UpdateUserDto_NewPassword_ShouldBeOptional()
    {
        // Arrange
        var dto = new UpdateUserDto
        {
            Nombre = "Updated User",
            Email = "updated@example.com",
            NewPassword = null,
            Roles = new List<string> { "REVISOR" }
        };

        // Act
        var results = ValidateModel(dto);

        // Assert
        results.Should().NotContain(r => r.MemberNames.Contains("NewPassword"));
    }

    #endregion

    #region UserListResponse Tests

    [Fact(DisplayName = "UserListResponse should have default values")]
    public void UserListResponse_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var response = new UserListResponse();

        // Assert
        response.Items.Should().NotBeNull().And.BeEmpty();
        response.TotalCount.Should().Be(0);
        response.Page.Should().Be(0);
        response.PageSize.Should().Be(0);
    }

    #endregion

    #region UserFilterDto Tests

    [Fact(DisplayName = "UserFilterDto should have default pagination values")]
    public void UserFilterDto_ShouldHaveDefaultPaginationValues()
    {
        // Arrange & Act
        var filter = new UserFilterDto();

        // Assert
        filter.Page.Should().Be(1);
        filter.PageSize.Should().Be(20);
        filter.Search.Should().BeNull();
        filter.Role.Should().BeNull();
        filter.Activo.Should().BeNull();
    }

    #endregion

    #region ZonaDto Tests

    [Fact(DisplayName = "ZonaDto should have default Activo as true")]
    public void ZonaDto_ShouldHaveDefaultActivoAsTrue()
    {
        // Arrange & Act
        var dto = new ZonaDto();

        // Assert
        dto.Activo.Should().BeTrue();
    }

    [Fact(DisplayName = "ZonaDto validation should require Nombre")]
    public void ZonaDto_Validation_ShouldRequireNombre()
    {
        // Arrange
        var dto = new ZonaDto { Nombre = "" };

        // Act
        var results = ValidateModel(dto);

        // Assert
        results.Should().Contain(r => r.MemberNames.Contains("Nombre"));
    }

    #endregion

    #region CreateZonaDto Tests

    [Fact(DisplayName = "CreateZonaDto should have default Activo as true")]
    public void CreateZonaDto_ShouldHaveDefaultActivoAsTrue()
    {
        // Arrange & Act
        var dto = new CreateZonaDto();

        // Assert
        dto.Activo.Should().BeTrue();
    }

    #endregion

    #region RegionDto Tests

    [Fact(DisplayName = "RegionDto should store zone relationship")]
    public void RegionDto_ShouldStoreZoneRelationship()
    {
        // Arrange & Act
        var dto = new RegionDto
        {
            Id = 1,
            Nombre = "Región Centro",
            ZonaId = 5,
            ZonaNombre = "Zona Norte"
        };

        // Assert
        dto.ZonaId.Should().Be(5);
        dto.ZonaNombre.Should().Be("Zona Norte");
    }

    #endregion

    #region CreateRegionDto Tests

    [Fact(DisplayName = "CreateRegionDto validation should require ZonaId")]
    public void CreateRegionDto_Validation_ShouldRequireZonaId()
    {
        // Arrange
        var dto = new CreateRegionDto
        {
            Nombre = "Test Region",
            ZonaId = 0
        };

        // Act
        var results = ValidateModel(dto);

        // Assert
        results.Should().Contain(r => r.MemberNames.Contains("ZonaId"));
    }

    #endregion

    #region SectorDto Tests

    [Fact(DisplayName = "SectorDto should store full hierarchy")]
    public void SectorDto_ShouldStoreFullHierarchy()
    {
        // Arrange & Act
        var dto = new SectorDto
        {
            Id = 1,
            Nombre = "Sector A",
            RegionId = 2,
            RegionNombre = "Región Centro",
            ZonaId = 3,
            ZonaNombre = "Zona Norte"
        };

        // Assert
        dto.RegionId.Should().Be(2);
        dto.ZonaId.Should().Be(3);
        dto.RegionNombre.Should().Be("Región Centro");
        dto.ZonaNombre.Should().Be("Zona Norte");
    }

    #endregion

    #region CuadranteDto Tests

    [Fact(DisplayName = "CuadranteDto should store complete geographic hierarchy")]
    public void CuadranteDto_ShouldStoreCompleteGeographicHierarchy()
    {
        // Arrange & Act
        var dto = new CuadranteDto
        {
            Id = 1,
            Nombre = "Cuadrante 1-A",
            SectorId = 2,
            SectorNombre = "Sector A",
            RegionId = 3,
            RegionNombre = "Región Centro",
            ZonaId = 4,
            ZonaNombre = "Zona Norte"
        };

        // Assert
        dto.SectorId.Should().Be(2);
        dto.RegionId.Should().Be(3);
        dto.ZonaId.Should().Be(4);
        dto.SectorNombre.Should().Be("Sector A");
    }

    #endregion

    #region SugerenciaDto Tests

    [Fact(DisplayName = "SugerenciaDto should have default values")]
    public void SugerenciaDto_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var dto = new SugerenciaDto();

        // Assert
        dto.Orden.Should().Be(0);
        dto.Activo.Should().BeTrue();
    }

    #endregion

    #region SugerenciaCampos Tests

    [Fact(DisplayName = "SugerenciaCampos should define all field constants")]
    public void SugerenciaCampos_ShouldDefineAllFieldConstants()
    {
        // Assert
        SugerenciaCampos.Sexo.Should().Be("sexo");
        SugerenciaCampos.Delito.Should().Be("delito");
        SugerenciaCampos.TipoDeAtencion.Should().Be("tipo_de_atencion");
        SugerenciaCampos.TurnoCeiba.Should().Be("turno_ceiba");
        SugerenciaCampos.TipoDeAccion.Should().Be("tipo_de_accion");
        SugerenciaCampos.Traslados.Should().Be("traslados");
    }

    [Fact(DisplayName = "SugerenciaCampos.All should contain all fields")]
    public void SugerenciaCampos_All_ShouldContainAllFields()
    {
        // Assert
        SugerenciaCampos.All.Should().HaveCount(6);
        SugerenciaCampos.All.Should().Contain("sexo");
        SugerenciaCampos.All.Should().Contain("delito");
        SugerenciaCampos.All.Should().Contain("tipo_de_atencion");
    }

    [Fact(DisplayName = "SugerenciaCampos.DatosPersona should contain person data fields")]
    public void SugerenciaCampos_DatosPersona_ShouldContainPersonDataFields()
    {
        // Assert
        SugerenciaCampos.DatosPersona.Should().HaveCount(3);
        SugerenciaCampos.DatosPersona.Should().Contain("sexo");
        SugerenciaCampos.DatosPersona.Should().Contain("delito");
        SugerenciaCampos.DatosPersona.Should().Contain("tipo_de_atencion");
    }

    [Fact(DisplayName = "SugerenciaCampos.DetallesOperativos should contain operational fields")]
    public void SugerenciaCampos_DetallesOperativos_ShouldContainOperationalFields()
    {
        // Assert
        SugerenciaCampos.DetallesOperativos.Should().HaveCount(3);
        SugerenciaCampos.DetallesOperativos.Should().Contain("turno_ceiba");
        SugerenciaCampos.DetallesOperativos.Should().Contain("tipo_de_accion");
        SugerenciaCampos.DetallesOperativos.Should().Contain("traslados");
    }

    [Theory(DisplayName = "SugerenciaCampos.GetDisplayName should return correct names")]
    [InlineData("sexo", "Sexo")]
    [InlineData("delito", "Tipo de Delito")]
    [InlineData("tipo_de_atencion", "Tipo de Atención")]
    [InlineData("turno_ceiba", "Turno CEIBA")]
    [InlineData("tipo_de_accion", "Tipo de Acción")]
    [InlineData("traslados", "Traslados")]
    [InlineData("unknown", "unknown")]
    public void SugerenciaCampos_GetDisplayName_ShouldReturnCorrectNames(string campo, string expected)
    {
        // Act
        var result = SugerenciaCampos.GetDisplayName(campo);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region AuditLogEntryDto Tests

    [Fact(DisplayName = "AuditLogEntryDto should have default values")]
    public void AuditLogEntryDto_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var dto = new AuditLogEntryDto();

        // Assert
        dto.Id.Should().Be(0);
        dto.Codigo.Should().BeEmpty();
        dto.CodigoDescripcion.Should().BeNull();
        dto.IdRelacionado.Should().BeNull();
        dto.UsuarioId.Should().BeNull();
    }

    #endregion

    #region AuditFilterDto Tests

    [Fact(DisplayName = "AuditFilterDto should have default pagination values")]
    public void AuditFilterDto_ShouldHaveDefaultPaginationValues()
    {
        // Arrange & Act
        var filter = new AuditFilterDto();

        // Assert
        filter.Page.Should().Be(1);
        filter.PageSize.Should().Be(50);
    }

    #endregion

    #region AuditCodes Tests

    [Fact(DisplayName = "AuditCodes should define authentication codes")]
    public void AuditCodes_ShouldDefineAuthenticationCodes()
    {
        // Assert
        AuditCodes.AUTH_LOGIN.Should().Be("AUTH_LOGIN");
        AuditCodes.AUTH_LOGOUT.Should().Be("AUTH_LOGOUT");
        AuditCodes.AUTH_FAILED.Should().Be("AUTH_FAILED");
        AuditCodes.AUTH_LOCKED.Should().Be("AUTH_LOCKED");
    }

    [Fact(DisplayName = "AuditCodes should define user management codes")]
    public void AuditCodes_ShouldDefineUserManagementCodes()
    {
        // Assert
        AuditCodes.USER_CREATE.Should().Be("USER_CREATE");
        AuditCodes.USER_UPDATE.Should().Be("USER_UPDATE");
        AuditCodes.USER_SUSPEND.Should().Be("USER_SUSPEND");
        AuditCodes.USER_DELETE.Should().Be("USER_DELETE");
    }

    [Fact(DisplayName = "AuditCodes should define report operation codes")]
    public void AuditCodes_ShouldDefineReportOperationCodes()
    {
        // Assert
        AuditCodes.REPORT_CREATE.Should().Be("REPORT_CREATE");
        AuditCodes.REPORT_UPDATE.Should().Be("REPORT_UPDATE");
        AuditCodes.REPORT_SUBMIT.Should().Be("REPORT_SUBMIT");
        AuditCodes.REPORT_EXPORT.Should().Be("REPORT_EXPORT");
    }

    [Theory(DisplayName = "AuditCodes.GetDescription should return Spanish descriptions")]
    [InlineData("AUTH_LOGIN", "Inicio de sesión")]
    [InlineData("AUTH_LOGOUT", "Cierre de sesión")]
    [InlineData("USER_CREATE", "Usuario creado")]
    [InlineData("USER_SUSPEND", "Usuario suspendido")]
    [InlineData("REPORT_CREATE", "Reporte creado")]
    [InlineData("REPORT_SUBMIT", "Reporte entregado")]
    [InlineData("UNKNOWN_CODE", "UNKNOWN_CODE")]
    public void AuditCodes_GetDescription_ShouldReturnSpanishDescriptions(string code, string expected)
    {
        // Act
        var result = AuditCodes.GetDescription(code);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region GeographicCatalogStatsDto Tests

    [Fact(DisplayName = "GeographicCatalogStatsDto should have default values")]
    public void GeographicCatalogStatsDto_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var dto = new GeographicCatalogStatsDto();

        // Assert
        dto.Message.Should().BeEmpty();
        dto.ZonasCount.Should().Be(0);
        dto.RegionesCount.Should().Be(0);
        dto.SectoresCount.Should().Be(0);
        dto.CuadrantesCount.Should().Be(0);
    }

    [Fact(DisplayName = "GeographicCatalogStatsDto should store catalog counts")]
    public void GeographicCatalogStatsDto_ShouldStoreCatalogCounts()
    {
        // Arrange & Act
        var dto = new GeographicCatalogStatsDto
        {
            Message = "Catalogs loaded successfully",
            ZonasCount = 5,
            RegionesCount = 20,
            SectoresCount = 100,
            CuadrantesCount = 500
        };

        // Assert
        dto.ZonasCount.Should().Be(5);
        dto.RegionesCount.Should().Be(20);
        dto.SectoresCount.Should().Be(100);
        dto.CuadrantesCount.Should().Be(500);
    }

    #endregion

    #region Helper Methods

    private static List<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(model);
        Validator.TryValidateObject(model, context, validationResults, true);
        return validationResults;
    }

    #endregion
}
