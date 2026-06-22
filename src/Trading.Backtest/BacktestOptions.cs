namespace Trading.Backtest;

/// <summary>Backtest configuration.</summary>
/// <param name="InitialCash">Starting cash in quote currency.</param>
/// <param name="LookbackCandles">Maximum candles included in each point-in-time snapshot.</param>
public sealed record BacktestOptions(decimal InitialCash = 10_000m, int LookbackCandles = 300);
