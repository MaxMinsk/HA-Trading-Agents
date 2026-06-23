using System.Globalization;
using System.Text.Json;
using Trading.Core.Decisions;

namespace Trading.Agents;

/// <summary>
/// Maps the trader agent's JSON reply to a validated <see cref="TradeDecision"/>. The model can wrap
/// JSON in prose or fences, so we extract the object span; anything malformed, out of range, or
/// missing fails CLOSED to Hold. The model can never produce an unvalidated order — and the
/// deterministic risk gate (TRD-S4) remains the hard backstop downstream.
/// </summary>
public static class TraderDecisionParser
{
    /// <summary>Parses model output into a decision, defaulting to Hold on any problem.</summary>
    /// <param name="modelOutput">Raw text returned by the trader agent.</param>
    public static TradeDecision Parse(string modelOutput)
    {
        if (string.IsNullOrWhiteSpace(modelOutput))
        {
            return Hold("empty model output");
        }

        var json = ExtractJsonObject(modelOutput);
        if (json is null)
        {
            return Hold("no JSON object in model output");
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return Hold("model output was not a JSON object");
            }

            var action = ParseAction(ReadString(root, "action"));
            var size = action == TradeAction.Hold ? 0m : Clamp(ReadDecimal(root, "sizeFraction"), 0m, 1m);
            var confidence = Clamp(ReadDouble(root, "confidence", 0.5), 0d, 1d);
            var rationale = ReadString(root, "rationale");
            if (string.IsNullOrWhiteSpace(rationale))
            {
                rationale = "(no rationale provided)";
            }

            return TradeDecision.Create(action, size, confidence, rationale, ReadStringArray(root, "keyRisks"));
        }
        catch (JsonException)
        {
            return Hold("unparseable model output (invalid JSON)");
        }
    }

    private static string? ExtractJsonObject(string text)
    {
        var start = text.IndexOf('{', StringComparison.Ordinal);
        var end = text.LastIndexOf('}');
        return start >= 0 && end > start ? text[start..(end + 1)] : null;
    }

    private static TradeAction ParseAction(string? value) => (value?.Trim().ToUpperInvariant()) switch
    {
        "BUY" => TradeAction.Buy,
        "SELL" => TradeAction.Sell,
        _ => TradeAction.Hold,
    };

    private static string? ReadString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static decimal ReadDecimal(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value)
            ? value.ValueKind switch
            {
                JsonValueKind.Number when value.TryGetDecimal(out var number) => number,
                JsonValueKind.String when decimal.TryParse(
                    value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) => parsed,
                _ => 0m,
            }
            : 0m;

    private static double ReadDouble(JsonElement root, string name, double fallback) =>
        root.TryGetProperty(name, out var value)
            ? value.ValueKind switch
            {
                JsonValueKind.Number when value.TryGetDouble(out var number) => number,
                JsonValueKind.String when double.TryParse(
                    value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) => parsed,
                _ => fallback,
            }
            : fallback;

    private static List<string> ReadStringArray(JsonElement root, string name)
    {
        var items = new List<string>();
        if (root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in value.EnumerateArray())
            {
                if (element.ValueKind == JsonValueKind.String && element.GetString() is { Length: > 0 } text)
                {
                    items.Add(text);
                }
            }
        }

        return items;
    }

    private static decimal Clamp(decimal value, decimal min, decimal max) => Math.Clamp(value, min, max);

    private static double Clamp(double value, double min, double max) => Math.Clamp(value, min, max);

    private static TradeDecision Hold(string reason) =>
        TradeDecision.Create(TradeAction.Hold, 0m, 0.5, "failing closed to hold: " + reason);
}
