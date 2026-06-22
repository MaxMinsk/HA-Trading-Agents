using Trading.Core.MarketData;
using Trading.Data.Quality;

namespace Trading.Data.Backfill;

/// <summary>Outcome of a single backfill run for one symbol/market/interval.</summary>
/// <param name="Symbol">Exchange symbol.</param>
/// <param name="Market">The market.</param>
/// <param name="Interval">The candle interval.</param>
/// <param name="CandlesWritten">Number of candles fetched and upserted.</param>
/// <param name="FirstOpenUtc">Earliest open time written, or null when nothing was fetched.</param>
/// <param name="LastCloseUtc">Latest close time written, or null when nothing was fetched.</param>
/// <param name="Quality">Data-quality report over the fetched candles.</param>
public sealed record BackfillResult(
    string Symbol,
    Market Market,
    CandleInterval Interval,
    int CandlesWritten,
    DateTimeOffset? FirstOpenUtc,
    DateTimeOffset? LastCloseUtc,
    CandleQualityReport Quality);
