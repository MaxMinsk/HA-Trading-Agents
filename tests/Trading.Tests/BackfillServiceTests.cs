using Microsoft.Data.Sqlite;
using Trading.Core.MarketData;
using Trading.Data.Backfill;
using Trading.Data.Storage;
using Xunit;

namespace Trading.Tests;

/// <summary>Backfill orchestration tests (fake source + real temp SQLite store).</summary>
public sealed class BackfillServiceTests : IDisposable
{
    private static readonly DateTimeOffset Origin = new(2026, 6, 22, 0, 0, 0, TimeSpan.Zero);

    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"trd-bf-{Guid.NewGuid():N}.sqlite");
    private readonly SqliteMarketDataStore _store;

    public BackfillServiceTests()
    {
        _store = new SqliteMarketDataStore(new SqliteConnectionFactory(_dbPath));
    }

    [Fact]
    public async Task BackfillAsync_WritesCandlesAndReportsCleanQuality()
    {
        var source = new FakeMarketDataSource([TestCandles.Hourly(Origin), TestCandles.Hourly(Origin.AddHours(1))]);
        var service = new BackfillService(source, _store);

        var result = await service.BackfillAsync("BTCUSDT", Market.Spot, CandleInterval.OneHour, Origin, Origin.AddHours(2));

        Assert.Equal(2, result.CandlesWritten);
        Assert.True(result.Quality.IsClean);

        var stored = await _store.GetCandlesAsOfAsync("BTCUSDT", Market.Spot, CandleInterval.OneHour, Origin.AddHours(2), 10);
        Assert.Equal(2, stored.Count);
    }

    [Fact]
    public async Task BackfillAsync_PropagatesGapInQuality()
    {
        var source = new FakeMarketDataSource([TestCandles.Hourly(Origin), TestCandles.Hourly(Origin.AddHours(2))]);
        var service = new BackfillService(source, _store);

        var result = await service.BackfillAsync("BTCUSDT", Market.Spot, CandleInterval.OneHour, Origin, Origin.AddHours(3));

        Assert.False(result.Quality.IsClean);
        Assert.Equal(1, result.Quality.MissingCandleCount);
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
