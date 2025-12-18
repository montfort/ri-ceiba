# Módulo de Exportación

El módulo de exportación genera archivos PDF y JSON de reportes.

## Componentes

```
src/
├── Ceiba.Application/
│   └── Services/Export/
│       ├── IExportService.cs
│       └── ExportService.cs
├── Ceiba.Infrastructure/
│   └── Services/
│       ├── PdfExportService.cs
│       └── JsonExportService.cs
└── Ceiba.Web/
    └── Components/Pages/Supervisor/
        └── ExportPage.razor
```

## Servicio de Exportación

### Interfaz

```csharp
public interface IExportService
{
    Task<ExportResultDto> ExportReportsAsync(
        ExportRequestDto request,
        Guid userId,
        bool isRevisor);
}

public class ExportRequestDto
{
    public int[] ReportIds { get; set; }
    public ExportFormat Format { get; set; }
}

public enum ExportFormat
{
    PDF,
    JSON
}
```

### Implementación

```csharp
public class ExportService : IExportService
{
    private readonly IPdfExportService _pdfService;
    private readonly IJsonExportService _jsonService;
    private readonly IReportService _reportService;

    public async Task<ExportResultDto> ExportReportsAsync(
        ExportRequestDto request,
        Guid userId,
        bool isRevisor)
    {
        // Obtener reportes
        var reports = await GetReportsAsync(request.ReportIds, userId, isRevisor);

        // Generar según formato
        var result = request.Format switch
        {
            ExportFormat.PDF => await _pdfService.GenerateAsync(reports),
            ExportFormat.JSON => await _jsonService.GenerateAsync(reports),
            _ => throw new ArgumentException("Formato no soportado")
        };

        // Auditar
        await _auditService.LogAsync("EXPORT", "Reportes",
            string.Join(",", request.ReportIds), userId,
            new { Format = request.Format, Count = reports.Count });

        return result;
    }
}
```

## Generación de PDF

### Con QuestPDF

```csharp
public class PdfExportService : IPdfExportService
{
    public async Task<ExportResultDto> GenerateAsync(List<ReportDto> reports)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(2, Unit.Centimetre);

                page.Header().Element(ComposeHeader);
                page.Content().Element(c => ComposeContent(c, reports));
                page.Footer().Element(ComposeFooter);
            });
        });

        var bytes = document.GeneratePdf();

        return new ExportResultDto
        {
            Data = bytes,
            FileName = $"reportes_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf",
            ContentType = "application/pdf",
            ReportCount = reports.Count
        };
    }

    private void ComposeContent(IContainer container, List<ReportDto> reports)
    {
        container.Column(column =>
        {
            foreach (var report in reports)
            {
                column.Item().Element(c => ComposeReport(c, report));
                column.Item().PageBreak();
            }
        });
    }

    private void ComposeReport(IContainer container, ReportDto report)
    {
        container.Column(column =>
        {
            // Encabezado del reporte
            column.Item().Text($"Reporte #{report.Id}")
                .FontSize(16).Bold();

            // Datos
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(1);
                    c.RelativeColumn(2);
                });

                table.Cell().Text("Delito:");
                table.Cell().Text(report.Delito);

                table.Cell().Text("Zona:");
                table.Cell().Text(report.Zona.Nombre);
                // ... más campos
            });

            // Narrativa
            column.Item().Text("Hechos Reportados").Bold();
            column.Item().Text(report.HechosReportados);
        });
    }
}
```

## Generación de JSON

```csharp
public class JsonExportService : IJsonExportService
{
    public async Task<ExportResultDto> GenerateAsync(List<ReportDto> reports)
    {
        var export = new
        {
            exportDate = DateTime.UtcNow,
            reportCount = reports.Count,
            reports = reports.Select(r => new
            {
                id = r.Id,
                tipoReporte = r.TipoReporte,
                createdAt = r.CreatedAt,
                datetimeHechos = r.DatetimeHechos,
                estado = r.Estado,
                creador = new { id = r.Creador.Id, email = r.Creador.Email },
                victima = new
                {
                    sexo = r.Sexo,
                    edad = r.Edad,
                    lgbtttiqPlus = r.LgbtttiqPlus,
                    situacionCalle = r.SituacionCalle,
                    migrante = r.Migrante,
                    discapacidad = r.Discapacidad
                },
                ubicacion = new
                {
                    zona = new { id = r.Zona.Id, nombre = r.Zona.Nombre },
                    region = new { id = r.Region.Id, nombre = r.Region.Nombre },
                    sector = new { id = r.Sector.Id, nombre = r.Sector.Nombre },
                    cuadrante = new { id = r.Cuadrante.Id, nombre = r.Cuadrante.Nombre }
                },
                // ... más campos
            })
        };

        var json = JsonSerializer.Serialize(export, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return new ExportResultDto
        {
            Data = Encoding.UTF8.GetBytes(json),
            FileName = $"reportes_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json",
            ContentType = "application/json",
            ReportCount = reports.Count
        };
    }
}
```

## Descarga en el Cliente

### JavaScript Interop

```javascript
// wwwroot/js/download.js
window.downloadFileFromBase64 = function(base64, fileName, contentType) {
    const link = document.createElement('a');
    link.href = `data:${contentType};base64,${base64}`;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};
```

### Blazor

```csharp
await JS.InvokeVoidAsync("downloadFileFromBase64",
    Convert.ToBase64String(result.Data),
    result.FileName,
    result.ContentType);
```

## Próximos Pasos

- [Módulo de Auditoría](Dev-Modulo-Auditoria)
- [Reportes Automatizados](Dev-Modulo-Reportes-Automatizados)
