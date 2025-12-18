# Catálogos de Sugerencias

Las sugerencias son listas de valores predefinidos que aparecen como autocompletado en los formularios de reportes.

## ¿Qué son las Sugerencias?

Cuando un usuario escribe en ciertos campos del formulario de reporte, el sistema muestra opciones sugeridas. Esto:

- Acelera la captura de datos
- Estandariza la información
- Reduce errores de escritura

> **Nota:** Las sugerencias **no son obligatorias**. Los usuarios pueden escribir valores personalizados.

## Acceder a Sugerencias

1. Desde el panel de administración, haz clic en **Sugerencias**
2. O navega a `/admin/suggestions`

![Gestor de sugerencias](images/admin-sugerencias-lista.png)

## Categorías de Sugerencias

El sistema tiene las siguientes categorías:

| Categoría | Campo del Formulario | Ejemplos |
|-----------|---------------------|----------|
| `sexo` | Sexo de la víctima | Masculino, Femenino, No especificado |
| `delito` | Tipo de delito | Robo, Asalto, Lesiones, Vandalismo |
| `tipo_de_atencion` | Tipo de atención | Flagrancia, Denuncia, Patrullaje |
| `turno_ceiba` | Turno de trabajo | Matutino, Vespertino, Nocturno |
| `traslados` | Estado de traslados | Con traslado, Sin traslado, Pendiente |

## Ver Sugerencias por Categoría

1. Selecciona la categoría en el menú desplegable
2. La lista mostrará todas las sugerencias activas
3. Cada sugerencia muestra su valor y estado

## Agregar una Sugerencia

1. Selecciona la categoría correspondiente
2. Haz clic en **Nueva Sugerencia**
3. Ingresa el valor de la sugerencia
4. Opcionalmente, define el orden de aparición
5. Haz clic en **Guardar**

### Campos del Formulario

| Campo | Descripción | Requerido |
|-------|-------------|-----------|
| Categoría | A qué campo pertenece | Si |
| Valor | Texto que se mostrará | Si |
| Orden | Posición en la lista | No |
| Activo | Si se muestra o no | Si (default: Si) |

## Editar una Sugerencia

1. Busca la sugerencia en la lista
2. Haz clic en **Editar**
3. Modifica el valor u orden
4. Guarda los cambios

## Desactivar una Sugerencia

En lugar de eliminar, puedes desactivar sugerencias:

1. Edita la sugerencia
2. Desmarca la casilla **Activo**
3. Guarda

La sugerencia no aparecerá en los formularios pero se mantiene en el sistema.

## Eliminar una Sugerencia

1. Busca la sugerencia
2. Haz clic en **Eliminar**
3. Confirma la acción

> **Nota:** Eliminar una sugerencia no afecta los reportes existentes que usaron ese valor.

## Ordenar Sugerencias

El orden determina cómo aparecen en el autocompletado:

- Menor número = aparece primero
- Los valores más comunes deben tener números más bajos
- Sugerencias sin orden aparecen al final (ordenadas alfabéticamente)

### Ejemplo de Orden

| Valor | Orden | Resultado |
|-------|-------|-----------|
| Masculino | 1 | Primero |
| Femenino | 2 | Segundo |
| No especificado | 3 | Tercero |
| Otro | 10 | Último |

## Mejores Prácticas

### Mantener Listas Concisas

- 5-15 sugerencias por categoría es ideal
- Demasiadas opciones dificultan la selección

### Usar Términos Estandarizados

- Define términos oficiales para delitos
- Mantén consistencia en mayúsculas/minúsculas
- Evita abreviaciones ambiguas

### Revisar Periódicamente

- Añade nuevos valores según necesidades
- Desactiva valores obsoletos
- Ajusta el orden según frecuencia de uso

### Documentar Cambios

- Registra por qué se añadió cada sugerencia
- Mantén un historial de cambios importantes

## Impacto en el Sistema

### Para Usuarios

- Los cambios son inmediatos
- Nuevas sugerencias aparecen al crear/editar reportes
- Sugerencias desactivadas dejan de mostrarse

### Para Datos Existentes

- Los reportes existentes **no se modifican**
- Si un valor se elimina, los reportes lo mantienen
- El análisis de datos debe considerar valores históricos

## Próximos Pasos

- [[Usuario Admin Catalogos Geograficos|Catálogos geográficos]]
- [[Usuario Admin Auditoria|Ver auditoría]]
- [[Usuario Admin FAQ|Preguntas frecuentes]]
