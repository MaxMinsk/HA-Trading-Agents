namespace Trading.Core.MarketData;

/// <summary>Supported candle (kline) intervals. Low-frequency by design — see ADR 0001.</summary>
public enum CandleInterval
{
    /// <summary>One hour.</summary>
    OneHour,

    /// <summary>Four hours.</summary>
    FourHours,

    /// <summary>One day.</summary>
    OneDay,
}
