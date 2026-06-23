using System.Globalization;
using System.Net.Http.Headers;
using Trading.Agent;
using Trading.Agents;
using Trading.Agents.Mcp;
using Trading.Core.Decisions;
using Trading.Core.Strategies;

// Relocatable agent host. Reaches market data ONLY over MCP (via the shared DataMcpClient), so it
// runs the same locally (TRADING_MCP_URL=https://server/mcp + bearer) or inside the add-on. With an
// LLM provider + key configured it runs the MAF crew (TRD-S5); otherwise it falls back to a plain
// SMA rule so the loop still works without keys (e.g. on the geo-restricted server).

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

var dataClient = new DataMcpClient(http, new Uri(url));
Console.WriteLine($"Agent -> data MCP at {url} ({symbol} {interval} {market})");

var lookback = Math.Max(slow + 5, 80);
var snapshot = await dataClient.GetSnapshotAsync(symbol, interval, market, lookback);
Console.WriteLine($"Got {snapshot.Candles.Count} candles via MCP for {snapshot.Symbol} as of {snapshot.AsOfUtc:O}.");

var llm = AgentEnvironment.TryReadLlmOptions();
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
    var outcome = await dataClient.SubmitIntentAsync(
        symbol, decision.Action == TradeAction.Buy ? "buy" : "sell", decision.SizeFraction, interval, market);
    Console.WriteLine("Execution outcome: " + outcome.GetRawText());
}

return 0;

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
