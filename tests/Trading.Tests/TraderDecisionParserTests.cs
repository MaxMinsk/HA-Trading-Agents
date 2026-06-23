using Trading.Agents;
using Trading.Core.Decisions;
using Xunit;

namespace Trading.Tests;

/// <summary>The parser is the safety boundary between a free-form model and a validated order.</summary>
public sealed class TraderDecisionParserTests
{
    [Fact]
    public void Parse_CleanJson_ReturnsDecision()
    {
        const string json = "{\"action\":\"buy\",\"sizeFraction\":0.2,\"confidence\":0.7,\"rationale\":\"uptrend\",\"keyRisks\":[\"reversal\"]}";

        var decision = TraderDecisionParser.Parse(json);

        Assert.Equal(TradeAction.Buy, decision.Action);
        Assert.Equal(0.2m, decision.SizeFraction);
        Assert.Equal(0.7, decision.Confidence, 3);
        Assert.Single(decision.KeyRisks);
    }

    [Fact]
    public void Parse_JsonWrappedInProseAndFences_StillParses()
    {
        const string text = "Here is my call:\n```json\n{\"action\":\"sell\",\"sizeFraction\":0.5,\"confidence\":0.6,\"rationale\":\"overbought\"}\n```\nThanks.";

        var decision = TraderDecisionParser.Parse(text);

        Assert.Equal(TradeAction.Sell, decision.Action);
        Assert.Equal(0.5m, decision.SizeFraction);
    }

    [Fact]
    public void Parse_HoldForcesZeroSize_EvenIfModelSendsSize()
    {
        const string json = "{\"action\":\"hold\",\"sizeFraction\":0.9,\"confidence\":0.4,\"rationale\":\"mixed\"}";

        var decision = TraderDecisionParser.Parse(json);

        Assert.Equal(TradeAction.Hold, decision.Action);
        Assert.Equal(0m, decision.SizeFraction);
    }

    [Fact]
    public void Parse_OutOfRangeSize_IsClamped()
    {
        const string json = "{\"action\":\"buy\",\"sizeFraction\":5,\"confidence\":9,\"rationale\":\"yolo\"}";

        var decision = TraderDecisionParser.Parse(json);

        Assert.Equal(1m, decision.SizeFraction);
        Assert.Equal(1d, decision.Confidence, 3);
    }

    [Theory]
    [InlineData("not json at all")]
    [InlineData("")]
    [InlineData("{ this is : broken json ]")]
    public void Parse_Garbage_FailsClosedToHold(string text)
    {
        var decision = TraderDecisionParser.Parse(text);

        Assert.Equal(TradeAction.Hold, decision.Action);
        Assert.Equal(0m, decision.SizeFraction);
    }

    [Fact]
    public void Parse_MissingRationale_GetsPlaceholderNotThrow()
    {
        const string json = "{\"action\":\"buy\",\"sizeFraction\":0.1,\"confidence\":0.5}";

        var decision = TraderDecisionParser.Parse(json);

        Assert.Equal(TradeAction.Buy, decision.Action);
        Assert.False(string.IsNullOrWhiteSpace(decision.Rationale));
    }
}
