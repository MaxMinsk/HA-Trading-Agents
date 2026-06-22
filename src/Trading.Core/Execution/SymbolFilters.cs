namespace Trading.Core.Execution;

/// <summary>
/// The Binance exchange filters that make an order acceptable: price must be a multiple of
/// <see cref="TickSize"/>, quantity a multiple of <see cref="StepSize"/> and at least
/// <see cref="MinQuantity"/>, and the order notional at least <see cref="MinNotional"/>.
/// Rounding floors toward zero so a rounded order never exceeds the requested size.
/// </summary>
public sealed record SymbolFilters
{
    private SymbolFilters(decimal tickSize, decimal stepSize, decimal minQuantity, decimal minNotional)
    {
        TickSize = tickSize;
        StepSize = stepSize;
        MinQuantity = minQuantity;
        MinNotional = minNotional;
    }

    /// <summary>Smallest price increment (0 = no constraint).</summary>
    public decimal TickSize { get; }

    /// <summary>Smallest quantity increment (0 = no constraint).</summary>
    public decimal StepSize { get; }

    /// <summary>Minimum order quantity.</summary>
    public decimal MinQuantity { get; }

    /// <summary>Minimum order notional (price * quantity), in the quote asset.</summary>
    public decimal MinNotional { get; }

    /// <summary>A permissive default used for paper trading / unknown symbols.</summary>
    public static SymbolFilters Permissive { get; } = new(0.01m, 0.00000001m, 0m, 0m);

    /// <summary>Creates a validated set of filters (all values must be non-negative).</summary>
    /// <param name="tickSize">Price increment.</param>
    /// <param name="stepSize">Quantity increment.</param>
    /// <param name="minQuantity">Minimum quantity.</param>
    /// <param name="minNotional">Minimum notional.</param>
    public static SymbolFilters Create(decimal tickSize, decimal stepSize, decimal minQuantity, decimal minNotional)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(tickSize);
        ArgumentOutOfRangeException.ThrowIfNegative(stepSize);
        ArgumentOutOfRangeException.ThrowIfNegative(minQuantity);
        ArgumentOutOfRangeException.ThrowIfNegative(minNotional);
        return new SymbolFilters(tickSize, stepSize, minQuantity, minNotional);
    }

    /// <summary>Floors <paramref name="price"/> to the nearest <see cref="TickSize"/> multiple.</summary>
    /// <param name="price">Raw price.</param>
    public decimal RoundPriceToTick(decimal price) => FloorToIncrement(price, TickSize);

    /// <summary>Floors <paramref name="quantity"/> to the nearest <see cref="StepSize"/> multiple.</summary>
    /// <param name="quantity">Raw quantity.</param>
    public decimal RoundQuantityToStep(decimal quantity) => FloorToIncrement(quantity, StepSize);

    /// <summary>True if the order clears both the minimum quantity and the minimum notional.</summary>
    /// <param name="price">Fill/reference price.</param>
    /// <param name="quantity">Order quantity (already step-rounded).</param>
    public bool IsTradeable(decimal price, decimal quantity) =>
        quantity > 0m && quantity >= MinQuantity && price * quantity >= MinNotional;

    private static decimal FloorToIncrement(decimal value, decimal increment) =>
        increment <= 0m ? value : Math.Floor(value / increment) * increment;
}
