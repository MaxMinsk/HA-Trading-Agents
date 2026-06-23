using Trading.Core.Decisions;

namespace Trading.Api;

/// <summary>Wire shape of a trade decision sent to the UI (action as a string).</summary>
public sealed record DecisionDto(
    string Action,
    decimal SizeFraction,
    double Confidence,
    string Rationale,
    IReadOnlyList<string> KeyRisks)
{
    /// <summary>Maps a domain decision to its wire DTO.</summary>
    public static DecisionDto From(TradeDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);
        return new DecisionDto(
            decision.Action.ToString(),
            decision.SizeFraction,
            decision.Confidence,
            decision.Rationale,
            decision.KeyRisks);
    }
}
