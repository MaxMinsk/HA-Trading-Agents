using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Trading.Agents;
using Trading.Api;
using Trading.Core.Decisions;
using Xunit;

namespace Trading.Tests;

/// <summary>Boots Trading.Api in-memory and checks the SSE run stream + config endpoint (fake crew runner).</summary>
public sealed class ApiIntegrationTests
{
    [Fact]
    public async Task Run_StreamsRoleMessagesThenDecision()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ICrewRunner>();
                services.AddSingleton<ICrewRunner>(new FakeCrewRunner());
            }));
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/run",
            new { symbol = "BTCUSDT", interval = "1h", market = "spot" });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("event: message", body, StringComparison.Ordinal);
        Assert.Contains("event: decision", body, StringComparison.Ordinal);
        Assert.Contains("\"role\":\"analyst\"", body, StringComparison.Ordinal);
        Assert.Contains("\"action\":\"Buy\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Config_RespondsWithReadOnlyStatus()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var json = await client.GetStringAsync("/api/config");

        Assert.Contains("llmConfigured", json, StringComparison.Ordinal);
        Assert.Contains("mcpUrl", json, StringComparison.Ordinal);
        Assert.Contains("mcpBearerSet", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Health_RespondsOk()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var body = await client.GetStringAsync("/health");

        Assert.Equal("ok", body);
    }

    private sealed class FakeCrewRunner : ICrewRunner
    {
        public async Task<TradeDecision> RunAsync(
            RunRequest request,
            Func<CrewMessage, CancellationToken, Task> onMessage,
            CancellationToken cancellationToken)
        {
            await onMessage(new CrewMessage("analyst", "trend is up"), cancellationToken).ConfigureAwait(false);
            await onMessage(new CrewMessage("trader", "buy"), cancellationToken).ConfigureAwait(false);
            return TradeDecision.Create(TradeAction.Buy, 0.2m, 0.7, "uptrend");
        }
    }
}
