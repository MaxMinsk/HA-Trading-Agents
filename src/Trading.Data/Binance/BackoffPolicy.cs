namespace Trading.Data.Binance;

/// <summary>Deterministic exponential backoff (no jitter): <c>initial · 2^(attempt-1)</c>, capped at a maximum.</summary>
public sealed class BackoffPolicy
{
    private readonly TimeSpan _initial;
    private readonly TimeSpan _max;

    /// <summary>Creates a backoff policy.</summary>
    /// <param name="initial">Delay for the first attempt (must be positive).</param>
    /// <param name="max">Maximum delay (must be greater than or equal to <paramref name="initial"/>).</param>
    public BackoffPolicy(TimeSpan initial, TimeSpan max)
    {
        if (initial <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(initial), initial, "Initial delay must be positive.");
        }

        if (max < initial)
        {
            throw new ArgumentOutOfRangeException(nameof(max), max, "Max delay must be >= initial.");
        }

        _initial = initial;
        _max = max;
    }

    /// <summary>Returns the delay for a 1-based attempt number, capped at the maximum.</summary>
    /// <param name="attempt">The attempt number (1 = first).</param>
    public TimeSpan Delay(int attempt)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(attempt, 1);
        var factor = Math.Pow(2, Math.Min(attempt - 1, 30));
        var milliseconds = Math.Min(_initial.TotalMilliseconds * factor, _max.TotalMilliseconds);
        return TimeSpan.FromMilliseconds(milliseconds);
    }
}
