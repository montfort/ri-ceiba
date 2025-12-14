using System.Text.Json;
using System.Text.Json.Serialization;
using Ceiba.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ceiba.Infrastructure.Data;

/// <summary>
/// DTOs for deserializing the regiones.json file structure.
/// </summary>
public record RegionJsonCuadrante(int Numero);

public record RegionJsonSector(
    [property: JsonPropertyName("nombre_sector")] string NombreSector,
    [property: JsonPropertyName("cuadrantes")] List<int> Cuadrantes
);

public record RegionJsonRegion(
    [property: JsonPropertyName("numero_region")] int NumeroRegion,
    [property: JsonPropertyName("sectores")] List<RegionJsonSector> Sectores
);

public record RegionJsonZona(
    [property: JsonPropertyName("nombre_zona")] string NombreZona,
    [property: JsonPropertyName("regiones")] List<RegionJsonRegion> Regiones
);

/// <summary>
/// Service to load geographic hierarchy data from regiones.json.
/// Supports both fresh seeding and updating existing data.
/// </summary>
public class RegionDataLoader : IRegionDataLoader
{
    private readonly CeibaDbContext _context;
    private readonly ILogger<RegionDataLoader> _logger;

    public RegionDataLoader(CeibaDbContext context, ILogger<RegionDataLoader> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Loads geographic data from the embedded regiones.json file.
    /// </summary>
    /// <param name="jsonPath">Path to the regiones.json file</param>
    /// <returns>List of parsed zone data</returns>
    public async Task<List<RegionJsonZona>> LoadFromJsonAsync(string jsonPath)
    {
        if (!File.Exists(jsonPath))
        {
            throw new FileNotFoundException($"regiones.json not found at: {jsonPath}");
        }

        var jsonContent = await File.ReadAllTextAsync(jsonPath);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var zonas = JsonSerializer.Deserialize<List<RegionJsonZona>>(jsonContent, options)
            ?? throw new InvalidOperationException("Failed to deserialize regiones.json");

        _logger.LogInformation("Loaded {ZonaCount} zonas from {Path}", zonas.Count, jsonPath);
        return zonas;
    }

    /// <summary>
    /// Seeds geographic catalogs from JSON data.
    /// This method is idempotent - safe to run multiple times.
    /// </summary>
    /// <param name="zonaData">Parsed zone data from JSON</param>
    /// <param name="adminUserId">User ID for audit tracking</param>
    /// <param name="clearExisting">If true, clears existing data before seeding</param>
    public async Task SeedGeographicCatalogsAsync(
        List<RegionJsonZona> zonaData,
        Guid adminUserId,
        bool clearExisting = false)
    {
        if (clearExisting)
        {
            await ClearGeographicCatalogsAsync();
        }
        else if (await _context.Zonas.AnyAsync())
        {
            _logger.LogInformation("Geographic catalogs already exist. Use clearExisting=true to replace.");
            return;
        }

        var stats = new SeedingStats();

        foreach (var zonaJson in zonaData)
        {
            var zona = new Zona
            {
                Nombre = zonaJson.NombreZona,
                Activo = true,
                UsuarioId = adminUserId
            };

            _context.Zonas.Add(zona);
            await _context.SaveChangesAsync(); // Save to get the ID
            stats.Zonas++;

            foreach (var regionJson in zonaJson.Regiones)
            {
                var region = new Region
                {
                    Nombre = $"Región {regionJson.NumeroRegion}",
                    ZonaId = zona.Id,
                    Activo = true,
                    UsuarioId = adminUserId
                };

                _context.Regiones.Add(region);
                await _context.SaveChangesAsync();
                stats.Regiones++;

                foreach (var sectorJson in regionJson.Sectores)
                {
                    var sector = new Sector
                    {
                        Nombre = sectorJson.NombreSector,
                        RegionId = region.Id,
                        Activo = true,
                        UsuarioId = adminUserId
                    };

                    _context.Sectores.Add(sector);
                    await _context.SaveChangesAsync();
                    stats.Sectores++;

                    foreach (var cuadranteNum in sectorJson.Cuadrantes)
                    {
                        var cuadrante = new Cuadrante
                        {
                            Nombre = cuadranteNum.ToString(),
                            SectorId = sector.Id,
                            Activo = true,
                            UsuarioId = adminUserId
                        };

                        _context.Cuadrantes.Add(cuadrante);
                        stats.Cuadrantes++;
                    }

                    await _context.SaveChangesAsync();
                }
            }
        }

        _logger.LogInformation(
            "Seeded geographic catalogs: {Zonas} zonas, {Regiones} regiones, {Sectores} sectores, {Cuadrantes} cuadrantes",
            stats.Zonas, stats.Regiones, stats.Sectores, stats.Cuadrantes);
    }

    /// <summary>
    /// Clears all geographic catalog data (Cuadrantes, Sectores, Regiones, Zonas).
    /// Will fail if any reports reference geographic data (required fields cannot be nullified).
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if reports exist that reference geographic data.</exception>
    public async Task ClearGeographicCatalogsAsync()
    {
        _logger.LogWarning("Clearing all geographic catalogs...");

        // Check if there are any reports - geographic fields are required, so we can't clear catalogs if reports exist
        var hasReports = await _context.ReportesIncidencia.AnyAsync();

        if (hasReports)
        {
            throw new InvalidOperationException(
                "Cannot clear geographic catalogs: reports exist that reference geographic data. " +
                "Geographic fields are required and cannot be nullified. Delete reports first.");
        }

        // Delete in order (respecting foreign key constraints)
        var deletedCuadrantes = await _context.Cuadrantes.ExecuteDeleteAsync();
        var deletedSectores = await _context.Sectores.ExecuteDeleteAsync();
        var deletedRegiones = await _context.Regiones.ExecuteDeleteAsync();
        var deletedZonas = await _context.Zonas.ExecuteDeleteAsync();

        _logger.LogInformation(
            "Cleared geographic catalogs: {Zonas} zonas, {Regiones} regiones, {Sectores} sectores, {Cuadrantes} cuadrantes",
            deletedZonas, deletedRegiones, deletedSectores, deletedCuadrantes);
    }

    /// <summary>
    /// Gets statistics about the current geographic catalog data.
    /// </summary>
    public async Task<SeedingStats> GetCurrentStatsAsync()
    {
        return new SeedingStats
        {
            Zonas = await _context.Zonas.CountAsync(),
            Regiones = await _context.Regiones.CountAsync(),
            Sectores = await _context.Sectores.CountAsync(),
            Cuadrantes = await _context.Cuadrantes.CountAsync()
        };
    }

    public class SeedingStats
    {
        public int Zonas { get; set; }
        public int Regiones { get; set; }
        public int Sectores { get; set; }
        public int Cuadrantes { get; set; }

        public override string ToString() =>
            $"Zonas: {Zonas}, Regiones: {Regiones}, Sectores: {Sectores}, Cuadrantes: {Cuadrantes}";
    }
}
