using Trading.Core.MarketData;

namespace Trading.Backtest;

/// <summary>Performance metrics derived from a backtest equity curve.</summary>
/// <param name="TotalReturnPct">Total return over the run, in percent.</param>
/// <param name="MaxDrawdownPct">Maximum peak-to-trough drawdown, in percent (non-negative).</param>
/// <param name="Sharpe">Annualised Sharpe ratio of per-bar returns (risk-free rate 0).</param>
/// <param name="Trades">Number of executed trades (entries and exits).</param>
/// <param name="Bars">Number of bars in the equity curve.</param>
public sealed record BacktestMetrics(
    decimal TotalReturnPct,
    decimal MaxDrawdownPct,
    double Sharpe,
    int Trades,
    int Bars)
{
    /// <summary>Computes metrics from an equity curve and the bar interval (for Sharpe annualisation).</summary>
    /// <param name="curve">Equity points in chronological order.</param>
    /// <param name="interval">Bar interval, used to annualise Sharpe.</param>
    /// <param name="trades">Number of executed trades.</param>
    public static BacktestMetrics FromEquityCurve(IReadOnlyList<EquityPoint> curve, CandleInterval interval, int trades)
    {
        ArgumentNullException.ThrowIfNull(curve);
        if (curve.Count == 0)
        {
            return new BacktestMetrics(0m, 0m, 0d, trades, 0);
        }

        var first = curve[0].Equity;
        var last = curve[^1].Equity;
        var totalReturnPct = first == 0m ? 0m : (last - first) / first * 100m;

        var peak = curve[0].Equity;
        var maxDrawdown = 0m;
        var returns = new List<double>(curve.Count);
        for (var i = 0; i < curve.Count; i++)
        {
            var equity = curve[i].Equity;
            if (equity > peak)
            {
                peak = equity;
            }
            else if (peak > 0m)
            {
                var drawdown = (peak - equity) / peak;
                if (drawdown > maxDrawdown)
                {
                    maxDrawdown = drawdown;
                }
            }

            if (i > 0 && curve[i - 1].Equity > 0m)
            {
                returns.Add((double)((curve[i].Equity - curve[i - 1].Equity) / curve[i - 1].Equity));
            }
        }

        return new BacktestMetrics(totalReturnPct, maxDrawdown * 100m, ComputeSharpe(returns, interval), trades, curve.Count);
    }

    private static double ComputeSharpe(List<double> returns, CandleInterval interval)
    {
        if (returns.Count < 2)
        {
            return 0d;
        }

        var mean = returns.Average();
        var variance = returns.Sum(r => (r - mean) * (r - mean)) / (returns.Count - 1);
        var std = Math.Sqrt(variance);
        if (std == 0d)
        {
            return 0d;
        }

        var periodsPerYear = TimeSpan.FromDays(365) / Intervals.ToTimeSpan(interval);
        return mean / std * Math.Sqrt(periodsPerYear);
    }
}
