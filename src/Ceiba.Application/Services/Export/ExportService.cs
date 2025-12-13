using Ceiba.Core.Entities;
using Ceiba.Core.Interfaces;
using Ceiba.Shared.DTOs.Export;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Ceiba.Application.Services.Export;

/// <summary>
/// High-level export service with authorization and data retrieval
/// Implements T-US2-021 to T-US2-030 test requirements
/// RT-003 Mitigations: Export limits, monitoring, and background job support
/// </summary>
public class ExportService : IExportService
{
    /// <summary>
    /// Maximum number of reports allowed for PDF export (T052a)
    /// </summary>
    public const int MaxPdfReports = 50;

    /// <summary>
    /// Maximum number of reports allowed for JSON export (T052a)
    /// </summary>
    public const int MaxJsonReports = 100;

    /// <summary>
    /// Threshold for background export job (T052c)
    /// </summary>
    public const int BackgroundExportThreshold = 50;

    /// <summary>
    /// Alert threshold for export duration in seconds (T052e)
    /// </summary>
    public const int AlertDurationSeconds = 30;

    /// <summary>
    /// Alert threshold for export file size in bytes (500MB) (T052e)
    /// </summary>
    public const long AlertFileSizeBytes = 500 * 1024 * 1024;

    private readonly IReportRepository _reportRepository;
    private readonly IPdfGenerator _pdfGenerator;
    private readonly IJsonExporter _jsonExporter;
    private readonly IUserManagementService _userService;
    private readonly ILogger<ExportService> _logger;

    public ExportService(
        IReportRepository reportRepository,
        IPdfGenerator pdfGenerator,
        IJsonExporter jsonExporter,
        IUserManagementService userService,
        ILogger<ExportService> logger)
    {
        _reportRepository = reportRepository;
        _pdfGenerator = pdfGenerator;
        _jsonExporter = jsonExporter;
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Exports reports based on request parameters
    /// RT-003 Mitigations: Enforces export limits (T052a), monitoring (T052e)
    /// </summary>
    /// <param name="request">Export request with format and options</param>
    /// <param name="userId">ID of user requesting export</param>
    /// <param name="isRevisor">Whether user has REVISOR role</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Export result with file data and metadata</returns>
    /// <exception cref="UnauthorizedAccessException">If user is not REVISOR</exception>
    /// <exception cref="ArgumentException">If no report IDs provided or limit exceeded</exception>
    public async Task<ExportResultDto> ExportReportsAsync(
        ExportRequestDto request,
        Guid userId,
        bool isRevisor,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

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

        // T052a: Enforce export limits with clear error messages
        ValidateExportLimits(request.ReportIds.Length, request.Format);

        // Retrieve reports from repository
        var reports = new List<ReporteIncidencia>();
        foreach (var reportId in request.ReportIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
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

        // Map entities to DTOs (need to resolve user emails)
        var exportDtos = new List<ReportExportDto>();
        foreach (var report in reports)
        {
            cancellationToken.ThrowIfCancellationRequested();
            exportDtos.Add(await MapToExportDtoAsync(report));
        }

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

        stopwatch.Stop();

        // T052e: Export monitoring with alerts
        LogExportMetrics(reports.Count, fileData.Length, stopwatch.Elapsed, request.Format, userId);

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
    /// T052a: Validates export limits and throws ArgumentException with clear error message if exceeded
    /// </summary>
    private static void ValidateExportLimits(int reportCount, ExportFormat format)
    {
        var maxReports = format == ExportFormat.PDF ? MaxPdfReports : MaxJsonReports;
        var formatName = format == ExportFormat.PDF ? "PDF" : "JSON";

        if (reportCount > maxReports)
        {
            throw new ArgumentException(
                $"El límite de exportación {formatName} es de {maxReports} reportes. " +
                $"Ha solicitado exportar {reportCount} reportes. " +
                $"Por favor, reduzca la selección o utilice la exportación en segundo plano para cantidades mayores.",
                nameof(reportCount));
        }
    }

    /// <summary>
    /// T052e: Logs export metrics and triggers alerts for slow or large exports
    /// </summary>
    private void LogExportMetrics(int reportCount, long fileSizeBytes, TimeSpan duration, ExportFormat format, Guid userId)
    {
        var fileSizeMb = fileSizeBytes / (1024.0 * 1024.0);
        var formatName = format == ExportFormat.PDF ? "PDF" : "JSON";

        _logger.LogInformation(
            "Export completed: {ReportCount} reports, {Format}, {FileSizeMb:F2}MB, {DurationMs}ms, User: {UserId}",
            reportCount, formatName, fileSizeMb, duration.TotalMilliseconds, userId);

        // Alert for slow exports
        if (duration.TotalSeconds > AlertDurationSeconds)
        {
            _logger.LogWarning(
                "ALERT: Slow export detected - {DurationSeconds:F1}s exceeds {ThresholdSeconds}s threshold. " +
                "Reports: {ReportCount}, Format: {Format}, Size: {FileSizeMb:F2}MB, User: {UserId}",
                duration.TotalSeconds, AlertDurationSeconds, reportCount, formatName, fileSizeMb, userId);
        }

        // Alert for large files
        if (fileSizeBytes > AlertFileSizeBytes)
        {
            _logger.LogWarning(
                "ALERT: Large export detected - {FileSizeMb:F2}MB exceeds {ThresholdMb}MB threshold. " +
                "Reports: {ReportCount}, Format: {Format}, Duration: {DurationMs}ms, User: {UserId}",
                fileSizeMb, AlertFileSizeBytes / (1024.0 * 1024.0), reportCount, formatName, duration.TotalMilliseconds, userId);
        }
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

        // Map entity to DTO (need to resolve user email)
        var exportDto = await MapToExportDtoAsync(report);

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
    /// Resolves user email for display in PDF header
    /// </summary>
    private async Task<ReportExportDto> MapToExportDtoAsync(ReporteIncidencia report)
    {
        // Generate folio if not present (format: CEIBA-YYYY-NNNNNN)
        var folio = $"CEIBA-{report.CreatedAt.Year}-{report.Id:D6}";

        // Get user email for display (GUID remains for audit)
        string usuarioEmail = report.UsuarioId.ToString(); // Fallback to GUID
        try
        {
            var user = await _userService.GetUserByIdAsync(report.UsuarioId);
            if (user?.Email != null)
            {
                usuarioEmail = user.Email;
            }
        }
        catch
        {
            // If user lookup fails, keep GUID as fallback
        }

        return new ReportExportDto
        {
            Id = report.Id,
            Folio = folio,
            Estado = report.Estado == 0 ? "Borrador" : "Entregado",
            FechaCreacion = report.CreatedAt,
            FechaEntrega = report.Estado == 1 ? report.UpdatedAt : null,
            UsuarioCreador = usuarioEmail, // Email for header display
            UsuarioCreadorId = report.UsuarioId.ToString(), // GUID for audit

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

            // Geographic Location (Zona → Región → Sector → Cuadrante)
            Zona = report.Zona?.Nombre ?? string.Empty,
            Region = report.Region?.Nombre ?? string.Empty,
            Sector = report.Sector?.Nombre ?? string.Empty,
            Cuadrante = report.Cuadrante?.Nombre ?? string.Empty,
            TurnoCeiba = MapTurnoCeiba(report.TurnoCeiba),

            // Incident Details
            HechosReportados = report.HechosReportados,
            AccionesRealizadas = report.AccionesRealizadas,
            Traslados = MapTraslados(report.Traslados),
            Observaciones = report.Observaciones,

            // Audit Information (uses GUID for technical identification)
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
