using Microsoft.Agents.AI;

namespace Trading.Agents;

/// <summary>Wraps a Microsoft Agent Framework <see cref="AIAgent"/> as an <see cref="IChatAgent"/> role.</summary>
internal sealed class MafChatAgent(string name, AIAgent agent) : IChatAgent
{
    public string Name { get; } = name;

    public async Task<string> RunAsync(string input, CancellationToken cancellationToken = default)
    {
        var response = await agent.RunAsync(input, cancellationToken: cancellationToken).ConfigureAwait(false);
        return response.Text ?? string.Empty;
    }
}
