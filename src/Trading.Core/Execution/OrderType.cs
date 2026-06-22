namespace Trading.Core.Execution;

/// <summary>Supported order types. Kept minimal on purpose (spot, testnet-first).</summary>
public enum OrderType
{
    /// <summary>Fill immediately at the best available price.</summary>
    Market,

    /// <summary>Rest on the book at a fixed price.</summary>
    Limit,
}
