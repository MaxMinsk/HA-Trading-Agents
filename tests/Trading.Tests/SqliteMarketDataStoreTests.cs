using Microsoft.Data.Sqlite;
using Trading.Core.MarketData;
using Trading.Data.Storage;
using Xunit;

namespace Trading.Tests;

/// <summary>Round-trip + point-in-time tests for the SQLite store (temp DB per test).</summary>
public sealed class SqliteMarketDataStoreTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"trd-test-{Guid.NewGuid():N}.sqlite");
    private readonly SqliteMarketDataStore _store;

    public SqliteMarketDataStoreTests()
    {
        _store = new SqliteMarketDataStore(new SqliteConnectionFactory(_dbPath));
    }

    private static Candle CandleClosingAt(DateTimeOffset closeUtc, decimal close = 105m) => new()
    {
        Symbol = "BTCUSDT",
        Market = Market.Spot,
        Interval = CandleInterval.OneHour,
        OpenTimeUtc = closeUtc.AddHours(-1),
        CloseTimeUtc = closeUtc,
        Open = 100m,
        High = 110m,
        Low = 90m,
        Close = close,
        Volume = 1.5m,
        Source = "test",
    };

    [Fact]
    public async Task UpsertThenGetAsOf_ReturnsStoredCandlesAscendingByCloseTime()
    {
        var asOf = new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);
        var older = CandleClosingAt(asOf.AddHours(-2));
        var newer = CandleClosingAt(asOf);

        await _store.UpsertCandlesAsync([newer, older]);
        var read = await _store.GetCandlesAsOfAsync("BTCUSDT", Market.Spot, CandleInterval.OneHour, asOf, 10);

        Assert.Equal(2, read.Count);
        Assert.Equal(older.CloseTimeUtc, read[0].CloseTimeUtc);
        Assert.Equal(newer.CloseTimeUtc, read[1].CloseTimeUtc);
        Assert.Equal(1.5m, read[0].Volume);
    }

    [Fact]
    public async Task GetCandlesAsOf_ExcludesCandlesClosingAfterAsOf()
    {
        var asOf = new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);
        var past = CandleClosingAt(asOf.AddHours(-1));
        var future = CandleClosingAt(asOf.AddHours(1));

        await _store.UpsertCandlesAsync([past, future]);
        var read = await _store.GetCandlesAsOfAsync("BTCUSDT", Market.Spot, CandleInterval.OneHour, asOf, 10);

        Assert.Single(read);
        Assert.Equal(past.CloseTimeUtc, read[0].CloseTimeUtc);
    }

    [Fact]
    public async Task Upsert_SamePrimaryKeyTwice_IsIdempotentAndUpdatesValues()
    {
        var asOf = new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);
        var first = CandleClosingAt(asOf, close: 100m);
        var second = CandleClosingAt(asOf, close: 200m);

        await _store.UpsertCandlesAsync([first]);
        await _store.UpsertCandlesAsync([second]);
        var read = await _store.GetCandlesAsOfAsync("BTCUSDT", Market.Spot, CandleInterval.OneHour, asOf, 10);

        Assert.Single(read);
        Assert.Equal(200m, read[0].Close);
    }

    [Fact]
    public async Task StoredSnapshotBuilder_BuildsNoLookAheadSnapshot()
    {
        var asOf = new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);
        await _store.UpsertCandlesAsync([CandleClosingAt(asOf.AddHours(-1)), CandleClosingAt(asOf.AddHours(1))]);

        var builder = new StoredSnapshotBuilder(_store);
        var snapshot = await builder.BuildAsync("BTCUSDT", Market.Spot, CandleInterval.OneHour, asOf, 10);

        Assert.All(snapshot.Candles, c => Assert.True(c.CloseTimeUtc <= asOf));
        Assert.Single(snapshot.Candles);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        foreach (var path in new[] { _dbPath, _dbPath + "-wal", _dbPath + "-shm" })
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
