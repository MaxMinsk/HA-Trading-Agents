namespace Trading.Core.MarketData;

/// <summary>
/// A single OHLCV candle (kline) for one symbol/market/interval, with provenance.
/// All times are UTC. <see cref="CloseTimeUtc"/> is the instant the candle is final and
/// becomes known; snapshots use it to enforce the no-look-ahead invariant (ADR 0002).
/// </summary>
public sealed record Candle
{
    /// <summary>Exchange symbol, e.g. <c>BTCUSDT</c>.</summary>
    public required string Symbol { get; init; }

    /// <summary>The market this candle belongs to.</summary>
    public required Market Market { get; init; }

    /// <summary>The candle interval.</summary>
    public required CandleInterval Interval { get; init; }

    /// <summary>Candle open time (UTC).</summary>
    public required DateTimeOffset OpenTimeUtc { get; init; }

    /// <summary>Candle close time (UTC) — the instant the candle becomes final/known.</summary>
    public required DateTimeOffset CloseTimeUtc { get; init; }

    /// <summary>Open price.</summary>
    public required decimal Open { get; init; }

    /// <summary>Highest price in the interval.</summary>
    public required decimal High { get; init; }

    /// <summary>Lowest price in the interval.</summary>
    public required decimal Low { get; init; }

    /// <summary>Close price.</summary>
    public required decimal Close { get; init; }

    /// <summary>Base-asset volume traded in the interval.</summary>
    public required decimal Volume { get; init; }

    /// <summary>Where this candle came from, e.g. <c>binance-rest</c>, <c>binance-ws</c>, <c>binance-data-dump</c>.</summary>
    public required string Source { get; init; }

    /// <summary>When this record was ingested into the store (UTC). Distinct from market time.</summary>
    public DateTimeOffset? IngestedAtUtc { get; init; }
}
