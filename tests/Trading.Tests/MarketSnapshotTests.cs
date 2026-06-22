using Trading.Core.MarketData;
using Xunit;

namespace Trading.Tests;

/// <summary>Tests for the no-look-ahead invariant — the system's core correctness guarantee (ADR 0002).</summary>
public sealed class MarketSnapshotTests
{
    private static Candle CandleClosingAt(DateTimeOffset closeUtc) => new()
    {
        Symbol = "BTCUSDT",
        Market = Market.Spot,
        Interval = CandleInterval.OneHour,
        OpenTimeUtc = closeUtc.AddHours(-1),
        CloseTimeUtc = closeUtc,
        Open = 100m,
        High = 110m,
        Low = 90m,
        Close = 105m,
        Volume = 1m,
        Source = "test",
    };

    [Fact]
    public void Create_CandleClosesAfterAsOf_Throws()
    {
        var asOf = new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);
        var future = CandleClosingAt(asOf.AddHours(1));

        var ex = Assert.Throws<LookAheadException>(() =>
            MarketSnapshot.Create("BTCUSDT", Market.Spot, asOf, [future]));

        Assert.Equal(future.CloseTimeUtc, ex.OffendingCloseTimeUtc);
    }

    [Fact]
    public void Create_CandlesAtOrBeforeAsOf_AcceptsAndOrdersByCloseTime()
    {
        var asOf = new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);
        var older = CandleClosingAt(asOf.AddHours(-2));
        var atBoundary = CandleClosingAt(asOf);

        var snapshot = MarketSnapshot.Create("BTCUSDT", Market.Spot, asOf, [atBoundary, older]);

        Assert.Equal(2, snapshot.Candles.Count);
        Assert.Equal(older.CloseTimeUtc, snapshot.Candles[0].CloseTimeUtc);
        Assert.Equal(atBoundary.CloseTimeUtc, snapshot.Candles[1].CloseTimeUtc);
    }
}
