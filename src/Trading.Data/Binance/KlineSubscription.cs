using Trading.Core.MarketData;

namespace Trading.Data.Binance;

/// <summary>A request to stream klines for one symbol at one interval.</summary>
/// <param name="Symbol">Exchange symbol, e.g. <c>BTCUSDT</c>.</param>
/// <param name="Interval">The candle interval.</param>
public sealed record KlineSubscription(string Symbol, CandleInterval Interval);
