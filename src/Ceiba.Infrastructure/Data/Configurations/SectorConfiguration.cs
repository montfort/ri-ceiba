using Ceiba.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ceiba.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for Sector entity.
/// Defines table name, column mappings, indexes, and relationships.
/// </summary>
public class SectorConfiguration : IEntityTypeConfiguration<Sector>
{
    public void Configure(EntityTypeBuilder<Sector> builder)
    {
        // Table name
        builder.ToTable("SECTOR");

        // Primary key
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // Properties
        builder.Property(s => s.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.ZonaId)
            .HasColumnName("zona_id")
            .IsRequired();

        builder.Property(s => s.Activo)
            .HasColumnName("activo")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(s => s.UsuarioId)
            .HasColumnName("usuario_id")
            .IsRequired();

        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        // Indexes
        builder.HasIndex(s => new { s.ZonaId, s.Nombre })
            .IsUnique()
            .HasDatabaseName("idx_sector_zona_nombre_unique");

        builder.HasIndex(s => s.Activo)
            .HasDatabaseName("idx_sector_activo");

        // Relationships
        builder.HasOne(s => s.Zona)
            .WithMany(z => z.Sectores)
            .HasForeignKey(s => s.ZonaId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("zona_id_FK");

        builder.HasMany(s => s.Cuadrantes)
            .WithOne(c => c.Sector)
            .HasForeignKey(c => c.SectorId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("sector_id_FK");
    }
}
