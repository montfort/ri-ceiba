# Guía del Revisor - Introducción

Como **Revisor** (supervisor), tu función principal es supervisar y gestionar todos los reportes de incidencias del sistema.

## Tus Permisos

| Acción | Permitido |
|--------|-----------|
| Ver todos los reportes | Si |
| Editar cualquier reporte | Si |
| Exportar a PDF | Si |
| Exportar a JSON | Si |
| Exportación masiva | Si |
| Ver reportes automatizados | Si |
| Configurar plantillas de reportes | Si |
| Gestionar usuarios | No |
| Gestionar catálogos | No |

## Tu Panel de Trabajo

Al iniciar sesión, verás el **Panel de Supervisión** con acceso a:

1. **Todos los Reportes**: Ver y gestionar reportes de cualquier usuario
2. **Exportar**: Generar PDF y JSON de reportes seleccionados
3. **Reportes Auto**: Ver reportes generados automáticamente con IA
4. **Plantillas**: Configurar plantillas para reportes automatizados

![Panel del Revisor](images/revisor-dashboard.png)

## Accesos Directos

| Módulo | Descripción | Ícono |
|--------|-------------|-------|
| Todos los Reportes | Lista completa de reportes | Portapapeles |
| Exportar | Herramienta de exportación | Descarga |
| Reportes Auto | Reportes generados por IA | Robot |
| Plantillas | Configuración de plantillas | Código |

## Diferencias con el Creador

| Característica | Creador | Revisor |
|----------------|---------|---------|
| Ver reportes | Solo propios | Todos |
| Editar reportes | Solo borradores propios | Cualquier reporte |
| Exportar | No | Si |
| Reportes automatizados | No | Si |

## Flujo de Trabajo Típico

```
┌──────────────────┐
│  Ver Reportes    │
│   Entregados     │
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│  Revisar/Editar  │
│   si necesario   │
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│    Exportar      │
│   PDF o JSON     │
└──────────────────┘
```

## Reportes Automatizados

Como Revisor, tienes acceso a:

1. **Reportes diarios generados por IA**: Resúmenes narrativos de los reportes del día
2. **Configuración de plantillas**: Define cómo se generan los reportes
3. **Descarga de reportes**: PDF con los resúmenes generados

## Próximos Pasos

- [[Usuario-Revisor-Ver-Reportes|Ver todos los reportes]]
- [[Usuario-Revisor-Editar-Reportes|Editar reportes]]
- [[Usuario-Revisor-Exportar-PDF|Exportar a PDF]]
- [[Usuario-Revisor-Exportacion-Masiva|Exportación masiva]]
- [[Usuario-Revisor-Reportes-Automatizados|Reportes automatizados]]
