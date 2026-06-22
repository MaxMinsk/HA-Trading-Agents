namespace Trading.Core.Execution;

/// <summary>The outcome of submitting (or canceling) an order, as reported by an execution adapter.</summary>
public sealed record OrderResult
{
    /// <summary>The client order id this result refers to.</summary>
    public required string ClientOrderId { get; init; }

    /// <summary>Venue-assigned order id, if any.</summary>
    public string? ExchangeOrderId { get; init; }

    /// <summary>Final/known status.</summary>
    public required OrderStatus Status { get; init; }

    /// <summary>Quantity filled, in base units.</summary>
    public decimal FilledQuantity { get; init; }

    /// <summary>Average fill price (0 if nothing filled).</summary>
    public decimal AveragePrice { get; init; }

    /// <summary>Fee charged, in <see cref="FeeAsset"/>.</summary>
    public decimal Fee { get; init; }

    /// <summary>Asset the fee was charged in.</summary>
    public string FeeAsset { get; init; } = string.Empty;

    /// <summary>Human-readable note (e.g. a rejection reason).</summary>
    public string? Note { get; init; }

    /// <summary>A fully-filled result.</summary>
    /// <param name="clientOrderId">Client order id.</param>
    /// <param name="quantity">Filled quantity.</param>
    /// <param name="averagePrice">Average fill price.</param>
    /// <param name="fee">Fee charged.</param>
    /// <param name="feeAsset">Fee asset.</param>
    /// <param name="exchangeOrderId">Optional venue order id.</param>
    public static OrderResult Filled(
        string clientOrderId,
        decimal quantity,
        decimal averagePrice,
        decimal fee,
        string feeAsset,
        string? exchangeOrderId = null) =>
        new()
        {
            ClientOrderId = clientOrderId,
            ExchangeOrderId = exchangeOrderId,
            Status = OrderStatus.Filled,
            FilledQuantity = quantity,
            AveragePrice = averagePrice,
            Fee = fee,
            FeeAsset = feeAsset,
        };

    /// <summary>A rejected result with a reason.</summary>
    /// <param name="clientOrderId">Client order id.</param>
    /// <param name="note">Why it was rejected.</param>
    public static OrderResult Rejected(string clientOrderId, string note) =>
        new() { ClientOrderId = clientOrderId, Status = OrderStatus.Rejected, Note = note };

    /// <summary>A canceled result.</summary>
    /// <param name="clientOrderId">Client order id.</param>
    /// <param name="exchangeOrderId">Optional venue order id.</param>
    /// <param name="note">Optional note.</param>
    public static OrderResult Canceled(string clientOrderId, string? exchangeOrderId = null, string? note = null) =>
        new()
        {
            ClientOrderId = clientOrderId,
            ExchangeOrderId = exchangeOrderId,
            Status = OrderStatus.Canceled,
            Note = note,
        };
}
