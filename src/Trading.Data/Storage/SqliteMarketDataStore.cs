using System.Globalization;
using Microsoft.Data.Sqlite;
using Trading.Core.MarketData;

namespace Trading.Data.Storage;

/// <summary>
/// SQLite-backed <see cref="IMarketDataStore"/>. Decimals are stored as invariant text (no float
/// precision loss); times are stored as UTC Unix milliseconds. Reads are point-in-time at the
/// query level (<c>close_time_ms &lt;= asOf</c>). SQLite has no async I/O, so methods complete
/// synchronously and return completed tasks (concurrency comes from WAL).
/// </summary>
public sealed class SqliteMarketDataStore : IMarketDataStore
{
    private readonly SqliteConnectionFactory _factory;
    private readonly TimeProvider _timeProvider;

    /// <summary>Creates the store and applies any pending schema migrations.</summary>
    /// <param name="factory">Connection factory for the market-data database.</param>
    /// <param name="timeProvider">Clock used to stamp ingestion time when a candle omits it; defaults to the system clock.</param>
    public SqliteMarketDataStore(SqliteConnectionFactory factory, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
        _timeProvider = timeProvider ?? TimeProvider.System;

        using var connection = factory.Open();
        MarketDataMigrator.Apply(connection);
    }

    /// <inheritdoc />
    public Task UpsertCandlesAsync(IReadOnlyList<Candle> candles, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candles);
        cancellationToken.ThrowIfCancellationRequested();
        if (candles.Count == 0)
        {
            return Task.CompletedTask;
        }

        using var connection = _factory.Open();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO candles
                (symbol, market, interval, open_time_ms, close_time_ms, open, high, low, close, volume, source, ingested_at_ms)
            VALUES
                ($symbol, $market, $interval, $openTime, $closeTime, $open, $high, $low, $close, $volume, $source, $ingestedAt)
            ON CONFLICT (symbol, market, interval, open_time_ms) DO UPDATE SET
                close_time_ms = excluded.close_time_ms,
                open = excluded.open, high = excluded.high, low = excluded.low, close = excluded.close,
                volume = excluded.volume, source = excluded.source, ingested_at_ms = excluded.ingested_at_ms;
            """;

        var pSymbol = command.Parameters.Add("$symbol", SqliteType.Text);
        var pMarket = command.Parameters.Add("$market", SqliteType.Integer);
        var pInterval = command.Parameters.Add("$interval", SqliteType.Integer);
        var pOpenTime = command.Parameters.Add("$openTime", SqliteType.Integer);
        var pCloseTime = command.Parameters.Add("$closeTime", SqliteType.Integer);
        var pOpen = command.Parameters.Add("$open", SqliteType.Text);
        var pHigh = command.Parameters.Add("$high", SqliteType.Text);
        var pLow = command.Parameters.Add("$low", SqliteType.Text);
        var pClose = command.Parameters.Add("$close", SqliteType.Text);
        var pVolume = command.Parameters.Add("$volume", SqliteType.Text);
        var pSource = command.Parameters.Add("$source", SqliteType.Text);
        var pIngestedAt = command.Parameters.Add("$ingestedAt", SqliteType.Integer);

        var defaultIngestedAtMs = _timeProvider.GetUtcNow().ToUnixTimeMilliseconds();

        foreach (var candle in candles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            pSymbol.Value = candle.Symbol;
            pMarket.Value = (int)candle.Market;
            pInterval.Value = (int)candle.Interval;
            pOpenTime.Value = candle.OpenTimeUtc.ToUnixTimeMilliseconds();
            pCloseTime.Value = candle.CloseTimeUtc.ToUnixTimeMilliseconds();
            pOpen.Value = candle.Open.ToString(CultureInfo.InvariantCulture);
            pHigh.Value = candle.High.ToString(CultureInfo.InvariantCulture);
            pLow.Value = candle.Low.ToString(CultureInfo.InvariantCulture);
            pClose.Value = candle.Close.ToString(CultureInfo.InvariantCulture);
            pVolume.Value = candle.Volume.ToString(CultureInfo.InvariantCulture);
            pSource.Value = candle.Source;
            pIngestedAt.Value = candle.IngestedAtUtc?.ToUnixTimeMilliseconds() ?? defaultIngestedAtMs;
            command.ExecuteNonQuery();
        }

        transaction.Commit();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Candle>> GetCandlesAsOfAsync(
        string symbol,
        Market market,
        CandleInterval interval,
        DateTimeOffset asOfUtc,
        int limit,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);
        cancellationToken.ThrowIfCancellationRequested();

        using var connection = _factory.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT symbol, market, interval, open_time_ms, close_time_ms, open, high, low, close, volume, source, ingested_at_ms
            FROM candles
            WHERE symbol = $symbol AND market = $market AND interval = $interval AND close_time_ms <= $asOf
            ORDER BY close_time_ms DESC
            LIMIT $limit;
            """;
        command.Parameters.Add("$symbol", SqliteType.Text).Value = symbol;
        command.Parameters.Add("$market", SqliteType.Integer).Value = (int)market;
        command.Parameters.Add("$interval", SqliteType.Integer).Value = (int)interval;
        command.Parameters.Add("$asOf", SqliteType.Integer).Value = asOfUtc.ToUnixTimeMilliseconds();
        command.Parameters.Add("$limit", SqliteType.Integer).Value = limit;

        var results = new List<Candle>();
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                results.Add(MapCandle(reader));
            }
        }

        results.Reverse(); // newest-first from SQL -> ascending by close time for the caller
        return Task.FromResult<IReadOnlyList<Candle>>(results);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Candle>> GetCandlesRangeAsync(
        string symbol,
        Market market,
        CandleInterval interval,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        cancellationToken.ThrowIfCancellationRequested();

        using var connection = _factory.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT symbol, market, interval, open_time_ms, close_time_ms, open, high, low, close, volume, source, ingested_at_ms
            FROM candles
            WHERE symbol = $symbol AND market = $market AND interval = $interval
              AND open_time_ms >= $from AND open_time_ms <= $to
            ORDER BY close_time_ms ASC;
            """;
        command.Parameters.Add("$symbol", SqliteType.Text).Value = symbol;
        command.Parameters.Add("$market", SqliteType.Integer).Value = (int)market;
        command.Parameters.Add("$interval", SqliteType.Integer).Value = (int)interval;
        command.Parameters.Add("$from", SqliteType.Integer).Value = fromUtc.ToUnixTimeMilliseconds();
        command.Parameters.Add("$to", SqliteType.Integer).Value = toUtc.ToUnixTimeMilliseconds();

        var results = new List<Candle>();
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                results.Add(MapCandle(reader));
            }
        }

        return Task.FromResult<IReadOnlyList<Candle>>(results);
    }

    private static Candle MapCandle(SqliteDataReader reader) => new()
    {
        Symbol = reader.GetString(0),
        Market = (Market)reader.GetInt32(1),
        Interval = (CandleInterval)reader.GetInt32(2),
        OpenTimeUtc = DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(3)),
        CloseTimeUtc = DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(4)),
        Open = decimal.Parse(reader.GetString(5), CultureInfo.InvariantCulture),
        High = decimal.Parse(reader.GetString(6), CultureInfo.InvariantCulture),
        Low = decimal.Parse(reader.GetString(7), CultureInfo.InvariantCulture),
        Close = decimal.Parse(reader.GetString(8), CultureInfo.InvariantCulture),
        Volume = decimal.Parse(reader.GetString(9), CultureInfo.InvariantCulture),
        Source = reader.GetString(10),
        IngestedAtUtc = DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(11)),
    };
}
