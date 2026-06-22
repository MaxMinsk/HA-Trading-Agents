using Trading.Core.MarketData;

namespace Trading.Core.Execution;

/// <summary>
/// A fully-specified, validated order ready for an <see cref="IExecutionAdapter"/>. Produced by the
/// execution service after the risk gate and exchange filters have run — adapters trust it and do
/// not re-check sizing. <see cref="ReferencePrice"/> is the price known at decision time; market
/// orders fill against it on paper, limit orders use <see cref="Price"/>.
/// </summary>
public sealed record OrderIntent
{
    private OrderIntent(
        string symbol,
        Market market,
        OrderSide side,
        OrderType type,
        decimal quantity,
        decimal referencePrice,
        decimal? price,
        string clientOrderId)
    {
        Symbol = symbol;
        Market = market;
        Side = side;
        Type = type;
        Quantity = quantity;
        ReferencePrice = referencePrice;
        Price = price;
        ClientOrderId = clientOrderId;
    }

    /// <summary>Exchange symbol, e.g. <c>BTCUSDT</c>.</summary>
    public string Symbol { get; }

    /// <summary>The market this order targets.</summary>
    public Market Market { get; }

    /// <summary>Buy or sell.</summary>
    public OrderSide Side { get; }

    /// <summary>Market or limit.</summary>
    public OrderType Type { get; }

    /// <summary>Order quantity in base-asset units.</summary>
    public decimal Quantity { get; }

    /// <summary>Reference price known at decision time (used for paper fills and notional checks).</summary>
    public decimal ReferencePrice { get; }

    /// <summary>Limit price; null for market orders.</summary>
    public decimal? Price { get; }

    /// <summary>Idempotent client order id (see <see cref="Execution.ClientOrderId"/>).</summary>
    public string ClientOrderId { get; }

    /// <summary>Creates a validated market order.</summary>
    /// <param name="symbol">Exchange symbol.</param>
    /// <param name="market">Target market.</param>
    /// <param name="side">Buy or sell.</param>
    /// <param name="quantity">Quantity in base units (must be positive).</param>
    /// <param name="referencePrice">Price known at decision time (must be positive).</param>
    /// <param name="clientOrderId">Idempotent client order id.</param>
    public static OrderIntent MarketOrder(
        string symbol,
        Market market,
        OrderSide side,
        decimal quantity,
        decimal referencePrice,
        string clientOrderId) =>
        Create(symbol, market, side, OrderType.Market, quantity, referencePrice, price: null, clientOrderId);

    /// <summary>Creates a validated limit order.</summary>
    /// <param name="symbol">Exchange symbol.</param>
    /// <param name="market">Target market.</param>
    /// <param name="side">Buy or sell.</param>
    /// <param name="quantity">Quantity in base units (must be positive).</param>
    /// <param name="price">Limit price (must be positive).</param>
    /// <param name="referencePrice">Price known at decision time (must be positive).</param>
    /// <param name="clientOrderId">Idempotent client order id.</param>
    public static OrderIntent LimitOrder(
        string symbol,
        Market market,
        OrderSide side,
        decimal quantity,
        decimal price,
        decimal referencePrice,
        string clientOrderId) =>
        Create(symbol, market, side, OrderType.Limit, quantity, referencePrice, price, clientOrderId);

    private static OrderIntent Create(
        string symbol,
        Market market,
        OrderSide side,
        OrderType type,
        decimal quantity,
        decimal referencePrice,
        decimal? price,
        string clientOrderId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientOrderId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(referencePrice);

        if (type == OrderType.Limit)
        {
            if (price is not > 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(price), price, "A limit order requires a positive price.");
            }
        }
        else if (price is not null)
        {
            throw new ArgumentException("A market order must not carry a price.", nameof(price));
        }

        return new OrderIntent(symbol, market, side, type, quantity, referencePrice, price, clientOrderId);
    }
}
