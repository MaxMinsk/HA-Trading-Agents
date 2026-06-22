namespace Trading.Backtest.Execution;

/// <summary>
/// Applies taker fee and slippage (both in basis points) to fills. Slippage moves the fill price
/// against you; the fee is charged on the traded notional. Also owns the all-in fill economics
/// (<see cref="UnitsForCash"/> / <see cref="BuyCost"/> / <see cref="SellProceeds"/>).
/// </summary>
public sealed class FeeModel
{
    private readonly decimal _feeRate;
    private readonly decimal _slippageRate;

    /// <summary>Creates a fee model.</summary>
    /// <param name="feeBps">Taker fee in basis points (e.g. 10 = 0.10%).</param>
    /// <param name="slippageBps">Slippage in basis points applied to the fill price.</param>
    public FeeModel(decimal feeBps, decimal slippageBps)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(feeBps);
        ArgumentOutOfRangeException.ThrowIfNegative(slippageBps);
        _feeRate = feeBps / 10_000m;
        _slippageRate = slippageBps / 10_000m;
    }

    /// <summary>Effective buy price (slippage pushes it up).</summary>
    /// <param name="price">Reference (close) price.</param>
    public decimal BuyPrice(decimal price) => price * (1m + _slippageRate);

    /// <summary>Effective sell price (slippage pushes it down).</summary>
    /// <param name="price">Reference (close) price.</param>
    public decimal SellPrice(decimal price) => price * (1m - _slippageRate);

    /// <summary>Fee charged on a traded notional.</summary>
    /// <param name="notional">Absolute traded value.</param>
    public decimal Fee(decimal notional) => Math.Abs(notional) * _feeRate;

    /// <summary>Units obtainable by spending all of <paramref name="cash"/> at <paramref name="price"/> (incl. fee + slippage).</summary>
    /// <param name="cash">Cash to spend.</param>
    /// <param name="price">Reference price.</param>
    public decimal UnitsForCash(decimal cash, decimal price) => cash / (BuyPrice(price) * (1m + _feeRate));

    /// <summary>Cash required to acquire <paramref name="units"/> at <paramref name="price"/> (incl. fee + slippage).</summary>
    /// <param name="units">Units to buy.</param>
    /// <param name="price">Reference price.</param>
    public decimal BuyCost(decimal units, decimal price) => units * BuyPrice(price) * (1m + _feeRate);

    /// <summary>Net proceeds from selling <paramref name="units"/> at <paramref name="price"/> (incl. fee + slippage).</summary>
    /// <param name="units">Units to sell.</param>
    /// <param name="price">Reference price.</param>
    public decimal SellProceeds(decimal units, decimal price) => units * SellPrice(price) * (1m - _feeRate);
}
