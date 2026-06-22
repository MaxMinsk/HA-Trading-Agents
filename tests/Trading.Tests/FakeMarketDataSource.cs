using Trading.Core.MarketData;

namespace Trading.Tests;

/// <summary>An <see cref="IMarketDataSource"/> that returns a fixed candle list (no network).</summary>
internal sealed class FakeMarketDataSource(IReadOnlyList<Candle> candles) : IMarketDataSource
{
    public Task<IReadOnlyList<Candle>> GetCandlesAsync(
        string symbol,
        Market market,
        CandleInterval interval,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default) => Task.FromResult(candles);
}
