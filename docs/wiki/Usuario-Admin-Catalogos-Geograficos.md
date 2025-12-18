# Catálogos Geográficos

Los catálogos geográficos definen la estructura de ubicaciones disponibles en los reportes de incidencias.

## Jerarquía Geográfica

El sistema usa una estructura jerárquica de 4 niveles:

```
Zona
└── Región
    └── Sector
        └── Cuadrante
```

Cada nivel depende del anterior. Por ejemplo, los sectores pertenecen a una región específica.

## Acceder a Catálogos

1. Desde el panel de administración, haz clic en **Catálogos**
2. O navega a `/admin/catalogs`

![Gestor de catálogos](images/admin-catalogos-zonas.png)

## Gestionar Zonas

Las zonas son el nivel más alto de la jerarquía.

### Ver Zonas

1. En el gestor de catálogos, selecciona la pestaña **Zonas**
2. Verás la lista de todas las zonas registradas

### Crear Zona

1. Haz clic en **Nueva Zona**
2. Ingresa el nombre de la zona
3. Opcionalmente, añade una descripción
4. Haz clic en **Guardar**

### Editar Zona

1. Busca la zona en la lista
2. Haz clic en **Editar**
3. Modifica el nombre o descripción
4. Guarda los cambios

### Eliminar Zona

> **Advertencia:** Solo puedes eliminar zonas que no tengan regiones asociadas.

1. Busca la zona sin dependencias
2. Haz clic en **Eliminar**
3. Confirma la acción

## Gestionar Regiones

Las regiones pertenecen a una zona específica.

![Lista de regiones](images/admin-catalogos-sectores.png)

### Crear Región

1. Selecciona la pestaña **Regiones**
2. Haz clic en **Nueva Región**
3. Selecciona la **Zona padre**
4. Ingresa el nombre de la región
5. Guarda

### Vincular a Zona

Cada región debe estar asociada a exactamente una zona.

## Gestionar Sectores

Los sectores pertenecen a una región específica.

### Crear Sector

1. Selecciona la pestaña **Sectores**
2. Haz clic en **Nuevo Sector**
3. Selecciona la **Zona** (para filtrar regiones)
4. Selecciona la **Región padre**
5. Ingresa el nombre del sector
6. Guarda

## Gestionar Cuadrantes

Los cuadrantes son el nivel más bajo, pertenecen a un sector.

![Lista de cuadrantes](images/admin-catalogos-cuadrantes.png)

### Crear Cuadrante

1. Selecciona la pestaña **Cuadrantes**
2. Haz clic en **Nuevo Cuadrante**
3. Navega la jerarquía: Zona → Región → Sector
4. Ingresa el nombre del cuadrante
5. Guarda

## Dependencias y Restricciones

### No Puedes Eliminar con Dependencias

| Elemento | Restricción |
|----------|-------------|
| Zona | No se puede eliminar si tiene regiones |
| Región | No se puede eliminar si tiene sectores |
| Sector | No se puede eliminar si tiene cuadrantes |
| Cuadrante | No se puede eliminar si hay reportes asociados |

### Cambiar el Padre

Actualmente no es posible mover un elemento a otro padre. Debes:
1. Crear el elemento en la nueva ubicación
2. (Opcional) Eliminar el original si no tiene dependencias

## Impacto en Reportes

### Reportes Existentes

Los catálogos **no se pueden eliminar** si hay reportes que los usan. Esto protege la integridad de los datos.

### Nuevos Reportes

Los cambios en catálogos afectan inmediatamente:
- Nuevas opciones aparecen en los formularios
- Las opciones eliminadas desaparecen (si no tienen dependencias)

## Importación Masiva

Para cargar muchos elementos, contacta al equipo de desarrollo para una importación por base de datos.

## Nomenclatura Recomendada

| Nivel | Ejemplo | Formato Sugerido |
|-------|---------|------------------|
| Zona | Norte, Sur, Centro | Nombre descriptivo |
| Región | Central, Poniente | Nombre descriptivo |
| Sector | A, B, C o Nombre | Letra o nombre corto |
| Cuadrante | A1, A2, B1 | Combinación sector+número |

## Próximos Pasos

- [[Usuario-Admin-Catalogos-Sugerencias|Configurar sugerencias]]
- [[Usuario-Admin-Gestion-Usuarios|Gestionar usuarios]]
- [[Usuario-Admin-FAQ|Preguntas frecuentes]]
