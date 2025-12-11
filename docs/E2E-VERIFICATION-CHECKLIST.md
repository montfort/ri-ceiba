# Checklist de Verificación E2E Manual

**Proyecto**: Ceiba - Sistema de Reportes de Incidencias
**Fecha**: 2025-12-10
**Versión**: 1.0

Este documento contiene todas las verificaciones manuales necesarias para comprobar el funcionamiento completo del sistema antes de un despliegue.

---

## Pre-requisitos

- [ ] Base de datos PostgreSQL ejecutándose
- [ ] Aplicación iniciada sin errores en consola
- [ ] Usuarios de prueba creados (CREADOR, REVISOR, ADMIN)
- [ ] Catálogos básicos configurados (Zonas, Sectores, Cuadrantes)

### Credenciales de Prueba Sugeridas

| Rol | Email | Contraseña |
|-----|-------|------------|
| ADMIN | admin@ceiba.local | Admin12345 |
| REVISOR | revisor@ceiba.local | Revisor12345 |
| CREADOR | creador@ceiba.local | Creador12345 |

---

## 1. Autenticación y Sesiones

### 1.1 Login
- [ ] Navegar a `/login` muestra formulario de inicio de sesión
- [ ] Login con credenciales correctas redirige a Home
- [ ] Login con credenciales incorrectas muestra mensaje de error
- [ ] Login con usuario suspendido muestra mensaje "cuenta suspendida"
- [ ] Campo de contraseña oculta los caracteres

### 1.2 Logout
- [ ] Botón "Cerrar Sesión" visible en navbar cuando autenticado
- [ ] Click en "Cerrar Sesión" redirige a `/login`
- [ ] Después de logout, acceder a rutas protegidas redirige a login

### 1.3 Sesión
- [ ] Sesión expira después de 30 minutos de inactividad
- [ ] Usuario ve mensaje apropiado cuando sesión expira
- [ ] Refresco de página mantiene la sesión activa

### 1.4 Políticas de Contraseña
- [ ] Contraseña menor a 10 caracteres es rechazada
- [ ] Contraseña sin mayúscula es rechazada
- [ ] Contraseña sin número es rechazada

---

## 2. User Story 1: Creación de Reportes (CREADOR)

### 2.1 Navegación
- [ ] CREADOR ve opción "Nuevo Reporte" en menú
- [ ] CREADOR ve opción "Mis Reportes" en menú
- [ ] CREADOR NO ve opciones de Administración
- [ ] CREADOR NO ve opciones de Reportes Automatizados

### 2.2 Crear Reporte Tipo A
- [ ] Click en "Nuevo Reporte" abre formulario vacío
- [ ] Formulario muestra todos los campos requeridos:
  - [ ] Sexo (con sugerencias)
  - [ ] Edad (numérico)
  - [ ] LGBT+ (checkbox)
  - [ ] Situación de calle (checkbox)
  - [ ] Migrante (checkbox)
  - [ ] Discapacidad (checkbox)
  - [ ] Delito (con sugerencias)
  - [ ] Tipo de Atención (con sugerencias)
  - [ ] Tipo de Acción (select)
  - [ ] Zona (dropdown)
  - [ ] Sector (dropdown, filtrado por zona)
  - [ ] Cuadrante (dropdown, filtrado por sector)
  - [ ] Turno Ceiba (select)
  - [ ] Hechos Reportados (textarea)
  - [ ] Acciones Realizadas (textarea)
  - [ ] Traslados (select)
  - [ ] Observaciones (textarea, opcional)

### 2.3 Validaciones de Formulario
- [ ] Guardar sin campos requeridos muestra errores de validación
- [ ] Edad fuera de rango (0-150) muestra error
- [ ] Campos de texto con caracteres especiales peligrosos son sanitizados
- [ ] Seleccionar Zona actualiza opciones de Sector
- [ ] Seleccionar Sector actualiza opciones de Cuadrante

### 2.4 Guardar como Borrador
- [ ] Click en "Guardar" con datos válidos guarda el reporte
- [ ] Reporte aparece en "Mis Reportes" con estado "Borrador"
- [ ] Reporte guardado puede ser editado
- [ ] Cambios en reporte borrador se persisten correctamente

### 2.5 Entregar Reporte
- [ ] Botón "Entregar" visible en reporte borrador
- [ ] Click en "Entregar" cambia estado a "Entregado"
- [ ] Reporte entregado NO es editable por CREADOR
- [ ] Reporte entregado muestra vista de solo lectura
- [ ] Confirmación antes de entregar (opcional pero recomendado)

### 2.6 Historial de Reportes
- [ ] "Mis Reportes" muestra solo reportes del usuario actual
- [ ] Lista ordenada por fecha (más reciente primero)
- [ ] Filtro por estado (Borrador/Entregado) funciona
- [ ] Búsqueda por texto funciona
- [ ] Paginación funciona con muchos reportes

---

## 3. User Story 2: Revisión y Exportación (REVISOR)

### 3.1 Navegación
- [ ] REVISOR ve opción "Todos los Reportes" en menú
- [ ] REVISOR ve opción "Exportar" en menú
- [ ] REVISOR ve opción "Reportes Automatizados" en menú
- [ ] REVISOR NO ve opciones de Administración
- [ ] REVISOR NO ve opción de "Nuevo Reporte"

### 3.2 Listado de Reportes
- [ ] REVISOR ve TODOS los reportes (propios y de otros)
- [ ] Lista muestra: Folio, Fecha, Agente, Estado, Zona, Delito
- [ ] Ordenamiento por columnas funciona
- [ ] Filtros disponibles:
  - [ ] Por fecha (desde/hasta)
  - [ ] Por zona
  - [ ] Por delito
  - [ ] Por agente
  - [ ] Por estado

### 3.3 Edición de Reportes
- [ ] REVISOR puede editar reportes en estado "Entregado"
- [ ] Cambios en campos se guardan correctamente
- [ ] Historial de cambios visible (si implementado)
- [ ] Campos editados se registran en auditoría

### 3.4 Exportación PDF
- [ ] Seleccionar un reporte y exportar genera PDF
- [ ] PDF contiene todos los datos del reporte
- [ ] PDF tiene formato profesional con logo/header
- [ ] Exportar múltiples reportes genera PDF consolidado
- [ ] Límite de 50 reportes por exportación PDF mostrado

### 3.5 Exportación JSON
- [ ] Seleccionar reportes y exportar a JSON funciona
- [ ] JSON tiene estructura válida
- [ ] JSON incluye todos los campos del reporte
- [ ] Límite de 100 reportes por exportación JSON mostrado

### 3.6 Exportación en Background
- [ ] Exportar más de 50 reportes ofrece exportación en background
- [ ] Usuario recibe notificación de que recibirá email
- [ ] Email llega con archivo adjunto (requiere SMTP configurado)

---

## 4. User Story 3: Administración (ADMIN)

### 4.1 Navegación
- [ ] ADMIN ve menú "Administración" con submenús
- [ ] ADMIN NO ve opciones de Reportes de Incidencias
- [ ] ADMIN NO ve opciones de Reportes Automatizados

### 4.2 Gestión de Usuarios (`/admin/users`)
- [ ] Lista de usuarios muestra: Email, Nombre, Roles, Estado, Fecha creación
- [ ] Botón "Nuevo Usuario" abre formulario
- [ ] Crear usuario con todos los campos funciona:
  - [ ] Email (validación de formato)
  - [ ] Nombre completo
  - [ ] Contraseña (validación de políticas)
  - [ ] Roles (múltiple selección)
- [ ] Editar usuario existente funciona
- [ ] Suspender usuario cambia estado a "Suspendido"
- [ ] Usuario suspendido no puede iniciar sesión
- [ ] Reactivar usuario suspendido funciona
- [ ] Eliminar usuario (soft delete) funciona
- [ ] No se puede crear usuario con email duplicado

### 4.3 Gestión de Catálogos (`/admin/catalogs`)

#### Zonas
- [ ] Lista de zonas muestra nombre y estado
- [ ] Crear nueva zona funciona
- [ ] Editar zona existente funciona
- [ ] Desactivar zona la oculta en formularios
- [ ] Zona desactivada no elimina reportes existentes

#### Sectores
- [ ] Lista de sectores muestra nombre, zona asociada y estado
- [ ] Crear sector requiere seleccionar zona
- [ ] Sector aparece solo cuando se selecciona su zona en formularios
- [ ] Editar sector funciona
- [ ] Desactivar sector funciona

#### Cuadrantes
- [ ] Lista de cuadrantes muestra nombre, sector asociado y estado
- [ ] Crear cuadrante requiere seleccionar sector
- [ ] Cuadrante aparece solo cuando se selecciona su sector
- [ ] Editar cuadrante funciona
- [ ] Desactivar cuadrante funciona

### 4.4 Listas de Sugerencias (`/admin/suggestions`)
- [ ] Ver listas existentes (Sexo, Delito, TipoDeAtencion)
- [ ] Agregar nueva sugerencia a lista
- [ ] Nueva sugerencia aparece en formularios de reporte
- [ ] Editar sugerencia existente
- [ ] Desactivar sugerencia la oculta pero no afecta datos existentes
- [ ] Orden de sugerencias configurable (si implementado)

### 4.5 Registros de Auditoría (`/admin/audit`)
- [ ] Lista de eventos muestra: Fecha, Usuario, Acción, Detalles, IP
- [ ] Filtro por fecha funciona
- [ ] Filtro por usuario funciona
- [ ] Filtro por tipo de acción funciona
- [ ] Búsqueda en detalles funciona
- [ ] Paginación funciona
- [ ] Exportar auditoría a CSV/Excel (si implementado)

### 4.6 Configuración de Email (`/admin/email-config`)
- [ ] Ver configuración actual de proveedor
- [ ] Cambiar proveedor (SMTP/SendGrid/Mailgun)
- [ ] Configurar credenciales del proveedor
- [ ] Botón "Probar Conexión" envía email de prueba
- [ ] Guardar configuración persiste cambios

### 4.7 Configuración de IA (`/admin/ai-config`)
- [ ] Ver proveedor de IA actual
- [ ] Configurar API Key
- [ ] Configurar modelo (si aplica)
- [ ] Botón "Probar Conexión" verifica conectividad
- [ ] Guardar configuración persiste cambios

---

## 5. User Story 4: Reportes Automatizados (REVISOR)

### 5.1 Navegación
- [ ] REVISOR ve "Reportes Automatizados" en menú
- [ ] Submenús: Lista de Reportes, Plantillas

### 5.2 Lista de Reportes Automatizados (`/automated/reports`)
- [ ] Lista muestra reportes generados con fecha
- [ ] Ordenamiento por fecha funciona
- [ ] Click en reporte abre detalle
- [ ] Botón para descargar PDF/Word

### 5.3 Detalle de Reporte Automatizado
- [ ] Ver contenido markdown renderizado
- [ ] Ver estadísticas del período
- [ ] Ver estado de envío por email
- [ ] Botón para reenviar email (si falló)
- [ ] Descargar en formato Word

### 5.4 Gestión de Plantillas (`/automated/templates`)
- [ ] Lista de plantillas disponibles
- [ ] Ver plantilla por defecto marcada
- [ ] Crear nueva plantilla con editor markdown
- [ ] Editar plantilla existente
- [ ] Variables disponibles documentadas:
  - [ ] `{{fecha_inicio}}`
  - [ ] `{{fecha_fin}}`
  - [ ] `{{total_reportes}}`
  - [ ] `{{estadisticas}}`
  - [ ] `{{narrativa_ia}}`
- [ ] Establecer plantilla como predeterminada
- [ ] Desactivar plantilla

### 5.5 Generación Manual
- [ ] Botón "Generar Reporte Ahora" disponible
- [ ] Seleccionar rango de fechas
- [ ] Seleccionar plantilla (o usar default)
- [ ] Proceso muestra progreso
- [ ] Reporte generado aparece en lista

### 5.6 Configuración de Programación (ADMIN: `/admin/automated-config`)
- [ ] Ver hora de generación actual
- [ ] Cambiar hora de generación
- [ ] Ver destinatarios de email
- [ ] Agregar/quitar destinatarios
- [ ] Activar/desactivar generación automática

---

## 6. Seguridad y Control de Acceso

### 6.1 Aislamiento de Roles
- [ ] CREADOR no puede acceder a `/admin/*`
- [ ] CREADOR no puede acceder a `/automated/*`
- [ ] REVISOR no puede acceder a `/admin/*`
- [ ] REVISOR no puede crear reportes
- [ ] ADMIN no puede acceder a reportes de incidencias
- [ ] URLs protegidas redirigen a 403 o login

### 6.2 Protección CSRF
- [ ] Formularios incluyen token anti-CSRF
- [ ] Requests sin token válido son rechazados

### 6.3 Validación de Entrada
- [ ] Scripts en campos de texto son escapados
- [ ] SQL injection en búsquedas no funciona
- [ ] Caracteres especiales en campos son manejados

### 6.4 Auditoría
- [ ] Login exitoso registrado
- [ ] Login fallido registrado
- [ ] Crear reporte registrado
- [ ] Editar reporte registrado
- [ ] Entregar reporte registrado
- [ ] Crear usuario registrado
- [ ] Suspender usuario registrado
- [ ] Cambios en catálogos registrados

---

## 7. Rendimiento y UX

### 7.1 Tiempos de Respuesta
- [ ] Páginas cargan en menos de 3 segundos
- [ ] Listados con paginación cargan rápido
- [ ] Exportación de 10 reportes < 5 segundos
- [ ] Generación de reporte automatizado < 30 segundos

### 7.2 Responsividad
- [ ] Aplicación usable en móvil (320px ancho)
- [ ] Aplicación usable en tablet (768px ancho)
- [ ] Menú colapsable en pantallas pequeñas
- [ ] Formularios adaptados a móvil

### 7.3 Mensajes de Usuario
- [ ] Operaciones exitosas muestran confirmación
- [ ] Errores muestran mensaje descriptivo
- [ ] Validaciones muestran campos con error
- [ ] Estados de carga visibles (spinners)

### 7.4 Navegación
- [ ] Breadcrumbs presentes (si aplica)
- [ ] Botón "Volver" funciona correctamente
- [ ] Navegación con teclado posible
- [ ] Focus visible en elementos interactivos

---

## 8. Casos Borde

### 8.1 Datos Límite
- [ ] Reporte con todos los campos al máximo de caracteres
- [ ] Lista con 1000+ reportes funciona
- [ ] Búsqueda con caracteres unicode funciona
- [ ] Fechas en límites del sistema

### 8.2 Concurrencia
- [ ] Dos usuarios editando mismo reporte (REVISOR)
- [ ] Usuario suspendido mientras tiene sesión activa
- [ ] Zona desactivada mientras usuario la tiene seleccionada

### 8.3 Recuperación de Errores
- [ ] Pérdida de conexión durante guardado
- [ ] Timeout en generación de reporte automatizado
- [ ] Fallo en servicio de email
- [ ] Fallo en servicio de IA

---

## 9. Integraciones Externas

### 9.1 Email (SMTP/SendGrid/Mailgun)
- [ ] Configuración de proveedor funciona
- [ ] Email de prueba se envía correctamente
- [ ] Reportes automatizados se envían
- [ ] Archivos adjuntos incluidos

### 9.2 IA (OpenAI/Azure/Local)
- [ ] Configuración de proveedor funciona
- [ ] Generación de narrativa funciona
- [ ] Fallback sin IA muestra solo estadísticas
- [ ] Timeout de 30s se respeta

### 9.3 Pandoc (Conversión a Word)
- [ ] Pandoc instalado y detectado
- [ ] Conversión markdown → Word funciona
- [ ] Documentos Word se descargan correctamente
- [ ] Error si Pandoc no disponible es manejado

---

## 10. Base de Datos

### 10.1 Integridad
- [ ] Reportes mantienen referencia a zona aunque se desactive
- [ ] Usuarios eliminados mantienen referencia en auditoría
- [ ] Cascadas configuradas correctamente

### 10.2 Migraciones
- [ ] Migraciones se ejecutan sin errores
- [ ] Rollback de migración funciona
- [ ] Datos de seed se insertan correctamente

---

## Notas de Verificación

| Fecha | Verificador | Ambiente | Resultado | Observaciones |
|-------|-------------|----------|-----------|---------------|
| | | | | |
| | | | | |
| | | | | |

---

## Firmas

**Verificado por**: _________________________
**Fecha**: _________________________
**Aprobado por**: _________________________
**Fecha**: _________________________
