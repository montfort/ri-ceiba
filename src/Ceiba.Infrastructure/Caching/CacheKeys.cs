namespace Ceiba.Infrastructure.Caching;

/// <summary>
/// Centralized cache key definitions for consistent cache management.
/// T117b: Query caching strategy
/// </summary>
public static class CacheKeys
{
    // Catalog caches (rarely change, long TTL)
    public const string AllZonas = "catalog:zonas:all";
    public const string AllRegiones = "catalog:regiones:all";
    public const string AllSectores = "catalog:sectores:all";
    public const string AllCuadrantes = "catalog:cuadrantes:all";
    public const string SugerenciasByCampo = "catalog:sugerencias:{0}"; // {0} = campo name

    // Geographic hierarchy caches (Zona → Región → Sector → Cuadrante)
    public const string RegionesByZona = "catalog:regiones:zona:{0}"; // {0} = zonaId
    public const string SectoresByRegion = "catalog:sectores:region:{0}"; // {0} = regionId
    public const string CuadrantesBySector = "catalog:cuadrantes:sector:{0}"; // {0} = sectorId

    // Report caches (short TTL due to frequent updates)
    public const string ReportById = "report:{0}"; // {0} = reportId
    public const string ReportsByUser = "reports:user:{0}"; // {0} = userId
    public const string ReportCount = "reports:count:{0}"; // {0} = filter hash

    // User caches
    public const string UserById = "user:{0}"; // {0} = userId
    public const string UserRoles = "user:roles:{0}"; // {0} = userId

    // Statistics caches
    public const string DashboardStats = "stats:dashboard:{0}"; // {0} = date
    public const string ReportsByZona = "stats:reports:zona:{0}:{1}"; // {0} = zonaId, {1} = date

    /// <summary>
    /// Generate a cache key from a template and parameters.
    /// </summary>
    public static string Format(string template, params object[] args)
    {
        return string.Format(template, args);
    }

    /// <summary>
    /// Generate a hash-based cache key for complex filter objects.
    /// </summary>
    public static string ForFilter(string prefix, object filter)
    {
        var hash = filter.GetHashCode();
        return $"{prefix}:{hash}";
    }
}
