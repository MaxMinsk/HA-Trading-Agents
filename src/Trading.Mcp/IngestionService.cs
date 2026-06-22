using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Trading.Core.MarketData;
using Trading.Data.Binance;

namespace Trading.Mcp;

/// <summary>
/// Keeps the store fresh inside the add-on: a startup backfill for the configured symbols, then the live
/// WebSocket collector writing closed candles. Runs alongside the MCP server in one host.
/// </summary>
internal sealed class IngestionService(
    IMarketDataSource source,
    IMarketDataStore store,
    BinanceKlineCollector collector,
    IngestionOptions options,
    TimeProvider time,
    ILogger<IngestionService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var now = time.GetUtcNow();
        var fromUtc = now.AddDays(-options.BackfillDays);

        foreach (var symbol in options.Symbols)
        {
            try
            {
                var candles = await source
                    .GetCandlesAsync(symbol, options.Market, options.Interval, fromUtc, now, stoppingToken)
                    .ConfigureAwait(false);
                await store.UpsertCandlesAsync(candles, stoppingToken).ConfigureAwait(false);
                logger.LogInformation("Backfilled {Count} {Interval} candles for {Symbol}.", candles.Count, options.Interval, symbol);
            }
            catch (HttpRequestException ex)
            {
                logger.LogWarning(ex, "Backfill failed for {Symbol}.", symbol);
            }
        }

        var subscriptions = options.Symbols.Select(s => new KlineSubscription(s, options.Interval)).ToList();
        await collector.RunAsync(
            options.Market,
            subscriptions,
            async (candle, ct) => await store.UpsertCandlesAsync([candle], ct).ConfigureAwait(false),
            message => logger.LogInformation("ingest ws: {Message}", message),
            stoppingToken).ConfigureAwait(false);
    }
}
