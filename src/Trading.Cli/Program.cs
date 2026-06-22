using System.Globalization;
using Trading.Core.MarketData;
using Trading.Data.Backfill;
using Trading.Data.Binance;
using Trading.Data.Storage;

// Trading.Cli — backfill historical candles or stream live closed candles into the SQLite store.
//
// Usage:
//   backfill (default):
//     dotnet run --project src/Trading.Cli -- [--symbols BTCUSDT,ETHUSDT] [--interval 1h|4h|1d]
//         [--market spot|usdm] [--days 30] [--db <path>] [--endpoint <spot klines base url>]
//   stream (live; Ctrl+C to stop, or --seconds N to auto-stop):
//     dotnet run --project src/Trading.Cli -- stream [--symbols ...] [--interval ...] [--market ...]
//         [--db <path>] [--ws-endpoint <spot stream base>] [--seconds N]
//
// Defaults: symbols BTCUSDT,ETHUSDT; interval 1h; market spot;
//           db = $TRADING_DB_PATH or ./data/market.sqlite; spot REST mirror = data-api.binance.vision.

var command = args.Length > 0 && !args[0].StartsWith("--", StringComparison.Ordinal) ? args[0] : "backfill";

var symbols = (GetOpt(args, "--symbols") ?? "BTCUSDT,ETHUSDT")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
var interval = ParseInterval(GetOpt(args, "--interval") ?? "1h");
var market = ParseMarket(GetOpt(args, "--market") ?? "spot");
var dbPath = GetOpt(args, "--db")
    ?? Environment.GetEnvironmentVariable("TRADING_DB_PATH")
    ?? Path.Combine("data", "market.sqlite");

var dbDir = Path.GetDirectoryName(Path.GetFullPath(dbPath));
if (!string.IsNullOrEmpty(dbDir))
{
    Directory.CreateDirectory(dbDir);
}

var store = new SqliteMarketDataStore(new SqliteConnectionFactory(dbPath));

if (string.Equals(command, "stream", StringComparison.OrdinalIgnoreCase))
{
    await RunStreamAsync().ConfigureAwait(false);
    return 0;
}

await RunBackfillAsync().ConfigureAwait(false);
return 0;

async Task RunBackfillAsync()
{
    var days = int.Parse(GetOpt(args, "--days") ?? "30", CultureInfo.InvariantCulture);
    var spotEndpoint = GetOpt(args, "--endpoint") ?? "https://data-api.binance.vision/api/v3/klines";

    var now = TimeProvider.System.GetUtcNow();
    var fromUtc = now.AddDays(-days);

    using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    var source = new BinanceMarketDataSource(httpClient, m => m == Market.Spot ? spotEndpoint : BinanceIntervals.KlinesBaseUrl(m));
    var backfill = new BackfillService(source, store);
    var snapshots = new StoredSnapshotBuilder(store);

    Console.WriteLine($"Backfill {market} {interval} {string.Join(",", symbols)} | last {days}d | db={dbPath}");
    Console.WriteLine($"Spot klines endpoint: {spotEndpoint}");

    foreach (var symbol in symbols)
    {
        var result = await backfill.BackfillAsync(symbol, market, interval, fromUtc, now);
        var q = result.Quality;
        Console.WriteLine($"  {symbol}: wrote {result.CandlesWritten} candles, range {result.FirstOpenUtc:u} .. {result.LastCloseUtc:u}");
        Console.WriteLine($"    quality: clean={q.IsClean} duplicates={q.DuplicateOpenTimes.Count} gaps={q.Gaps.Count} (missing {q.MissingCandleCount})");

        var snapshot = await snapshots.BuildAsync(symbol, market, interval, now, 5);
        var last = snapshot.Candles.Count > 0 ? snapshot.Candles[^1] : null;
        Console.WriteLine($"    snapshot as of now: {snapshot.Candles.Count} candles; last close {last?.Close} @ {last?.CloseTimeUtc:u}");
    }
}

async Task RunStreamAsync()
{
    var wsEndpoint = GetOpt(args, "--ws-endpoint");
    var secondsOpt = GetOpt(args, "--seconds");

    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        cts.Cancel();
    };
    if (secondsOpt is not null)
    {
        cts.CancelAfter(TimeSpan.FromSeconds(int.Parse(secondsOpt, CultureInfo.InvariantCulture)));
    }

    var subscriptions = symbols.Select(s => new KlineSubscription(s, interval)).ToList();
    var stream = new BinanceKlineCollector(m => m == Market.Spot && wsEndpoint is not null ? wsEndpoint : BinanceIntervals.StreamBaseUrl(m));
    var stopHint = secondsOpt is not null ? $"auto-stop {secondsOpt}s" : "Ctrl+C to stop";

    Console.WriteLine($"Stream {market} {interval} {string.Join(",", symbols)} | db={dbPath} | {stopHint}");

    await stream.RunAsync(
        market,
        subscriptions,
        async (candle, ct) =>
        {
            await store.UpsertCandlesAsync([candle], ct).ConfigureAwait(false);
            Console.WriteLine($"  stored {candle.Symbol} {candle.Interval} close {candle.Close} @ {candle.CloseTimeUtc:u}");
        },
        msg => Console.WriteLine($"  [ws] {msg}"),
        cts.Token).ConfigureAwait(false);
}

static string? GetOpt(string[] args, string name)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (string.Equals(args[i], name, StringComparison.Ordinal))
        {
            return args[i + 1];
        }
    }

    return null;
}

static CandleInterval ParseInterval(string value)
{
    return value switch
    {
        "1h" => CandleInterval.OneHour,
        "4h" => CandleInterval.FourHours,
        "1d" => CandleInterval.OneDay,
        _ => throw new ArgumentException($"Unsupported interval '{value}' (use 1h, 4h, or 1d).", nameof(value)),
    };
}

static Market ParseMarket(string value)
{
    return value.ToUpperInvariant() switch
    {
        "SPOT" => Market.Spot,
        "USDM" or "FUTURES" => Market.UsdmFutures,
        _ => throw new ArgumentException($"Unsupported market '{value}' (use spot or usdm).", nameof(value)),
    };
}
