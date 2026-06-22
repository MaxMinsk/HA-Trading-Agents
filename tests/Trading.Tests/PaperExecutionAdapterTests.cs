using Trading.Core.Execution;
using Trading.Core.MarketData;
using Trading.Execution;
using Xunit;

namespace Trading.Tests;

/// <summary>The paper adapter must keep a coherent in-memory ledger so the execution path is testable.</summary>
public sealed class PaperExecutionAdapterTests
{
    [Fact]
    public async Task Buy_DecrementsQuoteAndIncrementsBase()
    {
        var adapter = new PaperExecutionAdapter(10_000m);
        var intent = OrderIntent.MarketOrder("BTCUSDT", Market.Spot, OrderSide.Buy, 2m, 100m, ClientOrderId.Create("t1"));

        var result = await adapter.SubmitAsync(intent);
        var account = await adapter.GetAccountAsync();

        Assert.Equal(OrderStatus.Filled, result.Status);
        Assert.Equal(2m, result.FilledQuantity);
        Assert.Equal(2m, account.FreeOf("BTC"));
        Assert.True(account.FreeOf("USDT") < 10_000m);
    }

    [Fact]
    public async Task SellAfterBuy_ReturnsBaseToZeroAndQuoteNetOfFees()
    {
        var adapter = new PaperExecutionAdapter(10_000m);
        await adapter.SubmitAsync(OrderIntent.MarketOrder("BTCUSDT", Market.Spot, OrderSide.Buy, 2m, 100m, ClientOrderId.Create("b")));

        var sell = await adapter.SubmitAsync(OrderIntent.MarketOrder("BTCUSDT", Market.Spot, OrderSide.Sell, 2m, 100m, ClientOrderId.Create("s")));
        var account = await adapter.GetAccountAsync();

        Assert.Equal(OrderStatus.Filled, sell.Status);
        Assert.Equal(0m, account.FreeOf("BTC"));
        Assert.InRange(account.FreeOf("USDT"), 9_900m, 10_000m); // round-trip costs fees + slippage
    }

    [Fact]
    public async Task Buy_InsufficientQuote_IsRejected()
    {
        var adapter = new PaperExecutionAdapter(100m);
        var intent = OrderIntent.MarketOrder("BTCUSDT", Market.Spot, OrderSide.Buy, 2m, 100m, ClientOrderId.Create("t1"));

        var result = await adapter.SubmitAsync(intent);

        Assert.Equal(OrderStatus.Rejected, result.Status);
        Assert.Contains("insufficient", result.Note ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetAccount_ReflectsStartingQuote()
    {
        var adapter = new PaperExecutionAdapter(7_500m);

        var account = await adapter.GetAccountAsync();

        Assert.Equal(7_500m, account.FreeOf("USDT"));
    }
}
