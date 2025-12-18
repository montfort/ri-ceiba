# Módulo de Reportes Automatizados

Este módulo genera reportes narrativos automáticos usando inteligencia artificial.

## Arquitectura

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│  Background     │────▶│  AI Narrative   │────▶│     Email       │
│    Service      │     │    Service      │     │    Service      │
└─────────────────┘     └─────────────────┘     └─────────────────┘
         │                      │
         ▼                      ▼
┌─────────────────┐     ┌─────────────────┐
│   Plantillas    │     │  OpenAI/Azure/  │
│                 │     │     Local       │
└─────────────────┘     └─────────────────┘
```

## Entidades

### Plantilla de Reporte

```csharp
public class PlantillaReporte
{
    public int Id { get; set; }
    public string Nombre { get; set; }
    public string Descripcion { get; set; }
    public string Prompt { get; set; }          // Instrucciones para IA
    public string CronExpression { get; set; }  // Programación
    public string Destinatarios { get; set; }   // JSON array de emails
    public bool Activo { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastExecutedAt { get; set; }
}
```

### Reporte Automatizado

```csharp
public class ReporteAutomatizado
{
    public int Id { get; set; }
    public int PlantillaId { get; set; }
    public DateTime GeneratedAt { get; set; }
    public DateTime PeriodoInicio { get; set; }
    public DateTime PeriodoFin { get; set; }
    public string Contenido { get; set; }       // Narrativa generada
    public int ReportesIncluidos { get; set; }
    public int Estado { get; set; }             // 0=Generando, 1=Generado, 2=Enviado, 3=Error
    public string? ErrorMessage { get; set; }
    public virtual PlantillaReporte Plantilla { get; set; }
}
```

## Servicio de IA

### Interfaz Abstracta

```csharp
public interface IAiNarrativeService
{
    Task<string> GenerateNarrativeAsync(string prompt, IEnumerable<ReportDto> reports);
    Task<bool> TestConnectionAsync();
}
```

### Implementación OpenAI

```csharp
public class OpenAiNarrativeService : IAiNarrativeService
{
    private readonly HttpClient _httpClient;
    private readonly AiConfiguration _config;

    public async Task<string> GenerateNarrativeAsync(
        string prompt,
        IEnumerable<ReportDto> reports)
    {
        var reportsJson = JsonSerializer.Serialize(reports.Select(r => new
        {
            r.Delito,
            r.Zona.Nombre,
            r.DatetimeHechos,
            r.HechosReportados,
            r.AccionesRealizadas
        }));

        var fullPrompt = $"""
            {prompt}

            Datos de los reportes:
            {reportsJson}
            """;

        var request = new
        {
            model = _config.Model,
            messages = new[]
            {
                new { role = "system", content = "Eres un asistente que genera reportes ejecutivos." },
                new { role = "user", content = fullPrompt }
            },
            temperature = _config.Temperature,
            max_tokens = _config.MaxTokens
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"{_config.Endpoint}/chat/completions",
            request);

        var result = await response.Content.ReadFromJsonAsync<OpenAiResponse>();
        return result.Choices[0].Message.Content;
    }
}
```

## Background Service

```csharp
public class AutomatedReportBackgroundService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingTemplatesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing automated reports");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task ProcessPendingTemplatesAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CeibaDbContext>();

        var templates = await context.Plantillas
            .Where(p => p.Activo && ShouldExecute(p))
            .ToListAsync(ct);

        foreach (var template in templates)
        {
            await GenerateReportAsync(template, scope.ServiceProvider, ct);
        }
    }

    private async Task GenerateReportAsync(
        PlantillaReporte template,
        IServiceProvider services,
        CancellationToken ct)
    {
        var reportService = services.GetRequiredService<IReportService>();
        var aiService = services.GetRequiredService<IAiNarrativeService>();
        var emailService = services.GetRequiredService<IEmailService>();
        var pdfService = services.GetRequiredService<IPdfExportService>();

        // Obtener reportes del período
        var reports = await GetReportsForPeriodAsync(reportService);

        if (!reports.Any())
        {
            _logger.LogInformation("No reports to process for template {Id}", template.Id);
            return;
        }

        // Generar narrativa con IA
        var narrative = await aiService.GenerateNarrativeAsync(template.Prompt, reports);

        // Generar PDF
        var pdf = await GeneratePdfAsync(pdfService, narrative, reports);

        // Enviar por email
        var recipients = JsonSerializer.Deserialize<string[]>(template.Destinatarios);
        foreach (var recipient in recipients)
        {
            await emailService.SendWithAttachmentAsync(
                recipient,
                $"Reporte Automatizado - {DateTime.Now:yyyy-MM-dd}",
                narrative,
                pdf,
                "reporte.pdf");
        }

        // Guardar registro
        await SaveAutomatedReportAsync(template, narrative, reports.Count);
    }
}
```

## Configuración

```json
{
  "AI": {
    "Provider": "OpenAI",
    "ApiKey": "sk-...",
    "Model": "gpt-4",
    "Endpoint": "https://api.openai.com/v1",
    "Temperature": 0.7,
    "MaxTokens": 4000
  },
  "AutomatedReports": {
    "GenerationTime": "06:00:00",
    "TimeZone": "America/Mexico_City",
    "Recipients": ["supervisor@org.com"]
  }
}
```

## Próximos Pasos

- [Agregar campo al reporte](Dev-Guia-Agregar-Campo)
- [Uso de reportes automatizados](Usuario-Revisor-Reportes-Automatizados)
