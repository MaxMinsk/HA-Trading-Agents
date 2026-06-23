using Trading.Core.MarketData;

namespace Trading.Tests;

/// <summary>Builds point-in-time snapshots from a close series for the agent tests.</summary>
internal static class AgentTestData
{
    public static readonly DateTimeOffset AsOf = new(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);

    public static MarketSnapshot Snapshot(decimal[] closes, string symbol = "BTCUSDT")
    {
        var candles = new List<Candle>(closes.Length);
        for (var i = 0; i < closes.Length; i++)
        {
            var close = closes[i];
            var openTime = AsOf.AddHours(-(closes.Length - i));
            candles.Add(new Candle
            {
                Symbol = symbol,
                Market = Market.Spot,
                Interval = CandleInterval.OneHour,
                OpenTimeUtc = openTime,
                CloseTimeUtc = openTime.AddHours(1),
                Open = close,
                High = close + 1m,
                Low = close - 1m,
                Close = close,
                Volume = 1m,
                Source = "test",
            });
        }

        return MarketSnapshot.Create(symbol, Market.Spot, AsOf, candles);
    }
}
