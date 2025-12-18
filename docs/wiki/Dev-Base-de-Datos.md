# Base de Datos

El sistema Ceiba utiliza **PostgreSQL 18** como base de datos, gestionada con **Entity Framework Core**.

## Modelo de Datos

### Diagrama ER Simplificado

```
┌─────────────────┐      ┌─────────────────┐
│     Usuario     │      │ ReporteIncidencia│
├─────────────────┤      ├─────────────────┤
│ Id (GUID)       │◄────┐│ Id              │
│ Email           │     ││ CreadorId (FK)  ├──┘
│ Roles           │     │├─────────────────┤
│ Activo          │     ││ Estado          │
└─────────────────┘     ││ DatetimeHechos  │
                        ││ Delito          │
┌─────────────────┐     ││ ZonaId (FK)     ├──┐
│      Zona       │◄────┼│ RegionId (FK)   ├──┼──┐
├─────────────────┤     ││ SectorId (FK)   ├──┼──┼──┐
│ Id              │     ││ CuadranteId(FK) ├──┼──┼──┼──┐
│ Nombre          │     │└─────────────────┘  │  │  │  │
└─────────────────┘     │                     │  │  │  │
        │               │ ┌─────────────────┐ │  │  │  │
        ▼               │ │     Region      │◄┘  │  │  │
┌─────────────────┐     │ ├─────────────────┤    │  │  │
│     Region      │     │ │ Id              │    │  │  │
├─────────────────┤     │ │ Nombre          │    │  │  │
│ Id              │     │ │ ZonaId (FK)     │    │  │  │
│ Nombre          │     │ └─────────────────┘    │  │  │
│ ZonaId (FK)     │     │                        │  │  │
└─────────────────┘     │ ┌─────────────────┐    │  │  │
        │               │ │     Sector      │◄───┘  │  │
        ▼               │ ├─────────────────┤       │  │
┌─────────────────┐     │ │ Id              │       │  │
│     Sector      │     │ │ Nombre          │       │  │
├─────────────────┤     │ │ RegionId (FK)   │       │  │
│ Id              │     │ └─────────────────┘       │  │
│ Nombre          │     │                           │  │
│ RegionId (FK)   │     │ ┌─────────────────┐       │  │
└─────────────────┘     │ │   Cuadrante     │◄──────┘  │
        │               │ ├─────────────────┤          │
        ▼               │ │ Id              │          │
┌─────────────────┐     │ │ Nombre          │          │
│   Cuadrante     │     │ │ SectorId (FK)   │          │
├─────────────────┤     │ └─────────────────┘          │
│ Id              │     │                              │
│ Nombre          │     │                              │
│ SectorId (FK)   │     │                              │
└─────────────────┘     │                              │
                        │                              │
┌─────────────────┐     │                              │
│RegistroAuditoria│     │                              │
├─────────────────┤     │                              │
│ Id              │     │                              │
│ UsuarioId       │     │                              │
│ Accion          │     │                              │
│ Entidad         │     │                              │
│ Timestamp       │     │                              │
└─────────────────┘     │                              │
```

## Entidades Principales

### Usuario

```csharp
public class Usuario : IdentityUser<Guid>
{
    public bool Activo { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // Navigation
    public virtual ICollection<ReporteIncidencia> Reportes { get; set; }
}
```

### ReporteIncidencia

```csharp
public class ReporteIncidencia
{
    public int Id { get; set; }
    public string TipoReporte { get; set; } = "A";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime DatetimeHechos { get; set; }
    public int Estado { get; set; } // 0=Borrador, 1=Entregado

    // Víctima
    public string Sexo { get; set; }
    public int Edad { get; set; }
    public bool LgbtttiqPlus { get; set; }
    public bool SituacionCalle { get; set; }
    public bool Migrante { get; set; }
    public bool Discapacidad { get; set; }

    // Ubicación
    public int ZonaId { get; set; }
    public int RegionId { get; set; }
    public int SectorId { get; set; }
    public int CuadranteId { get; set; }

    // Incidencia
    public string Delito { get; set; }
    public string TipoDeAtencion { get; set; }

    // Operativo
    public string TurnoCeiba { get; set; }
    public string TipoDeAccion { get; set; }
    public string Traslados { get; set; }

    // Narrativa
    public string HechosReportados { get; set; }
    public string AccionesRealizadas { get; set; }
    public string? Observaciones { get; set; }

    // Relaciones
    public Guid CreadorId { get; set; }
    public virtual Usuario Creador { get; set; }
    public virtual Zona Zona { get; set; }
    public virtual Region Region { get; set; }
    public virtual Sector Sector { get; set; }
    public virtual Cuadrante Cuadrante { get; set; }
}
```

## Convenciones de Base de Datos

### Nombrado de Tablas

- Tablas en UPPER_SNAKE_CASE: `REPORTE_INCIDENCIA`
- Columnas en lower_snake_case: `created_at`

### Tipos de Datos

| C# Type | PostgreSQL Type |
|---------|-----------------|
| DateTime | TIMESTAMPTZ (UTC) |
| string | VARCHAR o TEXT |
| bool | BOOLEAN |
| int | INTEGER |
| Guid | UUID |
| decimal | NUMERIC |

### Timestamps

Todos los timestamps usan **TIMESTAMPTZ** y se almacenan en **UTC**.

```csharp
public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
```

## Migraciones

### Crear Migración

```bash
cd src/Ceiba.Infrastructure
dotnet ef migrations add NombreDeMigracion --startup-project ../Ceiba.Web
```

### Aplicar Migración

```bash
dotnet ef database update --startup-project ../Ceiba.Web
```

### Revertir Migración

```bash
dotnet ef database update MigracionAnterior --startup-project ../Ceiba.Web
```

### Generar Script SQL

```bash
dotnet ef migrations script --startup-project ../Ceiba.Web --output migration.sql
```

## DbContext

```csharp
public class CeibaDbContext : IdentityDbContext<Usuario, IdentityRole<Guid>, Guid>
{
    public DbSet<ReporteIncidencia> Reportes { get; set; }
    public DbSet<Zona> Zonas { get; set; }
    public DbSet<Region> Regiones { get; set; }
    public DbSet<Sector> Sectores { get; set; }
    public DbSet<Cuadrante> Cuadrantes { get; set; }
    public DbSet<RegistroAuditoria> Auditoria { get; set; }
    public DbSet<SugerenciaReporte> Sugerencias { get; set; }
    public DbSet<PlantillaReporte> Plantillas { get; set; }
    public DbSet<ReporteAutomatizado> ReportesAutomatizados { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(CeibaDbContext).Assembly);
    }
}
```

## Configuración Fluent API

```csharp
public class ReporteIncidenciaConfiguration : IEntityTypeConfiguration<ReporteIncidencia>
{
    public void Configure(EntityTypeBuilder<ReporteIncidencia> builder)
    {
        builder.ToTable("REPORTE_INCIDENCIA");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Delito)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.HechosReportados)
            .IsRequired()
            .HasMaxLength(10000);

        builder.HasOne(r => r.Creador)
            .WithMany(u => u.Reportes)
            .HasForeignKey(r => r.CreadorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.CreadorId);
        builder.HasIndex(r => r.Estado);
        builder.HasIndex(r => r.CreatedAt);
    }
}
```

## Próximos Pasos

- [[Dev-Guia-Migraciones|Guía detallada de migraciones]]
- [[Dev-API-Referencia|Referencia de API]]
- [[Dev-Arquitectura|Arquitectura del sistema]]
