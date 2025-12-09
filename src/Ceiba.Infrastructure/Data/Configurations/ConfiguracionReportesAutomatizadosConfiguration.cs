using Ceiba.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ceiba.Infrastructure.Data.Configurations;

public class ConfiguracionReportesAutomatizadosConfiguration : IEntityTypeConfiguration<ConfiguracionReportesAutomatizados>
{
    public void Configure(EntityTypeBuilder<ConfiguracionReportesAutomatizados> builder)
    {
        builder.ToTable("CONFIGURACION_REPORTES_AUTOMATIZADOS");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id")
            .UseIdentityByDefaultColumn();

        builder.Property(c => c.Habilitado)
            .HasColumnName("habilitado")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(c => c.HoraGeneracion)
            .HasColumnName("hora_generacion")
            .IsRequired()
            .HasDefaultValue(new TimeSpan(6, 0, 0));

        builder.Property(c => c.Destinatarios)
            .HasColumnName("destinatarios")
            .HasMaxLength(2000)
            .IsRequired()
            .HasDefaultValue(string.Empty);

        builder.Property(c => c.RutaSalida)
            .HasColumnName("ruta_salida")
            .HasMaxLength(500)
            .IsRequired()
            .HasDefaultValue("./generated-reports");

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .HasDefaultValueSql("now()");

        builder.Property(c => c.UsuarioId)
            .HasColumnName("usuario_id")
            .IsRequired();

        // Foreign key to Usuario (Identity)
        builder.HasOne<IdentityUser<Guid>>()
            .WithMany()
            .HasForeignKey(c => c.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Index for efficient queries
        builder.HasIndex(c => c.Habilitado)
            .HasDatabaseName("idx_config_reportes_habilitado");
    }
}
