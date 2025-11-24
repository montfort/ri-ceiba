using Ceiba.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ceiba.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración mínima para Phase 2. Se completará en User Story 1.
/// </summary>
public class ReporteIncidenciaConfiguration : IEntityTypeConfiguration<ReporteIncidencia>
{
    public void Configure(EntityTypeBuilder<ReporteIncidencia> builder)
    {
        builder.ToTable("REPORTE_INCIDENCIA");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(r => r.TipoReporte).HasColumnName("tipo_reporte").HasMaxLength(10).IsRequired().HasDefaultValue("A");
        builder.Property(r => r.Estado).HasColumnName("estado").IsRequired().HasDefaultValue((short)0);
        builder.Property(r => r.UsuarioId).HasColumnName("usuario_id").IsRequired();
        builder.Property(r => r.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired().HasDefaultValueSql("NOW()");

        // Relaciones geográficas
        builder.Property(r => r.ZonaId).HasColumnName("zona_id").IsRequired();
        builder.Property(r => r.SectorId).HasColumnName("sector_id").IsRequired();
        builder.Property(r => r.CuadranteId).HasColumnName("cuadrante_id").IsRequired();

        builder.HasOne(r => r.Zona).WithMany(z => z.Reportes).HasForeignKey(r => r.ZonaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.Sector).WithMany(s => s.Reportes).HasForeignKey(r => r.SectorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.Cuadrante).WithMany(c => c.Reportes).HasForeignKey(r => r.CuadranteId).OnDelete(DeleteBehavior.Restrict);

        // Nota: Campos adicionales se agregarán en Phase 3 (User Story 1)
    }
}
