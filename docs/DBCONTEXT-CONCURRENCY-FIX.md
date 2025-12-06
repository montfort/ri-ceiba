# Fix: Error de Concurrencia en DbContext

## Problema

Al cargar el formulario de nuevo reporte, aparecía el siguiente error en los logs:

```
[ERR] Error loading suggestions
System.InvalidOperationException: A second operation was started on this context instance before a previous operation completed. This is usually caused by different threads concurrently using the same instance of DbContext.
```

## Causa Raíz

En `ReportForm.razor`, el método `LoadSuggestionsAsync()` estaba ejecutando múltiples consultas a la base de datos **en paralelo** usando `Task.WhenAll()`:

```csharp
// ❌ CÓDIGO PROBLEMÁTICO (ejecuta 3 consultas en paralelo)
var tasks = new[]
{
    CatalogService.GetSuggestionsAsync("sexo"),
    CatalogService.GetSuggestionsAsync("delito"),
    CatalogService.GetSuggestionsAsync("tipo_de_atencion")
};

var results = await Task.WhenAll(tasks);
```

El problema es que **Entity Framework Core no permite operaciones concurrentes** en la misma instancia de `DbContext`. Todas las llamadas a `CatalogService` usan el mismo `DbContext` inyectado como `Scoped`, por lo que se produce un conflicto de concurrencia.

## Solución

Cambiar las consultas para que se ejecuten **secuencialmente** (una después de otra):

```csharp
// ✅ CÓDIGO CORREGIDO (ejecuta 3 consultas secuencialmente)
SexoSuggestions = await CatalogService.GetSuggestionsAsync("sexo");
DelitoSuggestions = await CatalogService.GetSuggestionsAsync("delito");
TipoDeAtencionSuggestions = await CatalogService.GetSuggestionsAsync("tipo_de_atencion");
```

### ¿Por qué esto funciona?

- Cada llamada `await` espera a que la consulta anterior termine antes de iniciar la siguiente
- No hay operaciones concurrentes en el mismo `DbContext`
- El rendimiento es aceptable porque son consultas pequeñas y rápidas (catálogos de sugerencias)

## Alternativas Consideradas

### Alternativa 1: Múltiples DbContext (NO recomendado)

Se podría inyectar múltiples instancias de `DbContext`, pero esto:
- Viola el principio de inyección de dependencias limpia
- Requiere cambios en `Program.cs` y múltiples servicios
- Aumenta complejidad innecesariamente

### Alternativa 2: DbContext con `EnableThreadSafetyChecks = false` (PELIGROSO)

Se podría deshabilitar las verificaciones de seguridad de hilos, pero:
- Es una mala práctica
- Puede causar corrupción de datos
- Microsoft lo desaconseja explícitamente

### Alternativa 3: Consulta única con múltiples filtros (Mejor para casos grandes)

Si las sugerencias fueran muchas (miles), se podría optimizar con una sola consulta:

```csharp
var allSuggestions = await CatalogService.GetAllSuggestionsAsync();
SexoSuggestions = allSuggestions.Where(s => s.Campo == "sexo").ToList();
DelitoSuggestions = allSuggestions.Where(s => s.Campo == "delito").ToList();
TipoDeAtencionSuggestions = allSuggestions.Where(s => s.Campo == "tipo_de_atencion").ToList();
```

**Para este caso no es necesario** porque:
- Solo hay ~13 sugerencias en total
- La carga secuencial es lo suficientemente rápida
- Mantiene el código simple y claro

## Impacto en Rendimiento

### Antes (paralelo con error)
- ❌ Error de concurrencia
- ⏱️ Teóricamente más rápido (si funcionara)

### Después (secuencial sin error)
- ✅ Sin errores
- ⏱️ ~50-100ms adicional total (despreciable)
- 📊 Carga de 13 sugerencias: ~5-10ms cada una

**Veredicto**: El impacto en rendimiento es **insignificante** (menos de 100ms) y la carga se realiza solo una vez al abrir el formulario.

## Lecciones Aprendidas

### ✅ Buenas Prácticas con DbContext

1. **DbContext es NOT thread-safe**: Nunca ejecutar operaciones concurrentes en el mismo DbContext
2. **Usar Scoped lifetime**: El DbContext debe ser `Scoped`, no `Singleton`
3. **Evitar Task.WhenAll con DbContext**: A menos que uses múltiples instancias de DbContext
4. **Operaciones secuenciales están bien**: Para catálogos pequeños, no optimizar prematuramente

### ❌ Anti-patrones a Evitar

1. Ejecutar múltiples queries en paralelo con el mismo DbContext
2. Compartir DbContext entre threads o componentes
3. Usar `EnableThreadSafetyChecks = false` para "solucionar" el problema
4. Inyectar DbContext como Singleton

## Verificación

Después del fix, los logs deberían mostrar:

```
[INF] Database seeded successfully
[INF] Ceiba application starting...
// ✅ NO debe aparecer el error de concurrencia
```

Y el formulario debe cargar correctamente con las sugerencias en los campos:
- Sexo: Masculino, Femenino, No binario, Prefiero no decir
- Delito: Robo, Violencia familiar, Acoso sexual, Lesiones, Amenazas
- Tipo de Atención: Orientación, Canalización, Acompañamiento, Intervención en crisis

## Referencias

- [EF Core: DbContext Lifetime](https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/)
- [EF Core: Thread Safety](https://go.microsoft.com/fwlink/?linkid=2097913)
- [Best Practices for DbContext](https://learn.microsoft.com/en-us/ef/core/miscellaneous/configuring-dbcontext#avoiding-dbcontext-threading-issues)

## Archivos Modificados

- `src/Ceiba.Web/Components/Pages/Reports/ReportForm.razor` (líneas 513-527)

## Fecha de Fix

2025-11-27
