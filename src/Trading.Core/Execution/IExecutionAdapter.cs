namespace Trading.Core.Execution;

/// <summary>
/// Places and cancels orders against a venue (or a paper simulator). Implementations trust the
/// <see cref="OrderIntent"/> they receive — the risk gate and exchange filters run upstream in the
/// execution service, so an adapter only translates the intent to the venue and reports the result.
/// </summary>
public interface IExecutionAdapter
{
    /// <summary>Adapter identity for logging/auditing, e.g. <c>paper</c> or <c>binance-testnet</c>.</summary>
    string Name { get; }

    /// <summary>Submits an order.</summary>
    /// <param name="intent">The validated order.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<OrderResult> SubmitAsync(OrderIntent intent, CancellationToken cancellationToken = default);

    /// <summary>Cancels an order by its client order id.</summary>
    /// <param name="symbol">Symbol the order is on.</param>
    /// <param name="clientOrderId">The client order id to cancel.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<OrderResult> CancelAsync(string symbol, string clientOrderId, CancellationToken cancellationToken = default);

    /// <summary>Reads current account balances.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<AccountSnapshot> GetAccountAsync(CancellationToken cancellationToken = default);
}
