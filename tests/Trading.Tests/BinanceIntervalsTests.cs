using Trading.Core.MarketData;
using Trading.Data.Binance;
using Xunit;

namespace Trading.Tests;

/// <summary>Mapping tests for Binance interval codes and klines endpoints.</summary>
public sealed class BinanceIntervalsTests
{
    [Theory]
    [InlineData(CandleInterval.OneHour, "1h")]
    [InlineData(CandleInterval.FourHours, "4h")]
    [InlineData(CandleInterval.OneDay, "1d")]
    public void ToCode_MapsInterval(CandleInterval interval, string expected) =>
        Assert.Equal(expected, BinanceIntervals.ToCode(interval));

    [Fact]
    public void KlinesBaseUrl_SpotAndFutures_Differ()
    {
        Assert.Contains("api.binance.com", BinanceIntervals.KlinesBaseUrl(Market.Spot), StringComparison.Ordinal);
        Assert.Contains("fapi.binance.com", BinanceIntervals.KlinesBaseUrl(Market.UsdmFutures), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("1h", CandleInterval.OneHour)]
    [InlineData("4h", CandleInterval.FourHours)]
    [InlineData("1d", CandleInterval.OneDay)]
    public void TryFromCode_KnownCode_RoundTrips(string code, CandleInterval expected)
    {
        Assert.True(BinanceIntervals.TryFromCode(code, out var interval));
        Assert.Equal(expected, interval);
        Assert.Equal(code, BinanceIntervals.ToCode(interval));
    }

    [Fact]
    public void TryFromCode_UnknownCode_ReturnsFalse() =>
        Assert.False(BinanceIntervals.TryFromCode("7m", out _));

    [Fact]
    public void StreamBaseUrl_UsesWssScheme()
    {
        Assert.StartsWith("wss://", BinanceIntervals.StreamBaseUrl(Market.Spot), StringComparison.Ordinal);
        Assert.StartsWith("wss://", BinanceIntervals.StreamBaseUrl(Market.UsdmFutures), StringComparison.Ordinal);
    }
}
