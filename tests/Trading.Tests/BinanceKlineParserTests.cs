using Trading.Core.MarketData;
using Trading.Data.Binance;
using Xunit;

namespace Trading.Tests;

/// <summary>Parser tests against a sample Binance klines payload (no network).</summary>
public sealed class BinanceKlineParserTests
{
    // One 1h kline: openTime, O,H,L,C,V, closeTime, then fields we ignore.
    private const string SampleJson =
        """[[1700000000000,"42000.10","42500.00","41900.00","42250.50","123.456",1700003599999,"5200000.0",1500,"60.0","2500000.0","0"]]""";

    [Fact]
    public void Parse_SingleKline_MapsAllFields()
    {
        var candles = BinanceKlineParser.Parse(SampleJson, "BTCUSDT", Market.Spot, CandleInterval.OneHour, "binance-rest");

        var candle = Assert.Single(candles);
        Assert.Equal("BTCUSDT", candle.Symbol);
        Assert.Equal(Market.Spot, candle.Market);
        Assert.Equal(CandleInterval.OneHour, candle.Interval);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1700000000000), candle.OpenTimeUtc);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1700003599999), candle.CloseTimeUtc);
        Assert.Equal(42000.10m, candle.Open);
        Assert.Equal(42500.00m, candle.High);
        Assert.Equal(41900.00m, candle.Low);
        Assert.Equal(42250.50m, candle.Close);
        Assert.Equal(123.456m, candle.Volume);
        Assert.Equal("binance-rest", candle.Source);
    }

    [Fact]
    public void Parse_EmptyArray_ReturnsEmpty()
    {
        var candles = BinanceKlineParser.Parse("[]", "BTCUSDT", Market.Spot, CandleInterval.OneHour, "binance-rest");
        Assert.Empty(candles);
    }
}
