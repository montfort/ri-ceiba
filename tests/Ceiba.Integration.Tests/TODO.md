# Integration Tests - Pending Work

## Status

The security integration tests (AuthorizationMatrixTests.cs and InputValidationTests.cs) have been created as **test specifications** for T020c and T020i. They currently have compilation errors that will be resolved as the application is implemented.

## Compilation Errors to Fix

### 1. Entity Property Mismatches

The tests assume certain property types that differ from actual entities:

**In AuthorizationMatrixTests.cs and InputValidationTests.cs:**

| Test Code | Actual Entity | Fix Needed |
|-----------|---------------|------------|
| `Estado = "borrador"` | `Estado` is `short` (0=Borrador, 1=Entregado) | Use `Estado = 0` |
| `TurnoCeiba = "Matutino"` | `TurnoCeiba` is `int` | Use integer values |
| `TipoDeAccion = "Orientación"` | `TipoDeAccion` is `short` (1/2/3) | Use `1`, `2`, or `3` |
| `Traslados = "Ninguno"` | `Traslados` is `short` (0/1/2) | Use `0`, `1`, or `2` |
| `report.CreadoPorId` | Property is `UsuarioId` (from BaseEntityWithUser) | Use `UsuarioId` |
| `user.Active` | Property is `Activo` (Spanish) | Use `Activo` |

### 2. Missing DTOs

The tests reference DTOs that don't exist yet in `Ceiba.Application`:

- `CreateReportDto` - Temporary version exists in InputValidationTests.cs
- `ReportDto` - Temporary version exists in InputValidationTests.cs

**Action Required**: When implementing User Story 1, create proper DTOs in `src/Ceiba.Application/DTOs/`:
- `CreateReportDto.cs`
- `UpdateReportDto.cs`
- `ReportDto.cs`
- `ReportListDto.cs`

### 3. Missing API Endpoints

Tests call endpoints that don't exist yet:

- `POST /api/reports` - Create report
- `POST /api/reports/export` - Export reports
- `POST /api/reports/search` - Search reports
- `POST /api/admin/users` - Create user
- `POST /api/admin/users/search` - Search users
- `POST /api/files/download` - File download
- `POST /api/notifications/send` - Send notification
- `POST /api/reports/import` - Import reports

**Action Required**: Implement these endpoints as part of User Stories 1-3.

### 4. Missing Usuario Type Resolution

The tests use `UserManager<Usuario>` but the compiler can't find `Usuario`:

**Current**: `var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();`

**Fix**: Ensure `Usuario` entity is properly referenced:
```csharp
using Ceiba.Infrastructure.Identity; // If Usuario is here
// OR
using Ceiba.Core.Entities; // If Usuario is here
```

Check where `Usuario` is defined and add the appropriate using statement.

## Implementation Order

To make these tests pass, implement in this order:

### Phase 1: Fix Entity References (Quick Wins)
1. Add proper using statements for `Usuario`
2. Update test data to match entity types:
   - Change `Estado` from string to short (0 or 1)
   - Change `TurnoCeiba` from string to int
   - Change `TipoDeAccion` from string to short (1, 2, or 3)
   - Change `Traslados` from string to short (0, 1, or 2)
   - Rename `CreadoPorId` to `UsuarioId`
   - Rename `Active` to `Activo`

### Phase 2: Create DTOs (User Story 1)
1. Create `src/Ceiba.Application/DTOs/Reports/` directory
2. Implement DTOs:
   - `CreateReportDto.cs`
   - `UpdateReportDto.cs`
   - `ReportDto.cs`
   - `ReportListDto.cs`
3. Remove temporary DTOs from test files
4. Add `using Ceiba.Application.DTOs.Reports;`

### Phase 3: Implement API Endpoints (User Stories 1-3)
1. **US1**: Reports CRUD endpoints
   - POST /api/reports (create)
   - GET /api/reports/{id} (read)
   - PUT /api/reports/{id} (update)
   - POST /api/reports/submit (submit/deliver)
   - GET /api/reports/my (list own reports)

2. **US2**: Supervisor endpoints
   - GET /api/reports (list all)
   - POST /api/reports/export (export to PDF/JSON)
   - POST /api/reports/search (search/filter)

3. **US3**: Admin endpoints
   - POST /api/admin/users (create user)
   - PUT /api/admin/users/{id}/suspend (suspend user)
   - POST /api/admin/users/search (search users)

### Phase 4: Run and Fix Tests
1. Build project: `dotnet build`
2. Run tests: `dotnet test --filter "Category=Integration&Security=InputValidation"`
3. Fix remaining issues (authentication, database setup, etc.)
4. Ensure all tests pass

## Running Tests

Once fixed, run tests with:

```bash
# All integration tests
dotnet test tests/Ceiba.Integration.Tests

# Only authorization tests
dotnet test --filter "FullyQualifiedName~AuthorizationMatrixTests"

# Only input validation tests
dotnet test --filter "FullyQualifiedName~InputValidationTests"

# Specific category
dotnet test --filter "Category=Integration&Security=Authorization"
```

## Notes

- These tests are **security specifications** documenting expected behavior
- They serve as living documentation for security requirements
- Do NOT delete tests even if they don't compile - fix them instead
- Tests validate OWASP Top 10 2021 mitigations (RS-001, RS-002)
- Authorization matrix tests ensure role-based access control
- Input validation tests prevent injection attacks

## Related Tasks

- **T020c**: Authorization Matrix Tests (RS-001 Mitigation)
- **T020i**: Input Validation Integration Tests (RS-002 Mitigation)
- **US1**: User Story 1 - Report Creation (implements required endpoints)
- **US2**: User Story 2 - Supervisor Review (implements export endpoints)
- **US3**: User Story 3 - User Management (implements admin endpoints)
