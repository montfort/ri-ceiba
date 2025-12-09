# Script de Generación de Reportes Dummy

## Descripción

Este script genera 20 reportes de prueba para el día **08 de diciembre de 2025**, diseñados para probar el sistema de reportes automatizados con IA.

## Archivo

- **`generate-dummy-reports.sql`** - Script SQL para generar 20 reportes variados

## Datos Generados

Los reportes incluyen una variedad representativa de casos:

1. **Violencia familiar** (Zona Norte, 08:00) - Mujer 32 años, con traslado
2. **Acoso sexual** (Zona Sur, 10:00) - Mujer 24 años, laboral
3. **Violación** (Zona Centro, 02:00) - Mujer 19 años, con traslado, alta prioridad
4. **Lesiones** (Zona Norte, 14:00) - Hombre LGBTTTIQ+ 28 años, delito de odio
5. **Amenazas** (Zona Sur, 16:00) - Mujer 45 años, ex pareja
6. **Violencia familiar** (Zona Norte, 11:00) - Mujer migrante 38 años
7. **Hostigamiento sexual** (Zona Centro, 09:00) - Mujer 27 años, universitaria
8. **Violencia en el noviazgo** (Zona Sur, 15:00) - Mujer 21 años, prevención
9. **Abuso sexual infantil** (Zona Norte, 13:00) - Niña 12 años, con traslado
10. **Discriminación** (Zona Centro, 12:00) - Persona no binaria 29 años
11. **Violencia digital** (Zona Sur, 17:00) - Mujer 23 años, Ley Olimpia
12. **Violencia obstétrica** (Zona Norte, 10:30) - Mujer 26 años
13. **Trata de personas** (Zona Centro, 03:00) - Menor migrante 17 años, con traslado
14. **Violencia patrimonial** (Zona Sur, 14:30) - Adulta mayor con discapacidad 52 años
15. **Acoso callejero** (Zona Norte, 18:00) - Mujer 19 años, transporte público
16. **Violencia económica** (Zona Centro, 11:30) - Mujer 41 años
17. **Tentativa de feminicidio** (Zona Sur, 01:00) - Mujer situación de calle 34 años, emergencia
18. **Violencia laboral** (Zona Norte, 16:30) - Mujer embarazada 36 años
19. **Violencia política de género** (Zona Centro, 13:30) - Regidora 47 años
20. **Violencia psicológica** (Zona Sur, 19:00) - Mujer 30 años, capacitación

## Características de los Datos

- **Variedad geográfica**: Distribuidos entre Zona Norte, Zona Sur y Zona Centro
- **Diversidad demográfica**:
  - Edades: 12 a 52 años
  - Géneros: Femenino, Masculino, No binario
  - Poblaciones vulnerables: LGBTTTIQ+, migrantes, situación de calle, discapacidad
- **Tipos de delitos**: 15 tipos diferentes de violencia de género
- **Tipos de acción**:
  - Tipo 1 (ATOS): 17 reportes
  - Tipo 2 (Capacitación): 1 reporte
  - Tipo 3 (Prevención): 2 reportes
- **Con traslado**: 5 reportes (25%)
- **Casos graves**: 4 reportes con observaciones de alta prioridad

## Uso

### 1. Ajustar el UUID del Usuario

Antes de ejecutar, verificar el UUID del usuario CREADOR en su base de datos:

```sql
SELECT "Id", "UserName" FROM "USUARIO" WHERE "UserName" LIKE 'creador%';
```

Actualizar la línea 7 del script con el UUID correcto:

```sql
usuario_creador UUID := '019ac648-b096-7afc-b2cb-6610f7b6711f'; -- Reemplazar con su UUID
```

### 2. Ejecutar el Script

```bash
# Con credenciales en línea de comandos
PGPASSWORD=ceiba123 psql -h localhost -U ceiba -d ceiba -f scripts/generate-dummy-reports.sql

# O de forma interactiva
psql -h localhost -U ceiba -d ceiba -f scripts/generate-dummy-reports.sql
```

### 3. Verificar los Resultados

```sql
-- Contar reportes del 08/12/2025
SELECT COUNT(*) as total
FROM "REPORTE_INCIDENCIA"
WHERE created_at AT TIME ZONE 'UTC' >= '2025-12-08 00:00:00'
  AND created_at AT TIME ZONE 'UTC' < '2025-12-09 00:00:00';

-- Listar reportes generados
SELECT
    id,
    delito,
    sexo,
    edad,
    TO_CHAR(created_at AT TIME ZONE 'UTC', 'HH24:MI') as hora
FROM "REPORTE_INCIDENCIA"
WHERE created_at AT TIME ZONE 'UTC' >= '2025-12-08 00:00:00'
  AND created_at AT TIME ZONE 'UTC' < '2025-12-09 00:00:00'
ORDER BY created_at;
```

## Probar Reportes Automatizados

Después de generar los datos, puede probar la generación de reportes automatizados:

1. **Configurar IA** (si no está configurada):
   - Navegar a `/admin/ai-config`
   - Configurar proveedor (OpenAI, Gemini, DeepSeek, etc.)
   - Guardar configuración

2. **Generar Reporte Manual**:
   - Como usuario REVISOR, ir a sección de reportes automatizados
   - Seleccionar fechas: **08/12/2025** al **08/12/2025**
   - Click en "Generar Reporte"
   - El sistema generará narrativa de los 20 casos

3. **Probar Límite de Reportes**:
   - En configuración avanzada de IA, ajustar "Max Reportes para Narrativa"
   - Valores sugeridos:
     - `0` = Incluir todos (20 casos)
     - `10` = Limitar a 10 casos más recientes
     - `5` = Limitar a 5 casos más recientes

## Limpieza

Para eliminar los reportes de prueba:

```sql
-- Eliminar reportes del 08/12/2025
DELETE FROM "REPORTE_INCIDENCIA"
WHERE created_at AT TIME ZONE 'UTC' >= '2025-12-08 00:00:00'
  AND created_at AT TIME ZONE 'UTC' < '2025-12-09 00:00:00';
```

⚠️ **PRECAUCIÓN**: Esta operación es irreversible. Verificar la fecha antes de ejecutar.

## Notas Importantes

- Todos los reportes están en estado **Entregado** (estado = 1)
- Las fechas/horas están en **UTC**
- El script usa zonas, sectores y cuadrantes existentes
- Cada reporte tiene narrativas realistas para casos de violencia de género
- Los datos son ficticios y solo para propósitos de prueba

## Soporte

Si encuentra algún error:

1. Verificar que las zonas/sectores/cuadrantes existen en la BD
2. Verificar el UUID del usuario CREADOR
3. Revisar permisos en la base de datos
4. Verificar que las tablas tengan la estructura correcta
