namespace Trading.Core.MarketData;

/// <summary>Builds a point-in-time <see cref="MarketSnapshot"/> for a decision timestamp.</summary>
public interface ISnapshotBuilder
{
    /// <summary>
    /// Assembles a snapshot for <paramref name="symbol"/> as of <paramref name="asOfUtc"/> with up to
    /// <paramref name="lookbackCandles"/> most-recent candles. Implementations MUST produce snapshots
    /// that satisfy the no-look-ahead invariant.
    /// </summary>
    /// <param name="symbol">Exchange symbol.</param>
    /// <param name="market">The market.</param>
    /// <param name="interval">The candle interval.</param>
    /// <param name="asOfUtc">The decision timestamp (UTC).</param>
    /// <param name="lookbackCandles">How many most-recent candles to include.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<MarketSnapshot> BuildAsync(
        string symbol,
        Market market,
        CandleInterval interval,
        DateTimeOffset asOfUtc,
        int lookbackCandles,
        CancellationToken cancellationToken = default);
}
