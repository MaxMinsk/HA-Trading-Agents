namespace Trading.Risk;

/// <summary>
/// The account facts the risk gate needs, evaluated at decision time: total equity, how much of it
/// is already in the position, and today's realized P&amp;L as a fraction of equity (negative = loss).
/// </summary>
public sealed record AccountRiskState
{
    private AccountRiskState(decimal equityQuote, decimal positionFraction, decimal dailyPnlFraction)
    {
        EquityQuote = equityQuote;
        PositionFraction = positionFraction;
        DailyPnlFraction = dailyPnlFraction;
    }

    /// <summary>Total account equity in the quote asset.</summary>
    public decimal EquityQuote { get; }

    /// <summary>Fraction of equity (0..1) currently held in the position.</summary>
    public decimal PositionFraction { get; }

    /// <summary>Today's realized P&amp;L as a fraction of equity; negative is a loss.</summary>
    public decimal DailyPnlFraction { get; }

    /// <summary>Creates a validated state.</summary>
    /// <param name="equityQuote">Total equity in quote asset (must be non-negative).</param>
    /// <param name="positionFraction">Fraction of equity in the position, clamped to [0, 1].</param>
    /// <param name="dailyPnlFraction">Today's realized P&amp;L fraction (default 0).</param>
    public static AccountRiskState Create(decimal equityQuote, decimal positionFraction, decimal dailyPnlFraction = 0m)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(equityQuote);
        var clamped = Math.Clamp(positionFraction, 0m, 1m);
        return new AccountRiskState(equityQuote, clamped, dailyPnlFraction);
    }
}
