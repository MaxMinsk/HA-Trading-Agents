using Trading.Core.MarketData;

namespace Trading.Backtest;

/// <summary>The outcome of one backtest run.</summary>
/// <param name="StrategyName">Strategy identifier.</param>
/// <param name="Symbol">Symbol tested.</param>
/// <param name="Interval">Bar interval.</param>
/// <param name="Metrics">Performance metrics.</param>
/// <param name="EquityCurve">Equity points in chronological order.</param>
public sealed record BacktestResult(
    string StrategyName,
    string Symbol,
    CandleInterval Interval,
    BacktestMetrics Metrics,
    IReadOnlyList<EquityPoint> EquityCurve);
