using Ceiba.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ceiba.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for Region entity.
/// Defines table name, column mappings, indexes, and relationships.
/// Part of hierarchical catalog: Zona → Región → Sector → Cuadrante
/// </summary>
public class RegionConfiguration : IEntityTypeConfiguration<Region>
{
    public void Configure(EntityTypeBuilder<Region> builder)
    {
        // Table name (UPPER_SNAKE_CASE per project conventions)
        builder.ToTable("REGION");

        // Primary key
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // Properties
        builder.Property(r => r.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(r => r.ZonaId)
            .HasColumnName("zona_id")
            .IsRequired();

        builder.Property(r => r.Activo)
            .HasColumnName("activo")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(r => r.UsuarioId)
            .HasColumnName("usuario_id")
            .IsRequired();

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        // Indexes
        builder.HasIndex(r => new { r.ZonaId, r.Nombre })
            .IsUnique()
            .HasDatabaseName("idx_region_zona_nombre_unique");

        builder.HasIndex(r => r.Activo)
            .HasDatabaseName("idx_region_activo");

        builder.HasIndex(r => r.ZonaId)
            .HasDatabaseName("idx_region_zona");

        // Relationships
        builder.HasOne(r => r.Zona)
            .WithMany(z => z.Regiones)
            .HasForeignKey(r => r.ZonaId)
            .OnDelete(DeleteBehavior.Restrict) // Prevent delete if regions exist
            .HasConstraintName("region_zona_id_FK");

        builder.HasMany(r => r.Sectores)
            .WithOne(s => s.Region)
            .HasForeignKey(s => s.RegionId)
            .OnDelete(DeleteBehavior.Restrict) // Prevent delete if sectors exist
            .HasConstraintName("sector_region_id_FK");
    }
}
