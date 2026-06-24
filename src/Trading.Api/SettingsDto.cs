namespace Trading.Api;

/// <summary>What GET /api/settings returns: non-secret values plus masked secret status.</summary>
public sealed record SettingsDto(
    string? McpUrl,
    SecretStatus McpBearer,
    string? LlmProvider,
    string? LlmModel,
    SecretStatus LlmApiKey,
    bool LlmConfigured)
{
    public static SettingsDto From(SettingsStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        var settings = store.Current;
        return new SettingsDto(
            settings.McpUrl,
            SecretStatus.Of(settings.McpBearer),
            settings.LlmProvider,
            settings.LlmModel,
            SecretStatus.Of(settings.LlmApiKey),
            store.ResolveLlm() is not null);
    }
}
