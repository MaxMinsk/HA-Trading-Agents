using Trading.Core.Decisions;
using Trading.Core.MarketData;

namespace Trading.Core.Strategies;

/// <summary>
/// A strategy turns a point-in-time market snapshot into a trade decision. Async so that both
/// deterministic rule strategies and (later) LLM/agent strategies satisfy the same contract.
/// </summary>
public interface IStrategy
{
    /// <summary>A short identifier for reports, e.g. <c>sma(20,50)</c> or <c>buy-and-hold</c>.</summary>
    string Name { get; }

    /// <summary>Decides an action from a point-in-time snapshot.</summary>
    /// <param name="snapshot">Market data known at or before the decision time (no look-ahead).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<TradeDecision> DecideAsync(MarketSnapshot snapshot, CancellationToken cancellationToken = default);
}
