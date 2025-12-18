# Ver Todos los Reportes

Como Revisor, puedes ver todos los reportes del sistema, independientemente de quién los creó.

## Acceder a la Lista de Reportes

1. Desde el panel principal, haz clic en **Todos los Reportes**
2. O navega directamente a `/supervisor/reports`

![Lista de reportes del revisor](images/revisor-lista-todos-reportes.png)

## Información Disponible

La lista muestra:

| Columna | Descripción |
|---------|-------------|
| ID | Identificador único del reporte |
| Creado Por | Usuario que creó el reporte |
| Fecha Creación | Cuándo se creó |
| Fecha Hechos | Cuándo ocurrió el incidente |
| Estado | Borrador o Entregado |
| Delito | Tipo de delito |
| Zona | Ubicación geográfica |
| Acciones | Botones de Ver, Editar |

## Filtros Disponibles

Usa los filtros para encontrar reportes específicos:

![Filtros avanzados](images/revisor-filtros-avanzados.png)

| Filtro | Descripción |
|--------|-------------|
| **Estado** | Borrador, Entregado, o Todos |
| **Creador** | Filtrar por usuario creador |
| **Delito** | Búsqueda por texto en tipo de delito |
| **Zona** | Filtrar por zona geográfica |
| **Desde** | Fecha inicial del rango |
| **Hasta** | Fecha final del rango |

### Aplicar Filtros

1. Selecciona los valores deseados
2. Los resultados se actualizan automáticamente
3. Haz clic en **Limpiar Filtros** para reiniciar

## Ver Detalle de un Reporte

Para ver los detalles completos:

1. Haz clic en el botón **Ver** (ícono de ojo)
2. Se abrirá la vista detallada con toda la información

![Detalle del reporte](images/revisor-reporte-detalle.png)

### Información en el Detalle

- **Datos del reporte**: ID, fechas, estado
- **Creador**: Quién lo creó
- **Datos de la víctima**: Sexo, edad, indicadores
- **Ubicación**: Zona, región, sector, cuadrante
- **Incidencia**: Delito, tipo de atención
- **Operativos**: Turno, acciones, traslados
- **Narrativa**: Hechos, acciones, observaciones

## Paginación

- Se muestran 20 reportes por página
- Usa **Anterior** y **Siguiente** para navegar
- El contador muestra "Mostrando X de Y reportes"

## Ordenamiento

Los reportes se ordenan por fecha de creación (más recientes primero).

## Acciones Disponibles

| Acción | Ícono | Descripción |
|--------|-------|-------------|
| Ver | Ojo | Abre el detalle del reporte |
| Editar | Lápiz | Abre el formulario de edición |

> **Nota:** Como Revisor, puedes editar **cualquier reporte**, incluso los entregados.

## Consejos

1. **Usa filtros de fecha** para períodos específicos
2. **Filtra por estado** para ver solo borradores pendientes
3. **Busca por delito** para análisis temáticos
4. **Exporta** los resultados filtrados para reportes

## Próximos Pasos

- [[Usuario-Revisor-Editar-Reportes|Editar reportes]]
- [[Usuario-Revisor-Exportar-PDF|Exportar a PDF]]
- [[Usuario-Revisor-Exportacion-Masiva|Exportación masiva]]
