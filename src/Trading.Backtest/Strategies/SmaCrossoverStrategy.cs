using System.Globalization;
using Trading.Core.Decisions;
using Trading.Core.MarketData;
using Trading.Core.Strategies;

namespace Trading.Backtest.Strategies;

/// <summary>
/// Long-only SMA crossover: when the fast SMA is above the slow SMA, target fully long (Buy);
/// otherwise go flat (Sell). Until there are enough candles for the slow SMA, Hold. Stateless and
/// computed only from the snapshot (no look-ahead).
/// </summary>
public sealed class SmaCrossoverStrategy : IStrategy
{
    private readonly int _fast;
    private readonly int _slow;

    /// <summary>Creates the strategy.</summary>
    /// <param name="fastPeriod">Fast SMA period in bars.</param>
    /// <param name="slowPeriod">Slow SMA period in bars; must exceed the fast period.</param>
    public SmaCrossoverStrategy(int fastPeriod, int slowPeriod)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(fastPeriod, 1);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(slowPeriod, fastPeriod);
        _fast = fastPeriod;
        _slow = slowPeriod;
    }

    /// <inheritdoc />
    public string Name => string.Create(CultureInfo.InvariantCulture, $"sma({_fast},{_slow})");

    /// <inheritdoc />
    public Task<TradeDecision> DecideAsync(MarketSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var candles = snapshot.Candles;
        if (candles.Count < _slow)
        {
            return Task.FromResult(TradeDecision.Create(TradeAction.Hold, 0m, 0.5, "warming up"));
        }

        var fastSma = Sma(candles, _fast);
        var slowSma = Sma(candles, _slow);
        var decision = fastSma > slowSma
            ? TradeDecision.Create(TradeAction.Buy, 1m, 0.6, string.Create(CultureInfo.InvariantCulture, $"fast {fastSma:0.##} > slow {slowSma:0.##}"))
            : TradeDecision.Create(TradeAction.Sell, 0m, 0.6, string.Create(CultureInfo.InvariantCulture, $"fast {fastSma:0.##} <= slow {slowSma:0.##}"));
        return Task.FromResult(decision);
    }

    private static decimal Sma(IReadOnlyList<Candle> candles, int period)
    {
        var sum = 0m;
        for (var i = candles.Count - period; i < candles.Count; i++)
        {
            sum += candles[i].Close;
        }

        return sum / period;
    }
}
