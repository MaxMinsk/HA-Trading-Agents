namespace Trading.Core.Execution;

/// <summary>Lifecycle status of an order as reported by an execution adapter.</summary>
public enum OrderStatus
{
    /// <summary>Accepted by the venue but not yet filled.</summary>
    New,

    /// <summary>Partially filled.</summary>
    PartiallyFilled,

    /// <summary>Completely filled.</summary>
    Filled,

    /// <summary>Canceled before completion.</summary>
    Canceled,

    /// <summary>Rejected by the adapter or venue (e.g. insufficient funds, filter violation).</summary>
    Rejected,
}
