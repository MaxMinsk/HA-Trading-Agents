namespace Trading.Core.Execution;

/// <summary>A single asset balance: freely usable plus locked in open orders.</summary>
public sealed record Balance
{
    /// <summary>Asset code, e.g. <c>USDT</c> or <c>BTC</c>.</summary>
    public required string Asset { get; init; }

    /// <summary>Amount available to trade.</summary>
    public required decimal Free { get; init; }

    /// <summary>Amount reserved by open orders.</summary>
    public decimal Locked { get; init; }
}
