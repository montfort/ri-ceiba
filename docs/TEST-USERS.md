# Test Users - Ceiba Application

This document lists the test users created automatically by the `SeedDataService` in development mode.

## Default Test Users

### CREADOR (Police Officer)
- **Email**: `creador@test.com`
- **Password**: `Creador123!`
- **Name**: Juan Pérez
- **Role**: CREADOR
- **Permissions**:
  - ✅ Create incident reports (Type A)
  - ✅ Edit own reports (Estado=0 only)
  - ✅ Submit own reports (Estado=0 → Estado=1)
  - ✅ View own reports
  - ❌ View other users' reports
  - ❌ Export reports
  - ❌ Manage users or catalogs

### REVISOR (Supervisor)
- **Email**: `revisor@test.com`
- **Password**: `Revisor123!`
- **Name**: María González
- **Role**: REVISOR
- **Permissions**:
  - ✅ View ALL reports (any user, any estado)
  - ✅ Edit ANY report (including submitted ones)
  - ✅ Export reports to PDF and JSON
  - ✅ Bulk export operations
  - ✅ Search and filter across all reports
  - ✅ View and configure automated reports (US4)
  - ❌ Create new reports
  - ❌ Manage users or catalogs

### ADMIN (Technical Administrator)
- **Email**: `admin@test.com`
- **Password**: `Admin123!Test`
- **Name**: Carlos Rodríguez
- **Role**: ADMIN
- **Permissions**:
  - ✅ Create, suspend, and delete users
  - ✅ Assign roles to users
  - ✅ Configure catalogs (Zona, Sector, Cuadrante)
  - ✅ Configure suggestion lists (Sexo, Delito, etc.)
  - ✅ View audit logs with filtering
  - ❌ Access incident reports module
  - ❌ Export reports

### Super Admin (All Roles)
- **Email**: `admin@ceiba.local`
- **Password**: `Admin123!@`
- **Name**: (Default Admin)
- **Roles**: CREADOR + REVISOR + ADMIN (all three)
- **Permissions**: All permissions from all roles
- **⚠️ WARNING**: Change password immediately after first login!

## Usage Instructions

### Login

1. Navigate to `http://localhost:5000/login`
2. Enter email and password
3. Click "Iniciar Sesión"

### Testing US1 (CREADOR Workflow)

1. **Login as CREADOR**:
   ```
   Email: creador@test.com
   Password: Creador123!
   ```

2. **Create a Report**:
   - Click "Nuevo Reporte" or navigate to `/reports/new`
   - Fill in all required fields:
     - Tipo Reporte: A
     - Fecha/Hora Hechos: (select date/time)
     - Sexo: (select or enter)
     - Edad: (1-149)
     - Delito: (select or enter)
     - Zona/Sector/Cuadrante: (cascading dropdowns)
     - Turno CEIBA: (enter number)
     - Tipo de Atención: (select or enter)
     - Tipo de Acción: (1, 2, or 3)
     - Hechos Reportados: (description)
     - Acciones Realizadas: (description)
     - Traslados: (0, 1, or 2)
   - Click "Guardar Borrador"

3. **Edit Draft Report**:
   - Navigate to "Mis Reportes" or `/reports`
   - Find report with Estado="Borrador"
   - Click "Editar"
   - Modify any field
   - Click "Guardar Cambios"

4. **Submit Report**:
   - Open draft report
   - Click "Entregar Reporte"
   - Confirm submission
   - Verify estado changes to "Entregado"
   - Try to edit (should be blocked)

5. **View Own Reports**:
   - Navigate to "Mis Reportes"
   - Verify only own reports are visible
   - Use filters and search

### Testing US2 (REVISOR Workflow)

1. **Login as REVISOR**:
   ```
   Email: revisor@test.com
   Password: Revisor123!
   ```

2. **View All Reports**:
   - Navigate to reports list
   - Verify all users' reports are visible

3. **Edit Any Report**:
   - Open any report (including submitted ones)
   - Edit fields
   - Save changes

4. **Export Reports**:
   - Select single or multiple reports
   - Export to PDF
   - Export to JSON

### Testing US3 (ADMIN Workflow)

1. **Login as ADMIN**:
   ```
   Email: admin@test.com
   Password: Admin123!Test
   ```

2. **Manage Users**:
   - Create new user
   - Assign roles
   - Suspend user
   - Delete user

3. **Manage Catalogs**:
   - Add/edit Zonas
   - Add/edit Sectores
   - Add/edit Cuadrantes
   - Configure suggestion lists

4. **View Audit Logs**:
   - Navigate to audit logs
   - Filter by user, date, action
   - Verify all operations are logged

## Database Verification

To verify users were created:

```bash
# Connect to database
psql -h localhost -U ceiba -d ceiba

# List all users
SELECT "Email", "UserName", "EmailConfirmed" FROM "AspNetUsers";

# List user roles
SELECT u."Email", r."Name" as "Role"
FROM "AspNetUsers" u
JOIN "AspNetUserRoles" ur ON u."Id" = ur."UserId"
JOIN "AspNetRoles" r ON ur."RoleId" = r."Id"
ORDER BY u."Email";
```

Expected output:
```
Email                 | Role
----------------------|----------
admin@ceiba.local     | ADMIN
admin@ceiba.local     | CREADOR
admin@ceiba.local     | REVISOR
admin@test.com        | ADMIN
creador@test.com      | CREADOR
revisor@test.com      | REVISOR
```

## Resetting Test Users

To recreate test users:

1. Delete database:
   ```bash
   psql -h localhost -U postgres -c "DROP DATABASE ceiba;"
   psql -h localhost -U postgres -c "CREATE DATABASE ceiba;"
   ```

2. Run migrations and seed:
   ```bash
   cd src/Ceiba.Web
   dotnet ef database update
   dotnet run
   ```

Or simply restart the application - the seed service is idempotent.

## Security Notes

⚠️ **IMPORTANT**: These are TEST CREDENTIALS for development only!

- **DO NOT** use these credentials in production
- **DO NOT** commit actual production passwords to source control
- **CHANGE** all default passwords immediately in production
- **USE** strong, unique passwords for production users
- **ENABLE** two-factor authentication in production (when implemented)

## Password Policy

All passwords must meet these requirements:
- Minimum 10 characters
- At least one uppercase letter
- At least one digit
- Optional: special characters

Examples of valid passwords:
- `Creador123!`
- `Password1234`
- `TestUser99`
- `Admin2024@`

## Session Configuration

- **Session Timeout**: 30 minutes of inactivity
- **Cookie Security**: HttpOnly, Secure, SameSite=Strict
- **Session Regeneration**: After successful login
- **User-Agent Validation**: Enabled
- **Anti-CSRF Protection**: Enabled

## Related Documentation

- User Story 1: `docs/US1-COMPLETION-STATUS.md`
- Project Instructions: `CLAUDE.md`
- Feature Specification: `specs/001-incident-management-system/spec.md`
- Security Configuration: `.github/PULL_REQUEST_TEMPLATE.md`
