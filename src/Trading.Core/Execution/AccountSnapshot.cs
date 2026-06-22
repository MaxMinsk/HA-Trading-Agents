using System.Collections.ObjectModel;

namespace Trading.Core.Execution;

/// <summary>A point-in-time view of account balances from an execution adapter.</summary>
public sealed record AccountSnapshot
{
    private AccountSnapshot(IReadOnlyList<Balance> balances)
    {
        Balances = balances;
    }

    /// <summary>All non-trivial balances.</summary>
    public IReadOnlyList<Balance> Balances { get; }

    /// <summary>Builds a snapshot from a set of balances.</summary>
    /// <param name="balances">Per-asset balances.</param>
    public static AccountSnapshot Create(IEnumerable<Balance> balances)
    {
        ArgumentNullException.ThrowIfNull(balances);
        return new AccountSnapshot(new ReadOnlyCollection<Balance>(balances.ToList()));
    }

    /// <summary>Free balance for <paramref name="asset"/>, or 0 if absent.</summary>
    /// <param name="asset">Asset code.</param>
    public decimal FreeOf(string asset)
    {
        foreach (var balance in Balances)
        {
            if (string.Equals(balance.Asset, asset, StringComparison.OrdinalIgnoreCase))
            {
                return balance.Free;
            }
        }

        return 0m;
    }
}
