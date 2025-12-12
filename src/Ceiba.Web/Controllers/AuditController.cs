using Ceiba.Core.Interfaces;
using Ceiba.Shared.DTOs;
using Ceiba.Web.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Ceiba.Web.Controllers;

/// <summary>
/// API controller for audit log viewing.
/// US3: FR-031 to FR-036 - ADMIN role only
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AuthorizeBeforeModelBinding("ADMIN")]
[AutoValidateAntiforgeryToken]
public class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;
    private readonly ILogger<AuditController> _logger;

    public AuditController(
        IAuditService auditService,
        ILogger<AuditController> logger)
    {
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Gets paginated audit logs with optional filtering.
    /// FR-034: View and search audit logs
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(AuditListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuditListResponse>> GetAuditLogs([FromQuery] AuditFilterDto filter)
    {
        try
        {
            var logs = await _auditService.QueryAsync(
                usuarioId: filter.UsuarioId,
                codigo: filter.Codigo,
                fechaInicio: filter.FechaDesde,
                fechaFin: filter.FechaHasta,
                skip: (filter.Page - 1) * filter.PageSize,
                take: filter.PageSize
            );

            // Map to enriched DTOs
            var items = logs.Select(log => new AuditLogEntryDto
            {
                Id = log.Id,
                Codigo = log.Codigo,
                CodigoDescripcion = AuditCodes.GetDescription(log.Codigo),
                IdRelacionado = log.IdRelacionado,
                TablaRelacionada = log.TablaRelacionada,
                CreatedAt = log.CreatedAt,
                UsuarioId = log.UsuarioId,
                UsuarioEmail = log.UsuarioNombre,
                Ip = log.Ip,
                Detalles = log.Detalles
            }).ToList();

            return Ok(new AuditListResponse
            {
                Items = items,
                TotalCount = items.Count, // Note: Would need separate count query for accurate total
                Page = filter.Page,
                PageSize = filter.PageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit logs");
            return StatusCode(500, new { message = "Error al obtener registros de auditoría" });
        }
    }

    /// <summary>
    /// Gets available audit action codes.
    /// </summary>
    [HttpGet("codes")]
    [ProducesResponseType(typeof(object[]), StatusCodes.Status200OK)]
    public ActionResult<object[]> GetAuditCodes()
    {
        var codes = new[]
        {
            // Authentication
            new { code = AuditCodes.AUTH_LOGIN, description = AuditCodes.GetDescription(AuditCodes.AUTH_LOGIN), category = "Autenticación" },
            new { code = AuditCodes.AUTH_LOGOUT, description = AuditCodes.GetDescription(AuditCodes.AUTH_LOGOUT), category = "Autenticación" },
            new { code = AuditCodes.AUTH_FAILED, description = AuditCodes.GetDescription(AuditCodes.AUTH_FAILED), category = "Autenticación" },
            new { code = AuditCodes.AUTH_LOCKED, description = AuditCodes.GetDescription(AuditCodes.AUTH_LOCKED), category = "Autenticación" },

            // User Management
            new { code = AuditCodes.USER_CREATE, description = AuditCodes.GetDescription(AuditCodes.USER_CREATE), category = "Usuarios" },
            new { code = AuditCodes.USER_UPDATE, description = AuditCodes.GetDescription(AuditCodes.USER_UPDATE), category = "Usuarios" },
            new { code = AuditCodes.USER_SUSPEND, description = AuditCodes.GetDescription(AuditCodes.USER_SUSPEND), category = "Usuarios" },
            new { code = AuditCodes.USER_ACTIVATE, description = AuditCodes.GetDescription(AuditCodes.USER_ACTIVATE), category = "Usuarios" },
            new { code = AuditCodes.USER_DELETE, description = AuditCodes.GetDescription(AuditCodes.USER_DELETE), category = "Usuarios" },

            // Reports
            new { code = AuditCodes.REPORT_CREATE, description = AuditCodes.GetDescription(AuditCodes.REPORT_CREATE), category = "Reportes" },
            new { code = AuditCodes.REPORT_UPDATE, description = AuditCodes.GetDescription(AuditCodes.REPORT_UPDATE), category = "Reportes" },
            new { code = AuditCodes.REPORT_SUBMIT, description = AuditCodes.GetDescription(AuditCodes.REPORT_SUBMIT), category = "Reportes" },
            new { code = AuditCodes.REPORT_EXPORT, description = AuditCodes.GetDescription(AuditCodes.REPORT_EXPORT), category = "Reportes" },
            new { code = AuditCodes.REPORT_EXPORT_BULK, description = AuditCodes.GetDescription(AuditCodes.REPORT_EXPORT_BULK), category = "Reportes" },

            // Catalogs
            new { code = AuditCodes.CATALOG_CREATE, description = AuditCodes.GetDescription(AuditCodes.CATALOG_CREATE), category = "Catálogos" },
            new { code = AuditCodes.CATALOG_UPDATE, description = AuditCodes.GetDescription(AuditCodes.CATALOG_UPDATE), category = "Catálogos" },
            new { code = AuditCodes.CATALOG_DELETE, description = AuditCodes.GetDescription(AuditCodes.CATALOG_DELETE), category = "Catálogos" },

            // Security
            new { code = AuditCodes.ACCESS_DENIED, description = AuditCodes.GetDescription(AuditCodes.ACCESS_DENIED), category = "Seguridad" },
            new { code = AuditCodes.SESSION_EXPIRED, description = AuditCodes.GetDescription(AuditCodes.SESSION_EXPIRED), category = "Seguridad" }
        };

        return Ok(codes);
    }

    /// <summary>
    /// Gets related table names for filtering.
    /// </summary>
    [HttpGet("tables")]
    [ProducesResponseType(typeof(string[]), StatusCodes.Status200OK)]
    public ActionResult<string[]> GetRelatedTables()
    {
        return Ok(new[]
        {
            "Usuario",
            "ReporteIncidencia",
            "Zona",
            "Sector",
            "Cuadrante",
            "CatalogoSugerencia"
        });
    }
}
