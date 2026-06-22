using System.Globalization;
using System.Text.Json;
using Trading.Core.MarketData;

namespace Trading.Data.Binance;

/// <summary>
/// Parses a Binance kline WebSocket message into a <see cref="Candle"/>, but only when the kline is
/// final (<c>k.x == true</c>) — so only closed, immutable candles are ever stored (no look-ahead).
/// Handles both raw single-stream messages and the combined-stream <c>{"stream","data"}</c> wrapper.
/// Pure and unit-testable.
/// </summary>
public static class BinanceKlineMessageParser
{
    /// <summary>Tries to parse a closed-candle kline message.</summary>
    /// <param name="json">The raw WebSocket message JSON.</param>
    /// <param name="market">The market the stream belongs to.</param>
    /// <param name="candle">The parsed candle when the message is a closed kline; otherwise null.</param>
    /// <returns>True only for a final (closed) kline of a supported interval.</returns>
    public static bool TryParseClosedCandle(string json, Market market, out Candle? candle)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        candle = null;

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var data))
        {
            root = data;
        }

        if (!root.TryGetProperty("k", out var k))
        {
            return false;
        }

        if (!k.TryGetProperty("x", out var closed) || closed.ValueKind != JsonValueKind.True)
        {
            return false;
        }

        if (!k.TryGetProperty("i", out var code) || !BinanceIntervals.TryFromCode(code.GetString(), out var interval))
        {
            return false;
        }

        candle = new Candle
        {
            Symbol = k.GetProperty("s").GetString() ?? string.Empty,
            Market = market,
            Interval = interval,
            OpenTimeUtc = DateTimeOffset.FromUnixTimeMilliseconds(k.GetProperty("t").GetInt64()),
            CloseTimeUtc = DateTimeOffset.FromUnixTimeMilliseconds(k.GetProperty("T").GetInt64()),
            Open = ParseDecimal(k, "o"),
            High = ParseDecimal(k, "h"),
            Low = ParseDecimal(k, "l"),
            Close = ParseDecimal(k, "c"),
            Volume = ParseDecimal(k, "v"),
            Source = "binance-ws",
        };
        return true;
    }

    private static decimal ParseDecimal(JsonElement k, string name) =>
        decimal.Parse(k.GetProperty(name).GetString() ?? "0", CultureInfo.InvariantCulture);
}
