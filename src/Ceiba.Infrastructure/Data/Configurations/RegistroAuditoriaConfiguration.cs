using Ceiba.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ceiba.Infrastructure.Data.Configurations;

public class RegistroAuditoriaConfiguration : IEntityTypeConfiguration<RegistroAuditoria>
{
    public void Configure(EntityTypeBuilder<RegistroAuditoria> builder)
    {
        builder.ToTable("AUDITORIA");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(r => r.Codigo).HasColumnName("codigo").HasMaxLength(50).IsRequired();
        builder.Property(r => r.IdRelacionado).HasColumnName("id_relacionado");
        builder.Property(r => r.TablaRelacionada).HasColumnName("tabla_relacionada").HasMaxLength(50);
        builder.Property(r => r.UsuarioId).HasColumnName("usuario_id");
        builder.Property(r => r.Ip).HasColumnName("ip").HasMaxLength(45);
        builder.Property(r => r.Detalles).HasColumnName("detalles").HasColumnType("jsonb");
        builder.Property(r => r.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired().HasDefaultValueSql("NOW()");

        // Indexes para búsquedas de auditoría (FR-035a)
        builder.HasIndex(r => r.CreatedAt).HasDatabaseName("idx_auditoria_fecha");
        builder.HasIndex(r => r.UsuarioId).HasDatabaseName("idx_auditoria_usuario");
        builder.HasIndex(r => r.Codigo).HasDatabaseName("idx_auditoria_codigo");
        builder.HasIndex(r => new { r.TablaRelacionada, r.IdRelacionado }).HasDatabaseName("idx_auditoria_entidad");
    }
}
