using Ceiba.Web.Configuration;
using FluentAssertions;

namespace Ceiba.Web.Tests.Configuration;

/// <summary>
/// Unit tests for FeatureFlags and FeatureFlagExtensions.
/// T019d: RT-004 Mitigation - Feature flag configuration system tests.
/// </summary>
[Trait("Category", "Unit")]
public class FeatureFlagsTests
{
    #region Default Values Tests

    [Fact(DisplayName = "FeatureFlags should have correct default values")]
    public void FeatureFlags_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var flags = new FeatureFlags();

        // Assert - Features that should be disabled by default
        flags.EnableReporteTipoB.Should().BeFalse();
        flags.EnableReporteTipoC.Should().BeFalse();
        flags.EnableSelfRegistration.Should().BeFalse();
        flags.MaintenanceMode.Should().BeFalse();

        // Assert - Features that should be enabled by default
        flags.EnableAutomatedReports.Should().BeTrue();
        flags.EnableAINarrative.Should().BeTrue();
        flags.EnableEmailNotifications.Should().BeTrue();
        flags.EnablePDFExport.Should().BeTrue();
        flags.EnableJSONExport.Should().BeTrue();
        flags.EnableBulkOperations.Should().BeTrue();
        flags.EnableAdvancedSearch.Should().BeTrue();
    }

    [Fact(DisplayName = "EnableReporteTipoB should default to false")]
    public void EnableReporteTipoB_Default_IsFalse()
    {
        // Arrange & Act
        var flags = new FeatureFlags();

        // Assert
        flags.EnableReporteTipoB.Should().BeFalse();
    }

    [Fact(DisplayName = "EnableReporteTipoC should default to false")]
    public void EnableReporteTipoC_Default_IsFalse()
    {
        // Arrange & Act
        var flags = new FeatureFlags();

        // Assert
        flags.EnableReporteTipoC.Should().BeFalse();
    }

    [Fact(DisplayName = "EnableAutomatedReports should default to true")]
    public void EnableAutomatedReports_Default_IsTrue()
    {
        // Arrange & Act
        var flags = new FeatureFlags();

        // Assert
        flags.EnableAutomatedReports.Should().BeTrue();
    }

    [Fact(DisplayName = "EnableAINarrative should default to true")]
    public void EnableAINarrative_Default_IsTrue()
    {
        // Arrange & Act
        var flags = new FeatureFlags();

        // Assert
        flags.EnableAINarrative.Should().BeTrue();
    }

    [Fact(DisplayName = "EnableEmailNotifications should default to true")]
    public void EnableEmailNotifications_Default_IsTrue()
    {
        // Arrange & Act
        var flags = new FeatureFlags();

        // Assert
        flags.EnableEmailNotifications.Should().BeTrue();
    }

    [Fact(DisplayName = "EnablePDFExport should default to true")]
    public void EnablePDFExport_Default_IsTrue()
    {
        // Arrange & Act
        var flags = new FeatureFlags();

        // Assert
        flags.EnablePDFExport.Should().BeTrue();
    }

    [Fact(DisplayName = "EnableJSONExport should default to true")]
    public void EnableJSONExport_Default_IsTrue()
    {
        // Arrange & Act
        var flags = new FeatureFlags();

        // Assert
        flags.EnableJSONExport.Should().BeTrue();
    }

    [Fact(DisplayName = "EnableBulkOperations should default to true")]
    public void EnableBulkOperations_Default_IsTrue()
    {
        // Arrange & Act
        var flags = new FeatureFlags();

        // Assert
        flags.EnableBulkOperations.Should().BeTrue();
    }

    [Fact(DisplayName = "EnableAdvancedSearch should default to true")]
    public void EnableAdvancedSearch_Default_IsTrue()
    {
        // Arrange & Act
        var flags = new FeatureFlags();

        // Assert
        flags.EnableAdvancedSearch.Should().BeTrue();
    }

    [Fact(DisplayName = "EnableSelfRegistration should default to false")]
    public void EnableSelfRegistration_Default_IsFalse()
    {
        // Arrange & Act
        var flags = new FeatureFlags();

        // Assert
        flags.EnableSelfRegistration.Should().BeFalse();
    }

    [Fact(DisplayName = "MaintenanceMode should default to false")]
    public void MaintenanceMode_Default_IsFalse()
    {
        // Arrange & Act
        var flags = new FeatureFlags();

        // Assert
        flags.MaintenanceMode.Should().BeFalse();
    }

    #endregion

    #region Property Setting Tests

    [Fact(DisplayName = "EnableReporteTipoB should be settable")]
    public void EnableReporteTipoB_CanBeSet()
    {
        // Arrange
        var flags = new FeatureFlags();

        // Act
        flags.EnableReporteTipoB = true;

        // Assert
        flags.EnableReporteTipoB.Should().BeTrue();
    }

    [Fact(DisplayName = "EnableReporteTipoC should be settable")]
    public void EnableReporteTipoC_CanBeSet()
    {
        // Arrange
        var flags = new FeatureFlags();

        // Act
        flags.EnableReporteTipoC = true;

        // Assert
        flags.EnableReporteTipoC.Should().BeTrue();
    }

    [Fact(DisplayName = "EnableAutomatedReports should be settable")]
    public void EnableAutomatedReports_CanBeSet()
    {
        // Arrange
        var flags = new FeatureFlags();

        // Act
        flags.EnableAutomatedReports = false;

        // Assert
        flags.EnableAutomatedReports.Should().BeFalse();
    }

    [Fact(DisplayName = "EnableAINarrative should be settable")]
    public void EnableAINarrative_CanBeSet()
    {
        // Arrange
        var flags = new FeatureFlags();

        // Act
        flags.EnableAINarrative = false;

        // Assert
        flags.EnableAINarrative.Should().BeFalse();
    }

    [Fact(DisplayName = "EnableEmailNotifications should be settable")]
    public void EnableEmailNotifications_CanBeSet()
    {
        // Arrange
        var flags = new FeatureFlags();

        // Act
        flags.EnableEmailNotifications = false;

        // Assert
        flags.EnableEmailNotifications.Should().BeFalse();
    }

    [Fact(DisplayName = "EnablePDFExport should be settable")]
    public void EnablePDFExport_CanBeSet()
    {
        // Arrange
        var flags = new FeatureFlags();

        // Act
        flags.EnablePDFExport = false;

        // Assert
        flags.EnablePDFExport.Should().BeFalse();
    }

    [Fact(DisplayName = "EnableJSONExport should be settable")]
    public void EnableJSONExport_CanBeSet()
    {
        // Arrange
        var flags = new FeatureFlags();

        // Act
        flags.EnableJSONExport = false;

        // Assert
        flags.EnableJSONExport.Should().BeFalse();
    }

    [Fact(DisplayName = "EnableBulkOperations should be settable")]
    public void EnableBulkOperations_CanBeSet()
    {
        // Arrange
        var flags = new FeatureFlags();

        // Act
        flags.EnableBulkOperations = false;

        // Assert
        flags.EnableBulkOperations.Should().BeFalse();
    }

    [Fact(DisplayName = "EnableAdvancedSearch should be settable")]
    public void EnableAdvancedSearch_CanBeSet()
    {
        // Arrange
        var flags = new FeatureFlags();

        // Act
        flags.EnableAdvancedSearch = false;

        // Assert
        flags.EnableAdvancedSearch.Should().BeFalse();
    }

    [Fact(DisplayName = "EnableSelfRegistration should be settable")]
    public void EnableSelfRegistration_CanBeSet()
    {
        // Arrange
        var flags = new FeatureFlags();

        // Act
        flags.EnableSelfRegistration = true;

        // Assert
        flags.EnableSelfRegistration.Should().BeTrue();
    }

    [Fact(DisplayName = "MaintenanceMode should be settable")]
    public void MaintenanceMode_CanBeSet()
    {
        // Arrange
        var flags = new FeatureFlags();

        // Act
        flags.MaintenanceMode = true;

        // Assert
        flags.MaintenanceMode.Should().BeTrue();
    }

    #endregion

    #region FeatureFlagExtensions Tests

    [Fact(DisplayName = "RequireFeature should not throw when feature is enabled")]
    public void RequireFeature_FeatureEnabled_DoesNotThrow()
    {
        // Arrange
        var flags = new FeatureFlags { EnablePDFExport = true };

        // Act
        var act = () => flags.RequireFeature(f => f.EnablePDFExport, "PDF Export");

        // Assert
        act.Should().NotThrow();
    }

    [Fact(DisplayName = "RequireFeature should throw InvalidOperationException when feature is disabled")]
    public void RequireFeature_FeatureDisabled_ThrowsInvalidOperationException()
    {
        // Arrange
        var flags = new FeatureFlags { EnablePDFExport = false };

        // Act
        var act = () => flags.RequireFeature(f => f.EnablePDFExport, "PDF Export");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Feature 'PDF Export' is currently disabled");
    }

    [Fact(DisplayName = "RequireFeature should include feature name in exception message")]
    public void RequireFeature_FeatureDisabled_IncludesFeatureNameInMessage()
    {
        // Arrange
        var flags = new FeatureFlags { EnableAutomatedReports = false };
        var featureName = "Automated Reports";

        // Act
        var act = () => flags.RequireFeature(f => f.EnableAutomatedReports, featureName);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{featureName}*");
    }

    [Fact(DisplayName = "RequireFeature should work with EnableReporteTipoB")]
    public void RequireFeature_EnableReporteTipoB_WorksCorrectly()
    {
        // Arrange
        var flags = new FeatureFlags { EnableReporteTipoB = false };

        // Act
        var act = () => flags.RequireFeature(f => f.EnableReporteTipoB, "Type B Reports");

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact(DisplayName = "RequireFeature should work with EnableReporteTipoC")]
    public void RequireFeature_EnableReporteTipoC_WorksCorrectly()
    {
        // Arrange
        var flags = new FeatureFlags { EnableReporteTipoC = true };

        // Act
        var act = () => flags.RequireFeature(f => f.EnableReporteTipoC, "Type C Reports");

        // Assert
        act.Should().NotThrow();
    }

    [Fact(DisplayName = "RequireFeature should work with EnableAINarrative")]
    public void RequireFeature_EnableAINarrative_WorksCorrectly()
    {
        // Arrange
        var flags = new FeatureFlags { EnableAINarrative = false };

        // Act
        var act = () => flags.RequireFeature(f => f.EnableAINarrative, "AI Narrative");

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact(DisplayName = "RequireFeature should work with EnableEmailNotifications")]
    public void RequireFeature_EnableEmailNotifications_WorksCorrectly()
    {
        // Arrange
        var flags = new FeatureFlags { EnableEmailNotifications = true };

        // Act
        var act = () => flags.RequireFeature(f => f.EnableEmailNotifications, "Email Notifications");

        // Assert
        act.Should().NotThrow();
    }

    [Fact(DisplayName = "RequireFeature should work with EnableJSONExport")]
    public void RequireFeature_EnableJSONExport_WorksCorrectly()
    {
        // Arrange
        var flags = new FeatureFlags { EnableJSONExport = false };

        // Act
        var act = () => flags.RequireFeature(f => f.EnableJSONExport, "JSON Export");

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact(DisplayName = "RequireFeature should work with EnableBulkOperations")]
    public void RequireFeature_EnableBulkOperations_WorksCorrectly()
    {
        // Arrange
        var flags = new FeatureFlags { EnableBulkOperations = true };

        // Act
        var act = () => flags.RequireFeature(f => f.EnableBulkOperations, "Bulk Operations");

        // Assert
        act.Should().NotThrow();
    }

    [Fact(DisplayName = "RequireFeature should work with EnableAdvancedSearch")]
    public void RequireFeature_EnableAdvancedSearch_WorksCorrectly()
    {
        // Arrange
        var flags = new FeatureFlags { EnableAdvancedSearch = false };

        // Act
        var act = () => flags.RequireFeature(f => f.EnableAdvancedSearch, "Advanced Search");

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact(DisplayName = "RequireFeature should work with EnableSelfRegistration")]
    public void RequireFeature_EnableSelfRegistration_WorksCorrectly()
    {
        // Arrange
        var flags = new FeatureFlags { EnableSelfRegistration = true };

        // Act
        var act = () => flags.RequireFeature(f => f.EnableSelfRegistration, "Self Registration");

        // Assert
        act.Should().NotThrow();
    }

    [Fact(DisplayName = "RequireFeature should work with MaintenanceMode")]
    public void RequireFeature_MaintenanceMode_WorksCorrectly()
    {
        // Arrange - MaintenanceMode = true means maintenance is ON (feature is "enabled")
        var flags = new FeatureFlags { MaintenanceMode = true };

        // Act - Require maintenance mode to be enabled
        var act = () => flags.RequireFeature(f => f.MaintenanceMode, "Maintenance Mode");

        // Assert
        act.Should().NotThrow();
    }

    [Fact(DisplayName = "RequireFeature should work with complex lambda expressions")]
    public void RequireFeature_ComplexLambda_WorksCorrectly()
    {
        // Arrange
        var flags = new FeatureFlags
        {
            EnablePDFExport = true,
            EnableJSONExport = true
        };

        // Act - Check if either export is enabled
        var act = () => flags.RequireFeature(
            f => f.EnablePDFExport || f.EnableJSONExport,
            "Export Functionality");

        // Assert
        act.Should().NotThrow();
    }

    [Fact(DisplayName = "RequireFeature should fail with complex lambda when both disabled")]
    public void RequireFeature_ComplexLambda_FailsWhenBothDisabled()
    {
        // Arrange
        var flags = new FeatureFlags
        {
            EnablePDFExport = false,
            EnableJSONExport = false
        };

        // Act
        var act = () => flags.RequireFeature(
            f => f.EnablePDFExport || f.EnableJSONExport,
            "Export Functionality");

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region Configuration Scenarios Tests

    [Fact(DisplayName = "Production configuration should have safe defaults")]
    public void ProductionConfiguration_SafeDefaults()
    {
        // Arrange & Act
        var flags = new FeatureFlags();

        // Assert - Security-sensitive features should be disabled
        flags.EnableSelfRegistration.Should().BeFalse("Self-registration should be disabled in production");
        flags.MaintenanceMode.Should().BeFalse("Maintenance mode should be off by default");

        // Assert - Future features should be disabled
        flags.EnableReporteTipoB.Should().BeFalse("Type B reports are not implemented");
        flags.EnableReporteTipoC.Should().BeFalse("Type C reports are not implemented");
    }

    [Fact(DisplayName = "All core features should be enabled by default")]
    public void CoreFeatures_EnabledByDefault()
    {
        // Arrange & Act
        var flags = new FeatureFlags();

        // Assert - Core features from US1-US4
        flags.EnableAutomatedReports.Should().BeTrue("US4 core feature");
        flags.EnableAINarrative.Should().BeTrue("US4 AI summarization");
        flags.EnableEmailNotifications.Should().BeTrue("Required for automated reports");
        flags.EnablePDFExport.Should().BeTrue("US2 export feature");
        flags.EnableJSONExport.Should().BeTrue("US2 export feature");
        flags.EnableBulkOperations.Should().BeTrue("US2 bulk export");
        flags.EnableAdvancedSearch.Should().BeTrue("Search functionality");
    }

    [Fact(DisplayName = "FeatureFlags can be fully customized")]
    public void FeatureFlags_FullCustomization()
    {
        // Arrange & Act
        var flags = new FeatureFlags
        {
            EnableReporteTipoB = true,
            EnableReporteTipoC = true,
            EnableAutomatedReports = false,
            EnableAINarrative = false,
            EnableEmailNotifications = false,
            EnablePDFExport = false,
            EnableJSONExport = false,
            EnableBulkOperations = false,
            EnableAdvancedSearch = false,
            EnableSelfRegistration = true,
            MaintenanceMode = true
        };

        // Assert - All values should be customized
        flags.EnableReporteTipoB.Should().BeTrue();
        flags.EnableReporteTipoC.Should().BeTrue();
        flags.EnableAutomatedReports.Should().BeFalse();
        flags.EnableAINarrative.Should().BeFalse();
        flags.EnableEmailNotifications.Should().BeFalse();
        flags.EnablePDFExport.Should().BeFalse();
        flags.EnableJSONExport.Should().BeFalse();
        flags.EnableBulkOperations.Should().BeFalse();
        flags.EnableAdvancedSearch.Should().BeFalse();
        flags.EnableSelfRegistration.Should().BeTrue();
        flags.MaintenanceMode.Should().BeTrue();
    }

    #endregion
}
