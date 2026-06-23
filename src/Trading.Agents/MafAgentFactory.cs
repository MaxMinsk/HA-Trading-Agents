using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Trading.Agents;

/// <summary>
/// Creates Microsoft Agent Framework agents over a shared <see cref="IChatClient"/>. Confines the MAF
/// API surface to this class so the rest of the crew is insulated from framework churn.
/// </summary>
public sealed class MafAgentFactory : IAgentFactory
{
    private readonly IChatClient _client;

    /// <summary>Creates the factory over an <see cref="IChatClient"/> (see <see cref="ChatProviderFactory"/>).</summary>
    /// <param name="client">The chat client all agents share.</param>
    public MafAgentFactory(IChatClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client;
    }

    /// <inheritdoc />
    public IChatAgent Create(string name, string instructions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(instructions);
        var agent = new ChatClientAgent(_client, instructions, name);
        return new MafChatAgent(name, agent);
    }
}
