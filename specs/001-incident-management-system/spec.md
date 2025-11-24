# Feature Specification: Sistema de Gestión de Reportes de Incidencias

**Feature Branch**: `001-incident-management-system`
**Created**: 2025-11-18
**Status**: Draft
**Input**: Proyecto "Ceiba - Reportes de Incidencias" - Sistema completo para la Unidad Especializada en Género de la SSC CDMX

## Clarifications

### Session 2025-11-18

- Q: What password complexity requirements should be enforced? → A: Moderate - minimum 10 characters, require uppercase + number
- Q: How long should user sessions remain active before requiring re-authentication? → A: 30 minutes of inactivity
- Q: How long should audit logs be retained? → A: Indefinite retention (never delete)

### Session 2025-11-21

- Q: What AI integration strategy should be used for narrative generation? → A: Provider-agnostic abstraction layer supporting multiple backends
- Q: What email service integration approach should be used? → A: SMTP with configurable provider via environment settings

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Creación y Entrega de Reportes por Agentes (Priority: P1)

Un agente de policía (CREADOR) necesita crear reportes de incidencias relacionadas con casos de género. El agente inicia sesión, selecciona el tipo de reporte (actualmente solo Tipo A), llena el formulario con datos del solicitante, hechos reportados, ubicación geográfica (zona/sector/cuadrante) y acciones realizadas. El reporte permanece en estado "borrador" mientras el agente lo edita. Una vez satisfecho, el agente entrega el reporte, cambiando su estado a "entregado" y bloqueando ediciones posteriores por parte del agente.

**Why this priority**: Esta es la funcionalidad central del sistema. Sin la capacidad de crear reportes, el resto del sistema no tiene propósito. Representa el flujo de trabajo principal de los usuarios más numerosos (agentes de policía).

**Independent Test**: Puede probarse completamente creando un usuario CREADOR, iniciando sesión, creando un reporte tipo A con todos los campos, guardándolo como borrador, editándolo, y finalmente entregándolo. Entrega valor inmediato al permitir digitalizar el proceso de reportes.

**Acceptance Scenarios**:

1. **Given** un usuario CREADOR autenticado en el sistema, **When** selecciona "Nuevo Reporte" y elige "Tipo A", **Then** se muestra el formulario completo con todos los campos requeridos y opcionales del Tipo A.

2. **Given** un CREADOR con un formulario Tipo A abierto, **When** llena los campos requeridos (sexo, edad, delito, zona, sector, cuadrante, turnoCeiba, tipoDeAtencion, tipoDeAccion, hechosReportados, accionesRealizadas, traslados) y guarda, **Then** el reporte se almacena en estado "borrador" y aparece en su historial.

3. **Given** un CREADOR con un reporte en estado "borrador", **When** modifica cualquier campo y guarda, **Then** los cambios se persisten y el reporte permanece editable.

4. **Given** un CREADOR con un reporte completo en estado "borrador", **When** selecciona "Entregar", **Then** el reporte cambia a estado "entregado", se registra la acción en auditoría, y el reporte ya no es editable por el CREADOR.

5. **Given** un CREADOR autenticado, **When** accede a su historial de reportes, **Then** ve solo sus propios reportes con opciones de búsqueda y filtrado.

6. **Given** un usuario CREADOR suspendido, **When** intenta iniciar sesión, **Then** el sistema rechaza el acceso mostrando mensaje de cuenta suspendida.

---

### User Story 2 - Revisión, Edición y Exportación por Supervisores (Priority: P2)

Un supervisor (REVISOR) necesita revisar los reportes entregados por los agentes, complementarlos con información adicional (como acciones correctivas), y exportarlos para informes oficiales. El supervisor inicia sesión, ve el listado de todos los reportes entregados, puede buscar y filtrar por múltiples criterios, selecciona un reporte para visualizarlo o editarlo, y puede exportar reportes individuales o conjuntos a PDF o JSON.

**Why this priority**: Los supervisores son usuarios críticos que aseguran la calidad de los datos y generan los entregables oficiales. Sin esta funcionalidad, los reportes creados no tendrían seguimiento ni utilidad institucional.

**Independent Test**: Puede probarse creando un usuario REVISOR, varios reportes en estado "entregado", y verificando que el REVISOR puede listar, buscar, editar y exportar estos reportes a PDF y JSON.

**Acceptance Scenarios**:

1. **Given** un usuario REVISOR autenticado, **When** accede al listado de reportes, **Then** ve todos los reportes del sistema (propios y de otros usuarios) con posibilidad de ordenamiento por fecha, zona, tipo de delito, y estado.

2. **Given** un REVISOR en el listado de reportes, **When** aplica filtros de búsqueda (por fecha, zona, delito, agente), **Then** el listado se actualiza mostrando solo los reportes que coinciden con los criterios.

3. **Given** un REVISOR visualizando un reporte entregado, **When** edita campos como observaciones o accionesRealizadas y guarda, **Then** los cambios se persisten y se registra la edición en auditoría.

4. **Given** un REVISOR con uno o más reportes seleccionados, **When** selecciona "Exportar a PDF", **Then** se genera un documento PDF con el contenido de los reportes seleccionados.

5. **Given** un REVISOR con reportes seleccionados, **When** selecciona "Exportar a JSON", **Then** se descarga un archivo JSON estructurado con los datos de los reportes.

6. **Given** un usuario REVISOR, **When** intenta crear un nuevo reporte, **Then** el sistema no muestra la opción de creación de reportes (solo disponible para CREADOR).

---

### User Story 3 - Gestión de Usuarios y Auditoría por Administrador (Priority: P3)

Un administrador técnico (ADMIN) necesita gestionar los usuarios del sistema, configurar los catálogos de campos (zonas, sectores, cuadrantes, listas de sugerencias), y monitorear la actividad del sistema mediante registros de auditoría. El administrador puede crear, suspender y eliminar usuarios, asignar roles, configurar las listas desplegables, y consultar el historial completo de acciones en el sistema.

**Why this priority**: La administración es esencial para el funcionamiento del sistema pero puede iniciar con configuraciones por defecto. Los usuarios ADMIN son pocos y su funcionalidad soporta, no ejecuta, el proceso principal.

**Independent Test**: Puede probarse creando un usuario ADMIN, ejecutando operaciones CRUD sobre usuarios, modificando catálogos de zona/sector/cuadrante, y verificando que todas las acciones quedan registradas en auditoría.

**Acceptance Scenarios**:

1. **Given** un usuario ADMIN autenticado, **When** accede a la gestión de usuarios, **Then** ve el listado completo de usuarios con sus roles, estado (activo/suspendido), y fecha de creación.

2. **Given** un ADMIN en gestión de usuarios, **When** crea un nuevo usuario con nombre, email, contraseña y rol(es) asignados, **Then** el usuario se crea en el sistema y puede iniciar sesión con las credenciales proporcionadas.

3. **Given** un ADMIN con un usuario existente seleccionado, **When** suspende al usuario, **Then** el usuario no puede iniciar sesión y se registra la suspensión en auditoría.

4. **Given** un ADMIN en configuración de catálogos, **When** agrega una nueva zona con nombre y activa el registro, **Then** la zona aparece disponible en los formularios de reportes para todos los usuarios CREADOR.

5. **Given** un ADMIN configurando sector/cuadrante, **When** crea un sector asociado a una zona específica, **Then** el sector solo aparece cuando se selecciona esa zona en el formulario de reportes.

6. **Given** un ADMIN en registros de auditoría, **When** busca por usuario, fecha o tipo de acción, **Then** ve el listado filtrado de eventos con usuario, acción, fecha/hora e IP.

7. **Given** un usuario ADMIN, **When** intenta acceder a listado o creación de reportes de incidencias, **Then** el sistema no muestra estas opciones (exclusivas para CREADOR y REVISOR).

---

### User Story 4 - Reportes Automatizados Diarios con IA (Priority: P4)

El sistema genera automáticamente un resumen diario de incidencias a una hora programada (configurable). Este resumen incluye estadísticas (total de reportes, delitos frecuentes, zonas con más incidencias) y un texto narrativo generado por inteligencia artificial basado en los campos descriptivos. El reporte se envía por correo electrónico a destinatarios configurables y se almacena en el sistema para consulta posterior por los REVISORES.

**Why this priority**: Es funcionalidad avanzada que agrega valor pero no es crítica para el funcionamiento básico. Requiere integraciones externas (IA, email) que pueden implementarse después del núcleo funcional.

**Independent Test**: Puede probarse configurando la hora de generación, creando reportes de prueba en el período, ejecutando el proceso automático, y verificando el envío del email y almacenamiento del resumen.

**Acceptance Scenarios**:

1. **Given** reportes creados en las últimas 24 horas y configuración de hora de generación (ej: 6:00 AM), **When** el sistema alcanza la hora programada, **Then** se genera automáticamente un resumen con estadísticas agregadas.

2. **Given** un resumen generado automáticamente, **When** el proceso finaliza, **Then** se convierte a formato Word y se envía por correo a todos los destinatarios configurados.

3. **Given** un REVISOR autenticado, **When** accede a la lista de reportes automatizados, **Then** ve todos los resúmenes generados ordenados por fecha con opción de visualización y edición.

4. **Given** un REVISOR editando un modelo de reporte (plantilla markdown), **When** modifica el contenido y guarda, **Then** el modelo actualizado se usa para futuras generaciones automáticas.

5. **Given** un proceso automatizado ejecutándose, **When** completa exitosamente o falla, **Then** se registra el evento en auditoría con detalles del resultado.

---

### User Story 5 - Gestión de Listas de Sugerencias (Priority: P5)

El administrador técnico necesita configurar las listas de sugerencias para campos de texto libre (sexo, delito, tipoDeAtencion) que ayudan a los agentes a llenar el formulario más rápido y con consistencia. Estas listas aparecen como opciones seleccionables pero también permiten entrada manual.

**Why this priority**: Mejora la usabilidad y consistencia de datos pero el sistema puede funcionar con valores por defecto iniciales.

**Independent Test**: Puede probarse modificando las listas de sugerencias desde la administración y verificando que aparecen en los formularios de creación de reportes.

**Acceptance Scenarios**:

1. **Given** un ADMIN en configuración de listas de sugerencias, **When** agrega una nueva opción a la lista de "delito", **Then** la opción aparece disponible en el campo delito de formularios de reportes.

2. **Given** un CREADOR llenando un formulario, **When** hace clic en el campo sexo, **Then** ve las sugerencias configuradas (Masculino, Femenino, Otro) pero puede escribir un valor personalizado.

3. **Given** un ADMIN que elimina una sugerencia, **When** la sugerencia se desactiva, **Then** no aparece en nuevos formularios pero los reportes existentes conservan el valor.

---

### Edge Cases

- **Qué sucede cuando un CREADOR intenta editar un reporte ya entregado**: El sistema muestra el reporte en modo solo lectura sin opciones de edición.
- **Qué sucede cuando se suspende a un usuario con sesión activa**: La sesión se invalida en la próxima solicitud y el usuario es redirigido al login con mensaje de cuenta suspendida.
- **Qué sucede cuando se elimina una zona que tiene reportes asociados**: La zona se marca como inactiva pero los reportes existentes conservan la referencia; no aparece en nuevos formularios.
- **Qué sucede cuando falla el envío de correo del reporte automático**: Se registra el error en auditoría, se notifica al ADMIN, y el reporte queda almacenado para reenvío manual.
- **Qué sucede cuando la API de IA no está disponible**: Se genera el reporte con estadísticas pero sin el resumen narrativo, notificando al ADMIN del error.
- **Qué sucede cuando un usuario tiene múltiples roles**: El usuario ve la interfaz combinada con todas las funcionalidades de sus roles asignados.
- **Qué sucede cuando se intenta crear un usuario con email duplicado**: El sistema rechaza la operación mostrando error de email ya registrado.
- **Qué sucede cuando se elimina un usuario con reportes y registros de auditoría asociados**: El usuario se marca como inactivo (soft delete) preservando su ID y nombre en la base de datos. Los reportes y registros de auditoría mantienen la referencia FK intacta. El registro aparece como "[Usuario Eliminado]" en la interfaz de administración pero conserva la trazabilidad histórica.

## Requirements *(mandatory)*

### Functional Requirements

#### Autenticación y Autorización

- **FR-001**: El sistema DEBE autenticar usuarios mediante credenciales (nombre de usuario y contraseña) con política de complejidad moderada: mínimo 10 caracteres, requiriendo al menos una mayúscula y un número.
- **FR-002**: El sistema DEBE implementar control de acceso basado en roles (RBAC) con tres roles: CREADOR, REVISOR, ADMIN.
- **FR-003**: El sistema DEBE permitir que un usuario tenga múltiples roles asignados simultáneamente.
- **FR-004**: El sistema DEBE impedir el acceso a usuarios con cuenta suspendida (activo = false).
- **FR-005**: El sistema DEBE mantener la sesión del usuario activa durante su uso de la aplicación, con cierre automático después de 30 minutos de inactividad.

#### Gestión de Reportes de Incidencias

- **FR-006**: El sistema DEBE permitir al CREADOR crear nuevos reportes de incidencia seleccionando el tipo (actualmente Tipo A).
- **FR-007**: El sistema DEBE mantener los reportes en estado "borrador" (código 0) mientras el CREADOR los edita.
- **FR-008**: El sistema DEBE cambiar el estado del reporte a "entregado" (código 1) cuando el CREADOR lo entrega.
- **FR-009**: El sistema DEBE impedir que el CREADOR edite reportes en estado "entregado".
- **FR-010**: El sistema DEBE permitir al CREADOR listar, buscar y visualizar solo sus propios reportes.
- **FR-011**: El sistema DEBE permitir al REVISOR listar, buscar, visualizar y editar todos los reportes del sistema.
- **FR-012**: El sistema DEBE permitir al REVISOR exportar reportes a formato PDF con límite de 50 reportes por solicitud directa (exportaciones mayores via background job).
- **FR-013**: El sistema DEBE permitir al REVISOR exportar reportes a formato JSON con límite de 100 reportes por solicitud directa.
- **FR-014**: El sistema DEBE impedir que el REVISOR cree nuevos reportes de incidencias.
- **FR-015**: El sistema DEBE impedir que el ADMIN acceda a reportes de incidencias.

#### Campos Dependientes y Configurables

- **FR-016**: El sistema DEBE mostrar los campos zona, sector y cuadrante como listas desplegables con texto descriptivo (no códigos numéricos).
- **FR-017**: El sistema DEBE cargar la lista de sectores dinámicamente según la zona seleccionada.
- **FR-018**: El sistema DEBE cargar la lista de cuadrantes dinámicamente según el sector seleccionado.
- **FR-019**: El sistema DEBE mostrar listas de sugerencias para campos de texto (sexo, delito, tipoDeAtencion) permitiendo también entrada manual.
- **FR-020**: El sistema DEBE compartir la configuración de campos entre todos los tipos de formulario que usen los mismos campos.

#### Gestión de Usuarios

- **FR-021**: El sistema DEBE permitir al ADMIN listar todos los usuarios del sistema.
- **FR-022**: El sistema DEBE permitir al ADMIN crear nuevos usuarios con nombre, email, contraseña y roles.
- **FR-023**: El sistema DEBE permitir al ADMIN suspender usuarios existentes.
- **FR-024**: El sistema DEBE permitir al ADMIN eliminar usuarios del sistema mediante soft delete (marca como inactivo), preservando integridad referencial con AUDITORIA y REPORTE_INCIDENCIA.
- **FR-025**: El sistema DEBE permitir al ADMIN asignar o modificar roles de usuarios.
- **FR-026**: El sistema DEBE validar que el email sea único para cada usuario.

#### Configuración de Catálogos

- **FR-027**: El sistema DEBE permitir al ADMIN configurar zonas (crear, editar, activar/desactivar).
- **FR-028**: El sistema DEBE permitir al ADMIN configurar sectores asociados a zonas específicas.
- **FR-029**: El sistema DEBE permitir al ADMIN configurar cuadrantes asociados a sectores específicos.
- **FR-030**: El sistema DEBE permitir al ADMIN configurar listas de sugerencias para campos de texto.

#### Auditoría

- **FR-031**: El sistema DEBE registrar todas las operaciones de usuarios en la tabla de auditoría.
- **FR-032**: El sistema DEBE capturar en cada registro de auditoría: código de acción, usuario, fecha/hora, ID relacionado (si aplica) e IP.
- **FR-033**: El sistema DEBE registrar las operaciones de procesos automatizados en auditoría.
- **FR-034**: El sistema DEBE permitir al ADMIN visualizar y buscar registros de auditoría.
- **FR-035**: El sistema DEBE impedir que CREADOR y REVISOR accedan a registros de auditoría.
- **FR-036**: El sistema DEBE retener los registros de auditoría de forma indefinida (sin eliminación automática).

#### Seguridad y Autorización (RS-001 Mitigation)

- **FR-037-SEC**: El sistema DEBE aplicar atributos de autorización `[Authorize]` en TODAS las páginas, componentes y endpoints de API.
- **FR-038-SEC**: El sistema DEBE seguir el principio de deny-by-default: ningún recurso es accesible sin autorización explícita.
- **FR-039-SEC**: El sistema DEBE validar autorización en dos capas: UI (Blazor) y API (Controllers) para defensa en profundidad.
- **FR-040-SEC**: El sistema DEBE registrar en auditoría TODOS los intentos de acceso no autorizado con detalles (usuario, recurso, IP, timestamp).
- **FR-041-SEC**: El sistema DEBE implementar políticas de autorización centralizadas basadas en Claims para permisos granulares.
- **FR-042-SEC**: El sistema DEBE tener cobertura de pruebas del 100% en autorizaciones (matriz Rol × Funcionalidad).

#### Prevención de Inyección y XSS (RS-002 Mitigation)

- **FR-043-SEC**: El sistema DEBE usar EXCLUSIVAMENTE Entity Framework Core con LINQ (prohibido SQL crudo o concatenación de queries).
- **FR-044-SEC**: El sistema DEBE validar TODAS las entradas de usuario con FluentValidation antes de procesamiento.
- **FR-045-SEC**: El sistema DEBE aplicar Content Security Policy (CSP) headers restrictivos prohibiendo inline scripts.
- **FR-046-SEC**: El sistema DEBE usar HtmlEncoder.Default para cualquier contenido generado por usuario que se renderice en HTML.
- **FR-047-SEC**: El sistema DEBE escanear el código automáticamente en CI/CD con SonarQube y Snyk para detectar patrones vulnerables.
- **FR-048-SEC**: El sistema DEBE rechazar entradas que excedan los límites de longitud definidos en el modelo de datos.

#### Protección de Datos en Logs (RS-003 Mitigation)

- **FR-049-SEC**: El sistema DEBE configurar Serilog para NUNCA loguear contraseñas, tokens, API keys o credenciales.
- **FR-050-SEC**: El sistema DEBE aplicar redacción automática de datos personales (email, teléfono, nombres) en logs mediante regex patterns.
- **FR-051-SEC**: El sistema DEBE mantener logs de aplicación separados de logs de auditoría con políticas de retención diferenciadas (30 días vs indefinido).
- **FR-052-SEC**: El sistema DEBE encriptar logs de aplicación en disco.
- **FR-053-SEC**: El sistema DEBE restringir acceso a logs únicamente a roles ADMIN y DevOps.
- **FR-054-SEC**: El sistema DEBE escanear logs automáticamente para detectar exposición accidental de datos sensibles.

#### Protección contra Fuerza Bruta (RS-004 Mitigation)

- **FR-055-SEC**: El sistema DEBE bloquear cuentas automáticamente después de 5 intentos fallidos de login por 30 minutos.
- **FR-056-SEC**: El sistema DEBE mostrar reCAPTCHA después de 3 intentos fallidos de login.
- **FR-057-SEC**: El sistema DEBE aplicar rate limiting de 10 intentos de login por minuto por IP.
- **FR-058-SEC**: El sistema DEBE implementar delays progresivos exponenciales (1s, 2s, 4s, 8s) entre intentos fallidos.
- **FR-059-SEC**: El sistema DEBE registrar en auditoría TODOS los intentos fallidos de login con IP, timestamp y username.
- **FR-060-SEC**: El sistema DEBE generar alertas cuando se detecten >50 intentos fallidos por hora desde una IP.

#### Protección de Sesiones (RS-005 Mitigation)

- **FR-061-SEC**: El sistema DEBE forzar HTTPS en producción con headers HSTS (max-age=31536000, includeSubDomains, preload).
- **FR-062-SEC**: El sistema DEBE configurar cookies de sesión con flags `Secure`, `HttpOnly`, y `SameSite=Strict`.
- **FR-063-SEC**: El sistema DEBE regenerar el session ID inmediatamente después de login exitoso.
- **FR-064-SEC**: El sistema DEBE validar User-Agent en cada request y detectar cambios sospechosos.
- **FR-065-SEC**: El sistema DEBE aplicar tokens Anti-CSRF en TODAS las operaciones que modifican estado.
- **FR-066-SEC**: El sistema DEBE registrar en auditoría toda creación y destrucción de sesiones.

#### Respaldos y Recuperación (RO-001 Mitigation)

- **FR-067-OPS**: El sistema DEBE ejecutar backup completo de la base de datos diariamente a las 2:00 AM.
- **FR-068-OPS**: El sistema DEBE ejecutar backups incrementales cada 6 horas durante horario laboral (8 AM, 2 PM, 8 PM).
- **FR-069-OPS**: El sistema DEBE validar la integridad de cada backup usando `pg_restore --list` inmediatamente después de su creación.
- **FR-070-OPS**: El sistema DEBE comprimir backups con gzip alcanzando ratio mínimo 3:1.
- **FR-071-OPS**: El sistema DEBE almacenar backups en disco secundario (/mnt/backups) Y copia offsite (S3/NAS).
- **FR-072-OPS**: El sistema DEBE implementar retención de backups: 7 diarios, 4 semanales, 12 mensuales, 3 anuales.
- **FR-073-OPS**: El sistema DEBE enviar alerta inmediata por email/Slack si un backup falla o excede 15 minutos.
- **FR-074-OPS**: El sistema DEBE alertar si el último backup válido tiene más de 24 horas de antigüedad.
- **FR-075-OPS**: El sistema DEBE ejecutar prueba de restauración mensual automatizada en ambiente staging.
- **FR-076-OPS**: El sistema DEBE loguear cada intento de backup con timestamp, tamaño, duración, y éxito/fallo.
- **FR-077-OPS**: El sistema DEBE alertar si el tamaño de un backup varía >20% respecto al promedio de los últimos 7 días.
- **FR-078-OPS**: El sistema DEBE mantener runbook de disaster recovery documentado con RTO <1 hora y RPO <6 horas.

#### Gestión de Recursos (RO-002 Mitigation)

- **FR-079-OPS**: El sistema DEBE configurar límites de recursos Docker: 2 CPU cores y 4 GB RAM por contenedor.
- **FR-080-OPS**: El sistema DEBE configurar connection pool de base de datos con máximo 20 conexiones simultáneas.
- **FR-081-OPS**: El sistema DEBE implementar rate limiting: 5 req/min para exports, 10 req/min para búsquedas, 3 req/min para AI.
- **FR-082-OPS**: El sistema DEBE limitar concurrencia de Hangfire a máximo 3 background jobs simultáneos.
- **FR-083-OPS**: El sistema DEBE cachear catálogos estáticos (Zonas, Sectores, Cuadrantes) con TTL de 1 hora.
- **FR-084-OPS**: El sistema DEBE implementar health check endpoint (/health) verificando DB, disk, y memoria.
- **FR-085-OPS**: El sistema DEBE generar alertas cuando CPU >70% por 5 minutos o RAM >75%.
- **FR-086-OPS**: El sistema DEBE exportar métricas Prometheus (CPU, RAM, disk I/O, network, request latency).
- **FR-087-OPS**: El sistema DEBE responder HTTP 503 cuando recursos insuficientes en lugar de timeout.
- **FR-088-OPS**: El sistema DEBE mantener diseño stateless permitiendo horizontal scaling futuro.

#### Entrega de Correo Electrónico (RO-003 Mitigation)

- **FR-089-OPS**: El sistema DEBE implementar retry policy con exponential backoff (0s, 1min, 5min) para envío de emails.
- **FR-090-OPS**: El sistema DEBE configurar fallback automático de SMTP a API (SendGrid/Mailgun) si primario falla.
- **FR-091-OPS**: El sistema DEBE persistir emails fallidos en tabla EMAIL_QUEUE con máximo 10 reintentos.
- **FR-092-OPS**: El sistema DEBE procesar cola EMAIL_QUEUE cada 5 minutos mediante background job.
- **FR-093-OPS**: El sistema DEBE implementar circuit breaker para SMTP (abre después de 5 fallos consecutivos, 2 min).
- **FR-094-OPS**: El sistema DEBE respetar rate limiting de 100 emails/hora con throttling automático al 90%.
- **FR-095-OPS**: El sistema DEBE registrar TODOS los intentos de envío en auditoría (timestamp, destinatario, status).
- **FR-096-OPS**: El sistema DEBE alertar administradores si tasa de fallo email >10% en última hora.
- **FR-097-OPS**: El sistema DEBE validar direcciones email (RFC 5322) y limitar attachments a 10 MB total.
- **FR-098-OPS**: El sistema DEBE mostrar métricas de email delivery en Hangfire dashboard (enviados, pendientes, fallidos).

#### Resiliencia de Servicios Externos (RO-004 Mitigation)

- **FR-099-OPS**: El sistema DEBE implementar circuit breaker Polly en TODOS los servicios externos (IA, email, futuros).
- **FR-100-OPS**: El sistema DEBE configurar timeouts: 30s para IA, 15s para email SMTP, 30s para base de datos.
- **FR-101-OPS**: El sistema DEBE degradar gracefully cuando servicios externos no disponibles (reportes sin narrativa, emails en cola).
- **FR-102-OPS**: El sistema DEBE exponer endpoint /health con health checks de servicios externos (cada 5 min).
- **FR-103-OPS**: El sistema DEBE exportar métricas Prometheus de servicios externos (duración, total, circuit state).
- **FR-104-OPS**: El sistema DEBE registrar histórico de disponibilidad en tabla SERVICE_HEALTH_LOG.
- **FR-105-OPS**: El sistema DEBE alertar si circuit breaker permanece abierto por >10 minutos.
- **FR-106-OPS**: El sistema DEBE mostrar mensajes descriptivos al usuario cuando funcionalidad degradada activa.
- **FR-107-OPS**: El sistema DEBE permitir a ADMIN deshabilitar manualmente servicios externos vía feature flags.
- **FR-108-OPS**: El sistema DEBE implementar retry con jitter solo para errores transitorios (no errores lógicos).

#### Gestión de Dependencias (RO-005 Mitigation)

- **FR-109-OPS**: El sistema DEBE seguir política LTS-first: solo .NET LTS, PostgreSQL con 5+ años soporte, NuGet estable.
- **FR-110-OPS**: El sistema DEBE integrar Dependabot + Snyk en CI/CD para escaneo automático de vulnerabilidades.
- **FR-111-OPS**: El sistema DEBE ejecutar suite completa de pruebas en staging idéntico a producción antes de updates.
- **FR-112-OPS**: El sistema DEBE mantener CHANGELOG.md con semantic versioning y referencias CVE para security patches.
- **FR-113-OPS**: El sistema DEBE crear snapshots/backups antes de upgrades mayores con rollback en <15 minutos.
- **FR-114-OPS**: El sistema DEBE programar updates en ventana mensual (primer domingo 2-6 AM, excluir períodos críticos).
- **FR-115-OPS**: El sistema DEBE pinear dependencias a versiones exactas (Docker SHA256, NuGet exactos, no wildcards).
- **FR-116-OPS**: El sistema DEBE monitorear error rate, latency, y resources 24h post-deployment con rollback si crítico.
- **FR-117-OPS**: El sistema DEBE documentar breaking changes en MIGRATIONS.md con pasos de upgrade.
- **FR-118-OPS**: El sistema DEBE notificar stakeholders 48h antes de mantenimiento programado (Slack + email).

#### Experiencia de Usuario y Adopción (RN-001 Mitigation)

- **FR-119-BIZ**: El sistema DEBE mostrar tour guiado interactivo en primer login del usuario.
- **FR-120-BIZ**: El sistema DEBE proporcionar tooltips contextuales en campos complejos con ejemplos.
- **FR-121-BIZ**: El sistema DEBE validar inputs en tiempo real con mensajes en lenguaje claro.
- **FR-122-BIZ**: El sistema DEBE permitir acceso a manual de usuario desde UI principal.
- **FR-123-BIZ**: El sistema DEBE incluir videos tutoriales embebidos (<2 min cada uno).
- **FR-124-BIZ**: El sistema DEBE rastrear tiempo de completación de reportes para métricas de UX.
- **FR-125-BIZ**: El sistema DEBE permitir envío de feedback desde UI (rating + comentarios).
- **FR-126-BIZ**: El sistema DEBE calcular NPS (Net Promoter Score) mensualmente.
- **FR-127-BIZ**: El sistema DEBE cargar página principal en <2 segundos (p95).
- **FR-128-BIZ**: El sistema DEBE generar métricas de adopción para dashboard de ADMIN (usuarios activos, reportes/usuario, feature usage).

#### Gestión de Cambios Institucionales (RN-002 Mitigation)

- **FR-129-BIZ**: El sistema DEBE permitir configuración de reglas de negocio sin redespliegue (tabla SYSTEM_CONFIG).
- **FR-130-BIZ**: El sistema DEBE mantener historial de cambios de configuración con audit trail (CONFIG_HISTORY).
- **FR-131-BIZ**: El sistema DEBE soportar feature flags con rollout por porcentaje, rol, o usuario específico.
- **FR-132-BIZ**: El sistema DEBE permitir versionado de formularios para soporte multi-versión (N-2 versiones).
- **FR-133-BIZ**: El sistema DEBE mantener spec.md como documento vivo con versionado semántico (v1.0, v1.1, v2.0).
- **FR-134-BIZ**: El sistema DEBE reservar 20% de capacidad de sprint para cambios emergentes institucionales.
- **FR-135-BIZ**: El sistema DEBE documentar mapping de requisitos a regulaciones (compliance matrix).
- **FR-136-BIZ**: El sistema DEBE permitir rollback de configuraciones a versión anterior.
- **FR-137-BIZ**: El sistema DEBE proveer UI de administración para modificar SYSTEM_CONFIG sin código.
- **FR-138-BIZ**: El sistema DEBE notificar stakeholders de cambios regulatorios con proceso de change request formal.

#### Transición de Papel a Digital (RN-003 Mitigation)

- **FR-139-BIZ**: El sistema DEBE soportar programa piloto con tracking de usuarios beta (tabla PILOT_USER).
- **FR-140-BIZ**: El sistema DEBE proveer soporte técnico vía tickets (tabla SUPPORT_TICKET) con SLA <4h.
- **FR-141-BIZ**: El sistema DEBE implementar plan de transición gradual (4 fases sobre 8 semanas).
- **FR-142-BIZ**: El sistema DEBE rastrear badges/logros de usuarios para gamificación (tabla USER_ACHIEVEMENT).
- **FR-143-BIZ**: El sistema DEBE permitir identificación de "champions" con acceso a portal exclusivo.
- **FR-144-BIZ**: El sistema DEBE generar métricas de satisfacción piloto para go-live readiness (target >75%).
- **FR-145-BIZ**: El sistema DEBE proveer impresión de reportes en formato PDF optimizado para papel.
- **FR-146-BIZ**: El sistema DEBE rastrear tasas de adopción (activación, usuarios semanales activos).
- **FR-147-BIZ**: El sistema DEBE permitir capacitación con tracking de completación (tabla TRAINING_COMPLETION).
- **FR-148-BIZ**: El sistema DEBE comunicar beneficios tangibles con métricas comparativas (tiempo digital vs papel).

#### Alta Disponibilidad y Continuidad (RN-004 Mitigation)

- **FR-149-BIZ**: El sistema DEBE mantener uptime >99.5% durante horario laboral (8 AM - 8 PM).
- **FR-150-BIZ**: El sistema DEBE proveer endpoint `/health` con checks de DB, disk, memory.
- **FR-151-BIZ**: El sistema DEBE implementar auto-restart de contenedores Docker en fallo.
- **FR-152-BIZ**: El sistema DEBE proveer página de status pública (`/status`) sin autenticación.
- **FR-153-BIZ**: El sistema DEBE programar mantenimientos EXCLUSIVAMENTE fuera de horario laboral (2-6 AM).
- **FR-154-BIZ**: El sistema DEBE notificar stakeholders 48h antes de mantenimiento programado.
- **FR-155-BIZ**: El sistema DEBE registrar incidentes en tabla INCIDENT_LOG con MTTR <30 min.
- **FR-156-BIZ**: El sistema DEBE implementar runbooks de troubleshooting documentados (docs/runbooks/).
- **FR-157-BIZ**: El sistema DEBE alertar vía PagerDuty/Email/SMS en incidentes críticos (<15 min response).
- **FR-158-BIZ**: El sistema DEBE monitorear latencia p95 con alerta si >3s sostenidos por 5 min.

#### Privacidad y Seguridad de Datos (RN-005 Mitigation)

- **FR-159-BIZ**: El sistema DEBE encriptar datos en reposo (disk encryption) y tránsito (TLS 1.3).
- **FR-160-BIZ**: El sistema DEBE aplicar watermarking en PDFs exportados (usuario + timestamp).
- **FR-161-BIZ**: El sistema DEBE detectar anomalías de acceso (>50 descargas/h) con alertas automáticas.
- **FR-162-BIZ**: El sistema DEBE realizar revisión trimestral de accesos con ADMIN (tabla ACCESS_REVIEW).
- **FR-163-BIZ**: El sistema DEBE desactivar cuentas inactivas >90 días automáticamente.
- **FR-164-BIZ**: El sistema DEBE registrar incidentes de seguridad en tabla SECURITY_INCIDENT con SIRT.
- **FR-165-BIZ**: El sistema DEBE implementar training de seguridad anual obligatorio (tabla SECURITY_TRAINING).
- **FR-166-BIZ**: El sistema DEBE encriptar backups con GPG (AES256) antes de almacenamiento offsite.
- **FR-167-BIZ**: El sistema DEBE cumplir con Ley Federal de Protección de Datos Personales (Mexico).
- **FR-168-BIZ**: El sistema DEBE realizar penetration testing externo anualmente con reporte de hallazgos.

#### Gestión de Alcance y Cambios (RP-001 Mitigation)

- **FR-169-PROJ**: El sistema DEBE mantener tabla CHANGE_REQUEST para tracking formal de change requests con CAB approval workflow.
- **FR-170-PROJ**: El sistema DEBE generar CR number único automáticamente (formato: CR-YYYY-NNN) al crear change request.
- **FR-171-PROJ**: El sistema DEBE requerir impact assessment (scope, timeline, resources, risks) para todo change request.
- **FR-172-PROJ**: El sistema DEBE notificar CAB members (PM, PO, Tech Lead) vía email al submitir change request.
- **FR-173-PROJ**: El sistema DEBE permitir decision workflow (submitted → under_review → approved/rejected/deferred) con rationale obligatorio.
- **FR-174-PROJ**: El sistema DEBE mantener tabla SPRINT_METRICS para tracking de velocidad (planned vs completed story points).
- **FR-175-PROJ**: El sistema DEBE calcular velocity health status (healthy >85%, warning >70%, critical <70%) basado en últimos 3 sprints.
- **FR-176-PROJ**: El sistema DEBE documentar features out-of-scope en spec.md sección "Excluded Features" con rationale y GitHub issue link.
- **FR-177-PROJ**: El sistema DEBE enforcer Definition of Done (unit tests, integration tests, code review, docs updated, staging deploy).
- **FR-178-PROJ**: El sistema DEBE implementar scope freeze 2 semanas antes de release (solo bug fixes, no new features).

#### Gestión del Conocimiento (RP-002 Mitigation)

- **FR-179-PROJ**: El sistema DEBE mantener tabla SPIKE_PROJECT para tracking de technical spikes (technology, developer, findings_document_url).
- **FR-180-PROJ**: El sistema DEBE documentar Architecture Decision Records (ADRs) en docs/adr/ con template estandarizado (context, decision, rationale, consequences).
- **FR-181-PROJ**: El sistema DEBE mantener docs/learning-resources.md con recursos aprobados (docs oficiales, cursos, books, code samples).
- **FR-182-PROJ**: El sistema DEBE proveer knowledge base interna (Wiki/Notion) con How-To guides, FAQs, troubleshooting.
- **FR-183-PROJ**: El sistema DEBE programar knowledge sharing sessions semanales (1h cada viernes, rotating presenter).
- **FR-184-PROJ**: El sistema DEBE trackear knowledge sessions (topic, presenter, recording_url, slides_url, attendees_count).
- **FR-185-PROJ**: El sistema DEBE enforcer mandatory code review con 2 approvals mínimo y checklist que incluya "knowledge transfer".
- **FR-186-PROJ**: El sistema DEBE presupuestar 5 días de consultoría externa para arquitectura review, code review, performance optimization.
- **FR-187-PROJ**: El sistema DEBE implementar incremental complexity approach (empezar con CRUD simple, escalar a features complejas).
- **FR-188-PROJ**: El sistema DEBE documentar bugs causados por knowledge gaps con tag "knowledge-gap" para post-mortem analysis.

#### Resiliencia del Equipo (RP-003 Mitigation)

- **FR-189-PROJ**: El sistema DEBE mantener tabla SKILLS_MATRIX con competency levels (1=aware, 2=assist, 3=own, 4=expert) por developer y skill area.
- **FR-190-PROJ**: El sistema DEBE enforcer backup owner system (primary + secondary + tertiary) para todas critical areas.
- **FR-191-PROJ**: El sistema DEBE calcular bus factor por área (target: ≥2 personas con competency ≥3).
- **FR-192-PROJ**: El sistema DEBE proveer skills matrix dashboard para PM/Tech Lead mostrando competency gaps y bus factor risk.
- **FR-193-PROJ**: El sistema DEBE enforcer handoff checklist formal para transferencias (knowledge sessions, docs review, pending tasks).
- **FR-194-PROJ**: El sistema DEBE grabar technical sessions importantes (architecture walkthroughs, feature deep-dives) con storage en shared drive.
- **FR-195-PROJ**: El sistema DEBE implementar pair programming rotation semanal para knowledge distribution.
- **FR-196-PROJ**: El sistema DEBE mantener README.md en cada módulo + inline comments + API docs (XML comments C#).
- **FR-197-PROJ**: El sistema DEBE dedicar 10% de sprint capacity a cross-training (developers trabajan fuera de primary area).
- **FR-198-PROJ**: El sistema DEBE enforcer que unplanned absence impact sea <2 días delay (test de continuidad sin key person).

#### Automatización de Infraestructura (RP-004 Mitigation)

- **FR-199-PROJ**: El sistema DEBE provisionar servidor Fedora 42 ANTES de desarrollo (Week 0) con base config completa.
- **FR-200-PROJ**: El sistema DEBE proveer setup scripts automatizados e idempotentes (01-os-baseline.sh, 02-docker-install.sh, 03-postgresql-setup.sh).
- **FR-201-PROJ**: El sistema DEBE soportar .NET ASPIRE para desarrollo local (PostgreSQL container, app orchestration, no server dependency).
- **FR-202-PROJ**: El sistema DEBE implementar CI/CD pipeline (GitHub Actions) en Week 2 con automated tests en PRs y deployment a staging.
- **FR-203-PROJ**: El sistema DEBE mantener staging environment con paridad 100% a producción (OS, Docker, PostgreSQL versions, resources, network).
- **FR-204-PROJ**: El sistema DEBE ejecutar deployment smoke tests semanalmente con checklist documentado (pre-deploy, deploy steps, smoke tests, sign-off).
- **FR-205-PROJ**: El sistema DEBE proveer .env.example file mostrando todas environment variables requeridas (nunca hardcode config).
- **FR-206-PROJ**: El sistema DEBE tagear Docker images con version específica (no 'latest') para rollback capability.
- **FR-207-PROJ**: El sistema DEBE documentar procedimientos de infraestructura (docs/infrastructure.md, docs/quickstart.md, docs/deployment.md).
- **FR-208-PROJ**: El sistema DEBE lograr setup time <4h (clean VM → working app) y developer onboarding <30min (ASPIRE local setup).

#### Reportes Automatizados

- **FR-209-AUTO**: El sistema DEBE generar automáticamente un resumen diario de incidencias a una hora configurable.
- **FR-210-AUTO**: El sistema DEBE incluir en el resumen: total de reportes, delitos frecuentes, zonas con más incidencias.
- **FR-211-AUTO**: El sistema DEBE generar un texto narrativo del resumen usando inteligencia artificial.
- **FR-212-AUTO**: El sistema DEBE convertir el resumen de Markdown a Word para envío por correo.
- **FR-213-AUTO**: El sistema DEBE enviar el resumen por correo electrónico a destinatarios configurables.
- **FR-214-AUTO**: El sistema DEBE almacenar los reportes automatizados en la base de datos.
- **FR-215-AUTO**: El sistema DEBE permitir al REVISOR listar, visualizar y editar reportes automatizados.
- **FR-216-AUTO**: El sistema DEBE permitir al REVISOR editar los modelos de reporte (plantillas markdown).

### Key Entities

- **Usuario**: Representa a las personas que acceden al sistema. Contiene identificación, credenciales, estado (activo/suspendido), roles asignados, y datos de perfil opcionales. Un usuario puede ser CREADOR, REVISOR, ADMIN, o combinación de roles.

- **Reporte de Incidencia (Tipo A)**: Documento que captura un evento atendido por los agentes. Contiene datos del solicitante (sexo, edad, características poblacionales), ubicación geográfica (zona/sector/cuadrante), descripción de hechos, acciones realizadas, y metadatos (creador, fecha, estado). El estado transiciona de borrador a entregado.

- **Zona/Sector/Cuadrante**: Entidades jerárquicas que representan la división geográfica operativa. Cada zona contiene múltiples sectores, y cada sector contiene múltiples cuadrantes. Son configurables por el administrador.

- **Registro de Auditoría**: Captura cada operación realizada en el sistema con identificación de quién, qué, cuándo y desde dónde. Permite trazabilidad completa de acciones.

- **Catálogo de Sugerencias**: Listas de opciones predefinidas para campos de texto libre que ayudan a la consistencia de datos sin restringir la entrada.

- **Reporte Automatizado**: Resumen diario generado por el sistema con estadísticas e inteligencia artificial. Almacenado para consulta y editable por supervisores.

- **Modelo de Reporte**: Plantilla en formato Markdown que define la estructura del reporte automatizado. Editable por supervisores para ajustar el formato de salida.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Los usuarios CREADOR pueden completar un reporte de incidencia Tipo A en menos de 5 minutos.
- **SC-002**: Los usuarios CREADOR pueden encontrar un reporte específico en su historial en menos de 30 segundos.
- **SC-003**: Los usuarios REVISOR pueden filtrar y encontrar reportes específicos entre más de 1,000 registros en menos de 10 segundos.
- **SC-004**: El sistema mantiene un 99.5% de disponibilidad durante horario laboral (8:00 AM - 8:00 PM).
- **SC-005**: La exportación a PDF de hasta 50 reportes se completa en menos de 30 segundos.
- **SC-006**: El 100% de las operaciones de usuario quedan registradas en auditoría con datos completos.
- **SC-007**: El reporte automatizado diario se genera y envía dentro de los 15 minutos siguientes a la hora programada.
- **SC-008**: El sistema soporta al menos 50 usuarios concurrentes sin degradación perceptible de rendimiento.
- **SC-009**: El 95% de los usuarios completan su primera tarea exitosamente sin asistencia (usabilidad).
- **SC-010**: Cero incidentes de acceso no autorizado a datos o funcionalidades por rol.
- **SC-011**: El sistema permite la adición de nuevos tipos de reporte (Tipo B, C, etc.) sin reestructuración de la base de datos existente mediante campos JSONB extensibles (RT-004 mitigation).
- **SC-012**: Los respaldos de datos se ejecutan diariamente con posibilidad de restauración en menos de 1 hora.

## Assumptions

- La generación de texto narrativo utilizará una capa de abstracción agnóstica al proveedor de IA, permitiendo múltiples backends (OpenAI, Azure OpenAI, LLM local) mediante una interfaz común.
- El envío de correos utilizará configuración SMTP estándar (host, puerto, credenciales) definida en variables de entorno, compatible con cualquier proveedor de email.
- Los usuarios tendrán conexión a internet estable para acceder a la aplicación web.
- El servidor de producción (Fedora Linux Server 42) cuenta con dos unidades de almacenamiento: principal para aplicación/BD y secundaria para respaldos.
- Los navegadores soportados son versiones recientes de Chrome, Firefox, Edge y Safari (últimas 2 versiones mayores).
- La zona horaria de operación es la correspondiente a Ciudad de México (America/Mexico_City).
- Los campos de texto largo (hechosReportados, accionesRealizadas, observaciones) no tienen límite práctico pero se asume contenido menor a 10,000 caracteres típicamente.
- El campo "turnoCeiba" es un identificador numérico proporcionado por el agente (no generado por el sistema).
- Los servicios de IA tienen disponibilidad variable con SLA asumido de 95% uptime; el sistema debe funcionar en modo degradado (sin narrativas, solo estadísticas) durante el 5% de indisponibilidad esperada mediante fallback graceful implementado en FR-101-OPS y RT-001 mitigation (T092d).
- Las llamadas a servicios de IA tienen timeout de 30 segundos; si exceden este tiempo se considera fallo y se usa fallback.

## Out of Scope

- Aplicación móvil nativa (se usará interfaz web responsiva).
- Integración con sistemas externos de la SSC más allá de los mencionados (email, IA).
- Notificaciones push o en tiempo real.
- Reportes tipo B, C, etc. (solo se implementa Tipo A en esta fase, con arquitectura extensible).
- Flujo de aprobación multi-nivel para reportes.
- Chat o comunicación entre usuarios dentro de la plataforma.
- Geolocalización automática (la ubicación se ingresa manualmente).
- Reconocimiento de voz para llenado de formularios.
