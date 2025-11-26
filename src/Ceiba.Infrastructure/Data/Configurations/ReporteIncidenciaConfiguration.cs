using Ceiba.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ceiba.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for ReporteIncidencia entity (Type A).
/// US1: T032
/// </summary>
public class ReporteIncidenciaConfiguration : IEntityTypeConfiguration<ReporteIncidencia>
{
    public void Configure(EntityTypeBuilder<ReporteIncidencia> builder)
    {
        builder.ToTable("REPORTE_INCIDENCIA");

        // Primary Key
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").ValueGeneratedOnAdd();

        // Basic Fields
        builder.Property(r => r.TipoReporte)
            .HasColumnName("tipo_reporte")
            .HasMaxLength(10)
            .IsRequired()
            .HasDefaultValue("A");

        builder.Property(r => r.Estado)
            .HasColumnName("estado")
            .IsRequired()
            .HasDefaultValue((short)0);

        builder.Property(r => r.UsuarioId)
            .HasColumnName("usuario_id")
            .IsRequired();

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(r => r.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz")
            .IsRequired(false);

        // Incident Details
        builder.Property(r => r.DatetimeHechos)
            .HasColumnName("datetime_hechos")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(r => r.Sexo)
            .HasColumnName("sexo")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.Edad)
            .HasColumnName("edad")
            .IsRequired();

        builder.Property(r => r.LgbtttiqPlus)
            .HasColumnName("lgbtttiq_plus")
            .HasDefaultValue(false);

        builder.Property(r => r.SituacionCalle)
            .HasColumnName("situacion_calle")
            .HasDefaultValue(false);

        builder.Property(r => r.Migrante)
            .HasColumnName("migrante")
            .HasDefaultValue(false);

        builder.Property(r => r.Discapacidad)
            .HasColumnName("discapacidad")
            .HasDefaultValue(false);

        builder.Property(r => r.Delito)
            .HasColumnName("delito")
            .HasMaxLength(100)
            .IsRequired();

        // Geographic Relations
        builder.Property(r => r.ZonaId)
            .HasColumnName("zona_id")
            .IsRequired();

        builder.Property(r => r.SectorId)
            .HasColumnName("sector_id")
            .IsRequired();

        builder.Property(r => r.CuadranteId)
            .HasColumnName("cuadrante_id")
            .IsRequired();

        builder.HasOne(r => r.Zona)
            .WithMany(z => z.Reportes)
            .HasForeignKey(r => r.ZonaId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_REPORTE_ZONA");

        builder.HasOne(r => r.Sector)
            .WithMany(s => s.Reportes)
            .HasForeignKey(r => r.SectorId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_REPORTE_SECTOR");

        builder.HasOne(r => r.Cuadrante)
            .WithMany(c => c.Reportes)
            .HasForeignKey(r => r.CuadranteId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_REPORTE_CUADRANTE");

        // Operational Details
        builder.Property(r => r.TurnoCeiba)
            .HasColumnName("turno_ceiba")
            .IsRequired();

        builder.Property(r => r.TipoDeAtencion)
            .HasColumnName("tipo_de_atencion")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(r => r.TipoDeAccion)
            .HasColumnName("tipo_de_accion")
            .IsRequired();

        builder.Property(r => r.HechosReportados)
            .HasColumnName("hechos_reportados")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(r => r.AccionesRealizadas)
            .HasColumnName("acciones_realizadas")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(r => r.Traslados)
            .HasColumnName("traslados")
            .IsRequired();

        builder.Property(r => r.Observaciones)
            .HasColumnName("observaciones")
            .HasColumnType("text")
            .IsRequired(false);

        // Extensibility Fields (RT-004 Mitigation)
        builder.Property(r => r.CamposAdicionales)
            .HasColumnName("campos_adicionales")
            .HasColumnType("jsonb")
            .IsRequired(false);

        builder.Property(r => r.SchemaVersion)
            .HasColumnName("schema_version")
            .HasMaxLength(10)
            .IsRequired()
            .HasDefaultValue("1.0");

        // Indexes for Performance (RT-002 Mitigation)
        builder.HasIndex(r => r.UsuarioId)
            .HasDatabaseName("idx_reporte_usuario");

        builder.HasIndex(r => r.Estado)
            .HasDatabaseName("idx_reporte_estado");

        builder.HasIndex(r => r.CreatedAt)
            .HasDatabaseName("idx_reporte_fecha")
            .IsDescending();

        builder.HasIndex(r => r.ZonaId)
            .HasDatabaseName("idx_reporte_zona");

        builder.HasIndex(r => r.Delito)
            .HasDatabaseName("idx_reporte_delito");

        // Composite indexes for common queries
        builder.HasIndex(r => new { r.Estado, r.ZonaId, r.CreatedAt })
            .HasDatabaseName("idx_reporte_composite_search")
            .IsDescending(false, false, true);

        builder.HasIndex(r => new { r.CreatedAt, r.Estado, r.Delito })
            .HasDatabaseName("idx_reporte_composite_revisor")
            .IsDescending(true, false, false);

        // Check Constraints
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_REPORTE_EDAD", "edad > 0 AND edad < 150");
            t.HasCheckConstraint("CK_REPORTE_ESTADO", "estado IN (0, 1)");
            t.HasCheckConstraint("CK_REPORTE_TIPO_ACCION", "tipo_de_accion IN (1, 2, 3)");
            t.HasCheckConstraint("CK_REPORTE_TRASLADOS", "traslados IN (0, 1, 2)");
        });
    }
}
