using System.Globalization;
using System.Text;

namespace Trading.Core.Execution;

/// <summary>
/// Builds a venue-safe client order id. A stable id makes submission idempotent: a retried order
/// with the same id is deduplicated by the exchange instead of double-filling. Restricted to the
/// characters Binance accepts and capped at 36 chars.
/// </summary>
public static class ClientOrderId
{
    private const int MaxLength = 36;

    /// <summary>Sanitizes <paramref name="seed"/> to the allowed charset and length.</summary>
    /// <param name="seed">Proposed id (any text).</param>
    /// <exception cref="ArgumentException"><paramref name="seed"/> is null/blank or has no usable characters.</exception>
    public static string Create(string seed)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(seed);
        var builder = new StringBuilder(MaxLength);
        foreach (var ch in seed)
        {
            if (builder.Length == MaxLength)
            {
                break;
            }

            if (char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_')
            {
                builder.Append(ch);
            }
        }

        return builder.Length == 0
            ? throw new ArgumentException("Seed contains no characters usable in a client order id.", nameof(seed))
            : builder.ToString();
    }

    /// <summary>Composes an id from the order's identity plus a uniqueness nonce, then sanitizes it.</summary>
    /// <param name="symbol">Exchange symbol.</param>
    /// <param name="side">Order side.</param>
    /// <param name="unixMs">Decision time in unix milliseconds.</param>
    /// <param name="nonce">A short uniqueness suffix (e.g. the first chars of a GUID).</param>
    public static string For(string symbol, OrderSide side, long unixMs, string nonce)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(nonce);
        var seed = string.Create(
            CultureInfo.InvariantCulture,
            $"T-{(side == OrderSide.Buy ? 'B' : 'S')}-{symbol}-{unixMs}-{nonce}");
        return Create(seed);
    }
}
