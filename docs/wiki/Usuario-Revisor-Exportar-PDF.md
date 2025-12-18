# Exportar Reportes a PDF

Como Revisor, puedes generar documentos PDF de los reportes para impresión, archivo o distribución.

## Acceder a la Exportación

1. Desde el panel principal, haz clic en **Exportar**
2. O navega directamente a `/supervisor/export`

![Página de exportación](images/revisor-exportar-menu.png)

## Proceso de Exportación

### Paso 1: Seleccionar Formato

En el panel izquierdo, asegúrate de que **PDF** esté seleccionado:
- Haz clic en el botón **PDF** (ícono de archivo PDF)
- El botón se resaltará en rojo cuando esté activo

### Paso 2: Buscar Reportes

Usa los filtros para encontrar los reportes que deseas exportar:

| Filtro | Descripción |
|--------|-------------|
| Estado | Borrador o Entregado |
| Delito | Texto a buscar |
| Fecha Desde | Inicio del rango |
| Fecha Hasta | Fin del rango |

Haz clic en **Buscar Reportes** para obtener resultados.

### Paso 3: Seleccionar Reportes

En la lista de resultados:
1. Marca los reportes individuales que deseas exportar
2. O usa **Seleccionar todos** para incluir todos los resultados
3. El contador mostrará cuántos reportes tienes seleccionados

### Paso 4: Exportar

1. Haz clic en **Exportar PDF**
2. Espera mientras se genera el documento
3. El archivo se descargará automáticamente

![Vista previa PDF](images/revisor-exportar-pdf-preview.png)

## Contenido del PDF

El PDF generado incluye:

### Por cada reporte:
- Encabezado con ID y fechas
- Datos de la víctima/persona atendida
- Ubicación geográfica completa
- Tipo de incidencia
- Detalles operativos
- Narrativa completa

### Formato:
- Diseño profesional para impresión
- Incluye logos institucionales (si están configurados)
- Numeración de páginas
- Fecha de generación

## Nombre del Archivo

El archivo descargado sigue el formato:
```
reportes_YYYYMMDD_HHMMSS.pdf
```

Por ejemplo: `reportes_20251218_143022.pdf`

## Exportación Rápida

Para casos comunes, usa los accesos directos:

| Opción | Descripción |
|--------|-------------|
| **Reportes de Hoy** | Todos los reportes creados hoy |
| **Últimos 7 Días** | Reportes de la última semana |
| **Este Mes** | Reportes del mes actual |

Haz clic en el botón **PDF** de cada opción para exportar directamente.

## Límites y Recomendaciones

- **Tamaño máximo**: Hasta 500 reportes por exportación
- **Tiempo de generación**: Varía según la cantidad de reportes
- **Almacenamiento**: El archivo se guarda en tu carpeta de descargas

## Problemas Comunes

### "No se encontraron reportes"

- Verifica los filtros aplicados
- Amplía el rango de fechas
- Limpia los filtros y busca nuevamente

### "Error al generar PDF"

- Reduce el número de reportes seleccionados
- Intenta nuevamente después de unos momentos
- Contacta al administrador si persiste

### El PDF no se descarga

- Verifica que tu navegador permita descargas
- Revisa la carpeta de descargas
- Desactiva bloqueadores de pop-ups

## Próximos Pasos

- [[Usuario Revisor Exportar JSON|Exportar a JSON]]
- [[Usuario Revisor Exportacion Masiva|Exportación masiva]]
- [[Usuario Revisor Ver Reportes|Ver reportes]]
