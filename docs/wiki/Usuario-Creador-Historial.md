# Historial de Reportes

Esta guía te enseña cómo consultar y filtrar tu historial de reportes.

## Acceder al Historial

1. Desde el panel principal, haz clic en **Ver Mis Reportes**
2. Verás la lista de todos los reportes que has creado

![Lista de reportes](images/creador-lista-reportes.png)

## Vista de la Lista

### En Escritorio

La lista se muestra como una tabla con las siguientes columnas:

| Columna | Descripción |
|---------|-------------|
| ID | Número único del reporte |
| Fecha Creación | Cuándo se creó el reporte |
| Fecha Hechos | Cuándo ocurrió el incidente |
| Estado | Borrador o Entregado |
| Delito | Tipo de delito registrado |
| Zona | Zona geográfica |
| Acciones | Botones de Ver y Editar |

### En Móvil

Cada reporte se muestra como una tarjeta individual con:
- Número de reporte y estado
- Delito y zona
- Fechas
- Botones de acción

## Filtrar Reportes

Usa el panel de filtros para encontrar reportes específicos:

![Panel de filtros](images/creador-filtros-historial.png)

### Filtros Disponibles

| Filtro | Opciones |
|--------|----------|
| **Estado** | Todos, Borrador, Entregado |
| **Delito** | Búsqueda por texto |
| **Desde** | Fecha inicial |
| **Hasta** | Fecha final |

### Cómo Usar los Filtros

1. Selecciona o escribe el valor del filtro
2. Los resultados se actualizan automáticamente
3. Para limpiar filtros, haz clic en **Limpiar Filtros**

### Ejemplos de Búsqueda

**Ver solo borradores:**
- Estado: Borrador

**Reportes de robo del último mes:**
- Delito: "robo"
- Desde: (primer día del mes)
- Hasta: (hoy)

**Reportes entregados de una zona específica:**
- Estado: Entregado
- (Ver en la tabla la columna Zona)

## Paginación

Si tienes muchos reportes:

1. Se muestran 20 reportes por página
2. Usa los botones **Anterior** y **Siguiente**
3. Puedes saltar a páginas específicas
4. El total de reportes se muestra debajo

## Ver Detalle de un Reporte

Para ver los detalles completos de un reporte:

1. Haz clic en el botón **Ver** (ícono de ojo)
2. Se abrirá la vista detallada del reporte
3. Verás toda la información en modo lectura

![Detalle de reporte](images/creador-reporte-detalle.png)

## Acciones Disponibles

### Botón Ver (ojo)
- Siempre disponible
- Muestra el reporte en modo lectura

### Botón Editar (lápiz)
- Solo disponible para reportes en **Borrador**
- Abre el formulario de edición

## Lista Vacía

Si no tienes reportes:
- Verás un mensaje indicando que no hay reportes
- Aparecerá un botón para **Crear Primer Reporte**

## Consejos

1. **Usa filtros de fecha** para encontrar reportes de períodos específicos
2. **El filtro de delito** busca coincidencias parciales (ej: "robo" encuentra "robo a transeúnte")
3. **Ordena por fecha** para ver los más recientes primero

## Próximos Pasos

- [[Usuario Creador Crear Reporte|Crear un nuevo reporte]]
- [[Usuario Creador FAQ|Preguntas frecuentes]]
