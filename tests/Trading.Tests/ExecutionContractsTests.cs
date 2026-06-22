using Trading.Core.Execution;
using Trading.Core.MarketData;
using Xunit;

namespace Trading.Tests;

/// <summary>Order contracts: validation, client-order-id sanitization, and symbol splitting.</summary>
public sealed class ExecutionContractsTests
{
    [Fact]
    public void MarketOrder_ValidInputs_Constructs()
    {
        var intent = OrderIntent.MarketOrder("BTCUSDT", Market.Spot, OrderSide.Buy, 1.5m, 100m, "abc");

        Assert.Equal(OrderType.Market, intent.Type);
        Assert.Null(intent.Price);
        Assert.Equal(1.5m, intent.Quantity);
    }

    [Fact]
    public void MarketOrder_NonPositiveQuantity_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => OrderIntent.MarketOrder("BTCUSDT", Market.Spot, OrderSide.Buy, 0m, 100m, "abc"));
    }

    [Fact]
    public void LimitOrder_NonPositivePrice_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => OrderIntent.LimitOrder("BTCUSDT", Market.Spot, OrderSide.Buy, 1m, 0m, 100m, "abc"));
    }

    [Fact]
    public void ClientOrderId_StripsDisallowedCharsAndCaps()
    {
        Assert.Equal("abcdef", ClientOrderId.Create("ab/cd ef!"));

        var longId = ClientOrderId.Create(new string('x', 50));
        Assert.Equal(36, longId.Length);
    }

    [Fact]
    public void ClientOrderId_For_IsDeterministicForSameInputs()
    {
        var a = ClientOrderId.For("BTCUSDT", OrderSide.Buy, 1_700_000_000_000L, "abc123");
        var b = ClientOrderId.For("BTCUSDT", OrderSide.Buy, 1_700_000_000_000L, "abc123");

        Assert.Equal(a, b);
        Assert.True(a.Length <= 36);
    }

    [Theory]
    [InlineData("BTCUSDT", "BTC", "USDT")]
    [InlineData("ETHBTC", "ETH", "BTC")]
    [InlineData("SOLFDUSD", "SOL", "FDUSD")]
    public void SymbolAssets_SplitsByKnownQuote(string symbol, string expectedBase, string expectedQuote)
    {
        var (baseAsset, quoteAsset) = SymbolAssets.Split(symbol);

        Assert.Equal(expectedBase, baseAsset);
        Assert.Equal(expectedQuote, quoteAsset);
    }
}
