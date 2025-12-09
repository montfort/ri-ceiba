namespace Ceiba.Shared.DTOs;

/// <summary>
/// DTO for displaying and editing AI configuration.
/// </summary>
public class AiConfigurationDto
{
    public int Id { get; set; }
    public string Proveedor { get; set; } = "OpenAI";
    public string? ApiKey { get; set; }
    public string? ApiKeyMasked { get; set; }
    public string Modelo { get; set; } = "gpt-4o-mini";
    public string? Endpoint { get; set; }
    public string? AzureEndpoint { get; set; }
    public string? AzureApiVersion { get; set; }
    public string? LocalEndpoint { get; set; }
    public int MaxTokens { get; set; } = 1000;
    public double Temperature { get; set; } = 0.7;
    public bool Activo { get; set; } = true;
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO for saving AI configuration.
/// </summary>
public class SaveAiConfigurationDto
{
    public string Proveedor { get; set; } = "OpenAI";
    public string? ApiKey { get; set; }
    public string Modelo { get; set; } = "gpt-4o-mini";
    public string? Endpoint { get; set; }
    public string? AzureEndpoint { get; set; }
    public string? AzureApiVersion { get; set; }
    public string? LocalEndpoint { get; set; }
    public int MaxTokens { get; set; } = 1000;
    public double Temperature { get; set; } = 0.7;
}

/// <summary>
/// DTO for AI connection test result.
/// </summary>
public class AiConnectionTestResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? ResponseTimeMs { get; set; }
}

/// <summary>
/// Available AI providers.
/// </summary>
public static class AiProviders
{
    public const string OpenAI = "OpenAI";
    public const string Gemini = "Gemini";
    public const string DeepSeek = "DeepSeek";
    public const string AzureOpenAI = "AzureOpenAI";
    public const string Local = "Local";
    public const string Ollama = "Ollama";

    public static readonly List<AiProviderInfo> All = new()
    {
        new AiProviderInfo
        {
            Id = OpenAI,
            Name = "OpenAI",
            Description = "Servicio de IA de OpenAI (GPT-4o, GPT-4.1)",
            RequiresApiKey = true,
            DefaultEndpoint = "https://api.openai.com/v1/chat/completions",
            DefaultModels = new[] { "gpt-4o-mini", "gpt-4o", "gpt-4.1-mini", "gpt-4.1-nano", "gpt-3.5-turbo" }
        },
        new AiProviderInfo
        {
            Id = Gemini,
            Name = "Google Gemini",
            Description = "Servicio de IA de Google (Gemini 2.5, Gemini 2.0)",
            RequiresApiKey = true,
            DefaultEndpoint = "https://generativelanguage.googleapis.com/v1beta/models",
            DefaultModels = new[] { "gemini-2.5-flash", "gemini-2.5-flash-lite", "gemini-2.0-flash", "gemini-2.0-flash-lite", "gemini-2.5-pro" }
        },
        new AiProviderInfo
        {
            Id = DeepSeek,
            Name = "DeepSeek",
            Description = "Servicio de IA de DeepSeek (V3, R1 Reasoning)",
            RequiresApiKey = true,
            DefaultEndpoint = "https://api.deepseek.com/chat/completions",
            DefaultModels = new[] { "deepseek-chat", "deepseek-reasoner" }
        },
        new AiProviderInfo
        {
            Id = AzureOpenAI,
            Name = "Azure OpenAI",
            Description = "Servicio de OpenAI en Azure (requiere suscripcion Azure)",
            RequiresApiKey = true,
            RequiresAzureConfig = true,
            DefaultModels = new[] { "gpt-4o", "gpt-4o-mini", "gpt-35-turbo" }
        },
        new AiProviderInfo
        {
            Id = Ollama,
            Name = "Ollama (Local)",
            Description = "Servidor local Ollama para modelos de codigo abierto",
            RequiresApiKey = false,
            RequiresLocalEndpoint = true,
            DefaultEndpoint = "http://localhost:11434/api/generate",
            DefaultModels = new[] { "llama3.2", "llama3.1", "mistral", "qwen2.5", "phi3" }
        },
        new AiProviderInfo
        {
            Id = Local,
            Name = "LLM Local Personalizado",
            Description = "Otro servidor LLM local (LM Studio, vLLM, etc.)",
            RequiresApiKey = false,
            RequiresLocalEndpoint = true,
            DefaultEndpoint = "http://localhost:8080/v1/chat/completions",
            DefaultModels = new[] { "local-model" }
        }
    };
}

/// <summary>
/// Information about an AI provider.
/// </summary>
public class AiProviderInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool RequiresApiKey { get; set; }
    public bool RequiresAzureConfig { get; set; }
    public bool RequiresLocalEndpoint { get; set; }
    public string? DefaultEndpoint { get; set; }
    public string[] DefaultModels { get; set; } = Array.Empty<string>();
}
