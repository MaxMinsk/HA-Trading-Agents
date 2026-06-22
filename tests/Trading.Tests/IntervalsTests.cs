using Trading.Core.MarketData;
using Xunit;

namespace Trading.Tests;

/// <summary>Tests for interval-duration semantics.</summary>
public sealed class IntervalsTests
{
    [Theory]
    [InlineData(CandleInterval.OneHour, 1)]
    [InlineData(CandleInterval.FourHours, 4)]
    public void ToTimeSpan_HourlyIntervals_MatchHours(CandleInterval interval, int hours) =>
        Assert.Equal(TimeSpan.FromHours(hours), Intervals.ToTimeSpan(interval));

    [Fact]
    public void ToTimeSpan_OneDay_IsOneDay() =>
        Assert.Equal(TimeSpan.FromDays(1), Intervals.ToTimeSpan(CandleInterval.OneDay));
}
