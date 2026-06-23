using Trading.Agents;
using Trading.Core.Decisions;

namespace Trading.Api;

/// <summary>Runs the MAF crew for a request, streaming each role message via the callback.</summary>
public interface ICrewRunner
{
    /// <summary>Fetches the snapshot, runs the crew, and reports each role's message in order.</summary>
    /// <param name="request">The run request (symbol/interval/market + optional provider/model).</param>
    /// <param name="onMessage">Awaited callback for each role message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<TradeDecision> RunAsync(
        RunRequest request,
        Func<CrewMessage, CancellationToken, Task> onMessage,
        CancellationToken cancellationToken);
}
