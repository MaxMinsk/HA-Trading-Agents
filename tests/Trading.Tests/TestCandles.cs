using Trading.Core.MarketData;

namespace Trading.Tests;

/// <summary>Shared builders for candle test data.</summary>
internal static class TestCandles
{
    public static Candle Hourly(DateTimeOffset openUtc, decimal close = 100m) => new()
    {
        Symbol = "BTCUSDT",
        Market = Market.Spot,
        Interval = CandleInterval.OneHour,
        OpenTimeUtc = openUtc,
        CloseTimeUtc = openUtc.AddHours(1).AddMilliseconds(-1),
        Open = 100m,
        High = 110m,
        Low = 90m,
        Close = close,
        Volume = 1m,
        Source = "test",
    };
}
