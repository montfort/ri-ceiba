using Ceiba.Shared.DTOs.Export;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Ceiba.Application.Services.Export;

/// <summary>
/// PDF document generation service using QuestPDF
/// Implements T-US2-001 to T-US2-010 test requirements
/// </summary>
public class PdfGenerator : IPdfGenerator
{
    static PdfGenerator()
    {
        // Configure QuestPDF license for Community use
        // https://www.questpdf.com/license/
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
    }
    /// <summary>
    /// Generates a PDF document from a single report
    /// </summary>
    /// <param name="report">Report data to export</param>
    /// <returns>PDF file as byte array</returns>
    /// <exception cref="ArgumentNullException">If report is null</exception>
    public byte[] GenerateSingleReport(ReportExportDto report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header()
                    .Element(c => ComposeHeader(c, report));

                page.Content()
                    .Element(c => ComposeContent(c, report));

                page.Footer()
                    .Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    /// <summary>
    /// Generates a PDF document containing multiple reports
    /// </summary>
    /// <param name="reports">Collection of reports to export</param>
    /// <returns>PDF file as byte array</returns>
    /// <exception cref="ArgumentException">If reports collection is empty</exception>
    public byte[] GenerateMultipleReports(IEnumerable<ReportExportDto> reports)
    {
        var reportList = reports?.ToList() ?? throw new ArgumentException("Reports collection cannot be null", nameof(reports));

        if (reportList.Count == 0)
        {
            throw new ArgumentException("Must provide at least one report to export", nameof(reports));
        }

        var document = Document.Create(container =>
        {
            foreach (var report in reportList)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header()
                        .Element(c => ComposeHeader(c, report));

                    page.Content()
                        .Element(c => ComposeContent(c, report));

                    page.Footer()
                        .Element(ComposeFooter);
                });
            }
        });

        return document.GeneratePdf();
    }

    /// <summary>
    /// Composes the PDF header with CEIBA branding
    /// </summary>
    private void ComposeHeader(IContainer container, ReportExportDto report)
    {
        container.Column(column =>
        {
            // CEIBA Branding
            column.Item().Background(Colors.Blue.Darken3).Padding(10).Row(row =>
            {
                row.RelativeItem().AlignLeft().Text("CEIBA - Centro de Atención a Víctimas")
                    .FontSize(14)
                    .Bold()
                    .FontColor(Colors.White);

                row.ConstantItem(120).AlignRight().Text($"Folio: {report.Folio}")
                    .FontSize(10)
                    .FontColor(Colors.White);
            });

            // Report metadata
            column.Item().PaddingTop(10).Row(row =>
            {
                row.RelativeItem().Text($"Estado: {report.Estado}").FontSize(9);
                row.RelativeItem().Text($"Creado: {report.FechaCreacion:dd/MM/yyyy HH:mm}").FontSize(9);
                if (report.FechaEntrega.HasValue)
                {
                    row.RelativeItem().Text($"Entregado: {report.FechaEntrega.Value:dd/MM/yyyy HH:mm}").FontSize(9);
                }
            });

            column.Item().PaddingTop(5).Text($"Usuario: {report.UsuarioCreador}").FontSize(9);

            // Separator
            column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);
        });
    }

    /// <summary>
    /// Composes the PDF content with all report sections
    /// </summary>
    private void ComposeContent(IContainer container, ReportExportDto report)
    {
        container.PaddingTop(10).Column(column =>
        {
            column.Item().PaddingBottom(10).Element(c => ComposeDemographicSection(c, report));
            column.Item().PaddingBottom(10).Element(c => ComposeClassificationSection(c, report));
            column.Item().PaddingBottom(10).Element(c => ComposeGeographicSection(c, report));
            column.Item().PaddingBottom(10).Element(c => ComposeIncidentDetailsSection(c, report));
            column.Item().PaddingTop(10).Element(c => ComposeAuditSection(c, report));
        });
    }

    /// <summary>
    /// Composes the demographic data section
    /// </summary>
    private static void ComposeDemographicSection(IContainer container, ReportExportDto report)
    {
        container.Column(section =>
        {
            section.Item().Text("DATOS DEMOGRÁFICOS").FontSize(12).Bold().FontColor(Colors.Blue.Darken2);
            section.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text($"Sexo: {report.Sexo}").FontSize(9);
                row.RelativeItem().Text($"Edad: {report.Edad} años").FontSize(9);
            });
            section.Item().PaddingTop(3).Row(row =>
            {
                row.RelativeItem().Text($"LGBTTTIQ+: {FormatBoolean(report.LgbtttiqPlus)}").FontSize(9);
                row.RelativeItem().Text($"Situación de calle: {FormatBoolean(report.SituacionCalle)}").FontSize(9);
            });
            section.Item().PaddingTop(3).Row(row =>
            {
                row.RelativeItem().Text($"Migrante: {FormatBoolean(report.Migrante)}").FontSize(9);
                row.RelativeItem().Text($"Discapacidad: {FormatBoolean(report.Discapacidad)}").FontSize(9);
            });
        });
    }

    /// <summary>
    /// Composes the classification section
    /// </summary>
    private static void ComposeClassificationSection(IContainer container, ReportExportDto report)
    {
        container.Column(section =>
        {
            section.Item().Text("CLASIFICACIÓN").FontSize(12).Bold().FontColor(Colors.Blue.Darken2);
            section.Item().PaddingTop(5).Text($"Delito: {report.Delito}").FontSize(9);
            section.Item().PaddingTop(3).Text($"Tipo de Atención: {report.TipoDeAtencion}").FontSize(9);
            section.Item().PaddingTop(3).Text($"Tipo de Acción: {report.TipoDeAccion}").FontSize(9);
        });
    }

    /// <summary>
    /// Composes the geographic location section
    /// </summary>
    private static void ComposeGeographicSection(IContainer container, ReportExportDto report)
    {
        container.Column(section =>
        {
            section.Item().Text("UBICACIÓN GEOGRÁFICA").FontSize(12).Bold().FontColor(Colors.Blue.Darken2);
            section.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text($"Zona: {report.Zona}").FontSize(9);
                row.RelativeItem().Text($"Sector: {report.Sector}").FontSize(9);
            });
            section.Item().PaddingTop(3).Row(row =>
            {
                row.RelativeItem().Text($"Cuadrante: {report.Cuadrante}").FontSize(9);
                row.RelativeItem().Text($"Turno CEIBA: {report.TurnoCeiba}").FontSize(9);
            });
        });
    }

    /// <summary>
    /// Composes the incident details section
    /// </summary>
    private static void ComposeIncidentDetailsSection(IContainer container, ReportExportDto report)
    {
        container.Column(section =>
        {
            section.Item().Text("DETALLES DEL INCIDENTE").FontSize(12).Bold().FontColor(Colors.Blue.Darken2);

            section.Item().PaddingTop(5).Text("Hechos Reportados:").FontSize(9).Bold();
            section.Item().PaddingTop(3).Text(report.HechosReportados).FontSize(9);

            section.Item().PaddingTop(5).Text("Acciones Realizadas:").FontSize(9).Bold();
            section.Item().PaddingTop(3).Text(report.AccionesRealizadas).FontSize(9);

            section.Item().PaddingTop(5).Text("Traslados:").FontSize(9).Bold();
            section.Item().PaddingTop(3).Text(report.Traslados).FontSize(9);

            AddOptionalField(section, "Observaciones:", report.Observaciones);
        });
    }

    /// <summary>
    /// Composes the audit information section
    /// </summary>
    private static void ComposeAuditSection(IContainer container, ReportExportDto report)
    {
        container.Column(section =>
        {
            section.Item().LineHorizontal(1).LineColor(Colors.Grey.Medium);
            section.Item().PaddingTop(5).Text("INFORMACIÓN DE AUDITORÍA").FontSize(10).Bold().FontColor(Colors.Grey.Darken2);

            AddOptionalAuditField(section, "ID Usuario Creador:", report.UsuarioCreadorId);
            AddOptionalDateField(section, "Última modificación:", report.FechaUltimaModificacion);
        });
    }

    /// <summary>
    /// Formats a boolean value as "Sí" or "No"
    /// </summary>
    private static string FormatBoolean(bool value) => value ? "Sí" : "No";

    /// <summary>
    /// Adds an optional text field if value is not empty
    /// </summary>
    private static void AddOptionalField(ColumnDescriptor section, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            section.Item().PaddingTop(5).Text(label).FontSize(9).Bold();
            section.Item().PaddingTop(3).Text(value).FontSize(9);
        }
    }

    /// <summary>
    /// Adds an optional audit field if value is not empty
    /// </summary>
    private static void AddOptionalAuditField(ColumnDescriptor section, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            section.Item().PaddingTop(3).Text($"{label} {value}").FontSize(8);
        }
    }

    /// <summary>
    /// Adds an optional date field if value has a value
    /// </summary>
    private static void AddOptionalDateField(ColumnDescriptor section, string label, DateTime? value)
    {
        if (value.HasValue)
        {
            section.Item().PaddingTop(2).Text($"{label} {value.Value:dd/MM/yyyy HH:mm}").FontSize(8);
        }
    }

    /// <summary>
    /// Composes the PDF footer with page numbers and generation timestamp
    /// </summary>
    private void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Column(column =>
        {
            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Medium);
            column.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().AlignLeft()
                    .Text($"Generado: {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC")
                    .FontSize(8)
                    .FontColor(Colors.Grey.Darken1);

                row.RelativeItem().AlignCenter().Text(text =>
                {
                    text.Span("Página ").FontSize(8).FontColor(Colors.Grey.Darken1);
                    text.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Darken1);
                    text.Span(" de ").FontSize(8).FontColor(Colors.Grey.Darken1);
                    text.TotalPages().FontSize(8).FontColor(Colors.Grey.Darken1);
                });

                row.RelativeItem().AlignRight()
                    .Text("CEIBA - Sistema de Reportes")
                    .FontSize(8)
                    .FontColor(Colors.Grey.Darken1);
            });
        });
    }
}
