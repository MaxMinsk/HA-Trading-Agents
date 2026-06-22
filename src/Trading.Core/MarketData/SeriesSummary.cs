namespace Trading.Core.MarketData;

/// <summary>A summary of one stored candle series (symbol/market/interval): count and time range.</summary>
/// <param name="Symbol">Exchange symbol.</param>
/// <param name="Market">The market.</param>
/// <param name="Interval">The candle interval.</param>
/// <param name="Count">Number of stored candles.</param>
/// <param name="FirstOpenUtc">Earliest open time, or null when empty.</param>
/// <param name="LastCloseUtc">Latest close time, or null when empty.</param>
public sealed record SeriesSummary(
    string Symbol,
    Market Market,
    CandleInterval Interval,
    int Count,
    DateTimeOffset? FirstOpenUtc,
    DateTimeOffset? LastCloseUtc);
