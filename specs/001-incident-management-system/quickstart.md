# Quickstart: Sistema de Gestión de Reportes de Incidencias

**Branch**: `001-incident-management-system` | **Date**: 2025-11-18

## Prerequisites

- .NET 10 SDK
- Docker and Docker Compose
- PostgreSQL 18 (via Docker or local)
- Pandoc (for Markdown→Word conversion)
- Git

## Development Setup

### 1. Clone and Setup

```bash
git clone <repository-url>
cd ri-ceiba
git checkout 001-incident-management-system
```

### 2. Install Dependencies

```bash
# Restore .NET packages
dotnet restore

# Install Pandoc (if not using Docker)
# Windows: choco install pandoc
# macOS: brew install pandoc
# Linux: sudo apt install pandoc
```

### 3. Start Database (Docker)

```bash
# Start PostgreSQL
docker-compose up -d ceiba-db

# Or use .NET ASPIRE for full orchestration
dotnet run --project src/Ceiba.AppHost
```

### 4. Apply Migrations

```bash
cd src/Ceiba.Infrastructure
dotnet ef database update
```

### 5. Seed Initial Data

```bash
dotnet run --project src/Ceiba.Web -- --seed
```

This seeds:
- Default ADMIN user (admin@ceiba.local / Admin123!)
- Initial Zonas, Sectores, Cuadrantes
- Default suggestion lists (Sexo, Delito, TipoDeAtencion)

### 6. Run Application

```bash
# Development mode with hot reload
dotnet watch run --project src/Ceiba.Web

# Or standard run
dotnet run --project src/Ceiba.Web
```

Application available at: `https://localhost:5001`

## Running Tests

```bash
# All tests
dotnet test

# Specific project
dotnet test tests/Ceiba.Core.Tests

# With coverage
dotnet test --collect:"XPlat Code Coverage"

# Watch mode
dotnet watch test --project tests/Ceiba.Application.Tests
```

### Test Categories

```bash
# Unit tests only
dotnet test --filter "Category=Unit"

# Integration tests (requires database)
dotnet test --filter "Category=Integration"

# Component tests (Blazor)
dotnet test --filter "Category=Component"
```

## Project Structure

```
src/
├── Ceiba.Web/           # Blazor Server app
├── Ceiba.Core/          # Domain entities and interfaces
├── Ceiba.Application/   # Application services
├── Ceiba.Infrastructure/ # Data access, external services
└── Ceiba.Shared/        # Shared DTOs

tests/
├── Ceiba.Core.Tests/
├── Ceiba.Application.Tests/
├── Ceiba.Infrastructure.Tests/
├── Ceiba.Web.Tests/
└── Ceiba.Integration.Tests/
```

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=ceiba;Username=ceiba;Password=ceiba123"
  },
  "Identity": {
    "SessionTimeout": 30,
    "PasswordMinLength": 10,
    "RequireUppercase": true,
    "RequireDigit": true
  },
  "Email": {
    "Provider": "Smtp",
    "Host": "smtp.example.com",
    "Port": 587,
    "Username": "",
    "Password": ""
  },
  "AI": {
    "Provider": "OpenAI",
    "ApiKey": "",
    "Model": "gpt-4"
  },
  "AutomatedReports": {
    "GenerationTime": "06:00:00",
    "Recipients": ["supervisor@ceiba.local"]
  }
}
```

### Environment Variables (Production)

```bash
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=<connection-string>
Email__ApiKey=<sendgrid-key>
AI__ApiKey=<openai-key>
```

## Docker Deployment

### Build and Run

```bash
# Build image
docker build -t ceiba-web -f docker/Dockerfile .

# Run with compose
docker-compose up -d

# View logs
docker-compose logs -f ceiba-web
```

### Production Compose

```yaml
# docker-compose.prod.yml
services:
  ceiba-web:
    image: ceiba-web:latest
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    ports:
      - "80:80"
      - "443:443"
    depends_on:
      - ceiba-db

  ceiba-db:
    image: postgres:18
    volumes:
      - postgres-data:/var/lib/postgresql/data
      - /mnt/backup:/backup
    environment:
      - POSTGRES_DB=ceiba
      - POSTGRES_USER=ceiba
      - POSTGRES_PASSWORD=${DB_PASSWORD}

volumes:
  postgres-data:
```

## Database Operations

### Create Migration

```bash
cd src/Ceiba.Infrastructure
dotnet ef migrations add <MigrationName> --startup-project ../Ceiba.Web
```

### Apply Migration

```bash
dotnet ef database update --startup-project ../Ceiba.Web
```

### Backup Database

```bash
# Manual backup (Docker)
docker exec ceiba-db pg_dump -U ceiba -d ceiba --format=custom --compress=9 > backup_$(date +%Y%m%d).dump

# Or use the backup scripts
./scripts/backup/backup-database.sh              # Linux/macOS
.\scripts\backup\backup-database.ps1             # Windows PowerShell

# Scheduled backups (add to crontab)
./scripts/backup/scheduled-backup.sh daily       # Daily backup with retention
```

### Restore Database

```bash
# Using restore script (recommended)
./scripts/backup/restore-database.sh backups/latest.dump --confirm

# Or manual restore
pg_restore -h localhost -U ceiba -d ceiba -c backup.dump
```

## User Roles and Access

| Role | Access |
|------|--------|
| CREADOR | Create/edit own reports, submit, view history |
| REVISOR | View/edit all reports, export PDF/JSON, automated reports |
| ADMIN | User management, catalog config, audit logs |

### Default Credentials

- **ADMIN**: admin@ceiba.local / Admin123!

> Change the admin password immediately after first login!

## Common Tasks

### Add New Suggestion

```csharp
// Via Admin UI or API
POST /api/admin/sugerencias
{
  "campo": "delito",
  "valor": "Nuevo Delito",
  "orden": 10
}
```

### Configure Automated Reports

1. Login as REVISOR
2. Navigate to "Reportes Automatizados" > "Modelos"
3. Edit the default template
4. Configure recipients in appsettings.json

### Export Reports

1. Login as REVISOR
2. Navigate to "Reportes"
3. Select reports using checkboxes
4. Click "Exportar PDF" or "Exportar JSON"

## Troubleshooting

### Database Connection Issues

```bash
# Check PostgreSQL is running
docker ps | grep ceiba-db

# Test connection
psql -h localhost -U ceiba -d ceiba
```

### Migration Errors

```bash
# Reset database (development only!)
dotnet ef database drop --force --startup-project ../Ceiba.Web
dotnet ef database update --startup-project ../Ceiba.Web
```

### Session Timeout

Sessions expire after 30 minutes of inactivity. Increase in appsettings if needed (not recommended for security).

## API Documentation

API contracts are defined in OpenAPI format:
- `specs/001-incident-management-system/contracts/api-auth.yaml`
- `specs/001-incident-management-system/contracts/api-reports.yaml`
- `specs/001-incident-management-system/contracts/api-admin.yaml`
- `specs/001-incident-management-system/contracts/api-audit.yaml`

Swagger UI available at `/swagger` in development mode.

## Support

- Project Constitution: `.specify/memory/constitution.md`
- Feature Spec: `specs/001-incident-management-system/spec.md`
- Data Model: `specs/001-incident-management-system/data-model.md`
- Research: `specs/001-incident-management-system/research.md`
