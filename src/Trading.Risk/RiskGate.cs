using Trading.Core.Decisions;

namespace Trading.Risk;

/// <summary>
/// The deterministic gate every trade passes through before execution. It is intentionally simple
/// and stateless: given a decision, the account state, and the limits, it returns an
/// <see cref="RiskAssessment"/> the execution service must honor. The model layer can suggest
/// anything; this is what actually constrains it (ADR 0001/0002, epic TRD-003).
/// </summary>
public static class RiskGate
{
    /// <summary>Rules on a proposed trade.</summary>
    /// <param name="decision">The strategy/agent decision.</param>
    /// <param name="state">Account facts at decision time.</param>
    /// <param name="limits">The active limits.</param>
    /// <returns>An approve/adjust/veto ruling with the permitted size fraction.</returns>
    public static RiskAssessment Evaluate(TradeDecision decision, AccountRiskState state, RiskLimits limits)
    {
        ArgumentNullException.ThrowIfNull(decision);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(limits);

        return limits.KillSwitch
            ? RiskAssessment.Vetoed("kill switch engaged")
            : decision.Action switch
            {
                TradeAction.Hold => RiskAssessment.Approved(0m, "hold"),
                TradeAction.Buy => EvaluateBuy(decision.SizeFraction, state, limits),
                _ => EvaluateSell(decision.SizeFraction, state, limits),
            };
    }

    private static RiskAssessment EvaluateBuy(decimal requested, AccountRiskState state, RiskLimits limits)
    {
        // A buy adds risk, so the daily-loss stop blocks it (sells, which de-risk, are still allowed).
        if (limits.DailyLossLimitFraction > 0m && state.DailyPnlFraction <= -limits.DailyLossLimitFraction)
        {
            return RiskAssessment.Vetoed("daily loss limit reached");
        }

        var headroom = limits.MaxPositionFraction - state.PositionFraction;
        if (headroom <= 0m)
        {
            return RiskAssessment.Vetoed("position already at max");
        }

        var capped = Math.Min(requested, Math.Min(headroom, limits.MaxPositionFraction));

        // Per-order notional cap (only meaningful with positive equity).
        if (state.EquityQuote > 0m && capped * state.EquityQuote > limits.MaxOrderNotional)
        {
            capped = limits.MaxOrderNotional / state.EquityQuote;
        }

        return capped <= 0m
            ? RiskAssessment.Vetoed("order below allowable size")
            : capped < requested
                ? RiskAssessment.Adjusted(capped, "clamped to position/notional limits")
                : RiskAssessment.Approved(capped);
    }

    private static RiskAssessment EvaluateSell(decimal requested, AccountRiskState state, RiskLimits limits)
    {
        if (!limits.AllowShorting && state.PositionFraction <= 0m)
        {
            return RiskAssessment.Vetoed("nothing to sell (shorting disabled)");
        }

        // Sell fraction is of the current holding, so it cannot exceed 1.
        var capped = Math.Min(requested, 1m);
        return capped <= 0m
            ? RiskAssessment.Vetoed("order below allowable size")
            : capped < requested
                ? RiskAssessment.Adjusted(capped, "clamped to current holding")
                : RiskAssessment.Approved(capped);
    }
}
