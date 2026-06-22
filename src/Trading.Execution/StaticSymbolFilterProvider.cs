using Trading.Core.Execution;

namespace Trading.Execution;

/// <summary>
/// A fixed filter provider: a default applied to every symbol, with optional per-symbol overrides.
/// Enough for paper trading and a known symbol set; a live adapter can later populate overrides
/// from Binance <c>exchangeInfo</c>.
/// </summary>
public sealed class StaticSymbolFilterProvider(
    SymbolFilters? defaultFilters = null,
    IReadOnlyDictionary<string, SymbolFilters>? overrides = null) : ISymbolFilterProvider
{
    private readonly SymbolFilters _default = defaultFilters ?? SymbolFilters.Permissive;
    private readonly Dictionary<string, SymbolFilters> _overrides = overrides is null
        ? new Dictionary<string, SymbolFilters>(StringComparer.OrdinalIgnoreCase)
        : new Dictionary<string, SymbolFilters>(overrides, StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public SymbolFilters GetFilters(string symbol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        return _overrides.TryGetValue(symbol, out var filters) ? filters : _default;
    }
}
