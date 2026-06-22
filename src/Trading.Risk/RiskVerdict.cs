namespace Trading.Risk;

/// <summary>How the risk gate ruled on a proposed trade.</summary>
public enum RiskVerdict
{
    /// <summary>Allowed as requested.</summary>
    Approved,

    /// <summary>Allowed, but the size was reduced to fit the limits.</summary>
    Adjusted,

    /// <summary>Blocked entirely; no order may be placed.</summary>
    Vetoed,
}
