using System.Globalization;
using Trading.Core.Decisions;
using Trading.Core.Execution;
using Trading.Core.MarketData;
using Trading.Risk;

namespace Trading.Execution;

/// <summary>
/// The single safe path from a decision to an order. It reads the account, asks the
/// <see cref="RiskGate"/> what is allowed, sizes the order from the approved fraction, applies the
/// exchange filters, and only then hands a validated <see cref="OrderIntent"/> to the adapter.
/// Sizing convention: a buy deploys the approved fraction of equity; a sell sells the approved
/// fraction of the current holding (long-only by default).
/// </summary>
public sealed class ExecutionService
{
    private readonly IExecutionAdapter _adapter;
    private readonly RiskLimits _limits;
    private readonly ISymbolFilterProvider _filters;
    private readonly TimeProvider _time;

    /// <summary>Creates the service.</summary>
    /// <param name="adapter">The execution adapter (paper or venue).</param>
    /// <param name="limits">The active limits.</param>
    /// <param name="filters">Exchange filter provider.</param>
    /// <param name="time">Clock for client-order-id timestamps.</param>
    public ExecutionService(
        IExecutionAdapter adapter,
        RiskLimits limits,
        ISymbolFilterProvider filters,
        TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(adapter);
        ArgumentNullException.ThrowIfNull(limits);
        ArgumentNullException.ThrowIfNull(filters);
        ArgumentNullException.ThrowIfNull(time);
        _adapter = adapter;
        _limits = limits;
        _filters = filters;
        _time = time;
    }

    /// <summary>Runs a decision through risk + filters and, if allowed, submits the order.</summary>
    /// <param name="symbol">Exchange symbol.</param>
    /// <param name="market">Target market.</param>
    /// <param name="decision">The decision to act on.</param>
    /// <param name="referencePrice">Price known at decision time (e.g. last close).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<ExecutionOutcome> ExecuteAsync(
        string symbol,
        Market market,
        TradeDecision decision,
        decimal referencePrice,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentNullException.ThrowIfNull(decision);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(referencePrice);

        var (baseAsset, quoteAsset) = SymbolAssets.Split(symbol);
        var account = await _adapter.GetAccountAsync(cancellationToken).ConfigureAwait(false);
        var quoteFree = account.FreeOf(quoteAsset);
        var baseFree = account.FreeOf(baseAsset);
        var positionValue = baseFree * referencePrice;
        var equity = quoteFree + positionValue;
        var positionFraction = equity > 0m ? positionValue / equity : 0m;

        var state = AccountRiskState.Create(equity, positionFraction);
        var assessment = RiskGate.Evaluate(decision, state, _limits);
        if (!assessment.IsActionable)
        {
            return ExecutionOutcome.NotPlaced(symbol, decision.Action, assessment);
        }

        var side = decision.Action == TradeAction.Buy ? OrderSide.Buy : OrderSide.Sell;
        var quantity = side == OrderSide.Buy
            ? Math.Min(assessment.ApprovedFraction * equity, quoteFree) / referencePrice
            : assessment.ApprovedFraction * baseFree;

        var filters = _filters.GetFilters(symbol);
        quantity = filters.RoundQuantityToStep(quantity);
        if (!filters.IsTradeable(referencePrice, quantity))
        {
            return ExecutionOutcome.NotPlaced(
                symbol, decision.Action, assessment, "below exchange minimums after rounding");
        }

        var nonce = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)[..6];
        var clientOrderId = ClientOrderId.For(symbol, side, _time.GetUtcNow().ToUnixTimeMilliseconds(), nonce);
        var intent = OrderIntent.MarketOrder(symbol, market, side, quantity, referencePrice, clientOrderId);

        var result = await _adapter.SubmitAsync(intent, cancellationToken).ConfigureAwait(false);
        return ExecutionOutcome.PlacedOrder(symbol, decision.Action, assessment, intent, result);
    }
}
