using Trading.Agents;
using Trading.Agents.Mcp;
using Trading.Core.Decisions;

namespace Trading.Api;

/// <summary>
/// The production crew runner: resolves the LLM options from the server environment (keys never leave
/// the server), fetches a point-in-time snapshot over MCP, and runs the MAF crew with streaming.
/// </summary>
public sealed class CrewRunner(DataMcpClient mcp) : ICrewRunner
{
    /// <inheritdoc />
    public async Task<TradeDecision> RunAsync(
        RunRequest request,
        Func<CrewMessage, CancellationToken, Task> onMessage,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var llm = AgentEnvironment.TryReadLlmOptions(request.Provider, request.Model)
            ?? throw new InvalidOperationException(
                "No LLM provider/key configured on the server. Set TRADING_LLM_PROVIDER and an API key.");

        var snapshot = await mcp.GetSnapshotAsync(
            string.IsNullOrWhiteSpace(request.Symbol) ? "BTCUSDT" : request.Symbol,
            string.IsNullOrWhiteSpace(request.Interval) ? "1h" : request.Interval,
            string.IsNullOrWhiteSpace(request.Market) ? "spot" : request.Market,
            lookback: 80,
            cancellationToken);

        var crew = new TradingCrew(new MafAgentFactory(ChatProviderFactory.Create(llm)));
        return await crew.DecideAsync(snapshot, onMessage, cancellationToken);
    }
}
