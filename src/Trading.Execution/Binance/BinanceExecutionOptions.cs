namespace Trading.Execution.Binance;

/// <summary>
/// Configuration for the Binance execution adapter. Defaults to the spot <b>testnet</b>: live
/// (mainnet) trading is off unless <see cref="IsLive"/> is explicitly set, so an accidental
/// misconfiguration cannot move real funds. Keys live only where this is constructed (the server).
/// </summary>
public sealed record BinanceExecutionOptions
{
    /// <summary>Spot testnet REST base URL.</summary>
    public const string SpotTestnetBaseUrl = "https://testnet.binance.vision";

    /// <summary>Spot live (mainnet) REST base URL.</summary>
    public const string SpotLiveBaseUrl = "https://api.binance.com";

    private BinanceExecutionOptions(Uri baseAddress, string apiKey, string apiSecret, int recvWindow, bool isLive)
    {
        BaseAddress = baseAddress;
        ApiKey = apiKey;
        ApiSecret = apiSecret;
        RecvWindow = recvWindow;
        IsLive = isLive;
    }

    /// <summary>REST base address (testnet unless live).</summary>
    public Uri BaseAddress { get; }

    /// <summary>API key (sent as the <c>X-MBX-APIKEY</c> header).</summary>
    public string ApiKey { get; }

    /// <summary>API secret (used to sign requests; never sent).</summary>
    public string ApiSecret { get; }

    /// <summary>Request validity window in milliseconds.</summary>
    public int RecvWindow { get; }

    /// <summary>Whether this targets live (mainnet) trading.</summary>
    public bool IsLive { get; }

    /// <summary>Creates validated options.</summary>
    /// <param name="apiKey">API key.</param>
    /// <param name="apiSecret">API secret.</param>
    /// <param name="isLive">Target live trading (defaults to testnet).</param>
    /// <param name="baseUrl">Override base URL; defaults to the testnet/live URL by <paramref name="isLive"/>.</param>
    /// <param name="recvWindow">Request validity window ms (1..60000, default 5000).</param>
    public static BinanceExecutionOptions Create(
        string apiKey,
        string apiSecret,
        bool isLive = false,
        string? baseUrl = null,
        int recvWindow = 5000)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiSecret);
        if (recvWindow is < 1 or > 60_000)
        {
            throw new ArgumentOutOfRangeException(nameof(recvWindow), recvWindow, "recvWindow must be in [1, 60000].");
        }

        var url = baseUrl ?? (isLive ? SpotLiveBaseUrl : SpotTestnetBaseUrl);
        return new BinanceExecutionOptions(new Uri(url, UriKind.Absolute), apiKey, apiSecret, recvWindow, isLive);
    }
}
