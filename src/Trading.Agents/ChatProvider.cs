namespace Trading.Agents;

/// <summary>The LLM provider backing the agent layer. Both reach the agents via one IChatClient seam.</summary>
public enum ChatProvider
{
    /// <summary>Anthropic Claude (via Anthropic.SDK).</summary>
    Anthropic,

    /// <summary>OpenAI (via the OpenAI SDK + Microsoft.Extensions.AI.OpenAI).</summary>
    OpenAI,
}
