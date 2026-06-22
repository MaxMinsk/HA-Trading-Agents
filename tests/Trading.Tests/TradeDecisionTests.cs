using Trading.Core.Decisions;
using Xunit;

namespace Trading.Tests;

/// <summary>Tests that malformed decisions fail closed (range + Hold/size invariants).</summary>
public sealed class TradeDecisionTests
{
    [Fact]
    public void Create_ValidBuy_Succeeds()
    {
        var decision = TradeDecision.Create(TradeAction.Buy, 0.1m, 0.6, "trend up", ["funding flip", "thin liquidity"]);

        Assert.Equal(TradeAction.Buy, decision.Action);
        Assert.Equal(0.1m, decision.SizeFraction);
        Assert.Equal(2, decision.KeyRisks.Count);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void Create_SizeOutOfRange_Throws(double size)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TradeDecision.Create(TradeAction.Buy, (decimal)size, 0.5, "x"));
    }

    [Fact]
    public void Create_HoldWithNonZeroSize_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            TradeDecision.Create(TradeAction.Hold, 0.2m, 0.5, "x"));
    }

    [Fact]
    public void Create_EmptyRationale_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            TradeDecision.Create(TradeAction.Hold, 0m, 0.5, "   "));
    }
}
