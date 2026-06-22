using Trading.Core.Decisions;
using Trading.Risk;
using Xunit;

namespace Trading.Tests;

/// <summary>The deterministic risk gate is the only thing constraining the model, so its rules are pinned here.</summary>
public sealed class RiskGateTests
{
    private static readonly RiskLimits Limits = RiskLimits.Create(
        maxPositionFraction: 0.25m, maxOrderNotional: 100_000m, dailyLossLimitFraction: 0.05m);

    [Fact]
    public void Hold_IsApprovedWithZeroSize()
    {
        var decision = TradeDecision.Create(TradeAction.Hold, 0m, 0.5, "flat");
        var state = AccountRiskState.Create(10_000m, 0m);

        var assessment = RiskGate.Evaluate(decision, state, Limits);

        Assert.Equal(RiskVerdict.Approved, assessment.Verdict);
        Assert.Equal(0m, assessment.ApprovedFraction);
    }

    [Fact]
    public void KillSwitch_VetoesEverything()
    {
        var decision = TradeDecision.Create(TradeAction.Buy, 0.1m, 0.9, "buy");
        var state = AccountRiskState.Create(10_000m, 0m);

        var assessment = RiskGate.Evaluate(decision, state, Limits.WithKillSwitch(true));

        Assert.Equal(RiskVerdict.Vetoed, assessment.Verdict);
        Assert.False(assessment.IsActionable);
    }

    [Fact]
    public void Buy_AboveMaxPosition_IsClampedToMax()
    {
        var decision = TradeDecision.Create(TradeAction.Buy, 0.5m, 0.9, "big buy");
        var state = AccountRiskState.Create(10_000m, 0m);

        var assessment = RiskGate.Evaluate(decision, state, Limits);

        Assert.Equal(RiskVerdict.Adjusted, assessment.Verdict);
        Assert.Equal(0.25m, assessment.ApprovedFraction);
    }

    [Fact]
    public void Buy_WhenPositionAlreadyAtMax_IsVetoed()
    {
        var decision = TradeDecision.Create(TradeAction.Buy, 0.1m, 0.9, "buy");
        var state = AccountRiskState.Create(10_000m, 0.25m);

        var assessment = RiskGate.Evaluate(decision, state, Limits);

        Assert.Equal(RiskVerdict.Vetoed, assessment.Verdict);
    }

    [Fact]
    public void Buy_AfterDailyLossLimit_IsVetoed()
    {
        var decision = TradeDecision.Create(TradeAction.Buy, 0.1m, 0.9, "buy");
        var state = AccountRiskState.Create(10_000m, 0m, dailyPnlFraction: -0.06m);

        var assessment = RiskGate.Evaluate(decision, state, Limits);

        Assert.Equal(RiskVerdict.Vetoed, assessment.Verdict);
        Assert.Contains("daily loss", assessment.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Buy_AboveOrderNotional_IsClampedByNotional()
    {
        var limits = RiskLimits.Create(maxPositionFraction: 0.5m, maxOrderNotional: 1_000m, dailyLossLimitFraction: 0.05m);
        var decision = TradeDecision.Create(TradeAction.Buy, 0.5m, 0.9, "buy");
        var state = AccountRiskState.Create(10_000m, 0m);

        var assessment = RiskGate.Evaluate(decision, state, limits);

        Assert.Equal(RiskVerdict.Adjusted, assessment.Verdict);
        Assert.Equal(0.1m, assessment.ApprovedFraction);
    }

    [Fact]
    public void Sell_WhenFlat_IsVetoed()
    {
        var decision = TradeDecision.Create(TradeAction.Sell, 0.5m, 0.9, "sell");
        var state = AccountRiskState.Create(10_000m, 0m);

        var assessment = RiskGate.Evaluate(decision, state, Limits);

        Assert.Equal(RiskVerdict.Vetoed, assessment.Verdict);
    }

    [Fact]
    public void Sell_WhenHolding_IsApproved()
    {
        var decision = TradeDecision.Create(TradeAction.Sell, 0.5m, 0.9, "sell");
        var state = AccountRiskState.Create(10_000m, 0.6m);

        var assessment = RiskGate.Evaluate(decision, state, Limits);

        Assert.Equal(RiskVerdict.Approved, assessment.Verdict);
        Assert.Equal(0.5m, assessment.ApprovedFraction);
    }

    [Fact]
    public void Sell_AboveFullHolding_IsClampedToOne()
    {
        var decision = TradeDecision.Create(TradeAction.Sell, 1m, 0.9, "sell all");
        var state = AccountRiskState.Create(10_000m, 0.5m);
        // Request more than the holding via a fraction > 1 is impossible through TradeDecision (capped at 1),
        // so verify the boundary: a full-holding sell is approved as-is.
        var assessment = RiskGate.Evaluate(decision, state, Limits);

        Assert.Equal(RiskVerdict.Approved, assessment.Verdict);
        Assert.Equal(1m, assessment.ApprovedFraction);
    }
}
