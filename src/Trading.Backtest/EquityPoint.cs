namespace Trading.Backtest;

/// <summary>One point on the backtest equity curve.</summary>
/// <param name="TimeUtc">Bar close time.</param>
/// <param name="Equity">Mark-to-market account equity (cash + position value).</param>
public sealed record EquityPoint(DateTimeOffset TimeUtc, decimal Equity);
