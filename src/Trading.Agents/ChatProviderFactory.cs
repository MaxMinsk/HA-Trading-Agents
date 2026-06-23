using Anthropic.SDK;
using Microsoft.Extensions.AI;
using OpenAI;

namespace Trading.Agents;

/// <summary>
/// Builds the single <see cref="IChatClient"/> the crew runs on, for whichever provider is configured.
/// This is the ONLY place provider SDKs are referenced, so every agent above it is provider-agnostic.
/// The configured model is pinned as the default for every call.
/// </summary>
public static class ChatProviderFactory
{
    /// <summary>Creates an <see cref="IChatClient"/> for the configured provider/model/key.</summary>
    /// <param name="options">Provider, model, and key.</param>
    public static IChatClient Create(LlmOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var inner = (IChatClient)(options.Provider switch
        {
            ChatProvider.OpenAI => new OpenAIClient(options.ApiKey).GetChatClient(options.Model).AsIChatClient(),
            ChatProvider.Anthropic => new AnthropicClient(options.ApiKey).Messages,
            _ => throw new ArgumentOutOfRangeException(nameof(options), options.Provider, "Unknown chat provider."),
        });

        // Pin the model as the default so per-call options need not repeat it (Anthropic sets it per request).
        return inner.AsBuilder()
            .ConfigureOptions(o => o.ModelId ??= options.Model)
            .Build();
    }
}
