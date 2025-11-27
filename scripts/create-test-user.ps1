# Script to create a CREADOR test user
# Usage: .\scripts\create-test-user.ps1 [-Email "user@test.com"] [-Password "Password123!"] [-Role "CREADOR"]

param(
    [string]$Email = "creador@test.com",
    [string]$Password = "Creador123!",
    [string]$Role = "CREADOR",
    [string]$Nombre = "Usuario",
    [string]$Apellido = "Creador"
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Create Test User for Ceiba" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Email: $Email" -ForegroundColor Yellow
Write-Host "Role: $Role" -ForegroundColor Yellow
Write-Host "Name: $Nombre $Apellido" -ForegroundColor Yellow
Write-Host ""

# Check if dotnet ef is installed
try {
    $efVersion = dotnet ef --version 2>&1 | Select-String "Entity Framework Core"
    Write-Host "✓ Entity Framework Core tools found" -ForegroundColor Green
} catch {
    Write-Host "✗ Entity Framework Core tools not found" -ForegroundColor Red
    Write-Host "Install with: dotnet tool install --global dotnet-ef" -ForegroundColor Yellow
    exit 1
}

# Create a C# script to add the user
$csharpScript = @"
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ceiba.Infrastructure.Data;
using Ceiba.Core.Entities;

var services = new ServiceCollection();

// Add DbContext
services.AddDbContext<CeibaDbContext>(options =>
    options.UseNpgsql("Host=localhost;Database=ceiba;Username=ceiba;Password=ceiba123"));

// Add Identity
services.AddIdentity<Usuario, IdentityRole<Guid>>()
    .AddEntityFrameworkStores<CeibaDbContext>()
    .AddDefaultTokenProviders();

var serviceProvider = services.BuildServiceProvider();

using (var scope = serviceProvider.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

    // Ensure role exists
    var roleExists = await roleManager.RoleExistsAsync("$Role");
    if (!roleExists)
    {
        Console.WriteLine("Creating role: $Role");
        await roleManager.CreateAsync(new IdentityRole<Guid>("$Role"));
    }

    // Check if user exists
    var existingUser = await userManager.FindByEmailAsync("$Email");
    if (existingUser != null)
    {
        Console.WriteLine("User already exists: $Email");

        // Check if user has the role
        var hasRole = await userManager.IsInRoleAsync(existingUser, "$Role");
        if (!hasRole)
        {
            Console.WriteLine("Adding role $Role to user");
            await userManager.AddToRoleAsync(existingUser, "$Role");
        }

        Console.WriteLine("User ID: " + existingUser.Id);
        return;
    }

    // Create new user
    var user = new Usuario
    {
        UserName = "$Email",
        Email = "$Email",
        Nombre = "$Nombre",
        Apellido = "$Apellido",
        EmailConfirmed = true,
        Activo = true
    };

    var result = await userManager.CreateAsync(user, "$Password");

    if (result.Succeeded)
    {
        Console.WriteLine("✓ User created successfully");

        // Add role
        await userManager.AddToRoleAsync(user, "$Role");
        Console.WriteLine("✓ Role $Role assigned");
        Console.WriteLine("User ID: " + user.Id);
    }
    else
    {
        Console.WriteLine("✗ Failed to create user:");
        foreach (var error in result.Errors)
        {
            Console.WriteLine("  - " + error.Description);
        }
    }
}
"@

# Save C# script to temp file
$tempScript = [System.IO.Path]::GetTempFileName() + ".cs"
$csharpScript | Out-File -FilePath $tempScript -Encoding UTF8

Write-Host "Creating user via database connection..." -ForegroundColor Yellow
Write-Host ""

# Since we can't easily run arbitrary C# scripts, we'll use SQL instead
$connectionString = "Host=localhost;Database=ceiba;Username=ceiba;Password=ceiba123"

# Check if psql is available
try {
    $psqlVersion = psql --version 2>&1
    Write-Host "✓ PostgreSQL client found" -ForegroundColor Green
} catch {
    Write-Host "✗ PostgreSQL client (psql) not found" -ForegroundColor Red
    Write-Host "Install PostgreSQL client tools to use this script" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Alternative: Use the SeedDataService in the application" -ForegroundColor Yellow
    Write-Host "Or create user via ADMIN interface once US3 is implemented" -ForegroundColor Yellow
    exit 1
}

# Generate password hash using a simple approach (we'll use the app's identity system)
Write-Host "Connecting to database..." -ForegroundColor Yellow

$sqlScript = @"
-- Check if user exists
DO `$`$
DECLARE
    user_id UUID;
    role_id UUID;
    user_exists BOOLEAN;
BEGIN
    -- Check if user exists
    SELECT id INTO user_id FROM "AspNetUsers" WHERE "Email" = '$Email';
    user_exists := FOUND;

    IF user_exists THEN
        RAISE NOTICE 'User already exists: $Email (ID: %)', user_id;

        -- Get role ID
        SELECT "Id" INTO role_id FROM "AspNetRoles" WHERE "Name" = '$Role';

        -- Check if user has role
        IF NOT EXISTS (SELECT 1 FROM "AspNetUserRoles" WHERE "UserId" = user_id AND "RoleId" = role_id) THEN
            INSERT INTO "AspNetUserRoles" ("UserId", "RoleId") VALUES (user_id, role_id);
            RAISE NOTICE 'Role % assigned to user', '$Role';
        ELSE
            RAISE NOTICE 'User already has role %', '$Role';
        END IF;
    ELSE
        RAISE NOTICE 'User does not exist. Please use the application to create users with proper password hashing.';
        RAISE NOTICE 'You can login as admin@ceiba.local / Admin123! and create users via the ADMIN interface.';
    END IF;
END `$`$;
"@

$sqlScript | psql -h localhost -U ceiba -d ceiba 2>&1

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Script completed" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Note: User creation with password hashing requires the application." -ForegroundColor Yellow
Write-Host ""
Write-Host "Recommended approach:" -ForegroundColor Green
Write-Host "1. Run the application: cd src/Ceiba.Web && dotnet run" -ForegroundColor White
Write-Host "2. Login as admin@ceiba.local / Admin123!" -ForegroundColor White
Write-Host "3. Use ADMIN interface to create CREADOR user (when US3 is implemented)" -ForegroundColor White
Write-Host ""
Write-Host "OR use the development seed data that should have created test users" -ForegroundColor Green
Write-Host ""

# Clean up
Remove-Item $tempScript -ErrorAction SilentlyContinue
