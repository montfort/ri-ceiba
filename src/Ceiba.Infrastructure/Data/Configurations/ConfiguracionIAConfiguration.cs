using Ceiba.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ceiba.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for ConfiguracionIA entity.
/// </summary>
public class ConfiguracionIAConfiguration : IEntityTypeConfiguration<ConfiguracionIA>
{
    public void Configure(EntityTypeBuilder<ConfiguracionIA> builder)
    {
        builder.ToTable("CONFIGURACION_IA");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Proveedor)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.ApiKey)
            .HasMaxLength(500);

        builder.Property(c => c.Modelo)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.Endpoint)
            .HasMaxLength(500);

        builder.Property(c => c.AzureEndpoint)
            .HasMaxLength(500);

        builder.Property(c => c.AzureApiVersion)
            .HasMaxLength(50);

        builder.Property(c => c.LocalEndpoint)
            .HasMaxLength(500);

        builder.Property(c => c.MaxTokens)
            .HasDefaultValue(1000);

        builder.Property(c => c.Temperature)
            .HasDefaultValue(0.7);

        builder.Property(c => c.Activo)
            .HasDefaultValue(true);

        builder.Property(c => c.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Index for finding active configuration quickly
        builder.HasIndex(c => c.Activo)
            .HasDatabaseName("IX_CONFIGURACION_IA_ACTIVO");
    }
}
