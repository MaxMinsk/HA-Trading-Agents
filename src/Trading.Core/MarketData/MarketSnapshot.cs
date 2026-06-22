using System.Collections.ObjectModel;

namespace Trading.Core.MarketData;

/// <summary>
/// A point-in-time view of the market for a single symbol as of <see cref="AsOfUtc"/>.
/// The core correctness invariant of the whole system (ADR 0002): a snapshot may ONLY contain
/// data known at or before its decision timestamp — no future leakage / look-ahead. Build
/// snapshots with <see cref="Create"/>, which enforces this and orders candles by close time.
/// </summary>
public sealed record MarketSnapshot
{
    private MarketSnapshot(string symbol, Market market, DateTimeOffset asOfUtc, IReadOnlyList<Candle> candles)
    {
        Symbol = symbol;
        Market = market;
        AsOfUtc = asOfUtc;
        Candles = candles;
    }

    /// <summary>Exchange symbol, e.g. <c>BTCUSDT</c>.</summary>
    public string Symbol { get; }

    /// <summary>The market this snapshot covers.</summary>
    public Market Market { get; }

    /// <summary>The decision timestamp this snapshot is valid for (UTC).</summary>
    public DateTimeOffset AsOfUtc { get; }

    /// <summary>Candles known at or before <see cref="AsOfUtc"/>, ascending by close time.</summary>
    public IReadOnlyList<Candle> Candles { get; }

    /// <summary>
    /// Builds a snapshot, enforcing the no-look-ahead invariant: every candle's
    /// <see cref="Candle.CloseTimeUtc"/> must be at or before <paramref name="asOfUtc"/>.
    /// </summary>
    /// <param name="symbol">Exchange symbol.</param>
    /// <param name="market">The market.</param>
    /// <param name="asOfUtc">The decision timestamp (UTC).</param>
    /// <param name="candles">Candidate candles, in any order.</param>
    /// <returns>A validated, close-time-ordered snapshot.</returns>
    /// <exception cref="LookAheadException">A candle closes after the decision timestamp.</exception>
    public static MarketSnapshot Create(string symbol, Market market, DateTimeOffset asOfUtc, IEnumerable<Candle> candles)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentNullException.ThrowIfNull(candles);

        var ordered = candles.OrderBy(c => c.CloseTimeUtc).ToList();
        foreach (var candle in ordered)
        {
            if (candle.CloseTimeUtc > asOfUtc)
            {
                throw new LookAheadException(symbol, asOfUtc, candle.CloseTimeUtc);
            }
        }

        return new MarketSnapshot(symbol, market, asOfUtc, new ReadOnlyCollection<Candle>(ordered));
    }
}
