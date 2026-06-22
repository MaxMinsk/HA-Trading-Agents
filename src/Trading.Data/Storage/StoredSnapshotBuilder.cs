using Trading.Core.MarketData;

namespace Trading.Data.Storage;

/// <summary>
/// Builds a point-in-time <see cref="MarketSnapshot"/> from the market-data store. The store
/// already filters to <c>close_time &lt;= asOf</c>; <see cref="MarketSnapshot.Create"/> re-checks
/// the no-look-ahead invariant, so the guarantee holds even if a store is later swapped in.
/// </summary>
public sealed class StoredSnapshotBuilder : ISnapshotBuilder
{
    private readonly IMarketDataStore _store;

    /// <summary>Creates a builder over the given store.</summary>
    /// <param name="store">The market-data store to read from.</param>
    public StoredSnapshotBuilder(IMarketDataStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    /// <inheritdoc />
    public async Task<MarketSnapshot> BuildAsync(
        string symbol,
        Market market,
        CandleInterval interval,
        DateTimeOffset asOfUtc,
        int lookbackCandles,
        CancellationToken cancellationToken = default)
    {
        var candles = await _store
            .GetCandlesAsOfAsync(symbol, market, interval, asOfUtc, lookbackCandles, cancellationToken)
            .ConfigureAwait(false);

        return MarketSnapshot.Create(symbol, market, asOfUtc, candles);
    }
}
