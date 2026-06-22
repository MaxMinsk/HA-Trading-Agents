using Trading.Core.Decisions;
using Trading.Core.Execution;
using Trading.Core.MarketData;
using Trading.Execution;
using Trading.Risk;
using Xunit;

namespace Trading.Tests;

/// <summary>End-to-end through the safe path: decision -&gt; risk gate -&gt; filters -&gt; paper adapter.</summary>
public sealed class ExecutionServiceTests
{
    private static readonly RiskLimits Limits = RiskLimits.Create(
        maxPositionFraction: 0.25m, maxOrderNotional: 100_000m, dailyLossLimitFraction: 0.05m);

    private static ExecutionService Build(IExecutionAdapter adapter, RiskLimits? limits = null, ISymbolFilterProvider? filters = null) =>
        new(adapter, limits ?? Limits, filters ?? new StaticSymbolFilterProvider(), TimeProvider.System);

    [Fact]
    public async Task Buy_WithinLimits_PlacesAndFills()
    {
        var adapter = new PaperExecutionAdapter(10_000m);
        var service = Build(adapter);
        var decision = TradeDecision.Create(TradeAction.Buy, 0.1m, 0.9, "buy");

        var outcome = await service.ExecuteAsync("BTCUSDT", Market.Spot, decision, referencePrice: 100m);

        Assert.True(outcome.Placed);
        Assert.Equal(RiskVerdict.Approved, outcome.Verdict);
        Assert.NotNull(outcome.Result);
        Assert.Equal(OrderStatus.Filled, outcome.Result!.Status);
        Assert.Equal(10m, outcome.Result.FilledQuantity); // 0.1 * 10000 / 100
    }

    [Fact]
    public async Task Buy_WithKillSwitch_PlacesNothing()
    {
        var adapter = new PaperExecutionAdapter(10_000m);
        var service = Build(adapter, Limits.WithKillSwitch(true));
        var decision = TradeDecision.Create(TradeAction.Buy, 0.1m, 0.9, "buy");

        var outcome = await service.ExecuteAsync("BTCUSDT", Market.Spot, decision, referencePrice: 100m);

        Assert.False(outcome.Placed);
        Assert.Equal(RiskVerdict.Vetoed, outcome.Verdict);
        Assert.Null(outcome.Result);
    }

    [Fact]
    public async Task Sell_AfterBuy_ReducesHolding()
    {
        var adapter = new PaperExecutionAdapter(10_000m);
        var service = Build(adapter);
        await service.ExecuteAsync("BTCUSDT", Market.Spot, TradeDecision.Create(TradeAction.Buy, 0.2m, 0.9, "buy"), 100m);

        var outcome = await service.ExecuteAsync("BTCUSDT", Market.Spot, TradeDecision.Create(TradeAction.Sell, 1m, 0.9, "sell all"), 100m);
        var account = await adapter.GetAccountAsync();

        Assert.True(outcome.Placed);
        Assert.Equal(OrderStatus.Filled, outcome.Result!.Status);
        Assert.Equal(0m, account.FreeOf("BTC"));
    }

    [Fact]
    public async Task Buy_BelowMinNotional_IsNotPlaced()
    {
        var adapter = new PaperExecutionAdapter(10_000m);
        var filters = new StaticSymbolFilterProvider(SymbolFilters.Create(0.01m, 0.001m, 0m, 5_000m));
        var service = Build(adapter, filters: filters);
        var decision = TradeDecision.Create(TradeAction.Buy, 0.1m, 0.9, "buy"); // notional 1000 < 5000

        var outcome = await service.ExecuteAsync("BTCUSDT", Market.Spot, decision, referencePrice: 100m);

        Assert.False(outcome.Placed);
        Assert.Equal(RiskVerdict.Approved, outcome.Verdict); // risk approved, exchange filter blocked
        Assert.Contains("minimum", outcome.Reason, StringComparison.OrdinalIgnoreCase);
    }
}
