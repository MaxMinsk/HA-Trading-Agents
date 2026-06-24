namespace Trading.Api;

/// <summary>Persisted runtime settings (server-side only). Any field may be null = "not set".</summary>
public sealed record StoredSettings
{
    public string? McpUrl { get; init; }
    public string? McpBearer { get; init; }
    public string? LlmProvider { get; init; }
    public string? LlmModel { get; init; }
    public string? LlmApiKey { get; init; }
}
