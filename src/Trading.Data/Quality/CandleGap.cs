namespace Trading.Data.Quality;

/// <summary>A detected gap between two consecutive candles in a series.</summary>
/// <param name="AfterOpenUtc">Open time of the candle before the gap.</param>
/// <param name="NextOpenUtc">Open time of the candle after the gap.</param>
/// <param name="MissingCount">Number of candles missing between them.</param>
public sealed record CandleGap(DateTimeOffset AfterOpenUtc, DateTimeOffset NextOpenUtc, int MissingCount);
