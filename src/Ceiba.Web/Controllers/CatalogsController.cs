using Ceiba.Core.Interfaces;
using Ceiba.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ceiba.Web.Controllers;

/// <summary>
/// API controller for catalog operations (Zona, Región, Sector, Cuadrante, Suggestions).
/// US1: T040
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[AutoValidateAntiforgeryToken]
public class CatalogsController : ControllerBase
{
    private readonly ICatalogService _catalogService;
    private readonly ILogger<CatalogsController> _logger;

    public CatalogsController(
        ICatalogService catalogService,
        ILogger<CatalogsController> logger)
    {
        _catalogService = catalogService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all active zones.
    /// </summary>
    [HttpGet("zonas")]
    public async Task<ActionResult<List<CatalogItemDto>>> GetZonas()
    {
        try
        {
            var zonas = await _catalogService.GetZonasAsync();
            return Ok(zonas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting zonas");
            return StatusCode(500, new { message = "Error al obtener las zonas" });
        }
    }

    /// <summary>
    /// Gets regions for a specific zone.
    /// </summary>
    [HttpGet("regiones")]
    public async Task<ActionResult<List<CatalogItemDto>>> GetRegiones([FromQuery] int zonaId)
    {
        try
        {
            if (zonaId <= 0)
            {
                return BadRequest(new { message = "El ID de zona es requerido" });
            }

            var regiones = await _catalogService.GetRegionesByZonaAsync(zonaId);
            return Ok(regiones);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting regiones for zona {ZonaId}", zonaId);
            return StatusCode(500, new { message = "Error al obtener las regiones" });
        }
    }

    /// <summary>
    /// Gets sectors for a specific region.
    /// </summary>
    [HttpGet("sectores")]
    public async Task<ActionResult<List<CatalogItemDto>>> GetSectores([FromQuery] int regionId)
    {
        try
        {
            if (regionId <= 0)
            {
                return BadRequest(new { message = "El ID de región es requerido" });
            }

            var sectores = await _catalogService.GetSectoresByRegionAsync(regionId);
            return Ok(sectores);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sectores for region {RegionId}", regionId);
            return StatusCode(500, new { message = "Error al obtener los sectores" });
        }
    }

    /// <summary>
    /// Gets quadrants for a specific sector.
    /// </summary>
    [HttpGet("cuadrantes")]
    public async Task<ActionResult<List<CatalogItemDto>>> GetCuadrantes([FromQuery] int sectorId)
    {
        try
        {
            if (sectorId <= 0)
            {
                return BadRequest(new { message = "El ID de sector es requerido" });
            }

            var cuadrantes = await _catalogService.GetCuadrantesBySectorAsync(sectorId);
            return Ok(cuadrantes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cuadrantes for sector {SectorId}", sectorId);
            return StatusCode(500, new { message = "Error al obtener los cuadrantes" });
        }
    }

    /// <summary>
    /// Gets suggestions for a specific field.
    /// </summary>
    [HttpGet("sugerencias")]
    public async Task<ActionResult<List<string>>> GetSuggestions([FromQuery] string campo)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(campo))
            {
                return BadRequest(new { message = "El campo es requerido" });
            }

            var allowedFields = new[] { "sexo", "delito", "tipo_de_atencion" };
            if (!allowedFields.Contains(campo.ToLower()))
            {
                return BadRequest(new { message = $"Campo no válido. Valores permitidos: {string.Join(", ", allowedFields)}" });
            }

            var suggestions = await _catalogService.GetSuggestionsAsync(campo);
            return Ok(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting suggestions for campo {Campo}", campo);
            return StatusCode(500, new { message = "Error al obtener las sugerencias" });
        }
    }

    /// <summary>
    /// Validates the geographic hierarchy (Zona → Región → Sector → Cuadrante).
    /// </summary>
    [HttpGet("validate-hierarchy")]
    public async Task<ActionResult<bool>> ValidateHierarchy(
        [FromQuery] int zonaId,
        [FromQuery] int regionId,
        [FromQuery] int sectorId,
        [FromQuery] int cuadranteId)
    {
        try
        {
            if (zonaId <= 0 || regionId <= 0 || sectorId <= 0 || cuadranteId <= 0)
            {
                return BadRequest(new { message = "Todos los IDs son requeridos" });
            }

            var isValid = await _catalogService.ValidateHierarchyAsync(zonaId, regionId, sectorId, cuadranteId);
            return Ok(new { isValid });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating hierarchy");
            return StatusCode(500, new { message = "Error al validar la jerarquía" });
        }
    }
}
