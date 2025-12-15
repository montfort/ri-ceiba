using Ceiba.Core.Entities;
using FluentAssertions;

namespace Ceiba.Core.Tests.Entities;

/// <summary>
/// Unit tests for ConfiguracionEmail entity.
/// Tests email service configuration for SMTP, SendGrid, and Mailgun providers.
/// </summary>
[Trait("Category", "Unit")]
public class ConfiguracionEmailTests
{
    #region Default Value Tests

    [Fact(DisplayName = "ConfiguracionEmail should have default provider as SMTP")]
    public void ConfiguracionEmail_Proveedor_ShouldDefaultToSMTP()
    {
        // Arrange & Act
        var config = new ConfiguracionEmail();

        // Assert
        config.Proveedor.Should().Be("SMTP");
    }

    [Fact(DisplayName = "ConfiguracionEmail should have default Habilitado as false")]
    public void ConfiguracionEmail_Habilitado_ShouldDefaultToFalse()
    {
        // Arrange & Act
        var config = new ConfiguracionEmail();

        // Assert
        config.Habilitado.Should().BeFalse();
    }

    [Fact(DisplayName = "ConfiguracionEmail should have default SmtpUseSsl as true")]
    public void ConfiguracionEmail_SmtpUseSsl_ShouldDefaultToTrue()
    {
        // Arrange & Act
        var config = new ConfiguracionEmail();

        // Assert
        config.SmtpUseSsl.Should().BeTrue();
    }

    [Fact(DisplayName = "ConfiguracionEmail should have default MailgunRegion as US")]
    public void ConfiguracionEmail_MailgunRegion_ShouldDefaultToUS()
    {
        // Arrange & Act
        var config = new ConfiguracionEmail();

        // Assert
        config.MailgunRegion.Should().Be("US");
    }

    [Fact(DisplayName = "ConfiguracionEmail should have FromEmail default to empty")]
    public void ConfiguracionEmail_FromEmail_ShouldDefaultToEmpty()
    {
        // Arrange & Act
        var config = new ConfiguracionEmail();

        // Assert
        config.FromEmail.Should().BeEmpty();
    }

    [Fact(DisplayName = "ConfiguracionEmail should have FromName default to empty")]
    public void ConfiguracionEmail_FromName_ShouldDefaultToEmpty()
    {
        // Arrange & Act
        var config = new ConfiguracionEmail();

        // Assert
        config.FromName.Should().BeEmpty();
    }

    [Fact(DisplayName = "ConfiguracionEmail nullable properties should default to null")]
    public void ConfiguracionEmail_NullableProperties_ShouldDefaultToNull()
    {
        // Arrange & Act
        var config = new ConfiguracionEmail();

        // Assert
        config.SmtpHost.Should().BeNull();
        config.SmtpPort.Should().BeNull();
        config.SmtpUsername.Should().BeNull();
        config.SmtpPassword.Should().BeNull();
        config.SendGridApiKey.Should().BeNull();
        config.MailgunApiKey.Should().BeNull();
        config.MailgunDomain.Should().BeNull();
        config.LastTestedAt.Should().BeNull();
        config.LastTestSuccess.Should().BeNull();
        config.LastTestError.Should().BeNull();
        config.UpdatedAt.Should().BeNull();
    }

    #endregion

    #region SMTP Configuration Tests

    [Fact(DisplayName = "ConfiguracionEmail should allow setting SMTP properties")]
    public void ConfiguracionEmail_ShouldAllowSettingSMTPProperties()
    {
        // Arrange
        var config = new ConfiguracionEmail();

        // Act
        config.Proveedor = "SMTP";
        config.SmtpHost = "smtp.example.com";
        config.SmtpPort = 587;
        config.SmtpUsername = "user@example.com";
        config.SmtpPassword = "secretPassword";
        config.SmtpUseSsl = true;
        config.FromEmail = "noreply@example.com";
        config.FromName = "Ceiba System";

        // Assert
        config.SmtpHost.Should().Be("smtp.example.com");
        config.SmtpPort.Should().Be(587);
        config.SmtpUsername.Should().Be("user@example.com");
        config.SmtpPassword.Should().Be("secretPassword");
        config.SmtpUseSsl.Should().BeTrue();
        config.FromEmail.Should().Be("noreply@example.com");
        config.FromName.Should().Be("Ceiba System");
    }

    [Theory(DisplayName = "ConfiguracionEmail should support common SMTP ports")]
    [InlineData(25)]
    [InlineData(465)]
    [InlineData(587)]
    [InlineData(2525)]
    public void ConfiguracionEmail_ShouldSupportCommonSMTPPorts(int port)
    {
        // Arrange
        var config = new ConfiguracionEmail();

        // Act
        config.SmtpPort = port;

        // Assert
        config.SmtpPort.Should().Be(port);
    }

    #endregion

    #region SendGrid Configuration Tests

    [Fact(DisplayName = "ConfiguracionEmail should allow setting SendGrid properties")]
    public void ConfiguracionEmail_ShouldAllowSettingSendGridProperties()
    {
        // Arrange
        var config = new ConfiguracionEmail();

        // Act
        config.Proveedor = "SendGrid";
        config.SendGridApiKey = "SG.xxxxxxxxxxxx";
        config.FromEmail = "noreply@example.com";
        config.FromName = "Ceiba Notifications";

        // Assert
        config.Proveedor.Should().Be("SendGrid");
        config.SendGridApiKey.Should().Be("SG.xxxxxxxxxxxx");
    }

    #endregion

    #region Mailgun Configuration Tests

    [Fact(DisplayName = "ConfiguracionEmail should allow setting Mailgun properties")]
    public void ConfiguracionEmail_ShouldAllowSettingMailgunProperties()
    {
        // Arrange
        var config = new ConfiguracionEmail();

        // Act
        config.Proveedor = "Mailgun";
        config.MailgunApiKey = "key-xxxxxxxxxxxx";
        config.MailgunDomain = "mg.example.com";
        config.MailgunRegion = "EU";
        config.FromEmail = "noreply@mg.example.com";

        // Assert
        config.Proveedor.Should().Be("Mailgun");
        config.MailgunApiKey.Should().Be("key-xxxxxxxxxxxx");
        config.MailgunDomain.Should().Be("mg.example.com");
        config.MailgunRegion.Should().Be("EU");
    }

    [Theory(DisplayName = "ConfiguracionEmail should support valid Mailgun regions")]
    [InlineData("US")]
    [InlineData("EU")]
    public void ConfiguracionEmail_ShouldSupportValidMailgunRegions(string region)
    {
        // Arrange
        var config = new ConfiguracionEmail();

        // Act
        config.MailgunRegion = region;

        // Assert
        config.MailgunRegion.Should().Be(region);
    }

    #endregion

    #region Test Status Properties

    [Fact(DisplayName = "ConfiguracionEmail should track test status")]
    public void ConfiguracionEmail_ShouldTrackTestStatus()
    {
        // Arrange
        var config = new ConfiguracionEmail();
        var testTime = DateTime.UtcNow;

        // Act
        config.LastTestedAt = testTime;
        config.LastTestSuccess = true;
        config.LastTestError = null;

        // Assert
        config.LastTestedAt.Should().Be(testTime);
        config.LastTestSuccess.Should().BeTrue();
        config.LastTestError.Should().BeNull();
    }

    [Fact(DisplayName = "ConfiguracionEmail should track failed test")]
    public void ConfiguracionEmail_ShouldTrackFailedTest()
    {
        // Arrange
        var config = new ConfiguracionEmail();
        var testTime = DateTime.UtcNow;
        var errorMessage = "Connection refused: Unable to connect to SMTP server";

        // Act
        config.LastTestedAt = testTime;
        config.LastTestSuccess = false;
        config.LastTestError = errorMessage;

        // Assert
        config.LastTestedAt.Should().Be(testTime);
        config.LastTestSuccess.Should().BeFalse();
        config.LastTestError.Should().Be(errorMessage);
    }

    #endregion

    #region Inheritance Tests

    [Fact(DisplayName = "ConfiguracionEmail should inherit from BaseEntityWithUser")]
    public void ConfiguracionEmail_ShouldInheritFromBaseEntityWithUser()
    {
        // Arrange & Act
        var config = new ConfiguracionEmail();

        // Assert
        config.Should().BeAssignableTo<BaseEntityWithUser>();
        config.UsuarioId.Should().Be(Guid.Empty);
    }

    [Fact(DisplayName = "ConfiguracionEmail should allow setting UsuarioId")]
    public void ConfiguracionEmail_ShouldAllowSettingUsuarioId()
    {
        // Arrange
        var config = new ConfiguracionEmail();
        var userId = Guid.NewGuid();

        // Act
        config.UsuarioId = userId;

        // Assert
        config.UsuarioId.Should().Be(userId);
    }

    #endregion
}
