using System.Globalization;
using System.Text;
using Trading.Core.MarketData;

namespace Trading.Agents;

/// <summary>
/// A compact, factual summary of a <see cref="MarketSnapshot"/> for the agents to reason over: last
/// price, recent returns, SMAs + trend, RSI, and the window high/low. Computed only from candles in
/// the snapshot, so it inherits the no-look-ahead invariant. Grounding the agents in numbers keeps
/// the debate about evidence rather than vibes.
/// </summary>
public sealed record MarketBrief
{
    private MarketBrief(
        string symbol,
        DateTimeOffset asOfUtc,
        int candleCount,
        decimal lastPrice,
        decimal returnPctLast,
        decimal returnPctWindow,
        decimal smaFast,
        decimal smaSlow,
        string trend,
        decimal rsi,
        decimal windowHigh,
        decimal windowLow,
        int fastPeriod,
        int slowPeriod)
    {
        Symbol = symbol;
        AsOfUtc = asOfUtc;
        CandleCount = candleCount;
        LastPrice = lastPrice;
        ReturnPctLast = returnPctLast;
        ReturnPctWindow = returnPctWindow;
        SmaFast = smaFast;
        SmaSlow = smaSlow;
        Trend = trend;
        Rsi = rsi;
        WindowHigh = windowHigh;
        WindowLow = windowLow;
        FastPeriod = fastPeriod;
        SlowPeriod = slowPeriod;
    }

    /// <summary>Exchange symbol.</summary>
    public string Symbol { get; }

    /// <summary>Decision timestamp the snapshot is valid for.</summary>
    public DateTimeOffset AsOfUtc { get; }

    /// <summary>Number of candles in the snapshot.</summary>
    public int CandleCount { get; }

    /// <summary>Most recent close.</summary>
    public decimal LastPrice { get; }

    /// <summary>Last candle's return vs the prior close, in percent.</summary>
    public decimal ReturnPctLast { get; }

    /// <summary>Return over the fast window, in percent.</summary>
    public decimal ReturnPctWindow { get; }

    /// <summary>Fast simple moving average.</summary>
    public decimal SmaFast { get; }

    /// <summary>Slow simple moving average.</summary>
    public decimal SmaSlow { get; }

    /// <summary>Trend label: <c>up</c>, <c>down</c>, <c>flat</c>, or <c>warming-up</c>.</summary>
    public string Trend { get; }

    /// <summary>Relative strength index over the configured period (50 when warming up).</summary>
    public decimal Rsi { get; }

    /// <summary>Highest high across the snapshot window.</summary>
    public decimal WindowHigh { get; }

    /// <summary>Lowest low across the snapshot window.</summary>
    public decimal WindowLow { get; }

    /// <summary>Fast SMA period used.</summary>
    public int FastPeriod { get; }

    /// <summary>Slow SMA period used.</summary>
    public int SlowPeriod { get; }

    /// <summary>Builds a brief from a snapshot.</summary>
    /// <param name="snapshot">Point-in-time snapshot (candles ascending by close time).</param>
    /// <param name="fast">Fast SMA period.</param>
    /// <param name="slow">Slow SMA period.</param>
    /// <param name="rsiPeriod">RSI lookback.</param>
    public static MarketBrief Build(MarketSnapshot snapshot, int fast = 20, int slow = 50, int rsiPeriod = 14)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(fast);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(slow);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rsiPeriod);

        var candles = snapshot.Candles;
        var count = candles.Count;
        var closes = new decimal[count];
        for (var i = 0; i < count; i++)
        {
            closes[i] = candles[i].Close;
        }

        var last = count > 0 ? closes[count - 1] : 0m;
        var prev = count > 1 ? closes[count - 2] : last;
        var returnLast = prev != 0m ? (last - prev) / prev * 100m : 0m;

        var windowN = Math.Min(count, fast);
        var windowStart = windowN > 0 ? closes[count - windowN] : last;
        var returnWindow = windowStart != 0m ? (last - windowStart) / windowStart * 100m : 0m;

        var smaFast = Sma(closes, fast);
        var smaSlow = Sma(closes, slow);
        var trend = count < slow
            ? "warming-up"
            : smaFast > smaSlow ? "up" : smaFast < smaSlow ? "down" : "flat";

        var rsi = ComputeRsi(closes, rsiPeriod);
        var (high, low) = HighLow(candles);

        return new MarketBrief(
            snapshot.Symbol, snapshot.AsOfUtc, count, last, returnLast, returnWindow,
            smaFast, smaSlow, trend, rsi, high, low, fast, slow);
    }

    /// <summary>Renders the brief as a compact text block for a prompt.</summary>
    public string ToPromptText()
    {
        var builder = new StringBuilder();
        builder.Append("Market brief for ").Append(Symbol)
            .Append(" as of ").Append(AsOfUtc.ToString("O", CultureInfo.InvariantCulture)).Append('\n');
        builder.Append("- candles: ").Append(CandleCount.ToString(CultureInfo.InvariantCulture)).Append('\n');
        builder.Append("- last price: ").Append(F(LastPrice)).Append('\n');
        builder.Append("- last return: ").Append(F(ReturnPctLast)).Append("%\n");
        builder.Append("- window return (").Append(FastPeriod.ToString(CultureInfo.InvariantCulture))
            .Append("): ").Append(F(ReturnPctWindow)).Append("%\n");
        builder.Append("- SMA").Append(FastPeriod.ToString(CultureInfo.InvariantCulture)).Append(": ").Append(F(SmaFast))
            .Append(" | SMA").Append(SlowPeriod.ToString(CultureInfo.InvariantCulture)).Append(": ").Append(F(SmaSlow))
            .Append(" | trend: ").Append(Trend).Append('\n');
        builder.Append("- RSI: ").Append(F(Rsi)).Append('\n');
        builder.Append("- window high/low: ").Append(F(WindowHigh)).Append(" / ").Append(F(WindowLow)).Append('\n');
        builder.Append("Use only these facts; do not invent data.");
        return builder.ToString();
    }

    private static decimal Sma(decimal[] closes, int period)
    {
        var n = Math.Min(closes.Length, period);
        if (n == 0)
        {
            return 0m;
        }

        var sum = 0m;
        for (var i = closes.Length - n; i < closes.Length; i++)
        {
            sum += closes[i];
        }

        return sum / n;
    }

    private static decimal ComputeRsi(decimal[] closes, int period)
    {
        if (closes.Length <= period)
        {
            return 50m;
        }

        var gain = 0m;
        var loss = 0m;
        for (var i = closes.Length - period; i < closes.Length; i++)
        {
            var delta = closes[i] - closes[i - 1];
            if (delta > 0m)
            {
                gain += delta;
            }
            else
            {
                loss -= delta;
            }
        }

        if (loss == 0m)
        {
            return 100m;
        }

        var avgGain = gain / period;
        var avgLoss = loss / period;
        var rs = avgGain / avgLoss;
        return 100m - (100m / (1m + rs));
    }

    private static (decimal High, decimal Low) HighLow(IReadOnlyList<Candle> candles)
    {
        if (candles.Count == 0)
        {
            return (0m, 0m);
        }

        var high = candles[0].High;
        var low = candles[0].Low;
        foreach (var candle in candles)
        {
            high = Math.Max(high, candle.High);
            low = Math.Min(low, candle.Low);
        }

        return (high, low);
    }

    private static string F(decimal value) => value.ToString("0.####", CultureInfo.InvariantCulture);
}
