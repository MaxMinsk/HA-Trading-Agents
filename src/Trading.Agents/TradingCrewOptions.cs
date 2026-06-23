namespace Trading.Agents;

/// <summary>Tunables for the crew: the SMA windows the brief uses and whether the risk reviewer runs.</summary>
public sealed record TradingCrewOptions
{
    /// <summary>Fast SMA period for the brief.</summary>
    public int SmaFast { get; init; } = 20;

    /// <summary>Slow SMA period for the brief.</summary>
    public int SmaSlow { get; init; } = 50;

    /// <summary>Whether the advisory risk reviewer can downgrade a non-hold decision to Hold.</summary>
    public bool UseRiskReviewer { get; init; } = true;

    /// <summary>Default options.</summary>
    public static TradingCrewOptions Default { get; } = new();
}
