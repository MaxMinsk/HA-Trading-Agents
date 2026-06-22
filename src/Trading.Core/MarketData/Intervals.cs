namespace Trading.Core.MarketData;

/// <summary>Provider-agnostic semantics for <see cref="CandleInterval"/>.</summary>
public static class Intervals
{
    /// <summary>Returns the wall-clock duration of one candle of the given interval.</summary>
    /// <param name="interval">The interval.</param>
    public static TimeSpan ToTimeSpan(CandleInterval interval) => interval switch
    {
        CandleInterval.OneHour => TimeSpan.FromHours(1),
        CandleInterval.FourHours => TimeSpan.FromHours(4),
        CandleInterval.OneDay => TimeSpan.FromDays(1),
        _ => throw new ArgumentOutOfRangeException(nameof(interval), interval, "Unsupported interval."),
    };
}
