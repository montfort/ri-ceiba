namespace Ceiba.Core.Enums;

/// <summary>
/// Standard audit action codes for system operations.
/// All critical operations must be logged with one of these codes.
/// Follows convention: ENTITY_ACTION pattern.
/// </summary>
public static class AuditActionCode
{
    // Authentication & Authorization
    public const string AUTH_LOGIN = "AUTH_LOGIN";
    public const string AUTH_LOGOUT = "AUTH_LOGOUT";
    public const string AUTH_FAILED = "AUTH_FAILED";
    public const string AUTH_SESSION_CREATED = "AUTH_SESSION_CREATED";
    public const string AUTH_SESSION_DESTROYED = "AUTH_SESSION_DESTROYED";

    // User Management
    public const string USER_CREATE = "USER_CREATE";
    public const string USER_UPDATE = "USER_UPDATE";
    public const string USER_SUSPEND = "USER_SUSPEND";
    public const string USER_DELETE = "USER_DELETE";
    public const string USER_ROLE_ASSIGN = "USER_ROLE_ASSIGN";
    public const string USER_ROLE_REMOVE = "USER_ROLE_REMOVE";

    // Report Operations
    public const string REPORT_CREATE = "REPORT_CREATE";
    public const string REPORT_UPDATE = "REPORT_UPDATE";
    public const string REPORT_SUBMIT = "REPORT_SUBMIT";
    public const string REPORT_EXPORT = "REPORT_EXPORT";
    public const string REPORT_BULK_EXPORT = "REPORT_BULK_EXPORT";

    // Catalog Configuration
    public const string CONFIG_ZONA = "CONFIG_ZONA";
    public const string CONFIG_SECTOR = "CONFIG_SECTOR";
    public const string CONFIG_CUADRANTE = "CONFIG_CUADRANTE";
    public const string CONFIG_SUGERENCIA = "CONFIG_SUGERENCIA";

    // Automated Reports
    public const string AUTO_REPORT_GEN = "AUTO_REPORT_GEN";
    public const string AUTO_REPORT_SEND = "AUTO_REPORT_SEND";
    public const string AUTO_REPORT_FAIL = "AUTO_REPORT_FAIL";

    // System Operations
    public const string SYSTEM_BACKUP = "SYSTEM_BACKUP";
    public const string SYSTEM_RESTORE = "SYSTEM_RESTORE";
    public const string SYSTEM_CONFIG_CHANGE = "SYSTEM_CONFIG_CHANGE";

    // Security Events (RS-004 Mitigation)
    public const string SECURITY_BRUTE_FORCE = "SECURITY_BRUTE_FORCE";
    public const string SECURITY_UNAUTHORIZED_ACCESS = "SECURITY_UNAUTHORIZED_ACCESS";
    public const string SECURITY_EXPORT_LIMIT_EXCEEDED = "SECURITY_EXPORT_LIMIT_EXCEEDED";
}
