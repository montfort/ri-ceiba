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
        // Validate hierarchy (Zona → Región → Sector → Cuadrante)
        var isValidHierarchy = await _catalogService.ValidateHierarchyAsync(
            createDto.ZonaId,
            createDto.RegionId,
            createDto.SectorId,
            createDto.CuadranteId
        );

        if (!isValidHierarchy)
        {
            throw new ValidationException(
                "La jerarquía geográfica no es válida. La región debe pertenecer a la zona, " +
                "el sector a la región y el cuadrante al sector seleccionado."
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
            RegionId = createDto.RegionId,
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
        var report = await _reportRepository.GetByIdAsync(reportId)
            ?? throw new NotFoundException($"Reporte con ID {reportId} no encontrado.");

        // Authorization check
        ValidateUpdateAuthorization(report, usuarioId, isRevisor);

        // Apply field updates
        ApplyBasicFieldUpdates(report, updateDto);

        // Handle geographic hierarchy updates
        await ApplyGeographicUpdatesAsync(report, updateDto);

        // Apply remaining field updates
        ApplyAdditionalFieldUpdates(report, updateDto);

        report.UpdatedAt = DateTime.UtcNow;

        // Validate entity
        var validationResult = report.Validate();
        if (!validationResult.IsValid)
        {
            throw new ValidationException(string.Join(", ", validationResult.Errors));
        }

        // Save changes
        await _reportRepository.UpdateAsync(report);

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

    private static void ValidateUpdateAuthorization(ReporteIncidencia report, Guid usuarioId, bool isRevisor)
    {
        if (!isRevisor && !report.CanBeEditedByCreador(usuarioId))
        {
            throw new ForbiddenException(
                "No tiene permisos para editar este reporte. " +
                "Solo puede editar sus propios reportes mientras estén en estado Borrador."
            );
        }
    }

    private static void ApplyBasicFieldUpdates(ReporteIncidencia report, UpdateReportDto updateDto)
    {
        if (updateDto.DatetimeHechos.HasValue)
            report.DatetimeHechos = updateDto.DatetimeHechos.Value.ToUniversalTime();

        if (updateDto.Sexo != null)
            report.Sexo = updateDto.Sexo;

        if (updateDto.Edad.HasValue)
        {
            ValidateAge(updateDto.Edad.Value);
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
    }

    private static void ValidateAge(int age)
    {
        if (age < 1 || age > 149)
            throw new ValidationException("La edad debe estar entre 1 y 149.");
    }

    private async Task ApplyGeographicUpdatesAsync(ReporteIncidencia report, UpdateReportDto updateDto)
    {
        if (!updateDto.ZonaId.HasValue && !updateDto.RegionId.HasValue &&
            !updateDto.SectorId.HasValue && !updateDto.CuadranteId.HasValue)
            return;

        var newZonaId = updateDto.ZonaId ?? report.ZonaId;
        var newRegionId = updateDto.RegionId ?? report.RegionId;
        var newSectorId = updateDto.SectorId ?? report.SectorId;
        var newCuadranteId = updateDto.CuadranteId ?? report.CuadranteId;

        var isValidHierarchy = await _catalogService.ValidateHierarchyAsync(
            newZonaId, newRegionId, newSectorId, newCuadranteId);
        if (!isValidHierarchy)
        {
            throw new ValidationException("La jerarquía geográfica no es válida.");
        }

        report.ZonaId = newZonaId;
        report.RegionId = newRegionId;
        report.SectorId = newSectorId;
        report.CuadranteId = newCuadranteId;
    }

    private static void ApplyAdditionalFieldUpdates(ReporteIncidencia report, UpdateReportDto updateDto)
    {
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

    public async Task DeleteReportAsync(int reportId, Guid usuarioId)
    {
        // Get report
        var report = await _reportRepository.GetByIdAsync(reportId);
        if (report == null)
        {
            throw new NotFoundException($"Reporte con ID {reportId} no encontrado.");
        }

        // Authorization check: only creator can delete their own reports
        if (report.UsuarioId != usuarioId)
        {
            throw new ForbiddenException("No tiene permisos para eliminar este reporte.");
        }

        // State check: only Borrador reports can be deleted
        if (report.Estado != 0)
        {
            throw new BadRequestException("Solo se pueden eliminar reportes en estado Borrador.");
        }

        // Delete report
        await _reportRepository.DeleteAsync(reportId);

        // Audit log
        await _auditService.LogAsync(
            "REPORT_DELETE",
            reportId,
            "REPORTE_INCIDENCIA",
            System.Text.Json.JsonSerializer.Serialize(new { reportId, usuarioId, estado = "Borrador" }),
            null
        );
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
            Region = new CatalogItemDto
            {
                Id = report.Region.Id,
                Nombre = report.Region.Nombre
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

    private static List<string> GetUpdatedFields(UpdateReportDto updateDto)
    {
        // Using a dictionary-based approach to reduce cognitive complexity
        var fieldChecks = new (bool hasValue, string fieldName)[]
        {
            (updateDto.DatetimeHechos.HasValue, "DatetimeHechos"),
            (updateDto.Sexo != null, "Sexo"),
            (updateDto.Edad.HasValue, "Edad"),
            (updateDto.LgbtttiqPlus.HasValue, "LgbtttiqPlus"),
            (updateDto.SituacionCalle.HasValue, "SituacionCalle"),
            (updateDto.Migrante.HasValue, "Migrante"),
            (updateDto.Discapacidad.HasValue, "Discapacidad"),
            (updateDto.Delito != null, "Delito"),
            (updateDto.ZonaId.HasValue, "ZonaId"),
            (updateDto.RegionId.HasValue, "RegionId"),
            (updateDto.SectorId.HasValue, "SectorId"),
            (updateDto.CuadranteId.HasValue, "CuadranteId"),
            (updateDto.TurnoCeiba.HasValue, "TurnoCeiba"),
            (updateDto.TipoDeAtencion != null, "TipoDeAtencion"),
            (updateDto.TipoDeAccion.HasValue, "TipoDeAccion"),
            (updateDto.HechosReportados != null, "HechosReportados"),
            (updateDto.AccionesRealizadas != null, "AccionesRealizadas"),
            (updateDto.Traslados.HasValue, "Traslados"),
            (updateDto.Observaciones != null, "Observaciones")
        };

        return fieldChecks.Where(f => f.hasValue).Select(f => f.fieldName).ToList();
    }

    #endregion
}
