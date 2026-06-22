using Trading.Backtest;
using Trading.Backtest.Execution;
using Trading.Backtest.Strategies;
using Trading.Core.MarketData;
using Xunit;

namespace Trading.Tests;

/// <summary>Tests for the backtest engine (long-only spot, all-in/all-out).</summary>
public sealed class BacktestEngineTests
{
    private static readonly DateTimeOffset Origin = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    private static List<Candle> Series(params decimal[] closes)
    {
        var candles = new List<Candle>(closes.Length);
        for (var i = 0; i < closes.Length; i++)
        {
            candles.Add(TestCandles.Hourly(Origin.AddHours(i), closes[i]));
        }

        return candles;
    }

    [Fact]
    public async Task BuyAndHold_NoFees_ReturnsPriceChange()
    {
        var candles = Series(100m, 110m, 121m); // +21% first close -> last close

        var result = await BacktestEngine.RunAsync(
            "BTCUSDT", Market.Spot, CandleInterval.OneHour, candles,
            new BuyAndHoldStrategy(), new FeeModel(0m, 0m), new BacktestOptions(10_000m));

        Assert.Equal(3, result.Metrics.Bars);
        Assert.Equal(1, result.Metrics.Trades);
        Assert.Equal(21m, Math.Round(result.Metrics.TotalReturnPct, 2));
    }

    [Fact]
    public async Task FlatPrices_ZeroReturn()
    {
        var candles = Series(100m, 100m, 100m);

        var result = await BacktestEngine.RunAsync(
            "BTCUSDT", Market.Spot, CandleInterval.OneHour, candles,
            new BuyAndHoldStrategy(), new FeeModel(0m, 0m), new BacktestOptions(10_000m));

        Assert.Equal(0m, Math.Round(result.Metrics.TotalReturnPct, 2));
    }
}
