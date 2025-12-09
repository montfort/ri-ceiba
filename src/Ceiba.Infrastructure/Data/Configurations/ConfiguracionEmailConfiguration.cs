using Ceiba.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ceiba.Infrastructure.Data.Configurations;

public class ConfiguracionEmailConfiguration : IEntityTypeConfiguration<ConfiguracionEmail>
{
    public void Configure(EntityTypeBuilder<ConfiguracionEmail> builder)
    {
        builder.ToTable("configuracion_email");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(c => c.Proveedor)
            .HasColumnName("proveedor")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.Habilitado)
            .HasColumnName("habilitado")
            .IsRequired()
            .HasDefaultValue(false);

        // SMTP Configuration
        builder.Property(c => c.SmtpHost)
            .HasColumnName("smtp_host")
            .HasMaxLength(255);

        builder.Property(c => c.SmtpPort)
            .HasColumnName("smtp_port");

        builder.Property(c => c.SmtpUsername)
            .HasColumnName("smtp_username")
            .HasMaxLength(255);

        builder.Property(c => c.SmtpPassword)
            .HasColumnName("smtp_password")
            .HasMaxLength(500);

        builder.Property(c => c.SmtpUseSsl)
            .HasColumnName("smtp_use_ssl")
            .IsRequired()
            .HasDefaultValue(true);

        // SendGrid Configuration
        builder.Property(c => c.SendGridApiKey)
            .HasColumnName("sendgrid_api_key")
            .HasMaxLength(500);

        // Common Configuration
        builder.Property(c => c.FromEmail)
            .HasColumnName("from_email")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(c => c.FromName)
            .HasColumnName("from_name")
            .HasMaxLength(255)
            .IsRequired();

        // Test results
        builder.Property(c => c.LastTestedAt)
            .HasColumnName("last_tested_at")
            .HasColumnType("timestamptz");

        builder.Property(c => c.LastTestSuccess)
            .HasColumnName("last_test_success");

        builder.Property(c => c.LastTestError)
            .HasColumnName("last_test_error")
            .HasMaxLength(1000);

        // Audit fields
        builder.Property(c => c.UsuarioId)
            .HasColumnName("usuario_id")
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz");

        // Relationships
        builder.HasOne<IdentityUser<Guid>>()
            .WithMany()
            .HasForeignKey(c => c.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(c => c.Proveedor)
            .HasDatabaseName("ix_configuracion_email_proveedor");

        builder.HasIndex(c => c.Habilitado)
            .HasDatabaseName("ix_configuracion_email_habilitado");

        builder.HasIndex(c => c.CreatedAt)
            .HasDatabaseName("ix_configuracion_email_created_at");
    }
}
