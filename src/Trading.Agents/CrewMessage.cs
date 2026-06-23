namespace Trading.Agents;

/// <summary>One role's complete message during a crew run, surfaced for the live debate view.</summary>
/// <param name="Role">Role name: analyst, bull, bear, trader, or risk-reviewer.</param>
/// <param name="Content">The role's text output.</param>
public sealed record CrewMessage(string Role, string Content);
