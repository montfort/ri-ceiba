using Ceiba.Core.Entities;
using Ceiba.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Ceiba.Infrastructure.Data;

/// <summary>
/// EF Core interceptor for automatic audit logging.
/// Intercepts SaveChanges operations and creates audit log entries.
/// Implements Constitution Principle III: Security and Auditability by Design.
/// </summary>
public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly CeibaDbContext _context;

    public AuditSaveChangesInterceptor(CeibaDbContext context)
    {
        _context = context;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        CreateAuditLogs(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        CreateAuditLogs(eventData.Context);
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void CreateAuditLogs(DbContext? context)
    {
        if (context == null)
            return;

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added ||
                       e.State == EntityState.Modified ||
                       e.State == EntityState.Deleted)
            .Where(e => e.Entity is not RegistroAuditoria) // Don't audit the audit logs themselves
            .ToList();

        foreach (var entry in entries)
        {
            var auditCode = DetermineAuditCode(entry);
            if (auditCode == null)
                continue;

            var auditLog = new RegistroAuditoria
            {
                Codigo = auditCode,
                IdRelacionado = GetEntityId(entry.Entity),
                TablaRelacionada = entry.Metadata.GetTableName(),
                UsuarioId = _context.CurrentUserId,
                Ip = null, // Will be set by middleware in Phase 8 (T017)
                Detalles = BuildAuditDetails(entry)
            };

            context.Set<RegistroAuditoria>().Add(auditLog);
        }
    }

    private static string? DetermineAuditCode(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var entityType = entry.Entity.GetType().Name;
        var state = entry.State;

        // Map entity type and state to audit code
        return (entityType, state) switch
        {
            // Geographic Catalogs
            ("Zona", EntityState.Added or EntityState.Modified) => AuditActionCode.CONFIG_ZONA,
            ("Sector", EntityState.Added or EntityState.Modified) => AuditActionCode.CONFIG_SECTOR,
            ("Cuadrante", EntityState.Added or EntityState.Modified) => AuditActionCode.CONFIG_CUADRANTE,
            ("CatalogoSugerencia", EntityState.Added or EntityState.Modified) => AuditActionCode.CONFIG_SUGERENCIA,

            // Reports (will be expanded in User Story 1)
            ("ReporteIncidencia", EntityState.Added) => AuditActionCode.REPORT_CREATE,
            ("ReporteIncidencia", EntityState.Modified) => AuditActionCode.REPORT_UPDATE,

            // User Management (will be expanded in User Story 3)
            ("IdentityUser", EntityState.Added) => AuditActionCode.USER_CREATE,
            ("IdentityUser", EntityState.Modified) => AuditActionCode.USER_UPDATE,
            ("IdentityUser", EntityState.Deleted) => AuditActionCode.USER_DELETE,

            // Automated Reports (will be expanded in User Story 4)
            ("ReporteAutomatizado", EntityState.Added) => AuditActionCode.AUTO_REPORT_GEN,

            _ => null // Don't audit other entities
        };
    }

    private static int? GetEntityId(object entity)
    {
        // Use reflection to get Id property
        var idProperty = entity.GetType().GetProperty("Id");
        if (idProperty == null)
            return null;

        var idValue = idProperty.GetValue(entity);
        return idValue switch
        {
            int intId => intId,
            long longId => (int)longId,
            _ => null
        };
    }

    private static string? BuildAuditDetails(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        if (entry.State == EntityState.Modified)
        {
            var changes = new Dictionary<string, object?>();
            foreach (var property in entry.Properties)
            {
                if (property.IsModified)
                {
                    changes[property.Metadata.Name] = new
                    {
                        OldValue = property.OriginalValue,
                        NewValue = property.CurrentValue
                    };
                }
            }

            if (changes.Count > 0)
                return System.Text.Json.JsonSerializer.Serialize(changes);
        }

        return null;
    }
}
