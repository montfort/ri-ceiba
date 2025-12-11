# Security Credentials Guide

This document describes the security improvements made to address SonarQube Cloud warnings about credential exposure and SSL certificate validation.

## Summary of Changes

Two security issues were addressed:

1. **PostgreSQL database passwords disclosed in source code**
2. **SSL certificate validation disabled in curl commands**

## 1. Database Credentials

### Problem

Hardcoded database passwords (`ceiba123`) were found in multiple files:

**Configuration Files:**
- `.claude/settings.local.json` - Claude Code local configuration
- `src/Ceiba.Web/appsettings.json` - Connection string
- `docker/docker-compose.yml` - Environment variables

**GitHub Workflows:**
- `.github/workflows/ci.yml` - Database container and connection strings
- `.github/workflows/security-scan.yml` - Database container configuration

**Shell Scripts:**
- `scripts/verification/db-health-check.sql` - Usage comment
- `scripts/setup-database.sql` - User creation script
- `scripts/backup/backup-database.sh` - Fallback value
- `scripts/backup/restore-database.sh` - Fallback value
- `scripts/backup/verify-backup.sh` - Fallback value
- `scripts/verification/e2e-verification.sh` - Fallback value
- `scripts/migrations/validate-migration.sh` - Fallback value
- `scripts/migrations/backup-before-migration.sh` - Fallback value

**PowerShell Scripts:**
- `scripts/backup/backup-database.ps1` - Fallback value
- `scripts/backup/restore-database.ps1` - Fallback value
- `scripts/verification/e2e-verification.ps1` - Fallback value
- `scripts/create-test-user.ps1` - Connection string
- `scripts/reset-database-with-postgres.ps1` - Default password parameter

**C# Code:**
- `src/Ceiba.Infrastructure/Data/MigrationBackupService.cs` - Fallback in connection string parsing

**Documentation:**
- `CLAUDE.md` - Example connection strings
- `specs/001-incident-management-system/quickstart.md` - Configuration examples
- `scripts/README.md` - Setup instructions
- `scripts/README-dummy-reports.md` - Usage examples
- `scripts/backup/README.md` - Default value documentation
- `docs/VERIFICATION-REPORT.md` - SQL command examples

### Solution

All hardcoded passwords have been removed. The new approach:

#### Environment Variables (Required)

All scripts now require `DB_PASSWORD` to be set as an environment variable:

```bash
# Backup example
DB_PASSWORD=your_secure_password ./scripts/backup/backup-database.sh

# PowerShell example
$env:DB_PASSWORD='your_secure_password'
.\scripts\backup\backup-database.ps1
```

Scripts will fail immediately with a clear error message if `DB_PASSWORD` is not set:

```
ERROR: DB_PASSWORD environment variable is required
Usage: DB_PASSWORD=your_password ./backup-database.sh [output_dir]
```

#### Docker Compose

For Docker, create a `.env` file from the template:

```bash
cd docker
cp .env.example .env
# Edit .env with your actual credentials
```

The `.env` file is excluded from version control via `.gitignore`.

#### Application Configuration

For local development, use one of these approaches:

1. **User Secrets** (recommended for development):
   ```bash
   cd src/Ceiba.Web
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;...;Password=your_password"
   ```

2. **Environment Variable**:
   ```bash
   export ConnectionStrings__DefaultConnection="Host=localhost;...;Password=your_password"
   ```

3. **appsettings.Development.json** (gitignored):
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;...;Password=your_password"
     }
   }
   ```

### Files Modified

#### Configuration & Version Control

| File | Change |
|------|--------|
| `.gitignore` | Added `.claude/settings.local.json`, `.env`, `.env.local` |
| `.claude/settings.local.json` | Removed from repository |
| `src/Ceiba.Web/appsettings.json` | Use `${DB_PASSWORD}` placeholder |
| `docker/docker-compose.yml` | Use `${DB_PASSWORD:?...}` (required) |
| `docker/.env.example` | Template with empty password values |

#### GitHub Workflows

| File | Change |
|------|--------|
| `.github/workflows/ci.yml` | Use `CI_DB_PASSWORD` from GitHub secrets with fallback |
| `.github/workflows/security-scan.yml` | Use `CI_DB_PASSWORD` from GitHub secrets with fallback |

#### Shell Scripts

| File | Change |
|------|--------|
| `scripts/verification/db-health-check.sql` | Removed password from comment |
| `scripts/setup-database.sql` | Changed to `CHANGE_THIS_PASSWORD` placeholder |
| `scripts/backup/backup-database.sh` | Require `DB_PASSWORD` env var |
| `scripts/backup/restore-database.sh` | Require `DB_PASSWORD` env var |
| `scripts/backup/verify-backup.sh` | Require `DB_PASSWORD` env var |
| `scripts/verification/e2e-verification.sh` | Require `DB_PASSWORD` env var, add `SKIP_SSL_VERIFY` |
| `scripts/migrations/validate-migration.sh` | Require `DB_PASSWORD` env var |
| `scripts/migrations/backup-before-migration.sh` | Require `DB_PASSWORD` env var |

#### PowerShell Scripts

| File | Change |
|------|--------|
| `scripts/backup/backup-database.ps1` | Require `DB_PASSWORD` env var |
| `scripts/backup/restore-database.ps1` | Require `DB_PASSWORD` env var |
| `scripts/verification/e2e-verification.ps1` | Add `-DbPassword` parameter with validation, add `-SkipSslValidation` |
| `scripts/create-test-user.ps1` | Require `DB_PASSWORD` env var |
| `scripts/setup-database.ps1` | Updated messages (no default password shown) |
| `scripts/reset-database-with-postgres.ps1` | Add `-CeibaPassword` parameter |

#### C# Code

| File | Change |
|------|--------|
| `src/Ceiba.Infrastructure/Data/MigrationBackupService.cs` | Throw `InvalidOperationException` if password is empty in connection string |

#### Documentation

| File | Change |
|------|--------|
| `CLAUDE.md` | Reference `$DB_PASSWORD` env var in examples |
| `specs/001-incident-management-system/quickstart.md` | Use `${DB_PASSWORD}` in connection string example |
| `scripts/README.md` | Use `YOUR_SECURE_PASSWORD` placeholder, add env var notes |
| `scripts/README-dummy-reports.md` | Use `$DB_PASSWORD` in command examples |
| `scripts/backup/README.md` | Mark password as required (no default) |
| `docs/VERIFICATION-REPORT.md` | Use `$DB_PASSWORD` in SQL command examples |

## 2. SSL Certificate Validation

### Problem

The `e2e-verification.sh` script used `curl -k` which disables SSL certificate validation. This is flagged by SonarQube as a security risk because it allows man-in-the-middle attacks.

### Solution

SSL certificate validation is now **enabled by default**. To skip validation for local development with self-signed certificates, you must explicitly opt-out:

```bash
# Default: SSL validation enabled
DB_PASSWORD=xxx ./scripts/verification/e2e-verification.sh

# Development with self-signed certificate: explicitly disable validation
SKIP_SSL_VERIFY=true DB_PASSWORD=xxx ./scripts/verification/e2e-verification.sh
```

When `SKIP_SSL_VERIFY=true` is set, the script displays a warning:

```
WARNING: SSL certificate validation is disabled (SKIP_SSL_VERIFY=true)
```

#### PowerShell Version

The PowerShell verification script (`e2e-verification.ps1`) also supports SSL skip:

```powershell
# Default: SSL validation enabled
.\scripts\verification\e2e-verification.ps1 -DbPassword "your_password"

# With SSL validation skipped (development only)
.\scripts\verification\e2e-verification.ps1 -DbPassword "your_password" -SkipSslValidation
```

### Files Modified

| File | Change |
|------|--------|
| `scripts/verification/e2e-verification.sh` | Added `SKIP_SSL_VERIFY` option, removed hardcoded `-k` |
| `scripts/verification/e2e-verification.ps1` | Added `-SkipSslValidation` switch parameter |

## Best Practices

### For Development

1. Never commit real credentials to version control
2. Use `.env` files for local configuration (gitignored)
3. Use User Secrets for .NET applications
4. Only skip SSL validation for localhost with self-signed certs

### For Production

1. Use secrets management (Azure Key Vault, AWS Secrets Manager, etc.)
2. Inject credentials via environment variables at deployment time
3. Never use `SKIP_SSL_VERIFY=true` in production
4. Rotate credentials regularly

### For CI/CD

1. Store credentials as encrypted secrets in your CI/CD platform
2. Inject credentials as environment variables during pipeline execution
3. Never echo or log credential values

#### GitHub Actions Configuration

The CI workflows use `CI_DB_PASSWORD` secret. Configure it in your repository:

1. Go to Repository Settings → Secrets and variables → Actions
2. Create a new secret: `CI_DB_PASSWORD`
3. Set a secure password value

If the secret is not configured, the workflows use a fallback value (`ci_test_password_not_secret`) which is acceptable for ephemeral CI containers that are destroyed after each run.

```yaml
# How it's configured in the workflows
env:
  CI_DB_PASSWORD: ${{ secrets.CI_DB_PASSWORD || 'ci_test_password_not_secret' }}
```

## Verification

To verify no credentials are committed:

```bash
# Search for potential password patterns
git grep -i "password.*=" -- "*.json" "*.yml" "*.sh" "*.ps1" "*.sql"

# Check for the old hardcoded password
git grep "ceiba123"
```

Both commands should return no results (or only placeholder/template values).

## Related Commits

- `6791d42` - fix(security): Remove hardcoded database credentials (SonarQube)
- `ef4a5e6` - fix(security): Make SSL certificate validation opt-out (SonarQube)
- `b1aa614` - docs: Add security credentials guide for SonarQube fixes
- `c3c3a7d` - fix(security): Require DB_PASSWORD env var in all scripts and workflows
