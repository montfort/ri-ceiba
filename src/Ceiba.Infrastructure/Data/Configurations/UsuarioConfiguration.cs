using Ceiba.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ceiba.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for Usuario entity.
/// Extends Identity table with application-specific columns.
/// </summary>
/// <remarks>
/// The base USUARIO table is created by ASP.NET Identity.
/// This configuration adds: nombre, created_at, last_login_at columns.
/// </remarks>
public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        // Table name already configured in CeibaDbContext.ConfigureIdentityTables()
        // but we need to ensure it's set for our custom properties
        builder.ToTable("USUARIO");

        // Custom properties (not in base IdentityUser)
        builder.Property(u => u.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(100)
            .IsRequired()
            .HasDefaultValue(string.Empty);

        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(u => u.LastLoginAt)
            .HasColumnName("last_login_at")
            .HasColumnType("timestamptz")
            .IsRequired(false);

        builder.Property(u => u.Activo)
            .HasColumnName("activo")
            .IsRequired()
            .HasDefaultValue(true);

        // Indexes
        builder.HasIndex(u => u.CreatedAt)
            .HasDatabaseName("idx_usuario_created_at");

        builder.HasIndex(u => u.LastLoginAt)
            .HasDatabaseName("idx_usuario_last_login_at");

        // Relationship: Usuario -> ReporteIncidencia (one-to-many)
        builder.HasMany(u => u.Reportes)
            .WithOne()
            .HasForeignKey(r => r.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict) // Prevent delete if reports exist
            .HasConstraintName("usuario_reportes_FK");
    }
}
