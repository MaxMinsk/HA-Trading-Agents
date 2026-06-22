using Trading.Core.Decisions;
using Trading.Core.MarketData;
using Trading.Core.Strategies;

namespace Trading.Backtest.Strategies;

/// <summary>Always targets fully long — the benchmark every other strategy is measured against.</summary>
public sealed class BuyAndHoldStrategy : IStrategy
{
    /// <inheritdoc />
    public string Name => "buy-and-hold";

    /// <inheritdoc />
    public Task<TradeDecision> DecideAsync(MarketSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return Task.FromResult(TradeDecision.Create(TradeAction.Buy, 1m, 1.0, "buy and hold"));
    }
}
