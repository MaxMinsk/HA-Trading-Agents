namespace Trading.Agents;

/// <summary>
/// Creates role agents. This is the seam that keeps the Microsoft Agent Framework API surface in one
/// place: the crew depends only on this, so it is driven by real MAF agents in production and by a
/// fake in tests (no network, no keys).
/// </summary>
public interface IAgentFactory
{
    /// <summary>Creates an agent with a role name and system instructions.</summary>
    /// <param name="name">Role name.</param>
    /// <param name="instructions">System instructions for the role.</param>
    IChatAgent Create(string name, string instructions);
}
