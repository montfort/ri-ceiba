# Datos de Prueba - Jerarquía Geográfica

Este documento describe los datos de prueba generados automáticamente para probar la funcionalidad de cascada en los campos de ubicación geográfica (Zona → Sector → Cuadrante).

## Estructura Generada

El `SeedDataService` crea automáticamente la siguiente estructura jerárquica:

### Zonas (5 zonas)

| ID | Nombre |
|----|--------|
| 1 | Zona Norte |
| 2 | Zona Sur |
| 3 | Zona Centro |
| 4 | Zona Oriente |
| 5 | Zona Poniente |

### Sectores por Zona

Cada zona tiene **3 o 4 sectores** (alternando):

#### Zona Norte (3 sectores)
- Sector Centro
- Sector Este
- Sector Oeste

#### Zona Sur (4 sectores)
- Sector Centro
- Sector Este
- Sector Oeste
- Sector Residencial

#### Zona Centro (3 sectores)
- Sector Centro
- Sector Este
- Sector Oeste

#### Zona Oriente (4 sectores)
- Sector Centro
- Sector Este
- Sector Oeste
- Sector Residencial

#### Zona Poniente (3 sectores)
- Sector Centro
- Sector Este
- Sector Oeste

**Total: 17 sectores**

### Cuadrantes por Sector

Cada sector tiene **3, 4 o 5 cuadrantes** (variando cíclicamente):

- Cuadrante A
- Cuadrante B
- Cuadrante C
- Cuadrante D (en sectores con 4+)
- Cuadrante E (en sectores con 5)

**Total: ~65 cuadrantes**

## Ejemplo de Cascada

### Flujo de Selección

1. **Usuario selecciona "Zona Norte"**
   - El dropdown de Sector se habilita
   - Se muestran 3 opciones:
     - Sector Centro
     - Sector Este
     - Sector Oeste

2. **Usuario selecciona "Sector Centro"**
   - El dropdown de Cuadrante se habilita
   - Se muestran los cuadrantes asociados a ese sector específico:
     - Cuadrante A
     - Cuadrante B
     - Cuadrante C
     - (etc., dependiendo del sector)

3. **Usuario selecciona "Cuadrante A"**
   - La selección se completa

### Comportamiento Esperado

✅ **Cascada Correcta:**
- Cambiar la Zona resetea Sector y Cuadrante
- Cambiar el Sector resetea Cuadrante
- Los dropdowns dependientes están deshabilitados hasta que se seleccione el padre

✅ **Filtrado Correcto:**
- Solo se muestran sectores de la zona seleccionada
- Solo se muestran cuadrantes del sector seleccionado

## Verificación en Base de Datos

Para verificar los datos generados:

```sql
-- Ver todas las zonas
SELECT "Id", "Nombre", "Activo" FROM "Zonas" ORDER BY "Nombre";

-- Ver sectores por zona
SELECT z."Nombre" AS "Zona", s."Nombre" AS "Sector", s."Id" AS "SectorId"
FROM "Sectores" s
JOIN "Zonas" z ON s."ZonaId" = z."Id"
ORDER BY z."Nombre", s."Nombre";

-- Ver cuadrantes por sector
SELECT z."Nombre" AS "Zona", s."Nombre" AS "Sector", c."Nombre" AS "Cuadrante"
FROM "Cuadrantes" c
JOIN "Sectores" s ON c."SectorId" = s."Id"
JOIN "Zonas" z ON s."ZonaId" = z."Id"
ORDER BY z."Nombre", s."Nombre", c."Nombre";

-- Contar registros por nivel
SELECT
    (SELECT COUNT(*) FROM "Zonas") AS "Total Zonas",
    (SELECT COUNT(*) FROM "Sectores") AS "Total Sectores",
    (SELECT COUNT(*) FROM "Cuadrantes") AS "Total Cuadrantes";
```

## Resetear Datos de Prueba

Si necesitas regenerar los datos de prueba con la estructura actualizada:

### Opción 1: Script PowerShell Estándar

```powershell
.\scripts\reset-database.ps1
```

Este script:
1. Elimina la base de datos actual (requiere permisos suficientes)
2. Aplica todas las migraciones
3. Ejecuta el seed automáticamente
4. Muestra un resumen de los datos creados

**⚠️ Si obtienes error "debe ser dueño de la base de datos ceiba"**, usa la Opción 2.

### Opción 2: Script con Usuario Postgres (Recomendado para Problemas de Permisos)

```powershell
.\scripts\reset-database-with-postgres.ps1
```

Este script alternativo:
1. Usa el usuario `postgres` (superusuario) para eliminar la base de datos
2. Recrea la base de datos con el usuario `ceiba` como propietario
3. Aplica migraciones y ejecuta seed

**Nota**: El script te pedirá la contraseña del usuario `postgres`.

### Opción 3: Corregir Permisos Manualmente

Si prefieres corregir los permisos una sola vez:

```powershell
# 1. Conectar como postgres y ejecutar el script SQL
psql -h localhost -U postgres -d postgres -f scripts/fix-database-ownership.sql

# 2. Luego usar el script estándar
.\scripts\reset-database.ps1
```

### Opción 4: Comandos Manuales

```powershell
# 1. Eliminar base de datos como postgres
psql -h localhost -U postgres -d postgres -c "DROP DATABASE IF EXISTS ceiba;"

# 2. Crear base de datos con propietario correcto
psql -h localhost -U postgres -d postgres -c "CREATE DATABASE ceiba OWNER ceiba;"

# 3. Aplicar migraciones
cd src/Ceiba.Web
dotnet ef database update

# 4. Ejecutar aplicación (seed se ejecuta automáticamente)
dotnet run
```

## Endpoints API para Cascada

Los siguientes endpoints soportan la funcionalidad de cascada:

```
GET /api/catalogs/zonas
  → Retorna todas las zonas activas

GET /api/catalogs/sectores?zonaId={id}
  → Retorna sectores de una zona específica

GET /api/catalogs/cuadrantes?sectorId={id}
  → Retorna cuadrantes de un sector específico
```

## Componentes Blazor

Los componentes que implementan la cascada:

- **CascadingSelect.razor**: Componente reutilizable para dropdowns con cascada
- **ReportForm.razor**: Formulario que usa los 3 CascadingSelect en cascada

### Ejemplo de Uso

```razor
<!-- Zona -->
<CascadingSelect Id="zona"
                 Label="Zona"
                 TValue="int"
                 Value="@Model.ZonaId"
                 ValueChanged="@OnZonaChanged"
                 Items="@Zonas"
                 Required="true" />

<!-- Sector (depende de Zona) -->
<CascadingSelect Id="sector"
                 Label="Sector"
                 TValue="int"
                 Value="@Model.SectorId"
                 ValueChanged="@OnSectorChanged"
                 Items="@Sectores"
                 Required="true"
                 Disabled="@(Model.ZonaId == 0)" />

<!-- Cuadrante (depende de Sector) -->
<CascadingSelect Id="cuadrante"
                 Label="Cuadrante"
                 TValue="int"
                 Value="@Model.CuadranteId"
                 ValueChanged="@OnCuadranteChanged"
                 Items="@Cuadrantes"
                 Required="true"
                 Disabled="@(Model.SectorId == 0)" />
```

## Testing Manual

### Escenario 1: Cascada Básica

1. Iniciar sesión como `creador@test.com / Creador123!`
2. Ir a "Nuevo Reporte"
3. Seleccionar "Zona Norte" en el dropdown de Zona
4. ✅ Verificar que el dropdown de Sector se habilita
5. ✅ Verificar que aparecen solo los 3 sectores de Zona Norte
6. Seleccionar "Sector Centro"
7. ✅ Verificar que el dropdown de Cuadrante se habilita
8. ✅ Verificar que aparecen solo los cuadrantes de ese sector

### Escenario 2: Reseteo en Cascada

1. Seleccionar: Zona Norte → Sector Centro → Cuadrante A
2. Cambiar la Zona a "Zona Sur"
3. ✅ Verificar que Sector y Cuadrante se resetean a vacío
4. ✅ Verificar que el dropdown de Cuadrante se deshabilita
5. Seleccionar un nuevo Sector
6. ✅ Verificar que aparecen cuadrantes diferentes (de Zona Sur)

### Escenario 3: Validación

1. Intentar guardar sin seleccionar Zona
2. ✅ Verificar mensaje de validación
3. Seleccionar Zona pero no Sector
4. ✅ Verificar mensaje de validación
5. Seleccionar Zona y Sector pero no Cuadrante
6. ✅ Verificar mensaje de validación

## Notas Técnicas

### Lógica de Cascada (ReportForm.razor)

```csharp
private Task OnZonaChanged(int value)
{
    Model.ZonaId = value;
    // Reset dependientes
    Model.SectorId = 0;
    Model.CuadranteId = 0;
    Sectores.Clear();
    Cuadrantes.Clear();
    // Cargar sectores de la nueva zona
    if (value > 0)
        await LoadSectoresAsync(value);
    return Task.CompletedTask;
}

private Task OnSectorChanged(int value)
{
    Model.SectorId = value;
    // Reset dependientes
    Model.CuadranteId = 0;
    Cuadrantes.Clear();
    // Cargar cuadrantes del nuevo sector
    if (value > 0)
        await LoadCuadrantesAsync(value);
    return Task.CompletedTask;
}
```

## Problemas Conocidos

- ⚠️ Si los datos no aparecen, verificar que el seed se ejecutó correctamente (logs en consola)
- ⚠️ Si la cascada no funciona, verificar que los endpoints de API están devolviendo datos correctos

## Relacionado

- `docs/TEST-USERS.md` - Usuarios de prueba
- `specs/001-incident-management-system/spec.md` - Especificación completa
- `src/Ceiba.Infrastructure/Data/SeedDataService.cs` - Código de seed
