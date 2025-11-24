namespace Ceiba.Web.Configuration;

/// <summary>
/// T019d: RT-004 Mitigation - Feature flag configuration system.
/// Allows enabling/disabling features without code changes or migrations.
/// Configured via appsettings.json.
/// </summary>
public class FeatureFlags
{
    /// <summary>
    /// Enable Type B incident reports (future feature).
    /// Default: false
    /// </summary>
    public bool EnableReporteTipoB { get; set; } = false;

    /// <summary>
    /// Enable Type C incident reports (future feature).
    /// Default: false
    /// </summary>
    public bool EnableReporteTipoC { get; set; } = false;

    /// <summary>
    /// Enable automated report generation with AI.
    /// Default: true (core feature from US4)
    /// </summary>
    public bool EnableAutomatedReports { get; set; } = true;

    /// <summary>
    /// Enable AI narrative summarization.
    /// Default: true
    /// </summary>
    public bool EnableAINarrative { get; set; } = true;

    /// <summary>
    /// Enable email notifications.
    /// Default: true
    /// </summary>
    public bool EnableEmailNotifications { get; set; } = true;

    /// <summary>
    /// Enable PDF export functionality.
    /// Default: true
    /// </summary>
    public bool EnablePDFExport { get; set; } = true;

    /// <summary>
    /// Enable JSON export functionality.
    /// Default: true
    /// </summary>
    public bool EnableJSONExport { get; set; } = true;

    /// <summary>
    /// Enable bulk operations (batch export, etc.).
    /// Default: true
    /// </summary>
    public bool EnableBulkOperations { get; set; } = true;

    /// <summary>
    /// Enable advanced search with full-text indexing.
    /// Default: true
    /// </summary>
    public bool EnableAdvancedSearch { get; set; } = true;

    /// <summary>
    /// Enable user self-registration (for testing environments).
    /// Default: false (production should use admin-created accounts only)
    /// </summary>
    public bool EnableSelfRegistration { get; set; } = false;

    /// <summary>
    /// Enable maintenance mode (blocks all non-admin access).
    /// Default: false
    /// </summary>
    public bool MaintenanceMode { get; set; } = false;
}

/// <summary>
/// Extension methods for feature flag checks.
/// </summary>
public static class FeatureFlagExtensions
{
    /// <summary>
    /// Checks if a feature is enabled.
    /// Throws InvalidOperationException if feature is disabled.
    /// </summary>
    public static void RequireFeature(this FeatureFlags flags, Func<FeatureFlags, bool> featureCheck, string featureName)
    {
        if (!featureCheck(flags))
        {
            throw new InvalidOperationException($"Feature '{featureName}' is currently disabled");
        }
    }
}
