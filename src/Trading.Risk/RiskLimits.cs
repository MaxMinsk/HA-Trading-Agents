namespace Trading.Risk;

/// <summary>
/// The hard limits the risk gate enforces. These are the only thing standing between a model's
/// suggested trade and the exchange, so they are deterministic and conservative by default
/// (long-only spot, a quarter of equity per position, a 5% daily loss stop).
/// </summary>
public sealed record RiskLimits
{
    private RiskLimits(
        decimal maxPositionFraction,
        decimal maxOrderNotional,
        decimal dailyLossLimitFraction,
        bool allowShorting,
        bool killSwitch)
    {
        MaxPositionFraction = maxPositionFraction;
        MaxOrderNotional = maxOrderNotional;
        DailyLossLimitFraction = dailyLossLimitFraction;
        AllowShorting = allowShorting;
        KillSwitch = killSwitch;
    }

    /// <summary>Maximum fraction of equity (0..1) that may be held in a single position.</summary>
    public decimal MaxPositionFraction { get; }

    /// <summary>Maximum notional (quote asset) for a single order.</summary>
    public decimal MaxOrderNotional { get; }

    /// <summary>Daily realized-loss stop as a fraction of equity (0..1); buys are vetoed once breached.</summary>
    public decimal DailyLossLimitFraction { get; }

    /// <summary>Whether selling more than the current holding (going short) is allowed.</summary>
    public bool AllowShorting { get; }

    /// <summary>Global kill switch: when true every order is vetoed.</summary>
    public bool KillSwitch { get; }

    /// <summary>Conservative defaults for spot trading.</summary>
    public static RiskLimits Default { get; } = new(
        maxPositionFraction: 0.25m,
        maxOrderNotional: 1_000m,
        dailyLossLimitFraction: 0.05m,
        allowShorting: false,
        killSwitch: false);

    /// <summary>Creates validated limits.</summary>
    /// <param name="maxPositionFraction">Max position fraction in [0, 1].</param>
    /// <param name="maxOrderNotional">Max single-order notional (&gt; 0).</param>
    /// <param name="dailyLossLimitFraction">Daily loss stop in [0, 1].</param>
    /// <param name="allowShorting">Allow short selling.</param>
    /// <param name="killSwitch">Engage the global kill switch.</param>
    public static RiskLimits Create(
        decimal maxPositionFraction,
        decimal maxOrderNotional,
        decimal dailyLossLimitFraction,
        bool allowShorting = false,
        bool killSwitch = false)
    {
        if (maxPositionFraction is < 0m or > 1m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxPositionFraction), maxPositionFraction, "Max position fraction must be in [0, 1].");
        }

        if (dailyLossLimitFraction is < 0m or > 1m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dailyLossLimitFraction), dailyLossLimitFraction, "Daily loss limit must be in [0, 1].");
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxOrderNotional);
        return new RiskLimits(maxPositionFraction, maxOrderNotional, dailyLossLimitFraction, allowShorting, killSwitch);
    }

    /// <summary>Returns a copy with the kill switch set to <paramref name="engaged"/>.</summary>
    /// <param name="engaged">Whether the kill switch is engaged.</param>
    public RiskLimits WithKillSwitch(bool engaged) =>
        new(MaxPositionFraction, MaxOrderNotional, DailyLossLimitFraction, AllowShorting, engaged);
}
