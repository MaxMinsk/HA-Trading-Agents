using Trading.Core.MarketData;
using Trading.Data.Quality;

namespace Trading.Data.Backfill;

/// <summary>
/// Orchestrates a one-shot backfill: fetch candles from a source, upsert them into the store
/// (idempotent), and run a data-quality pass over what was fetched.
/// </summary>
public sealed class BackfillService
{
    private readonly IMarketDataSource _source;
    private readonly IMarketDataStore _store;

    /// <summary>Creates the service over a market-data source and store.</summary>
    /// <param name="source">Where candles are fetched from (e.g. Binance REST).</param>
    /// <param name="store">Where candles are persisted.</param>
    public BackfillService(IMarketDataSource source, IMarketDataStore store)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(store);
        _source = source;
        _store = store;
    }

    /// <summary>Fetches candles in [<paramref name="fromUtc"/>, <paramref name="toUtc"/>], stores them, and reports quality.</summary>
    /// <param name="symbol">Exchange symbol.</param>
    /// <param name="market">The market.</param>
    /// <param name="interval">The candle interval.</param>
    /// <param name="fromUtc">Range start (UTC), inclusive.</param>
    /// <param name="toUtc">Range end (UTC), inclusive.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<BackfillResult> BackfillAsync(
        string symbol,
        Market market,
        CandleInterval interval,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        var candles = await _source
            .GetCandlesAsync(symbol, market, interval, fromUtc, toUtc, cancellationToken)
            .ConfigureAwait(false);

        await _store.UpsertCandlesAsync(candles, cancellationToken).ConfigureAwait(false);

        var quality = CandleQualityChecker.Check(candles, interval);
        return new BackfillResult(symbol, market, interval, candles.Count, quality.FirstOpenUtc, quality.LastCloseUtc, quality);
    }
}
