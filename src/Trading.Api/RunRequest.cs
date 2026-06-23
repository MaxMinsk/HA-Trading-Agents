namespace Trading.Api;

/// <summary>Body for POST /api/run: which market to analyze and (optionally) which provider/model.</summary>
public sealed record RunRequest(string? Symbol, string? Interval, string? Market, string? Provider, string? Model);
