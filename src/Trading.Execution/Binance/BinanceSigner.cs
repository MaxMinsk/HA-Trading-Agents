using System.Security.Cryptography;
using System.Text;

namespace Trading.Execution.Binance;

/// <summary>
/// Signs a Binance request query string with HMAC-SHA256, as required by SIGNED (TRADE / USER_DATA)
/// endpoints. The signature is the lowercase hex digest of the exact query string, keyed by the API
/// secret. Isolated and pure so it can be unit-tested against Binance's documented example.
/// </summary>
public static class BinanceSigner
{
    /// <summary>Returns the lowercase hex HMAC-SHA256 of <paramref name="queryString"/> under <paramref name="secret"/>.</summary>
    /// <param name="queryString">The exact total query string to be signed (without the signature param).</param>
    /// <param name="secret">The API secret key.</param>
    public static string Sign(string queryString, string secret)
    {
        ArgumentNullException.ThrowIfNull(queryString);
        ArgumentException.ThrowIfNullOrEmpty(secret);
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(queryString));
        return Convert.ToHexStringLower(hash);
    }
}
