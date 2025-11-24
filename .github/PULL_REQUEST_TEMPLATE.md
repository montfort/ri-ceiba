# Pull Request - Ceiba Incident Reporting System

## Description
<!-- Provide a brief description of the changes in this PR -->

## Type of Change
- [ ] Bug fix (non-breaking change which fixes an issue)
- [ ] New feature (non-breaking change which adds functionality)
- [ ] Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] Database migration (includes schema changes)
- [ ] Security enhancement
- [ ] Documentation update
- [ ] Configuration change

## Related Tasks
<!-- Link to tasks.md items (e.g., T042, US1-T003) -->
- Closes:

## Testing Performed
- [ ] Unit tests added/updated
- [ ] Integration tests added/updated
- [ ] Component tests added/updated (Blazor)
- [ ] E2E tests added/updated
- [ ] Manual testing performed
- [ ] All tests passing locally

## Security Checklist (T020e - RS-001 to RS-006)

### OWASP Top 10 Verification
- [ ] **A01:2021 - Broken Access Control**
  - [ ] Authorization checks implemented for all protected resources
  - [ ] Role-based permissions verified (CREADOR, REVISOR, ADMIN)
  - [ ] Horizontal privilege escalation prevented (users can't access others' data)
  - [ ] Vertical privilege escalation prevented (role boundaries enforced)

- [ ] **A02:2021 - Cryptographic Failures**
  - [ ] Sensitive data encrypted in transit (HTTPS enforced)
  - [ ] Passwords hashed with ASP.NET Identity (not stored in plain text)
  - [ ] No sensitive data in logs (PII redacted by PIIRedactionEnricher)
  - [ ] Connection strings in secure configuration (not hardcoded)

- [ ] **A03:2021 - Injection**
  - [ ] **NO raw SQL concatenation** (parameterized queries or EF Core only)
  - [ ] User input validated and sanitized
  - [ ] LINQ queries used instead of raw SQL
  - [ ] No dynamic LINQ with user input

- [ ] **A04:2021 - Insecure Design**
  - [ ] Security requirements reviewed against FR-001 to FR-007
  - [ ] Threat model considered for new features
  - [ ] Least privilege principle applied

- [ ] **A05:2021 - Security Misconfiguration**
  - [ ] No default credentials in production configuration
  - [ ] Security headers configured (CSP, HSTS, X-Frame-Options)
  - [ ] Error messages don't leak sensitive information
  - [ ] Development-only features disabled in production

- [ ] **A06:2021 - Vulnerable and Outdated Components**
  - [ ] All NuGet packages up to date
  - [ ] No known vulnerabilities in dependencies
  - [ ] .NET 10 latest patch version used

- [ ] **A07:2021 - Identification and Authentication Failures**
  - [ ] Password policy enforced (min 10 chars, uppercase + digit)
  - [ ] Session timeout implemented (30 minutes - FR-005)
  - [ ] User-Agent validation active (RS-005)
  - [ ] Anti-CSRF tokens used on forms

- [ ] **A08:2021 - Software and Data Integrity Failures**
  - [ ] Database migrations include rollback procedures
  - [ ] Pre-migration backups created (MigrationBackupService)
  - [ ] No unsigned or unverified code execution

- [ ] **A09:2021 - Security Logging and Monitoring Failures**
  - [ ] Critical operations logged to audit table (RegistroAuditoria)
  - [ ] Failed authentication attempts logged
  - [ ] Authorization failures logged (AuthorizationLoggingMiddleware)
  - [ ] Logs include sufficient context (user, IP, timestamp, action)
  - [ ] No PII in logs (verified by PIIRedactionEnricher)

- [ ] **A10:2021 - Server-Side Request Forgery (SSRF)**
  - [ ] No user-controlled URLs in HTTP requests
  - [ ] External API calls validated and restricted

### Input Validation (RS-004 Mitigation)
- [ ] All user inputs validated on server side
- [ ] Client-side validation is supplementary only
- [ ] Data annotations used on DTOs/models
- [ ] String length limits enforced
- [ ] Numeric ranges validated
- [ ] Date ranges validated
- [ ] File uploads validated (type, size, content)
- [ ] Special characters handled safely

### SQL Injection Prevention (Critical)
- [ ] **ZERO raw SQL string concatenation with user input**
- [ ] All database queries use EF Core LINQ or parameterized queries
- [ ] `FromSqlRaw` only used with parameters (`{0}` placeholders)
- [ ] No dynamic table/column names from user input
- [ ] Verified by Roslyn analyzer (no warnings)

### Cross-Site Scripting (XSS) Prevention
- [ ] Blazor automatic escaping maintained (no `@((MarkupString)userInput)`)
- [ ] User-generated content sanitized before display
- [ ] CSP headers configured in Program.cs
- [ ] No `dangerouslySetInnerHTML` equivalents

### Authentication & Authorization
- [ ] Endpoints protected with `[Authorize]` attribute or policy
- [ ] Anonymous access explicitly marked with `[AllowAnonymous]`
- [ ] Role requirements verified (RequireCreadorRole, RequireRevisorRole, RequireAdminRole)
- [ ] Current user context correctly retrieved (HttpContextAccessor)
- [ ] Session hijacking mitigated (UserAgentValidationMiddleware active)

### Audit Logging (RS-001 Mitigation)
- [ ] All data modifications logged (automatic via AuditSaveChangesInterceptor)
- [ ] Manual audit entries created for business operations (IAuditService)
- [ ] Audit logs include: UserId, ActionCode, IP, Timestamp, Details
- [ ] Failed operations logged (not just successful ones)
- [ ] Audit logs immutable (no UPDATE or DELETE on RegistroAuditoria)

### Sensitive Data Handling (RS-003 Mitigation)
- [ ] No passwords, API keys, or secrets in code
- [ ] Secrets stored in environment variables or Azure Key Vault
- [ ] PII redacted from logs (email, IP, CURP, phone numbers)
- [ ] Database backups secured and encrypted
- [ ] No sensitive data in error messages shown to users

### Configuration Security
- [ ] Feature flags used instead of code changes (FeatureFlags configuration)
- [ ] Database connection strings in appsettings (not hardcoded)
- [ ] Development settings not deployed to production
- [ ] CORS policy configured with specific origins (not `*`)

### Database Changes
- [ ] Migration includes Up and Down methods
- [ ] Migration changelog updated in MIGRATIONS.md
- [ ] Breaking changes documented
- [ ] Data migration scripts tested on copy of production data
- [ ] Indexes created for new query patterns
- [ ] Foreign key constraints verified

### Code Quality
- [ ] No compiler errors
- [ ] No critical analyzer warnings (CA, IDE rules)
- [ ] Code follows C# naming conventions (PascalCase, camelCase, _privateFields)
- [ ] XML documentation on public APIs
- [ ] No unused using statements
- [ ] No commented-out code

### Performance Considerations
- [ ] Database queries optimized (no N+1 queries)
- [ ] Pagination implemented for large result sets
- [ ] Indexes created for frequently queried columns
- [ ] No synchronous database calls in async methods
- [ ] HttpClient used correctly (not new instance per request)

## Constitution Compliance (Non-Negotiable Principles)

- [ ] **Principle I - Modular Design**: Changes contained within module boundaries
- [ ] **Principle II - TDD Mandatory**: Tests written before implementation (Red-Green-Refactor)
- [ ] **Principle III - Security by Design**: Least privilege and OWASP Top 10 addressed
- [ ] **Principle IV - Accessibility**: Mobile-responsive, WCAG Level AA compliant
- [ ] **Principle V - Documentation as Deliverable**: Code documented, README updated if needed

## Risk Assessment (RS-001 to RS-006, RT-001 to RT-006)

### Security Risks Addressed
- [ ] RS-001: Unauthorized access (Authorization policies + logging)
- [ ] RS-002: XSS attacks (CSP headers + Blazor escaping)
- [ ] RS-003: Data exposure in logs (PII redaction)
- [ ] RS-004: Data integrity (Input validation)
- [ ] RS-005: Session hijacking (User-Agent validation + secure cookies)
- [ ] RS-006: SQL injection (Parameterized queries + analyzer)

### Technical Risks Addressed
- [ ] RT-001: Database unavailability (Error handling + retry policies)
- [ ] RT-002: Email delivery failure (Logging + retry mechanism)
- [ ] RT-003: AI service failures (Graceful degradation)
- [ ] RT-004: Deployment errors (Feature flags + migration backups)
- [ ] RT-005: Performance degradation (Indexes + pagination)
- [ ] RT-006: Storage exhaustion (Log retention + backup cleanup)

## Deployment Checklist (if applicable)

- [ ] Database migration tested on staging environment
- [ ] Pre-migration backup script executed
- [ ] Environment variables configured
- [ ] Feature flags set correctly for environment
- [ ] Rollback procedure documented
- [ ] Monitoring alerts configured for new features

## Screenshots (if applicable)
<!-- Add screenshots for UI changes -->

## Additional Notes
<!-- Any additional information reviewers should know -->

---

**Reviewer Notes:**
- Security checklist items marked N/A must include justification
- All security-related PRs require approval from ADMIN role
- Database migrations require backup verification before merge
- Failed security checks block PR merge (non-negotiable)
