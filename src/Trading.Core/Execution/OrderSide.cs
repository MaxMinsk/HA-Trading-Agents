namespace Trading.Core.Execution;

/// <summary>Order side: buying or selling the base asset.</summary>
public enum OrderSide
{
    /// <summary>Buy the base asset (spend quote).</summary>
    Buy,

    /// <summary>Sell the base asset (receive quote).</summary>
    Sell,
}
