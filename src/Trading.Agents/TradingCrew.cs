using System.Globalization;
using Trading.Core.Decisions;
using Trading.Core.MarketData;
using Trading.Core.Strategies;

namespace Trading.Agents;

/// <summary>
/// The MAF multi-agent strategy: analyst → bull/bear debate → trader → (advisory) risk reviewer. It
/// is an <see cref="IStrategy"/>, so it backtests and executes like any other strategy. It depends
/// only on <see cref="IAgentFactory"/>, so it runs on real MAF agents in production and a fake in
/// tests. The trader's JSON is parsed fail-closed; the deterministic risk gate (TRD-S4) is still the
/// hard backstop downstream.
/// </summary>
public sealed class TradingCrew : IStrategy
{
    private readonly IAgentFactory _agents;
    private readonly TradingCrewOptions _options;

    /// <summary>Creates the crew over an agent factory.</summary>
    /// <param name="agents">Factory that builds the role agents.</param>
    /// <param name="options">Crew tunables (defaults if null).</param>
    public TradingCrew(IAgentFactory agents, TradingCrewOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(agents);
        _agents = agents;
        _options = options ?? TradingCrewOptions.Default;
    }

    /// <inheritdoc />
    public string Name => "maf-crew";

    /// <inheritdoc />
    public async Task<TradeDecision> DecideAsync(MarketSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var briefText = MarketBrief.Build(snapshot, _options.SmaFast, _options.SmaSlow).ToPromptText();

        var analysis = await _agents.Create("analyst", AgentRoles.Analyst)
            .RunAsync(briefText, cancellationToken).ConfigureAwait(false);

        var debateContext = briefText + "\n\n## Analyst notes\n" + analysis;
        var bullCase = await _agents.Create("bull", AgentRoles.Bull)
            .RunAsync(debateContext, cancellationToken).ConfigureAwait(false);
        var bearCase = await _agents.Create("bear", AgentRoles.Bear)
            .RunAsync(debateContext, cancellationToken).ConfigureAwait(false);

        var traderInput = debateContext
            + "\n\n## Bull case\n" + bullCase
            + "\n\n## Bear case\n" + bearCase
            + "\n\nDecide now. Respond with ONLY the JSON object.";
        var traderOutput = await _agents.Create("trader", AgentRoles.Trader)
            .RunAsync(traderInput, cancellationToken).ConfigureAwait(false);

        var decision = TraderDecisionParser.Parse(traderOutput);
        if (!_options.UseRiskReviewer || decision.Action == TradeAction.Hold)
        {
            return decision;
        }

        var summary = string.Create(
            CultureInfo.InvariantCulture,
            $"Proposed decision: {decision.Action} sizeFraction {decision.SizeFraction} confidence {decision.Confidence}. Rationale: {decision.Rationale}");
        var verdict = await _agents.Create("risk-reviewer", AgentRoles.RiskReviewer)
            .RunAsync(briefText + "\n\n" + summary, cancellationToken).ConfigureAwait(false);

        return verdict.Contains("BLOCK", StringComparison.OrdinalIgnoreCase)
            ? TradeDecision.Create(TradeAction.Hold, 0m, decision.Confidence, "risk reviewer blocked the trade")
            : decision;
    }
}
