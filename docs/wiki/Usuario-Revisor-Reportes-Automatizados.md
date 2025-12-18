# Reportes Automatizados con IA

El sistema Ceiba puede generar reportes narrativos automáticamente utilizando inteligencia artificial.

## ¿Qué son los Reportes Automatizados?

Son resúmenes narrativos generados automáticamente que:
- Consolidan los reportes de un período
- Generan texto profesional usando IA
- Se envían por email a destinatarios configurados
- Se almacenan para consulta posterior

## Acceder a Reportes Automatizados

1. Desde el panel principal, haz clic en **Reportes Auto**
2. O navega a `/automated`

![Lista de reportes automatizados](images/revisor-reportes-auto-lista.png)

## Lista de Reportes Generados

La lista muestra:

| Columna | Descripción |
|---------|-------------|
| Fecha | Cuándo se generó el reporte |
| Plantilla | Qué plantilla se usó |
| Estado | Generado, Enviado, Error |
| Reportes Incluidos | Cuántos reportes se consolidaron |
| Acciones | Ver, Descargar PDF |

## Ver un Reporte Automatizado

1. Haz clic en el reporte de la lista
2. Se abrirá el detalle con:
   - Resumen narrativo generado
   - Lista de reportes incluidos
   - Metadatos de generación

## Descargar PDF

1. Haz clic en el botón **Descargar PDF**
2. El documento incluye:
   - Encabezado institucional
   - Resumen narrativo generado por IA
   - Estadísticas del período
   - Lista de incidencias incluidas

## Gestionar Plantillas

Las plantillas definen cómo se generan los reportes automáticos.

### Acceder a Plantillas

1. Desde el panel principal, haz clic en **Plantillas**
2. O navega a `/automated/templates`

![Configuración de plantillas](images/revisor-reportes-auto-config.png)

### Configurar una Plantilla

| Campo | Descripción |
|-------|-------------|
| Nombre | Identificador de la plantilla |
| Descripción | Para qué sirve esta plantilla |
| Prompt de IA | Instrucciones para la generación |
| Horario | Cuándo se ejecuta |
| Destinatarios | Emails para recibir el reporte |
| Activo | Si la plantilla está habilitada |

### Prompt de IA

El prompt es el texto que instruye a la IA sobre cómo generar el resumen. Por ejemplo:

```
Genera un resumen ejecutivo de los incidentes del día.
Incluye:
- Total de incidentes por tipo
- Zonas con mayor actividad
- Patrones identificados
- Recomendaciones de seguimiento
```

## Programación de Reportes

Los reportes automatizados se generan según el horario configurado:

| Frecuencia | Descripción |
|------------|-------------|
| Diario | Cada día a la hora especificada |
| Semanal | Un día específico de la semana |
| Mensual | Un día específico del mes |

## Estados del Reporte

| Estado | Descripción |
|--------|-------------|
| **Generando** | En proceso de creación |
| **Generado** | Listo pero no enviado |
| **Enviado** | Entregado a destinatarios |
| **Error** | Falló la generación o envío |

## Configuración de Email

Los reportes generados pueden enviarse automáticamente por email:

1. Configura los destinatarios en la plantilla
2. El sistema envía el PDF adjunto
3. Se registra el envío exitoso o fallido

## Requisitos del Sistema

Para que los reportes automatizados funcionen:

1. **IA configurada**: El administrador debe configurar el proveedor de IA
2. **Email configurado**: SMTP habilitado para envíos
3. **Plantilla activa**: Al menos una plantilla habilitada

## Solución de Problemas

### "No se generó el reporte"

- Verifica que la plantilla esté activa
- Revisa que haya reportes en el período
- Consulta con el administrador sobre la configuración de IA

### "El reporte no se envió"

- Verifica los destinatarios configurados
- Revisa la configuración de email
- El reporte puede descargarse manualmente

## Próximos Pasos

- [Ver reportes manuales](Usuario-Revisor-Ver-Reportes)
- [Preguntas frecuentes](Usuario-Revisor-FAQ)
- [Configuración del sistema (Admin)](Usuario-Admin-Configuracion)
