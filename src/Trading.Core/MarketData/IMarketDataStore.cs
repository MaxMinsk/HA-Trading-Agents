namespace Trading.Core.MarketData;

/// <summary>
/// Durable, append-friendly store of market data. Backed by a time-series/columnar store in
/// Trading.Data (storage choice pending TRD-001); this contract stays storage-agnostic.
/// </summary>
public interface IMarketDataStore
{
    /// <summary>Persists candles idempotently (keyed by symbol/market/interval/open time).</summary>
    /// <param name="candles">Candles to upsert.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpsertCandlesAsync(IReadOnlyList<Candle> candles, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns up to <paramref name="limit"/> stored candles known at or before <paramref name="asOfUtc"/>,
    /// ascending by close time. The store must never return candles closing after the decision time.
    /// </summary>
    /// <param name="symbol">Exchange symbol.</param>
    /// <param name="market">The market.</param>
    /// <param name="interval">The candle interval.</param>
    /// <param name="asOfUtc">The decision timestamp (UTC).</param>
    /// <param name="limit">Maximum number of most-recent candles to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<Candle>> GetCandlesAsOfAsync(
        string symbol,
        Market market,
        CandleInterval interval,
        DateTimeOffset asOfUtc,
        int limit,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns stored candles whose open time is within [<paramref name="fromUtc"/>, <paramref name="toUtc"/>],
    /// ascending by close time. Used by the backtest harness to load a contiguous range.
    /// </summary>
    /// <param name="symbol">Exchange symbol.</param>
    /// <param name="market">The market.</param>
    /// <param name="interval">The candle interval.</param>
    /// <param name="fromUtc">Range start (UTC), inclusive, by open time.</param>
    /// <param name="toUtc">Range end (UTC), inclusive, by open time.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<Candle>> GetCandlesRangeAsync(
        string symbol,
        Market market,
        CandleInterval interval,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default);

    /// <summary>Summarises every stored candle series (count + time range), for status/availability reporting.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<SeriesSummary>> GetSeriesSummaryAsync(CancellationToken cancellationToken = default);
}
