using System.Globalization;
using System.Text.Json;
using Trading.Core.MarketData;

namespace Trading.Data.Binance;

/// <summary>
/// Parses a Binance klines JSON response (an array of arrays) into <see cref="Candle"/> records.
/// Factored out of the HTTP source so it is unit-testable without a network call. Element layout:
/// <c>[openTime, open, high, low, close, volume, closeTime, ...]</c>.
/// </summary>
public static class BinanceKlineParser
{
    /// <summary>Parses klines JSON for one symbol/market/interval.</summary>
    /// <param name="json">The raw klines JSON array.</param>
    /// <param name="symbol">Exchange symbol the data is for.</param>
    /// <param name="market">The market.</param>
    /// <param name="interval">The candle interval.</param>
    /// <param name="source">Provenance label to stamp on each candle.</param>
    /// <param name="ingestedAtUtc">Optional ingestion timestamp.</param>
    public static IReadOnlyList<Candle> Parse(
        string json,
        string symbol,
        Market market,
        CandleInterval interval,
        string source,
        DateTimeOffset? ingestedAtUtc = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        using var document = JsonDocument.Parse(json);
        var candles = new List<Candle>();
        foreach (var element in document.RootElement.EnumerateArray())
        {
            candles.Add(new Candle
            {
                Symbol = symbol,
                Market = market,
                Interval = interval,
                OpenTimeUtc = DateTimeOffset.FromUnixTimeMilliseconds(element[0].GetInt64()),
                CloseTimeUtc = DateTimeOffset.FromUnixTimeMilliseconds(element[6].GetInt64()),
                Open = ParseDecimal(element[1]),
                High = ParseDecimal(element[2]),
                Low = ParseDecimal(element[3]),
                Close = ParseDecimal(element[4]),
                Volume = ParseDecimal(element[5]),
                Source = source,
                IngestedAtUtc = ingestedAtUtc,
            });
        }

        return candles;
    }

    private static decimal ParseDecimal(JsonElement element) =>
        decimal.Parse(element.GetString() ?? "0", CultureInfo.InvariantCulture);
}
