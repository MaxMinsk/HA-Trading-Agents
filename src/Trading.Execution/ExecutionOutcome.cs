using Trading.Core.Decisions;
using Trading.Core.Execution;
using Trading.Risk;

namespace Trading.Execution;

/// <summary>
/// The full result of running a decision through the execution service: the risk ruling, and — when
/// an order was actually placed — the intent and the adapter's result. This is the audit record a
/// caller (or MCP tool) gets back, whether or not anything traded.
/// </summary>
public sealed record ExecutionOutcome
{
    private ExecutionOutcome(
        string symbol,
        TradeAction action,
        RiskVerdict verdict,
        decimal approvedFraction,
        string reason,
        bool placed,
        OrderIntent? intent,
        OrderResult? result)
    {
        Symbol = symbol;
        Action = action;
        Verdict = verdict;
        ApprovedFraction = approvedFraction;
        Reason = reason;
        Placed = placed;
        Intent = intent;
        Result = result;
    }

    /// <summary>Symbol the decision was for.</summary>
    public string Symbol { get; }

    /// <summary>The action the decision recommended.</summary>
    public TradeAction Action { get; }

    /// <summary>The risk gate's verdict.</summary>
    public RiskVerdict Verdict { get; }

    /// <summary>The permitted size fraction.</summary>
    public decimal ApprovedFraction { get; }

    /// <summary>Why this outcome occurred (risk reason or a placement note).</summary>
    public string Reason { get; }

    /// <summary>True if an order was submitted to the adapter.</summary>
    public bool Placed { get; }

    /// <summary>The submitted order, if any.</summary>
    public OrderIntent? Intent { get; }

    /// <summary>The adapter's result, if an order was submitted.</summary>
    public OrderResult? Result { get; }

    /// <summary>No order was placed (vetoed, flat, or too small).</summary>
    /// <param name="symbol">Symbol.</param>
    /// <param name="action">Recommended action.</param>
    /// <param name="assessment">The risk ruling.</param>
    /// <param name="reasonOverride">Optional reason replacing the assessment's.</param>
    public static ExecutionOutcome NotPlaced(
        string symbol,
        TradeAction action,
        RiskAssessment assessment,
        string? reasonOverride = null)
    {
        ArgumentNullException.ThrowIfNull(assessment);
        return new ExecutionOutcome(
            symbol, action, assessment.Verdict, assessment.ApprovedFraction,
            reasonOverride ?? assessment.Reason, placed: false, intent: null, result: null);
    }

    /// <summary>An order was placed; carries the intent and adapter result.</summary>
    /// <param name="symbol">Symbol.</param>
    /// <param name="action">Recommended action.</param>
    /// <param name="assessment">The risk ruling.</param>
    /// <param name="intent">The submitted order.</param>
    /// <param name="result">The adapter's result.</param>
    public static ExecutionOutcome PlacedOrder(
        string symbol,
        TradeAction action,
        RiskAssessment assessment,
        OrderIntent intent,
        OrderResult result)
    {
        ArgumentNullException.ThrowIfNull(assessment);
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(result);
        return new ExecutionOutcome(
            symbol, action, assessment.Verdict, assessment.ApprovedFraction,
            assessment.Reason, placed: true, intent, result);
    }
}
