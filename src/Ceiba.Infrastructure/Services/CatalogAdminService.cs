using Ceiba.Core.Entities;
using Ceiba.Core.Interfaces;
using Ceiba.Infrastructure.Data;
using Ceiba.Shared.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ceiba.Infrastructure.Services;

/// <summary>
/// Catalog administration service for ADMIN operations.
/// US3: FR-027 to FR-030
/// </summary>
public class CatalogAdminService : ICatalogAdminService
{
    private readonly CeibaDbContext _context;
    private readonly ILogger<CatalogAdminService> _logger;

    // Note: Audit logging is handled automatically by AuditSaveChangesInterceptor
    // No need to inject IAuditService for manual logging

    public CatalogAdminService(
        CeibaDbContext context,
        ILogger<CatalogAdminService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Zona Management

    public async Task<List<ZonaDto>> GetZonasAsync(
        bool? activo = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Zonas.AsQueryable();

        if (activo.HasValue)
        {
            query = query.Where(z => z.Activo == activo.Value);
        }

        return await query
            .OrderBy(z => z.Nombre)
            .Select(z => new ZonaDto
            {
                Id = z.Id,
                Nombre = z.Nombre,
                Activo = z.Activo,
                RegionesCount = z.Regiones.Count
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ZonaDto?> GetZonaByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Zonas
            .Where(z => z.Id == id)
            .Select(z => new ZonaDto
            {
                Id = z.Id,
                Nombre = z.Nombre,
                Activo = z.Activo,
                RegionesCount = z.Regiones.Count
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ZonaDto> CreateZonaAsync(
        CreateZonaDto createDto,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        var zona = new Zona
        {
            Nombre = createDto.Nombre,
            Activo = createDto.Activo
        };

        _context.Zonas.Add(zona);
        await _context.SaveChangesAsync(cancellationToken);

        // Note: Audit logging is handled automatically by AuditSaveChangesInterceptor
        _logger.LogInformation("Zona {Nombre} created by admin {AdminId}", zona.Nombre, adminUserId);

        return new ZonaDto
        {
            Id = zona.Id,
            Nombre = zona.Nombre,
            Activo = zona.Activo,
            RegionesCount = 0
        };
    }

    public async Task<ZonaDto> UpdateZonaAsync(
        int id,
        CreateZonaDto updateDto,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        var zona = await _context.Zonas
            .Include(z => z.Regiones)
            .FirstOrDefaultAsync(z => z.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Zona con ID {id} no encontrada");

        zona.Nombre = updateDto.Nombre;
        zona.Activo = updateDto.Activo;

        await _context.SaveChangesAsync(cancellationToken);

        // Note: Audit logging is handled automatically by AuditSaveChangesInterceptor
        _logger.LogInformation("Zona {Id} updated by admin {AdminId}", id, adminUserId);

        return new ZonaDto
        {
            Id = zona.Id,
            Nombre = zona.Nombre,
            Activo = zona.Activo,
            RegionesCount = zona.Regiones.Count
        };
    }

    public async Task<ZonaDto> ToggleZonaActivoAsync(
        int id,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        var zona = await _context.Zonas
            .Include(z => z.Regiones)
            .FirstOrDefaultAsync(z => z.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Zona con ID {id} no encontrada");

        zona.Activo = !zona.Activo;
        await _context.SaveChangesAsync(cancellationToken);

        // Note: Audit logging is handled automatically by AuditSaveChangesInterceptor
        _logger.LogInformation("Zona {Id} toggled to {Activo} by admin {AdminId}", id, zona.Activo, adminUserId);

        return new ZonaDto
        {
            Id = zona.Id,
            Nombre = zona.Nombre,
            Activo = zona.Activo,
            RegionesCount = zona.Regiones.Count
        };
    }

    #endregion

    #region Region Management

    public async Task<List<RegionDto>> GetRegionesAsync(
        int? zonaId = null,
        bool? activo = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Regiones
            .Include(r => r.Zona)
            .AsQueryable();

        if (zonaId.HasValue)
        {
            query = query.Where(r => r.ZonaId == zonaId.Value);
        }

        if (activo.HasValue)
        {
            query = query.Where(r => r.Activo == activo.Value);
        }

        return await query
            .OrderBy(r => r.Zona.Nombre)
            .ThenBy(r => r.Nombre)
            .Select(r => new RegionDto
            {
                Id = r.Id,
                Nombre = r.Nombre,
                ZonaId = r.ZonaId,
                ZonaNombre = r.Zona.Nombre,
                Activo = r.Activo,
                SectoresCount = r.Sectores.Count
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<RegionDto?> GetRegionByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Regiones
            .Include(r => r.Zona)
            .Where(r => r.Id == id)
            .Select(r => new RegionDto
            {
                Id = r.Id,
                Nombre = r.Nombre,
                ZonaId = r.ZonaId,
                ZonaNombre = r.Zona.Nombre,
                Activo = r.Activo,
                SectoresCount = r.Sectores.Count
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<RegionDto> CreateRegionAsync(
        CreateRegionDto createDto,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        // Validate zona exists
        var zona = await _context.Zonas.FindAsync(new object[] { createDto.ZonaId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Zona con ID {createDto.ZonaId} no encontrada");

        var region = new Region
        {
            Nombre = createDto.Nombre,
            ZonaId = createDto.ZonaId,
            Activo = createDto.Activo,
            UsuarioId = adminUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Regiones.Add(region);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Region {Nombre} created in Zona {ZonaId} by admin {AdminId}",
            region.Nombre, region.ZonaId, adminUserId);

        return new RegionDto
        {
            Id = region.Id,
            Nombre = region.Nombre,
            ZonaId = region.ZonaId,
            ZonaNombre = zona.Nombre,
            Activo = region.Activo,
            SectoresCount = 0
        };
    }

    public async Task<RegionDto> UpdateRegionAsync(
        int id,
        CreateRegionDto updateDto,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        var region = await _context.Regiones
            .Include(r => r.Zona)
            .Include(r => r.Sectores)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Región con ID {id} no encontrada");

        // Validate new zona if changed
        if (region.ZonaId != updateDto.ZonaId)
        {
            var newZona = await _context.Zonas.FindAsync(new object[] { updateDto.ZonaId }, cancellationToken)
                ?? throw new KeyNotFoundException($"Zona con ID {updateDto.ZonaId} no encontrada");
            region.ZonaId = updateDto.ZonaId;
        }

        region.Nombre = updateDto.Nombre;
        region.Activo = updateDto.Activo;

        await _context.SaveChangesAsync(cancellationToken);

        // Reload zona name
        await _context.Entry(region).Reference(r => r.Zona).LoadAsync(cancellationToken);

        _logger.LogInformation("Region {Id} updated by admin {AdminId}", id, adminUserId);

        return new RegionDto
        {
            Id = region.Id,
            Nombre = region.Nombre,
            ZonaId = region.ZonaId,
            ZonaNombre = region.Zona.Nombre,
            Activo = region.Activo,
            SectoresCount = region.Sectores.Count
        };
    }

    public async Task<RegionDto> ToggleRegionActivoAsync(
        int id,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        var region = await _context.Regiones
            .Include(r => r.Zona)
            .Include(r => r.Sectores)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Región con ID {id} no encontrada");

        region.Activo = !region.Activo;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Region {Id} toggled to {Activo} by admin {AdminId}", id, region.Activo, adminUserId);

        return new RegionDto
        {
            Id = region.Id,
            Nombre = region.Nombre,
            ZonaId = region.ZonaId,
            ZonaNombre = region.Zona.Nombre,
            Activo = region.Activo,
            SectoresCount = region.Sectores.Count
        };
    }

    #endregion

    #region Sector Management

    public async Task<List<SectorDto>> GetSectoresAsync(
        int? regionId = null,
        bool? activo = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Sectores
            .Include(s => s.Region)
            .ThenInclude(r => r.Zona)
            .AsQueryable();

        if (regionId.HasValue)
        {
            query = query.Where(s => s.RegionId == regionId.Value);
        }

        if (activo.HasValue)
        {
            query = query.Where(s => s.Activo == activo.Value);
        }

        return await query
            .OrderBy(s => s.Region.Zona.Nombre)
            .ThenBy(s => s.Region.Nombre)
            .ThenBy(s => s.Nombre)
            .Select(s => new SectorDto
            {
                Id = s.Id,
                Nombre = s.Nombre,
                RegionId = s.RegionId,
                RegionNombre = s.Region.Nombre,
                ZonaId = s.Region.ZonaId,
                ZonaNombre = s.Region.Zona.Nombre,
                Activo = s.Activo,
                CuadrantesCount = s.Cuadrantes.Count
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<SectorDto?> GetSectorByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Sectores
            .Include(s => s.Region)
            .ThenInclude(r => r.Zona)
            .Where(s => s.Id == id)
            .Select(s => new SectorDto
            {
                Id = s.Id,
                Nombre = s.Nombre,
                RegionId = s.RegionId,
                RegionNombre = s.Region.Nombre,
                ZonaId = s.Region.ZonaId,
                ZonaNombre = s.Region.Zona.Nombre,
                Activo = s.Activo,
                CuadrantesCount = s.Cuadrantes.Count
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<SectorDto> CreateSectorAsync(
        CreateSectorDto createDto,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        // Validate region exists
        var region = await _context.Regiones
            .Include(r => r.Zona)
            .FirstOrDefaultAsync(r => r.Id == createDto.RegionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Región con ID {createDto.RegionId} no encontrada");

        var sector = new Sector
        {
            Nombre = createDto.Nombre,
            RegionId = createDto.RegionId,
            Activo = createDto.Activo,
            UsuarioId = adminUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Sectores.Add(sector);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Sector {Nombre} created in Region {RegionId} by admin {AdminId}",
            sector.Nombre, sector.RegionId, adminUserId);

        return new SectorDto
        {
            Id = sector.Id,
            Nombre = sector.Nombre,
            RegionId = sector.RegionId,
            RegionNombre = region.Nombre,
            ZonaId = region.ZonaId,
            ZonaNombre = region.Zona.Nombre,
            Activo = sector.Activo,
            CuadrantesCount = 0
        };
    }

    public async Task<SectorDto> UpdateSectorAsync(
        int id,
        CreateSectorDto updateDto,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        var sector = await _context.Sectores
            .Include(s => s.Region)
            .ThenInclude(r => r.Zona)
            .Include(s => s.Cuadrantes)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Sector con ID {id} no encontrado");

        // Validate new region if changed
        if (sector.RegionId != updateDto.RegionId)
        {
            var newRegion = await _context.Regiones
                .Include(r => r.Zona)
                .FirstOrDefaultAsync(r => r.Id == updateDto.RegionId, cancellationToken)
                ?? throw new KeyNotFoundException($"Región con ID {updateDto.RegionId} no encontrada");
            sector.RegionId = updateDto.RegionId;
        }

        sector.Nombre = updateDto.Nombre;
        sector.Activo = updateDto.Activo;

        await _context.SaveChangesAsync(cancellationToken);

        // Reload region and zona names
        await _context.Entry(sector).Reference(s => s.Region).LoadAsync(cancellationToken);
        await _context.Entry(sector.Region).Reference(r => r.Zona).LoadAsync(cancellationToken);

        _logger.LogInformation("Sector {Id} updated by admin {AdminId}", id, adminUserId);

        return new SectorDto
        {
            Id = sector.Id,
            Nombre = sector.Nombre,
            RegionId = sector.RegionId,
            RegionNombre = sector.Region.Nombre,
            ZonaId = sector.Region.ZonaId,
            ZonaNombre = sector.Region.Zona.Nombre,
            Activo = sector.Activo,
            CuadrantesCount = sector.Cuadrantes.Count
        };
    }

    public async Task<SectorDto> ToggleSectorActivoAsync(
        int id,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        var sector = await _context.Sectores
            .Include(s => s.Region)
            .ThenInclude(r => r.Zona)
            .Include(s => s.Cuadrantes)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Sector con ID {id} no encontrado");

        sector.Activo = !sector.Activo;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Sector {Id} toggled to {Activo} by admin {AdminId}", id, sector.Activo, adminUserId);

        return new SectorDto
        {
            Id = sector.Id,
            Nombre = sector.Nombre,
            RegionId = sector.RegionId,
            RegionNombre = sector.Region.Nombre,
            ZonaId = sector.Region.ZonaId,
            ZonaNombre = sector.Region.Zona.Nombre,
            Activo = sector.Activo,
            CuadrantesCount = sector.Cuadrantes.Count
        };
    }

    #endregion

    #region Cuadrante Management

    public async Task<List<CuadranteDto>> GetCuadrantesAsync(
        int? sectorId = null,
        bool? activo = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Cuadrantes
            .Include(c => c.Sector)
            .ThenInclude(s => s.Region)
            .ThenInclude(r => r.Zona)
            .AsQueryable();

        if (sectorId.HasValue)
        {
            query = query.Where(c => c.SectorId == sectorId.Value);
        }

        if (activo.HasValue)
        {
            query = query.Where(c => c.Activo == activo.Value);
        }

        return await query
            .OrderBy(c => c.Sector.Region.Zona.Nombre)
            .ThenBy(c => c.Sector.Region.Nombre)
            .ThenBy(c => c.Sector.Nombre)
            .ThenBy(c => c.Nombre)
            .Select(c => new CuadranteDto
            {
                Id = c.Id,
                Nombre = c.Nombre,
                SectorId = c.SectorId,
                SectorNombre = c.Sector.Nombre,
                RegionId = c.Sector.RegionId,
                RegionNombre = c.Sector.Region.Nombre,
                ZonaId = c.Sector.Region.ZonaId,
                ZonaNombre = c.Sector.Region.Zona.Nombre,
                Activo = c.Activo
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<CuadranteDto?> GetCuadranteByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Cuadrantes
            .Include(c => c.Sector)
            .ThenInclude(s => s.Region)
            .ThenInclude(r => r.Zona)
            .Where(c => c.Id == id)
            .Select(c => new CuadranteDto
            {
                Id = c.Id,
                Nombre = c.Nombre,
                SectorId = c.SectorId,
                SectorNombre = c.Sector.Nombre,
                RegionId = c.Sector.RegionId,
                RegionNombre = c.Sector.Region.Nombre,
                ZonaId = c.Sector.Region.ZonaId,
                ZonaNombre = c.Sector.Region.Zona.Nombre,
                Activo = c.Activo
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CuadranteDto> CreateCuadranteAsync(
        CreateCuadranteDto createDto,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        var sector = await _context.Sectores
            .Include(s => s.Region)
            .ThenInclude(r => r.Zona)
            .FirstOrDefaultAsync(s => s.Id == createDto.SectorId, cancellationToken)
            ?? throw new KeyNotFoundException($"Sector con ID {createDto.SectorId} no encontrado");

        var cuadrante = new Cuadrante
        {
            Nombre = createDto.Nombre,
            SectorId = createDto.SectorId,
            Activo = createDto.Activo,
            UsuarioId = adminUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Cuadrantes.Add(cuadrante);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Cuadrante {Nombre} created in Sector {SectorId} by admin {AdminId}",
            cuadrante.Nombre, cuadrante.SectorId, adminUserId);

        return new CuadranteDto
        {
            Id = cuadrante.Id,
            Nombre = cuadrante.Nombre,
            SectorId = cuadrante.SectorId,
            SectorNombre = sector.Nombre,
            RegionId = sector.RegionId,
            RegionNombre = sector.Region.Nombre,
            ZonaId = sector.Region.ZonaId,
            ZonaNombre = sector.Region.Zona.Nombre,
            Activo = cuadrante.Activo
        };
    }

    public async Task<CuadranteDto> UpdateCuadranteAsync(
        int id,
        CreateCuadranteDto updateDto,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        var cuadrante = await _context.Cuadrantes
            .Include(c => c.Sector)
            .ThenInclude(s => s.Region)
            .ThenInclude(r => r.Zona)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Cuadrante con ID {id} no encontrado");

        if (cuadrante.SectorId != updateDto.SectorId)
        {
            var newSector = await _context.Sectores
                .Include(s => s.Region)
                .ThenInclude(r => r.Zona)
                .FirstOrDefaultAsync(s => s.Id == updateDto.SectorId, cancellationToken)
                ?? throw new KeyNotFoundException($"Sector con ID {updateDto.SectorId} no encontrado");
            cuadrante.SectorId = updateDto.SectorId;
        }

        cuadrante.Nombre = updateDto.Nombre;
        cuadrante.Activo = updateDto.Activo;

        await _context.SaveChangesAsync(cancellationToken);

        await _context.Entry(cuadrante).Reference(c => c.Sector).LoadAsync(cancellationToken);
        await _context.Entry(cuadrante.Sector).Reference(s => s.Region).LoadAsync(cancellationToken);
        await _context.Entry(cuadrante.Sector.Region).Reference(r => r.Zona).LoadAsync(cancellationToken);

        _logger.LogInformation("Cuadrante {Id} updated by admin {AdminId}", id, adminUserId);

        return new CuadranteDto
        {
            Id = cuadrante.Id,
            Nombre = cuadrante.Nombre,
            SectorId = cuadrante.SectorId,
            SectorNombre = cuadrante.Sector.Nombre,
            RegionId = cuadrante.Sector.RegionId,
            RegionNombre = cuadrante.Sector.Region.Nombre,
            ZonaId = cuadrante.Sector.Region.ZonaId,
            ZonaNombre = cuadrante.Sector.Region.Zona.Nombre,
            Activo = cuadrante.Activo
        };
    }

    public async Task<CuadranteDto> ToggleCuadranteActivoAsync(
        int id,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        var cuadrante = await _context.Cuadrantes
            .Include(c => c.Sector)
            .ThenInclude(s => s.Region)
            .ThenInclude(r => r.Zona)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Cuadrante con ID {id} no encontrado");

        cuadrante.Activo = !cuadrante.Activo;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Cuadrante {Id} toggled to {Activo} by admin {AdminId}", id, cuadrante.Activo, adminUserId);

        return new CuadranteDto
        {
            Id = cuadrante.Id,
            Nombre = cuadrante.Nombre,
            SectorId = cuadrante.SectorId,
            SectorNombre = cuadrante.Sector.Nombre,
            RegionId = cuadrante.Sector.RegionId,
            RegionNombre = cuadrante.Sector.Region.Nombre,
            ZonaId = cuadrante.Sector.Region.ZonaId,
            ZonaNombre = cuadrante.Sector.Region.Zona.Nombre,
            Activo = cuadrante.Activo
        };
    }

    #endregion

    #region Suggestion Management

    public async Task<List<SugerenciaDto>> GetSugerenciasAsync(
        string? campo = null,
        bool? activo = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.CatalogosSugerencia.AsQueryable();

        if (!string.IsNullOrWhiteSpace(campo))
        {
            query = query.Where(s => s.Campo == campo);
        }

        if (activo.HasValue)
        {
            query = query.Where(s => s.Activo == activo.Value);
        }

        return await query
            .OrderBy(s => s.Campo)
            .ThenBy(s => s.Orden)
            .ThenBy(s => s.Valor)
            .Select(s => new SugerenciaDto
            {
                Id = s.Id,
                Campo = s.Campo,
                Valor = s.Valor,
                Orden = s.Orden,
                Activo = s.Activo
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<SugerenciaDto?> GetSugerenciaByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _context.CatalogosSugerencia
            .Where(s => s.Id == id)
            .Select(s => new SugerenciaDto
            {
                Id = s.Id,
                Campo = s.Campo,
                Valor = s.Valor,
                Orden = s.Orden,
                Activo = s.Activo
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<SugerenciaDto> CreateSugerenciaAsync(
        CreateSugerenciaDto createDto,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        // Validate campo
        if (!SugerenciaCampos.All.Contains(createDto.Campo))
        {
            throw new ArgumentException($"Campo inválido: {createDto.Campo}. Valores válidos: {string.Join(", ", SugerenciaCampos.All)}");
        }

        // Check for duplicate (campo + valor must be unique)
        var exists = await _context.CatalogosSugerencia
            .AnyAsync(s => s.Campo == createDto.Campo && s.Valor == createDto.Valor, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"Ya existe una sugerencia con el valor '{createDto.Valor}' para el campo '{SugerenciaCampos.GetDisplayName(createDto.Campo)}'.");
        }

        var sugerencia = new CatalogoSugerencia
        {
            Campo = createDto.Campo,
            Valor = createDto.Valor,
            Orden = createDto.Orden,
            Activo = createDto.Activo,
            UsuarioId = adminUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.CatalogosSugerencia.Add(sugerencia);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error saving suggestion: {Campo}={Valor}", createDto.Campo, createDto.Valor);
            throw new InvalidOperationException($"Error al guardar la sugerencia: {ex.InnerException?.Message ?? ex.Message}");
        }

        // Note: Audit logging is handled automatically by AuditSaveChangesInterceptor
        _logger.LogInformation("Sugerencia {Campo}={Valor} created by admin {AdminId}",
            sugerencia.Campo, sugerencia.Valor, adminUserId);

        return new SugerenciaDto
        {
            Id = sugerencia.Id,
            Campo = sugerencia.Campo,
            Valor = sugerencia.Valor,
            Orden = sugerencia.Orden,
            Activo = sugerencia.Activo
        };
    }

    public async Task<SugerenciaDto> UpdateSugerenciaAsync(
        int id,
        CreateSugerenciaDto updateDto,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        var sugerencia = await _context.CatalogosSugerencia.FindAsync(new object[] { id }, cancellationToken)
            ?? throw new KeyNotFoundException($"Sugerencia con ID {id} no encontrada");

        if (!SugerenciaCampos.All.Contains(updateDto.Campo))
        {
            throw new ArgumentException($"Campo inválido: {updateDto.Campo}");
        }

        sugerencia.Campo = updateDto.Campo;
        sugerencia.Valor = updateDto.Valor;
        sugerencia.Orden = updateDto.Orden;
        sugerencia.Activo = updateDto.Activo;

        await _context.SaveChangesAsync(cancellationToken);

        // Note: Audit logging is handled automatically by AuditSaveChangesInterceptor
        _logger.LogInformation("Sugerencia {Id} updated by admin {AdminId}", id, adminUserId);

        return new SugerenciaDto
        {
            Id = sugerencia.Id,
            Campo = sugerencia.Campo,
            Valor = sugerencia.Valor,
            Orden = sugerencia.Orden,
            Activo = sugerencia.Activo
        };
    }

    public async Task DeleteSugerenciaAsync(
        int id,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        var sugerencia = await _context.CatalogosSugerencia.FindAsync(new object[] { id }, cancellationToken)
            ?? throw new KeyNotFoundException($"Sugerencia con ID {id} no encontrada");

        _context.CatalogosSugerencia.Remove(sugerencia);
        await _context.SaveChangesAsync(cancellationToken);

        // Note: Audit logging is handled automatically by AuditSaveChangesInterceptor
        _logger.LogInformation("Sugerencia {Id} deleted by admin {AdminId}", id, adminUserId);
    }

    public async Task ReorderSugerenciasAsync(
        string campo,
        int[] orderedIds,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        var sugerencias = await _context.CatalogosSugerencia
            .Where(s => s.Campo == campo && orderedIds.Contains(s.Id))
            .ToListAsync(cancellationToken);

        for (int i = 0; i < orderedIds.Length; i++)
        {
            var sugerencia = sugerencias.FirstOrDefault(s => s.Id == orderedIds[i]);
            if (sugerencia != null)
            {
                sugerencia.Orden = i;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Note: Audit logging is handled automatically by AuditSaveChangesInterceptor
        _logger.LogInformation("Sugerencias for {Campo} reordered by admin {AdminId}", campo, adminUserId);
    }

    #endregion
}
