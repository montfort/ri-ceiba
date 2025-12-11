# Security Credentials Guide

This document describes the security improvements made to address SonarQube Cloud warnings about credential exposure and SSL certificate validation.

## Summary of Changes

Two security issues were addressed:

1. **PostgreSQL database passwords disclosed in source code**
2. **SSL certificate validation disabled in curl commands**

## 1. Database Credentials

### Problem

Hardcoded database passwords (`ceiba123`) were found in multiple files:

- `.claude/settings.local.json` - Claude Code local configuration
- `scripts/verification/db-health-check.sql` - Usage comment
- `scripts/setup-database.sql` - User creation script
- `scripts/backup/backup-database.sh` - Fallback value
- `scripts/backup/backup-database.ps1` - Fallback value
- `scripts/backup/restore-database.sh` - Fallback value
- `scripts/backup/restore-database.ps1` - Fallback value
- `scripts/verification/e2e-verification.sh` - Fallback value
- `src/Ceiba.Web/appsettings.json` - Connection string
- `docker/docker-compose.yml` - Environment variables

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

| File | Change |
|------|--------|
| `.gitignore` | Added `.claude/settings.local.json`, `.env`, `.env.local` |
| `.claude/settings.local.json` | Removed from repository |
| `scripts/verification/db-health-check.sql` | Removed password from comment |
| `scripts/setup-database.sql` | Changed to `CHANGE_THIS_PASSWORD` placeholder |
| `scripts/backup/*.sh` | Require `DB_PASSWORD` env var |
| `scripts/backup/*.ps1` | Require `DB_PASSWORD` env var |
| `scripts/verification/e2e-verification.sh` | Require `DB_PASSWORD` env var |
| `src/Ceiba.Web/appsettings.json` | Use `${DB_PASSWORD}` placeholder |
| `docker/docker-compose.yml` | Use `${DB_PASSWORD:?...}` (required) |
| `docker/.env.example` | Template with empty password values |

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

### Files Modified

| File | Change |
|------|--------|
| `scripts/verification/e2e-verification.sh` | Added `SKIP_SSL_VERIFY` option, removed hardcoded `-k` |

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
