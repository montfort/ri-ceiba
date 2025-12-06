# Pruebas de Funcionalidad de Cascada - Ubicación Geográfica

## Resumen del Fix

Se corrigieron los métodos `OnZonaChanged` y `OnSectorChanged` en `ReportForm.razor` para que:
1. Carguen los datos de los selectores dependientes
2. Reseteen los campos dependientes
3. Fuercen la actualización de la UI con `StateHasChanged()`

## URLs de la Aplicación

- **HTTPS**: https://localhost:7160
- **HTTP**: http://localhost:5028

## Credenciales de Prueba

| Usuario | Contraseña | Rol |
|---------|------------|-----|
| creador@test.com | Creador123! | CREADOR |
| revisor@test.com | Revisor123! | REVISOR |
| admin@test.com | Admin123!Test | ADMIN |

## Pasos para Probar la Cascada

### 1. Iniciar Sesión

1. Abre el navegador y ve a: `https://localhost:7160`
2. Inicia sesión con: `creador@test.com` / `Creador123!`
3. Deberías ver el dashboard del usuario CREADOR

### 2. Acceder al Formulario

1. Haz clic en "Nuevo Reporte" o navega a: `https://localhost:7160/reports/new`
2. Espera a que el formulario cargue completamente

### 3. Probar Cascada Zona → Sector

**Estado Inicial:**
- ✅ Selector de Zona: Habilitado, con 5 opciones
- ❌ Selector de Sector: Deshabilitado, vacío
- ❌ Selector de Cuadrante: Deshabilitado, vacío

**Acción: Seleccionar una Zona**

1. Abre el selector "Zona"
2. Selecciona "Zona Norte"
3. **Resultado Esperado:**
   - ✅ Selector de Sector se HABILITA
   - ✅ Aparecen 3 opciones:
     - Sector Centro
     - Sector Este
     - Sector Oeste
   - ✅ Selector de Cuadrante permanece deshabilitado

**Verificar en Logs (Consola del servidor):**
```
[INF] Loading sectores for zona 1
[INF] Loaded 3 sectores for zona 1
```

### 4. Probar Cascada Sector → Cuadrante

**Acción: Seleccionar un Sector**

1. Abre el selector "Sector" (ahora habilitado)
2. Selecciona "Sector Centro"
3. **Resultado Esperado:**
   - ✅ Selector de Cuadrante se HABILITA
   - ✅ Aparecen 3 cuadrantes:
     - Cuadrante A
     - Cuadrante B
     - Cuadrante C

**Verificar en Logs:**
```
[INF] Loading cuadrantes for sector X
[INF] Loaded 3 cuadrantes for sector X
```

### 5. Probar Reset en Cascada

**Acción: Cambiar la Zona después de haber seleccionado todo**

1. Selecciona: Zona Norte → Sector Centro → Cuadrante A
2. Cambia la Zona a "Zona Sur"
3. **Resultado Esperado:**
   - ✅ Selector de Sector se VACÍA y se HABILITA
   - ✅ Selector de Cuadrante se VACÍA y se DESHABILITA
   - ✅ Aparecen los sectores de "Zona Sur" (4 sectores):
     - Sector Centro
     - Sector Este
     - Sector Oeste
     - Sector Residencial

### 6. Probar Validación

**Acción: Intentar guardar sin completar la cascada**

1. Selecciona solo Zona Norte (sin sector ni cuadrante)
2. Intenta guardar el formulario
3. **Resultado Esperado:**
   - ❌ Mensaje de validación: "El campo Sector es requerido"

## Datos de Prueba Disponibles

### Zonas (5 total)
- Zona Norte (3 sectores)
- Zona Sur (4 sectores)
- Zona Centro (3 sectores)
- Zona Oriente (4 sectores)
- Zona Poniente (3 sectores)

### Sectores por Zona

**Zona Norte (3 sectores, 12 cuadrantes):**
- Sector Centro: 3 cuadrantes (A, B, C)
- Sector Este: 4 cuadrantes (A, B, C, D)
- Sector Oeste: 5 cuadrantes (A, B, C, D, E)

**Zona Sur (4 sectores, ~15 cuadrantes):**
- Sector Centro: 3 cuadrantes
- Sector Este: 4 cuadrantes
- Sector Oeste: 5 cuadrantes
- Sector Residencial: 3 cuadrantes

## Monitorear Logs en Tiempo Real

### PowerShell (Windows)

```powershell
# Ver logs generales
Get-Content E:\Proyectos\SSC\software\ReportesIncidencias\ri-ceiba\src\Ceiba.Web\bin\Debug\net10.0\logs\ceiba-*.log -Wait -Tail 20

# Filtrar solo mensajes de carga de catálogos
Get-Content /tmp/ceiba-run.log -Wait | Select-String -Pattern "Loading.*sector|Loading.*cuadrante|Loaded.*sector|Loaded.*cuadrante"
```

### Bash (Linux/Git Bash)

```bash
# Ver logs de la aplicación en ejecución
tail -f /tmp/ceiba-run.log | grep -E "Loading|Loaded|sector|cuadrante"
```

## Verificar en Base de Datos

```sql
-- Verificar datos de Zona Norte
SELECT z.nombre AS zona, s.nombre AS sector, c.nombre AS cuadrante
FROM "CUADRANTE" c
JOIN "SECTOR" s ON c.sector_id = s.id
JOIN "ZONA" z ON s.zona_id = z.id
WHERE z.nombre = 'Zona Norte'
ORDER BY s.nombre, c.nombre;
```

## Debugging en el Navegador

### Chrome DevTools (F12)

1. **Console**: Verificar errores de JavaScript
2. **Network**: Verificar llamadas a la API (si se usan)
3. **Elements**: Verificar que los `<option>` se generan correctamente

### Verificar Elementos HTML

**Estado Correcto del Selector de Sector (después de seleccionar Zona):**

```html
<select id="sector" class="form-select" disabled="false">
  <option value="0">-- Seleccione --</option>
  <option value="1">Sector Centro</option>
  <option value="2">Sector Este</option>
  <option value="3">Sector Oeste</option>
</select>
```

## Problemas Conocidos y Soluciones

### Problema: Selector de Sector no se habilita

**Posibles Causas:**
1. `Model.ZonaId` no se está actualizando
2. `CatalogService` no está devolviendo datos
3. Error de JavaScript en el navegador

**Solución:**
1. Verificar logs del servidor para mensajes "Loading sectores..."
2. Verificar consola del navegador (F12) para errores
3. Verificar datos en base de datos

### Problema: Selector se habilita pero no tiene opciones

**Posibles Causas:**
1. `GetSectoresByZonaAsync` devuelve lista vacía
2. No hay datos en la base de datos para esa zona
3. `StateHasChanged()` no se está llamando

**Solución:**
1. Verificar logs: "Loaded 0 sectores for zona X"
2. Ejecutar query SQL para verificar datos
3. Verificar que `StateHasChanged()` está en el código

### Problema: Cascada funciona pero al cambiar la zona, los sectores anteriores permanecen

**Causa:** No se está limpiando la lista `Sectores.Clear()`

**Solución:** Ya está corregido en el código actual

## Siguiente Paso: Pruebas Automáticas

Una vez verificado manualmente, considera agregar pruebas con bUnit:

```csharp
[Fact]
public async Task OnZonaChanged_ShouldLoadSectores()
{
    // Arrange
    var mockCatalogService = new Mock<ICatalogService>();
    mockCatalogService
        .Setup(x => x.GetSectoresByZonaAsync(1))
        .ReturnsAsync(new List<CatalogItemDto>
        {
            new() { Id = 1, Nombre = "Sector Centro" }
        });

    var component = RenderComponent<ReportForm>(parameters =>
        parameters.Add(p => p.CatalogService, mockCatalogService.Object));

    // Act
    await component.Instance.OnZonaChanged(1);

    // Assert
    mockCatalogService.Verify(x => x.GetSectoresByZonaAsync(1), Times.Once);
    Assert.Single(component.Instance.Sectores);
}
```

## Contacto

Si encuentras algún problema, verifica:
1. Logs del servidor
2. Consola del navegador
3. Datos en base de datos
4. Documentación en `docs/GEOGRAPHIC-TEST-DATA.md`
