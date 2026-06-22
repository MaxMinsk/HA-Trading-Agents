using Trading.Core.MarketData;

namespace Trading.Data.Binance;

/// <summary>Maps domain enums to Binance API codes and endpoints. Futures-ready: both spot and USDⓈ-M klines share the same response shape.</summary>
public static class BinanceIntervals
{
    /// <summary>Returns the Binance interval code (e.g. <c>1h</c>) for a <see cref="CandleInterval"/>.</summary>
    /// <param name="interval">The domain interval.</param>
    public static string ToCode(CandleInterval interval) => interval switch
    {
        CandleInterval.OneHour => "1h",
        CandleInterval.FourHours => "4h",
        CandleInterval.OneDay => "1d",
        _ => throw new ArgumentOutOfRangeException(nameof(interval), interval, "Unsupported interval."),
    };

    /// <summary>Returns the klines endpoint base URL for a <see cref="Market"/>.</summary>
    /// <param name="market">The market.</param>
    public static string KlinesBaseUrl(Market market) => market switch
    {
        Market.Spot => "https://api.binance.com/api/v3/klines",
        Market.UsdmFutures => "https://fapi.binance.com/fapi/v1/klines",
        _ => throw new ArgumentOutOfRangeException(nameof(market), market, "Unsupported market."),
    };

    /// <summary>Returns the WebSocket stream base (host) URL for a <see cref="Market"/>; combine as <c>{base}/stream?streams=...</c>.</summary>
    /// <param name="market">The market.</param>
    public static string StreamBaseUrl(Market market) => market switch
    {
        Market.Spot => "wss://stream.binance.com:9443",
        Market.UsdmFutures => "wss://fstream.binance.com",
        _ => throw new ArgumentOutOfRangeException(nameof(market), market, "Unsupported market."),
    };

    /// <summary>Maps a Binance interval code (e.g. <c>1h</c>) back to a <see cref="CandleInterval"/>.</summary>
    /// <param name="code">The Binance interval code.</param>
    /// <param name="interval">The mapped interval when recognised.</param>
    /// <returns>True when the code is a supported interval.</returns>
    public static bool TryFromCode(string? code, out CandleInterval interval)
    {
        switch (code)
        {
            case "1h": interval = CandleInterval.OneHour; return true;
            case "4h": interval = CandleInterval.FourHours; return true;
            case "1d": interval = CandleInterval.OneDay; return true;
            default: interval = default; return false;
        }
    }
}
