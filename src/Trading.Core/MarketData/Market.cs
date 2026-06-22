namespace Trading.Core.MarketData;

/// <summary>The Binance market a symbol trades on.</summary>
public enum Market
{
    /// <summary>Binance spot market.</summary>
    Spot,

    /// <summary>Binance USDⓈ-M perpetual / quarterly futures.</summary>
    UsdmFutures,
}
