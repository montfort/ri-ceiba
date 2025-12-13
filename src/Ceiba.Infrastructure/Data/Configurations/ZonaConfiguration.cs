using Ceiba.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ceiba.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for Zona entity.
/// Defines table name, column mappings, indexes, and relationships.
/// </summary>
public class ZonaConfiguration : IEntityTypeConfiguration<Zona>
{
    public void Configure(EntityTypeBuilder<Zona> builder)
    {
        // Table name (UPPER_SNAKE_CASE per project conventions)
        builder.ToTable("ZONA");

        // Primary key
        builder.HasKey(z => z.Id);
        builder.Property(z => z.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // Properties
        builder.Property(z => z.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(z => z.Activo)
            .HasColumnName("activo")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(z => z.UsuarioId)
            .HasColumnName("usuario_id")
            .IsRequired();

        builder.Property(z => z.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        // Indexes
        builder.HasIndex(z => z.Nombre)
            .IsUnique()
            .HasDatabaseName("idx_zona_nombre_unique");

        builder.HasIndex(z => z.Activo)
            .HasDatabaseName("idx_zona_activo");

        // Relationships
        builder.HasMany(z => z.Regiones)
            .WithOne(r => r.Zona)
            .HasForeignKey(r => r.ZonaId)
            .OnDelete(DeleteBehavior.Restrict) // Prevent delete if regions exist
            .HasConstraintName("zona_id_FK");
    }
}
