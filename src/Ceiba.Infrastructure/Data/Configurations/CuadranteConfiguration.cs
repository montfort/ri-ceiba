using Ceiba.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ceiba.Infrastructure.Data.Configurations;

public class CuadranteConfiguration : IEntityTypeConfiguration<Cuadrante>
{
    public void Configure(EntityTypeBuilder<Cuadrante> builder)
    {
        builder.ToTable("CUADRANTE");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(c => c.Nombre).HasColumnName("nombre").HasMaxLength(100).IsRequired();
        builder.Property(c => c.SectorId).HasColumnName("sector_id").IsRequired();
        builder.Property(c => c.Activo).HasColumnName("activo").IsRequired().HasDefaultValue(true);
        builder.Property(c => c.UsuarioId).HasColumnName("usuario_id").IsRequired();
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired().HasDefaultValueSql("NOW()");

        builder.HasIndex(c => new { c.SectorId, c.Nombre }).IsUnique().HasDatabaseName("idx_cuadrante_sector_nombre_unique");
        builder.HasIndex(c => c.Activo).HasDatabaseName("idx_cuadrante_activo");

        builder.HasOne(c => c.Sector).WithMany(s => s.Cuadrantes).HasForeignKey(c => c.SectorId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("sector_id_FK");
    }
}
