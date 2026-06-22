using Trading.Core.Execution;

namespace Trading.Execution;

/// <summary>Supplies the exchange filters for a symbol (tick/step/min-notional).</summary>
public interface ISymbolFilterProvider
{
    /// <summary>Returns the filters for <paramref name="symbol"/>, falling back to a default.</summary>
    /// <param name="symbol">Exchange symbol.</param>
    SymbolFilters GetFilters(string symbol);
}
