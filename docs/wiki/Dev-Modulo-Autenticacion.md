# Módulo de Autenticación

El módulo de autenticación gestiona el acceso al sistema usando **ASP.NET Identity**.

## Componentes

### Entidad Usuario

```csharp
public class Usuario : IdentityUser<Guid>
{
    public bool Activo { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
}
```

### Roles del Sistema

| Rol | Descripción |
|-----|-------------|
| CREADOR | Oficiales que crean reportes |
| REVISOR | Supervisores que revisan y exportan |
| ADMIN | Administradores técnicos |

## Configuración

### Program.cs

```csharp
// Identity
builder.Services.AddIdentity<Usuario, IdentityRole<Guid>>(options =>
{
    options.Password.RequiredLength = 10;
    options.Password.RequireUppercase = true;
    options.Password.RequireDigit = true;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<CeibaDbContext>()
.AddDefaultTokenProviders();

// Cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
});
```

## Flujo de Login

### Endpoint de Login

```csharp
app.MapPost("/account/login-form", async (
    HttpContext context,
    SignInManager<Usuario> signInManager,
    UserManager<Usuario> userManager,
    [FromForm] string email,
    [FromForm] string password,
    [FromForm] bool rememberMe,
    [FromForm] string? returnUrl) =>
{
    var user = await userManager.FindByEmailAsync(email);
    if (user == null || !user.Activo)
    {
        return Results.Redirect("/login?error=invalid");
    }

    var result = await signInManager.PasswordSignInAsync(
        email, password, rememberMe, lockoutOnFailure: true);

    if (result.Succeeded)
    {
        user.LastLoginAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);
        return Results.Redirect(returnUrl ?? "/");
    }

    return Results.Redirect("/login?error=invalid");
});
```

### Componente Login.razor

```razor
@page "/login"

<form method="post" action="/account/login-form">
    <input type="email" name="email" required />
    <input type="password" name="password" required minlength="10" />
    <input type="checkbox" name="rememberMe" value="true" />
    <button type="submit">Iniciar Sesión</button>
</form>
```

## Autorización

### Basada en Roles

```razor
@attribute [Authorize(Roles = "CREADOR")]

<AuthorizeView Roles="REVISOR">
    <Authorized>
        <!-- Contenido para revisores -->
    </Authorized>
</AuthorizeView>
```

### En Servicios

```csharp
public async Task<ReportDto> GetReportByIdAsync(int id, Guid userId, bool isRevisor)
{
    var report = await _context.Reports.FindAsync(id);

    if (!isRevisor && report.CreadorId != userId)
    {
        throw new ForbiddenException("No tienes permiso para ver este reporte");
    }

    return MapToDto(report);
}
```

## Sesión

### Timeout

La sesión expira después de 30 minutos de inactividad. `SlidingExpiration = true` renueva el tiempo con cada actividad.

### Claims

```csharp
var claims = await signInManager.ClaimsFactory.CreateAsync(user);
// Contiene: NameIdentifier, Email, Role(s)
```

## Auditoría de Acceso

Todos los eventos de autenticación se registran:

```csharp
await _auditService.LogAsync(
    "LOGIN",
    "Usuario",
    user.Id.ToString(),
    user.Id,
    new { Success = true, RememberMe = rememberMe });
```

## Próximos Pasos

- [Módulo de Reportes](Dev-Modulo-Reportes)
- [Agregar un nuevo rol](Dev-Guia-Agregar-Rol)
