using System.Collections.ObjectModel;

namespace Trading.Core.Execution;

/// <summary>
/// Splits an exchange symbol (e.g. <c>BTCUSDT</c>) into its base and quote assets by matching the
/// longest known quote suffix. Binance symbols have no separator, so this is a pragmatic lookup
/// rather than a parse; unknown quotes fall back to a 4-char suffix.
/// </summary>
public static class SymbolAssets
{
    // Longest-first so e.g. FDUSD wins over USD-like prefixes; ordinal, upper-case symbols.
    private static readonly ReadOnlyCollection<string> KnownQuotes = new(
    [
        "FDUSD", "USDT", "USDC", "BUSD", "TUSD", "DAI", "EUR", "TRY", "BTC", "ETH", "BNB",
    ]);

    /// <summary>Returns the (base, quote) assets for <paramref name="symbol"/>.</summary>
    /// <param name="symbol">Exchange symbol, e.g. <c>BTCUSDT</c>.</param>
    public static (string Base, string Quote) Split(string symbol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        var upper = symbol.ToUpperInvariant();
        foreach (var quote in KnownQuotes)
        {
            if (upper.Length > quote.Length && upper.EndsWith(quote, StringComparison.Ordinal))
            {
                return (upper[..^quote.Length], quote);
            }
        }

        // Fallback: assume a 4-character quote (covers USDT-style pairs not listed above).
        return upper.Length > 4 ? (upper[..^4], upper[^4..]) : (upper, "USDT");
    }
}
