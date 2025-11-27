# Security Scripts

This directory contains security validation and scanning scripts for the Ceiba project.

## Scripts

### check-no-raw-sql.sh / check-no-raw-sql.ps1

**Purpose**: T020j - RS-002 Mitigation - Zero Raw SQL Policy Check

Scans the codebase for raw SQL queries to prevent SQL injection vulnerabilities.

**Usage**:

```bash
# Linux/macOS
./scripts/security/check-no-raw-sql.sh [project-root]

# Windows PowerShell
.\scripts\security\check-no-raw-sql.ps1 [-ProjectRoot <path>]
```

**What it checks**:
- ❌ `ExecuteSqlRaw` and `ExecuteSqlRawAsync` (unsafe)
- ❌ `FromSqlRaw` (unsafe)
- ❌ String concatenation with SQL keywords
- ❌ Raw SQL in `SqlQuery` calls
- ✅ `ExecuteSqlInterpolated` and `FromSqlInterpolated` (safe - parameterized)
- ✅ LINQ queries (best practice)

**Exit codes**:
- `0`: No violations found (pass)
- `1`: Violations detected (fail)

**Example output**:

```
==================================================
Zero Raw SQL Policy Check
T020j: RS-002 Mitigation
==================================================

Scanning for raw SQL usage in C# files...

[VIOLATION] ./src/Services/UserService.cs:42
  var sql = "SELECT * FROM Users WHERE Name = '" + userName + "'";

[APPROVED] ./src/Infrastructure/Reports/CustomQuery.cs:15
  // APPROVED: Performance-critical query with manual optimization
  context.Database.ExecuteSqlRaw("VACUUM ANALYZE");

==================================================
Scan Complete
==================================================
Violations: 1
Approved Raw SQL: 1

❌ FAILED: Raw SQL violations detected
```

**How to fix violations**:

1. **Use LINQ queries (recommended)**:
   ```csharp
   // ❌ BAD
   var sql = "SELECT * FROM Users WHERE Name = '" + userName + "'";
   var users = context.Database.SqlQuery<User>(sql);

   // ✅ GOOD
   var users = context.Users.Where(u => u.Name == userName).ToList();
   ```

2. **Use parameterized queries**:
   ```csharp
   // ❌ BAD
   context.Database.ExecuteSqlRaw("DELETE FROM Reports WHERE Id = " + reportId);

   // ✅ GOOD
   context.Database.ExecuteSqlInterpolated($"DELETE FROM Reports WHERE Id = {reportId}");
   ```

3. **Approve necessary raw SQL**:
   ```csharp
   // ✅ APPROVED (with justification)
   // APPROVED: PostgreSQL-specific VACUUM command for performance maintenance
   context.Database.ExecuteSqlRaw("VACUUM ANALYZE");
   ```

**Approved patterns**:
- Lines with `// APPROVED:` or `/* APPROVED:` comments
- `ExecuteSqlInterpolated()` - uses parameterized queries
- `FromSqlInterpolated()` - uses parameterized queries

**CI/CD Integration**:

Add to GitHub Actions workflow:

```yaml
- name: Check for Raw SQL
  run: |
    chmod +x ./scripts/security/check-no-raw-sql.sh
    ./scripts/security/check-no-raw-sql.sh
```

Or for Windows runners:

```yaml
- name: Check for Raw SQL
  run: .\scripts\security\check-no-raw-sql.ps1
  shell: pwsh
```

**Pre-commit Hook**:

Add to `.git/hooks/pre-commit`:

```bash
#!/bin/bash
./scripts/security/check-no-raw-sql.sh
if [ $? -ne 0 ]; then
    echo "Commit blocked: Raw SQL violations detected"
    exit 1
fi
```

## Security Best Practices

### SQL Injection Prevention

1. **Always use parameterized queries**
   - Entity Framework Core does this automatically with LINQ
   - Use `ExecuteSqlInterpolated()` for raw queries

2. **Never concatenate user input into SQL strings**
   ```csharp
   // ❌ NEVER DO THIS
   var query = "SELECT * FROM Users WHERE Email = '" + userEmail + "'";

   // ✅ DO THIS
   var user = context.Users.FirstOrDefault(u => u.Email == userEmail);
   ```

3. **Validate and sanitize all inputs**
   - Use data annotations: `[StringLength]`, `[Range]`, `[RegularExpression]`
   - Validate at multiple layers (client, server, database)

4. **Use least privilege database accounts**
   - Application account should only have necessary permissions
   - Never use `sa` or `postgres` superuser accounts

5. **Enable database audit logging**
   - Track all queries and modifications
   - Alert on suspicious patterns

### Additional Resources

- [OWASP SQL Injection Prevention Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/SQL_Injection_Prevention_Cheat_Sheet.html)
- [Entity Framework Core Security](https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-strings#security)
- [PostgreSQL Security Best Practices](https://www.postgresql.org/docs/current/security.html)

## Related Security Measures

- **T020c**: Authorization matrix tests
- **T020d**: OWASP ZAP security scanning
- **T020g**: SonarQube + Snyk scanning
- **T020i**: Input validation integration tests
- **T020j**: Zero raw SQL policy check (this script)

## Maintenance

Scripts should be reviewed and updated when:
- New EF Core versions introduce new APIs
- New SQL injection vectors are discovered
- Project adds new database access patterns
- Security policies are updated
