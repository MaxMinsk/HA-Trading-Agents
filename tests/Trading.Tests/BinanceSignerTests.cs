using Trading.Execution.Binance;
using Xunit;

namespace Trading.Tests;

/// <summary>
/// Pins the request signing against an independently computed HMAC-SHA256 digest (verified with
/// <c>openssl dgst -sha256 -hmac</c>), so the adapter's signatures are correct without any network.
/// Uses Binance's documented example parameters and secret.
/// </summary>
public sealed class BinanceSignerTests
{
    [Fact]
    public void Sign_MatchesIndependentHmac()
    {
        const string secret = "NhqPtmdSJYdKjVHjA7PZj4Mge3R5YNiP1e3UZjInClVN65XAbvqqM6A7H5fATj0";
        const string query =
            "symbol=LTCBTC&side=BUY&type=LIMIT&timeInForce=GTC&quantity=1&price=0.1&recvWindow=5000&timestamp=1499827319559";
        const string expected = "b89008e7051ffbf2242be7dc5ae67fd146e6430688627b802c0cbec146e46aef";

        Assert.Equal(expected, BinanceSigner.Sign(query, secret));
    }

    [Fact]
    public void Options_DefaultsToTestnet_AndLiveSwitchesBaseUrl()
    {
        var testnet = BinanceExecutionOptions.Create("key", "secret");
        var live = BinanceExecutionOptions.Create("key", "secret", isLive: true);

        Assert.False(testnet.IsLive);
        Assert.Equal(BinanceExecutionOptions.SpotTestnetBaseUrl, testnet.BaseAddress.ToString().TrimEnd('/'));
        Assert.True(live.IsLive);
        Assert.Equal(BinanceExecutionOptions.SpotLiveBaseUrl, live.BaseAddress.ToString().TrimEnd('/'));
    }

    [Fact]
    public void Options_BlankKey_Throws() =>
        Assert.Throws<ArgumentException>(() => BinanceExecutionOptions.Create("", "secret"));
}
