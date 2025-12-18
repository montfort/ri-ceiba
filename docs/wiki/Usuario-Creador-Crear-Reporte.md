# Crear un Nuevo Reporte

Esta guía te enseña cómo crear un reporte de incidencia Tipo A paso a paso.

## Acceder al Formulario

1. Desde el panel principal, haz clic en **Nuevo Reporte**
2. O navega a **Mis Reportes** y haz clic en el botón **Nuevo Reporte**

![Formulario de nuevo reporte](images/creador-reporte-nuevo-vacio.png)

## Completar el Formulario

### Sección 1: Datos de los Hechos

| Campo | Descripción | Requerido |
|-------|-------------|-----------|
| Fecha y Hora de los Hechos | Cuándo ocurrió el incidente | Si |

### Sección 2: Datos de la Víctima/Persona Atendida

| Campo | Descripción | Requerido |
|-------|-------------|-----------|
| Sexo | Sexo de la persona atendida | Si |
| Edad | Edad en años | Si |
| LGBTTTIQ+ | Marcar si aplica | No |
| Situación de Calle | Marcar si aplica | No |
| Migrante | Marcar si aplica | No |
| Discapacidad | Marcar si aplica | No |

> **Nota:** Los campos de Sexo usan autocompletado con sugerencias predefinidas, pero puedes escribir valores personalizados.

### Sección 3: Ubicación Geográfica

La ubicación se selecciona de forma jerárquica:

1. **Zona**: Selecciona primero la zona
2. **Región**: Se habilita después de seleccionar zona
3. **Sector**: Se habilita después de seleccionar región
4. **Cuadrante**: Se habilita después de seleccionar sector

> **Importante:** Debes seleccionar los campos en orden. Cada selección filtra las opciones del siguiente nivel.

### Sección 4: Tipo de Incidencia

| Campo | Descripción | Requerido |
|-------|-------------|-----------|
| Delito | Tipo de delito (con sugerencias) | Si |
| Tipo de Atención | Cómo se atendió el caso | Si |

### Sección 5: Detalles Operativos

| Campo | Descripción | Requerido |
|-------|-------------|-----------|
| Turno CEIBA | Tu turno de trabajo | Si |
| Tipo de Acción | Descripción de la acción tomada (máx. 500 caracteres) | Si |
| Traslados | Estado de traslados | Si |

### Sección 6: Narrativa del Incidente

| Campo | Descripción | Requerido |
|-------|-------------|-----------|
| Hechos Reportados | Descripción detallada de lo ocurrido (10-10,000 caracteres) | Si |
| Acciones Realizadas | Qué acciones tomaste (10-10,000 caracteres) | Si |
| Observaciones | Notas adicionales (máx. 5,000 caracteres) | No |

![Formulario con datos](images/creador-reporte-nuevo-llenado.png)

## Guardar el Reporte

### Guardar como Borrador

Haz clic en **Guardar Borrador** para:
- Guardar tu progreso
- Poder continuar editando después
- El reporte quedará en estado **Borrador**

![Confirmación de guardado](images/creador-reporte-guardado.png)

### Guardar y Entregar

Si el reporte está completo y deseas entregarlo inmediatamente:
1. Completa todos los campos requeridos
2. Haz clic en **Guardar y Entregar**
3. El reporte cambiará a estado **Entregado**

> **Advertencia:** Una vez entregado, no podrás editar el reporte. Solo un Revisor puede modificarlo.

## Cancelar

Si deseas cancelar sin guardar:
1. Haz clic en **Cancelar**
2. Serás redirigido a la lista de reportes
3. Los datos no guardados se perderán

## Errores Comunes

### "Campo requerido"
Asegúrate de completar todos los campos marcados con asterisco (*).

### "La ubicación no está completa"
Debes seleccionar Zona, Región, Sector Y Cuadrante.

### "El texto es muy corto"
Los campos de narrativa requieren al menos 10 caracteres.

## Próximos Pasos

- [Cómo editar un reporte guardado](Usuario-Creador-Editar-Reporte)
- [Cómo entregar un reporte](Usuario-Creador-Enviar-Reporte)
- [Ver tu historial de reportes](Usuario-Creador-Historial)
