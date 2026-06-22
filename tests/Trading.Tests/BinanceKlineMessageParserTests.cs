using Trading.Core.MarketData;
using Trading.Data.Binance;
using Xunit;

namespace Trading.Tests;

/// <summary>Tests for the live kline WebSocket message parser (closed-only).</summary>
public sealed class BinanceKlineMessageParserTests
{
    private const string ClosedKline =
        """{"e":"kline","E":1700003599999,"s":"BTCUSDT","k":{"t":1700000000000,"T":1700003599999,"s":"BTCUSDT","i":"1h","o":"42000.10","c":"42250.50","h":"42500.00","l":"41900.00","v":"123.456","n":1500,"x":true,"q":"0","V":"0","Q":"0","B":"0"}}""";

    private const string OpenKline =
        """{"e":"kline","E":1700001000000,"s":"BTCUSDT","k":{"t":1700000000000,"T":1700003599999,"s":"BTCUSDT","i":"1h","o":"42000.10","c":"42100.00","h":"42200.00","l":"41950.00","v":"50.0","n":700,"x":false,"q":"0","V":"0","Q":"0","B":"0"}}""";

    private const string CombinedClosed =
        """{"stream":"btcusdt@kline_1h","data":{"e":"kline","s":"BTCUSDT","k":{"t":1700000000000,"T":1700003599999,"s":"BTCUSDT","i":"1h","o":"42000.10","c":"42250.50","h":"42500.00","l":"41900.00","v":"123.456","x":true}}}""";

    [Fact]
    public void TryParse_ClosedKline_MapsCandle()
    {
        var ok = BinanceKlineMessageParser.TryParseClosedCandle(ClosedKline, Market.Spot, out var candle);

        Assert.True(ok);
        Assert.NotNull(candle);
        Assert.Equal("BTCUSDT", candle!.Symbol);
        Assert.Equal(CandleInterval.OneHour, candle.Interval);
        Assert.Equal(42250.50m, candle.Close);
        Assert.Equal("binance-ws", candle.Source);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1700003599999), candle.CloseTimeUtc);
    }

    [Fact]
    public void TryParse_OpenKline_ReturnsFalse()
    {
        Assert.False(BinanceKlineMessageParser.TryParseClosedCandle(OpenKline, Market.Spot, out var candle));
        Assert.Null(candle);
    }

    [Fact]
    public void TryParse_CombinedStreamWrapper_Unwrapped()
    {
        Assert.True(BinanceKlineMessageParser.TryParseClosedCandle(CombinedClosed, Market.Spot, out var candle));
        Assert.NotNull(candle);
        Assert.Equal(CandleInterval.OneHour, candle!.Interval);
    }

    [Fact]
    public void TryParse_NonKlineMessage_ReturnsFalse() =>
        Assert.False(BinanceKlineMessageParser.TryParseClosedCandle("""{"result":null,"id":1}""", Market.Spot, out _));
}
