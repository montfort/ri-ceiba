# ADR-002: Uso de PostgreSQL

**Estado:** Aceptado
**Fecha:** 2025-01-15
**Autores:** Equipo de Desarrollo

## Contexto

Necesitamos una base de datos que:
- Sea robusta y confiable para datos críticos
- Tenga buen rendimiento con consultas complejas
- Sea open source (sin costos de licencia)
- Sea compatible con Entity Framework Core
- Funcione bien en contenedores Docker

## Opciones Consideradas

### Opción 1: PostgreSQL

- Base de datos relacional open source
- Extensiones avanzadas (JSON, GIS, etc.)
- Excelente rendimiento

### Opción 2: SQL Server

- Producto Microsoft
- Excelente integración con .NET
- Costos de licencia en producción

### Opción 3: MySQL/MariaDB

- Popular y probado
- Algunas limitaciones en consultas complejas
- Menos funcionalidades avanzadas

### Opción 4: SQLite

- Embebido, sin servidor
- Limitado para concurrencia
- Solo para desarrollo/testing

## Decisión

**Elegimos PostgreSQL** por las siguientes razones:

1. **Open Source**: Sin costos de licencia
2. **Rendimiento**: Excelente para consultas analíticas
3. **JSONB**: Soporte nativo para datos semi-estructurados
4. **Timestamps con zona**: TIMESTAMPTZ para manejo correcto de UTC
5. **Madurez**: Décadas de desarrollo y estabilidad
6. **Docker**: Imágenes oficiales bien mantenidas

## Consecuencias

### Positivas

- Sin costos de licenciamiento
- Excelente soporte en EF Core con Npgsql
- Funcionalidades avanzadas disponibles
- Comunidad activa y documentación extensa

### Negativas

- Menos familiar que SQL Server para desarrolladores .NET
- Tooling menos integrado en Visual Studio
- Requiere aprender diferencias de sintaxis

### Configuración Clave

```csharp
// Connection string
Host=localhost;Database=ceiba;Username=ceiba;Password=secret

// Timestamps siempre en UTC
builder.Property(e => e.CreatedAt)
    .HasColumnType("timestamptz");
```

## Versión Elegida

PostgreSQL 18 por:
- Mejoras de rendimiento
- Nuevas funcionalidades de particionamiento
- Mejor manejo de JSON

## Alternativas para el Futuro

Si las necesidades cambian:
- Migrar a Azure Database for PostgreSQL para cloud
- Usar read replicas para escalabilidad
- Considerar Citus para sharding

## Referencias

- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [Npgsql EF Core Provider](https://www.npgsql.org/efcore/)
- [PostgreSQL vs SQL Server](https://www.postgresql.org/about/featurematrix/)
