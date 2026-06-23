namespace Trading.Agents;

/// <summary>One role in the crew: takes an input prompt and returns the model's text reply.</summary>
public interface IChatAgent
{
    /// <summary>Role name, e.g. <c>analyst</c> or <c>trader</c>.</summary>
    string Name { get; }

    /// <summary>Runs the agent on <paramref name="input"/> and returns its reply text.</summary>
    /// <param name="input">The prompt/context for this turn.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<string> RunAsync(string input, CancellationToken cancellationToken = default);
}
