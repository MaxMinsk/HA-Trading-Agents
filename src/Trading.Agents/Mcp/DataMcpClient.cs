using System.Text.Json;
using Trading.Core.MarketData;

namespace Trading.Agents.Mcp;

/// <summary>
/// Typed access to the trading MCP surface: a point-in-time snapshot (parsed to a
/// <see cref="MarketSnapshot"/>) plus passthrough JSON for status, balances, and execution. Used by
/// the CLI host and the web backend so both share one client and one snapshot parser.
/// </summary>
public sealed class DataMcpClient
{
    private static readonly Dictionary<string, object?> NoArgs = new(StringComparer.Ordinal);

    private readonly McpHttpClient _mcp;

    /// <summary>Creates the client over a raw MCP client.</summary>
    /// <param name="mcp">The JSON-RPC MCP client.</param>
    public DataMcpClient(McpHttpClient mcp)
    {
        ArgumentNullException.ThrowIfNull(mcp);
        _mcp = mcp;
    }

    /// <summary>Convenience: creates the client over an HTTP client and MCP endpoint.</summary>
    /// <param name="http">HTTP client (with bearer header if needed).</param>
    /// <param name="endpoint">The /mcp endpoint.</param>
    public DataMcpClient(HttpClient http, Uri endpoint)
        : this(new McpHttpClient(http, endpoint))
    {
    }

    /// <summary>Fetches a point-in-time snapshot and parses it into a <see cref="MarketSnapshot"/>.</summary>
    /// <param name="symbol">Symbol, e.g. BTCUSDT.</param>
    /// <param name="interval">Interval (1h/4h/1d).</param>
    /// <param name="market">Market (spot/usdm).</param>
    /// <param name="lookback">Max candles.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<MarketSnapshot> GetSnapshotAsync(
        string symbol,
        string interval,
        string market,
        int lookback,
        CancellationToken cancellationToken = default)
    {
        var json = await _mcp.CallToolAsync(
            "market_snapshot",
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["symbol"] = symbol,
                ["interval"] = interval,
                ["market"] = market,
                ["lookback"] = lookback,
            },
            cancellationToken).ConfigureAwait(false);
        return ParseSnapshot(json);
    }

    /// <summary>Lists stored series (raw structured JSON from market_status).</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task<JsonElement> GetStatusAsync(CancellationToken cancellationToken = default) =>
        _mcp.CallToolAsync("market_status", NoArgs, cancellationToken);

    /// <summary>Reads account balances (raw structured JSON from account_balances; needs execution enabled).</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task<JsonElement> GetBalancesAsync(CancellationToken cancellationToken = default) =>
        _mcp.CallToolAsync("account_balances", NoArgs, cancellationToken);

    /// <summary>Submits a trade intent through the server-side risk gate (raw outcome JSON).</summary>
    /// <param name="symbol">Symbol.</param>
    /// <param name="action">buy, sell, or hold.</param>
    /// <param name="sizeFraction">Size fraction in [0,1].</param>
    /// <param name="interval">Interval used for the reference price.</param>
    /// <param name="market">Market.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task<JsonElement> SubmitIntentAsync(
        string symbol,
        string action,
        decimal sizeFraction,
        string interval,
        string market,
        CancellationToken cancellationToken = default) =>
        _mcp.CallToolAsync(
            "exec_submit_intent",
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["symbol"] = symbol,
                ["action"] = action,
                ["sizeFraction"] = sizeFraction,
                ["interval"] = interval,
                ["market"] = market,
            },
            cancellationToken);

    /// <summary>Parses a serialized market_snapshot result into a domain <see cref="MarketSnapshot"/>.</summary>
    /// <param name="root">The structured snapshot JSON.</param>
    public static MarketSnapshot ParseSnapshot(JsonElement root)
    {
        var symbol = root.GetProperty("symbol").GetString() ?? "UNKNOWN";
        var market = ParseEnum<Market>(root.GetProperty("market"));
        var asOf = root.GetProperty("asOfUtc").GetDateTimeOffset();

        var candles = new List<Candle>();
        foreach (var c in root.GetProperty("candles").EnumerateArray())
        {
            candles.Add(new Candle
            {
                Symbol = c.GetProperty("symbol").GetString() ?? symbol,
                Market = ParseEnum<Market>(c.GetProperty("market")),
                Interval = ParseEnum<CandleInterval>(c.GetProperty("interval")),
                OpenTimeUtc = c.GetProperty("openTimeUtc").GetDateTimeOffset(),
                CloseTimeUtc = c.GetProperty("closeTimeUtc").GetDateTimeOffset(),
                Open = c.GetProperty("open").GetDecimal(),
                High = c.GetProperty("high").GetDecimal(),
                Low = c.GetProperty("low").GetDecimal(),
                Close = c.GetProperty("close").GetDecimal(),
                Volume = c.GetProperty("volume").GetDecimal(),
                Source = c.TryGetProperty("source", out var s) ? (s.GetString() ?? "mcp") : "mcp",
            });
        }

        return MarketSnapshot.Create(symbol, market, asOf, candles);
    }

    private static TEnum ParseEnum<TEnum>(JsonElement element)
        where TEnum : struct, Enum
    {
        return element.ValueKind == JsonValueKind.Number
            ? (TEnum)Enum.ToObject(typeof(TEnum), element.GetInt32())
            : Enum.Parse<TEnum>(element.GetString() ?? string.Empty, ignoreCase: true);
    }
}
