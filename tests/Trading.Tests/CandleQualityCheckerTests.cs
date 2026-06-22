using Trading.Core.MarketData;
using Trading.Data.Quality;
using Xunit;

namespace Trading.Tests;

/// <summary>Tests for duplicate/gap detection in a candle series.</summary>
public sealed class CandleQualityCheckerTests
{
    private static readonly DateTimeOffset Origin = new(2026, 6, 22, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Check_ContiguousSeries_IsClean()
    {
        Candle[] candles =
        [
            TestCandles.Hourly(Origin),
            TestCandles.Hourly(Origin.AddHours(1)),
            TestCandles.Hourly(Origin.AddHours(2)),
        ];

        var report = CandleQualityChecker.Check(candles, CandleInterval.OneHour);

        Assert.True(report.IsClean);
        Assert.Equal(3, report.Count);
        Assert.Empty(report.Gaps);
        Assert.Empty(report.DuplicateOpenTimes);
    }

    [Fact]
    public void Check_MissingCandle_ReportsGapWithMissingCount()
    {
        // Origin+1h is skipped -> a one-candle gap between Origin and Origin+2h.
        Candle[] candles =
        [
            TestCandles.Hourly(Origin),
            TestCandles.Hourly(Origin.AddHours(2)),
            TestCandles.Hourly(Origin.AddHours(3)),
        ];

        var report = CandleQualityChecker.Check(candles, CandleInterval.OneHour);

        Assert.False(report.IsClean);
        var gap = Assert.Single(report.Gaps);
        Assert.Equal(1, gap.MissingCount);
        Assert.Equal(1, report.MissingCandleCount);
    }

    [Fact]
    public void Check_DuplicateOpenTime_ReportsDuplicate()
    {
        Candle[] candles = [TestCandles.Hourly(Origin), TestCandles.Hourly(Origin)];

        var report = CandleQualityChecker.Check(candles, CandleInterval.OneHour);

        Assert.Equal(Origin, Assert.Single(report.DuplicateOpenTimes));
    }

    [Fact]
    public void Check_Empty_ReturnsCleanZero()
    {
        var report = CandleQualityChecker.Check([], CandleInterval.OneHour);

        Assert.Equal(0, report.Count);
        Assert.True(report.IsClean);
        Assert.Null(report.FirstOpenUtc);
    }
}
