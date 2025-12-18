# Preguntas Frecuentes - Revisor

Respuestas a las preguntas más comunes para usuarios con rol REVISOR.

## Permisos y Acceso

### ¿Puedo ver los reportes de todos los usuarios?

**Sí**, como Revisor tienes acceso a todos los reportes del sistema, independientemente de quién los creó.

### ¿Puedo editar reportes entregados?

**Sí**, a diferencia de los Creadores, los Revisores pueden editar cualquier reporte, incluso los que ya fueron entregados.

### ¿Mis cambios quedan registrados?

**Sí**, todas las modificaciones se registran en el sistema de auditoría, incluyendo quién hizo el cambio y cuándo.

### ¿Puedo crear reportes nuevos?

Si también tienes el rol **CREADOR**, sí. Si solo tienes rol REVISOR, tu función es supervisar y exportar, no crear reportes nuevos.

## Exportación

### ¿Cuántos reportes puedo exportar a la vez?

El límite es de **500 reportes por exportación**. Para conjuntos mayores, realiza múltiples exportaciones con diferentes rangos de fecha.

### ¿En qué formatos puedo exportar?

Dos formatos disponibles:
- **PDF**: Para impresión y archivo
- **JSON**: Para integración con otros sistemas y análisis de datos

### ¿El PDF incluye todos los campos del reporte?

**Sí**, el PDF incluye toda la información: datos de la víctima, ubicación, incidencia, detalles operativos y narrativa completa.

### ¿Puedo exportar solo reportes de un creador específico?

**Sí**, usa el filtro de "Creador" antes de buscar y seleccionar reportes para exportar.

### ¿Por qué tarda tanto la exportación?

El tiempo depende de:
- Cantidad de reportes seleccionados
- Complejidad del contenido
- Carga del servidor

Para exportaciones grandes (100+ reportes), espera hasta 2 minutos.

## Reportes Automatizados

### ¿Cómo funciona la generación automática?

1. El sistema ejecuta las plantillas activas según su horario
2. Recopila los reportes del período
3. Envía los datos a la IA para generar el resumen
4. Crea el PDF y lo envía por email

### ¿Puedo crear mis propias plantillas?

**Sí**, puedes crear y configurar plantillas con prompts personalizados para diferentes tipos de resúmenes.

### ¿Qué pasa si no hay reportes en el período?

El sistema notifica que no hay datos para generar. No se crea un reporte vacío.

### ¿Puedo regenerar un reporte automatizado?

Actualmente debes esperar al próximo ciclo o contactar al administrador para una ejecución manual.

### ¿Los reportes automatizados reemplazan la exportación manual?

No, son complementarios. Los reportes automatizados son resúmenes narrativos; la exportación manual te da los datos completos.

## Filtros y Búsqueda

### ¿Cómo busco reportes de una fecha específica?

Usa los filtros **Desde** y **Hasta** con la misma fecha para ver solo reportes de ese día.

### ¿El filtro de delito es exacto?

No, busca coincidencias parciales. "robo" encontrará "robo a transeúnte", "robo de vehículo", etc.

### ¿Puedo guardar mis filtros favoritos?

Actualmente no hay función de guardado de filtros. Debes configurarlos cada vez.

### ¿Por qué no aparecen algunos reportes?

Verifica:
- El rango de fechas es correcto
- El estado seleccionado coincide
- No hay filtros de texto que excluyan resultados

## Problemas Técnicos

### La exportación falla

1. Reduce el número de reportes seleccionados
2. Intenta con un rango de fechas más pequeño
3. Espera unos minutos y reintenta
4. Contacta al administrador si persiste

### No puedo ver reportes automatizados

Verifica:
- Tienes rol REVISOR
- La configuración de IA está activa
- Hay plantillas configuradas y activas

### El PDF generado está vacío

- Verifica que los reportes seleccionados tengan contenido
- Intenta con menos reportes
- Reporta el problema al administrador

### Los filtros no funcionan

1. Limpia todos los filtros con "Limpiar Filtros"
2. Aplica un solo filtro a la vez
3. Actualiza la página (F5)

## Integraciones

### ¿Puedo importar el JSON en Excel?

**Sí**, Excel puede importar archivos JSON directamente:
1. Datos → Obtener datos → Desde archivo → JSON
2. Selecciona el archivo exportado
3. Excel creará columnas para cada campo

### ¿El JSON es compatible con Power BI?

**Sí**, Power BI puede conectarse a archivos JSON y crear visualizaciones con los datos.

### ¿Hay una API para exportar automáticamente?

Consulta con el administrador sobre las opciones de integración disponibles.

## Coordinación con Otros Roles

### ¿Cómo informo a un Creador sobre correcciones?

Actualmente el sistema no tiene notificaciones. Debes comunicarte directamente con el usuario.

### ¿Puedo asignar reportes para revisión?

Actualmente no hay flujo de asignación. Todos los reportes están disponibles para cualquier Revisor.

### ¿Los Administradores pueden ver los reportes?

No, los Administradores solo gestionan usuarios y catálogos. No tienen acceso al módulo de reportes.

---

¿No encontraste tu pregunta? Contacta al administrador del sistema.
