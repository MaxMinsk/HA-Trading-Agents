using System.ComponentModel;
using ModelContextProtocol.Server;
using Trading.Core.Decisions;
using Trading.Core.Execution;
using Trading.Core.MarketData;
using Trading.Execution;

namespace Trading.Mcp;

/// <summary>
/// The execution MCP tool surface (write tools). Every order goes through the deterministic risk
/// gate inside <see cref="ExecutionService"/> server-side, so a client cannot bypass sizing/limits.
/// These tools are only registered when execution is explicitly enabled; the default adapter is the
/// paper simulator and live trading is off unless configured.
/// </summary>
[McpServerToolType]
public sealed class ExecutionTools
{
    private readonly ExecutionService _execution;
    private readonly IExecutionAdapter _adapter;
    private readonly ISnapshotBuilder _snapshots;
    private readonly TimeProvider _time;

    /// <summary>Creates the execution tool set.</summary>
    /// <param name="execution">The risk-gated execution service.</param>
    /// <param name="adapter">The execution adapter (for cancel/account passthrough).</param>
    /// <param name="snapshots">Snapshot builder, used to read the reference price.</param>
    /// <param name="time">Clock for resolving "now".</param>
    public ExecutionTools(
        ExecutionService execution,
        IExecutionAdapter adapter,
        ISnapshotBuilder snapshots,
        TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(execution);
        ArgumentNullException.ThrowIfNull(adapter);
        ArgumentNullException.ThrowIfNull(snapshots);
        ArgumentNullException.ThrowIfNull(time);
        _execution = execution;
        _adapter = adapter;
        _snapshots = snapshots;
        _time = time;
    }

    /// <summary>Submits a trade intent through the risk gate.</summary>
    [McpServerTool(Name = "exec_submit_intent", UseStructuredContent = true)]
    [Description("Submit a trade intent. The server runs the deterministic risk gate, sizes the order, applies exchange filters, and (if allowed) places it via the configured adapter (paper or Binance testnet). Returns the full outcome including any veto reason; nothing is placed when vetoed.")]
    public async Task<ExecutionOutcome> SubmitIntent(
        [Description("Symbol, e.g. BTCUSDT")] string symbol,
        [Description("Action: buy, sell, or hold")] string action,
        [Description("Size fraction in [0,1]. Buy = fraction of equity to deploy; sell = fraction of holding to sell; 0 for hold.")] decimal sizeFraction = 0m,
        [Description("Interval used to read the reference price: 1h, 4h, or 1d")] string interval = "1h",
        [Description("Market: spot or usdm")] string market = "spot",
        [Description("Confidence in [0,1]")] double confidence = 0.5,
        [Description("Short rationale for the decision")] string rationale = "mcp exec_submit_intent",
        CancellationToken cancellationToken = default)
    {
        var act = ParseAction(action);
        var mkt = ParseMarket(market);
        var size = act == TradeAction.Hold ? 0m : sizeFraction;
        var decision = TradeDecision.Create(act, size, confidence, rationale);
        var referencePrice = await ResolveReferencePriceAsync(symbol, mkt, ParseInterval(interval), cancellationToken)
            .ConfigureAwait(false);
        return await _execution.ExecuteAsync(symbol, mkt, decision, referencePrice, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Cancels an open order by client order id.</summary>
    [McpServerTool(Name = "exec_cancel", UseStructuredContent = true)]
    [Description("Cancel an open order by its client order id.")]
    public Task<OrderResult> Cancel(
        [Description("Symbol the order is on, e.g. BTCUSDT")] string symbol,
        [Description("The client order id returned when the order was submitted")] string clientOrderId,
        CancellationToken cancellationToken = default) =>
        _adapter.CancelAsync(symbol, clientOrderId, cancellationToken);

    /// <summary>Reads account balances from the adapter.</summary>
    [McpServerTool(Name = "account_balances", ReadOnly = true, UseStructuredContent = true)]
    [Description("Read current account balances from the execution adapter (paper or Binance testnet).")]
    public Task<AccountSnapshot> Balances(CancellationToken cancellationToken = default) =>
        _adapter.GetAccountAsync(cancellationToken);

    private async Task<decimal> ResolveReferencePriceAsync(
        string symbol,
        Market market,
        CandleInterval interval,
        CancellationToken cancellationToken)
    {
        var snapshot = await _snapshots
            .BuildAsync(symbol, market, interval, _time.GetUtcNow(), 1, cancellationToken)
            .ConfigureAwait(false);
        return snapshot.Candles.Count > 0
            ? snapshot.Candles[^1].Close
            : throw new InvalidOperationException($"No candles available for {symbol} to derive a reference price.");
    }

    private static TradeAction ParseAction(string value) => value.ToUpperInvariant() switch
    {
        "BUY" => TradeAction.Buy,
        "SELL" => TradeAction.Sell,
        "HOLD" => TradeAction.Hold,
        _ => throw new ArgumentException($"Unsupported action '{value}' (use buy, sell, or hold).", nameof(value)),
    };

    private static CandleInterval ParseInterval(string value) => value switch
    {
        "1h" => CandleInterval.OneHour,
        "4h" => CandleInterval.FourHours,
        "1d" => CandleInterval.OneDay,
        _ => throw new ArgumentException($"Unsupported interval '{value}' (use 1h, 4h, or 1d).", nameof(value)),
    };

    private static Market ParseMarket(string value) => value.ToUpperInvariant() switch
    {
        "SPOT" => Market.Spot,
        "USDM" or "FUTURES" => Market.UsdmFutures,
        _ => throw new ArgumentException($"Unsupported market '{value}' (use spot or usdm).", nameof(value)),
    };
}
