namespace Trading.Core.MarketData;

/// <summary>
/// Thrown when data that would only be known after a snapshot's decision timestamp is included
/// in that snapshot — i.e. future leakage / look-ahead. Backtests must never see this (ADR 0002).
/// </summary>
/// <param name="symbol">The symbol whose snapshot was being built.</param>
/// <param name="asOfUtc">The snapshot decision timestamp.</param>
/// <param name="offendingCloseTimeUtc">The close time that lies after <paramref name="asOfUtc"/>.</param>
public sealed class LookAheadException(string symbol, DateTimeOffset asOfUtc, DateTimeOffset offendingCloseTimeUtc)
    : Exception($"Look-ahead detected for {symbol}: a candle closes at {offendingCloseTimeUtc:O}, after the snapshot decision time {asOfUtc:O}.")
{
    /// <summary>The symbol whose snapshot was being built.</summary>
    public string Symbol { get; } = symbol;

    /// <summary>The snapshot decision timestamp.</summary>
    public DateTimeOffset AsOfUtc { get; } = asOfUtc;

    /// <summary>The candle close time that violated the invariant.</summary>
    public DateTimeOffset OffendingCloseTimeUtc { get; } = offendingCloseTimeUtc;
}
