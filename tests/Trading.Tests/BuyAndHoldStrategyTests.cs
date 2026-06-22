using Trading.Backtest.Strategies;
using Trading.Core.Decisions;
using Trading.Core.MarketData;
using Xunit;

namespace Trading.Tests;

/// <summary>Tests for the buy-and-hold benchmark strategy.</summary>
public sealed class BuyAndHoldStrategyTests
{
    [Fact]
    public async Task DecideAsync_AlwaysBuysFull()
    {
        var asOf = new DateTimeOffset(2026, 6, 22, 0, 0, 0, TimeSpan.Zero);
        var snapshot = MarketSnapshot.Create(
            "BTCUSDT", Market.Spot, asOf,
            [TestCandles.Hourly(asOf.AddHours(-1))]);

        var decision = await new BuyAndHoldStrategy().DecideAsync(snapshot);

        Assert.Equal(TradeAction.Buy, decision.Action);
        Assert.Equal(1m, decision.SizeFraction);
    }
}
