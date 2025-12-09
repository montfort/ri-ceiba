using Ceiba.Shared.DTOs;

namespace Ceiba.Core.Interfaces;

/// <summary>
/// Servicio para gestionar la configuración de reportes automatizados
/// </summary>
public interface IAutomatedReportConfigService
{
    /// <summary>
    /// Obtiene la configuración actual de reportes automatizados
    /// </summary>
    Task<AutomatedReportConfigDto?> GetConfigurationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza la configuración de reportes automatizados
    /// </summary>
    Task<AutomatedReportConfigDto> UpdateConfigurationAsync(
        AutomatedReportConfigUpdateDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Crea la configuración inicial si no existe
    /// </summary>
    Task<AutomatedReportConfigDto> EnsureConfigurationExistsAsync(CancellationToken cancellationToken = default);
}
