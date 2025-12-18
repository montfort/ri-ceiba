# Guía: Debugging

Esta guía cubre técnicas y herramientas para depurar el sistema Ceiba.

## Configuración de Logging

### appsettings.Development.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information",
      "Ceiba": "Debug"
    }
  }
}
```

### Niveles de Log

| Nivel | Uso |
|-------|-----|
| Trace | Información muy detallada |
| Debug | Información de desarrollo |
| Information | Flujo normal de la aplicación |
| Warning | Condiciones inesperadas |
| Error | Errores que no detienen la app |
| Critical | Fallas del sistema |

## Logging en Código

### Inyección del Logger

```csharp
public class ReportService : IReportService
{
    private readonly ILogger<ReportService> _logger;

    public ReportService(ILogger<ReportService> logger)
    {
        _logger = logger;
    }

    public async Task<ReportDto> CreateReportAsync(CreateReportDto dto, Guid userId)
    {
        _logger.LogInformation("Creating report for user {UserId}", userId);

        try
        {
            var report = MapToEntity(dto);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Report {ReportId} created successfully", report.Id);
            return MapToDto(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create report for user {UserId}", userId);
            throw;
        }
    }
}
```

### En Componentes Blazor

```csharp
@inject ILogger<ReportForm> Logger

@code {
    protected override async Task OnInitializedAsync()
    {
        Logger.LogDebug("ReportForm initialized with ReportId={ReportId}", ReportId);
    }
}
```

## Debugging en VS Code

### launch.json

```json
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": ".NET Core Launch (web)",
            "type": "coreclr",
            "request": "launch",
            "preLaunchTask": "build",
            "program": "${workspaceFolder}/src/Ceiba.Web/bin/Debug/net10.0/Ceiba.Web.dll",
            "args": [],
            "cwd": "${workspaceFolder}/src/Ceiba.Web",
            "stopAtEntry": false,
            "serverReadyAction": {
                "action": "openExternally",
                "pattern": "\\bNow listening on:\\s+(https?://\\S+)"
            },
            "env": {
                "ASPNETCORE_ENVIRONMENT": "Development"
            }
        }
    ]
}
```

### Breakpoints

1. Click en el margen izquierdo de la línea
2. F5 para iniciar debugging
3. F10 para step over
4. F11 para step into
5. Shift+F11 para step out

## Debugging EF Core

### Log de Queries SQL

```csharp
optionsBuilder
    .UseNpgsql(connectionString)
    .LogTo(Console.WriteLine, LogLevel.Information)
    .EnableSensitiveDataLogging(); // Solo en desarrollo
```

### Ver Query Generada

```csharp
var query = _context.Reports
    .Where(r => r.Estado == 1)
    .OrderByDescending(r => r.CreatedAt);

// Ver SQL generada
var sql = query.ToQueryString();
_logger.LogDebug("SQL: {Sql}", sql);

var result = await query.ToListAsync();
```

## Debugging Blazor

### Console del Navegador

```csharp
@inject IJSRuntime JS

@code {
    private async Task DebugLog(object data)
    {
        await JS.InvokeVoidAsync("console.log", data);
    }
}
```

### Blazor DevTools

1. Instala la extensión "Blazor" en tu navegador
2. Abre DevTools (F12)
3. Pestaña "Blazor" para ver el árbol de componentes

### Error Boundaries

```razor
<ErrorBoundary>
    <ChildContent>
        <MyComponent />
    </ChildContent>
    <ErrorContent Context="exception">
        <div class="alert alert-danger">
            @exception.Message
        </div>
    </ErrorContent>
</ErrorBoundary>
```

## Problemas Comunes

### DbContext Concurrency

**Síntoma:** "A second operation was started on this context"

**Causa:** DbContext usado en múltiples operaciones async paralelas.

**Solución:**
```csharp
// ❌ Malo
await Task.WhenAll(
    LoadZonasAsync(),
    LoadRegionesAsync());

// ✅ Bueno
await LoadZonasAsync();
await LoadRegionesAsync();
```

### Circuit Disconnection

**Síntoma:** La página se congela o pierde conectividad.

**Debug:**
```razor
<script>
    Blazor.start({
        reconnectionHandler: {
            onConnectionDown: (options, error) => console.error('Connection down', error),
            onConnectionUp: () => console.log('Connection up')
        }
    });
</script>
```

### Memory Leaks

**Síntoma:** La aplicación se vuelve lenta con el tiempo.

**Solución:** Implementar IDisposable

```csharp
@implements IDisposable

@code {
    private Timer? _timer;

    protected override void OnInitialized()
    {
        _timer = new Timer(async _ =>
        {
            await InvokeAsync(StateHasChanged);
        }, null, 0, 5000);
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
```

## Herramientas Útiles

### dotnet-counters

```bash
# Monitoreo en tiempo real
dotnet counters monitor --process-id <PID>
```

### dotnet-trace

```bash
# Capturar trace
dotnet trace collect --process-id <PID>
```

### Docker Logs

```bash
docker compose logs -f ceiba-web
```

## Checklist de Debugging

1. [ ] Revisar logs de la aplicación
2. [ ] Revisar consola del navegador
3. [ ] Verificar queries SQL generadas
4. [ ] Verificar estado del DbContext
5. [ ] Comprobar errores de red (DevTools > Network)
6. [ ] Verificar variables de entorno
7. [ ] Revisar configuración de appsettings
