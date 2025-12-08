using Ceiba.Core.Entities;

namespace Ceiba.Core.Interfaces;

/// <summary>
/// Service interface for managing AI configuration.
/// US4: Reportes Automatizados Diarios con IA.
/// </summary>
public interface IAiConfigurationService
{
    /// <summary>
    /// Gets the current active AI configuration.
    /// </summary>
    Task<ConfiguracionIA?> GetActiveConfigurationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves or updates the AI configuration.
    /// </summary>
    Task<ConfiguracionIA> SaveConfigurationAsync(ConfiguracionIA configuration, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests the AI connection with the current configuration.
    /// </summary>
    Task<(bool Success, string Message)> TestConnectionAsync(ConfiguracionIA configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the configuration history.
    /// </summary>
    Task<List<ConfiguracionIA>> GetConfigurationHistoryAsync(int take = 10, CancellationToken cancellationToken = default);
}
