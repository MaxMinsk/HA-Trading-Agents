using Trading.Agents;
using Trading.Core.Decisions;

namespace Trading.Api;

/// <summary>
/// The production crew runner: resolves LLM options from settings (UI) then env, fetches a
/// point-in-time snapshot over a per-request MCP client, and runs the MAF crew with streaming.
/// </summary>
public sealed class CrewRunner(McpClientProvider mcp, SettingsStore settings) : ICrewRunner
{
    /// <inheritdoc />
    public async Task<TradeDecision> RunAsync(
        RunRequest request,
        Func<CrewMessage, CancellationToken, Task> onMessage,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var llm = settings.ResolveLlm(request.Provider, request.Model)
            ?? throw new InvalidOperationException(
                "No LLM provider/key configured. Set it on the Settings page (or via env / add-on options).");

        var snapshot = await mcp.Create().GetSnapshotAsync(
            string.IsNullOrWhiteSpace(request.Symbol) ? "BTCUSDT" : request.Symbol,
            string.IsNullOrWhiteSpace(request.Interval) ? "1h" : request.Interval,
            string.IsNullOrWhiteSpace(request.Market) ? "spot" : request.Market,
            lookback: 80,
            cancellationToken);

        var crew = new TradingCrew(new MafAgentFactory(ChatProviderFactory.Create(llm)));
        return await crew.DecideAsync(snapshot, onMessage, cancellationToken);
    }
}
