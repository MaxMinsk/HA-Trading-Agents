namespace Trading.Data.Quality;

/// <summary>The result of a data-quality pass over a candle series.</summary>
/// <param name="Count">Number of candles examined.</param>
/// <param name="DuplicateOpenTimes">Open times that appeared more than once.</param>
/// <param name="Gaps">Detected gaps (missing intervals) in the series.</param>
/// <param name="FirstOpenUtc">Earliest open time, or null when the series is empty.</param>
/// <param name="LastCloseUtc">Latest close time, or null when the series is empty.</param>
public sealed record CandleQualityReport(
    int Count,
    IReadOnlyList<DateTimeOffset> DuplicateOpenTimes,
    IReadOnlyList<CandleGap> Gaps,
    DateTimeOffset? FirstOpenUtc,
    DateTimeOffset? LastCloseUtc)
{
    /// <summary>True when there are no duplicate open times and no gaps.</summary>
    public bool IsClean => DuplicateOpenTimes.Count == 0 && Gaps.Count == 0;

    /// <summary>Total number of missing candles summed across all gaps.</summary>
    public int MissingCandleCount => Gaps.Sum(g => g.MissingCount);
}
