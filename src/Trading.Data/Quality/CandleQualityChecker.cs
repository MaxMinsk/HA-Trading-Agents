using Trading.Core.MarketData;

namespace Trading.Data.Quality;

/// <summary>
/// Deterministic data-quality checks over a candle series: duplicate open times and gaps
/// (missing intervals). Pure and unit-testable; performs no I/O.
/// </summary>
public static class CandleQualityChecker
{
    /// <summary>Examines a candle series and reports duplicates and gaps for the expected interval.</summary>
    /// <param name="candles">Candles to examine (any order).</param>
    /// <param name="interval">The interval the series is expected to follow.</param>
    public static CandleQualityReport Check(IReadOnlyList<Candle> candles, CandleInterval interval)
    {
        ArgumentNullException.ThrowIfNull(candles);
        if (candles.Count == 0)
        {
            return new CandleQualityReport(0, [], [], null, null);
        }

        var ordered = candles.OrderBy(c => c.OpenTimeUtc).ToList();
        var step = Intervals.ToTimeSpan(interval);

        var duplicates = new List<DateTimeOffset>();
        var gaps = new List<CandleGap>();

        for (var i = 1; i < ordered.Count; i++)
        {
            var prevOpen = ordered[i - 1].OpenTimeUtc;
            var open = ordered[i].OpenTimeUtc;

            if (open == prevOpen)
            {
                duplicates.Add(open);
                continue;
            }

            var steps = (int)Math.Round((open - prevOpen) / step);
            if (steps > 1)
            {
                gaps.Add(new CandleGap(prevOpen, open, steps - 1));
            }
        }

        return new CandleQualityReport(
            ordered.Count,
            duplicates.Distinct().ToList(),
            gaps,
            ordered[0].OpenTimeUtc,
            ordered[^1].CloseTimeUtc);
    }
}
