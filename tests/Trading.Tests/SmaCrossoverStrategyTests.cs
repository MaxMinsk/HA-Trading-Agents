using Trading.Backtest.Strategies;
using Trading.Core.Decisions;
using Trading.Core.MarketData;
using Xunit;

namespace Trading.Tests;

/// <summary>Tests for the SMA-crossover baseline strategy.</summary>
public sealed class SmaCrossoverStrategyTests
{
    private static readonly DateTimeOffset Origin = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    private static MarketSnapshot SnapshotFromCloses(params decimal[] closes)
    {
        var candles = new List<Candle>(closes.Length);
        for (var i = 0; i < closes.Length; i++)
        {
            candles.Add(TestCandles.Hourly(Origin.AddHours(i), closes[i]));
        }

        return MarketSnapshot.Create("BTCUSDT", Market.Spot, candles[^1].CloseTimeUtc, candles);
    }

    [Fact]
    public async Task DecideAsync_TooFewCandles_Holds()
    {
        var decision = await new SmaCrossoverStrategy(2, 3).DecideAsync(SnapshotFromCloses(100m, 101m));
        Assert.Equal(TradeAction.Hold, decision.Action);
    }

    [Fact]
    public async Task DecideAsync_FastAboveSlow_Buys()
    {
        var decision = await new SmaCrossoverStrategy(2, 4).DecideAsync(SnapshotFromCloses(100m, 101m, 102m, 110m, 130m));
        Assert.Equal(TradeAction.Buy, decision.Action);
    }

    [Fact]
    public async Task DecideAsync_FastBelowSlow_Sells()
    {
        var decision = await new SmaCrossoverStrategy(2, 4).DecideAsync(SnapshotFromCloses(130m, 120m, 110m, 90m, 70m));
        Assert.Equal(TradeAction.Sell, decision.Action);
    }
}
