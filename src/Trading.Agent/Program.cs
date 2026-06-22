using System.Globalization;
using System.Net.Http.Headers;
using Trading.Agent;
using Trading.Core.Decisions;

// Relocatable agent host (skeleton). Reaches market data ONLY over MCP, so it runs the same whether
// it sits locally (TRADING_MCP_URL=https://server/mcp + bearer) or inside the add-on
// (TRADING_MCP_URL=http://localhost:8080/mcp). The real decision logic (MAF multi-agent) lands in TRD-004;
// here we run a simple SMA rule on the MCP snapshot to prove the loop end-to-end.

var url = Environment.GetEnvironmentVariable("TRADING_MCP_URL") ?? "http://localhost:8080/mcp";
var bearer = Environment.GetEnvironmentVariable("TRADING_BEARER_TOKEN");
var symbol = GetOpt(args, "--symbol") ?? "BTCUSDT";
var interval = GetOpt(args, "--interval") ?? "1h";
var fast = int.Parse(GetOpt(args, "--fast") ?? "20", CultureInfo.InvariantCulture);
var slow = int.Parse(GetOpt(args, "--slow") ?? "50", CultureInfo.InvariantCulture);

using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
if (!string.IsNullOrWhiteSpace(bearer))
{
    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
}

var client = new McpHttpClient(http, url);
Console.WriteLine($"Agent -> data MCP at {url}  ({symbol} {interval}, sma {fast}/{slow})");

var snapshot = await client.CallToolAsync("market_snapshot", new Dictionary<string, object?>(StringComparer.Ordinal)
{
    ["symbol"] = symbol,
    ["interval"] = interval,
    ["lookback"] = slow + 5,
});

var closes = new List<decimal>();
foreach (var candle in snapshot.GetProperty("candles").EnumerateArray())
{
    closes.Add(candle.GetProperty("close").GetDecimal());
}

Console.WriteLine($"Got {closes.Count} candles via MCP.");
var decision = Decide(closes, fast, slow);
Console.WriteLine($"Decision: {decision.Action} size {decision.SizeFraction} — {decision.Rationale}");
return 0;

static TradeDecision Decide(List<decimal> closes, int fast, int slow)
{
    if (closes.Count < slow)
    {
        return TradeDecision.Create(TradeAction.Hold, 0m, 0.5, "warming up");
    }

    var fastSma = Sma(closes, fast);
    var slowSma = Sma(closes, slow);
    return fastSma > slowSma
        ? TradeDecision.Create(TradeAction.Buy, 1m, 0.6, $"fast {fastSma:0.##} > slow {slowSma:0.##}")
        : TradeDecision.Create(TradeAction.Sell, 0m, 0.6, $"fast {fastSma:0.##} <= slow {slowSma:0.##}");
}

static decimal Sma(List<decimal> closes, int period)
{
    var sum = 0m;
    for (var i = closes.Count - period; i < closes.Count; i++)
    {
        sum += closes[i];
    }

    return sum / period;
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
