# Pre-Implementation Preflight Checklist

**Date**: 2025-11-22
**Project**: Ceiba - Reportes de Incidencias
**Purpose**: Validate all required tools, CLIs, and permissions before implementation

---

## Required Tools & Expected Versions

### Core Development
- [ ] **.NET SDK 10** (dotnet CLI)
  - Required for: Project creation, compilation, testing, migrations
  - Commands: `dotnet new`, `dotnet build`, `dotnet test`, `dotnet run`, `dotnet ef`
  - Minimum version: 10.0.0

- [ ] **Git**
  - Required for: Version control, branching, commits, PR workflow
  - Commands: `git status`, `git add`, `git commit`, `git push`, `git branch`
  - Minimum version: 2.30+

### Containerization & Databases
- [ ] **Docker** (with Compose V2)
  - Required for: Containerization, local PostgreSQL, production deployment
  - Commands: `docker version`, `docker compose up`, `docker build`, `docker ps`
  - ⚠️ CRITICAL: Must use `docker compose` (V2) NOT `docker-compose` (deprecated)
  - Minimum version: Docker 24+, Compose V2

- [ ] **PostgreSQL Client** (psql)
  - Required for: Database connection testing, migrations validation, backups
  - Commands: `psql --version`, `psql -h localhost -U postgres`, `pg_dump`, `pg_restore`
  - Minimum version: PostgreSQL 16+ client (server will be 18 in container)

### Document Processing
- [ ] **Pandoc**
  - Required for: Markdown → Word conversion (RT-006 mitigation, US4)
  - Commands: `pandoc --version`, `pandoc -f markdown -t docx`
  - Minimum version: 3.1+
  - ⚠️ CRITICAL: Must be accessible from PATH

### CI/CD & Automation
- [ ] **PowerShell**
  - Required for: Setup scripts, validation scripts
  - Commands: `pwsh --version`, `powershell -ExecutionPolicy Bypass`
  - Minimum version: PowerShell 7+ (Core)

- [ ] **GitHub CLI** (gh)
  - Required for: Issue creation, PR management (optional for /speckit.taskstoissues)
  - Commands: `gh --version`, `gh auth status`, `gh issue create`
  - Minimum version: 2.40+

### Testing & Quality
- [ ] **Playwright** (via .NET NuGet)
  - Required for: E2E testing (RT-005 mitigation)
  - Note: Installed via NuGet, but may require `playwright install` command
  - Will validate during project setup

### Optional but Recommended
- [ ] **Node.js & npm**
  - Required for: Frontend tooling (if needed), playwright dependencies
  - Commands: `node --version`, `npm --version`
  - Minimum version: Node 20 LTS

---

## Validation Tests

### Test 1: .NET SDK
```bash
dotnet --version
dotnet --list-sdks
dotnet new console -n TestConsole -o temp/test-dotnet
cd temp/test-dotnet && dotnet build
cd ../.. && rm -rf temp/test-dotnet
```

### Test 2: Docker Compose V2
```bash
docker --version
docker compose version  # Must work (NOT docker-compose)
docker ps
docker compose --help
```

### Test 3: PostgreSQL Client
```bash
psql --version
# Connection test will be done with containerized PostgreSQL
```

### Test 4: Pandoc
```bash
pandoc --version
echo "# Test" | pandoc -f markdown -t docx -o temp/test.docx
rm temp/test.docx
```

### Test 5: Git
```bash
git --version
git config --global user.name
git config --global user.email
git status
```

### Test 6: PowerShell
```bash
pwsh --version
# Test execution policy
powershell -ExecutionPolicy Bypass -Command "Write-Output 'Test OK'"
```

### Test 7: GitHub CLI (Optional)
```bash
gh --version
gh auth status
```

---

## Permission Validation

### Write Permissions
- [ ] Can create directories in project root
- [ ] Can create files in src/
- [ ] Can create files in tests/
- [ ] Can create files in docker/
- [ ] Can create files in scripts/
- [ ] Can execute PowerShell scripts
- [ ] Can execute Bash scripts (if WSL/Git Bash available)

### Execution Permissions
- [ ] Can run `dotnet` commands
- [ ] Can run `docker` commands (may require Docker Desktop running)
- [ ] Can run `git` commands
- [ ] Can run `pwsh` commands

---

## Integration Test: Full Stack

### Create Minimal .NET + Docker + PostgreSQL Project
```bash
# 1. Create test directory
mkdir -p temp/integration-test
cd temp/integration-test

# 2. Create .NET project
dotnet new web -n TestApp

# 3. Add EF Core PostgreSQL
cd TestApp
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL

# 4. Create docker-compose.yml
# (Validate docker compose up -d works)

# 5. Test connection to PostgreSQL container

# 6. Cleanup
cd ../../..
rm -rf temp/integration-test
```

---

## Known Issues & Workarounds

### Issue 1: docker-compose vs docker compose
- **Problem**: Old `docker-compose` command deprecated
- **Solution**: Always use `docker compose` (space, not hyphen)
- **Validation**: `docker compose version` must work

### Issue 2: .NET ASPIRE Requirements
- **Problem**: .NET ASPIRE may require additional workloads
- **Solution**: Install with `dotnet workload install aspire`
- **Validation**: Check after .NET SDK validation

### Issue 3: Pandoc not in PATH
- **Problem**: Pandoc installed but not accessible
- **Solution**: Add to PATH or install via package manager
- **Validation**: `pandoc --version` must work from any directory

### Issue 4: PostgreSQL Client Missing
- **Problem**: psql not installed on Windows by default
- **Solution**: Install via PostgreSQL installer or use Docker container
- **Validation**: `psql --version` or use containerized client

### Issue 5: PowerShell Execution Policy
- **Problem**: Scripts blocked by execution policy
- **Solution**: Use `-ExecutionPolicy Bypass` flag explicitly
- **Validation**: Test with sample script

---

## Checklist Summary

**PASS Criteria**: All core tools validated ✅
**WARN Criteria**: Optional tools missing but workarounds available ⚠️
**FAIL Criteria**: Core tools missing or broken ❌

**Status**: [ ] NOT STARTED | [ ] IN PROGRESS | [ ] COMPLETED

---

**Next Steps After Validation**:
1. If all ✅ → Proceed with `/speckit.implement`
2. If any ⚠️ → Document workarounds, proceed with caution
3. If any ❌ → Install missing tools, re-validate, then proceed
