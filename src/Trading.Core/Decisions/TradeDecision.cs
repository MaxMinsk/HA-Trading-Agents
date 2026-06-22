using System.Collections.ObjectModel;

namespace Trading.Core.Decisions;

/// <summary>
/// The structured output every strategy (rule-based, single-agent, or multi-agent) produces for
/// one decision. The LLM layer fills this via structured output; the deterministic risk layer then
/// validates and may veto it (ADR 0001/0002). Construct via <see cref="Create"/> so a malformed
/// model output fails closed rather than silently producing a bad order.
/// </summary>
public sealed record TradeDecision
{
    private TradeDecision(TradeAction action, decimal sizeFraction, double confidence, string rationale, IReadOnlyList<string> keyRisks)
    {
        Action = action;
        SizeFraction = sizeFraction;
        Confidence = confidence;
        Rationale = rationale;
        KeyRisks = keyRisks;
    }

    /// <summary>The recommended action.</summary>
    public TradeAction Action { get; }

    /// <summary>Fraction of capital to allocate, in [0, 1]. Always 0 for <see cref="TradeAction.Hold"/>.</summary>
    public decimal SizeFraction { get; }

    /// <summary>Model confidence in [0, 1].</summary>
    public double Confidence { get; }

    /// <summary>Short human-readable justification.</summary>
    public string Rationale { get; }

    /// <summary>The main risks the decision acknowledges.</summary>
    public IReadOnlyList<string> KeyRisks { get; }

    /// <summary>Creates a validated decision; ranges and the Hold/size invariant are enforced.</summary>
    /// <param name="action">The recommended action.</param>
    /// <param name="sizeFraction">Fraction of capital in [0, 1] (must be 0 when <paramref name="action"/> is Hold).</param>
    /// <param name="confidence">Confidence in [0, 1].</param>
    /// <param name="rationale">Non-empty justification.</param>
    /// <param name="keyRisks">Optional acknowledged risks.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="sizeFraction"/> or <paramref name="confidence"/> out of range.</exception>
    /// <exception cref="ArgumentException">A Hold with non-zero size, or an empty rationale.</exception>
    public static TradeDecision Create(
        TradeAction action,
        decimal sizeFraction,
        double confidence,
        string rationale,
        IEnumerable<string>? keyRisks = null)
    {
        if (sizeFraction is < 0m or > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeFraction), sizeFraction, "Size fraction must be in [0, 1].");
        }

        if (confidence is < 0d or > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(confidence), confidence, "Confidence must be in [0, 1].");
        }

        if (action == TradeAction.Hold && sizeFraction != 0m)
        {
            throw new ArgumentException("A Hold decision must have size fraction 0.", nameof(sizeFraction));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(rationale);

        var risks = new ReadOnlyCollection<string>((keyRisks ?? []).ToList());
        return new TradeDecision(action, sizeFraction, confidence, rationale, risks);
    }
}
