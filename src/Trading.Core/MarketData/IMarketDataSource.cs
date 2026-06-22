namespace Trading.Core.MarketData;

/// <summary>
/// Reads market data from an exchange (e.g. Binance REST / WebSocket). Implementations live in
/// Trading.Data; this contract is storage- and vendor-agnostic.
/// </summary>
public interface IMarketDataSource
{
    /// <summary>Backfills closed candles for a symbol in the inclusive range [<paramref name="fromUtc"/>, <paramref name="toUtc"/>].</summary>
    /// <param name="symbol">Exchange symbol, e.g. <c>BTCUSDT</c>.</param>
    /// <param name="market">The market.</param>
    /// <param name="interval">The candle interval.</param>
    /// <param name="fromUtc">Range start (UTC), inclusive.</param>
    /// <param name="toUtc">Range end (UTC), inclusive.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<Candle>> GetCandlesAsync(
        string symbol,
        Market market,
        CandleInterval interval,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default);
}
