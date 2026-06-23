using Trading.Core.Decisions;
using Trading.Core.MarketData;
using Trading.Core.Strategies;

namespace Trading.Agent;

/// <summary>A simple SMA-crossover fallback strategy used when no LLM provider is configured.</summary>
internal sealed class SmaStrategy(int fast, int slow) : IStrategy
{
    public string Name => $"sma({fast},{slow})";

    public Task<TradeDecision> DecideAsync(MarketSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var closes = snapshot.Candles.Select(c => c.Close).ToList();
        if (closes.Count < slow)
        {
            return Task.FromResult(TradeDecision.Create(TradeAction.Hold, 0m, 0.5, "warming up"));
        }

        var fastSma = Sma(closes, fast);
        var slowSma = Sma(closes, slow);
        var decision = fastSma > slowSma
            ? TradeDecision.Create(TradeAction.Buy, 1m, 0.6, $"fast {fastSma:0.##} > slow {slowSma:0.##}")
            : TradeDecision.Create(TradeAction.Sell, 0m, 0.6, $"fast {fastSma:0.##} <= slow {slowSma:0.##}");
        return Task.FromResult(decision);
    }

    private static decimal Sma(List<decimal> closes, int period)
    {
        var sum = 0m;
        for (var i = closes.Count - period; i < closes.Count; i++)
        {
            sum += closes[i];
        }

        return sum / period;
    }
}
