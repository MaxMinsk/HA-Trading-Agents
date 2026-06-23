using Trading.Agents;
using Xunit;

namespace Trading.Tests;

/// <summary>The brief is the agents' grounded input; its numbers and warm-up handling are pinned here.</summary>
public sealed class MarketBriefTests
{
    private static decimal[] Increasing(int n)
    {
        var closes = new decimal[n];
        for (var i = 0; i < n; i++)
        {
            closes[i] = 100m + i;
        }

        return closes;
    }

    [Fact]
    public void Build_StrictlyRisingSeries_IsUptrendWithMaxRsi()
    {
        var snapshot = AgentTestData.Snapshot(Increasing(60));

        var brief = MarketBrief.Build(snapshot, fast: 20, slow: 50, rsiPeriod: 14);

        Assert.Equal("up", brief.Trend);
        Assert.True(brief.SmaFast > brief.SmaSlow);
        Assert.Equal(100m, brief.Rsi);              // all-gains window
        Assert.Equal(159m, brief.LastPrice);        // 100 + 59
        Assert.True(brief.ReturnPctWindow > 0m);
    }

    [Fact]
    public void Build_TooFewCandles_IsWarmingUp()
    {
        var snapshot = AgentTestData.Snapshot(Increasing(10));

        var brief = MarketBrief.Build(snapshot, fast: 20, slow: 50);

        Assert.Equal("warming-up", brief.Trend);
        Assert.Equal(10, brief.CandleCount);
    }

    [Fact]
    public void Build_UsesSnapshotAsOf_AndIncludesFactsInPromptText()
    {
        var snapshot = AgentTestData.Snapshot(Increasing(55));

        var brief = MarketBrief.Build(snapshot);
        var text = brief.ToPromptText();

        Assert.Equal(AgentTestData.AsOf, brief.AsOfUtc);
        Assert.Contains("BTCUSDT", text, StringComparison.Ordinal);
        Assert.Contains("trend:", text, StringComparison.Ordinal);
    }
}
