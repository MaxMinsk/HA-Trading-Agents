namespace Trading.Api;

/// <summary>Body for POST /api/execute: a trade intent to send through the server-side risk gate.</summary>
public sealed record ExecuteRequest(string? Symbol, string? Action, decimal SizeFraction, string? Interval, string? Market);
