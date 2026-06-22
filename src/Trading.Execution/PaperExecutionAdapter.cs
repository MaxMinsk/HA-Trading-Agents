using Trading.Core.Execution;

namespace Trading.Execution;

/// <summary>
/// An in-memory execution adapter that simulates fills against a reference price with a configurable
/// fee and slippage. It is the default adapter: it needs no API keys, never touches the network, and
/// lets the whole decision -&gt; risk -&gt; order path be exercised and tested safely.
/// </summary>
public sealed class PaperExecutionAdapter : IExecutionAdapter
{
    private readonly Lock _lock = new();
    private readonly Dictionary<string, decimal> _free = new(StringComparer.OrdinalIgnoreCase);
    private readonly decimal _feeRate;
    private readonly decimal _slippageRate;

    /// <summary>Creates the paper adapter funded with quote cash.</summary>
    /// <param name="startingQuote">Starting balance of the quote asset.</param>
    /// <param name="quoteAsset">Quote asset code (default <c>USDT</c>).</param>
    /// <param name="feeBps">Taker fee in basis points (default 10 = 0.10%).</param>
    /// <param name="slippageBps">Slippage in basis points (default 5 = 0.05%).</param>
    public PaperExecutionAdapter(
        decimal startingQuote,
        string quoteAsset = "USDT",
        decimal feeBps = 10m,
        decimal slippageBps = 5m)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(startingQuote);
        ArgumentException.ThrowIfNullOrWhiteSpace(quoteAsset);
        ArgumentOutOfRangeException.ThrowIfNegative(feeBps);
        ArgumentOutOfRangeException.ThrowIfNegative(slippageBps);
        _free[quoteAsset] = startingQuote;
        _feeRate = feeBps / 10_000m;
        _slippageRate = slippageBps / 10_000m;
    }

    /// <inheritdoc />
    public string Name => "paper";

    /// <inheritdoc />
    public Task<OrderResult> SubmitAsync(OrderIntent intent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(intent);
        var (baseAsset, quoteAsset) = SymbolAssets.Split(intent.Symbol);
        var basePrice = intent.Type == OrderType.Limit && intent.Price is { } limit ? limit : intent.ReferencePrice;

        OrderResult result;
        lock (_lock)
        {
            if (intent.Side == OrderSide.Buy)
            {
                var fillPrice = basePrice * (1m + _slippageRate);
                var notional = intent.Quantity * fillPrice;
                var fee = notional * _feeRate;
                var cost = notional + fee;
                if (FreeOf(quoteAsset) < cost)
                {
                    result = OrderResult.Rejected(intent.ClientOrderId, "insufficient " + quoteAsset);
                }
                else
                {
                    _free[quoteAsset] = FreeOf(quoteAsset) - cost;
                    _free[baseAsset] = FreeOf(baseAsset) + intent.Quantity;
                    result = OrderResult.Filled(intent.ClientOrderId, intent.Quantity, fillPrice, fee, quoteAsset);
                }
            }
            else
            {
                var fillPrice = basePrice * (1m - _slippageRate);
                if (FreeOf(baseAsset) < intent.Quantity)
                {
                    result = OrderResult.Rejected(intent.ClientOrderId, "insufficient " + baseAsset);
                }
                else
                {
                    var notional = intent.Quantity * fillPrice;
                    var fee = notional * _feeRate;
                    _free[baseAsset] = FreeOf(baseAsset) - intent.Quantity;
                    _free[quoteAsset] = FreeOf(quoteAsset) + (notional - fee);
                    result = OrderResult.Filled(intent.ClientOrderId, intent.Quantity, fillPrice, fee, quoteAsset);
                }
            }
        }

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<OrderResult> CancelAsync(string symbol, string clientOrderId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientOrderId);
        return Task.FromResult(
            OrderResult.Canceled(clientOrderId, note: "paper orders fill immediately; nothing to cancel"));
    }

    /// <inheritdoc />
    public Task<AccountSnapshot> GetAccountAsync(CancellationToken cancellationToken = default)
    {
        List<Balance> balances;
        lock (_lock)
        {
            balances = _free
                .Where(kvp => kvp.Value != 0m)
                .Select(kvp => new Balance { Asset = kvp.Key, Free = kvp.Value })
                .ToList();
        }

        return Task.FromResult(AccountSnapshot.Create(balances));
    }

    private decimal FreeOf(string asset) => _free.TryGetValue(asset, out var value) ? value : 0m;
}
