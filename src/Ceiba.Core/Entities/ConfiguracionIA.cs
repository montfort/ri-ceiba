namespace Ceiba.Core.Entities;

/// <summary>
/// Entity for storing AI service configuration.
/// Only one active configuration should exist at a time.
/// US4: Reportes Automatizados Diarios con IA.
/// </summary>
public class ConfiguracionIA : BaseEntity
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// UTC timestamp when the entity was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    /// <summary>
    /// AI provider type: OpenAI, AzureOpenAI, Local, Ollama
    /// </summary>
    public string Proveedor { get; set; } = "OpenAI";

    /// <summary>
    /// API key for the AI service (encrypted in database).
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Model name to use (e.g., gpt-4o-mini, gpt-4o, llama3.2).
    /// </summary>
    public string Modelo { get; set; } = "gpt-4o-mini";

    /// <summary>
    /// API endpoint URL.
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Azure-specific: Resource endpoint.
    /// </summary>
    public string? AzureEndpoint { get; set; }

    /// <summary>
    /// Azure-specific: API version.
    /// </summary>
    public string? AzureApiVersion { get; set; }

    /// <summary>
    /// Local LLM endpoint (e.g., Ollama, LM Studio).
    /// </summary>
    public string? LocalEndpoint { get; set; }

    /// <summary>
    /// Maximum tokens for response generation.
    /// Recommended: 2000-8000 for daily reports with all incidents.
    /// </summary>
    public int MaxTokens { get; set; } = 4000;

    /// <summary>
    /// Temperature for response generation (0.0 - 2.0).
    /// </summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Maximum number of incidents to include in AI narrative.
    /// 0 = unlimited (include all incidents).
    /// Recommended: 50-100 for daily reports.
    /// </summary>
    public int MaxReportesParaNarrativa { get; set; } = 0;

    /// <summary>
    /// Whether this configuration is active.
    /// </summary>
    public bool Activo { get; set; } = true;

    /// <summary>
    /// Last user who modified this configuration.
    /// </summary>
    public Guid? ModificadoPorId { get; set; }

    /// <summary>
    /// Validates the configuration based on the selected provider.
    /// </summary>
    public (bool IsValid, List<string> Errors) Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Proveedor))
            errors.Add("El proveedor es requerido.");

        switch (Proveedor.ToLower())
        {
            case "openai":
                if (string.IsNullOrWhiteSpace(ApiKey))
                    errors.Add("La API Key es requerida para OpenAI.");
                break;

            case "azureopenai":
                if (string.IsNullOrWhiteSpace(ApiKey))
                    errors.Add("La API Key es requerida para Azure OpenAI.");
                if (string.IsNullOrWhiteSpace(AzureEndpoint))
                    errors.Add("El endpoint de Azure es requerido.");
                break;

            case "local":
            case "ollama":
                if (string.IsNullOrWhiteSpace(LocalEndpoint))
                    errors.Add("El endpoint local es requerido.");
                break;
        }

        if (string.IsNullOrWhiteSpace(Modelo))
            errors.Add("El modelo es requerido.");

        if (MaxTokens < 500 || MaxTokens > 128000)
            errors.Add("MaxTokens debe estar entre 500 y 128000.");

        if (Temperature < 0 || Temperature > 2)
            errors.Add("Temperature debe estar entre 0 y 2.");

        if (MaxReportesParaNarrativa < 0 || MaxReportesParaNarrativa > 1000)
            errors.Add("MaxReportesParaNarrativa debe estar entre 0 y 1000.");

        return (errors.Count == 0, errors);
    }
}
