# Exportar Reportes a JSON

El formato JSON permite integrar los datos de reportes con otros sistemas o realizar análisis programáticos.

## ¿Qué es JSON?

JSON (JavaScript Object Notation) es un formato de datos estructurado que:
- Es legible por humanos y máquinas
- Se usa para intercambio de datos entre sistemas
- Puede procesarse con herramientas de programación
- Es ideal para análisis de datos y estadísticas

## Acceder a la Exportación

1. Ve a **Exportar** desde el panel principal
2. Selecciona el formato **JSON** en el panel izquierdo

## Proceso de Exportación

### Paso 1: Seleccionar Formato JSON

1. En el panel de opciones, haz clic en **JSON**
2. El botón se resaltará en azul cuando esté activo

### Paso 2: Buscar y Seleccionar Reportes

El proceso es idéntico a la exportación PDF:
1. Aplica filtros según necesites
2. Haz clic en **Buscar Reportes**
3. Selecciona los reportes deseados

### Paso 3: Exportar

1. Haz clic en **Exportar JSON**
2. El archivo se descargará automáticamente

## Estructura del JSON

El archivo JSON contiene un array de objetos con la siguiente estructura:

```json
{
  "exportDate": "2025-12-18T14:30:00Z",
  "reportCount": 5,
  "reports": [
    {
      "id": 123,
      "tipoReporte": "A",
      "createdAt": "2025-12-18T10:00:00Z",
      "datetimeHechos": "2025-12-17T15:30:00Z",
      "estado": 1,
      "creador": {
        "id": "guid-usuario",
        "email": "usuario@ejemplo.com"
      },
      "victima": {
        "sexo": "Masculino",
        "edad": 35,
        "lgbtttiqPlus": false,
        "situacionCalle": false,
        "migrante": false,
        "discapacidad": false
      },
      "ubicacion": {
        "zona": {"id": 1, "nombre": "Norte"},
        "region": {"id": 2, "nombre": "Central"},
        "sector": {"id": 3, "nombre": "A"},
        "cuadrante": {"id": 4, "nombre": "A1"}
      },
      "incidencia": {
        "delito": "Robo a transeúnte",
        "tipoDeAtencion": "Flagrancia"
      },
      "operativo": {
        "turnoCeiba": "Matutino",
        "tipoDeAccion": "Detención",
        "traslados": "Con traslado"
      },
      "narrativa": {
        "hechosReportados": "...",
        "accionesRealizadas": "...",
        "observaciones": "..."
      }
    }
  ]
}
```

## Campos del JSON

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `id` | number | ID único del reporte |
| `tipoReporte` | string | Tipo de reporte (ej: "A") |
| `createdAt` | datetime | Fecha de creación (UTC) |
| `datetimeHechos` | datetime | Fecha del incidente (UTC) |
| `estado` | number | 0=Borrador, 1=Entregado |
| `creador` | object | Datos del creador |
| `victima` | object | Datos de la persona atendida |
| `ubicacion` | object | Ubicación geográfica |
| `incidencia` | object | Tipo de delito y atención |
| `operativo` | object | Datos operativos |
| `narrativa` | object | Textos narrativos |

## Nombre del Archivo

El archivo descargado sigue el formato:
```
reportes_YYYYMMDD_HHMMSS.json
```

## Casos de Uso

### Análisis Estadístico

Importa el JSON en herramientas como:
- Excel / Google Sheets
- Python (pandas)
- R
- Power BI / Tableau

### Integración con Otros Sistemas

Usa el JSON para:
- Sincronizar con sistemas externos
- Generar reportes personalizados
- Alimentar dashboards

### Respaldo de Datos

Mantén copias locales de los reportes en formato estructurado.

## Exportación Rápida

Las opciones de exportación rápida también están disponibles para JSON:

| Opción | Descripción |
|--------|-------------|
| Reportes de Hoy | JSON con reportes del día |
| Últimos 7 Días | JSON de la última semana |
| Este Mes | JSON del mes actual |

## Próximos Pasos

- [[Usuario Revisor Exportar PDF|Exportar a PDF]]
- [[Usuario Revisor Exportacion Masiva|Exportación masiva]]
- [[Usuario Revisor Ver Reportes|Ver reportes]]
