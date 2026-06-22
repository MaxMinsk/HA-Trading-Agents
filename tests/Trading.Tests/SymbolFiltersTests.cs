using Trading.Core.Execution;
using Xunit;

namespace Trading.Tests;

/// <summary>Exchange-filter rounding and minimums — the difference between an accepted and a rejected order.</summary>
public sealed class SymbolFiltersTests
{
    [Fact]
    public void RoundPriceToTick_FloorsToTick()
    {
        var filters = SymbolFilters.Create(tickSize: 0.01m, stepSize: 0.001m, minQuantity: 0m, minNotional: 0m);

        Assert.Equal(123.45m, filters.RoundPriceToTick(123.4567m));
    }

    [Fact]
    public void RoundQuantityToStep_FloorsToStep()
    {
        var filters = SymbolFilters.Create(tickSize: 0.01m, stepSize: 0.001m, minQuantity: 0m, minNotional: 0m);

        Assert.Equal(10.123m, filters.RoundQuantityToStep(10.12399m));
    }

    [Fact]
    public void IsTradeable_FalseBelowMinNotional()
    {
        var filters = SymbolFilters.Create(tickSize: 0.01m, stepSize: 0.001m, minQuantity: 0m, minNotional: 10m);

        Assert.False(filters.IsTradeable(price: 100m, quantity: 0.05m)); // notional 5 < 10
        Assert.True(filters.IsTradeable(price: 100m, quantity: 0.2m));   // notional 20 >= 10
    }

    [Fact]
    public void IsTradeable_FalseBelowMinQuantity()
    {
        var filters = SymbolFilters.Create(tickSize: 0.01m, stepSize: 0.001m, minQuantity: 0.01m, minNotional: 0m);

        Assert.False(filters.IsTradeable(price: 100m, quantity: 0.005m));
    }

    [Fact]
    public void ZeroIncrement_LeavesValueUnchanged()
    {
        var filters = SymbolFilters.Create(tickSize: 0m, stepSize: 0m, minQuantity: 0m, minNotional: 0m);

        Assert.Equal(123.4567m, filters.RoundPriceToTick(123.4567m));
        Assert.Equal(10.12399m, filters.RoundQuantityToStep(10.12399m));
    }
}
