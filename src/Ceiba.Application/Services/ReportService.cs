using Ceiba.Core.Entities;
using Ceiba.Core.Exceptions;
using Ceiba.Core.Interfaces;
using Ceiba.Shared.DTOs;

namespace Ceiba.Application.Services;

/// <summary>
/// Service implementation for report management operations.
/// US1: T035
/// </summary>
public class ReportService : IReportService
{
    private readonly IReportRepository _reportRepository;
    private readonly IAuditService _auditService;
    private readonly ICatalogService _catalogService;
    private readonly IUserManagementService _userService;

    public ReportService(
        IReportRepository reportRepository,
        IAuditService auditService,
        ICatalogService catalogService,
        IUserManagementService userService)
    {
        _reportRepository = reportRepository;
        _auditService = auditService;
        _catalogService = catalogService;
        _userService = userService;
    }

    public async Task<ReportDto> CreateReportAsync(CreateReportDto createDto, Guid usuarioId)
    {
        // Validate hierarchy
        var isValidHierarchy = await _catalogService.ValidateHierarchyAsync(
            createDto.ZonaId,
            createDto.SectorId,
            createDto.CuadranteId
        );

        if (!isValidHierarchy)
        {
            throw new ValidationException(
                "La jerarquía geográfica no es válida. El sector debe pertenecer a la zona seleccionada " +
                "y el cuadrante debe pertenecer al sector seleccionado."
            );
        }

        // Validate edad range
        if (createDto.Edad < 1 || createDto.Edad > 149)
        {
            throw new ValidationException("La edad debe estar entre 1 y 149.");
        }

        // Create entity
        var report = new ReporteIncidencia
        {
            TipoReporte = createDto.TipoReporte,
            Estado = 0, // Borrador
            UsuarioId = usuarioId,
            CreatedAt = DateTime.UtcNow,
            DatetimeHechos = createDto.DatetimeHechos.ToUniversalTime(),
            Sexo = createDto.Sexo,
            Edad = createDto.Edad,
            LgbtttiqPlus = createDto.LgbtttiqPlus,
            SituacionCalle = createDto.SituacionCalle,
            Migrante = createDto.Migrante,
            Discapacidad = createDto.Discapacidad,
            Delito = createDto.Delito,
            ZonaId = createDto.ZonaId,
            SectorId = createDto.SectorId,
            CuadranteId = createDto.CuadranteId,
            TurnoCeiba = createDto.TurnoCeiba,
            TipoDeAtencion = createDto.TipoDeAtencion,
            TipoDeAccion = (short)createDto.TipoDeAccion,
            HechosReportados = createDto.HechosReportados,
            AccionesRealizadas = createDto.AccionesRealizadas,
            Traslados = (short)createDto.Traslados,
            Observaciones = createDto.Observaciones
        };

        // Validate entity
        var validationResult = report.Validate();
        if (!validationResult.IsValid)
        {
            throw new ValidationException(string.Join(", ", validationResult.Errors));
        }

        // Save to database
        var savedReport = await _reportRepository.AddAsync(report);

        // Audit log
        await _auditService.LogAsync(
            "REPORT_CREATE",
            savedReport.Id,
            "REPORTE_INCIDENCIA",
            System.Text.Json.JsonSerializer.Serialize(new { reportId = savedReport.Id, estado = "Borrador", usuarioId }),
            null
        );

        // Reload with relations for DTO mapping
        var reportWithRelations = await _reportRepository.GetByIdWithRelationsAsync(savedReport.Id)
            ?? throw new NotFoundException($"Reporte con ID {savedReport.Id} no encontrado.");

        // Return DTO
        return await MapToDto(reportWithRelations);
    }

    public async Task<ReportDto> UpdateReportAsync(
        int reportId,
        UpdateReportDto updateDto,
        Guid usuarioId,
        bool isRevisor = false)
    {
        // Get existing report
        var report = await _reportRepository.GetByIdAsync(reportId);
        if (report == null)
        {
            throw new NotFoundException($"Reporte con ID {reportId} no encontrado.");
        }

        // Authorization check
        if (!isRevisor && !report.CanBeEditedByCreador(usuarioId))
        {
            throw new ForbiddenException(
                "No tiene permisos para editar este reporte. " +
                "Solo puede editar sus propios reportes mientras estén en estado Borrador."
            );
        }

        // Apply updates (only non-null fields)
        if (updateDto.DatetimeHechos.HasValue)
            report.DatetimeHechos = updateDto.DatetimeHechos.Value.ToUniversalTime();

        if (updateDto.Sexo != null)
            report.Sexo = updateDto.Sexo;

        if (updateDto.Edad.HasValue)
        {
            if (updateDto.Edad.Value < 1 || updateDto.Edad.Value > 149)
                throw new ValidationException("La edad debe estar entre 1 y 149.");
            report.Edad = updateDto.Edad.Value;
        }

        if (updateDto.LgbtttiqPlus.HasValue)
            report.LgbtttiqPlus = updateDto.LgbtttiqPlus.Value;

        if (updateDto.SituacionCalle.HasValue)
            report.SituacionCalle = updateDto.SituacionCalle.Value;

        if (updateDto.Migrante.HasValue)
            report.Migrante = updateDto.Migrante.Value;

        if (updateDto.Discapacidad.HasValue)
            report.Discapacidad = updateDto.Discapacidad.Value;

        if (updateDto.Delito != null)
            report.Delito = updateDto.Delito;

        // Validate and update geographic hierarchy if any field changed
        if (updateDto.ZonaId.HasValue || updateDto.SectorId.HasValue || updateDto.CuadranteId.HasValue)
        {
            var newZonaId = updateDto.ZonaId ?? report.ZonaId;
            var newSectorId = updateDto.SectorId ?? report.SectorId;
            var newCuadranteId = updateDto.CuadranteId ?? report.CuadranteId;

            var isValidHierarchy = await _catalogService.ValidateHierarchyAsync(
                newZonaId,
                newSectorId,
                newCuadranteId
            );

            if (!isValidHierarchy)
            {
                throw new ValidationException("La jerarquía geográfica no es válida.");
            }

            report.ZonaId = newZonaId;
            report.SectorId = newSectorId;
            report.CuadranteId = newCuadranteId;
        }

        if (updateDto.TurnoCeiba.HasValue)
            report.TurnoCeiba = updateDto.TurnoCeiba.Value;

        if (updateDto.TipoDeAtencion != null)
            report.TipoDeAtencion = updateDto.TipoDeAtencion;

        if (updateDto.TipoDeAccion.HasValue)
            report.TipoDeAccion = (short)updateDto.TipoDeAccion.Value;

        if (updateDto.HechosReportados != null)
            report.HechosReportados = updateDto.HechosReportados;

        if (updateDto.AccionesRealizadas != null)
            report.AccionesRealizadas = updateDto.AccionesRealizadas;

        if (updateDto.Traslados.HasValue)
            report.Traslados = (short)updateDto.Traslados.Value;

        if (updateDto.Observaciones != null)
            report.Observaciones = updateDto.Observaciones;

        report.UpdatedAt = DateTime.UtcNow;

        // Validate entity
        var validationResult = report.Validate();
        if (!validationResult.IsValid)
        {
            throw new ValidationException(string.Join(", ", validationResult.Errors));
        }

        // Save changes
        var updatedReport = await _reportRepository.UpdateAsync(report);

        // Audit log
        await _auditService.LogAsync(
            "REPORT_UPDATE",
            reportId,
            "REPORTE_INCIDENCIA",
            System.Text.Json.JsonSerializer.Serialize(new { reportId, usuarioId, updatedFields = GetUpdatedFields(updateDto) }),
            null
        );

        // Reload with relations for DTO mapping
        var reportWithRelations = await _reportRepository.GetByIdWithRelationsAsync(reportId)
            ?? throw new NotFoundException($"Reporte con ID {reportId} no encontrado.");

        return await MapToDto(reportWithRelations);
    }

    public async Task<ReportDto> SubmitReportAsync(int reportId, Guid usuarioId)
    {
        // Get report
        var report = await _reportRepository.GetByIdAsync(reportId);
        if (report == null)
        {
            throw new NotFoundException($"Reporte con ID {reportId} no encontrado.");
        }

        // Authorization check
        if (!report.CanBeSubmittedByCreador(usuarioId))
        {
            throw new ForbiddenException(
                "No tiene permisos para entregar este reporte. " +
                "Solo puede entregar sus propios reportes que estén en estado Borrador."
            );
        }

        // Check if already submitted
        if (report.Estado == 1)
        {
            throw new BadRequestException("El reporte ya fue entregado previamente.");
        }

        // Submit (changes estado from 0 to 1)
        report.Submit();

        // Save changes
        var submittedReport = await _reportRepository.UpdateAsync(report);

        // Audit log
        await _auditService.LogAsync(
            "REPORT_SUBMIT",
            reportId,
            "REPORTE_INCIDENCIA",
            System.Text.Json.JsonSerializer.Serialize(new { reportId, usuarioId, estadoAnterior = 0, estadoNuevo = 1 }),
            null
        );

        // Reload with relations for DTO mapping
        var reportWithRelations = await _reportRepository.GetByIdWithRelationsAsync(reportId)
            ?? throw new NotFoundException($"Reporte con ID {reportId} no encontrado.");

        return await MapToDto(reportWithRelations);
    }

    public async Task<ReportDto> GetReportByIdAsync(int reportId, Guid usuarioId, bool isRevisor = false)
    {
        var report = await _reportRepository.GetByIdWithRelationsAsync(reportId);
        if (report == null)
        {
            throw new NotFoundException($"Reporte con ID {reportId} no encontrado.");
        }

        // Authorization: CREADOR can only see their own reports
        if (!isRevisor && report.UsuarioId != usuarioId)
        {
            throw new ForbiddenException("No tiene permisos para ver este reporte.");
        }

        return await MapToDto(report);
    }

    public async Task<ReportListResponse> ListReportsAsync(
        ReportFilterDto filter,
        Guid usuarioId,
        bool isRevisor = false)
    {
        // CREADOR can only see their own reports
        var filterUsuarioId = isRevisor ? filter.Estado.HasValue ? null : (Guid?)null : usuarioId;

        var (items, totalCount) = await _reportRepository.SearchAsync(
            estado: filter.Estado,
            zonaId: filter.ZonaId,
            delito: filter.Delito,
            fechaDesde: filter.FechaDesde,
            fechaHasta: filter.FechaHasta,
            usuarioId: filterUsuarioId,
            page: filter.Page,
            pageSize: Math.Min(filter.PageSize, 500), // Max 500 per page
            sortBy: filter.SortBy,
            sortDesc: filter.SortDesc
        );

        var reportDtos = new List<ReportDto>();
        foreach (var item in items)
        {
            reportDtos.Add(await MapToDto(item));
        }

        return new ReportListResponse
        {
            Items = reportDtos,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    #region Helper Methods

    private async Task<ReportDto> MapToDto(ReporteIncidencia report)
    {
        // Assumes relations (Zona, Sector, Cuadrante) are already loaded by caller

        // Get user email for display
        string? usuarioEmail = null;
        try
        {
            var user = await _userService.GetUserByIdAsync(report.UsuarioId);
            usuarioEmail = user?.Email;
        }
        catch
        {
            // If user lookup fails, leave email as null
        }

        var dto = new ReportDto
        {
            Id = report.Id,
            TipoReporte = report.TipoReporte,
            Estado = report.Estado,
            UsuarioId = report.UsuarioId,
            UsuarioEmail = usuarioEmail,
            CreatedAt = report.CreatedAt,
            UpdatedAt = report.UpdatedAt,
            DatetimeHechos = report.DatetimeHechos,
            Sexo = report.Sexo,
            Edad = report.Edad,
            LgbtttiqPlus = report.LgbtttiqPlus,
            SituacionCalle = report.SituacionCalle,
            Migrante = report.Migrante,
            Discapacidad = report.Discapacidad,
            Delito = report.Delito,
            Zona = new CatalogItemDto
            {
                Id = report.Zona.Id,
                Nombre = report.Zona.Nombre
            },
            Sector = new CatalogItemDto
            {
                Id = report.Sector.Id,
                Nombre = report.Sector.Nombre
            },
            Cuadrante = new CatalogItemDto
            {
                Id = report.Cuadrante.Id,
                Nombre = report.Cuadrante.Nombre
            },
            TurnoCeiba = report.TurnoCeiba,
            TipoDeAtencion = report.TipoDeAtencion,
            TipoDeAccion = report.TipoDeAccion,
            HechosReportados = report.HechosReportados,
            AccionesRealizadas = report.AccionesRealizadas,
            Traslados = report.Traslados,
            Observaciones = report.Observaciones
        };

        return dto;
    }

    private List<string> GetUpdatedFields(UpdateReportDto updateDto)
    {
        var fields = new List<string>();
        if (updateDto.DatetimeHechos.HasValue) fields.Add("DatetimeHechos");
        if (updateDto.Sexo != null) fields.Add("Sexo");
        if (updateDto.Edad.HasValue) fields.Add("Edad");
        if (updateDto.LgbtttiqPlus.HasValue) fields.Add("LgbtttiqPlus");
        if (updateDto.SituacionCalle.HasValue) fields.Add("SituacionCalle");
        if (updateDto.Migrante.HasValue) fields.Add("Migrante");
        if (updateDto.Discapacidad.HasValue) fields.Add("Discapacidad");
        if (updateDto.Delito != null) fields.Add("Delito");
        if (updateDto.ZonaId.HasValue) fields.Add("ZonaId");
        if (updateDto.SectorId.HasValue) fields.Add("SectorId");
        if (updateDto.CuadranteId.HasValue) fields.Add("CuadranteId");
        if (updateDto.TurnoCeiba.HasValue) fields.Add("TurnoCeiba");
        if (updateDto.TipoDeAtencion != null) fields.Add("TipoDeAtencion");
        if (updateDto.TipoDeAccion.HasValue) fields.Add("TipoDeAccion");
        if (updateDto.HechosReportados != null) fields.Add("HechosReportados");
        if (updateDto.AccionesRealizadas != null) fields.Add("AccionesRealizadas");
        if (updateDto.Traslados.HasValue) fields.Add("Traslados");
        if (updateDto.Observaciones != null) fields.Add("Observaciones");
        return fields;
    }

    #endregion
}
