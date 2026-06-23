using Trading.Agents;
using Trading.Core.Decisions;
using Xunit;

namespace Trading.Tests;

/// <summary>End-to-end crew behavior over a scripted (fake) agent factory — no network, no keys.</summary>
public sealed class TradingCrewTests
{
    private static decimal[] Rising(int n)
    {
        var closes = new decimal[n];
        for (var i = 0; i < n; i++)
        {
            closes[i] = 100m + i;
        }

        return closes;
    }

    private const string BuyJson =
        "{\"action\":\"buy\",\"sizeFraction\":0.2,\"confidence\":0.7,\"rationale\":\"uptrend\",\"keyRisks\":[\"reversal\"]}";

    [Fact]
    public async Task DecideAsync_RunsFullPipeline_AndReturnsTraderDecision()
    {
        var factory = new FakeAgentFactory((role, _) => role switch
        {
            "trader" => BuyJson,
            "risk-reviewer" => "OK",
            _ => "ok",
        });
        var crew = new TradingCrew(factory);

        var decision = await crew.DecideAsync(AgentTestData.Snapshot(Rising(60)));

        Assert.Equal(TradeAction.Buy, decision.Action);
        Assert.Equal(0.2m, decision.SizeFraction);
        Assert.Equal(["analyst", "bull", "bear", "trader", "risk-reviewer"], factory.CreatedRoles);
    }

    [Fact]
    public async Task DecideAsync_RiskReviewerBlocks_DowngradesToHold()
    {
        var factory = new FakeAgentFactory((role, _) => role switch
        {
            "trader" => BuyJson,
            "risk-reviewer" => "BLOCK",
            _ => "ok",
        });
        var crew = new TradingCrew(factory);

        var decision = await crew.DecideAsync(AgentTestData.Snapshot(Rising(60)));

        Assert.Equal(TradeAction.Hold, decision.Action);
        Assert.Equal(0m, decision.SizeFraction);
    }

    [Fact]
    public async Task DecideAsync_TraderGarbage_FailsClosedAndSkipsReviewer()
    {
        var factory = new FakeAgentFactory((role, _) =>
            string.Equals(role, "trader", StringComparison.Ordinal) ? "i cannot decide" : "ok");
        var crew = new TradingCrew(factory);

        var decision = await crew.DecideAsync(AgentTestData.Snapshot(Rising(60)));

        Assert.Equal(TradeAction.Hold, decision.Action);
        Assert.DoesNotContain("risk-reviewer", factory.CreatedRoles);
    }

    [Fact]
    public async Task DecideAsync_RiskReviewerDisabled_KeepsTraderDecision()
    {
        var factory = new FakeAgentFactory((role, _) =>
            string.Equals(role, "trader", StringComparison.Ordinal) ? BuyJson : "ok");
        var crew = new TradingCrew(factory, new TradingCrewOptions { UseRiskReviewer = false });

        var decision = await crew.DecideAsync(AgentTestData.Snapshot(Rising(60)));

        Assert.Equal(TradeAction.Buy, decision.Action);
        Assert.DoesNotContain("risk-reviewer", factory.CreatedRoles);
    }
}
