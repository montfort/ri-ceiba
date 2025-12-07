using Ceiba.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ceiba.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for ModeloReporte entity.
/// Defines table name, column mappings, indexes, and relationships.
/// US4: Reportes Automatizados Diarios con IA.
/// </summary>
public class ModeloReporteConfiguration : IEntityTypeConfiguration<ModeloReporte>
{
    public void Configure(EntityTypeBuilder<ModeloReporte> builder)
    {
        // Table name (UPPER_SNAKE_CASE per project conventions)
        builder.ToTable("MODELO_REPORTE");

        // Primary key
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // Properties
        builder.Property(m => m.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(m => m.Descripcion)
            .HasColumnName("descripcion")
            .HasMaxLength(500);

        builder.Property(m => m.ContenidoMarkdown)
            .HasColumnName("contenido_markdown")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(m => m.Activo)
            .HasColumnName("activo")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(m => m.EsDefault)
            .HasColumnName("es_default")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(m => m.UsuarioId)
            .HasColumnName("usuario_id")
            .IsRequired();

        builder.Property(m => m.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(m => m.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz");

        // Indexes
        builder.HasIndex(m => m.Nombre)
            .IsUnique()
            .HasDatabaseName("idx_modelo_nombre_unique");

        builder.HasIndex(m => m.Activo)
            .HasDatabaseName("idx_modelo_activo");

        builder.HasIndex(m => m.EsDefault)
            .HasDatabaseName("idx_modelo_default");
    }
}
