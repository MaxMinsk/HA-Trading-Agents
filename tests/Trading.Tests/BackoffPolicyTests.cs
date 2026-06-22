using Trading.Data.Binance;
using Xunit;

namespace Trading.Tests;

/// <summary>Tests for the reconnect backoff policy.</summary>
public sealed class BackoffPolicyTests
{
    private static BackoffPolicy Policy(int maxSeconds = 30) =>
        new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(maxSeconds));

    [Fact]
    public void Delay_FirstAttempt_IsInitial() =>
        Assert.Equal(TimeSpan.FromSeconds(1), Policy().Delay(1));

    [Fact]
    public void Delay_GrowsExponentially()
    {
        var policy = Policy();
        Assert.Equal(TimeSpan.FromSeconds(2), policy.Delay(2));
        Assert.Equal(TimeSpan.FromSeconds(4), policy.Delay(3));
        Assert.Equal(TimeSpan.FromSeconds(8), policy.Delay(4));
    }

    [Fact]
    public void Delay_IsCappedAtMax() =>
        Assert.Equal(TimeSpan.FromSeconds(10), Policy(10).Delay(20));

    [Fact]
    public void Delay_AttemptBelowOne_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Policy().Delay(0));
}
