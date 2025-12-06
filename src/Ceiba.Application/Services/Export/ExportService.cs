using Ceiba.Core.Entities;
using Ceiba.Core.Interfaces;
using Ceiba.Shared.DTOs.Export;

namespace Ceiba.Application.Services.Export;

/// <summary>
/// High-level export service with authorization and data retrieval
/// Implements T-US2-021 to T-US2-030 test requirements
/// </summary>
public class ExportService : IExportService
{
    private readonly IReportRepository _reportRepository;
    private readonly IPdfGenerator _pdfGenerator;
    private readonly IJsonExporter _jsonExporter;

    public ExportService(
        IReportRepository reportRepository,
        IPdfGenerator pdfGenerator,
        IJsonExporter jsonExporter)
    {
        _reportRepository = reportRepository;
        _pdfGenerator = pdfGenerator;
        _jsonExporter = jsonExporter;
    }

    /// <summary>
    /// Exports reports based on request parameters
    /// </summary>
    /// <param name="request">Export request with format and options</param>
    /// <param name="userId">ID of user requesting export</param>
    /// <param name="isRevisor">Whether user has REVISOR role</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Export result with file data and metadata</returns>
    /// <exception cref="UnauthorizedAccessException">If user is not REVISOR</exception>
    /// <exception cref="ArgumentException">If no report IDs provided</exception>
    public async Task<ExportResultDto> ExportReportsAsync(
        ExportRequestDto request,
        Guid userId,
        bool isRevisor,
        CancellationToken cancellationToken = default)
    {
        // Authorization check
        if (!isRevisor)
        {
            throw new UnauthorizedAccessException("Only users with REVISOR role can export reports");
        }

        // Validate request
        if (request.ReportIds == null || request.ReportIds.Length == 0)
        {
            throw new ArgumentException("Must provide at least one report ID to export", nameof(request));
        }

        // Retrieve reports from repository
        var reports = new List<ReporteIncidencia>();
        foreach (var reportId in request.ReportIds)
        {
            var report = await _reportRepository.GetByIdWithRelationsAsync(reportId);
            if (report != null)
            {
                reports.Add(report);
            }
        }

        if (reports.Count == 0)
        {
            throw new KeyNotFoundException("No reports found with the provided IDs");
        }

        // Map entities to DTOs
        var exportDtos = reports.Select(MapToExportDto).ToList();

        // Generate export based on format
        byte[] fileData;
        string contentType;
        string fileExtension;

        if (request.Format == ExportFormat.PDF)
        {
            fileData = reports.Count == 1
                ? _pdfGenerator.GenerateSingleReport(exportDtos[0])
                : _pdfGenerator.GenerateMultipleReports(exportDtos);
            contentType = "application/pdf";
            fileExtension = "pdf";
        }
        else // JSON
        {
            fileData = reports.Count == 1
                ? _jsonExporter.ExportSingleReport(exportDtos[0], request.Options)
                : _jsonExporter.ExportMultipleReports(exportDtos, request.Options);
            contentType = "application/json";
            fileExtension = "json";
        }

        // Generate filename
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var fileName = reports.Count == 1
            ? $"reporte_{exportDtos[0].Folio}_{timestamp}.{fileExtension}"
            : $"reportes_{reports.Count}_{timestamp}.{fileExtension}";

        return new ExportResultDto
        {
            Data = fileData,
            FileName = fileName,
            ContentType = contentType,
            ReportCount = reports.Count,
            GeneratedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Exports a single report by ID
    /// </summary>
    /// <param name="reportId">ID of report to export</param>
    /// <param name="format">Export format (PDF or JSON)</param>
    /// <param name="userId">ID of user requesting export</param>
    /// <param name="isRevisor">Whether user has REVISOR role</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Export result with file data and metadata</returns>
    /// <exception cref="UnauthorizedAccessException">If user is not REVISOR</exception>
    /// <exception cref="KeyNotFoundException">If report not found</exception>
    public async Task<ExportResultDto> ExportSingleReportAsync(
        int reportId,
        ExportFormat format,
        Guid userId,
        bool isRevisor,
        CancellationToken cancellationToken = default)
    {
        // Authorization check
        if (!isRevisor)
        {
            throw new UnauthorizedAccessException("Only users with REVISOR role can export reports");
        }

        // Retrieve report from repository
        var report = await _reportRepository.GetByIdWithRelationsAsync(reportId);
        if (report == null)
        {
            throw new KeyNotFoundException($"Report with ID {reportId} not found");
        }

        // Map entity to DTO
        var exportDto = MapToExportDto(report);

        // Generate export based on format
        byte[] fileData;
        string contentType;
        string fileExtension;

        if (format == ExportFormat.PDF)
        {
            fileData = _pdfGenerator.GenerateSingleReport(exportDto);
            contentType = "application/pdf";
            fileExtension = "pdf";
        }
        else // JSON
        {
            fileData = _jsonExporter.ExportSingleReport(exportDto);
            contentType = "application/json";
            fileExtension = "json";
        }

        // Generate filename
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var fileName = $"reporte_{exportDto.Folio}_{timestamp}.{fileExtension}";

        return new ExportResultDto
        {
            Data = fileData,
            FileName = fileName,
            ContentType = contentType,
            ReportCount = 1,
            GeneratedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Maps ReporteIncidencia entity to ReportExportDto
    /// </summary>
    private static ReportExportDto MapToExportDto(ReporteIncidencia report)
    {
        // Generate folio if not present (format: CEIBA-YYYY-NNNNNN)
        var folio = $"CEIBA-{report.CreatedAt.Year}-{report.Id:D6}";

        return new ReportExportDto
        {
            Id = report.Id,
            Folio = folio,
            Estado = report.Estado == 0 ? "Borrador" : "Entregado",
            FechaCreacion = report.CreatedAt,
            FechaEntrega = report.Estado == 1 ? report.UpdatedAt : null,
            UsuarioCreador = report.UsuarioId.ToString(),

            // Demographic Data
            Sexo = report.Sexo,
            Edad = report.Edad,
            LgbtttiqPlus = report.LgbtttiqPlus,
            SituacionCalle = report.SituacionCalle,
            Migrante = report.Migrante,
            Discapacidad = report.Discapacidad,

            // Classification
            Delito = report.Delito,
            TipoDeAtencion = report.TipoDeAtencion,
            TipoDeAccion = MapTipoDeAccion(report.TipoDeAccion),

            // Geographic Location
            Zona = report.Zona?.Nombre ?? string.Empty,
            Sector = report.Sector?.Nombre ?? string.Empty,
            Cuadrante = report.Cuadrante?.Nombre ?? string.Empty,
            TurnoCeiba = MapTurnoCeiba(report.TurnoCeiba),

            // Incident Details
            HechosReportados = report.HechosReportados,
            AccionesRealizadas = report.AccionesRealizadas,
            Traslados = MapTraslados(report.Traslados),
            Observaciones = report.Observaciones,

            // Audit Information (optional)
            FechaUltimaModificacion = report.UpdatedAt,
            UsuarioUltimaModificacion = report.UsuarioId.ToString()
        };
    }

    /// <summary>
    /// Maps TipoDeAccion numeric code to string
    /// </summary>
    private static string MapTipoDeAccion(short tipoDeAccion)
    {
        return tipoDeAccion switch
        {
            1 => "Orientación",
            2 => "Capacitación",
            3 => "Prevención",
            _ => "Desconocido"
        };
    }

    /// <summary>
    /// Maps TurnoCeiba numeric code to string
    /// </summary>
    private static string MapTurnoCeiba(int turnoCeiba)
    {
        return turnoCeiba switch
        {
            1 => "Matutino",
            2 => "Vespertino",
            3 => "Nocturno",
            _ => "Desconocido"
        };
    }

    /// <summary>
    /// Maps Traslados numeric code to string
    /// </summary>
    private static string MapTraslados(short traslados)
    {
        return traslados switch
        {
            0 => "Sin traslados",
            1 => "Con traslados",
            2 => "No aplica",
            _ => "Desconocido"
        };
    }
}
