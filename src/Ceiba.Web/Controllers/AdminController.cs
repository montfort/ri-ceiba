using Ceiba.Core.Interfaces;
using Ceiba.Infrastructure.Data;
using Ceiba.Shared.DTOs;
using Ceiba.Web.Filters;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ceiba.Web.Controllers;

/// <summary>
/// API controller for administration operations.
/// US3: User management and catalog configuration - ADMIN role only
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AuthorizeBeforeModelBinding("ADMIN")]
[AutoValidateAntiforgeryToken]
public class AdminController : ControllerBase
{
    private readonly IUserManagementService _userService;
    private readonly ICatalogAdminService _catalogService;
    private readonly ISeedDataService _seedDataService;
    private readonly IRegionDataLoader _regionDataLoader;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IUserManagementService userService,
        ICatalogAdminService catalogService,
        ISeedDataService seedDataService,
        IRegionDataLoader regionDataLoader,
        ILogger<AdminController> logger)
    {
        _userService = userService;
        _catalogService = catalogService;
        _seedDataService = seedDataService;
        _regionDataLoader = regionDataLoader;
        _logger = logger;
    }

    #region User Management (FR-021 to FR-026)

    /// <summary>
    /// Lists all users with optional filtering.
    /// FR-021: List all system users
    /// </summary>
    [HttpGet("users")]
    [ProducesResponseType(typeof(UserListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserListResponse>> ListUsers([FromQuery] UserFilterDto filter)
    {
        try
        {
            var result = await _userService.ListUsersAsync(filter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing users");
            return StatusCode(500, new { message = "Error al obtener usuarios" });
        }
    }

    /// <summary>
    /// Gets a user by ID.
    /// </summary>
    [HttpGet("users/{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetUser(Guid id)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound(new { message = $"Usuario con ID {id} no encontrado" });

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId}", id);
            return StatusCode(500, new { message = "Error al obtener usuario" });
        }
    }

    /// <summary>
    /// Creates a new user.
    /// FR-022: Create users with name, email, password, roles
    /// </summary>
    [HttpPost("users")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserDto createDto)
    {
        try
        {
            var adminId = GetAdminId();
            var user = await _userService.CreateUserAsync(createDto, adminId);
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, new { message = "Error al crear usuario" });
        }
    }

    /// <summary>
    /// Updates an existing user.
    /// FR-025: Assign or modify roles
    /// </summary>
    [HttpPut("users/{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> UpdateUser(Guid id, [FromBody] UpdateUserDto updateDto)
    {
        try
        {
            var adminId = GetAdminId();
            var user = await _userService.UpdateUserAsync(id, updateDto, adminId);
            return Ok(user);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", id);
            return StatusCode(500, new { message = "Error al actualizar usuario" });
        }
    }

    /// <summary>
    /// Suspends a user.
    /// FR-023: Suspend existing users
    /// </summary>
    [HttpPost("users/{id:guid}/suspend")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> SuspendUser(Guid id)
    {
        try
        {
            var adminId = GetAdminId();
            var user = await _userService.SuspendUserAsync(id, adminId);
            return Ok(user);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suspending user {UserId}", id);
            return StatusCode(500, new { message = "Error al suspender usuario" });
        }
    }

    /// <summary>
    /// Activates a suspended user.
    /// </summary>
    [HttpPost("users/{id:guid}/activate")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> ActivateUser(Guid id)
    {
        try
        {
            var adminId = GetAdminId();
            var user = await _userService.ActivateUserAsync(id, adminId);
            return Ok(user);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating user {UserId}", id);
            return StatusCode(500, new { message = "Error al activar usuario" });
        }
    }

    /// <summary>
    /// Deletes a user (soft delete).
    /// FR-024: Preserve referential integrity
    /// </summary>
    [HttpDelete("users/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        try
        {
            var adminId = GetAdminId();
            await _userService.DeleteUserAsync(id, adminId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", id);
            return StatusCode(500, new { message = "Error al eliminar usuario" });
        }
    }

    /// <summary>
    /// Gets available roles.
    /// </summary>
    [HttpGet("roles")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<string>>> GetRoles()
    {
        var roles = await _userService.GetAvailableRolesAsync();
        return Ok(roles);
    }

    #endregion

    #region Catalog Management (FR-027 to FR-030)

    /// <summary>
    /// Gets current geographic catalog statistics.
    /// </summary>
    [HttpGet("catalogs/geographic-stats")]
    [ProducesResponseType(typeof(GeographicCatalogStatsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<GeographicCatalogStatsDto>> GetGeographicCatalogStats()
    {
        try
        {
            var stats = await _regionDataLoader.GetCurrentStatsAsync();

            return Ok(new GeographicCatalogStatsDto
            {
                Message = "Estadísticas actuales de catálogos geográficos",
                ZonasCount = stats.Zonas,
                RegionesCount = stats.Regiones,
                SectoresCount = stats.Sectores,
                CuadrantesCount = stats.Cuadrantes
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting geographic catalog stats");
            return StatusCode(500, new { message = "Error al obtener estadísticas" });
        }
    }

    /// <summary>
    /// Reloads geographic catalogs (Zonas, Regiones, Sectores, Cuadrantes) from regiones.json.
    /// WARNING: This will clear existing geographic data and replace it with data from the JSON file.
    /// </summary>
    [HttpPost("catalogs/reload-geographic")]
    [ProducesResponseType(typeof(GeographicCatalogStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GeographicCatalogStatsDto>> ReloadGeographicCatalogs()
    {
        try
        {
            _logger.LogWarning("Admin {AdminId} initiated geographic catalog reload", GetAdminId());
            await _seedDataService.ReloadGeographicCatalogsAsync();

            // Get the new stats
            var stats = await _regionDataLoader.GetCurrentStatsAsync();

            return Ok(new GeographicCatalogStatsDto
            {
                Message = "Catálogos geográficos recargados exitosamente desde regiones.json",
                ZonasCount = stats.Zonas,
                RegionesCount = stats.Regiones,
                SectoresCount = stats.Sectores,
                CuadrantesCount = stats.Cuadrantes
            });
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "regiones.json not found during reload");
            return StatusCode(500, new { message = "Archivo regiones.json no encontrado" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reloading geographic catalogs");
            return StatusCode(500, new { message = "Error al recargar catálogos geográficos", error = ex.Message });
        }
    }

    // Zonas

    [HttpGet("catalogs/zonas")]
    public async Task<ActionResult<List<ZonaDto>>> GetZonas([FromQuery] bool? activo = null)
    {
        var zonas = await _catalogService.GetZonasAsync(activo);
        return Ok(zonas);
    }

    [HttpGet("catalogs/zonas/{id:int}")]
    public async Task<ActionResult<ZonaDto>> GetZona(int id)
    {
        var zona = await _catalogService.GetZonaByIdAsync(id);
        if (zona == null)
            return NotFound(new { message = $"Zona con ID {id} no encontrada" });
        return Ok(zona);
    }

    [HttpPost("catalogs/zonas")]
    public async Task<ActionResult<ZonaDto>> CreateZona([FromBody] CreateZonaDto createDto)
    {
        try
        {
            var adminId = GetAdminId();
            var zona = await _catalogService.CreateZonaAsync(createDto, adminId);
            return CreatedAtAction(nameof(GetZona), new { id = zona.Id }, zona);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating zona");
            return StatusCode(500, new { message = "Error al crear zona" });
        }
    }

    [HttpPut("catalogs/zonas/{id:int}")]
    public async Task<ActionResult<ZonaDto>> UpdateZona(int id, [FromBody] CreateZonaDto updateDto)
    {
        try
        {
            var adminId = GetAdminId();
            var zona = await _catalogService.UpdateZonaAsync(id, updateDto, adminId);
            return Ok(zona);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating zona {ZonaId}", id);
            return StatusCode(500, new { message = "Error al actualizar zona" });
        }
    }

    [HttpPost("catalogs/zonas/{id:int}/toggle")]
    public async Task<ActionResult<ZonaDto>> ToggleZona(int id)
    {
        try
        {
            var adminId = GetAdminId();
            var zona = await _catalogService.ToggleZonaActivoAsync(id, adminId);
            return Ok(zona);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling zona {ZonaId}", id);
            return StatusCode(500, new { message = "Error al cambiar estado de zona" });
        }
    }

    // Regiones

    [HttpGet("catalogs/regiones")]
    public async Task<ActionResult<List<RegionDto>>> GetRegiones([FromQuery] int? zonaId = null, [FromQuery] bool? activo = null)
    {
        var regiones = await _catalogService.GetRegionesAsync(zonaId, activo);
        return Ok(regiones);
    }

    [HttpGet("catalogs/regiones/{id:int}")]
    public async Task<ActionResult<RegionDto>> GetRegion(int id)
    {
        var region = await _catalogService.GetRegionByIdAsync(id);
        if (region == null)
            return NotFound(new { message = $"Región con ID {id} no encontrada" });
        return Ok(region);
    }

    [HttpPost("catalogs/regiones")]
    public async Task<ActionResult<RegionDto>> CreateRegion([FromBody] CreateRegionDto createDto)
    {
        try
        {
            var adminId = GetAdminId();
            var region = await _catalogService.CreateRegionAsync(createDto, adminId);
            return CreatedAtAction(nameof(GetRegion), new { id = region.Id }, region);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating region");
            return StatusCode(500, new { message = "Error al crear región" });
        }
    }

    [HttpPut("catalogs/regiones/{id:int}")]
    public async Task<ActionResult<RegionDto>> UpdateRegion(int id, [FromBody] CreateRegionDto updateDto)
    {
        try
        {
            var adminId = GetAdminId();
            var region = await _catalogService.UpdateRegionAsync(id, updateDto, adminId);
            return Ok(region);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating region {RegionId}", id);
            return StatusCode(500, new { message = "Error al actualizar región" });
        }
    }

    [HttpPost("catalogs/regiones/{id:int}/toggle")]
    public async Task<ActionResult<RegionDto>> ToggleRegion(int id)
    {
        try
        {
            var adminId = GetAdminId();
            var region = await _catalogService.ToggleRegionActivoAsync(id, adminId);
            return Ok(region);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling region {RegionId}", id);
            return StatusCode(500, new { message = "Error al cambiar estado de región" });
        }
    }

    // Sectores

    [HttpGet("catalogs/sectores")]
    public async Task<ActionResult<List<SectorDto>>> GetSectores([FromQuery] int? regionId = null, [FromQuery] bool? activo = null)
    {
        var sectores = await _catalogService.GetSectoresAsync(regionId, activo);
        return Ok(sectores);
    }

    [HttpGet("catalogs/sectores/{id:int}")]
    public async Task<ActionResult<SectorDto>> GetSector(int id)
    {
        var sector = await _catalogService.GetSectorByIdAsync(id);
        if (sector == null)
            return NotFound(new { message = $"Sector con ID {id} no encontrado" });
        return Ok(sector);
    }

    [HttpPost("catalogs/sectores")]
    public async Task<ActionResult<SectorDto>> CreateSector([FromBody] CreateSectorDto createDto)
    {
        try
        {
            var adminId = GetAdminId();
            var sector = await _catalogService.CreateSectorAsync(createDto, adminId);
            return CreatedAtAction(nameof(GetSector), new { id = sector.Id }, sector);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating sector");
            return StatusCode(500, new { message = "Error al crear sector" });
        }
    }

    [HttpPut("catalogs/sectores/{id:int}")]
    public async Task<ActionResult<SectorDto>> UpdateSector(int id, [FromBody] CreateSectorDto updateDto)
    {
        try
        {
            var adminId = GetAdminId();
            var sector = await _catalogService.UpdateSectorAsync(id, updateDto, adminId);
            return Ok(sector);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating sector {SectorId}", id);
            return StatusCode(500, new { message = "Error al actualizar sector" });
        }
    }

    [HttpPost("catalogs/sectores/{id:int}/toggle")]
    public async Task<ActionResult<SectorDto>> ToggleSector(int id)
    {
        try
        {
            var adminId = GetAdminId();
            var sector = await _catalogService.ToggleSectorActivoAsync(id, adminId);
            return Ok(sector);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling sector {SectorId}", id);
            return StatusCode(500, new { message = "Error al cambiar estado de sector" });
        }
    }

    // Cuadrantes

    [HttpGet("catalogs/cuadrantes")]
    public async Task<ActionResult<List<CuadranteDto>>> GetCuadrantes([FromQuery] int? sectorId = null, [FromQuery] bool? activo = null)
    {
        var cuadrantes = await _catalogService.GetCuadrantesAsync(sectorId, activo);
        return Ok(cuadrantes);
    }

    [HttpGet("catalogs/cuadrantes/{id:int}")]
    public async Task<ActionResult<CuadranteDto>> GetCuadrante(int id)
    {
        var cuadrante = await _catalogService.GetCuadranteByIdAsync(id);
        if (cuadrante == null)
            return NotFound(new { message = $"Cuadrante con ID {id} no encontrado" });
        return Ok(cuadrante);
    }

    [HttpPost("catalogs/cuadrantes")]
    public async Task<ActionResult<CuadranteDto>> CreateCuadrante([FromBody] CreateCuadranteDto createDto)
    {
        try
        {
            var adminId = GetAdminId();
            var cuadrante = await _catalogService.CreateCuadranteAsync(createDto, adminId);
            return CreatedAtAction(nameof(GetCuadrante), new { id = cuadrante.Id }, cuadrante);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating cuadrante");
            return StatusCode(500, new { message = "Error al crear cuadrante" });
        }
    }

    [HttpPut("catalogs/cuadrantes/{id:int}")]
    public async Task<ActionResult<CuadranteDto>> UpdateCuadrante(int id, [FromBody] CreateCuadranteDto updateDto)
    {
        try
        {
            var adminId = GetAdminId();
            var cuadrante = await _catalogService.UpdateCuadranteAsync(id, updateDto, adminId);
            return Ok(cuadrante);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cuadrante {CuadranteId}", id);
            return StatusCode(500, new { message = "Error al actualizar cuadrante" });
        }
    }

    [HttpPost("catalogs/cuadrantes/{id:int}/toggle")]
    public async Task<ActionResult<CuadranteDto>> ToggleCuadrante(int id)
    {
        try
        {
            var adminId = GetAdminId();
            var cuadrante = await _catalogService.ToggleCuadranteActivoAsync(id, adminId);
            return Ok(cuadrante);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling cuadrante {CuadranteId}", id);
            return StatusCode(500, new { message = "Error al cambiar estado de cuadrante" });
        }
    }

    // Sugerencias

    [HttpGet("catalogs/sugerencias")]
    public async Task<ActionResult<List<SugerenciaDto>>> GetSugerencias([FromQuery] string? campo = null, [FromQuery] bool? activo = null)
    {
        var sugerencias = await _catalogService.GetSugerenciasAsync(campo, activo);
        return Ok(sugerencias);
    }

    [HttpGet("catalogs/sugerencias/campos")]
    public ActionResult<object[]> GetSugerenciaCampos()
    {
        var campos = SugerenciaCampos.All.Select(c => new
        {
            value = c,
            label = SugerenciaCampos.GetDisplayName(c)
        });
        return Ok(campos);
    }

    [HttpGet("catalogs/sugerencias/{id:int}")]
    public async Task<ActionResult<SugerenciaDto>> GetSugerencia(int id)
    {
        var sugerencia = await _catalogService.GetSugerenciaByIdAsync(id);
        if (sugerencia == null)
            return NotFound(new { message = $"Sugerencia con ID {id} no encontrada" });
        return Ok(sugerencia);
    }

    [HttpPost("catalogs/sugerencias")]
    public async Task<ActionResult<SugerenciaDto>> CreateSugerencia([FromBody] CreateSugerenciaDto createDto)
    {
        try
        {
            var adminId = GetAdminId();
            var sugerencia = await _catalogService.CreateSugerenciaAsync(createDto, adminId);
            return CreatedAtAction(nameof(GetSugerencia), new { id = sugerencia.Id }, sugerencia);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating sugerencia");
            return StatusCode(500, new { message = "Error al crear sugerencia" });
        }
    }

    [HttpPut("catalogs/sugerencias/{id:int}")]
    public async Task<ActionResult<SugerenciaDto>> UpdateSugerencia(int id, [FromBody] CreateSugerenciaDto updateDto)
    {
        try
        {
            var adminId = GetAdminId();
            var sugerencia = await _catalogService.UpdateSugerenciaAsync(id, updateDto, adminId);
            return Ok(sugerencia);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating sugerencia {SugerenciaId}", id);
            return StatusCode(500, new { message = "Error al actualizar sugerencia" });
        }
    }

    [HttpDelete("catalogs/sugerencias/{id:int}")]
    public async Task<IActionResult> DeleteSugerencia(int id)
    {
        try
        {
            var adminId = GetAdminId();
            await _catalogService.DeleteSugerenciaAsync(id, adminId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting sugerencia {SugerenciaId}", id);
            return StatusCode(500, new { message = "Error al eliminar sugerencia" });
        }
    }

    #endregion

    #region Helpers

    private Guid GetAdminId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Usuario no autenticado");
        }
        return userId;
    }

    #endregion
}
