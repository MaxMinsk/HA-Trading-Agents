using System.Text.Json;
using Trading.Api;

// Web backend for the agent-layer UI. Serves the SPA and a small API: read/write settings, market
// status/balances proxies (over MCP), an SSE crew-run stream, and a guarded execute. Settings (incl.
// keys) are stored server-side and resolved as: UI settings -> env / HA add-on options -> default.

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddSingleton(_ => new SettingsStore());
builder.Services.AddSingleton<McpClientProvider>();
builder.Services.AddSingleton<ICrewRunner, CrewRunner>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

var api = app.MapGroup("/api");

// Resolved, read-only status (no secrets) — handy for quick checks.
api.MapGet("/config", (SettingsStore settings) =>
{
    var llm = settings.ResolveLlm();
    var (mcpUrl, bearer) = settings.ResolveMcp();
    return Results.Json(new
    {
        llmConfigured = llm is not null,
        provider = llm?.Provider.ToString(),
        model = llm?.Model,
        mcpUrl = mcpUrl.ToString(),
        mcpBearerSet = !string.IsNullOrWhiteSpace(bearer),
    });
});

// Editable settings. GET masks secrets (set + last4); POST applies a patch (null fields unchanged).
api.MapGet("/settings", (SettingsStore settings) => Results.Json(SettingsDto.From(settings)));
api.MapPost("/settings", (SettingsUpdate update, SettingsStore settings) =>
{
    settings.Update(update);
    return Results.Json(SettingsDto.From(settings));
});

api.MapGet("/status", (McpClientProvider mcp, CancellationToken ct) => Proxy(() => mcp.Create().GetStatusAsync(ct)));
api.MapGet("/balances", (McpClientProvider mcp, CancellationToken ct) => Proxy(() => mcp.Create().GetBalancesAsync(ct)));
api.MapPost("/execute", (ExecuteRequest req, McpClientProvider mcp, CancellationToken ct) =>
    Proxy(() => mcp.Create().SubmitIntentAsync(
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
