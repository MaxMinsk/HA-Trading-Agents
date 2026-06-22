using System.Globalization;
using Trading.Core.MarketData;

namespace Trading.Data.Binance;

/// <summary>
/// Reads candles from the Binance public klines REST API (spot or USDⓈ-M futures). Pages through
/// the 1000-row limit by advancing the start cursor. The klines base URL is resolved per market,
/// overridable so a caller can point spot at the public data mirror (data-api.binance.vision) or
/// a testnet. Live WebSocket streaming and futures funding/open-interest are later increments.
/// </summary>
public sealed class BinanceMarketDataSource : IMarketDataSource
{
    private const int MaxLimit = 1000;
    private const string Source = "binance-rest";

    private readonly HttpClient _httpClient;
    private readonly Func<Market, string> _klinesBaseUrl;

    /// <summary>Creates the source over a caller-owned <see cref="HttpClient"/> (not disposed here).</summary>
    /// <param name="httpClient">The HTTP client to use.</param>
    /// <param name="klinesBaseUrlResolver">
    /// Optional override for the klines base URL per market; defaults to <see cref="BinanceIntervals.KlinesBaseUrl"/>.
    /// </param>
    public BinanceMarketDataSource(HttpClient httpClient, Func<Market, string>? klinesBaseUrlResolver = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        _httpClient = httpClient;
        _klinesBaseUrl = klinesBaseUrlResolver ?? BinanceIntervals.KlinesBaseUrl;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Candle>> GetCandlesAsync(
        string symbol,
        Market market,
        CandleInterval interval,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        var baseUrl = _klinesBaseUrl(market);
        var code = BinanceIntervals.ToCode(interval);
        var endMs = toUtc.ToUnixTimeMilliseconds();
        var cursorMs = fromUtc.ToUnixTimeMilliseconds();

        var results = new List<Candle>();
        while (cursorMs <= endMs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var url = string.Create(
                CultureInfo.InvariantCulture,
                $"{baseUrl}?symbol={Uri.EscapeDataString(symbol)}&interval={code}&startTime={cursorMs}&endTime={endMs}&limit={MaxLimit}");

            using var response = await _httpClient.GetAsync(new Uri(url), cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            var page = BinanceKlineParser.Parse(json, symbol, market, interval, Source);
            if (page.Count == 0)
            {
                break;
            }

            results.AddRange(page);
            if (page.Count < MaxLimit)
            {
                break;
            }

            cursorMs = page[^1].OpenTimeUtc.ToUnixTimeMilliseconds() + 1;
        }

        return results;
    }
}
