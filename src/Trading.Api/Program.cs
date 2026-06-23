using System.Net.Http.Headers;
using System.Text.Json;
using Trading.Agents;
using Trading.Agents.Mcp;
using Trading.Api;

// Web backend for the agent-layer UI. Serves the SPA and exposes a small API: market status/balances
// proxies (over MCP), an SSE crew-run stream, and a guarded execute. LLM keys + the MCP bearer are
// read from the server environment only.

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(_ =>
{
    var url = Environment.GetEnvironmentVariable("TRADING_MCP_URL") ?? "http://localhost:8080/mcp";
    var bearer = Environment.GetEnvironmentVariable("TRADING_BEARER_TOKEN");
    var http = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
    if (!string.IsNullOrWhiteSpace(bearer))
    {
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
    }

    return new DataMcpClient(http, new Uri(url));
});
builder.Services.AddSingleton<ICrewRunner, CrewRunner>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

var api = app.MapGroup("/api");

// Read-only config status so the UI can verify the server is set up. Keys are configured via HA
// add-on options / env — never through the UI — so only their presence is reported, never the value.
api.MapGet("/config", () =>
{
    var llm = AgentEnvironment.TryReadLlmOptions();
    var mcpUrl = Environment.GetEnvironmentVariable("TRADING_MCP_URL") ?? "http://localhost:8080/mcp";
    var bearerSet = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TRADING_BEARER_TOKEN"));
    return Results.Json(new
    {
        llmConfigured = llm is not null,
        provider = llm?.Provider.ToString(),
        model = llm?.Model,
        mcpUrl,
        mcpBearerSet = bearerSet,
    });
});

api.MapGet("/status", (DataMcpClient mcp, CancellationToken ct) => Proxy(() => mcp.GetStatusAsync(ct)));
api.MapGet("/balances", (DataMcpClient mcp, CancellationToken ct) => Proxy(() => mcp.GetBalancesAsync(ct)));
api.MapPost("/execute", (ExecuteRequest req, DataMcpClient mcp, CancellationToken ct) =>
    Proxy(() => mcp.SubmitIntentAsync(
        req.Symbol ?? "BTCUSDT",
        req.Action ?? "hold",
        req.SizeFraction,
        req.Interval ?? "1h",
        req.Market ?? "spot",
        ct)));

// Run the crew and stream the debate as Server-Sent Events: one `message` event per role, then a
// `decision` event (or an `error` event). Messages are written in order (the callback is awaited).
api.MapPost("/run", async (RunRequest req, HttpContext ctx, ICrewRunner runner, CancellationToken ct) =>
{
    ctx.Response.ContentType = "text/event-stream";
    ctx.Response.Headers.CacheControl = "no-cache";

    async Task Write(string eventName, object payload)
    {
        await ctx.Response.WriteAsync($"event: {eventName}\n", ct);
        await ctx.Response.WriteAsync("data: " + JsonSerializer.Serialize(payload, JsonSerializerOptions.Web) + "\n\n", ct);
        await ctx.Response.Body.FlushAsync(ct);
    }

    try
    {
        var decision = await runner.RunAsync(req, (msg, _) => Write("message", msg), ct);
        await Write("decision", DecisionDto.From(decision));
    }
    catch (Exception ex)
    {
        await Write("error", new { message = ex.Message });
    }
});

app.MapGet("/health", () => Results.Text("ok"));
app.MapFallbackToFile("index.html");

await app.RunAsync();

static async Task<IResult> Proxy(Func<Task<JsonElement>> call)
{
    try
    {
        return Results.Json(await call());
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
}

/// <summary>Exposed so integration tests can boot the app via WebApplicationFactory.</summary>
public partial class Program;
