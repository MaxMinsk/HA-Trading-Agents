using System.ComponentModel;
using System.Globalization;
using ModelContextProtocol.Server;
using Trading.Core.MarketData;

namespace Trading.Mcp;

/// <summary>
/// The market-data MCP tool surface (read-only). Thin wrapper over the store and snapshot builder so an
/// agent — local or co-located — gets point-in-time data over MCP. Ingestion is not exposed here.
/// </summary>
[McpServerToolType]
public sealed class MarketTools
{
    private readonly IMarketDataStore _store;
    private readonly ISnapshotBuilder _snapshots;
    private readonly TimeProvider _time;

    /// <summary>Creates the tool set over the store, snapshot builder and clock.</summary>
    /// <param name="store">Market-data store.</param>
    /// <param name="snapshots">Point-in-time snapshot builder.</param>
    /// <param name="time">Clock for resolving a default "now" decision time.</param>
    public MarketTools(IMarketDataStore store, ISnapshotBuilder snapshots, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(snapshots);
        ArgumentNullException.ThrowIfNull(time);
        _store = store;
        _snapshots = snapshots;
        _time = time;
    }

    /// <summary>Point-in-time snapshot tool.</summary>
    [McpServerTool(Name = "market_snapshot", ReadOnly = true, UseStructuredContent = true)]
    [Description("Point-in-time market snapshot for a symbol: candles known at or before the decision time (no look-ahead). The agent's primary input.")]
    public Task<MarketSnapshot> GetSnapshot(
        [Description("Symbol, e.g. BTCUSDT")] string symbol,
        [Description("Interval: 1h, 4h, or 1d")] string interval = "1h",
        [Description("Market: spot or usdm")] string market = "spot",
        [Description("Decision time as UTC ISO-8601; default = now")] string? asOf = null,
        [Description("Max candles to include (most recent)")] int lookback = 200,
        CancellationToken cancellationToken = default)
        => _snapshots.BuildAsync(symbol, ParseMarket(market), ParseInterval(interval), ResolveAsOf(asOf), lookback, cancellationToken);

    /// <summary>Candle range tool.</summary>
    [McpServerTool(Name = "market_get_candles", ReadOnly = true, UseStructuredContent = true)]
    [Description("Stored candles for a symbol over a UTC time range (ascending by close time). For backtests / analysis.")]
    public Task<IReadOnlyList<Candle>> GetCandles(
        [Description("Symbol, e.g. BTCUSDT")] string symbol,
        [Description("Range start, UTC ISO-8601")] string fromUtc,
        [Description("Range end, UTC ISO-8601")] string toUtc,
        [Description("Interval: 1h, 4h, or 1d")] string interval = "1h",
        [Description("Market: spot or usdm")] string market = "spot",
        CancellationToken cancellationToken = default)
        => _store.GetCandlesRangeAsync(symbol, ParseMarket(market), ParseInterval(interval), ResolveAsOf(fromUtc), ResolveAsOf(toUtc), cancellationToken);

    /// <summary>Availability/freshness tool.</summary>
    [McpServerTool(Name = "market_status", ReadOnly = true, UseStructuredContent = true)]
    [Description("Lists the stored candle series (symbol/market/interval) with counts and freshness, so an agent can see what data is available.")]
    public Task<IReadOnlyList<SeriesSummary>> GetStatus(CancellationToken cancellationToken = default)
        => _store.GetSeriesSummaryAsync(cancellationToken);

    private DateTimeOffset ResolveAsOf(string? value) =>
        value is null
            ? _time.GetUtcNow()
            : DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

    private static CandleInterval ParseInterval(string value) => value switch
    {
        "1h" => CandleInterval.OneHour,
        "4h" => CandleInterval.FourHours,
        "1d" => CandleInterval.OneDay,
        _ => throw new ArgumentException($"Unsupported interval '{value}' (use 1h, 4h, or 1d).", nameof(value)),
    };

    private static Market ParseMarket(string value) => value.ToUpperInvariant() switch
    {
        "SPOT" => Market.Spot,
        "USDM" or "FUTURES" => Market.UsdmFutures,
        _ => throw new ArgumentException($"Unsupported market '{value}' (use spot or usdm).", nameof(value)),
    };
}
