# Guía del Creador - Introducción

Como **Creador** (oficial de policía), tu función principal es registrar los reportes de incidencias que atiendes en el campo.

## Tus Permisos

| Acción | Permitido |
|--------|-----------|
| Crear nuevos reportes | Si |
| Ver tus propios reportes | Si |
| Editar reportes en Borrador | Si |
| Enviar reportes (Borrador → Entregado) | Si |
| Editar reportes Entregados | No |
| Ver reportes de otros usuarios | No |
| Exportar PDF/JSON | No |

## Tu Panel de Trabajo

Al iniciar sesión, verás el **Panel de Reportes** con dos opciones principales:

1. **Ver Mis Reportes**: Accede a la lista de todos tus reportes
2. **Nuevo Reporte**: Crea un nuevo reporte de incidencia

![Panel del Creador](images/creador-dashboard.png)

## Flujo de Trabajo

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   Crear     │────▶│   Editar    │────▶│   Enviar    │
│   Reporte   │     │  (Borrador) │     │  (Entregar) │
└─────────────┘     └─────────────┘     └─────────────┘
                           │
                           ▼
                    ┌─────────────┐
                    │   Reporte   │
                    │  Entregado  │
                    └─────────────┘
```

### Estados del Reporte

| Estado | Color | Descripción |
|--------|-------|-------------|
| **Borrador** | Amarillo | Reporte en edición, aún no entregado |
| **Entregado** | Verde | Reporte enviado para revisión |

## Información que Registrarás

Cada reporte de incidencia (Tipo A) incluye:

### Datos de los Hechos
- Fecha y hora del incidente

### Datos de la Víctima/Persona Atendida
- Sexo y edad
- Indicadores: LGBTTTIQ+, situación de calle, migrante, discapacidad

### Ubicación Geográfica
- Zona → Región → Sector → Cuadrante

### Tipo de Incidencia
- Delito
- Tipo de atención

### Detalles Operativos
- Turno CEIBA
- Tipo de acción
- Traslados

### Narrativa
- Hechos reportados
- Acciones realizadas
- Observaciones

## Próximos Pasos

- [Aprender a crear un reporte](Usuario-Creador-Crear-Reporte)
- [Aprender a editar un reporte](Usuario-Creador-Editar-Reporte)
- [Aprender a enviar un reporte](Usuario-Creador-Enviar-Reporte)
- [Ver tu historial de reportes](Usuario-Creador-Historial)
