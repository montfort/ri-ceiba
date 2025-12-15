using Ceiba.Core.Entities;
using FluentAssertions;

namespace Ceiba.Core.Tests.Entities;

/// <summary>
/// Unit tests for ConfiguracionReportesAutomatizados entity.
/// Tests automated report generation configuration.
/// </summary>
[Trait("Category", "Unit")]
public class ConfiguracionReportesAutomatizadosTests
{
    #region Default Value Tests

    [Fact(DisplayName = "ConfiguracionReportesAutomatizados should have default Habilitado as false")]
    public void ConfiguracionReportesAutomatizados_Habilitado_ShouldDefaultToFalse()
    {
        // Arrange & Act
        var config = new ConfiguracionReportesAutomatizados();

        // Assert
        config.Habilitado.Should().BeFalse();
    }

    [Fact(DisplayName = "ConfiguracionReportesAutomatizados should have default HoraGeneracion as 06:00")]
    public void ConfiguracionReportesAutomatizados_HoraGeneracion_ShouldDefaultTo0600()
    {
        // Arrange & Act
        var config = new ConfiguracionReportesAutomatizados();

        // Assert
        config.HoraGeneracion.Should().Be(new TimeSpan(6, 0, 0));
    }

    [Fact(DisplayName = "ConfiguracionReportesAutomatizados should have default Destinatarios as empty")]
    public void ConfiguracionReportesAutomatizados_Destinatarios_ShouldDefaultToEmpty()
    {
        // Arrange & Act
        var config = new ConfiguracionReportesAutomatizados();

        // Assert
        config.Destinatarios.Should().BeEmpty();
    }

    [Fact(DisplayName = "ConfiguracionReportesAutomatizados should have default RutaSalida")]
    public void ConfiguracionReportesAutomatizados_RutaSalida_ShouldDefaultToGeneratedReports()
    {
        // Arrange & Act
        var config = new ConfiguracionReportesAutomatizados();

        // Assert
        config.RutaSalida.Should().Be("./generated-reports");
    }

    [Fact(DisplayName = "ConfiguracionReportesAutomatizados should have UpdatedAt default to null")]
    public void ConfiguracionReportesAutomatizados_UpdatedAt_ShouldDefaultToNull()
    {
        // Arrange & Act
        var config = new ConfiguracionReportesAutomatizados();

        // Assert
        config.UpdatedAt.Should().BeNull();
    }

    #endregion

    #region HoraGeneracion Tests

    [Theory(DisplayName = "ConfiguracionReportesAutomatizados should accept valid generation times")]
    [InlineData(0, 0, 0)]    // Midnight
    [InlineData(6, 0, 0)]    // 6:00 AM (default)
    [InlineData(12, 0, 0)]   // Noon
    [InlineData(23, 59, 59)] // Last second of day
    public void ConfiguracionReportesAutomatizados_ShouldAcceptValidGenerationTimes(int hours, int minutes, int seconds)
    {
        // Arrange
        var config = new ConfiguracionReportesAutomatizados();
        var timeSpan = new TimeSpan(hours, minutes, seconds);

        // Act
        config.HoraGeneracion = timeSpan;

        // Assert
        config.HoraGeneracion.Should().Be(timeSpan);
    }

    #endregion

    #region Destinatarios Helper Method Tests

    [Fact(DisplayName = "GetDestinatariosArray should return empty array for empty string")]
    public void GetDestinatariosArray_EmptyString_ShouldReturnEmptyArray()
    {
        // Arrange
        var config = new ConfiguracionReportesAutomatizados { Destinatarios = "" };

        // Act
        var result = config.GetDestinatariosArray();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact(DisplayName = "GetDestinatariosArray should return empty array for whitespace")]
    public void GetDestinatariosArray_Whitespace_ShouldReturnEmptyArray()
    {
        // Arrange
        var config = new ConfiguracionReportesAutomatizados { Destinatarios = "   " };

        // Act
        var result = config.GetDestinatariosArray();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact(DisplayName = "GetDestinatariosArray should return single email")]
    public void GetDestinatariosArray_SingleEmail_ShouldReturnSingleElementArray()
    {
        // Arrange
        var config = new ConfiguracionReportesAutomatizados { Destinatarios = "admin@example.com" };

        // Act
        var result = config.GetDestinatariosArray();

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().Be("admin@example.com");
    }

    [Fact(DisplayName = "GetDestinatariosArray should return multiple emails")]
    public void GetDestinatariosArray_MultipleEmails_ShouldReturnAllEmails()
    {
        // Arrange
        var config = new ConfiguracionReportesAutomatizados
        {
            Destinatarios = "admin@example.com,supervisor@example.com,director@example.com"
        };

        // Act
        var result = config.GetDestinatariosArray();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain("admin@example.com");
        result.Should().Contain("supervisor@example.com");
        result.Should().Contain("director@example.com");
    }

    [Fact(DisplayName = "GetDestinatariosArray should trim whitespace from emails")]
    public void GetDestinatariosArray_EmailsWithWhitespace_ShouldTrim()
    {
        // Arrange
        var config = new ConfiguracionReportesAutomatizados
        {
            Destinatarios = "  admin@example.com  ,  supervisor@example.com  "
        };

        // Act
        var result = config.GetDestinatariosArray();

        // Assert
        result.Should().HaveCount(2);
        result[0].Should().Be("admin@example.com");
        result[1].Should().Be("supervisor@example.com");
    }

    [Fact(DisplayName = "GetDestinatariosArray should skip empty entries")]
    public void GetDestinatariosArray_EmptyEntries_ShouldSkip()
    {
        // Arrange
        var config = new ConfiguracionReportesAutomatizados
        {
            Destinatarios = "admin@example.com,,supervisor@example.com,  ,director@example.com"
        };

        // Act
        var result = config.GetDestinatariosArray();

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact(DisplayName = "SetDestinatariosArray should set emails from array")]
    public void SetDestinatariosArray_ValidArray_ShouldSetDestinatarios()
    {
        // Arrange
        var config = new ConfiguracionReportesAutomatizados();
        var emails = new[] { "admin@example.com", "supervisor@example.com" };

        // Act
        config.SetDestinatariosArray(emails);

        // Assert
        config.Destinatarios.Should().Be("admin@example.com,supervisor@example.com");
    }

    [Fact(DisplayName = "SetDestinatariosArray should skip empty entries")]
    public void SetDestinatariosArray_ArrayWithEmptyEntries_ShouldSkipEmpty()
    {
        // Arrange
        var config = new ConfiguracionReportesAutomatizados();
        var emails = new[] { "admin@example.com", "", "  ", "supervisor@example.com" };

        // Act
        config.SetDestinatariosArray(emails);

        // Assert
        config.Destinatarios.Should().Be("admin@example.com,supervisor@example.com");
    }

    [Fact(DisplayName = "SetDestinatariosArray with empty array should set empty string")]
    public void SetDestinatariosArray_EmptyArray_ShouldSetEmptyString()
    {
        // Arrange
        var config = new ConfiguracionReportesAutomatizados { Destinatarios = "existing@example.com" };

        // Act
        config.SetDestinatariosArray(Array.Empty<string>());

        // Assert
        config.Destinatarios.Should().BeEmpty();
    }

    #endregion

    #region Inheritance Tests

    [Fact(DisplayName = "ConfiguracionReportesAutomatizados should inherit from BaseEntityWithUser")]
    public void ConfiguracionReportesAutomatizados_ShouldInheritFromBaseEntityWithUser()
    {
        // Arrange & Act
        var config = new ConfiguracionReportesAutomatizados();

        // Assert
        config.Should().BeAssignableTo<BaseEntityWithUser>();
        config.UsuarioId.Should().Be(Guid.Empty);
        config.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    #endregion
}
