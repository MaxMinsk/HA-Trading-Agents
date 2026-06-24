namespace Trading.Api;

/// <summary>Body for POST /api/settings. A null field is left unchanged; a non-null value (incl. "") is applied.</summary>
public sealed record SettingsUpdate(
    string? McpUrl,
    string? McpBearer,
    string? LlmProvider,
    string? LlmModel,
    string? LlmApiKey);
