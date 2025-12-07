using Ceiba.Shared.DTOs;

namespace Ceiba.Core.Interfaces;

/// <summary>
/// Provider-agnostic abstraction for AI narrative generation.
/// Supports multiple backends: OpenAI, Azure OpenAI, local LLM.
/// US4: Reportes Automatizados Diarios con IA.
/// </summary>
public interface IAiNarrativeService
{
    /// <summary>
    /// Generates a narrative summary based on incident report statistics and descriptions.
    /// </summary>
    /// <param name="request">The narrative generation request with statistics and content.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated narrative response.</returns>
    Task<NarrativeResponseDto> GenerateNarrativeAsync(
        NarrativeRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the AI service is available and properly configured.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the service is available.</returns>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current provider name (e.g., "OpenAI", "AzureOpenAI", "Local").
    /// </summary>
    string ProviderName { get; }
}
