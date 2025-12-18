# Pull Request - Sistema de Reportes de Incidencias Ceiba

## Descripción
<!-- Proporciona una breve descripción de los cambios en este PR -->

## Tipo de Cambio
- [ ] 🐛 Bug fix (cambio que corrige un issue)
- [ ] ✨ Nueva funcionalidad (cambio que agrega funcionalidad)
- [ ] 💥 Breaking change (cambio que afecta funcionalidad existente)
- [ ] 🗃️ Migración de base de datos (incluye cambios de esquema)
- [ ] 🔒 Mejora de seguridad
- [ ] 📝 Actualización de documentación
- [ ] 🔧 Cambio de configuración

## Tareas Relacionadas
<!-- Vincula los items de tasks.md (ej. T042, US1-T003) -->
- Closes:

## Testing Realizado
- [ ] Tests unitarios agregados/actualizados
- [ ] Tests de integración agregados/actualizados
- [ ] Tests de componentes agregados/actualizados (Blazor)
- [ ] Tests E2E agregados/actualizados
- [ ] Testing manual realizado
- [ ] Todos los tests pasan localmente

## Checklist de Seguridad (T020e - RS-001 a RS-006)

### Verificación OWASP Top 10
- [ ] **A01:2021 - Control de Acceso Roto**
  - [ ] Verificaciones de autorización implementadas para todos los recursos protegidos
  - [ ] Permisos basados en roles verificados (CREADOR, REVISOR, ADMIN)
  - [ ] Escalación de privilegios horizontal prevenida (usuarios no pueden acceder a datos de otros)
  - [ ] Escalación de privilegios vertical prevenida (límites de roles respetados)

- [ ] **A02:2021 - Fallas Criptográficas**
  - [ ] Datos sensibles cifrados en tránsito (HTTPS forzado)
  - [ ] Contraseñas hasheadas con ASP.NET Identity (no almacenadas en texto plano)
  - [ ] Sin datos sensibles en logs (PII redactada por PIIRedactionEnricher)
  - [ ] Cadenas de conexión en configuración segura (no hardcodeadas)

- [ ] **A03:2021 - Inyección**
  - [ ] **SIN concatenación de SQL raw** (solo consultas parametrizadas o EF Core)
  - [ ] Input de usuario validado y sanitizado
  - [ ] Consultas LINQ usadas en lugar de SQL raw
  - [ ] Sin LINQ dinámico con input de usuario

- [ ] **A04:2021 - Diseño Inseguro**
  - [ ] Requisitos de seguridad revisados contra FR-001 a FR-007
  - [ ] Modelo de amenazas considerado para nuevas funcionalidades
  - [ ] Principio de mínimo privilegio aplicado

- [ ] **A05:2021 - Configuración de Seguridad Incorrecta**
  - [ ] Sin credenciales por defecto en configuración de producción
  - [ ] Headers de seguridad configurados (CSP, HSTS, X-Frame-Options)
  - [ ] Mensajes de error no filtran información sensible
  - [ ] Funcionalidades solo de desarrollo deshabilitadas en producción

- [ ] **A06:2021 - Componentes Vulnerables y Desactualizados**
  - [ ] Todos los paquetes NuGet actualizados
  - [ ] Sin vulnerabilidades conocidas en dependencias
  - [ ] Última versión de parche de .NET 10 usada

- [ ] **A07:2021 - Fallas de Identificación y Autenticación**
  - [ ] Política de contraseñas aplicada (mín 10 caracteres, mayúscula + dígito)
  - [ ] Timeout de sesión implementado (30 minutos - FR-005)
  - [ ] Validación de User-Agent activa (RS-005)
  - [ ] Tokens anti-CSRF usados en formularios

- [ ] **A08:2021 - Fallas de Integridad de Software y Datos**
  - [ ] Migraciones de base de datos incluyen procedimientos de rollback
  - [ ] Backups pre-migración creados (MigrationBackupService)
  - [ ] Sin ejecución de código sin firmar o sin verificar

- [ ] **A09:2021 - Fallas de Logging y Monitoreo de Seguridad**
  - [ ] Operaciones críticas logueadas en tabla de auditoría (RegistroAuditoria)
  - [ ] Intentos de autenticación fallidos logueados
  - [ ] Fallas de autorización logueadas (AuthorizationLoggingMiddleware)
  - [ ] Logs incluyen contexto suficiente (usuario, IP, timestamp, acción)
  - [ ] Sin PII en logs (verificado por PIIRedactionEnricher)

- [ ] **A10:2021 - Server-Side Request Forgery (SSRF)**
  - [ ] Sin URLs controladas por usuario en peticiones HTTP
  - [ ] Llamadas a APIs externas validadas y restringidas

### Validación de Input (Mitigación RS-004)
- [ ] Todos los inputs de usuario validados en servidor
- [ ] Validación del lado del cliente es solo suplementaria
- [ ] Data annotations usadas en DTOs/modelos
- [ ] Límites de longitud de strings aplicados
- [ ] Rangos numéricos validados
- [ ] Rangos de fechas validados
- [ ] Uploads de archivos validados (tipo, tamaño, contenido)
- [ ] Caracteres especiales manejados de forma segura

### Prevención de Inyección SQL (Crítico)
- [ ] **CERO concatenación de strings SQL raw con input de usuario**
- [ ] Todas las consultas de BD usan EF Core LINQ o consultas parametrizadas
- [ ] `FromSqlRaw` solo usado con parámetros (placeholders `{0}`)
- [ ] Sin nombres de tabla/columna dinámicos desde input de usuario
- [ ] Verificado por analizador Roslyn (sin warnings)

### Prevención de Cross-Site Scripting (XSS)
- [ ] Escapado automático de Blazor mantenido (sin `@((MarkupString)userInput)`)
- [ ] Contenido generado por usuario sanitizado antes de mostrar
- [ ] Headers CSP configurados en Program.cs
- [ ] Sin equivalentes a `dangerouslySetInnerHTML`

### Autenticación y Autorización
- [ ] Endpoints protegidos con atributo `[Authorize]` o política
- [ ] Acceso anónimo explícitamente marcado con `[AllowAnonymous]`
- [ ] Requisitos de rol verificados (RequireCreadorRole, RequireRevisorRole, RequireAdminRole)
- [ ] Contexto de usuario actual obtenido correctamente (HttpContextAccessor)
- [ ] Secuestro de sesión mitigado (UserAgentValidationMiddleware activo)

### Logging de Auditoría (Mitigación RS-001)
- [ ] Todas las modificaciones de datos logueadas (automático vía AuditSaveChangesInterceptor)
- [ ] Entradas de auditoría manuales creadas para operaciones de negocio (IAuditService)
- [ ] Logs de auditoría incluyen: UserId, ActionCode, IP, Timestamp, Details
- [ ] Operaciones fallidas logueadas (no solo las exitosas)
- [ ] Logs de auditoría inmutables (sin UPDATE o DELETE en RegistroAuditoria)

### Manejo de Datos Sensibles (Mitigación RS-003)
- [ ] Sin contraseñas, API keys o secretos en código
- [ ] Secretos almacenados en variables de entorno o Azure Key Vault
- [ ] PII redactada de logs (email, IP, CURP, números de teléfono)
- [ ] Backups de base de datos asegurados y cifrados
- [ ] Sin datos sensibles en mensajes de error mostrados a usuarios

### Seguridad de Configuración
- [ ] Feature flags usados en lugar de cambios de código (configuración FeatureFlags)
- [ ] Cadenas de conexión de BD en appsettings (no hardcodeadas)
- [ ] Configuraciones de desarrollo no desplegadas a producción
- [ ] Política CORS configurada con orígenes específicos (no `*`)

### Cambios de Base de Datos
- [ ] Migración incluye métodos Up y Down
- [ ] Changelog de migración actualizado en MIGRATIONS.md
- [ ] Breaking changes documentados
- [ ] Scripts de migración de datos probados en copia de datos de producción
- [ ] Índices creados para nuevos patrones de consulta
- [ ] Restricciones de foreign key verificadas

### Calidad de Código
- [ ] Sin errores de compilador
- [ ] Sin warnings críticos de analizador (reglas CA, IDE)
- [ ] Código sigue convenciones de nombres C# (PascalCase, camelCase, _privateFields)
- [ ] Documentación XML en APIs públicas
- [ ] Sin statements using no utilizados
- [ ] Sin código comentado

### Consideraciones de Rendimiento
- [ ] Consultas de BD optimizadas (sin consultas N+1)
- [ ] Paginación implementada para conjuntos de resultados grandes
- [ ] Índices creados para columnas consultadas frecuentemente
- [ ] Sin llamadas síncronas a BD en métodos async
- [ ] HttpClient usado correctamente (no nueva instancia por petición)

## Cumplimiento de Constitución (Principios No Negociables)

- [ ] **Principio I - Diseño Modular**: Cambios contenidos dentro de límites de módulo
- [ ] **Principio II - TDD Obligatorio**: Tests escritos antes de implementación (Red-Green-Refactor)
- [ ] **Principio III - Seguridad por Diseño**: Mínimo privilegio y OWASP Top 10 abordados
- [ ] **Principio IV - Accesibilidad**: Mobile-responsive, cumple WCAG Nivel AA
- [ ] **Principio V - Documentación como Entregable**: Código documentado, README actualizado si es necesario

## Evaluación de Riesgos (RS-001 a RS-006, RT-001 a RT-006)

### Riesgos de Seguridad Abordados
- [ ] RS-001: Acceso no autorizado (Políticas de autorización + logging)
- [ ] RS-002: Ataques XSS (Headers CSP + escapado de Blazor)
- [ ] RS-003: Exposición de datos en logs (Redacción de PII)
- [ ] RS-004: Integridad de datos (Validación de input)
- [ ] RS-005: Secuestro de sesión (Validación de User-Agent + cookies seguras)
- [ ] RS-006: Inyección SQL (Consultas parametrizadas + analizador)

### Riesgos Técnicos Abordados
- [ ] RT-001: Indisponibilidad de base de datos (Manejo de errores + políticas de reintento)
- [ ] RT-002: Falla de entrega de email (Logging + mecanismo de reintento)
- [ ] RT-003: Fallas de servicio de IA (Degradación elegante)
- [ ] RT-004: Errores de despliegue (Feature flags + backups de migración)
- [ ] RT-005: Degradación de rendimiento (Índices + paginación)
- [ ] RT-006: Agotamiento de almacenamiento (Retención de logs + limpieza de backups)

## Checklist de Despliegue (si aplica)

- [ ] Migración de base de datos probada en ambiente de staging
- [ ] Script de backup pre-migración ejecutado
- [ ] Variables de entorno configuradas
- [ ] Feature flags configurados correctamente para el ambiente
- [ ] Procedimiento de rollback documentado
- [ ] Alertas de monitoreo configuradas para nuevas funcionalidades

## Capturas de Pantalla (si aplica)
<!-- Agrega capturas de pantalla para cambios de UI -->

## Notas Adicionales
<!-- Cualquier información adicional que los revisores deban conocer -->

---

**Notas para Revisores:**
- Items del checklist de seguridad marcados como N/A deben incluir justificación
- Todos los PRs relacionados con seguridad requieren aprobación de rol ADMIN
- Migraciones de base de datos requieren verificación de backup antes de merge
- Verificaciones de seguridad fallidas bloquean el merge del PR (no negociable)
