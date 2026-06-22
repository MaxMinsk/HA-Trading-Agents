using System.Globalization;
using Trading.Core.MarketData;

namespace Trading.Mcp;

/// <summary>Configuration for the in-add-on ingestion service, read from environment variables.</summary>
internal sealed record IngestionOptions(
    IReadOnlyList<string> Symbols,
    CandleInterval Interval,
    Market Market,
    int BackfillDays,
    string SpotKlinesUrl)
{
    public static IngestionOptions FromEnvironment()
    {
        var symbols = (Environment.GetEnvironmentVariable("TRADING_SYMBOLS") ?? "BTCUSDT,ETHUSDT")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var interval = ParseInterval(Environment.GetEnvironmentVariable("TRADING_INTERVAL") ?? "1h");
        var days = int.TryParse(
            Environment.GetEnvironmentVariable("TRADING_BACKFILL_DAYS"),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var parsed) && parsed > 0 ? parsed : 30;
        var spotUrl = Environment.GetEnvironmentVariable("TRADING_SPOT_KLINES_URL")
            ?? "https://data-api.binance.vision/api/v3/klines";
        return new IngestionOptions(symbols, interval, Market.Spot, days, spotUrl);
    }

    private static CandleInterval ParseInterval(string value) => value switch
    {
        "4h" => CandleInterval.FourHours,
        "1d" => CandleInterval.OneDay,
        _ => CandleInterval.OneHour,
    };
}
