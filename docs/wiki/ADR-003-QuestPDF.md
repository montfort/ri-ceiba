# ADR-003: Uso de QuestPDF

**Estado:** Aceptado
**Fecha:** 2025-01-15
**Autores:** Equipo de Desarrollo

## Contexto

Necesitamos generar documentos PDF para:
- Exportación de reportes individuales
- Exportación masiva de reportes
- Reportes automatizados con IA
- Posible impresión oficial

Requisitos:
- Diseño profesional y consistente
- Soporte para español (acentos, ñ)
- Rendimiento para generación masiva
- Código C# mantenible (no templates XML/HTML)

## Opciones Consideradas

### Opción 1: QuestPDF

- API fluida en C#
- Open source (MIT)
- Diseño moderno con layout automático

### Opción 2: iTextSharp / iText 7

- Muy potente y completo
- Licencia AGPL (requiere licencia comercial)
- API más compleja

### Opción 3: PDFsharp / MigraDoc

- Open source
- Menos documentación actualizada
- API menos moderna

### Opción 4: Puppeteer/Playwright + HTML

- Renderizar HTML a PDF
- Requiere navegador headless
- Más peso en servidor

### Opción 5: Servicios externos (API)

- Azure PDF, AWS, etc.
- Costos por uso
- Dependencia externa

## Decisión

**Elegimos QuestPDF** por las siguientes razones:

1. **API Fluida**: Código C# limpio y legible
2. **Licencia MIT**: Open source sin restricciones
3. **Rendimiento**: Optimizado para generación rápida
4. **Documentación**: Excelente documentación y ejemplos
5. **Layout Moderno**: Sistema de layout similar a Flexbox
6. **Hot Reload**: Companion app para preview en desarrollo

## Ejemplo de Uso

```csharp
Document.Create(container =>
{
    container.Page(page =>
    {
        page.Size(PageSizes.Letter);
        page.Margin(2, Unit.Centimetre);

        page.Header().Row(row =>
        {
            row.RelativeItem().Text("Reporte de Incidencia")
                .FontSize(20).Bold();
        });

        page.Content().Column(column =>
        {
            column.Item().Text($"Delito: {report.Delito}");
            column.Item().Text($"Fecha: {report.DatetimeHechos:dd/MM/yyyy}");
            column.Item().Text(report.HechosReportados);
        });

        page.Footer().Text(text =>
        {
            text.Span("Página ");
            text.CurrentPageNumber();
            text.Span(" de ");
            text.TotalPages();
        });
    });
}).GeneratePdf(stream);
```

## Consecuencias

### Positivas

- Código muy legible y mantenible
- Sin costos de licencia
- Excelente rendimiento
- Fácil de testear (componentes separados)
- Companion app para desarrollo

### Negativas

- Menos features que iText para casos muy complejos
- Comunidad más pequeña
- Proyecto relativamente nuevo (pero estable)

## Configuración

```xml
<PackageReference Include="QuestPDF" Version="2024.10.0" />
```

```csharp
// En Program.cs
QuestPDF.Settings.License = LicenseType.Community;
```

## Referencias

- [QuestPDF Documentation](https://www.questpdf.com/)
- [QuestPDF GitHub](https://github.com/QuestPDF/QuestPDF)
- [Comparación de librerías PDF](https://www.questpdf.com/comparison.html)
