using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using Trading.Core.MarketData;
using Trading.Data.Binance;
using Trading.Data.Storage;
using Trading.Mcp;

// Market-data MCP server. HTTP+bearer for a remote/local agent (data-plane on the HA add-on), or stdio
// for a co-located agent. Set TRADING_TRANSPORT=http for the add-on.
const string ServerInstructions =
    "Read-only market-data MCP for a Binance trading agent. Tools: market_snapshot (point-in-time candles, " +
    "no look-ahead — the primary input), market_get_candles (range), market_status (what's stored). " +
    "Times are UTC ISO-8601; intervals are 1h/4h/1d; markets are spot or usdm.";

var transport = Environment.GetEnvironmentVariable("TRADING_TRANSPORT") ?? "stdio";
var dbPath = Environment.GetEnvironmentVariable("TRADING_DB_PATH") ?? Path.Combine("data", "market.sqlite");
EnsureDbDirectory(dbPath);

if (string.Equals(transport, "http", StringComparison.OrdinalIgnoreCase))
{
    var bearer = Environment.GetEnvironmentVariable("TRADING_BEARER_TOKEN");
    if (string.IsNullOrWhiteSpace(bearer)
        && !string.Equals(Environment.GetEnvironmentVariable("ALLOW_UNAUTHENTICATED_HTTP"), "true", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException(
            "Refusing to start HTTP mode without TRADING_BEARER_TOKEN. Set a bearer token, or ALLOW_UNAUTHENTICATED_HTTP=true for local dev only.");
    }

    var builder = WebApplication.CreateBuilder(args);
    RegisterServices(builder.Services, dbPath);
    AddIngestionIfEnabled(builder.Services, defaultEnabled: true);
    builder.Services.AddMcpServer(options => options.ServerInstructions = ServerInstructions)
        .WithHttpTransport(options => options.Stateless = true)
        .WithTools<MarketTools>(StructuredToolJson());

    var app = builder.Build();
    Bootstrap(app.Services);

    var bearerBytes = string.IsNullOrWhiteSpace(bearer) ? null : Encoding.UTF8.GetBytes(bearer);
    app.Use(async (context, next) =>
    {
        if (context.Request.Path == "/health")
        {
            await next().ConfigureAwait(false);
            return;
        }

        if (bearerBytes is not null && !IsAuthorized(context, bearerBytes))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await next().ConfigureAwait(false);
    });

    app.MapGet("/health", () => Results.Text("ok"));
    app.MapMcp("/mcp");
    await app.RunAsync().ConfigureAwait(false);
}
else
{
    var builder = Host.CreateApplicationBuilder(args);
    // stdio carries the protocol on stdout, so logs must go to stderr.
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
    RegisterServices(builder.Services, dbPath);
    AddIngestionIfEnabled(builder.Services, defaultEnabled: false);
    builder.Services.AddMcpServer(options => options.ServerInstructions = ServerInstructions)
        .WithStdioServerTransport()
        .WithTools<MarketTools>(StructuredToolJson());

    var app = builder.Build();
    Bootstrap(app.Services);
    await app.RunAsync().ConfigureAwait(false);
}

static void RegisterServices(IServiceCollection services, string dbPath)
{
    services.AddSingleton(TimeProvider.System);
    services.AddSingleton<IMarketDataStore>(provider =>
        new SqliteMarketDataStore(new SqliteConnectionFactory(dbPath), provider.GetRequiredService<TimeProvider>()));
    services.AddSingleton<ISnapshotBuilder>(provider =>
        new StoredSnapshotBuilder(provider.GetRequiredService<IMarketDataStore>()));
}

static void AddIngestionIfEnabled(IServiceCollection services, bool defaultEnabled)
{
    var flag = Environment.GetEnvironmentVariable("TRADING_INGEST");
    var enabled = flag is null ? defaultEnabled : string.Equals(flag, "true", StringComparison.OrdinalIgnoreCase);
    if (!enabled)
    {
        return;
    }

    var options = IngestionOptions.FromEnvironment();
    services.AddSingleton(options);
    services.AddSingleton<IMarketDataSource>(_ => new BinanceMarketDataSource(
        new HttpClient { Timeout = TimeSpan.FromSeconds(30) },
        market => market == Market.Spot ? options.SpotKlinesUrl : BinanceIntervals.KlinesBaseUrl(market)));
    services.AddSingleton(_ => new BinanceKlineCollector());
    services.AddHostedService<IngestionService>();
}

// Force the SQLite store to construct (running migrations) at startup, before the first tool call.
static void Bootstrap(IServiceProvider services)
{
    _ = services.GetRequiredService<IMarketDataStore>();
}

static bool IsAuthorized(HttpContext context, byte[] bearerBytes)
{
    var header = context.Request.Headers.Authorization.ToString();
    if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    var presented = Encoding.UTF8.GetBytes(header["Bearer ".Length..].Trim());
    return CryptographicOperations.FixedTimeEquals(presented, bearerBytes);
}

static JsonSerializerOptions StructuredToolJson()
{
    return new JsonSerializerOptions(McpJsonUtilities.DefaultOptions) { DefaultIgnoreCondition = JsonIgnoreCondition.Never };
}

static void EnsureDbDirectory(string dbPath)
{
    var dir = Path.GetDirectoryName(Path.GetFullPath(dbPath));
    if (!string.IsNullOrEmpty(dir))
    {
        Directory.CreateDirectory(dir);
    }
}
