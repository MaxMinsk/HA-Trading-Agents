using Trading.Backtest.Execution;
using Xunit;

namespace Trading.Tests;

/// <summary>Tests for fee + slippage application and all-in fill economics.</summary>
public sealed class FeeModelTests
{
    [Fact]
    public void BuyAndSellPrice_ApplySlippage()
    {
        var fees = new FeeModel(0m, 50m); // 0.50% slippage
        Assert.Equal(100.5m, fees.BuyPrice(100m));
        Assert.Equal(99.5m, fees.SellPrice(100m));
    }

    [Fact]
    public void Fee_IsNotionalTimesRate()
    {
        var fees = new FeeModel(10m, 0m); // 0.10%
        Assert.Equal(1m, fees.Fee(1000m));
    }

    [Fact]
    public void UnitsForCash_BuyCost_RoundTripsToCash()
    {
        var fees = new FeeModel(10m, 5m);
        var units = fees.UnitsForCash(10_000m, 200m);
        Assert.Equal(10_000m, Math.Round(fees.BuyCost(units, 200m), 6));
    }

    [Fact]
    public void NegativeFee_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new FeeModel(-1m, 0m));
}
