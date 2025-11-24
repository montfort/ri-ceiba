# Research: Sistema de Gestión de Reportes de Incidencias

**Branch**: `001-incident-management-system` | **Date**: 2025-11-18

## 1. PDF Generation Library

**Decision**: QuestPDF

**Rationale**:
- Open-source MIT license, suitable for government projects
- Fluent C# API integrates naturally with .NET ecosystem
- Excellent documentation and active development
- Supports complex layouts needed for report exports
- Better performance than iTextSharp alternatives

**Alternatives Considered**:
- **iTextSharp**: AGPL license complications for closed-source deployment
- **PDFsharp**: Less feature-rich for complex layouts
- **Syncfusion PDF**: Commercial license cost, overkill for this use case

---

## 2. Markdown Processing Library

**Decision**: Markdig

**Rationale**:
- Most popular .NET Markdown parser with extensible pipeline
- Supports CommonMark and additional extensions
- Easy conversion to HTML for Blazor rendering
- Active maintenance and good performance
- Can integrate with Pandoc for Markdown→Word conversion

**Alternatives Considered**:
- **MarkdownSharp**: Older, less maintained
- **CommonMark.NET**: Good but fewer extensions than Markdig

---

## 3. Markdown to Word Conversion

**Decision**: Pandoc (external CLI) via process invocation with fallback

**Rationale**:
- Industry standard for document conversion
- Produces high-quality Word documents from Markdown
- Available in Fedora repositories (easy to include in Docker image)
- More reliable output than pure .NET solutions

**Alternatives Considered**:
- **DocX library**: Requires building Word structure manually, complex
- **OpenXML SDK**: Low-level, significant development effort
- **Aspose.Words**: Commercial license cost

**Risk Mitigation (RT-006)**:
- **Docker Installation**: Include Pandoc in Dockerfile (Fedora: `dnf install pandoc`)
- **Startup Validation**: Check Pandoc availability on application startup, fail fast if missing
- **Version Pinning**: Use specific Pandoc version (e.g., 3.1.x) to avoid breaking changes
- **Fallback Strategy**: If Pandoc fails, generate HTML email body instead of Word attachment
- **Input Validation**: Limit Markdown complexity (no nested tables, external images sanitized)
- **Error Handling**: Detailed logging of Pandoc stderr for debugging conversion failures
- **Timeout**: 10-second timeout on Pandoc process to prevent hanging
- **Future Alternative**: Document migration path to OpenXML SDK if Pandoc becomes problematic
- **Testing**: Integration tests with known-good Markdown samples to detect Pandoc regressions

---

## 4. Email Service Integration

**Decision**: SMTP client with fallback to API services

**Rationale**:
- ASP.NET Core has built-in SMTP support via MailKit
- Configuration-based switching between SMTP and API (SendGrid, Mailgun)
- MailKit is production-tested and actively maintained
- Supports attachments (Word documents) and HTML emails

**Alternatives Considered**:
- **SendGrid SDK only**: Vendor lock-in
- **System.Net.Mail**: Deprecated, less secure

**Risk Mitigation (RO-003 - Falla en Envío de Correos)**:
- **Retry con Exponential Backoff**:
  - Polly retry policy: 3 intentos (inmediato, 1 min, 5 min)
  - Configuración: `WaitAndRetryAsync(new[] { TimeSpan.Zero, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5) })`
- **Fallback a Proveedor Secundario**:
  - Primario: SMTP interno (MailKit)
  - Secundario: SendGrid/Mailgun API (si SMTP falla después de retries)
  - Configuración en appsettings.json con switch automático
- **Cola de Persistencia**:
  - Tabla EMAIL_QUEUE en base de datos para emails fallidos
  - Campos: id, to, subject, body, attachments, attempts, last_error, created_at
  - Background job procesa cola cada 5 minutos
  - Máximo 10 intentos antes de marcar como "failed permanently"
- **Monitoreo en Hangfire Dashboard**:
  - Job para procesar cola EMAIL_QUEUE visible en dashboard
  - Métricas: emails enviados, pendientes, fallidos
  - Filtros por timestamp para troubleshooting
- **Rate Limiting Respetando Cuotas**:
  - SMTP: 100 emails/hora (configurable)
  - SendGrid: según plan contratado (ej: 300/día free tier)
  - Throttling automático si se acerca al límite (90%)
- **Auditoría Completa**:
  - Registro en tabla AUDITORIA de cada intento de envío
  - Campos registrados: timestamp, destinatario, asunto, status (success/failed), error_message
  - Útil para compliance y troubleshooting
- **Alertas Proactivas**:
  - Email a administradores si tasa de fallo >10% en última hora
  - Slack notification si >5 emails en cola pendiente por >30 min
  - Dashboard metric: "Email Delivery Rate" (objetivo: >95%)
- **Circuit Breaker**:
  - Polly circuit breaker para SMTP: abre después de 5 fallos consecutivos
  - Estado abierto por 2 minutos antes de reintentar
  - Durante circuit abierto, usar fallback a proveedor secundario inmediatamente
- **Validación Pre-envío**:
  - Email address validation (regex RFC 5322)
  - Attachment size limit: 10 MB total
  - HTML sanitization para prevenir XSS en email body
- **Configuración Multi-Ambiente**:
  ```json
  {
    "EmailService": {
      "Provider": "SMTP|SendGrid|Mailgun",
      "Primary": { "Host": "smtp.internal.gov", "Port": 587, "UseTLS": true },
      "Secondary": { "ApiKey": "env:SENDGRID_API_KEY" },
      "MaxRetries": 3,
      "QueueProcessingInterval": 5,
      "RateLimitPerHour": 100
    }
  }
  ```

---

## 4b. External Services Resilience Pattern (RO-004 Mitigation)

**Decision**: Unified resilience strategy for all external dependencies (IA, Email, future integrations)

**Rationale**:
- Centralized failure handling improves maintainability
- Consistent behavior across all external service calls
- Reduces duplicate code and configuration

**Risk Mitigation (RO-004 - Pérdida de Conectividad)**:
- **Circuit Breaker Pattern (Polly)**:
  - Configuración unificada para todos los servicios externos
  - Estado CLOSED: operación normal, requests pasan
  - Estado OPEN: después de N fallos consecutivos, rechaza requests inmediatamente sin llamar al servicio
  - Estado HALF-OPEN: permite pruebas periódicas para verificar si el servicio se recuperó
  - Thresholds por servicio:
    - AI Service: 5 fallos consecutivos → OPEN por 2 minutos
    - Email Service: 5 fallos consecutivos → OPEN por 2 minutos
    - Otros: configurables por tipo
- **Graceful Degradation**:
  - AI Service: Generar reporte sin narrativa (solo estadísticas)
  - Email Service: Almacenar en EMAIL_QUEUE para reenvío posterior
  - Mensajes claros al usuario sobre funcionalidad degradada
  - No bloquear operaciones críticas (ej: creación de reportes)
- **Timeouts Agresivos**:
  - AI Service: 30 segundos (configurable)
  - Email SMTP: 15 segundos
  - Base de datos: 30 segundos
  - Evita threads colgados esperando servicios no disponibles
- **Health Checks Activos**:
  - Endpoint `/health` con health checks de servicios externos
  - Ejecución cada 5 minutos en background
  - Métricas expuestas:
    - AI Service: status (healthy/unhealthy), last_success, consecutive_failures
    - Email Service: status, emails_pending_in_queue, circuit_state
    - Database: status, connection_count, query_latency_p95
  - Dashboard en `/health-ui` (solo ADMIN)
- **Monitoring y Observabilidad**:
  - Logging estructurado de todas las llamadas externas con resultado
  - Métricas Prometheus:
    - `external_service_call_duration_seconds` (histogram)
    - `external_service_call_total` (counter con labels: service, status)
    - `circuit_breaker_state` (gauge: 0=closed, 1=open, 2=half-open)
  - Alertas configuradas:
    - Circuit breaker abierto por >10 minutos → Critical
    - Latencia p95 >threshold → Warning
- **Uptime Tracking**:
  - Registro histórico de disponibilidad de servicios externos
  - Tabla SERVICE_HEALTH_LOG con: timestamp, service, status, latency_ms, error_message
  - Reportes mensuales de SLA de servicios externos
  - Útil para evaluar proveedores y planificar redundancia
- **Retry con Límites**:
  - Reintentos solo para errores transitorios (network, timeout)
  - NO reintentar errores lógicos (400 Bad Request, 401 Unauthorized)
  - Jitter en retry delays para evitar thundering herd
- **User Feedback Claro**:
  - Mensajes descriptivos cuando funcionalidad degradada activa
  - Evitar errores genéricos ("Error 500")
  - Ejemplo: "El servicio de IA no está disponible temporalmente. El reporte se generó sin narrativa automática."
- **Manual Override**:
  - Admin puede deshabilitar manualmente servicios externos desde configuración
  - Útil para mantenimiento planificado o testing
  - Feature flags: `EnableAIService`, `EnableEmailService`

**Configuration Example**:
```json
{
  "Resilience": {
    "CircuitBreaker": {
      "FailureThreshold": 5,
      "DurationOfBreakSeconds": 120,
      "SamplingDurationSeconds": 60
    },
    "Retry": {
      "MaxRetries": 3,
      "BaseDelaySeconds": 1,
      "UseJitter": true
    },
    "Timeout": {
      "AIService": 30,
      "EmailService": 15,
      "Database": 30
    },
    "HealthCheck": {
      "IntervalSeconds": 300,
      "TimeoutSeconds": 10
    }
  }
}
```

---

## 5. AI Text Generation Integration

**Decision**: HTTP client abstraction with OpenAI-compatible API

**Rationale**:
- OpenAI API is de facto standard, many compatible providers
- Simple REST interface, easy to mock for testing
- Can swap providers (Azure OpenAI, Anthropic, local LLM) without code changes
- Timeout and retry policies via Polly for resilience

**Alternatives Considered**:
- **Semantic Kernel**: Overkill for simple text generation
- **LangChain.NET**: Less mature in .NET ecosystem

**Risk Mitigation (RT-001)**:
- **Timeout**: Aggressive 30-second timeout on all AI calls to prevent hanging
- **Fallback**: Graceful degradation - generate report with statistics only if AI fails
- **Retry Policy**: Polly circuit breaker (open after 5 consecutive failures for 2 minutes)
- **Caching**: Response cache for identical prompts (daily reports have similar patterns)
- **Mock Service**: AIServiceMock for development/testing with deterministic responses
- **Monitoring**: Log all AI calls with latency, token count, success/failure for observability
- **Configuration**: Support multiple providers via appsettings.json:
  ```json
  {
    "AIService": {
      "Provider": "OpenAI|AzureOpenAI|LocalLLM",
      "Endpoint": "https://api.openai.com/v1",
      "ApiKey": "env:AI_API_KEY",
      "Model": "gpt-4o-mini",
      "Timeout": 30000,
      "EnableFallback": true,
      "EnableCache": true
    }
  }
  ```

---

## 6. Background Job Scheduling

**Decision**: Hangfire with PostgreSQL storage

**Rationale**:
- Production-ready job scheduler for .NET
- Dashboard for monitoring jobs (useful for ADMIN)
- PostgreSQL storage keeps all data in single database
- Supports recurring jobs (daily report generation at 6:00 AM)
- Retry policies and failure handling built-in

**Alternatives Considered**:
- **Quartz.NET**: More complex configuration
- **Built-in BackgroundService**: No persistence, lost on restart
- **Azure Functions**: Requires cloud dependency

---

## 7. Cascading Dropdown Implementation

**Decision**: Blazor component with async data loading

**Rationale**:
- Native Blazor approach, no JavaScript dependencies
- Server-side rendering keeps data secure
- EF Core eager loading for Zona→Sector→Cuadrante hierarchy
- Cached catalog data for performance

**Implementation Pattern**:
```csharp
// OnZonaChanged → Load Sectors for selected Zona
// OnSectorChanged → Load Cuadrantes for selected Sector
// Clear dependent selections on parent change
```

---

## 8. Audit Logging Pattern

**Decision**: EF Core interceptors + domain events

**Rationale**:
- Interceptors capture all database changes automatically
- Domain events for business-level audit (login, report submission)
- Separates audit concerns from business logic
- Supports both entity changes and action codes

**Implementation Pattern**:
- `SaveChangesInterceptor` for automatic entity tracking
- `IAuditService` for explicit business event logging
- Async write to avoid blocking main operations

---

## 9. Authentication and Session Management

**Decision**: ASP.NET Identity with cookie authentication

**Rationale**:
- Built-in with ASP.NET Core, well-tested
- Cookie auth works well with Blazor Server
- Supports role-based authorization out of the box
- Easy to configure session timeout (30 min sliding)
- Password hashing with secure defaults

**Configuration**:
- Session timeout: 30 minutes sliding expiration
- Password policy: 10+ chars, uppercase + number required
- Account lockout after 5 failed attempts (30 min lockout duration)

**Risk Mitigation (RS-004 - Brute Force Attacks)**:
- **Account Lockout**: Automatic lockout after 5 failed login attempts for 30 minutes
- **CAPTCHA Integration**: reCAPTCHA v3 after 3 failed attempts (invisible challenge)
- **Rate Limiting**: IP-based rate limiting (10 login attempts per minute)
- **Progressive Delays**: Exponential backoff delays (1s, 2s, 4s, 8s) between failed attempts
- **Audit Logging**: All failed login attempts logged with IP, timestamp, username
- **Attack Detection**: Alerts triggered for >50 failed attempts/hour from single IP
- **IP Blocking**: Temporary IP ban (1 hour) after 20 failed attempts
- **Password Spray Protection**: Detection of same password across multiple accounts
- **User Notification**: Email alert to user after account lockout
- **Admin Dashboard**: Real-time monitoring of failed login patterns

**Risk Mitigation (RS-005 - Session Hijacking)**:
- **HTTPS Enforcement**: Strict HTTPS in production with HSTS headers (max-age=31536000, includeSubDomains, preload)
- **Secure Cookies**: Cookie flags set to `Secure`, `HttpOnly`, `SameSite=Strict`
- **Session Timeout**: Aggressive 30-minute inactivity timeout with sliding expiration
- **Session ID Regeneration**: New session ID generated after successful login
- **User-Agent Validation**: Session bound to User-Agent header (detect suspicious changes)
- **IP Validation**: Optional IP binding for high-security scenarios (configurable)
- **Session Invalidation**: Logout on all devices if suspicious activity detected
- **Audit Session Changes**: All session creation/destruction logged
- **Anti-CSRF Tokens**: ASP.NET Core AntiForgery tokens on all state-changing operations
- **SameSite Cookies**: Prevent CSRF attacks via SameSite=Strict policy

**Risk Mitigation (RS-001 - RBAC)**:
- **Authorization Attributes**: `[Authorize(Roles = "...")]` on ALL pages, components, and API endpoints
- **Deny-by-Default**: No access granted unless explicitly authorized
- **Test Matrix**: Comprehensive authorization tests (User Role × Functionality = Expected Access)
- **Double Verification**: UI-level authorization + API-level authorization (defense in depth)
- **Claims-Based**: Use ASP.NET Identity Claims for granular permissions beyond roles
- **Code Review Checklist**: Security-focused review on every PR touching authorization
- **Automated Security Testing**: OWASP ZAP integrated in CI/CD pipeline
- **Authorization Middleware**: Custom middleware to log unauthorized access attempts
- **Test Coverage Requirement**: 100% of endpoints must have authorization tests
- **Separation of Concerns**: Authorization logic centralized in policy-based handlers

**Risk Mitigation (RS-002 - SQL Injection & XSS)**:
- **ORM Exclusive**: ONLY Entity Framework Core with LINQ queries (ZERO raw SQL)
- **Input Validation**: FluentValidation on ALL user inputs with whitelist approach
- **Output Encoding**: Blazor automatic HTML encoding via `@bind` directive
- **Content Security Policy**: Strict CSP headers preventing inline scripts
- **Parameterized Queries**: EF Core ensures all queries are parameterized
- **SQL Injection Scanner**: SonarQube + Snyk in CI/CD detecting vulnerable patterns
- **XSS Prevention**: HtmlEncoder.Default for any user-generated content rendering
- **Validation Rules**: Regex patterns for structured fields (email, phone, IDs)
- **Length Limits**: Maximum field lengths enforced at validation layer
- **Code Analysis**: Static analysis tools (Roslyn analyzers) for SQL concatenation detection

**Risk Mitigation (RS-003 - Data Exposure in Logs)**:
- **Credential Exclusion**: NEVER log passwords, tokens, API keys, or credentials (Serilog destructuring policies)
- **PII Redaction**: Automatic redaction of personal data (email, phone, names) using regex patterns
- **Structured Logging**: Serilog with structured logs (no string interpolation exposing sensitive data)
- **Log Separation**: Application logs (technical, 30-day retention) vs Audit logs (business, indefinite retention)
- **Encryption at Rest**: Application logs encrypted on disk
- **Access Control**: Logs accessible only to ADMIN + DevOps roles
- **Sensitive Data Markers**: Use `{@SensitiveData:l}` destructuring to automatically redact
- **Log Review**: Automated scripts to scan logs for potential data exposure patterns
- **Environment Segregation**: Production logs isolated from development environments
- **Audit Trail**: Who accessed logs (meta-logging for compliance)

---

## 10. Data Seeding Strategy

**Decision**: EF Core migrations with seed data

**Rationale**:
- Initial Zonas, Sectores, Cuadrantes from provided lists
- Default suggestion lists (Sexo, Delito, TipoDeAtencion)
- Initial ADMIN user for first-time setup
- Idempotent seeding (safe to re-run)

---

## 11. Report Export Strategy

**Decision**: Server-side generation with file download

**Rationale**:
- PDF: QuestPDF generates in memory, returns FileResult
- JSON: System.Text.Json serialization
- Batch export: Zip archive for multiple reports
- Progress indicator for large exports (Blazor SignalR)

**Risk Mitigation (RT-003)**:
- **Synchronous Export Limits**:
  - PDF: Maximum 50 reports per direct request (2-3 seconds generation time)
  - JSON: Maximum 100 reports per direct request (<1 second generation time)
  - Over-limit requests rejected with clear error message suggesting background job
- **Background Job Processing**:
  - Exports >50 reports queued via Hangfire with email notification on completion
  - Job timeout: 2 minutes maximum per export job
  - Download link valid for 24 hours
- **Memory Management**:
  - Stream PDF directly to response (no full in-memory buffering)
  - Generate PDFs individually and zip incrementally for batch exports
  - Dispose QuestPDF document objects immediately after generation
- **Concurrency Control**:
  - Hangfire configured with max 3 concurrent export jobs
  - Rate limiting: 5 export requests per user per minute
- **Monitoring**:
  - Log export size (report count, file size, generation time)
  - Alert on exports taking >30 seconds or using >500 MB memory

---

## 12. .NET ASPIRE for Development

**Decision**: Use .NET ASPIRE for local development orchestration

**Rationale**:
- Simplifies running PostgreSQL, app, and dependencies locally
- Service discovery and configuration management
- Dashboard for monitoring during development
- Easy transition to Docker Compose for production

---

## 13. Docker Deployment Strategy

**Decision**: Multi-stage Dockerfile with Docker Compose

**Rationale**:
- Multi-stage build for smaller production images
- Docker Compose for orchestrating app + PostgreSQL
- Volume mounts for persistent data and backups
- Environment-based configuration (dev/prod)

**Container Structure**:
- `ceiba-web`: ASP.NET Core application
- `ceiba-db`: PostgreSQL 18
- Shared network for internal communication
- Exposed ports: 80/443 for web, 5432 for DB (internal only in prod)

**Risk Mitigation (RO-002 - Agotamiento de Recursos)**:
- **Resource Limits en Docker**:
  - ceiba-web: `--cpus="2.0" --memory="4g" --memory-swap="4g"`
  - ceiba-db: `--cpus="2.0" --memory="4g" --memory-reservation="2g"`
  - Previene que un contenedor consuma todos los recursos del host
- **Database Connection Pooling**:
  - MaxPoolSize: 20 conexiones simultáneas
  - MinPoolSize: 5 conexiones mínimas
  - ConnectionTimeout: 30 segundos
  - Comando de diagnóstico: `SELECT count(*) FROM pg_stat_activity;`
- **Rate Limiting en Endpoints Costosos**:
  - AspNetCoreRateLimit middleware configurado
  - Exportaciones: 5 requests/min por usuario
  - Búsquedas complejas: 10 requests/min por usuario
  - Generación de reportes AI: 3 requests/min por usuario
- **Background Job Concurrency**:
  - Hangfire WorkerCount: máximo 3 jobs simultáneos
  - Queue priority: backups (high), exports (normal), emails (low)
  - Prevent resource starvation on critical jobs
- **Cache Strategy**:
  - IMemoryCache para catálogos estáticos (Zonas, Sectores, Cuadrantes) - TTL 1 hora
  - Distributed cache (Redis opcional) para escalabilidad horizontal futura
  - Cache eviction policy: LRU (Least Recently Used)
- **Monitoring y Alertas**:
  - Prometheus exporters: CPU, RAM, Disk I/O, Network
  - Grafana dashboards con thresholds configurables
  - Alertas automáticas:
    - CPU >70% por 5 minutos → Warning
    - CPU >85% por 2 minutos → Critical
    - RAM >75% → Warning
    - RAM >90% → Critical
    - Disk >80% → Warning
  - Health checks endpoint: /health con checks de DB, disk, memory
- **Graceful Degradation**:
  - Circuit breaker en servicios externos (IA, email)
  - Fallback a modo degradado si recursos escasos
  - Queue overflow handling: rechazar requests con HTTP 503 Service Unavailable
- **Auto-Scaling Preparado**:
  - Diseño stateless permite horizontal scaling
  - Session state en Redis (no in-memory) para multi-instance
  - Configuración lista para Kubernetes/Docker Swarm (futuro)

---

## 14. Backup Strategy

**Decision**: pg_dump daily to secondary storage

**Rationale**:
- PostgreSQL native backup tool, reliable
- Cron job (or Hangfire) for scheduled execution
- Compress backups (gzip) for storage efficiency
- Rotate: keep 7 daily, 4 weekly, 12 monthly
- Secondary disk mount for backup storage

**Recovery**: pg_restore with documented procedure in quickstart.md

**Risk Mitigation (RO-001 - Falla en Respaldos)**:
- **Monitoreo Activo**: Hangfire job con alertas email/Slack en fallo (timeout 15 min)
- **Validación Automática**: pg_restore --list post-backup para verificar integridad
- **Backups Incrementales**: pg_basebackup cada 6 horas (8 AM - 8 PM) para minimizar pérdida de datos
- **Pruebas de Restauración**: Automatizadas mensualmente en ambiente staging con validación de data
- **Almacenamiento Redundante**:
  - Primario: Disco secundario montado en /mnt/backups
  - Secundario: Copia offsite (S3/NAS) para disaster recovery
- **Logging Detallado**: Todos los intentos de backup registrados con tamaño, duración, éxito/fallo
- **Alertas Proactivas**:
  - Email inmediato si backup falla
  - Alerta si último backup válido >24 hrs
  - Warning si tamaño de backup varía >20% vs promedio (posible corrupción)
- **Runbook de Recuperación**: Documentado en docs/runbooks/disaster-recovery.md con:
  - Procedimiento paso a paso de restauración
  - Tiempos estimados de recuperación (RTO: <1 hora, RPO: <6 horas)
  - Contactos de escalación
  - Scripts de validación post-restore
- **Retención Inteligente**:
  - Diarios: 7 días (último backup cada día)
  - Semanales: 4 semanas (backup del domingo)
  - Mensuales: 12 meses (backup del primer día del mes)
  - Anual: 3 años (backup del 1 de enero)
- **Métricas**:
  - Tiempo promedio de backup (baseline para detectar anomalías)
  - Tasa de éxito de backups (objetivo: 100%)
  - Tamaño de backup (trending para capacity planning)

---

## 15. Full-Text Search Strategy

**Decision**: PostgreSQL native full-text search with GIN indexes

**Rationale**:
- PostgreSQL FTS built-in, no external dependencies (Elasticsearch, etc.)
- Spanish language support with stemming and stop words
- GIN indexes provide fast search on TEXT columns
- Simpler deployment and maintenance than external search engines
- Sufficient for expected volume (thousands of reports)

**Implementation**:
- Create tsvector indexes on hechos_reportados and acciones_realizadas
- Use 'spanish' configuration for language-specific stemming
- Search with tsquery for phrase and boolean queries
- Combine FTS with traditional filters (zona, fecha, estado)

**Risk Mitigation (RT-002)**:
- **Composite Indexes**: Create multi-column indexes for common filter combinations (estado+zona+fecha)
- **Pagination**: Enforce 500 records/page maximum to prevent large result sets
- **Caching**: IMemoryCache with 5-minute TTL for identical search queries (cache key = filter hash)
- **Query Optimization**: EXPLAIN ANALYZE validation in integration tests to verify index usage
- **Monitoring**: Log slow queries (>3 seconds) for analysis and optimization

**Alternatives Considered**:
- **Elasticsearch**: Overkill for expected volume, additional infrastructure complexity
- **Azure Cognitive Search**: Cloud dependency, cost concerns
- **LIKE queries**: Poor performance on large datasets, no stemming

---

## 16. Testing Strategy

**Decision**: xUnit + bUnit + TestContainers + Playwright

**Rationale**:
- xUnit: Standard .NET testing framework
- bUnit: Blazor component testing
- TestContainers: Real PostgreSQL for integration tests
- FluentAssertions: Readable test assertions
- Moq: Service mocking for unit tests
- Playwright: Cross-browser E2E testing (RT-005)

**Test Categories**:
- Unit: Business logic in isolation
- Integration: Database and external services
- Component: Blazor UI components
- Contract: API endpoint validation
- E2E: Full user flows across browsers

**Risk Mitigation (RT-005)**:
- **Browser Matrix**: Automated tests on Chrome, Firefox, Edge, Safari (latest 2 versions)
- **Responsive Testing**: Viewports tested: 320px (mobile), 768px (tablet), 1024px (desktop), 1920px (HD)
- **Playwright Configuration**:
  ```json
  {
    "browsers": ["chromium", "firefox", "webkit"],
    "viewport": { "width": 1280, "height": 720 },
    "mobileViewports": ["iPhone 13", "iPad", "Pixel 5"]
  }
  ```
- **CI/CD Integration**: Playwright tests run on every PR, blocking merge if failures
- **Visual Regression**: Screenshot comparisons for critical pages (login, report form, dashboard)
- **Accessibility Testing**: Automated a11y checks with axe-core in Playwright tests
- **Manual Testing**: Weekly manual tests on real iOS/Android devices (not emulators)
- **CSS Framework**: Bootstrap 5 or Tailwind CSS for proven cross-browser compatibility
- **Polyfills**: Blazor Server includes necessary polyfills for SignalR in older browsers


---

## 17. Dependency Management and Updates

**Decision**: Conservative LTS-first update policy with automated vulnerability scanning

**Rationale**:
- Stability over bleeding-edge features in government production system
- Automated security scanning catches critical vulnerabilities early
- Staging environment allows safe testing before production deployment
- Documented process reduces risk of breaking changes

**Risk Mitigation (RO-005 - Migración y Actualización de Dependencias)**:
- **LTS-First Policy**:
  - .NET: Only upgrade to LTS versions (.NET 8 LTS current, .NET 10 evaluation only)
  - PostgreSQL: Stick to major versions with 5+ year support (PostgreSQL 16/17/18)
  - NuGet packages: Prefer stable releases over pre-release/beta
  - Exceptions require Tech Lead approval with documented justification
- **Automated Vulnerability Scanning**:
  - Dependabot configured in GitHub repository
  - Snyk CLI integrated in CI/CD pipeline (fail build on critical vulnerabilities)
  - Weekly automated PR for dependency updates (review required before merge)
  - OWASP Dependency-Check in build process
  - Vulnerabilities categorized: Critical (patch within 24h), High (7 days), Medium (30 days)
- **Staging Environment Testing**:
  - Identical to production configuration (Docker, PostgreSQL version, resource limits)
  - Full test suite runs on staging before production deploy
  - Manual smoke tests on critical user flows (login, report creation, export)
  - Minimum 48h soak time in staging for major updates
- **Change Documentation**:
  - CHANGELOG.md in repository root with semantic versioning
  - Breaking changes documented in MIGRATIONS.md with upgrade steps
  - Dependencies tracked in docs/dependencies.md with version history
  - Release notes reference CVE IDs for security patches
- **Rollback Planning**:
  - VM/container snapshots before major upgrades
  - Database backup immediately before schema migrations
  - Documented rollback procedure in docs/runbooks/rollback.md
  - Maximum rollback time target: 15 minutes to previous version
- **Update Windows**:
  - Scheduled monthly maintenance window: First Sunday 2:00 AM - 6:00 AM
  - Emergency security patches: Any time with stakeholder notification
  - No updates during critical periods (end-of-month reporting, audits)
- **Compatibility Testing Matrix**:
  - Test combinations: .NET version × PostgreSQL version × OS version
  - Integration tests verify external service compatibility (AI, email APIs)
  - Performance regression tests: ensure no degradation >10%
- **Deprecation Handling**:
  - Monitor deprecation notices from .NET/NuGet/PostgreSQL
  - Plan migration 6 months before EOL (End of Life)
  - Test deprecated feature replacements in staging first
  - Document migration path for future maintainers
- **Dependency Pinning**:
  - Docker base images: Pin to specific SHA256 hash (not `latest` tag)
  - NuGet packages: Use exact version numbers in .csproj, not wildcards
  - PostgreSQL client libs: Pin compatible with server version
  - Allows reproducible builds across environments
- **Update Checklist**:
  1. Review release notes for breaking changes
  2. Update staging environment
  3. Run full test suite (unit, integration, E2E)
  4. Manual smoke tests on staging
  5. Update CHANGELOG.md and docs
  6. Create rollback plan
  7. Schedule production deployment in maintenance window
  8. Monitor for 24h post-deployment
  9. Document any issues in runbook
- **Team Communication**:
  - Slack notification 48h before scheduled maintenance
  - Email to stakeholders for major version upgrades
  - Post-deployment summary with changes and known issues

**Monitoring Post-Update**:
- Error rate monitoring (compare pre/post deploy)
- Performance metrics (latency p50/p95/p99)
- Resource usage (CPU, RAM trending)
- User-reported issues tracking
- Rollback decision within 2 hours if critical issues detected

**Configuration Example** (Dependabot):
```yaml
version: 2
updates:
  - package-ecosystem: "nuget"
    directory: "/"
    schedule:
      interval: "weekly"
      day: "monday"
    open-pull-requests-limit: 10
    reviewers:
      - "tech-lead"
    labels:
      - "dependencies"
      - "automated"
    ignore:
      - dependency-name: "*"
        update-types: ["version-update:semver-major"]  # Require manual review
```

---

## 18. User Experience & Adoption Strategy

**Purpose**: Minimize user adoption resistance and ensure system usability for non-technical police agents.

**Context**: RN-001 Mitigation - Addressing the risk of low adoption due to interface complexity. Target users (CREADOR role) may have limited technical skills and prefer paper-based workflows. The system must be intuitive, fast, and provide immediate value to overcome resistance.

**Risk Mitigation for RN-001**: Baja Adopción por Complejidad de Interfaz

### Strategy Components

**1. Interactive Onboarding System**:
- First-login guided tour using Blazor components (e.g., Blazorise Tour or custom)
- Step-by-step walkthrough of key features: create report, save draft, submit
- Optional skip with "Show me later" option
- Tour completion tracked in USUARIO table (onboarding_completed flag)
- Tour can be reactivated from help menu
- Mobile-responsive design (works on tablets/phones)

**2. Contextual Help & Tooltips**:
- Tooltip on hover/focus for complex fields (e.g., "Características poblacionales")
- Help icon (?) next to field labels with detailed explanations
- Examples embedded in tooltips (e.g., "Ejemplo: Mujer de 35 años, condición de movilidad reducida")
- Glossary of terms accessible from navigation menu
- Field-level validation messages in plain language (no technical jargon)

**3. Real-Time Validation & Feedback**:
- FluentValidation messages displayed immediately on blur
- Green checkmarks for valid fields, red highlights for errors
- Progress indicator showing form completion percentage
- Autosave drafts every 30 seconds (visual indicator)
- Clear error summaries at top of form before submit

**4. User Documentation**:
- User manual in docs/user-manual.md (Markdown → HTML/PDF)
- Screenshot-based guides for each major workflow
- Searchable FAQ section
- Video tutorials <2 minutes each (screen recordings with narration)
- Embedded videos in help panel (YouTube/Vimeo integration)
- Downloadable PDF quick reference card (1-page)

**5. Training Program**:
- Initial training sessions (presencial/remoto) at launch
- Training materials: slides, handouts, checklists
- Hands-on practice environment (staging with dummy data)
- Train-the-trainer program for power users (champions)
- Monthly refresher webinars for new features
- Training attendance tracked for compliance

**6. Usability Testing Protocol**:
- Pre-launch UAT with 3-5 real users (CREADOR role)
- Task-based testing: "Complete a report from scratch"
- System Usability Scale (SUS) questionnaire (target: >68)
- Observation notes: confusion points, task completion time
- Iterative improvements based on feedback before GA (General Availability)
- Post-launch usability testing quarterly

**7. Feedback Loop**:
- In-app feedback form accessible from navigation
- Anonymous option to encourage honest feedback
- Feedback stored in FEEDBACK table (user_id, timestamp, rating, comments)
- Weekly review of feedback by Product Owner
- Public roadmap showing requested features and status
- Quarterly "You asked, we delivered" communication

**8. Performance Optimization**:
- Page load time <2 seconds (measured at p95)
- Blazor Server SignalR latency <500ms
- Minimize JavaScript bundle size
- Lazy loading for non-critical components
- Image optimization (WebP format, lazy loading)
- Browser caching for static assets (CSS, JS, images)

**9. Net Promoter Score (NPS) Tracking**:
- Quarterly NPS survey (1-10 rating + open comment)
- Question: "¿Qué tan probable es que recomiende este sistema a un colega?"
- NPS calculation: % Promoters (9-10) - % Detractors (0-6)
- Target NPS: >40 (good), >70 (excellent)
- Trend tracking over time in analytics dashboard
- Action items from detractor feedback

**10. Adoption Metrics Dashboard (ADMIN)**:
- Active users (daily, weekly, monthly)
- Reports created per user (avg, median)
- Time to complete first report (avg)
- Feature usage heatmap
- Dropout points in workflows (funnel analysis)
- Support tickets by category
- Training completion rate

### Implementation Details

**Onboarding Tour Example** (Blazor Component):
```csharp
// Components/OnboardingTour.razor
@if (!UserProfile.OnboardingCompleted)
{
    <Tour Steps="@tourSteps" OnComplete="@HandleTourComplete" />
}

@code {
    List<TourStep> tourSteps = new()
    {
        new("Bienvenido", "Este sistema permite reportar incidencias de forma digital", "#main-nav"),
        new("Crear Reporte", "Haz clic aquí para crear un nuevo reporte", "#btn-new-report"),
        new("Guardar Borrador", "Puedes guardar y continuar después", "#btn-save-draft"),
        new("Entregar Reporte", "Cuando termines, haz clic en Entregar", "#btn-submit")
    };

    async Task HandleTourComplete()
    {
        await UserService.MarkOnboardingComplete(CurrentUser.Id);
        StateHasChanged();
    }
}
```

**Tooltip Implementation**:
```razor
<FormField Label="Características Poblacionales"
           Tooltip="Describe condiciones especiales de la persona (edad, género, discapacidad, etc.)"
           HelpExample="Ejemplo: Mujer de 35 años, movilidad reducida">
    <InputText @bind-Value="reporte.CaracteristicasPoblacionales" />
</FormField>
```

**Feedback Form**:
```csharp
// Components/FeedbackModal.razor
<Modal @bind-Visible="showFeedback">
    <ModalHeader>Enviar Comentario</ModalHeader>
    <ModalBody>
        <Field>
            <FieldLabel>¿Qué tan satisfecho estás con el sistema? (1-5)</FieldLabel>
            <Rating @bind-Value="feedback.Rating" MaxValue="5" />
        </Field>
        <Field>
            <FieldLabel>Comentarios (opcional)</FieldLabel>
            <MemoEdit @bind-Text="feedback.Comments" Rows="4" />
        </Field>
    </ModalBody>
    <ModalFooter>
        <Button Color="Color.Primary" Clicked="@SubmitFeedback">Enviar</Button>
    </ModalFooter>
</Modal>
```

**NPS Calculation**:
```csharp
public class NPSService
{
    public async Task<NPSResult> CalculateNPS(DateTime startDate, DateTime endDate)
    {
        var responses = await _db.NPSResponses
            .Where(r => r.Date >= startDate && r.Date <= endDate)
            .ToListAsync();

        var promoters = responses.Count(r => r.Score >= 9);
        var detractors = responses.Count(r => r.Score <= 6);
        var total = responses.Count;

        var nps = total > 0 ? ((promoters - detractors) * 100.0 / total) : 0;

        return new NPSResult
        {
            Score = nps,
            Promoters = promoters,
            Passives = responses.Count(r => r.Score is 7 or 8),
            Detractors = detractors,
            TotalResponses = total
        };
    }
}
```

**Adoption Metrics Dashboard**:
```csharp
public class AdoptionMetrics
{
    public int ActiveUsersToday { get; set; }
    public int ActiveUsersThisWeek { get; set; }
    public int ActiveUsersThisMonth { get; set; }
    public double AvgReportsPerUser { get; set; }
    public TimeSpan AvgTimeToCompleteReport { get; set; }
    public Dictionary<string, int> FeatureUsage { get; set; }
    public double NPSScore { get; set; }
    public int TrainingCompletionRate { get; set; } // Percentage
}
```

### Validation & Success Metrics

**Usability Metrics**:
- System Usability Scale (SUS) score >68 (above average)
- Task completion rate >90% in usability testing
- Time to complete first report <5 minutes (p90)
- Page load time <2 seconds (p95)

**Adoption Metrics**:
- User activation rate >80% (logged in at least once in first week)
- Weekly active users >70% of total users
- Reports created per user >3 per week (for CREADOR)
- NPS score >40 (good) within 3 months post-launch

**Support Metrics**:
- Support tickets related to usability <10 per month
- Training completion rate >85% of new users
- Feedback response rate >60% (users submitting feedback)

**Continuous Improvement**:
- Quarterly usability reviews with user feedback integration
- A/B testing for UI improvements (if feasible)
- Heatmap analysis to identify unused features
- Iterative simplification based on actual usage patterns

### Database Schema Additions

**USUARIO table extensions**:
```sql
ALTER TABLE USUARIO ADD COLUMN onboarding_completed BOOLEAN DEFAULT FALSE;
ALTER TABLE USUARIO ADD COLUMN first_login_date TIMESTAMPTZ;
ALTER TABLE USUARIO ADD COLUMN last_active_date TIMESTAMPTZ;
```

**FEEDBACK table** (new):
```sql
CREATE TABLE FEEDBACK (
    feedback_id SERIAL PRIMARY KEY,
    usuario_FK INTEGER REFERENCES USUARIO(usuario_id),
    rating INTEGER CHECK (rating BETWEEN 1 AND 5),
    comments TEXT,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    feedback_type VARCHAR(50) DEFAULT 'general', -- general, bug, feature_request
    is_anonymous BOOLEAN DEFAULT FALSE
);
```

**NPS_RESPONSE table** (new):
```sql
CREATE TABLE NPS_RESPONSE (
    response_id SERIAL PRIMARY KEY,
    usuario_FK INTEGER REFERENCES USUARIO(usuario_id),
    score INTEGER CHECK (score BETWEEN 0 AND 10),
    comment TEXT,
    survey_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    survey_period VARCHAR(20) -- e.g., '2025-Q1'
);
```

**TRAINING_COMPLETION table** (new):
```sql
CREATE TABLE TRAINING_COMPLETION (
    training_id SERIAL PRIMARY KEY,
    usuario_FK INTEGER REFERENCES USUARIO(usuario_id),
    training_module VARCHAR(100), -- e.g., 'basic_navigation', 'report_creation'
    completed_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    certificate_issued BOOLEAN DEFAULT FALSE
);
```

### Integration Points

- **Onboarding Tour**: Integrated in `App.razor` or `MainLayout.razor`, triggered on first login
- **Tooltips**: Reusable `<FormField>` component wrapping all form inputs
- **Feedback Form**: Accessible from main navigation or floating help button
- **NPS Survey**: Triggered quarterly via background job, sent via email or in-app notification
- **Metrics Dashboard**: Admin module, accessible only to ADMIN role
- **User Manual**: Served as static HTML from `/docs/user-manual` route
- **Video Tutorials**: Embedded in help panel using `<iframe>` or `<video>` tags

### Testing Strategy

**Pre-Launch UAT**:
1. Recruit 3-5 users from target audience (police agents - CREADOR role)
2. Provide task scenarios: "Create a report for an incident you attended yesterday"
3. Observe without intervention, note confusion points
4. Administer SUS questionnaire post-task
5. Conduct 15-min interview about pain points
6. Iterate UI based on findings before general availability

**Post-Launch Monitoring**:
- Weekly review of adoption metrics dashboard
- Monthly NPS calculation and trend analysis
- Quarterly usability testing with new users
- Annual comprehensive UX audit with external consultant (if budget allows)

**Continuous Feedback Integration**:
- Product backlog items created from high-frequency feedback
- Quick wins prioritized (simple UI improvements)
- Major changes go through change request process (RP-001 mitigation)

---

## 19. Institutional Change Management Strategy

**Purpose**: Adapt to evolving institutional requirements and regulatory changes without system disruption.

**Context**: RN-002 Mitigation - Addressing the risk of institutional requirement changes that could invalidate design decisions. SSC policies, procedures, and reporting standards may evolve, requiring system flexibility.

**Risk Mitigation for RN-002**: Cambios en Requisitos Institucionales

### Strategy Components

**1. Modular Architecture**:
- Self-contained modules with well-defined boundaries
- Module-level versioning for independent evolution
- Public API contracts between modules (IReportService, IUserService, etc.)
- No cross-module database access (only through service layer)
- Feature toggles for gradual rollout of changes

**2. Configurable Business Rules**:
- Configuration table for business parameters (SYSTEM_CONFIG)
- Admin UI for changing rules without code deployment
- Examples: age ranges, priority thresholds, escalation criteria
- Version history for configuration changes (audit trail)
- Rollback capability for configuration changes

**3. Extensible Data Model**:
- `campos_adicionales` JSONB field in REPORTE_INCIDENCIA (already implemented in RT-004)
- `schema_version` field for backward compatibility
- New report types (Tipo B, C) addable without schema migration
- Metadata-driven forms (form definition in database, not hardcoded)
- Flexible validation rules configurable per report type

**4. Feature Flags System**:
- Toggle features on/off without redeployment
- A/B testing for new features with subset of users
- Gradual rollout (10% → 50% → 100%)
- Emergency kill switch for problematic features
- User-level and role-level flags

**5. Stakeholder Communication Protocol**:
- Monthly stakeholder review meetings
- Quarterly roadmap planning sessions
- Change request process with impact analysis
- RFC (Request for Comments) document for major changes
- Stakeholder approval workflow for breaking changes

**6. Change Request Management**:
- Formal change request template (docs/templates/change-request.md)
- Impact assessment: scope, timeline, resources, risks
- Prioritization framework: MoSCoW (Must, Should, Could, Won't)
- Change advisory board (CAB) for approval (PM, Tech Lead, PO, Stakeholder rep)
- Tracking in project management tool (GitHub Projects, Jira, etc.)

**7. Requirement Versioning**:
- spec.md maintained as living document
- Major versions for breaking changes (v1.0, v2.0)
- Minor versions for additive changes (v1.1, v1.2)
- Change log in spec.md with rationale
- Previous versions archived in git history

**8. Sprint Capacity Reservation**:
- 20% of sprint capacity reserved for emergent changes
- Buffer for institutional change requests
- Quick response capability for regulatory compliance
- Prevents sprint derailment from unexpected requirements

**9. Form Versioning**:
- Multiple report form versions coexist (legacy + current)
- Reports stamped with form version used at creation
- UI adapts based on form version when viewing old reports
- Gradual migration path for users (not forced upgrades)
- Backward compatibility maintained for 2 major versions

**10. Regulatory Compliance Tracking**:
- Document mapping: requirement ID → regulation/policy reference
- Compliance matrix in docs/compliance-matrix.md
- Automated alerts for policy update announcements
- Annual compliance audit with legal/compliance team
- Traceability from code → requirement → regulation

### Implementation Details

**SYSTEM_CONFIG Table**:
```sql
CREATE TABLE SYSTEM_CONFIG (
    config_id SERIAL PRIMARY KEY,
    config_key VARCHAR(100) UNIQUE NOT NULL, -- e.g., 'report.max_age_months'
    config_value TEXT NOT NULL,              -- JSON for complex values
    config_type VARCHAR(20) NOT NULL,        -- 'string', 'number', 'boolean', 'json'
    description TEXT,
    last_modified_by INTEGER REFERENCES USUARIO(usuario_id),
    last_modified_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    version INTEGER DEFAULT 1
);

-- Example configurations
INSERT INTO SYSTEM_CONFIG (config_key, config_value, config_type, description) VALUES
('report.submission_deadline_hours', '24', 'number', 'Horas máximas para entregar reporte después de creación'),
('report.require_supervisor_approval', 'false', 'boolean', 'Si reportes requieren aprobación de supervisor'),
('email.daily_report_recipients', '["supervisor1@ssc.mx", "director@ssc.mx"]', 'json', 'Lista de emails para reporte diario');
```

**CONFIG_HISTORY Table** (audit trail):
```sql
CREATE TABLE CONFIG_HISTORY (
    history_id SERIAL PRIMARY KEY,
    config_key VARCHAR(100) NOT NULL,
    old_value TEXT,
    new_value TEXT,
    changed_by INTEGER REFERENCES USUARIO(usuario_id),
    changed_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    change_reason TEXT
);
```

**Feature Flags Table**:
```sql
CREATE TABLE FEATURE_FLAG (
    flag_id SERIAL PRIMARY KEY,
    flag_key VARCHAR(100) UNIQUE NOT NULL, -- e.g., 'enable_ai_reports'
    is_enabled BOOLEAN DEFAULT FALSE,
    rollout_percentage INTEGER DEFAULT 0 CHECK (rollout_percentage BETWEEN 0 AND 100),
    enabled_roles TEXT[], -- e.g., ARRAY['ADMIN', 'REVISOR']
    enabled_users INTEGER[], -- Specific user IDs for beta testing
    description TEXT,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
);
```

**Feature Flag Service**:
```csharp
public class FeatureFlagService : IFeatureFlagService
{
    private readonly ApplicationDbContext _db;

    public async Task<bool> IsEnabled(string flagKey, int userId, string userRole)
    {
        var flag = await _db.FeatureFlags.FirstOrDefaultAsync(f => f.FlagKey == flagKey);

        if (flag == null || !flag.IsEnabled)
            return false;

        // Check user-specific override
        if (flag.EnabledUsers?.Contains(userId) == true)
            return true;

        // Check role-based access
        if (flag.EnabledRoles?.Contains(userRole) == true)
            return true;

        // Check rollout percentage (deterministic based on user ID)
        if (flag.RolloutPercentage > 0)
        {
            var bucket = userId % 100;
            return bucket < flag.RolloutPercentage;
        }

        return false;
    }
}
```

**Change Request Template**:
```markdown
# Change Request: [Title]

**ID**: CR-YYYY-NNN
**Date Submitted**: YYYY-MM-DD
**Submitted By**: [Name, Role]
**Priority**: [Critical / High / Medium / Low]

## Description
[Clear description of the requested change]

## Business Justification
[Why this change is needed, expected benefits]

## Affected Components
- [ ] Database schema
- [ ] UI (specify pages)
- [ ] Business logic
- [ ] Reports
- [ ] Configuration
- [ ] Documentation

## Impact Assessment
**Scope**: [Small / Medium / Large]
**Estimated Effort**: [Hours/Days]
**Risk Level**: [Low / Medium / High]
**Dependencies**: [Other features, external systems]

## Proposed Solution
[Technical approach, alternatives considered]

## Backward Compatibility
- [ ] Fully backward compatible
- [ ] Requires migration (specify)
- [ ] Breaking change (requires communication plan)

## Approval
- [ ] Product Owner: [Name, Date]
- [ ] Tech Lead: [Name, Date]
- [ ] Stakeholder: [Name, Date]
- [ ] Change Advisory Board: [Date]

## Implementation Plan
[Timeline, phases, rollback plan]
```

**Configuration Management UI** (Admin):
```razor
@page "/admin/configuration"
@attribute [Authorize(Roles = "ADMIN")]

<h3>Configuración del Sistema</h3>

<DataGrid TItem="SystemConfig" Data="@configurations">
    <DataGridColumn Field="@nameof(SystemConfig.ConfigKey)" Caption="Parámetro" />
    <DataGridColumn Field="@nameof(SystemConfig.ConfigValue)" Caption="Valor" Editable />
    <DataGridColumn Field="@nameof(SystemConfig.Description)" Caption="Descripción" />
    <DataGridColumn Field="@nameof(SystemConfig.LastModifiedAt)" Caption="Última Modificación" />
    <DataGridCommandColumn>
        <SaveCommandItem Clicked="@OnSaveConfig" />
        <CancelCommandItem />
    </DataGridCommandColumn>
</DataGrid>

@code {
    private List<SystemConfig> configurations;

    protected override async Task OnInitializedAsync()
    {
        configurations = await ConfigService.GetAllConfigs();
    }

    async Task OnSaveConfig(SaveCommandItemEventArgs<SystemConfig> e)
    {
        await ConfigService.UpdateConfig(e.Item.ConfigKey, e.Item.ConfigValue, CurrentUser.Id, "Manual update via Admin UI");
        await NotificationService.Success("Configuración actualizada correctamente");
    }
}
```

### Validation & Success Metrics

**Flexibility Metrics**:
- Change requests implemented per quarter (tracking)
- Time to implement institutional change <2 weeks (avg)
- Percentage of changes requiring code deployment <30% (target: config-driven)
- Breaking changes per year <3 (target: minimize)

**Backward Compatibility**:
- Old reports viewable without errors: 100%
- Support for N-2 form versions: 100%
- Configuration rollback success rate: 100%

**Communication Effectiveness**:
- Stakeholder satisfaction with change process >80%
- Change requests with complete impact analysis: 100%
- Unplanned changes per sprint <1 (avg)

### Integration Points

- **Feature Flags**: Checked in service layer and UI components
- **Configuration**: Loaded at application startup, cached with IMemoryCache (15-min TTL)
- **Change Requests**: Tracked in GitHub Issues with label `change-request`
- **Compliance Matrix**: Maintained in docs/ and reviewed in security audits
- **Stakeholder Communication**: Slack channel + quarterly review meetings

---

## 20. Change Resistance & Paper Workflow Transition

**Purpose**: Facilitate smooth transition from paper-based workflows to digital system, minimizing user resistance.

**Context**: RN-003 Mitigation - Addressing resistance to change from users accustomed to manual paper processes. Success depends on demonstrating tangible benefits and providing robust support during transition.

**Risk Mitigation for RN-003**: Resistencia al Cambio y Preferencia por Papel

### Strategy Components

**1. User Involvement & Co-Design**:
- Involve end users (police agents) from design phase
- Prototype validation sessions before development
- User stories written with real user quotes
- Champion program: identify early adopters as advocates
- User advisory board for ongoing feedback
- Monthly user forums for Q&A and feature demonstrations

**2. Pilot Program**:
- Limited rollout to 10-15 users (pilot group) before general availability
- Selection criteria: mix of tech-savvy and tech-averse users
- 4-week pilot with dedicated support
- Daily check-ins first week, weekly afterward
- Pilot feedback incorporated before full rollout
- Success metrics: >80% satisfaction, <5 critical bugs

**3. Tangible Benefits Communication**:
- Quantify time savings: "5 min digital vs 15 min paper"
- Highlight search/retrieval: "Find report in 10 seconds vs 30 minutes"
- Emphasize no lost documents: "100% retention vs 5% paper loss"
- Demonstrate automatic reporting: "No manual aggregation"
- Show mobile access: "Submit from field, not just office"
- Case studies from pilot users

**4. Dedicated Support Team**:
- Support hotline (phone + WhatsApp) during business hours
- Email support with <4 hour response SLA
- On-site support visits first 2 weeks post-launch
- Escalation path for critical issues (<15 min response)
- Support ticket tracking in SUPPORT_TICKET table
- Weekly support metrics review

**5. Gradual Transition Plan**:
- Phase 1 (Weeks 1-2): Pilot group only, paper backup allowed
- Phase 2 (Weeks 3-4): 50% of users, parallel paper/digital
- Phase 3 (Weeks 5-6): 100% of users, digital required
- Phase 4 (Weeks 7-8): Paper forms discontinued
- Flexibility for users struggling with digital (extended transition)
- Print capability maintained for those needing paper reference

**6. Quick Wins & Early Successes**:
- Implement most-requested features first (low-hanging fruit)
- Publicly celebrate user success stories
- Monthly "Feature of the Month" announcements
- Showcase efficiency gains with metrics
- Recognize and reward early adopters (certificates, acknowledgment)
- Gamification: badges for milestones (10 reports, 50 reports, etc.)

**7. Comprehensive Training Program**:
- Mandatory training for all users before access granted
- In-person sessions (2 hours) with hands-on practice
- Remote training for field staff (video conferencing)
- Training materials: slides, handouts, cheat sheets
- Train-the-trainer program for supervisors
- Refresher training monthly for new features

**8. Print-Friendly Outputs**:
- PDF export of reports for archival/reference
- Print-optimized layouts (no UI chrome)
- Ability to print drafts for review before submission
- Paper forms with QR code linking to digital report
- Satisfies users who want paper backup
- Gradual weaning from paper over 6 months

**9. Feedback Loop & Rapid Iteration**:
- Weekly feedback sessions during first month
- Public roadmap showing user requests and status
- Monthly release notes highlighting user-requested features
- User survey after 1 month, 3 months, 6 months
- Net Promoter Score (NPS) tracking
- Transparent communication about what's feasible vs not

**10. Change Champions Network**:
- Identify 5-10 enthusiastic users as champions
- Provide advanced training and early access to features
- Champions support peers informally
- Monthly champion meetings to gather insights
- Champions recognized publicly (e.g., in stakeholder reports)
- Incentivize with small rewards (certificates, mentions)

### Implementation Details

**PILOT_USER Table**:
```sql
CREATE TABLE PILOT_USER (
    pilot_id SERIAL PRIMARY KEY,
    usuario_FK INTEGER REFERENCES USUARIO(usuario_id),
    pilot_start_date DATE,
    pilot_end_date DATE,
    feedback_submitted BOOLEAN DEFAULT FALSE,
    satisfaction_score INTEGER CHECK (satisfaction_score BETWEEN 1 AND 5),
    would_recommend BOOLEAN,
    comments TEXT,
    is_champion BOOLEAN DEFAULT FALSE
);
```

**SUPPORT_TICKET Table**:
```sql
CREATE TABLE SUPPORT_TICKET (
    ticket_id SERIAL PRIMARY KEY,
    usuario_FK INTEGER REFERENCES USUARIO(usuario_id),
    title VARCHAR(200) NOT NULL,
    description TEXT,
    category VARCHAR(50), -- 'technical', 'training', 'feature_request', 'bug'
    priority VARCHAR(20) DEFAULT 'medium', -- 'low', 'medium', 'high', 'critical'
    status VARCHAR(20) DEFAULT 'open', -- 'open', 'in_progress', 'resolved', 'closed'
    assigned_to INTEGER REFERENCES USUARIO(usuario_id),
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    resolved_at TIMESTAMPTZ,
    resolution_notes TEXT
);

CREATE INDEX idx_support_ticket_status ON SUPPORT_TICKET(status);
CREATE INDEX idx_support_ticket_user ON SUPPORT_TICKET(usuario_FK);
```

**USER_ACHIEVEMENT Table** (gamification):
```sql
CREATE TABLE USER_ACHIEVEMENT (
    achievement_id SERIAL PRIMARY KEY,
    usuario_FK INTEGER REFERENCES USUARIO(usuario_id),
    badge_type VARCHAR(50), -- 'first_report', 'ten_reports', 'fifty_reports', 'fast_submitter'
    earned_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(usuario_FK, badge_type)
);
```

**Pilot Program Dashboard**:
```csharp
public class PilotMetrics
{
    public int TotalPilotUsers { get; set; }
    public int ActivePilotUsers { get; set; }
    public int FeedbackSubmitted { get; set; }
    public double AvgSatisfactionScore { get; set; } // 1-5
    public int WouldRecommendCount { get; set; }
    public List<string> TopIssues { get; set; } // Most common support categories
    public int CriticalBugs { get; set; }
    public double GoLiveReadiness { get; set; } // Calculated score 0-100
}
```

**Go-Live Readiness Calculation**:
```csharp
public double CalculateGoLiveReadiness(PilotMetrics metrics)
{
    var satisfactionScore = (metrics.AvgSatisfactionScore / 5.0) * 30; // 30% weight
    var recommendScore = (metrics.WouldRecommendCount / (double)metrics.FeedbackSubmitted) * 25; // 25%
    var bugScore = metrics.CriticalBugs == 0 ? 25 : Math.Max(0, 25 - (metrics.CriticalBugs * 5)); // 25%
    var feedbackScore = (metrics.FeedbackSubmitted / (double)metrics.TotalPilotUsers) * 20; // 20%

    return satisfactionScore + recommendScore + bugScore + feedbackScore; // Max 100
}
// Target: >75 for go-live approval
```

**Support Ticket Management UI**:
```razor
@page "/support/tickets"
@attribute [Authorize(Roles = "ADMIN,REVISOR")]

<h3>Tickets de Soporte</h3>

<Tabs>
    <Tab Name="open">
        <DataGrid Data="@openTickets" TItem="SupportTicket">
            <DataGridColumn Field="@nameof(SupportTicket.TicketId)" Caption="ID" />
            <DataGridColumn Field="@nameof(SupportTicket.Title)" Caption="Título" />
            <DataGridColumn Field="@nameof(SupportTicket.Category)" Caption="Categoría" />
            <DataGridColumn Field="@nameof(SupportTicket.Priority)" Caption="Prioridad" />
            <DataGridColumn Field="@nameof(SupportTicket.CreatedAt)" Caption="Fecha" />
            <DataGridCommandColumn>
                <Button Color="Color.Primary" Clicked="@(() => AssignTicket(context))">Atender</Button>
            </DataGridCommandColumn>
        </DataGrid>
    </Tab>
    <Tab Name="resolved">
        <!-- Resolved tickets list -->
    </Tab>
</Tabs>
```

**Change Champion Portal**:
```razor
@page "/champions"
@attribute [Authorize] <!-- Champions have flag in PILOT_USER table -->

<h3>Portal de Campeones</h3>

<Row>
    <Column>
        <Card>
            <CardHeader>Tus Métricas</CardHeader>
            <CardBody>
                <p>Usuarios apoyados: @supportedUsers</p>
                <p>Feedback compartido: @feedbackCount</p>
                <p>Nivel: @championLevel</p>
            </CardBody>
        </Card>
    </Column>
    <Column>
        <Card>
            <CardHeader>Próximos Eventos</CardHeader>
            <CardBody>
                <p>Reunión mensual: [Fecha]</p>
                <p>Capacitación avanzada: [Fecha]</p>
            </CardBody>
        </Card>
    </Column>
</Row>

<h4>Recursos Exclusivos</h4>
<ul>
    <li><a href="/docs/advanced-features">Guía de Funciones Avanzadas</a></li>
    <li><a href="/beta-features">Acceso Beta a Nuevas Funciones</a></li>
    <li><a href="/champion-forum">Foro Exclusivo de Campeones</a></li>
</ul>
```

### Validation & Success Metrics

**Adoption Metrics**:
- User activation rate >80% (used system at least once in first week)
- Weekly active users >70% of total users by end of month 3
- Digital report submissions >90% by end of month 6
- Paper report submissions <10% by end of month 6

**Satisfaction Metrics**:
- Pilot satisfaction score >4/5 (80%)
- Post-launch NPS >40 within 3 months
- Support tickets <10 per month by month 3
- Training completion rate >85%

**Transition Metrics**:
- Go-live readiness score >75 before full rollout
- Critical bugs during pilot: 0
- Average ticket resolution time <4 hours (critical), <24 hours (high)

**Champion Program**:
- Champions identified: >5 users
- Champion satisfaction: >4.5/5
- Peer support instances: >10 per champion per month

### Integration Points

- **Pilot Program**: Tracked via flag in PILOT_USER table, dashboard in Admin module
- **Support System**: Integrated with email (tickets created from support@), in-app form
- **Training**: Completion tracked in TRAINING_COMPLETION table
- **Champions**: Portal accessible only to flagged users (is_champion = true)
- **Feedback**: Collected via in-app forms, stored in FEEDBACK table
- **Print Outputs**: PDF generation service (already implemented in RT-003)

---

## 21. High Availability & Service Continuity

**Purpose**: Minimize service interruptions during business hours to maintain operational continuity for police reporting.

**Context**: RN-004 Mitigation - Addressing the risk of service disruptions during critical hours (8 AM - 8 PM). System downtime could prevent urgent incident reporting, impacting public safety operations.

**Risk Mitigation for RN-004**: Interrupción del Servicio Durante Horario Laboral

### Strategy Components

**1. High Availability Architecture**:
- Uptime target: 99.5% during business hours (8 AM - 8 PM Mon-Sun)
- Allowed downtime: ~22 hours/year or ~1.8 hours/month
- Maintenance windows EXCLUSIVELY outside business hours (2 AM - 6 AM)
- Auto-restart policies for Docker containers
- Health checks with automatic recovery
- Database replication for failover (if budget allows)

**2. Proactive Health Monitoring**:
- Health check endpoint `/health` queried every 60 seconds
- Checks: database connectivity, disk space, memory usage, API responsiveness
- Prometheus metrics collection
- Grafana dashboards for real-time visibility
- PagerDuty/OpsGenie integration for immediate alerting
- SMS + email alerts for critical issues

**3. Automated Recovery**:
- Docker restart policy: `unless-stopped` or `always`
- Systemd service for Docker Compose (auto-restart on host reboot)
- Database connection pool recovery (reconnect on transient failures)
- Circuit breakers for external services (already in RO-004)
- Graceful degradation: disable non-critical features if resources low
- Self-healing scripts: auto-clear disk space, restart hung processes

**4. Incident Response Protocol**:
- Runbook for common failures (docs/runbooks/)
- On-call rotation schedule (DevOps + SysAdmin)
- SLA: <15 min response for critical incidents during business hours
- Escalation path: L1 (SysAdmin) → L2 (DevOps) → L3 (Tech Lead)
- Post-incident review (PIR) within 48 hours
- Root cause analysis (RCA) documented

**5. Status Page**:
- Public status page at `/status` (read-only, no auth required)
- Real-time system status: operational, degraded, outage
- Incident notifications with estimated time to resolution (ETR)
- Historical uptime metrics (last 30 days, 90 days)
- Planned maintenance calendar
- Subscribe to updates via email/SMS

**6. Maintenance Window Discipline**:
- Pre-scheduled maintenance: First Sunday 2:00 AM - 6:00 AM monthly
- Stakeholder notification 48 hours in advance (email + Slack)
- Emergency maintenance: only for critical security patches or P1 incidents
- No deployments during critical periods (end-of-month, special events)
- Dry run in staging before production maintenance
- Rollback plan ready before every maintenance

**7. Backup Server (Active-Passive)**:
- If budget allows: secondary server as hot standby
- Continuous database replication (PostgreSQL streaming replication)
- Automatic failover using Patroni/Keepalived (if implemented)
- Manual failover procedure documented (docs/runbooks/failover.md)
- Failover tested quarterly
- RPO (Recovery Point Objective): <15 minutes
- RTO (Recovery Time Objective): <30 minutes

**8. Database Connection Resilience**:
- Connection pooling with retry logic (Npgsql with Polly)
- Transient fault handling: retry 3 times with exponential backoff
- Connection validation before use
- Connection timeout: 30 seconds
- Pool exhaustion monitoring with alerts
- Prepared for read replicas (future scalability)

**9. Disk Space Management**:
- Automated disk space monitoring (alert at 75%, critical at 85%)
- Log rotation: application logs kept for 30 days, compressed after 7 days
- Database VACUUM scheduled weekly (off-hours)
- Temp file cleanup cron job (daily)
- Backup file retention policy (7 daily, 4 weekly, 12 monthly, 3 annual - from RO-001)
- Auto-archival of old audit logs (>1 year) to cold storage

**10. Performance Degradation Detection**:
- Response time monitoring (p50, p95, p99 latency)
- Alert on p95 latency >3 seconds sustained for 5 minutes
- Slow query logging (PostgreSQL: log queries >1 second)
- Weekly performance report: trends, anomalies
- Capacity planning review quarterly
- Load testing annually to validate performance under stress

### Implementation Details

**Health Check Endpoint**:
```csharp
// Endpoints/HealthEndpoint.cs
app.MapGet("/health", async (ApplicationDbContext db, IConfiguration config) =>
{
    var health = new HealthStatus { Status = "healthy", Checks = new List<HealthCheck>() };

    // Database connectivity
    try
    {
        await db.Database.ExecuteSqlRawAsync("SELECT 1");
        health.Checks.Add(new HealthCheck { Name = "database", Status = "healthy" });
    }
    catch (Exception ex)
    {
        health.Status = "unhealthy";
        health.Checks.Add(new HealthCheck { Name = "database", Status = "unhealthy", Error = ex.Message });
    }

    // Disk space
    var drive = new DriveInfo(config["DataPath"] ?? "/var/lib/app");
    var freeSpacePercent = (drive.AvailableFreeSpace / (double)drive.TotalSize) * 100;
    if (freeSpacePercent < 15)
    {
        health.Status = "degraded";
        health.Checks.Add(new HealthCheck { Name = "disk_space", Status = "degraded", Message = $"Only {freeSpacePercent:F1}% free" });
    }
    else
    {
        health.Checks.Add(new HealthCheck { Name = "disk_space", Status = "healthy" });
    }

    // Memory usage
    var memoryUsed = GC.GetTotalMemory(false) / (1024.0 * 1024.0); // MB
    health.Checks.Add(new HealthCheck { Name = "memory", Status = "healthy", Message = $"{memoryUsed:F0} MB used" });

    return Results.Json(health, statusCode: health.Status == "healthy" ? 200 : 503);
});

public class HealthStatus
{
    public string Status { get; set; } // healthy, degraded, unhealthy
    public List<HealthCheck> Checks { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class HealthCheck
{
    public string Name { get; set; }
    public string Status { get; set; }
    public string Message { get; set; }
    public string Error { get; set; }
}
```

**Docker Compose Restart Policy**:
```yaml
# docker-compose.yml
services:
  webapp:
    image: ceiba-reportes:latest
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 60s
      timeout: 10s
      retries: 3
      start_period: 40s

  postgres:
    image: postgres:18
    restart: unless-stopped
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ceiba"]
      interval: 30s
      timeout: 5s
      retries: 3
```

**Systemd Service** (auto-start Docker Compose on server boot):
```ini
# /etc/systemd/system/ceiba-reportes.service
[Unit]
Description=Ceiba Reportes Application
Requires=docker.service
After=docker.service

[Service]
Type=oneshot
RemainAfterExit=yes
WorkingDirectory=/opt/ceiba-reportes
ExecStart=/usr/bin/docker-compose up -d
ExecStop=/usr/bin/docker-compose down
Restart=on-failure
RestartSec=30s

[Install]
WantedBy=multi-user.target
```

**Prometheus Alerting Rules**:
```yaml
# prometheus/alerts.yml
groups:
  - name: ceiba_alerts
    rules:
      - alert: HighResponseTime
        expr: histogram_quantile(0.95, http_request_duration_seconds) > 3
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "High response time detected (p95 > 3s)"

      - alert: ServiceDown
        expr: up{job="ceiba-webapp"} == 0
        for: 2m
        labels:
          severity: critical
        annotations:
          summary: "Ceiba service is down"

      - alert: DatabaseConnectionFailed
        expr: health_check_status{check="database"} == 0
        for: 1m
        labels:
          severity: critical
        annotations:
          summary: "Database connection failed"

      - alert: DiskSpaceLow
        expr: (node_filesystem_avail_bytes / node_filesystem_size_bytes) * 100 < 15
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "Disk space below 15%"
```

**Status Page Component**:
```razor
@page "/status"
@AllowAnonymous

<h3>Estado del Sistema</h3>

<Alert Color="@GetAlertColor(systemStatus.Status)" Visible="true">
    <strong>Estado:</strong> @systemStatus.StatusText
</Alert>

<h4>Componentes</h4>
<Table>
    <TableHeader>
        <TableRow>
            <TableHeaderCell>Componente</TableHeaderCell>
            <TableHeaderCell>Estado</TableHeaderCell>
            <TableHeaderCell>Mensaje</TableHeaderCell>
        </TableRow>
    </TableHeader>
    <TableBody>
        @foreach (var check in systemStatus.Checks)
        {
            <TableRow>
                <TableRowCell>@check.Name</TableRowCell>
                <TableRowCell><Badge Color="@GetBadgeColor(check.Status)">@check.Status</Badge></TableRowCell>
                <TableRowCell>@check.Message</TableRowCell>
            </TableRow>
        }
    </TableBody>
</Table>

<h4>Historial de Disponibilidad (Últimos 30 Días)</h4>
<p>Uptime: <strong>@systemStatus.Uptime30Days%</strong></p>

<h4>Mantenimientos Programados</h4>
<ul>
    @foreach (var maintenance in scheduledMaintenances)
    {
        <li>@maintenance.Date.ToString("dd/MM/yyyy HH:mm") - @maintenance.Description</li>
    }
</ul>

@code {
    private HealthStatus systemStatus;
    private List<ScheduledMaintenance> scheduledMaintenances;

    protected override async Task OnInitializedAsync()
    {
        systemStatus = await Http.GetFromJsonAsync<HealthStatus>("/health");
        scheduledMaintenances = await MaintenanceService.GetScheduled();
    }

    Color GetAlertColor(string status) => status switch
    {
        "healthy" => Color.Success,
        "degraded" => Color.Warning,
        "unhealthy" => Color.Danger,
        _ => Color.Secondary
    };

    Color GetBadgeColor(string status) => status switch
    {
        "healthy" => Color.Success,
        "degraded" => Color.Warning,
        "unhealthy" => Color.Danger,
        _ => Color.Secondary
    };
}
```

**Incident Response Runbook** (example):
```markdown
# Runbook: Database Connection Failures

## Symptoms
- Health check endpoint returns unhealthy for database
- Users unable to load reports
- Error logs: "Npgsql.NpgsqlException: Connection timeout"

## Immediate Actions
1. Check PostgreSQL container status: `docker ps | grep postgres`
2. If down: `docker-compose restart postgres`
3. Check PostgreSQL logs: `docker logs ceiba-postgres`
4. Check connection pool: query `pg_stat_activity` for active connections

## Resolution Steps
### If PostgreSQL is down:
1. Restart container: `docker-compose restart postgres`
2. Wait 30 seconds for initialization
3. Verify with `docker logs ceiba-postgres | grep "ready to accept connections"`
4. Test health endpoint: `curl http://localhost/health`

### If connection pool exhausted:
1. Check active queries: `SELECT * FROM pg_stat_activity WHERE state = 'active';`
2. Kill long-running queries if necessary: `SELECT pg_terminate_backend(pid);`
3. Restart web app to reset pool: `docker-compose restart webapp`

### If disk full:
1. Check disk space: `df -h`
2. Clear old logs: `find /var/log -name "*.log" -mtime +7 -delete`
3. Run manual VACUUM: `docker exec ceiba-postgres psql -U ceiba -c "VACUUM FULL;"`

## Escalation
If not resolved within 15 minutes, escalate to Tech Lead.

## Post-Incident
- Document incident in INCIDENT_LOG table
- Update runbook if new learnings
- Schedule PIR within 48 hours
```

**INCIDENT_LOG Table**:
```sql
CREATE TABLE INCIDENT_LOG (
    incident_id SERIAL PRIMARY KEY,
    title VARCHAR(200) NOT NULL,
    severity VARCHAR(20) NOT NULL, -- 'low', 'medium', 'high', 'critical'
    started_at TIMESTAMPTZ NOT NULL,
    resolved_at TIMESTAMPTZ,
    duration_minutes INTEGER GENERATED ALWAYS AS (EXTRACT(EPOCH FROM (resolved_at - started_at)) / 60) STORED,
    impact TEXT, -- User-facing impact description
    root_cause TEXT,
    resolution TEXT,
    responder_FK INTEGER REFERENCES USUARIO(usuario_id),
    post_incident_review_completed BOOLEAN DEFAULT FALSE
);

CREATE INDEX idx_incident_log_started ON INCIDENT_LOG(started_at DESC);
```

**SCHEDULED_MAINTENANCE Table**:
```sql
CREATE TABLE SCHEDULED_MAINTENANCE (
    maintenance_id SERIAL PRIMARY KEY,
    title VARCHAR(200) NOT NULL,
    description TEXT,
    scheduled_start TIMESTAMPTZ NOT NULL,
    scheduled_end TIMESTAMPTZ NOT NULL,
    actual_start TIMESTAMPTZ,
    actual_end TIMESTAMPTZ,
    status VARCHAR(20) DEFAULT 'scheduled', -- 'scheduled', 'in_progress', 'completed', 'cancelled'
    notification_sent BOOLEAN DEFAULT FALSE,
    created_by INTEGER REFERENCES USUARIO(usuario_id)
);

CREATE INDEX idx_maintenance_scheduled ON SCHEDULED_MAINTENANCE(scheduled_start);
```

### Validation & Success Metrics

**Availability Metrics**:
- Uptime during business hours (8 AM - 8 PM): >99.5%
- MTBF (Mean Time Between Failures): >720 hours (30 days)
- MTTR (Mean Time To Recovery): <30 minutes
- Planned maintenance during business hours: 0

**Response Metrics**:
- Critical incident response time: <15 minutes
- High-priority incident response time: <1 hour
- Incident resolution time: <30 minutes (critical), <4 hours (high)

**Monitoring Metrics**:
- Health check failures detected: <1% false positives
- Alert fatigue rate: <10% ignored alerts
- Monitoring coverage: 100% of critical endpoints

**User Impact**:
- User-reported outages: <2 per month
- Support tickets related to availability: <5 per month
- Incidents during business hours: <1 per month

### Integration Points

- **Health Checks**: `/health` endpoint called by Prometheus, Grafana, PagerDuty
- **Status Page**: Public route `/status`, no authentication required
- **Alerting**: Prometheus AlertManager → PagerDuty/Email/Slack
- **Incident Logging**: Incidents recorded in INCIDENT_LOG by on-call responder
- **Maintenance Notifications**: 48h email via background job (RO-003 email system)
- **Runbooks**: Stored in docs/runbooks/, linked from admin dashboard

---

## 22. Data Privacy & Information Security

**Purpose**: Prevent unauthorized access, data leaks, and privacy violations in a system handling sensitive law enforcement data.

**Context**: RN-005 Mitigation - Addressing the critical risk of information leakage or privacy breaches. As a government system handling citizen data and incident reports, any security breach could have severe legal, reputational, and operational consequences.

**Risk Mitigation for RN-005**: Fuga de Información o Violación de Privacidad

### Strategy Components

**1. Encryption at Rest and in Transit**:
- PostgreSQL data encryption at rest (LUKS/dm-crypt for disk volumes)
- TLS 1.3 for all HTTP traffic (forced HTTPS redirection)
- Database connection encryption (SSL/TLS for PostgreSQL connections)
- Backup files encrypted (gpg encryption with key rotation)
- Secrets management: environment variables, never in code
- Encryption key storage in hardware security module (HSM) or secure vault (if budget)

**2. Principle of Least Privilege**:
- Role-Based Access Control (RBAC) strictly enforced (already in RS-001)
- Database users with minimal permissions (app user: SELECT/INSERT/UPDATE only, no DROP/TRUNCATE)
- OS-level user permissions (app runs as non-root user in Docker)
- Network segmentation: database not exposed to public internet
- API endpoints require authentication + authorization
- No default credentials (enforce password change on first login)

**3. Audit Trail & Activity Monitoring**:
- All data access logged to AUDITORIA table (already implemented)
- Anomaly detection: alert on unusual patterns (e.g., >100 report downloads in 1 hour)
- Quarterly access review: who accessed what sensitive data
- Audit log retention: indefinite (compliance requirement)
- Audit log integrity: append-only, immutable (PostgreSQL rules)
- SIEM integration (Security Information and Event Management) if budget allows

**4. Data Loss Prevention (DLP)**:
- PDF watermarking with user name + timestamp on exports
- Export limits enforced (already in RT-003: 50 PDFs, 100 JSONs)
- Rate limiting on exports (already in RO-002: 5 exports/user/min)
- No bulk download API (force pagination)
- Email export: recipients whitelisted (configurable in SYSTEM_CONFIG)
- USB/removable media disabled on server (physical security)

**5. Security Audits & Penetration Testing**:
- Internal security audit quarterly
- External penetration test annually (contract with security firm)
- OWASP Top 10 compliance verification (already in RS-002)
- Vulnerability scanning with Snyk/SonarQube (already in RS-002)
- Security code review for all PRs (already in RS-001)
- Compliance with Ley Federal de Protección de Datos Personales (Mexico GDPR)

**6. Incident Response Plan**:
- Security Incident Response Team (SIRT): Security Lead + Tech Lead + Legal
- Incident classification: P1 (data breach), P2 (unauthorized access attempt), P3 (policy violation)
- Containment: isolate affected systems within 1 hour
- Notification: stakeholders within 4 hours, affected users within 72 hours (if PII exposed)
- Forensic analysis: preserve logs, snapshots for investigation
- Post-incident review: update security controls based on lessons learned

**7. Access Review & User Lifecycle**:
- Quarterly access review: ADMIN verifies all user accounts still valid
- Automated account deactivation: inactive >90 days → suspended
- Offboarding process: immediate account deactivation on employee departure
- Privileged access recertification: ADMIN + REVISOR accounts reviewed monthly
- Audit log of access changes (who granted/revoked access to whom)

**8. Data Minimization & Retention**:
- Collect only necessary data (no excessive PII)
- Data retention policy: audit logs indefinite, application logs 30 days, backups per RO-001
- Anonymization: reports archived >5 years have PII redacted (future compliance requirement)
- Right to erasure: documented process for data deletion requests (GDPR-style)
- Data classification: public, internal, confidential, restricted

**9. Physical & Network Security**:
- Server in secure facility (locked room, access control)
- Network firewall: only ports 80 (redirect to 443), 443, and 22 (SSH, key-only) open
- SSH key-based authentication only (no password login)
- VPN required for administrative access (if feasible)
- Intrusion Detection System (IDS) monitoring network traffic
- Regular security patching (already in RO-005)

**10. User Awareness Training**:
- Annual security training mandatory for all users
- Topics: phishing, password security, social engineering, data handling
- Simulated phishing tests quarterly
- Security awareness posters/reminders
- Reporting suspicious activity encouraged (dedicated email/hotline)
- Security champion embedded in each user group

### Implementation Details

**Disk Encryption** (LUKS example for Linux):
```bash
# scripts/setup-encryption.sh
# Encrypt data volume before creating filesystem
cryptsetup luksFormat /dev/sdb
cryptsetup luksOpen /dev/sdb encrypted_data
mkfs.ext4 /dev/mapper/encrypted_data
mount /dev/mapper/encrypted_data /var/lib/postgresql/data

# Add to /etc/crypttab for auto-unlock on boot (with key file)
echo "encrypted_data /dev/sdb /root/luks-key luks" >> /etc/crypttab
```

**PostgreSQL SSL Connection**:
```yaml
# docker-compose.yml
services:
  postgres:
    environment:
      POSTGRES_SSLMODE: require
    volumes:
      - ./certs/server.crt:/var/lib/postgresql/server.crt:ro
      - ./certs/server.key:/var/lib/postgresql/server.key:ro
    command: >
      postgres
      -c ssl=on
      -c ssl_cert_file=/var/lib/postgresql/server.crt
      -c ssl_key_file=/var/lib/postgresql/server.key
```

**Connection String** (ASP.NET Core):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=postgres;Database=ceiba;Username=ceiba_app;Password=${DB_PASSWORD};SSL Mode=Require;Trust Server Certificate=false"
  }
}
```

**PDF Watermarking**:
```csharp
public class PDFWatermarkService
{
    public byte[] AddWatermark(byte[] pdfBytes, string userName, DateTime exportedAt)
    {
        using var pdfDoc = PdfDocument.Open(pdfBytes);
        using var pdfWriter = new PdfDocumentBuilder();

        foreach (var page in pdfDoc.GetPages())
        {
            var watermarkText = $"Exportado por {userName} el {exportedAt:dd/MM/yyyy HH:mm}";

            // Add watermark to page footer
            pdfWriter.AddPage(page);
            pdfWriter.DrawText(
                watermarkText,
                fontSize: 8,
                x: 50,
                y: 20, // Footer position
                color: new PdfRgbColor(0.7, 0.7, 0.7) // Gray
            );
        }

        return pdfWriter.Build();
    }
}
```

**Anomaly Detection Service**:
```csharp
public class AnomalyDetectionService : BackgroundService
{
    private readonly IServiceProvider _services;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var alertService = scope.ServiceProvider.GetRequiredService<IAlertService>();

            var oneHourAgo = DateTime.UtcNow.AddHours(-1);

            // Detect excessive downloads
            var excessiveDownloads = await db.Auditoria
                .Where(a => a.Accion == "EXPORT_PDF" && a.FechaHora >= oneHourAgo)
                .GroupBy(a => a.UsuarioFK)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .Where(x => x.Count > 50)
                .ToListAsync();

            foreach (var anomaly in excessiveDownloads)
            {
                await alertService.SendSecurityAlert(
                    $"Usuario {anomaly.UserId} descargó {anomaly.Count} PDFs en la última hora",
                    severity: "high"
                );
            }

            // Detect unusual access patterns
            var afterHoursAccess = await db.Auditoria
                .Where(a => a.FechaHora >= oneHourAgo
                    && (a.FechaHora.Hour < 6 || a.FechaHora.Hour > 22))
                .CountAsync();

            if (afterHoursAccess > 10)
            {
                await alertService.SendSecurityAlert(
                    $"{afterHoursAccess} acciones realizadas fuera de horario laboral (6 AM - 10 PM)",
                    severity: "medium"
                );
            }

            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken); // Check every 15 min
        }
    }
}
```

**ACCESS_REVIEW Table**:
```sql
CREATE TABLE ACCESS_REVIEW (
    review_id SERIAL PRIMARY KEY,
    review_date DATE NOT NULL,
    reviewed_by INTEGER REFERENCES USUARIO(usuario_id),
    total_users_reviewed INTEGER,
    accounts_deactivated INTEGER DEFAULT 0,
    accounts_modified INTEGER DEFAULT 0,
    findings TEXT, -- Summary of review findings
    completed_at TIMESTAMPTZ,
    next_review_due DATE
);
```

**SECURITY_INCIDENT Table**:
```sql
CREATE TABLE SECURITY_INCIDENT (
    incident_id SERIAL PRIMARY KEY,
    incident_type VARCHAR(50) NOT NULL, -- 'data_breach', 'unauthorized_access', 'policy_violation'
    severity VARCHAR(20) NOT NULL, -- 'low', 'medium', 'high', 'critical'
    description TEXT NOT NULL,
    affected_users INTEGER[], -- Array of user IDs
    detected_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    detected_by INTEGER REFERENCES USUARIO(usuario_id),
    contained_at TIMESTAMPTZ,
    resolved_at TIMESTAMPTZ,
    root_cause TEXT,
    remediation_actions TEXT,
    stakeholders_notified BOOLEAN DEFAULT FALSE,
    notification_date TIMESTAMPTZ,
    post_incident_review_completed BOOLEAN DEFAULT FALSE
);

CREATE INDEX idx_security_incident_severity ON SECURITY_INCIDENT(severity, detected_at DESC);
```

**Quarterly Access Review Dashboard**:
```razor
@page "/admin/access-review"
@attribute [Authorize(Roles = "ADMIN")]

<h3>Revisión Trimestral de Accesos</h3>

<p>Última revisión: @lastReview?.ReviewDate.ToString("dd/MM/yyyy")</p>
<p>Próxima revisión: @nextReviewDue.ToString("dd/MM/yyyy")</p>

<Button Color="Color.Primary" Clicked="@StartReview">Iniciar Revisión</Button>

<h4>Usuarios Activos</h4>
<DataGrid TItem="UserReview" Data="@activeUsers">
    <DataGridColumn Field="@nameof(UserReview.NombreCompleto)" Caption="Usuario" />
    <DataGridColumn Field="@nameof(UserReview.Rol)" Caption="Rol" />
    <DataGridColumn Field="@nameof(UserReview.LastActiveDate)" Caption="Último Acceso" />
    <DataGridColumn Field="@nameof(UserReview.DaysSinceLastActive)" Caption="Días Inactivo" />
    <DataGridCommandColumn>
        <Button Color="Color.Warning" Clicked="@(() => DeactivateUser(context))" Disabled="@(context.DaysSinceLastActive < 90)">
            Desactivar
        </Button>
        <Button Color="Color.Danger" Clicked="@(() => ModifyAccess(context))">
            Modificar Acceso
        </Button>
    </DataGridCommandColumn>
</DataGrid>

<h4>Cuentas Inactivas >90 Días</h4>
<DataGrid TItem="UserReview" Data="@inactiveUsers">
    <!-- Similar columns -->
    <DataGridCommandColumn>
        <Button Color="Color.Danger" Clicked="@(() => DeactivateUser(context))">Desactivar</Button>
    </DataGridCommandColumn>
</DataGrid>

@code {
    private List<UserReview> activeUsers;
    private List<UserReview> inactiveUsers;
    private AccessReview lastReview;
    private DateTime nextReviewDue;

    protected override async Task OnInitializedAsync()
    {
        activeUsers = await AccessReviewService.GetActiveUsers();
        inactiveUsers = await AccessReviewService.GetInactiveUsers(90);
        lastReview = await AccessReviewService.GetLastReview();
        nextReviewDue = lastReview?.NextReviewDue ?? DateTime.Today.AddMonths(3);
    }

    async Task StartReview()
    {
        await AccessReviewService.CreateReview(CurrentUser.Id);
        await NotificationService.Info("Revisión iniciada. Por favor revisa cada cuenta.");
    }

    async Task DeactivateUser(UserReview user)
    {
        await UserService.DeactivateUser(user.UserId, "Inactivo >90 días - Revisión trimestral");
        await AuditService.Log("USER_DEACTIVATED", user.UserId, $"Desactivado por {CurrentUser.NombreCompleto}");
        await OnInitializedAsync(); // Refresh
    }
}
```

**Security Training Tracking**:
```sql
CREATE TABLE SECURITY_TRAINING (
    training_id SERIAL PRIMARY KEY,
    usuario_FK INTEGER REFERENCES USUARIO(usuario_id),
    training_module VARCHAR(100), -- 'phishing_awareness', 'data_security', 'password_policy'
    completed_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    score INTEGER CHECK (score BETWEEN 0 AND 100),
    certificate_issued BOOLEAN DEFAULT FALSE,
    expiration_date DATE, -- Annual re-certification required
    UNIQUE(usuario_FK, training_module, EXTRACT(YEAR FROM completed_at))
);

CREATE INDEX idx_security_training_expiration ON SECURITY_TRAINING(expiration_date) WHERE certificate_issued = TRUE;
```

**Backup Encryption Script**:
```bash
# scripts/backup-with-encryption.sh
#!/bin/bash
set -euo pipefail

BACKUP_DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="/backups/ceiba_${BACKUP_DATE}.sql"
ENCRYPTED_FILE="${BACKUP_FILE}.gpg"

# Dump database
docker exec ceiba-postgres pg_dump -U ceiba ceiba > "$BACKUP_FILE"

# Encrypt with GPG (symmetric encryption)
gpg --symmetric --cipher-algo AES256 --output "$ENCRYPTED_FILE" "$BACKUP_FILE"

# Remove unencrypted file
rm "$BACKUP_FILE"

# Upload to offsite storage (S3, NAS, etc.)
aws s3 cp "$ENCRYPTED_FILE" "s3://ceiba-backups/$(basename $ENCRYPTED_FILE)"

echo "Backup encrypted and uploaded: $ENCRYPTED_FILE"
```

### Validation & Success Metrics

**Security Metrics**:
- Security incidents (data breaches): 0
- Unauthorized access attempts blocked: 100%
- Data encryption coverage: 100% (data at rest + in transit)
- Vulnerability scan findings: 0 critical, <5 high

**Compliance Metrics**:
- Compliance with Ley Federal de Protección de Datos: 100%
- Quarterly access reviews completed on time: 100%
- Security training completion rate: 100% of users
- Penetration test pass rate: >90% (no critical findings)

**Audit Metrics**:
- Audit log completeness: 100% of sensitive operations logged
- Anomalous activity detection time: <5 minutes
- Incident response time: <1 hour (containment), <4 hours (notification)

**User Awareness**:
- Phishing simulation click rate: <10% (target: declining over time)
- Security policy acknowledgment: 100% of users
- Suspicious activity reports: >5 per quarter (user vigilance indicator)

### Integration Points

- **Encryption**: Disk encryption via LUKS, database SSL via PostgreSQL config, HTTPS via Nginx/Traefik
- **Watermarking**: Integrated in PDF export service (RT-003)
- **Anomaly Detection**: Background service, alerts via RO-003 email system
- **Access Review**: Admin dashboard, manual process quarterly
- **Security Training**: Tracked in SECURITY_TRAINING table, certificates issued
- **Incident Response**: SECURITY_INCIDENT table, alerting via PagerDuty/email
- **Auditing**: All security events logged to AUDITORIA table (already implemented)

---

## 23. Scope Management & Change Control

**Purpose**: Prevent scope creep and maintain project focus through rigorous change control processes.

**Context**: RP-001 Mitigation - Addressing the high-probability risk (70%) of scope creep during development. Without formal change control, feature requests can derail delivery timelines and compromise core functionality quality.

**Risk Mitigation for RP-001**: Alcance Extendido (Scope Creep)

### Strategy Components

**1. Requirements Baseline & Contract**:
- spec.md as formal requirements contract signed off by stakeholders
- Version-controlled with git (every change tracked)
- Major version increment for scope changes (v1.0 → v2.0)
- Minor version for clarifications (v1.0 → v1.1)
- Frozen baseline at project kickoff - no changes without formal CR

**2. Formal Change Request Process**:
- Mandatory CR template (docs/templates/change-request.md) - already created in RN-002
- Change Advisory Board (CAB): PM + PO + Tech Lead + Stakeholder rep
- Impact assessment required: scope, timeline, resources, risks
- MoSCoW prioritization: Must have, Should have, Could have, Won't have
- Decision logged with rationale in CHANGE_REQUEST table

**3. Backlog Segmentation**:
- **Phase 1 (MVP)**: P1 user stories only (critical for launch)
- **Phase 2 (Enhancements)**: P2-P3 user stories (post-launch)
- **Phase 3 (Future)**: P4-P5 user stories (roadmap items)
- Out-of-scope features documented in spec.md "Excluded Features" section
- "Backlog de Fase 2" maintained separately for future consideration

**4. Sprint Planning & Velocity Tracking**:
- Fixed sprint capacity (no overcommitment)
- Velocity tracking per sprint (story points completed)
- Burn-down chart reviewed daily
- Scope changes trigger immediate re-planning
- Buffer: 20% capacity reserved for emergent work (already in RN-002)

**5. Stakeholder Communication Protocol**:
- Sprint review every 2 weeks - demo working software
- Transparent backlog visibility (public board)
- Monthly roadmap review with stakeholders
- "No surprises" policy: communicate scope risks early
- Feature flag approach for optional features (already in RN-002)

**6. Definition of Done (DoD)**:
- Unit tests written and passing
- Integration tests passing
- Code reviewed and approved
- Documentation updated
- Acceptance criteria met
- No known critical bugs
- Deployed to staging and smoke tested

**7. Scope Freeze Periods**:
- 2 weeks before each major release: scope freeze (bug fixes only)
- UAT phase: no new features, only defect fixes
- Critical periods (end-of-month): no deployments
- Emergency changes: only P1 incidents

**8. Feature Flags for Scope Flexibility**:
- New features behind feature flags (FEATURE_FLAG table - RN-002)
- Can be enabled/disabled without deployment
- Allows "soft launch" to subset of users
- Reduces scope pressure ("we can add it post-launch with flag")

**9. Out-of-Scope Documentation**:
- Maintain "Deferred Features" section in spec.md
- Document why each feature was excluded (rationale)
- Link to GitHub issue for future consideration
- Prevents re-discussion of rejected features

**10. Change Impact Visualization**:
- Gantt chart updated for every approved CR
- Critical path analysis: what gets delayed?
- Resource allocation impact shown
- Risk register updated with new risks introduced

### Implementation Details

**CHANGE_REQUEST Table**:
```sql
CREATE TABLE CHANGE_REQUEST (
    cr_id SERIAL PRIMARY KEY,
    cr_number VARCHAR(20) UNIQUE NOT NULL, -- e.g., 'CR-2025-001'
    title VARCHAR(200) NOT NULL,
    description TEXT NOT NULL,
    requested_by INTEGER REFERENCES USUARIO(usuario_id),
    requested_date DATE DEFAULT CURRENT_DATE,

    -- Impact Assessment
    scope_impact VARCHAR(20) NOT NULL, -- 'small', 'medium', 'large'
    timeline_impact_days INTEGER, -- Estimated delay in days
    resource_impact TEXT, -- e.g., "Requires 2 developers for 3 days"
    risk_level VARCHAR(20), -- 'low', 'medium', 'high'

    -- Prioritization
    moscow_priority VARCHAR(20), -- 'must', 'should', 'could', 'wont'
    business_justification TEXT NOT NULL,

    -- Decision
    status VARCHAR(20) DEFAULT 'submitted', -- 'submitted', 'under_review', 'approved', 'rejected', 'deferred'
    decision_date DATE,
    decision_by INTEGER REFERENCES USUARIO(usuario_id),
    decision_rationale TEXT,

    -- Implementation
    target_phase VARCHAR(20), -- 'phase1_mvp', 'phase2_enhancements', 'phase3_future'
    assigned_to INTEGER REFERENCES USUARIO(usuario_id),
    implemented_date DATE
);

CREATE INDEX idx_cr_status ON CHANGE_REQUEST(status);
CREATE INDEX idx_cr_requested ON CHANGE_REQUEST(requested_date DESC);
```

**Change Request Workflow**:
```csharp
public class ChangeRequestService : IChangeRequestService
{
    private readonly ApplicationDbContext _db;
    private readonly IEmailService _emailService;

    public async Task<string> SubmitChangeRequest(ChangeRequestDto dto, int requestedBy)
    {
        // Generate CR number
        var year = DateTime.Now.Year;
        var count = await _db.ChangeRequests.CountAsync(cr => cr.CrNumber.StartsWith($"CR-{year}"));
        var crNumber = $"CR-{year}-{(count + 1):D3}";

        var cr = new ChangeRequest
        {
            CrNumber = crNumber,
            Title = dto.Title,
            Description = dto.Description,
            RequestedBy = requestedBy,
            ScopeImpact = dto.ScopeImpact,
            TimelineImpactDays = dto.TimelineImpactDays,
            ResourceImpact = dto.ResourceImpact,
            RiskLevel = dto.RiskLevel,
            MoscowPriority = dto.MoscowPriority,
            BusinessJustification = dto.BusinessJustification,
            Status = "submitted"
        };

        _db.ChangeRequests.Add(cr);
        await _db.SaveChangesAsync();

        // Notify CAB members
        await _emailService.SendEmail(
            to: new[] { "pm@ssc.mx", "po@ssc.mx", "techlead@ssc.mx" },
            subject: $"New Change Request: {crNumber} - {dto.Title}",
            body: $"A new change request requires review. Impact: {dto.ScopeImpact}, Timeline: +{dto.TimelineImpactDays} days."
        );

        return crNumber;
    }

    public async Task<bool> ReviewChangeRequest(string crNumber, string decision, string rationale, int reviewedBy)
    {
        var cr = await _db.ChangeRequests.FirstOrDefaultAsync(c => c.CrNumber == crNumber);
        if (cr == null) return false;

        cr.Status = decision; // 'approved', 'rejected', 'deferred'
        cr.DecisionDate = DateOnly.FromDateTime(DateTime.Now);
        cr.DecisionBy = reviewedBy;
        cr.DecisionRationale = rationale;

        await _db.SaveChangesAsync();

        // Notify requester
        await _emailService.SendEmail(
            to: new[] { GetUserEmail(cr.RequestedBy) },
            subject: $"Change Request {crNumber} - {decision.ToUpper()}",
            body: $"Your change request '{cr.Title}' has been {decision}.\n\nRationale: {rationale}"
        );

        return true;
    }
}
```

**Sprint Velocity Tracking**:
```csharp
public class VelocityTrackingService
{
    public class SprintMetrics
    {
        public int SprintNumber { get; set; }
        public int PlannedStoryPoints { get; set; }
        public int CompletedStoryPoints { get; set; }
        public int CarriedOver { get; set; }
        public double VelocityPercentage => PlannedStoryPoints > 0
            ? (CompletedStoryPoints * 100.0 / PlannedStoryPoints)
            : 0;
    }

    public async Task<List<SprintMetrics>> GetVelocityTrend(int lastNSprints = 5)
    {
        // Retrieve from SPRINT_METRICS table (to be created)
        var metrics = await _db.SprintMetrics
            .OrderByDescending(s => s.SprintNumber)
            .Take(lastNSprints)
            .Select(s => new SprintMetrics
            {
                SprintNumber = s.SprintNumber,
                PlannedStoryPoints = s.PlannedStoryPoints,
                CompletedStoryPoints = s.CompletedStoryPoints,
                CarriedOver = s.CarriedOver
            })
            .ToListAsync();

        return metrics.OrderBy(m => m.SprintNumber).ToList();
    }

    public async Task<string> GetVelocityHealthStatus()
    {
        var last3Sprints = await GetVelocityTrend(3);
        var avgVelocity = last3Sprints.Average(s => s.VelocityPercentage);

        if (avgVelocity >= 85) return "healthy"; // Green
        if (avgVelocity >= 70) return "warning"; // Yellow - scope creep likely
        return "critical"; // Red - serious scope issues
    }
}
```

**Out-of-Scope Documentation Example** (spec.md):
```markdown
## Excluded Features (Out of Scope for Phase 1)

The following features were considered but explicitly excluded from Phase 1 (MVP) scope:

### ❌ Advanced Reporting Analytics Dashboard
**Rationale**: Complex visualization requirements would delay MVP by 4 weeks. Deferred to Phase 2.
**Related CR**: CR-2025-012 (rejected for Phase 1, approved for Phase 2)
**GitHub Issue**: #45

### ❌ Mobile Native Apps (iOS/Android)
**Rationale**: Blazor Server already provides mobile-responsive web UI. Native apps add significant development effort (8-10 weeks) with marginal benefit.
**Related CR**: CR-2025-008 (rejected)
**Alternative**: Progressive Web App (PWA) approach in Phase 3

### ❌ Integration with External GIS Systems
**Rationale**: No existing GIS system at SSC. Integration would be speculative. Deferred pending GIS procurement decision.
**Related CR**: CR-2025-015 (deferred pending external dependency)
```

**Change Advisory Board (CAB) Meeting Template**:
```markdown
# CAB Meeting - [Date]

**Attendees**: PM, PO, Tech Lead, Stakeholder Rep

## Change Requests for Review

### CR-2025-001: Add Report Approval Workflow
- **Requested by**: Director SSC
- **Impact**: Medium (3 weeks delay)
- **MoSCoW**: Should have
- **CAB Decision**: **APPROVED** for Phase 1
- **Rationale**: Aligns with institutional policy update. Critical for compliance.
- **Action**: Add to Sprint 5 backlog, re-plan timeline

### CR-2025-002: Export to Excel (in addition to PDF/JSON)
- **Requested by**: REVISOR users
- **Impact**: Small (1 week)
- **MoSCoW**: Could have
- **CAB Decision**: **DEFERRED** to Phase 2
- **Rationale**: PDF/JSON cover current needs. Nice-to-have, not blocker.
- **Action**: Add to Phase 2 backlog

### CR-2025-003: Real-time Notifications via Push
- **Requested by**: Tech enthusiast stakeholder
- **Impact**: Large (5 weeks, complex infrastructure)
- **MoSCoW**: Won't have (Phase 1)
- **CAB Decision**: **REJECTED** for Phase 1
- **Rationale**: Email notifications sufficient. Significant complexity for marginal benefit.
- **Action**: Document in "Excluded Features"
```

### Validation & Success Metrics

**Scope Control Metrics**:
- Change requests submitted per month: tracking
- Change request approval rate: <30% (most should be deferred/rejected)
- Scope drift percentage: <10% of original story points
- Sprint velocity stability: ±15% variance max

**Process Metrics**:
- CR turnaround time (submission → decision): <5 business days
- CAB meeting frequency: bi-weekly (aligned with sprint boundaries)
- Out-of-scope documentation completeness: 100%

**Project Health**:
- On-time delivery confidence: >80%
- Feature completeness (DoD met): 100%
- Technical debt introduced by scope changes: <10 story points per sprint

### Integration Points

- **Change Requests**: Tracked in CHANGE_REQUEST table, UI in Admin module
- **Sprint Metrics**: SPRINT_METRICS table (to be created), velocity dashboard
- **Feature Flags**: FEATURE_FLAG table (already in RN-002) for scope flexibility
- **Stakeholder Communication**: Sprint reviews, monthly roadmap sessions (RN-002)
- **Documentation**: spec.md maintained as living document with versioning (RN-002)

---

## 24. Knowledge Management & Technical Onboarding

**Purpose**: Mitigate risk of knowledge gaps in new technologies through structured learning and documentation.

**Context**: RP-002 Mitigation - Addressing medium-probability risk (55%) of team inexperience with Blazor Server, PostgreSQL 18, or .NET ASPIRE causing implementation delays and errors.

**Risk Mitigation for RP-002**: Falta de Conocimiento en Tecnologías Nuevas

### Strategy Components

**1. Technical Spike Week (Week 1)**:
- Dedicate first week to proof-of-concepts and exploration
- Each developer builds small spike project with new tech stack
- Spike deliverables: working code + learnings document
- Topics: Blazor Server SSR, PostgreSQL advanced features, .NET ASPIRE
- Time-boxed: max 2 days per spike, then share findings

**2. Pair Programming Protocol**:
- Mandatory pairing for first 2 weeks of development
- Rotate pairs daily to spread knowledge
- Senior-junior pairing for knowledge transfer
- Pair on complex features (authentication, real-time updates, advanced queries)
- Retrospective after each pairing session

**3. Curated Learning Resources**:
- Create docs/learning-resources.md with approved materials
- Official documentation: Microsoft Learn, PostgreSQL docs
- Video tutorials: Blazor University, PostgreSQL Performance
- Books: "Blazor in Action", "PostgreSQL Up and Running"
- Code samples: Blazorise component library examples
- Budget for online courses (Pluralsight, Udemy)

**4. Internal Knowledge Base**:
- Wiki or Notion workspace for team knowledge
- "How-To" guides for common patterns
- Troubleshooting FAQs (common errors and solutions)
- Architecture Decision Records (ADRs) in docs/adr/
- Code snippets repository (approved patterns)
- Runbooks for operational tasks (already in RN-004)

**5. Weekly Knowledge Sharing Sessions**:
- 1-hour session every Friday
- Rotating presenter (each developer presents once per month)
- Topics: new learnings, solved challenges, best practices
- Recorded for future reference
- Q&A encouraged

**6. Code Review as Learning Tool**:
- Mandatory code review for all PRs (2 approvals minimum)
- Review checklist includes "knowledge transfer" item
- Reviewers add educational comments, not just corrections
- "Teaching moments" highlighted in reviews
- Anti-patterns documented with explanation

**7. External Expert Consultation**:
- Budget for consulting days (e.g., 5 days across project)
- Engage for architecture review (week 2)
- Code review deep-dive (mid-project)
- Performance optimization guidance (pre-launch)
- On-call for critical questions (Slack channel with expert)

**8. Community Engagement**:
- Active participation in Stack Overflow (search before asking)
- GitHub issues for Blazorise, EF Core, Npgsql
- ASP.NET Core Discord/Slack communities
- Local .NET user group meetups (if available)
- Share learnings back to community (blog posts, talks)

**9. Incremental Complexity Approach**:
- Start with simplest features (CRUD operations)
- Gradually introduce complexity (real-time updates, advanced queries)
- Validate learnings at each increment
- Refactor early code as understanding deepens
- "Learning sprints" interspersed with delivery sprints

**10. Post-Mortem Learning**:
- Document bugs caused by knowledge gaps
- Root cause analysis: what didn't we know?
- Update knowledge base with newfound understanding
- Prevent recurrence through pattern documentation
- Celebrate learning moments (blameless culture)

### Implementation Details

**SPIKE_PROJECT Table**:
```sql
CREATE TABLE SPIKE_PROJECT (
    spike_id SERIAL PRIMARY KEY,
    title VARCHAR(200) NOT NULL,
    technology VARCHAR(100) NOT NULL, -- 'Blazor Server', 'PostgreSQL', '.NET ASPIRE'
    developer_FK INTEGER REFERENCES USUARIO(usuario_id),
    started_date DATE,
    completed_date DATE,
    findings_document_url TEXT, -- Link to docs/spikes/spike-NNN.md
    code_repo_url TEXT, -- Link to GitHub repo or branch
    learnings_shared BOOLEAN DEFAULT FALSE,
    CONSTRAINT spike_duration CHECK (completed_date IS NULL OR completed_date >= started_date)
);
```

**ADR (Architecture Decision Record) Template**:
```markdown
# ADR-001: Use Blazor Server over Blazor WebAssembly

**Date**: 2025-11-20
**Status**: Accepted
**Deciders**: Tech Lead, Lead Developer, Architect

## Context
We need to choose between Blazor Server and Blazor WebAssembly for the incident reporting system UI.

## Decision
We will use **Blazor Server** with Server-Side Rendering (SSR).

## Rationale
- **Pros**:
  - Smaller initial download size (no WASM runtime)
  - Full .NET API access on server (no client-side limitations)
  - Better for enterprise intranet scenarios (low latency to server)
  - Easier authentication integration (server-side sessions)
  - Direct database access without API layer

- **Cons**:
  - Requires persistent SignalR connection (bandwidth)
  - Higher server resource usage (per-user state)
  - Offline support requires additional work

## Consequences
- SignalR connection must be stable (mitigated by local intranet deployment)
- Server must handle concurrent connections (monitored via RO-002)
- Offline editing not supported in Phase 1 (acceptable per stakeholder confirmation)

## Alternatives Considered
- **Blazor WebAssembly**: Rejected due to larger initial payload and API complexity
- **MVC + Razor Pages**: Rejected due to lack of rich interactivity requirements

## References
- [Blazor hosting models comparison](https://learn.microsoft.com/en-us/aspnet/core/blazor/hosting-models)
- Stakeholder meeting notes 2025-11-15
```

**Knowledge Sharing Session Tracker**:
```csharp
public class KnowledgeSessionService
{
    public class KnowledgeSession
    {
        public int SessionId { get; set; }
        public DateTime Date { get; set; }
        public string Topic { get; set; }
        public int PresenterId { get; set; }
        public string PresenterName { get; set; }
        public string RecordingUrl { get; set; }
        public string SlidesUrl { get; set; }
        public List<string> KeyTakeaways { get; set; }
        public int AttendeesCount { get; set; }
    }

    public async Task<int> ScheduleSession(string topic, int presenterId, DateTime date)
    {
        var session = new KnowledgeSession
        {
            Topic = topic,
            PresenterId = presenterId,
            Date = date
        };

        // Save to database, send calendar invite to team
        // ...
        return session.SessionId;
    }

    public async Task<List<KnowledgeSession>> GetUpcomingSessions()
    {
        // Return next 4 weeks of scheduled sessions
        return await _db.KnowledgeSessions
            .Where(s => s.Date >= DateTime.Today && s.Date <= DateTime.Today.AddDays(28))
            .OrderBy(s => s.Date)
            .ToListAsync();
    }
}
```

**Learning Resources Document Example**:
```markdown
# Learning Resources - Ceiba Project

## Blazor Server

### Official Documentation
- [Blazor documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/) - Start here
- [Blazor Server hosting model](https://learn.microsoft.com/en-us/aspnet/core/blazor/hosting-models)
- [Blazor component lifecycle](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/lifecycle)

### Video Courses
- [Blazor Fundamentals (Pluralsight)](https://www.pluralsight.com/courses/blazor-fundamentals) - 4 hours
- [Building Web Apps with Blazor (LinkedIn Learning)](https://www.linkedin.com/learning/building-web-applications-with-blazor)

### Books
- "Blazor in Action" by Chris Sainty (Manning, 2022) - Comprehensive guide
- "Blazor WebAssembly by Example" by Toi B. Wright (Packt, 2023)

### Component Library (Blazorise)
- [Blazorise documentation](https://blazorise.com/docs) - We use this for UI components
- [Blazorise DataGrid examples](https://blazorise.com/docs/components/datagrid)
- [Blazorise Forms validation](https://blazorise.com/docs/components/validation)

## PostgreSQL 18

### Official Documentation
- [PostgreSQL 18 documentation](https://www.postgresql.org/docs/18/) - Reference manual
- [PostgreSQL tutorial](https://www.postgresqltutorial.com/) - Interactive learning

### Video Courses
- [PostgreSQL Performance Tuning (Udemy)](https://www.udemy.com/course/postgresql-performance-tuning/)
- [Mastering PostgreSQL (Coursera)](https://www.coursera.org/learn/postgresql-database-management)

### Books
- "PostgreSQL Up and Running" (O'Reilly) - Quick start
- "The Art of PostgreSQL" by Dimitri Fontaine - Advanced techniques

### Specific Topics
- [Full-Text Search](https://www.postgresql.org/docs/18/textsearch.html) - For RT-002 mitigation
- [JSONB](https://www.postgresql.org/docs/18/datatype-json.html) - For RT-004 extensibility
- [Indexes](https://www.postgresql.org/docs/18/indexes.html) - Performance optimization

## .NET ASPIRE

### Official Documentation
- [.NET ASPIRE overview](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview)
- [.NET ASPIRE local development](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/setup-tooling)

### Video Tutorials
- [.NET ASPIRE for Cloud-Native Apps (YouTube)](https://www.youtube.com/results?search_query=.NET+ASPIRE)
- [.NET Conf sessions on ASPIRE](https://www.dotnetconf.net/)

## Code Samples & Patterns

### Internal Repository
- `/docs/code-samples/blazor-crud-pattern.md` - Standard CRUD with DataGrid
- `/docs/code-samples/validation-pattern.md` - FluentValidation setup
- `/docs/code-samples/service-layer-pattern.md` - Repository pattern with EF Core

### External Examples
- [Blazor samples (Microsoft)](https://github.com/dotnet/blazor-samples)
- [EF Core samples](https://github.com/dotnet/EntityFramework.Docs)
- [Blazorise demo apps](https://github.com/Megabit/Blazorise/tree/master/Demos)

## Community Resources

- **Stack Overflow**: Tag `blazor`, `postgresql`, `entity-framework-core`
- **GitHub Discussions**: [Blazor](https://github.com/dotnet/aspnetcore/discussions), [EF Core](https://github.com/dotnet/efcore/discussions)
- **Discord**: [ASP.NET Core Discord](https://discord.gg/dotnet)
- **Reddit**: r/dotnet, r/PostgreSQL

## Expert Consultants

- **Blazor Expert**: [Contact via consulting firm] - 5 days budgeted
- **PostgreSQL DBA**: [Local consultant] - On-call for performance questions
- **Security Audit**: [Security firm] - Annual penetration test (RN-005)
```

### Validation & Success Metrics

**Learning Metrics**:
- Spike projects completed: 1 per developer in Week 1 (target: 100%)
- Knowledge sharing sessions: 1 per week (target: 100% attendance)
- ADRs documented: 1 per major technical decision (target: >10 ADRs by project end)
- Code review participation: 2 reviews per developer per sprint (target: 100%)

**Quality Metrics**:
- Bugs caused by knowledge gaps: tracking with tag "knowledge-gap"
- Code review findings related to anti-patterns: declining trend
- Time to resolve "how-to" questions: <2 hours (avg)

**Onboarding Metrics**:
- New developer onboarding time: <2 weeks (target)
- Onboarding satisfaction survey: >4/5
- Knowledge base completeness: >80% of common questions documented

### Integration Points

- **Spike Projects**: Tracked in SPIKE_PROJECT table, results in docs/spikes/
- **ADRs**: Stored in docs/adr/, referenced in code comments where applicable
- **Knowledge Sessions**: Calendar invites, recordings in shared drive
- **Learning Resources**: docs/learning-resources.md, updated continuously
- **Code Patterns**: docs/code-samples/, referenced in PR templates
- **External Consultants**: Scheduled via PM, findings integrated into knowledge base

---

## 25. Team Resilience & Knowledge Distribution

**Purpose**: Prevent project disruption from key personnel unavailability through redundancy and documentation.

**Context**: RP-003 Mitigation - Addressing medium-probability risk (40%) of dependency on key individuals (specialist in security, DBA, architect) causing project blockers if they become unavailable.

**Risk Mitigation for RP-003**: Dependencia de Personal Clave

### Strategy Components

**1. Backup Owner System**:
- Every critical area has 2-3 people with competence
- Primary owner + secondary owner (backup) + tertiary (aware)
- Skills matrix maintained showing competency levels
- Backup owner shadows primary owner on complex tasks
- Rotate ownership quarterly to prevent single points of failure

**2. Architecture Decision Records (ADRs)**:
- All major technical decisions documented in docs/adr/
- Template includes: context, decision, rationale, consequences, alternatives
- Indexed by technology area and date
- New team members read all ADRs as onboarding
- ADRs prevent knowledge loss from personnel changes

**3. Comprehensive Code Reviews**:
- Every PR requires 2 approvals (minimum)
- Cross-team reviews (backend reviews frontend code, vice versa)
- Review checklist includes knowledge transfer
- "Bus factor" consideration: is this code understandable to others?
- Reviews create shared understanding of codebase

**4. Runbook Maintenance**:
- Operational procedures documented in docs/runbooks/ (already in RN-004)
- Step-by-step guides for common tasks (deployment, troubleshooting, backup restoration)
- Screenshots included for clarity
- Tested regularly (simulate procedures with different team members)
- Updated immediately when procedures change

**5. Formal Handoff Process**:
- If someone leaves project: mandatory 1-week handoff period
- Handoff checklist: knowledge transfer sessions, documentation review, pending tasks transfer
- Record handoff sessions for team reference
- Update skills matrix with new owner
- Post-handoff follow-up after 2 weeks

**6. Recorded Technical Sessions**:
- Architecture walkthroughs recorded (Loom, Zoom)
- Complex feature deep-dives recorded
- Stored in shared drive with searchable titles
- Indexed by topic and date
- New team members watch key recordings during onboarding

**7. Skills Matrix Tracking**:
- Matrix showing team competency in each technology/area
- Competency levels: 1 (aware), 2 (can assist), 3 (can own), 4 (expert)
- Updated quarterly or when skills change
- Identifies skill gaps requiring training
- Prevents over-dependence on single expert

**8. Pair Programming Rotation**:
- Rotate pairs weekly (not daily, to build deeper understanding)
- Pair on critical path features
- Knowledge transfer is explicit goal (not just code production)
- Junior-senior pairing for learning
- Senior-senior pairing for complex features

**9. Documentation-Driven Development**:
- README.md in every module explaining purpose and architecture
- Inline code comments for non-obvious logic
- API documentation (XML comments for C#)
- Database schema documented with ER diagram + descriptions
- Update docs in same PR as code changes

**10. Cross-Training Program**:
- Dedicate 10% of sprint capacity to cross-training
- Developers work on features outside their primary area
- Monthly "swap day": frontend dev works on backend, etc.
- Encourage curiosity and exploration
- Reduce silos and tribal knowledge

### Implementation Details

**SKILLS_MATRIX Table**:
```sql
CREATE TABLE SKILLS_MATRIX (
    skill_id SERIAL PRIMARY KEY,
    developer_FK INTEGER REFERENCES USUARIO(usuario_id),
    skill_area VARCHAR(100) NOT NULL, -- 'Blazor', 'PostgreSQL', 'Security', 'DevOps', etc.
    competency_level INTEGER CHECK (competency_level BETWEEN 1 AND 4), -- 1=aware, 2=assist, 3=own, 4=expert
    last_updated DATE DEFAULT CURRENT_DATE,
    notes TEXT, -- Optional context
    UNIQUE(developer_FK, skill_area)
);

-- Example data
INSERT INTO SKILLS_MATRIX (developer_FK, skill_area, competency_level, notes) VALUES
(1, 'Blazor Server', 4, 'Primary frontend developer, built onboarding system'),
(1, 'PostgreSQL', 2, 'Can write queries, needs DBA guidance for optimization'),
(1, 'Security', 2, 'Understands OWASP, not experienced with penetration testing'),
(2, 'Blazor Server', 3, 'Can own features, less experience with advanced scenarios'),
(2, 'PostgreSQL', 4, 'DBA expert, optimized all indexes'),
(2, 'Security', 3, 'Implemented authentication, conducted security audit');
```

**Backup Owner Assignment**:
```csharp
public class TeamOwnershipService
{
    public class AreaOwnership
    {
        public string Area { get; set; } // 'Authentication', 'Reporting', 'Database', 'DevOps'
        public int PrimaryOwnerId { get; set; }
        public string PrimaryOwnerName { get; set; }
        public int SecondaryOwnerId { get; set; }
        public string SecondaryOwnerName { get; set; }
        public int? TertiaryOwnerId { get; set; }
        public string TertiaryOwnerName { get; set; }
    }

    public async Task<List<AreaOwnership>> GetOwnershipMatrix()
    {
        // Example: retrieve from configuration or database
        return new List<AreaOwnership>
        {
            new() { Area = "Authentication & RBAC", PrimaryOwnerId = 1, PrimaryOwnerName = "Alice",
                    SecondaryOwnerId = 3, SecondaryOwnerName = "Charlie", TertiaryOwnerId = 2 },
            new() { Area = "Report CRUD & Validation", PrimaryOwnerId = 2, PrimaryOwnerName = "Bob",
                    SecondaryOwnerId = 1, SecondaryOwnerName = "Alice" },
            new() { Area = "Database Schema & Optimization", PrimaryOwnerId = 2, PrimaryOwnerName = "Bob",
                    SecondaryOwnerId = 4, SecondaryOwnerName = "Diana" },
            new() { Area = "DevOps & CI/CD", PrimaryOwnerId = 4, PrimaryOwnerName = "Diana",
                    SecondaryOwnerId = 3, SecondaryOwnerName = "Charlie" },
            new() { Area = "Security & Penetration Testing", PrimaryOwnerId = 3, PrimaryOwnerName = "Charlie",
                    SecondaryOwnerId = 1, SecondaryOwnerName = "Alice" },
        };
    }

    public async Task<int> CalculateBusFactor(string area)
    {
        var ownership = await GetOwnershipMatrix();
        var areaOwnership = ownership.FirstOrDefault(o => o.Area == area);

        if (areaOwnership == null) return 0; // No ownership - critical risk!

        int factor = 0;
        if (areaOwnership.PrimaryOwnerId > 0) factor++;
        if (areaOwnership.SecondaryOwnerId > 0) factor++;
        if (areaOwnership.TertiaryOwnerId.HasValue && areaOwnership.TertiaryOwnerId > 0) factor++;

        return factor; // Target: ≥2 for all critical areas
    }
}
```

**Handoff Checklist Template**:
```markdown
# Handoff Checklist - [Developer Name] → [New Owner]

**Area**: [e.g., Authentication Module]
**Handoff Date**: [Date]
**Handoff Duration**: [e.g., 5 days]

## Knowledge Transfer Sessions
- [ ] Session 1 (2h): Architecture overview, design decisions (ADRs), key components
- [ ] Session 2 (2h): Code walkthrough, critical paths, edge cases
- [ ] Session 3 (2h): Operational procedures, troubleshooting, known issues
- [ ] Session 4 (1h): Pending work, backlog priorities, technical debt
- [ ] Session 5 (1h): Q&A, clarifications, shadowing on live issue

## Documentation Review
- [ ] Read all ADRs related to this area (docs/adr/)
- [ ] Review README.md in module directory
- [ ] Study runbooks (docs/runbooks/)
- [ ] Review recent PRs and code reviews (last 3 months)

## Pending Tasks Transfer
- [ ] In-progress tasks documented in tasks.md or project board
- [ ] Priority order clarified
- [ ] Blockers identified and resolution plan discussed
- [ ] Handover approval from Tech Lead

## Verification
- [ ] New owner can explain architecture to peer (tested)
- [ ] New owner can deploy module independently (tested)
- [ ] New owner can troubleshoot common issues (simulated)
- [ ] Skills matrix updated with new owner at competency level ≥3

## Post-Handoff
- [ ] 2-week check-in: any questions or blockers?
- [ ] Outgoing owner available for async questions (email/Slack) for 1 month
```

**Skills Matrix Dashboard** (for PM/Tech Lead):
```razor
@page "/admin/skills-matrix"
@attribute [Authorize(Roles = "ADMIN")]

<h3>Team Skills Matrix</h3>

<Table>
    <TableHeader>
        <TableRow>
            <TableHeaderCell>Developer</TableHeaderCell>
            <TableHeaderCell>Blazor</TableHeaderCell>
            <TableHeaderCell>PostgreSQL</TableHeaderCell>
            <TableHeaderCell>Security</TableHeaderCell>
            <TableHeaderCell>DevOps</TableHeaderCell>
            <TableHeaderCell>Bus Factor Risk</TableHeaderCell>
        </TableRow>
    </TableHeader>
    <TableBody>
        @foreach (var dev in developers)
        {
            <TableRow>
                <TableRowCell>@dev.Name</TableRowCell>
                <TableRowCell><Badge Color="@GetCompetencyColor(dev.BlazorLevel)">@dev.BlazorLevel</Badge></TableRowCell>
                <TableRowCell><Badge Color="@GetCompetencyColor(dev.PostgresLevel)">@dev.PostgresLevel</Badge></TableRowCell>
                <TableRowCell><Badge Color="@GetCompetencyColor(dev.SecurityLevel)">@dev.SecurityLevel</Badge></TableRowCell>
                <TableRowCell><Badge Color="@GetCompetencyColor(dev.DevOpsLevel)">@dev.DevOpsLevel</Badge></TableRowCell>
                <TableRowCell>@CalculateBusFactorRisk(dev)</TableRowCell>
            </TableRow>
        }
    </TableBody>
</Table>

<h4>Skill Gap Analysis</h4>
<ul>
    @foreach (var gap in skillGaps)
    {
        <li><Badge Color="Color.Warning">@gap.Area</Badge>: Only @gap.ExpertCount expert(s). Recommend cross-training.</li>
    }
</ul>

@code {
    Color GetCompetencyColor(int level) => level switch
    {
        4 => Color.Success, // Expert - green
        3 => Color.Info, // Can own - blue
        2 => Color.Warning, // Can assist - yellow
        1 => Color.Secondary, // Aware - gray
        _ => Color.Danger // No knowledge - red
    };

    string CalculateBusFactorRisk(Developer dev)
    {
        var expertAreas = new[] { dev.BlazorLevel, dev.PostgresLevel, dev.SecurityLevel, dev.DevOpsLevel }
            .Count(level => level == 4);

        if (expertAreas >= 2) return "🔴 High (too concentrated)"; // Single point of failure
        if (expertAreas == 1) return "⚠️ Medium";
        return "✅ Low (distributed knowledge)";
    }
}
```

### Validation & Success Metrics

**Redundancy Metrics**:
- Bus factor per critical area: ≥2 people with competency ≥3 (target: 100% of areas)
- Skills matrix coverage: >80% of team has competency ≥2 in at least 3 skill areas
- Single points of failure (competency 4 with no backup ≥3): 0 (target)

**Documentation Metrics**:
- ADRs documented: 1 per major decision (target: >15 by project end)
- Runbook coverage: 100% of operational procedures
- Code documentation coverage: >70% of public APIs have XML comments

**Knowledge Transfer Metrics**:
- Handoff completion rate: 100% (all handoffs follow checklist)
- Post-handoff blocker rate: <10% (new owner can operate independently)
- Cross-training participation: >50% of developers per quarter

**Team Resilience**:
- Unplanned absence impact: project continues without delay (target: <2 days delay per absence)
- Knowledge sessions attendance: >80%
- Code review participation: 100% (everyone reviews and is reviewed)

### Integration Points

- **Skills Matrix**: SKILLS_MATRIX table, dashboard in Admin module
- **ADRs**: docs/adr/, template in docs/templates/adr-template.md
- **Handoff Process**: Checklist in docs/templates/handoff-checklist.md
- **Runbooks**: docs/runbooks/ (already in RN-004)
- **Recorded Sessions**: Shared drive (Google Drive, OneDrive), indexed by topic
- **Ownership Matrix**: Maintained in docs/team-ownership.md, reviewed quarterly

---

## 26. Infrastructure Automation & Reproducibility

**Purpose**: Eliminate delays from infrastructure configuration through automation and early provisioning.

**Context**: RP-004 Mitigation - Addressing medium-probability risk (45%) of delays caused by server configuration, permissions, networking, or Docker issues. Proactive infrastructure setup prevents deployment blockers.

**Risk Mitigation for RP-004**: Retrasos en Configuración de Infraestructura

### Strategy Components

**1. Pre-Development Infrastructure Setup**:
- Provision Fedora Linux 42 server BEFORE development starts (Week 0)
- Complete base configuration: OS updates, firewall, users, SSH keys
- Install Docker, Docker Compose, Git, and required tools
- Verify network connectivity and DNS resolution
- Document server specifications in docs/infrastructure.md

**2. Infrastructure-as-Code (IaC)**:
- All server configuration scripted (Bash + Ansible if needed)
- Scripts stored in repository: scripts/setup/
- Idempotent scripts (can run multiple times safely)
- Version-controlled alongside application code
- Enables rapid environment recreation

**3. Automated Setup Scripts**:
- `scripts/setup/01-os-baseline.sh`: OS updates, firewall, SELinux
- `scripts/setup/02-docker-install.sh`: Docker + Docker Compose installation
- `scripts/setup/03-postgresql-setup.sh`: PostgreSQL container configuration
- `scripts/setup/04-app-deployment.sh`: Application container deployment
- `scripts/setup/05-ssl-certificates.sh`: TLS certificate setup (Let's Encrypt)
- Scripts tested on clean Fedora 42 VM

**4. .NET ASPIRE for Local Development**:
- Developers use .NET ASPIRE locally (no server dependency for development)
- ASPIRE provides PostgreSQL container, app orchestration, service discovery
- Consistent dev environment across all machines
- Fast inner loop (F5 debugging works immediately)
- Server only needed for integration testing and deployment

**5. CI/CD Pipeline (Early Implementation)**:
- Set up GitHub Actions CI/CD in Week 2 (before significant code)
- Automated tests run on every PR
- Automated deployment to staging on merge to main
- Catches deployment issues early (not at go-live)
- Smoke tests validate deployment success

**6. Staging Environment Parity**:
- Staging environment identical to production
- Same OS version, Docker version, PostgreSQL version
- Same resource limits (CPU, RAM)
- Same network configuration (firewall rules, ports)
- "If it works in staging, it works in production"

**7. Infrastructure Documentation**:
- docs/infrastructure.md: server specs, network topology, access instructions
- docs/quickstart.md: how to set up local dev environment (ASPIRE)
- docs/deployment.md: step-by-step deployment guide with screenshots
- docs/troubleshooting.md: common infrastructure issues and solutions
- docs/runbooks/: operational procedures (already in RN-004)

**8. Weekly Deployment Smoke Tests**:
- Deploy to staging every week (even with minimal changes)
- Validate deployment process works
- Catches infrastructure drift early
- Builds confidence in deployment procedure
- Documented checklist executed each time

**9. Environment Configuration Management**:
- Environment variables for all configuration (never hardcode)
- `.env.example` file in repository showing all required variables
- Secrets managed securely (never committed to git)
- Docker Compose uses `.env` file
- Consistent configuration across environments

**10. Rollback Strategy**:
- Docker images tagged with version (not `latest`)
- Previous version kept for quick rollback
- Rollback tested in staging before prod deployment
- Documented rollback procedure in runbooks (RN-004)
- Target: rollback in <15 minutes (RN-004, RO-005)

### Implementation Details

**Automated OS Baseline Setup Script**:
```bash
#!/bin/bash
# scripts/setup/01-os-baseline.sh
# Purpose: Configure Fedora 42 baseline (firewall, SELinux, updates)

set -euo pipefail

echo "=== Ceiba Incident Reporting - OS Baseline Setup ==="

# Update system packages
echo "[1/5] Updating system packages..."
sudo dnf update -y

# Configure firewall
echo "[2/5] Configuring firewall..."
sudo firewall-cmd --permanent --add-service=http
sudo firewall-cmd --permanent --add-service=https
sudo firewall-cmd --permanent --add-port=22/tcp # SSH
sudo firewall-cmd --reload

# SELinux permissive (for Docker volumes, adjust as needed)
echo "[3/5] Configuring SELinux..."
sudo setenforce 0
sudo sed -i 's/^SELINUX=enforcing/SELINUX=permissive/' /etc/selinux/config

# Create app user
echo "[4/5] Creating application user..."
if ! id "ceiba" &>/dev/null; then
    sudo useradd -m -s /bin/bash ceiba
    sudo usermod -aG docker ceiba # Will add to docker group after Docker install
    echo "User 'ceiba' created"
else
    echo "User 'ceiba' already exists"
fi

# Install basic tools
echo "[5/5] Installing basic tools..."
sudo dnf install -y git curl wget vim htop

echo "✅ OS baseline setup complete!"
```

**Docker Installation Script**:
```bash
#!/bin/bash
# scripts/setup/02-docker-install.sh
# Purpose: Install Docker and Docker Compose on Fedora 42

set -euo pipefail

echo "=== Docker Installation ==="

# Install Docker
echo "[1/4] Installing Docker..."
sudo dnf -y install dnf-plugins-core
sudo dnf config-manager --add-repo https://download.docker.com/linux/fedora/docker-ce.repo
sudo dnf install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

# Start Docker service
echo "[2/4] Starting Docker service..."
sudo systemctl start docker
sudo systemctl enable docker

# Add ceiba user to docker group
echo "[3/4] Adding user to docker group..."
sudo usermod -aG docker ceiba
echo "Re-login required for docker group changes to take effect"

# Verify installation
echo "[4/4] Verifying Docker installation..."
sudo docker run hello-world

# Docker Compose version
docker compose version

echo "✅ Docker installation complete!"
echo "Next: Run 03-postgresql-setup.sh"
```

**PostgreSQL Container Setup Script**:
```bash
#!/bin/bash
# scripts/setup/03-postgresql-setup.sh
# Purpose: Configure PostgreSQL 18 container with persistence

set -euo pipefail

echo "=== PostgreSQL 18 Setup ==="

# Create data directory
echo "[1/5] Creating PostgreSQL data directory..."
sudo mkdir -p /var/lib/postgresql/data
sudo chown -R 999:999 /var/lib/postgresql/data # PostgreSQL container UID:GID

# Generate strong password (store securely!)
echo "[2/5] Generating database password..."
DB_PASSWORD=$(openssl rand -base64 32)
echo "POSTGRES_PASSWORD=${DB_PASSWORD}" | sudo tee /opt/ceiba/.env.postgres
sudo chmod 600 /opt/ceiba/.env.postgres
echo "Password stored in /opt/ceiba/.env.postgres (keep secure!)"

# Create docker-compose.yml for PostgreSQL
echo "[3/5] Creating Docker Compose configuration..."
cat <<EOF | sudo tee /opt/ceiba/docker-compose.postgres.yml
version: '3.8'
services:
  postgres:
    image: postgres:18
    container_name: ceiba-postgres
    restart: unless-stopped
    environment:
      POSTGRES_DB: ceiba
      POSTGRES_USER: ceiba
      POSTGRES_PASSWORD: \${POSTGRES_PASSWORD}
    volumes:
      - /var/lib/postgresql/data:/var/lib/postgresql/data
    ports:
      - "5432:5432"
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ceiba"]
      interval: 30s
      timeout: 5s
      retries: 3
EOF

# Start PostgreSQL
echo "[4/5] Starting PostgreSQL container..."
cd /opt/ceiba
sudo docker compose -f docker-compose.postgres.yml --env-file .env.postgres up -d

# Wait for healthy
echo "[5/5] Waiting for PostgreSQL to be healthy..."
timeout 60s bash -c 'until sudo docker exec ceiba-postgres pg_isready -U ceiba; do sleep 2; done'

echo "✅ PostgreSQL 18 is running!"
echo "Connection string: Host=localhost;Port=5432;Database=ceiba;Username=ceiba;Password=<from .env.postgres>"
```

**.NET ASPIRE Local Development Configuration**:
```csharp
// AppHost/Program.cs (.NET ASPIRE orchestration)
var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL resource for local development
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin() // Optional: PgAdmin UI for local dev
    .AddDatabase("ceiba");

// Ceiba web app
var ceibaApp = builder.AddProject<Projects.Ceiba_Web>("ceiba-web")
    .WithReference(postgres);

builder.Build().Run();
```

**Deployment Smoke Test Checklist**:
```markdown
# Deployment Smoke Test - [Date]

**Environment**: Staging
**Deployer**: [Name]
**Version**: [Git commit SHA or tag]

## Pre-Deployment
- [ ] Backup database (`scripts/backup.sh`)
- [ ] Verify staging environment health (`curl https://staging.ceiba.ssc.mx/health`)
- [ ] Review changes since last deployment (`git log --oneline`)

## Deployment Steps
- [ ] Pull latest code from `main` branch
- [ ] Build Docker image (`docker compose build`)
- [ ] Tag image with version (`docker tag ceiba:latest ceiba:v1.2.3`)
- [ ] Stop running containers (`docker compose down`)
- [ ] Start new containers (`docker compose up -d`)
- [ ] Wait for health check to pass (60s max)

## Smoke Tests
- [ ] Homepage loads (HTTPS): `curl -I https://staging.ceiba.ssc.mx`
- [ ] Login page accessible: Manual test
- [ ] Authentication works: Login as test user
- [ ] Database connection healthy: Check `/health` endpoint
- [ ] Create test report: CREADOR flow end-to-end
- [ ] Export PDF: Download test report as PDF
- [ ] Admin dashboard loads: ADMIN user login
- [ ] No errors in logs: `docker logs ceiba-web --tail 100`

## Post-Deployment
- [ ] Deployment time recorded: ____ minutes
- [ ] Any issues encountered: [None / Details]
- [ ] Rollback tested (optional): [Yes / No / N/A]
- [ ] Team notified in Slack: [Yes]

## Sign-Off
- [ ] Smoke test PASSED - Ready for production
- [ ] Smoke test FAILED - Issues to resolve: [Details]

**Signed**: [Name], [Date]
```

**Environment Configuration Example** (`.env.example`):
```bash
# .env.example - Copy to .env and fill in actual values
# DO NOT commit .env to git (listed in .gitignore)

# Database
POSTGRES_PASSWORD=<generate_strong_password>
ConnectionStrings__DefaultConnection=Host=postgres;Database=ceiba;Username=ceiba;Password=${POSTGRES_PASSWORD}

# Application
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080

# External Services
AI_SERVICE_API_KEY=<openai_or_gemini_api_key>
EMAIL_SMTP_HOST=smtp.example.com
EMAIL_SMTP_PORT=587
EMAIL_SMTP_USERNAME=noreply@ssc.mx
EMAIL_SMTP_PASSWORD=<smtp_password>

# Security
JWT_SECRET_KEY=<generate_random_256bit_key>
DATA_PROTECTION_KEY_PATH=/var/lib/ceiba/dataprotection-keys

# Feature Flags (optional overrides)
FEATURE_FLAG__ENABLE_AI_REPORTS=true
FEATURE_FLAG__ENABLE_PILOT_MODE=false
```

### Validation & Success Metrics

**Setup Time Metrics**:
- New environment setup time (clean VM → working app): <4 hours
- Developer onboarding time (laptop setup with ASPIRE): <30 minutes
- Deployment time (staging): <10 minutes
- Rollback time: <15 minutes (target from RO-005)

**Reliability Metrics**:
- Deployment success rate (staging): >95%
- Discrepancies between staging and production: 0 (infrastructure parity)
- Infrastructure-related bugs in production: <2 per release

**Documentation Metrics**:
- Infrastructure documentation completeness: 100% (all procedures documented)
- Setup script success rate on clean VM: 100% (tested quarterly)
- Runbook coverage: 100% of operational procedures (RN-004)

### Integration Points

- **Setup Scripts**: scripts/setup/, executable and idempotent
- **.NET ASPIRE**: AppHost project, local development orchestration
- **CI/CD**: GitHub Actions workflows (.github/workflows/), automated testing & deployment
- **Staging Environment**: Parallel to production, maintained by DevOps
- **Documentation**: docs/infrastructure.md, docs/quickstart.md, docs/deployment.md
- **Runbooks**: docs/runbooks/ (already in RN-004) for operational procedures

---
