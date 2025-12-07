using Ceiba.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ceiba.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for ReporteAutomatizado entity.
/// Defines table name, column mappings, indexes, and relationships.
/// US4: Reportes Automatizados Diarios con IA.
/// </summary>
public class ReporteAutomatizadoConfiguration : IEntityTypeConfiguration<ReporteAutomatizado>
{
    public void Configure(EntityTypeBuilder<ReporteAutomatizado> builder)
    {
        // Table name (UPPER_SNAKE_CASE per project conventions)
        builder.ToTable("REPORTE_AUTOMATIZADO");

        // Primary key
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // Properties
        builder.Property(r => r.FechaInicio)
            .HasColumnName("fecha_inicio")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(r => r.FechaFin)
            .HasColumnName("fecha_fin")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(r => r.ContenidoMarkdown)
            .HasColumnName("contenido_markdown")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(r => r.ContenidoWordPath)
            .HasColumnName("contenido_word_path")
            .HasMaxLength(500);

        builder.Property(r => r.Estadisticas)
            .HasColumnName("estadisticas")
            .HasColumnType("jsonb")
            .IsRequired()
            .HasDefaultValue("{}");

        builder.Property(r => r.Enviado)
            .HasColumnName("enviado")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(r => r.FechaEnvio)
            .HasColumnName("fecha_envio")
            .HasColumnType("timestamptz");

        builder.Property(r => r.ErrorMensaje)
            .HasColumnName("error_mensaje")
            .HasColumnType("text");

        builder.Property(r => r.ModeloReporteId)
            .HasColumnName("modelo_reporte_id");

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        // Indexes
        builder.HasIndex(r => r.FechaInicio)
            .HasDatabaseName("idx_reporte_auto_fecha_inicio");

        builder.HasIndex(r => r.Enviado)
            .HasDatabaseName("idx_reporte_auto_enviado");

        builder.HasIndex(r => r.CreatedAt)
            .IsDescending()
            .HasDatabaseName("idx_reporte_auto_created");

        // Relationships
        builder.HasOne(r => r.ModeloReporte)
            .WithMany(m => m.ReportesGenerados)
            .HasForeignKey(r => r.ModeloReporteId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("modelo_reporte_id_FK");
    }
}
