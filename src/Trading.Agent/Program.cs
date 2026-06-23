using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using Trading.Agent;
using Trading.Agents;
using Trading.Core.Decisions;
using Trading.Core.MarketData;
using Trading.Core.Strategies;

// Relocatable agent host. Reaches market data ONLY over MCP, so it runs the same whether it sits
// locally (TRADING_MCP_URL=https://server/mcp + bearer) or inside the add-on. With an LLM provider +
// key configured it runs the MAF multi-agent crew (TRD-S5); otherwise it falls back to a plain SMA
// rule so the loop still works without keys (e.g. on the geo-restricted server).

var url = Environment.GetEnvironmentVariable("TRADING_MCP_URL") ?? "http://localhost:8080/mcp";
var bearer = Environment.GetEnvironmentVariable("TRADING_BEARER_TOKEN");
var symbol = GetOpt(args, "--symbol") ?? "BTCUSDT";
var interval = GetOpt(args, "--interval") ?? "1h";
var market = GetOpt(args, "--market") ?? "spot";
var strategyName = GetOpt(args, "--strategy") ?? "crew";
var fast = int.Parse(GetOpt(args, "--fast") ?? "20", CultureInfo.InvariantCulture);
var slow = int.Parse(GetOpt(args, "--slow") ?? "50", CultureInfo.InvariantCulture);

using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
if (!string.IsNullOrWhiteSpace(bearer))
{
    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
}

var client = new McpHttpClient(http, url);
Console.WriteLine($"Agent -> data MCP at {url} ({symbol} {interval} {market})");

var lookback = Math.Max(slow + 5, 80);
var snapshotJson = await client.CallToolAsync("market_snapshot", new Dictionary<string, object?>(StringComparer.Ordinal)
{
    ["symbol"] = symbol,
    ["interval"] = interval,
    ["market"] = market,
    ["lookback"] = lookback,
});

var snapshot = ParseSnapshot(snapshotJson);
Console.WriteLine($"Got {snapshot.Candles.Count} candles via MCP for {snapshot.Symbol} as of {snapshot.AsOfUtc:O}.");

var llm = TryBuildLlmOptions();
IStrategy strategy;
if (string.Equals(strategyName, "sma", StringComparison.OrdinalIgnoreCase) || llm is null)
{
    if (llm is null && string.Equals(strategyName, "crew", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("No LLM provider/key configured; falling back to SMA. Set TRADING_LLM_PROVIDER + an API key to run the crew.");
    }

    strategy = new SmaStrategy(fast, slow);
}
else
{
    var chat = ChatProviderFactory.Create(llm);
    strategy = new TradingCrew(new MafAgentFactory(chat), new TradingCrewOptions { SmaFast = fast, SmaSlow = slow });
    Console.WriteLine($"Running MAF crew on {llm.Provider} ({llm.Model}).");
}

var decision = await strategy.DecideAsync(snapshot);
Console.WriteLine($"[{strategy.Name}] Decision: {decision.Action} size {decision.SizeFraction} conf {decision.Confidence:0.00} — {decision.Rationale}");
if (decision.KeyRisks.Count > 0)
{
    Console.WriteLine("Key risks: " + string.Join("; ", decision.KeyRisks));
}

if (decision.Action != TradeAction.Hold
    && string.Equals(Environment.GetEnvironmentVariable("TRADING_AGENT_SUBMIT"), "true", StringComparison.OrdinalIgnoreCase))
{
    var outcome = await client.CallToolAsync("exec_submit_intent", new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["symbol"] = symbol,
        ["action"] = decision.Action == TradeAction.Buy ? "buy" : "sell",
        ["sizeFraction"] = decision.SizeFraction,
        ["interval"] = interval,
        ["market"] = market,
    });
    Console.WriteLine("Execution outcome: " + outcome.GetRawText());
}

return 0;

static LlmOptions? TryBuildLlmOptions()
{
    var providerRaw = Environment.GetEnvironmentVariable("TRADING_LLM_PROVIDER");
    if (string.IsNullOrWhiteSpace(providerRaw))
    {
        return null;
    }

    ChatProvider? provider = providerRaw.Trim().ToUpperInvariant() switch
    {
        "ANTHROPIC" or "CLAUDE" => ChatProvider.Anthropic,
        "OPENAI" or "GPT" => ChatProvider.OpenAI,
        _ => null,
    };
    if (provider is null)
    {
        return null;
    }

    var key = Environment.GetEnvironmentVariable("TRADING_LLM_API_KEY")
        ?? Environment.GetEnvironmentVariable(provider == ChatProvider.Anthropic ? "ANTHROPIC_API_KEY" : "OPENAI_API_KEY");
    if (string.IsNullOrWhiteSpace(key))
    {
        return null;
    }

    var model = Environment.GetEnvironmentVariable("TRADING_LLM_MODEL")
        ?? (provider == ChatProvider.Anthropic ? "claude-sonnet-4-6" : "gpt-4.1");
    return LlmOptions.Create(provider.Value, model, key);
}

static MarketSnapshot ParseSnapshot(JsonElement root)
{
    var symbol = root.GetProperty("symbol").GetString() ?? "UNKNOWN";
    var market = ParseEnum<Market>(root.GetProperty("market"));
    var asOf = root.GetProperty("asOfUtc").GetDateTimeOffset();

    var candles = new List<Candle>();
    foreach (var c in root.GetProperty("candles").EnumerateArray())
    {
        candles.Add(new Candle
        {
            Symbol = c.GetProperty("symbol").GetString() ?? symbol,
            Market = ParseEnum<Market>(c.GetProperty("market")),
            Interval = ParseEnum<CandleInterval>(c.GetProperty("interval")),
            OpenTimeUtc = c.GetProperty("openTimeUtc").GetDateTimeOffset(),
            CloseTimeUtc = c.GetProperty("closeTimeUtc").GetDateTimeOffset(),
            Open = c.GetProperty("open").GetDecimal(),
            High = c.GetProperty("high").GetDecimal(),
            Low = c.GetProperty("low").GetDecimal(),
            Close = c.GetProperty("close").GetDecimal(),
            Volume = c.GetProperty("volume").GetDecimal(),
            Source = c.TryGetProperty("source", out var s) ? (s.GetString() ?? "mcp") : "mcp",
        });
    }

    return MarketSnapshot.Create(symbol, market, asOf, candles);
}

static TEnum ParseEnum<TEnum>(JsonElement element)
    where TEnum : struct, Enum
{
    return element.ValueKind == JsonValueKind.Number
        ? (TEnum)Enum.ToObject(typeof(TEnum), element.GetInt32())
        : Enum.Parse<TEnum>(element.GetString() ?? string.Empty, ignoreCase: true);
}

static string? GetOpt(string[] args, string name)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (string.Equals(args[i], name, StringComparison.Ordinal))
        {
            return args[i + 1];
        }
    }

    return null;
}
