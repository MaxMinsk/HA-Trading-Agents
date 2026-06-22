namespace Trading.Risk;

/// <summary>
/// The risk gate's ruling: a verdict, the size fraction actually permitted, and a reason. The
/// fraction's meaning matches the decision — for a buy it is the fraction of equity to deploy; for
/// a sell it is the fraction of the current holding to sell.
/// </summary>
public sealed record RiskAssessment
{
    private RiskAssessment(RiskVerdict verdict, decimal approvedFraction, string reason)
    {
        Verdict = verdict;
        ApprovedFraction = approvedFraction;
        Reason = reason;
    }

    /// <summary>The ruling.</summary>
    public RiskVerdict Verdict { get; }

    /// <summary>The permitted size fraction (0 when vetoed or holding flat).</summary>
    public decimal ApprovedFraction { get; }

    /// <summary>Why the gate ruled this way.</summary>
    public string Reason { get; }

    /// <summary>True when an order may be placed (approved or adjusted with a positive size).</summary>
    public bool IsActionable => Verdict != RiskVerdict.Vetoed && ApprovedFraction > 0m;

    /// <summary>An approved ruling at <paramref name="fraction"/>.</summary>
    /// <param name="fraction">Permitted fraction.</param>
    /// <param name="reason">Optional reason.</param>
    public static RiskAssessment Approved(decimal fraction, string reason = "within limits") =>
        new(RiskVerdict.Approved, fraction, reason);

    /// <summary>An adjusted (size-reduced) ruling at <paramref name="fraction"/>.</summary>
    /// <param name="fraction">Permitted fraction.</param>
    /// <param name="reason">Why it was reduced.</param>
    public static RiskAssessment Adjusted(decimal fraction, string reason) =>
        new(RiskVerdict.Adjusted, fraction, reason);

    /// <summary>A veto.</summary>
    /// <param name="reason">Why it was blocked.</param>
    public static RiskAssessment Vetoed(string reason) =>
        new(RiskVerdict.Vetoed, 0m, reason);
}
