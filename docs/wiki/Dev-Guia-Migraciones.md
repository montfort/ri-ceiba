# Guía: Migraciones de Base de Datos

Esta guía cubre el manejo de migraciones con Entity Framework Core.

## Comandos Básicos

Todos los comandos se ejecutan desde `src/Ceiba.Infrastructure`:

```bash
cd src/Ceiba.Infrastructure
```

### Crear Migración

```bash
dotnet ef migrations add NombreDeMigracion --startup-project ../Ceiba.Web
```

Ejemplos de nombres descriptivos:
- `AddNacionalidadToReporte`
- `CreateAutomatedReportsTable`
- `AddIndexToReportesCreatedAt`
- `RenameColumnaDelito`

### Aplicar Migraciones

```bash
# Aplicar todas las pendientes
dotnet ef database update --startup-project ../Ceiba.Web

# Aplicar hasta una migración específica
dotnet ef database update NombreDeMigracion --startup-project ../Ceiba.Web
```

### Revertir Migración

```bash
# Revertir a la migración anterior
dotnet ef database update MigracionAnterior --startup-project ../Ceiba.Web

# Revertir todas las migraciones (¡cuidado!)
dotnet ef database update 0 --startup-project ../Ceiba.Web
```

### Generar Script SQL

```bash
# Script de todas las migraciones
dotnet ef migrations script --startup-project ../Ceiba.Web --output migration.sql

# Script de un rango específico
dotnet ef migrations script FromMigration ToMigration --startup-project ../Ceiba.Web --output partial.sql

# Script idempotente (seguro para producción)
dotnet ef migrations script --idempotent --startup-project ../Ceiba.Web --output production.sql
```

### Eliminar Última Migración

Solo si no se ha aplicado:

```bash
dotnet ef migrations remove --startup-project ../Ceiba.Web
```

## Estructura de una Migración

```csharp
public partial class AddNacionalidadToReporte : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "nacionalidad",
            table: "reporte_incidencia",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "nacionalidad",
            table: "reporte_incidencia");
    }
}
```

## Buenas Prácticas

### 1. Migraciones Pequeñas y Enfocadas

```csharp
// ✅ Bueno: Una migración por cambio lógico
AddNacionalidadToReporte
AddIndiceReportesEstado
CreatePlantillasTable

// ❌ Malo: Muchos cambios en una migración
UpdateDatabaseEverything
```

### 2. Siempre Implementar Down()

```csharp
protected override void Down(MigrationBuilder migrationBuilder)
{
    // Siempre incluir la operación inversa
    migrationBuilder.DropColumn(...);
}
```

### 3. Datos de Migración

Para insertar datos iniciales:

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.InsertData(
        table: "sugerencia_reporte",
        columns: new[] { "categoria", "valor", "orden", "activo" },
        values: new object[,]
        {
            { "nacionalidad", "Mexicana", 1, true },
            { "nacionalidad", "Estadounidense", 2, true }
        });
}
```

### 4. Índices para Rendimiento

```csharp
migrationBuilder.CreateIndex(
    name: "IX_reporte_incidencia_created_at",
    table: "reporte_incidencia",
    column: "created_at");

migrationBuilder.CreateIndex(
    name: "IX_reporte_incidencia_estado_creador",
    table: "reporte_incidencia",
    columns: new[] { "estado", "creador_id" });
```

## Manejo de Errores

### Migración Fallida

Si una migración falla a mitad de camino:

1. Revisa el error en la consola
2. Corrige el problema
3. Elimina la migración si es posible: `dotnet ef migrations remove`
4. Crea una nueva migración corregida

### Conflictos de Migración

Si hay conflictos con cambios de otros desarrolladores:

1. Revierte a un punto común: `dotnet ef database update MigracionComun`
2. Elimina migraciones conflictivas
3. Crea nueva migración que incluya todos los cambios

## Producción

### Generar Script Idempotente

```bash
dotnet ef migrations script --idempotent -o production.sql --startup-project ../Ceiba.Web
```

### Aplicar en Producción

```bash
# Opción 1: Con la aplicación
dotnet ef database update --startup-project ../Ceiba.Web

# Opción 2: Con script SQL
psql -h host -U user -d ceiba -f production.sql
```

### Verificar Estado

```bash
# Ver migraciones aplicadas
dotnet ef migrations list --startup-project ../Ceiba.Web
```

## Troubleshooting

### "No migrations found"

```bash
# Verificar que estás en el directorio correcto
pwd  # Debe ser src/Ceiba.Infrastructure
```

### "The context is not configured"

```bash
# Asegúrate de usar --startup-project
dotnet ef migrations add Test --startup-project ../Ceiba.Web
```

### "Build failed"

```bash
# Compilar primero
dotnet build ../Ceiba.Web
# Luego crear migración
dotnet ef migrations add Test --startup-project ../Ceiba.Web
```
