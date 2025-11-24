using Ceiba.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ceiba.Infrastructure.Data.Configurations;

public class CatalogoSugerenciaConfiguration : IEntityTypeConfiguration<CatalogoSugerencia>
{
    public void Configure(EntityTypeBuilder<CatalogoSugerencia> builder)
    {
        builder.ToTable("CATALOGO_SUGERENCIA");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(c => c.Campo).HasColumnName("campo").HasMaxLength(50).IsRequired();
        builder.Property(c => c.Valor).HasColumnName("valor").HasMaxLength(200).IsRequired();
        builder.Property(c => c.Orden).HasColumnName("orden").IsRequired().HasDefaultValue(0);
        builder.Property(c => c.Activo).HasColumnName("activo").IsRequired().HasDefaultValue(true);
        builder.Property(c => c.UsuarioId).HasColumnName("usuario_id").IsRequired();
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired().HasDefaultValueSql("NOW()");

        builder.HasIndex(c => new { c.Campo, c.Valor }).IsUnique().HasDatabaseName("idx_sugerencia_campo_valor_unique");
        builder.HasIndex(c => new { c.Campo, c.Activo }).HasDatabaseName("idx_sugerencia_campo_activo");
    }
}
