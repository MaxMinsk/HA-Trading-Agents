namespace Trading.Agents;

/// <summary>
/// Reads the LLM provider/model/key from the environment (server-side / local only — keys never go
/// to the browser). Shared by the CLI host and the web backend so both resolve config identically.
/// </summary>
public static class AgentEnvironment
{
    /// <summary>Builds <see cref="LlmOptions"/> from env, or null if no usable provider+key is set.</summary>
    /// <param name="providerOverride">Optional provider name from a request (anthropic/openai); env wins for the key.</param>
    /// <param name="modelOverride">Optional model id from a request.</param>
    public static LlmOptions? TryReadLlmOptions(string? providerOverride = null, string? modelOverride = null)
    {
        var providerRaw = providerOverride ?? Environment.GetEnvironmentVariable("TRADING_LLM_PROVIDER");
        if (string.IsNullOrWhiteSpace(providerRaw))
        {
            return null;
        }

        ChatProvider? provider = providerRaw.Trim().ToUpperInvariant() switch
        {
            "ANTHROPIC" or "CLAUDE" => ChatProvider.Anthropic,
            "OPENAI" or "GPT" => ChatProvider.OpenAI,
            _ => null,
        };
        if (provider is null)
        {
            return null;
        }

        var key = Environment.GetEnvironmentVariable("TRADING_LLM_API_KEY")
            ?? Environment.GetEnvironmentVariable(provider == ChatProvider.Anthropic ? "ANTHROPIC_API_KEY" : "OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        var model = modelOverride
            ?? Environment.GetEnvironmentVariable("TRADING_LLM_MODEL")
            ?? (provider == ChatProvider.Anthropic ? "claude-sonnet-4-6" : "gpt-4.1");
        return LlmOptions.Create(provider.Value, model, key);
    }
}
