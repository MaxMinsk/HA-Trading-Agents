namespace Trading.Core.Decisions;

/// <summary>A discrete trading action a decision can recommend.</summary>
public enum TradeAction
{
    /// <summary>Do nothing / stay flat.</summary>
    Hold,

    /// <summary>Open or increase a long position.</summary>
    Buy,

    /// <summary>Close a long, or open/increase a short (futures).</summary>
    Sell,
}
