namespace Trading.Agents;

/// <summary>
/// Which provider/model/key the agent layer uses. Constructed where keys are available (locally) and
/// passed to <see cref="ChatProviderFactory"/>. The key is sensitive — never log it.
/// </summary>
public sealed record LlmOptions
{
    private LlmOptions(ChatProvider provider, string model, string apiKey)
    {
        Provider = provider;
        Model = model;
        ApiKey = apiKey;
    }

    /// <summary>The provider.</summary>
    public ChatProvider Provider { get; }

    /// <summary>The model id (e.g. a Claude or GPT model).</summary>
    public string Model { get; }

    /// <summary>The API key (sensitive).</summary>
    public string ApiKey { get; }

    /// <summary>Creates validated options.</summary>
    /// <param name="provider">The provider.</param>
    /// <param name="model">Model id (non-empty).</param>
    /// <param name="apiKey">API key (non-empty).</param>
    public static LlmOptions Create(ChatProvider provider, string model, string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        return new LlmOptions(provider, model, apiKey);
    }
}
