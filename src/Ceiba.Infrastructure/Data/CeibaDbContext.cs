using Ceiba.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Ceiba.Infrastructure.Data;

/// <summary>
/// Main database context for the Ceiba incident reporting system.
/// Extends IdentityDbContext for ASP.NET Identity integration.
/// Configured for PostgreSQL 18 with audit logging interceptor.
/// </summary>
public class CeibaDbContext : IdentityDbContext<IdentityUser<Guid>, IdentityRole<Guid>, Guid>
{
    /// <summary>
    /// Current authenticated user ID for audit logging.
    /// Set by middleware on each request.
    /// NULL for system operations (background jobs, migrations).
    /// </summary>
    public Guid? CurrentUserId { get; }

    public CeibaDbContext(DbContextOptions<CeibaDbContext> options, Guid? userId = null)
        : base(options)
    {
        CurrentUserId = userId;
    }

    // Geographic Catalogs (Hierarchical: Zona → Sector → Cuadrante)
    public DbSet<Zona> Zonas => Set<Zona>();
    public DbSet<Sector> Sectores => Set<Sector>();
    public DbSet<Cuadrante> Cuadrantes => Set<Cuadrante>();

    // Suggestion Catalogs
    public DbSet<CatalogoSugerencia> CatalogosSugerencia => Set<CatalogoSugerencia>();

    // Reports
    public DbSet<ReporteIncidencia> ReportesIncidencia => Set<ReporteIncidencia>();

    // Audit
    public DbSet<RegistroAuditoria> RegistrosAuditoria => Set<RegistroAuditoria>();

    // Automated Reports (US4)
    public DbSet<ReporteAutomatizado> ReportesAutomatizados => Set<ReporteAutomatizado>();
    public DbSet<ModeloReporte> ModelosReporte => Set<ModeloReporte>();

    // AI Configuration (US4)
    public DbSet<ConfiguracionIA> ConfiguracionesIA => Set<ConfiguracionIA>();

    // Automated Report Configuration (US4 Enhancement)
    public DbSet<ConfiguracionReportesAutomatizados> ConfiguracionReportesAutomatizados => Set<ConfiguracionReportesAutomatizados>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        // Add audit logging interceptor
        optionsBuilder.AddInterceptors(new AuditSaveChangesInterceptor(this));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply entity configurations from separate files
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CeibaDbContext).Assembly);

        // Configure Identity table names to match our conventions
        ConfigureIdentityTables(modelBuilder);
    }

    private static void ConfigureIdentityTables(ModelBuilder modelBuilder)
    {
        // Use UPPER_SNAKE_CASE for Identity tables to match project conventions
        modelBuilder.Entity<IdentityUser<Guid>>().ToTable("USUARIO");
        modelBuilder.Entity<IdentityRole<Guid>>().ToTable("ROL");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("USUARIO_ROL");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("USUARIO_CLAIM");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("USUARIO_LOGIN");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("USUARIO_TOKEN");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("ROL_CLAIM");
    }
}
