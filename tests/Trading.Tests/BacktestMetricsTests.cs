using Trading.Backtest;
using Trading.Core.MarketData;
using Xunit;

namespace Trading.Tests;

/// <summary>Tests for performance metrics derived from an equity curve.</summary>
public sealed class BacktestMetricsTests
{
    private static readonly DateTimeOffset Origin = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    private static List<EquityPoint> Curve(params decimal[] equities)
    {
        var points = new List<EquityPoint>(equities.Length);
        for (var i = 0; i < equities.Length; i++)
        {
            points.Add(new EquityPoint(Origin.AddHours(i), equities[i]));
        }

        return points;
    }

    [Fact]
    public void MaxDrawdown_AndReturn_AreComputed()
    {
        var metrics = BacktestMetrics.FromEquityCurve(Curve(100m, 120m, 60m, 90m), CandleInterval.OneHour, 0);

        Assert.Equal(50m, Math.Round(metrics.MaxDrawdownPct, 2)); // 120 -> 60
        Assert.Equal(-10m, Math.Round(metrics.TotalReturnPct, 2)); // 100 -> 90
    }

    [Fact]
    public void FlatCurve_HasZeroSharpe() =>
        Assert.Equal(0d, BacktestMetrics.FromEquityCurve(Curve(100m, 100m, 100m), CandleInterval.OneDay, 0).Sharpe);

    [Fact]
    public void EmptyCurve_IsZeroed() =>
        Assert.Equal(0, BacktestMetrics.FromEquityCurve([], CandleInterval.OneHour, 0).Bars);
}
