using System.Text;
using System.Text.Json;
using Ceiba.Core.Entities;
using Ceiba.Core.Interfaces;
using Ceiba.Infrastructure.Data;
using Ceiba.Shared.DTOs;
using Markdig;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Ceiba.Infrastructure.Services;

/// <summary>
/// Service for managing automated daily reports.
/// US4: Reportes Automatizados Diarios con IA.
/// </summary>
public class AutomatedReportService : IAutomatedReportService
{
    private readonly CeibaDbContext _context;
    private readonly IAiNarrativeService _aiService;
    private readonly IEmailService _emailService;
    private readonly IAuditService _auditService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AutomatedReportService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public AutomatedReportService(
        CeibaDbContext context,
        IAiNarrativeService aiService,
        IEmailService emailService,
        IAuditService auditService,
        IConfiguration configuration,
        ILogger<AutomatedReportService> logger)
    {
        _context = context;
        _aiService = aiService;
        _emailService = emailService;
        _auditService = auditService;
        _configuration = configuration;
        _logger = logger;
    }

    #region Automated Reports

    public async Task<List<AutomatedReportListDto>> GetReportsAsync(
        int skip = 0,
        int take = 20,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        bool? enviado = null)
    {
        var query = _context.ReportesAutomatizados
            .Include(r => r.ModeloReporte)
            .AsQueryable();

        if (fechaDesde.HasValue)
        {
            var fechaDesdeUtc = DateTime.SpecifyKind(fechaDesde.Value, DateTimeKind.Utc);
            query = query.Where(r => r.FechaInicio >= fechaDesdeUtc);
        }

        if (fechaHasta.HasValue)
        {
            var fechaHastaUtc = DateTime.SpecifyKind(fechaHasta.Value, DateTimeKind.Utc);
            query = query.Where(r => r.FechaFin <= fechaHastaUtc);
        }

        if (enviado.HasValue)
            query = query.Where(r => r.Enviado == enviado.Value);

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Select(r => new AutomatedReportListDto
            {
                Id = r.Id,
                FechaInicio = r.FechaInicio,
                FechaFin = r.FechaFin,
                CreatedAt = r.CreatedAt,
                Enviado = r.Enviado,
                FechaEnvio = r.FechaEnvio,
                TieneError = r.ErrorMensaje != null,
                NombreModelo = r.ModeloReporte != null ? r.ModeloReporte.Nombre : null,
                TotalReportes = ExtractTotalFromStats(r.Estadisticas)
            })
            .ToListAsync();
    }

    public async Task<AutomatedReportDetailDto?> GetReportByIdAsync(int id)
    {
        var report = await _context.ReportesAutomatizados
            .Include(r => r.ModeloReporte)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (report == null)
            return null;

        return new AutomatedReportDetailDto
        {
            Id = report.Id,
            FechaInicio = report.FechaInicio,
            FechaFin = report.FechaFin,
            ContenidoMarkdown = report.ContenidoMarkdown,
            ContenidoWordPath = report.ContenidoWordPath,
            Estadisticas = DeserializeStatistics(report.Estadisticas),
            Enviado = report.Enviado,
            FechaEnvio = report.FechaEnvio,
            ErrorMensaje = report.ErrorMensaje,
            ModeloReporteId = report.ModeloReporteId,
            NombreModelo = report.ModeloReporte?.Nombre,
            CreatedAt = report.CreatedAt
        };
    }

    public async Task<AutomatedReportDetailDto> GenerateReportAsync(
        GenerateReportRequestDto request,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        // Ensure dates are UTC for PostgreSQL compatibility
        var fechaInicio = DateTime.SpecifyKind(request.FechaInicio, DateTimeKind.Utc);
        var fechaFin = DateTime.SpecifyKind(request.FechaFin, DateTimeKind.Utc);

        _logger.LogInformation(
            "Generating automated report for period {Start} - {End}",
            fechaInicio,
            fechaFin);

        try
        {
            // 1. Calculate statistics
            var statistics = await CalculateStatisticsAsync(
                fechaInicio,
                fechaFin,
                cancellationToken);

            // 2. Get template
            ModeloReporte? template = null;
            if (request.ModeloReporteId.HasValue)
            {
                template = await _context.ModelosReporte
                    .FirstOrDefaultAsync(m => m.Id == request.ModeloReporteId.Value, cancellationToken);
            }
            template ??= await _context.ModelosReporte
                .FirstOrDefaultAsync(m => m.EsDefault && m.Activo, cancellationToken);

            // 3. Get sample incidents for narrative
            var incidents = await _context.ReportesIncidencia
                .Where(r => r.CreatedAt >= fechaInicio && r.CreatedAt < fechaFin)
                .OrderByDescending(r => r.CreatedAt)
                .Take(10)
                .Select(r => new { r.HechosReportados, r.AccionesRealizadas })
                .ToListAsync(cancellationToken);

            // 4. Generate AI narrative
            var narrativeRequest = new NarrativeRequestDto
            {
                Statistics = statistics,
                HechosReportados = incidents.Select(i => i.HechosReportados).ToList(),
                AccionesRealizadas = incidents.Select(i => i.AccionesRealizadas).ToList(),
                FechaInicio = fechaInicio,
                FechaFin = fechaFin
            };

            var narrativeResponse = await _aiService.GenerateNarrativeAsync(narrativeRequest, cancellationToken);

            // 5. Build markdown content
            var markdown = BuildMarkdownContent(
                template?.ContenidoMarkdown,
                statistics,
                narrativeResponse.Narrativa,
                fechaInicio,
                fechaFin);

            // 6. Create report entity
            var report = new ReporteAutomatizado
            {
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                ContenidoMarkdown = markdown,
                Estadisticas = JsonSerializer.Serialize(statistics, JsonOptions),
                ModeloReporteId = template?.Id,
                Enviado = false
            };

            _context.ReportesAutomatizados.Add(report);
            await _context.SaveChangesAsync(cancellationToken);

            // 7. Generate Word document
            try
            {
                report.ContenidoWordPath = await GenerateWordDocumentAsync(report.Id, markdown, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate Word document for report {Id}", report.Id);
            }

            // 8. Send email if requested
            if (request.EnviarEmail)
            {
                var recipients = request.EmailDestinatarios?.Any() == true
                    ? request.EmailDestinatarios
                    : GetConfiguredRecipients();

                if (recipients.Any())
                {
                    await SendReportByEmailAsync(report.Id, recipients, cancellationToken);
                }
            }

            // 9. Audit log
            await _auditService.LogAsync(
                AuditCodes.AUTO_REPORT_GEN,
                report.Id,
                "ReporteAutomatizado",
                JsonSerializer.Serialize(new { fechaInicio, fechaFin, statistics.TotalReportes, UserId = userId }),
                null,
                cancellationToken);

            _logger.LogInformation("Automated report {Id} generated successfully", report.Id);

            return (await GetReportByIdAsync(report.Id))!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate automated report");

            // Log failure
            await _auditService.LogAsync(
                AuditCodes.AUTO_REPORT_FAIL,
                null,
                "ReporteAutomatizado",
                JsonSerializer.Serialize(new { Error = ex.Message, fechaInicio, fechaFin, UserId = userId }),
                null,
                cancellationToken);

            throw;
        }
    }

    public async Task<bool> SendReportByEmailAsync(
        int reportId,
        List<string>? recipients = null,
        CancellationToken cancellationToken = default)
    {
        var report = await _context.ReportesAutomatizados.FindAsync(new object[] { reportId }, cancellationToken);
        if (report == null)
            return false;

        var emailRecipients = recipients ?? GetConfiguredRecipients();
        if (!emailRecipients.Any())
        {
            _logger.LogWarning("No email recipients configured for automated report {Id}", reportId);
            return false;
        }

        try
        {
            // Convert markdown to HTML
            var htmlContent = Markdown.ToHtml(report.ContenidoMarkdown);

            var emailRequest = new SendEmailRequestDto
            {
                Recipients = emailRecipients,
                Subject = $"Reporte de Incidencias - {report.FechaInicio:dd/MM/yyyy} al {report.FechaFin:dd/MM/yyyy}",
                BodyHtml = WrapInHtmlTemplate(htmlContent)
            };

            // Attach Word document if available
            if (!string.IsNullOrEmpty(report.ContenidoWordPath) && File.Exists(report.ContenidoWordPath))
            {
                emailRequest.Attachments.Add(new EmailAttachmentDto
                {
                    FileName = Path.GetFileName(report.ContenidoWordPath),
                    Content = await File.ReadAllBytesAsync(report.ContenidoWordPath, cancellationToken),
                    ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                });
            }

            var result = await _emailService.SendAsync(emailRequest, cancellationToken);

            if (result.Success)
            {
                report.Enviado = true;
                report.FechaEnvio = result.SentAt ?? DateTime.UtcNow;
                report.ErrorMensaje = null;

                await _auditService.LogAsync(
                    AuditCodes.AUTO_REPORT_SEND,
                    reportId,
                    "ReporteAutomatizado",
                    JsonSerializer.Serialize(new { Recipients = emailRecipients }),
                    null,
                    cancellationToken);
            }
            else
            {
                report.ErrorMensaje = result.Error;

                await _auditService.LogAsync(
                    AuditCodes.AUTO_REPORT_FAIL,
                    reportId,
                    "ReporteAutomatizado",
                    JsonSerializer.Serialize(new { Error = result.Error }),
                    null,
                    cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send automated report {Id} by email", reportId);
            report.ErrorMensaje = ex.Message;
            await _context.SaveChangesAsync(cancellationToken);
            return false;
        }
    }

    public async Task<bool> DeleteReportAsync(int id)
    {
        var report = await _context.ReportesAutomatizados.FindAsync(id);
        if (report == null)
            return false;

        // Delete Word file if exists
        if (!string.IsNullOrEmpty(report.ContenidoWordPath) && File.Exists(report.ContenidoWordPath))
        {
            try
            {
                File.Delete(report.ContenidoWordPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete Word file for report {Id}", id);
            }
        }

        _context.ReportesAutomatizados.Remove(report);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<string?> RegenerateWordDocumentAsync(int reportId)
    {
        var report = await _context.ReportesAutomatizados.FindAsync(reportId);
        if (report == null)
            return null;

        var path = await GenerateWordDocumentAsync(reportId, report.ContenidoMarkdown);
        report.ContenidoWordPath = path;
        await _context.SaveChangesAsync();

        return path;
    }

    #endregion

    #region Templates

    public async Task<List<ReportTemplateListDto>> GetTemplatesAsync(bool includeInactive = false)
    {
        var query = _context.ModelosReporte.AsQueryable();

        if (!includeInactive)
            query = query.Where(m => m.Activo);

        return await query
            .OrderBy(m => m.Nombre)
            .Select(m => new ReportTemplateListDto
            {
                Id = m.Id,
                Nombre = m.Nombre,
                Descripcion = m.Descripcion,
                Activo = m.Activo,
                EsDefault = m.EsDefault,
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<ReportTemplateDto?> GetTemplateByIdAsync(int id)
    {
        var template = await _context.ModelosReporte.FindAsync(id);
        if (template == null)
            return null;

        return new ReportTemplateDto
        {
            Id = template.Id,
            Nombre = template.Nombre,
            Descripcion = template.Descripcion,
            ContenidoMarkdown = template.ContenidoMarkdown,
            Activo = template.Activo,
            EsDefault = template.EsDefault
        };
    }

    public async Task<ReportTemplateDto?> GetDefaultTemplateAsync()
    {
        var template = await _context.ModelosReporte
            .FirstOrDefaultAsync(m => m.EsDefault && m.Activo);

        if (template == null)
            return null;

        return new ReportTemplateDto
        {
            Id = template.Id,
            Nombre = template.Nombre,
            Descripcion = template.Descripcion,
            ContenidoMarkdown = template.ContenidoMarkdown,
            Activo = template.Activo,
            EsDefault = template.EsDefault
        };
    }

    public async Task<ReportTemplateDto> CreateTemplateAsync(CreateTemplateDto dto, Guid userId)
    {
        // If this is set as default, clear other defaults
        if (dto.EsDefault)
        {
            await ClearDefaultTemplatesAsync();
        }

        var template = new ModeloReporte
        {
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion,
            ContenidoMarkdown = dto.ContenidoMarkdown,
            EsDefault = dto.EsDefault,
            UsuarioId = userId
        };

        _context.ModelosReporte.Add(template);
        await _context.SaveChangesAsync();

        return (await GetTemplateByIdAsync(template.Id))!;
    }

    public async Task<ReportTemplateDto?> UpdateTemplateAsync(int id, UpdateTemplateDto dto, Guid userId)
    {
        var template = await _context.ModelosReporte.FindAsync(id);
        if (template == null)
            return null;

        // If this is set as default, clear other defaults
        if (dto.EsDefault && !template.EsDefault)
        {
            await ClearDefaultTemplatesAsync();
        }

        template.Nombre = dto.Nombre;
        template.Descripcion = dto.Descripcion;
        template.ContenidoMarkdown = dto.ContenidoMarkdown;
        template.Activo = dto.Activo;
        template.EsDefault = dto.EsDefault;
        template.UpdatedAt = DateTime.UtcNow;
        template.UsuarioId = userId;

        await _context.SaveChangesAsync();

        return await GetTemplateByIdAsync(id);
    }

    public async Task<bool> DeleteTemplateAsync(int id)
    {
        var template = await _context.ModelosReporte.FindAsync(id);
        if (template == null)
            return false;

        // Check if template is in use
        var inUse = await _context.ReportesAutomatizados.AnyAsync(r => r.ModeloReporteId == id);
        if (inUse)
        {
            // Soft delete - just deactivate
            template.Activo = false;
            template.EsDefault = false;
        }
        else
        {
            _context.ModelosReporte.Remove(template);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetDefaultTemplateAsync(int id)
    {
        var template = await _context.ModelosReporte.FindAsync(id);
        if (template == null || !template.Activo)
            return false;

        await ClearDefaultTemplatesAsync();

        template.EsDefault = true;
        template.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    private async Task ClearDefaultTemplatesAsync()
    {
        var defaults = await _context.ModelosReporte
            .Where(m => m.EsDefault)
            .ToListAsync();

        foreach (var t in defaults)
        {
            t.EsDefault = false;
        }
    }

    #endregion

    #region Statistics

    public async Task<ReportStatisticsDto> CalculateStatisticsAsync(
        DateTime fechaInicio,
        DateTime fechaFin,
        CancellationToken cancellationToken = default)
    {
        // Ensure dates are UTC for PostgreSQL compatibility
        var fechaInicioUtc = DateTime.SpecifyKind(fechaInicio, DateTimeKind.Utc);
        var fechaFinUtc = DateTime.SpecifyKind(fechaFin, DateTimeKind.Utc);

        var reports = await _context.ReportesIncidencia
            .Include(r => r.Zona)
            .Where(r => r.CreatedAt >= fechaInicioUtc && r.CreatedAt < fechaFinUtc)
            .ToListAsync(cancellationToken);

        var stats = new ReportStatisticsDto
        {
            TotalReportes = reports.Count,
            ReportesEntregados = reports.Count(r => r.Estado == 1),
            ReportesBorrador = reports.Count(r => r.Estado == 0)
        };

        // Demographics
        stats.PorSexo = reports
            .GroupBy(r => r.Sexo)
            .ToDictionary(g => g.Key, g => g.Count());

        stats.PorRangoEdad = reports
            .GroupBy(r => GetAgeRange(r.Edad))
            .ToDictionary(g => g.Key, g => g.Count());

        stats.TotalLgbtttiq = reports.Count(r => r.LgbtttiqPlus);
        stats.TotalMigrantes = reports.Count(r => r.Migrante);
        stats.TotalSituacionCalle = reports.Count(r => r.SituacionCalle);
        stats.TotalDiscapacidad = reports.Count(r => r.Discapacidad);

        // Crime types
        stats.PorDelito = reports
            .GroupBy(r => r.Delito)
            .OrderByDescending(g => g.Count())
            .ToDictionary(g => g.Key, g => g.Count());

        stats.DelitoMasFrecuente = stats.PorDelito.FirstOrDefault().Key;

        // Geographic
        stats.PorZona = reports
            .Where(r => r.Zona != null)
            .GroupBy(r => r.Zona!.Nombre)
            .OrderByDescending(g => g.Count())
            .ToDictionary(g => g.Key, g => g.Count());

        stats.ZonaMasActiva = stats.PorZona.FirstOrDefault().Key;

        // Attention types
        stats.PorTipoAtencion = reports
            .GroupBy(r => r.TipoDeAtencion)
            .ToDictionary(g => g.Key, g => g.Count());

        // Action types
        stats.PorTipoAccion = reports
            .GroupBy(r => r.TipoDeAccion switch
            {
                1 => "ATOS",
                2 => "Capacitación",
                3 => "Prevención",
                _ => "Otro"
            })
            .ToDictionary(g => g.Key, g => g.Count());

        // Transfers
        stats.ConTraslado = reports.Count(r => r.Traslados == 1);
        stats.SinTraslado = reports.Count(r => r.Traslados == 0);
        stats.TrasladoNoAplica = reports.Count(r => r.Traslados == 2);

        return stats;
    }

    private static string GetAgeRange(int age)
    {
        return age switch
        {
            < 18 => "Menor de 18",
            < 25 => "18-24",
            < 35 => "25-34",
            < 45 => "35-44",
            < 55 => "45-54",
            < 65 => "55-64",
            _ => "65+"
        };
    }

    #endregion

    #region Configuration

    public Task<AutomatedReportConfigDto> GetConfigurationAsync()
    {
        var config = new AutomatedReportConfigDto
        {
            GenerationTime = TimeSpan.TryParse(_configuration["AutomatedReports:GenerationTime"], out var time)
                ? time
                : new TimeSpan(6, 0, 0),
            Recipients = _configuration.GetSection("AutomatedReports:Recipients").Get<List<string>>() ?? new(),
            Enabled = bool.TryParse(_configuration["AutomatedReports:Enabled"], out var enabled) && enabled,
            DefaultTemplateId = int.TryParse(_configuration["AutomatedReports:DefaultTemplateId"], out var templateId)
                ? templateId
                : null
        };

        return Task.FromResult(config);
    }

    public Task UpdateConfigurationAsync(AutomatedReportConfigDto config)
    {
        // Configuration updates would typically be persisted to a settings table
        // For now, this is managed via appsettings.json
        _logger.LogWarning("Configuration update requested but persistence not implemented. Update appsettings.json manually.");
        return Task.CompletedTask;
    }

    private List<string> GetConfiguredRecipients()
    {
        return _configuration.GetSection("AutomatedReports:Recipients").Get<List<string>>() ?? new();
    }

    #endregion

    #region Private Helpers

    private static int ExtractTotalFromStats(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("totalReportes", out var prop)
                ? prop.GetInt32()
                : 0;
        }
        catch
        {
            return 0;
        }
    }

    private static ReportStatisticsDto DeserializeStatistics(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<ReportStatisticsDto>(json, JsonOptions) ?? new();
        }
        catch
        {
            return new ReportStatisticsDto();
        }
    }

    private string BuildMarkdownContent(
        string? templateMarkdown,
        ReportStatisticsDto stats,
        string narrative,
        DateTime fechaInicio,
        DateTime fechaFin)
    {
        if (!string.IsNullOrEmpty(templateMarkdown))
        {
            // Replace placeholders in template
            return templateMarkdown
                .Replace("{{fecha_inicio}}", fechaInicio.ToString("dd/MM/yyyy"))
                .Replace("{{fecha_fin}}", fechaFin.ToString("dd/MM/yyyy"))
                .Replace("{{total_reportes}}", stats.TotalReportes.ToString())
                .Replace("{{estadisticas}}", JsonSerializer.Serialize(stats, JsonOptions))
                .Replace("{{narrativa_ia}}", narrative)
                .Replace("{{tabla_delitos}}", BuildCrimeTable(stats.PorDelito))
                .Replace("{{tabla_zonas}}", BuildZoneTable(stats.PorZona));
        }

        // Default template
        var sb = new StringBuilder();
        sb.AppendLine($"# Reporte de Incidencias de Género");
        sb.AppendLine($"## Período: {fechaInicio:dd/MM/yyyy} - {fechaFin:dd/MM/yyyy}");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## Resumen Ejecutivo");
        sb.AppendLine();
        sb.AppendLine(narrative);
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## Estadísticas Generales");
        sb.AppendLine();
        sb.AppendLine($"| Métrica | Valor |");
        sb.AppendLine($"|---------|-------|");
        sb.AppendLine($"| Total de Reportes | {stats.TotalReportes} |");
        sb.AppendLine($"| Reportes Entregados | {stats.ReportesEntregados} |");
        sb.AppendLine($"| Reportes en Borrador | {stats.ReportesBorrador} |");
        sb.AppendLine($"| Casos LGBTTTIQ+ | {stats.TotalLgbtttiq} |");
        sb.AppendLine($"| Casos Migrantes | {stats.TotalMigrantes} |");
        sb.AppendLine($"| Casos Situación de Calle | {stats.TotalSituacionCalle} |");
        sb.AppendLine($"| Casos con Discapacidad | {stats.TotalDiscapacidad} |");
        sb.AppendLine();
        sb.AppendLine("## Distribución por Tipo de Delito");
        sb.AppendLine();
        sb.AppendLine(BuildCrimeTable(stats.PorDelito));
        sb.AppendLine();
        sb.AppendLine("## Distribución por Zona");
        sb.AppendLine();
        sb.AppendLine(BuildZoneTable(stats.PorZona));
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine($"*Reporte generado automáticamente el {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC*");

        return sb.ToString();
    }

    private static string BuildCrimeTable(Dictionary<string, int> data)
    {
        var sb = new StringBuilder();
        sb.AppendLine("| Tipo de Delito | Cantidad |");
        sb.AppendLine("|----------------|----------|");
        foreach (var item in data.OrderByDescending(x => x.Value).Take(10))
        {
            sb.AppendLine($"| {item.Key} | {item.Value} |");
        }
        return sb.ToString();
    }

    private static string BuildZoneTable(Dictionary<string, int> data)
    {
        var sb = new StringBuilder();
        sb.AppendLine("| Zona | Cantidad |");
        sb.AppendLine("|------|----------|");
        foreach (var item in data.OrderByDescending(x => x.Value))
        {
            sb.AppendLine($"| {item.Key} | {item.Value} |");
        }
        return sb.ToString();
    }

    private async Task<string?> GenerateWordDocumentAsync(int reportId, string markdown, CancellationToken cancellationToken = default)
    {
        // Use Pandoc to convert Markdown to Word
        // Pandoc must be installed on the system (see Docker configuration)
        var outputDir = Path.Combine(
            _configuration["AutomatedReports:OutputPath"] ?? Path.GetTempPath(),
            "ceiba-reports");

        Directory.CreateDirectory(outputDir);

        var outputPath = Path.Combine(outputDir, $"reporte_{reportId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.docx");
        var inputPath = Path.Combine(outputDir, $"temp_{reportId}.md");

        try
        {
            // Write markdown to temp file
            await File.WriteAllTextAsync(inputPath, markdown, cancellationToken);

            // Run Pandoc
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "pandoc",
                    Arguments = $"\"{inputPath}\" -o \"{outputPath}\" --from=markdown --to=docx",
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                _logger.LogWarning("Pandoc error: {Error}", error);
                return null;
            }

            return outputPath;
        }
        finally
        {
            // Clean up temp file
            if (File.Exists(inputPath))
            {
                try { File.Delete(inputPath); } catch { }
            }
        }
    }

    private static string WrapInHtmlTemplate(string htmlContent)
    {
        return $@"
<!DOCTYPE html>
<html lang=""es"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Reporte de Incidencias</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 800px; margin: 0 auto; padding: 20px; }}
        h1 {{ color: #2c3e50; border-bottom: 2px solid #3498db; padding-bottom: 10px; }}
        h2 {{ color: #34495e; margin-top: 30px; }}
        table {{ border-collapse: collapse; width: 100%; margin: 20px 0; }}
        th, td {{ border: 1px solid #ddd; padding: 12px; text-align: left; }}
        th {{ background-color: #3498db; color: white; }}
        tr:nth-child(even) {{ background-color: #f9f9f9; }}
        hr {{ border: none; border-top: 1px solid #eee; margin: 30px 0; }}
    </style>
</head>
<body>
{htmlContent}
</body>
</html>";
    }

    #endregion
}
