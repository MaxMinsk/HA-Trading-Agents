using System.Net;
using System.Text;
using System.Text.Json;
using Trading.Agents.Mcp;
using Trading.Core.MarketData;
using Xunit;

namespace Trading.Tests;

/// <summary>Snapshot parsing is shared by the CLI and the web backend, so its decoding is pinned here.</summary>
public sealed class DataMcpClientTests
{
    private const string SnapshotJson = """
    {"symbol":"BTCUSDT","market":"Spot","asOfUtc":"2026-06-22T12:00:00+00:00","candles":[
      {"symbol":"BTCUSDT","market":"Spot","interval":"OneHour","openTimeUtc":"2026-06-22T10:00:00+00:00","closeTimeUtc":"2026-06-22T11:00:00+00:00","open":100,"high":110,"low":90,"close":105,"volume":1.5,"source":"test"},
      {"symbol":"BTCUSDT","market":"Spot","interval":"OneHour","openTimeUtc":"2026-06-22T11:00:00+00:00","closeTimeUtc":"2026-06-22T12:00:00+00:00","open":105,"high":120,"low":100,"close":118,"volume":2.0,"source":"test"}
    ]}
    """;

    [Fact]
    public void ParseSnapshot_BuildsDomainSnapshot()
    {
        using var doc = JsonDocument.Parse(SnapshotJson);

        var snapshot = DataMcpClient.ParseSnapshot(doc.RootElement);

        Assert.Equal("BTCUSDT", snapshot.Symbol);
        Assert.Equal(Market.Spot, snapshot.Market);
        Assert.Equal(2, snapshot.Candles.Count);
        Assert.Equal(CandleInterval.OneHour, snapshot.Candles[0].Interval);
        Assert.Equal(118m, snapshot.Candles[^1].Close);
    }

    [Fact]
    public void ParseSnapshot_AcceptsNumericEnums()
    {
        const string json = """{"symbol":"ETHUSDT","market":0,"asOfUtc":"2026-06-22T12:00:00+00:00","candles":[]}""";
        using var doc = JsonDocument.Parse(json);

        var snapshot = DataMcpClient.ParseSnapshot(doc.RootElement);

        Assert.Equal(Market.Spot, snapshot.Market); // 0 == Spot
        Assert.Empty(snapshot.Candles);
    }

    [Fact]
    public async Task GetSnapshotAsync_ParsesSseWrappedRpcResponse()
    {
        // Real MCP SSE puts the JSON on a single `data:` line, so compact it (no embedded newlines).
        using var snap = JsonDocument.Parse(SnapshotJson);
        var compact = JsonSerializer.Serialize(snap.RootElement);
        var rpc = "data: {\"jsonrpc\":\"2.0\",\"id\":1,\"result\":{\"structuredContent\":" + compact + "}}\n";
        using var http = new HttpClient(new StubHandler(rpc));
        var client = new DataMcpClient(http, new Uri("http://localhost/mcp"));

        var snapshot = await client.GetSnapshotAsync("BTCUSDT", "1h", "spot", 10);

        Assert.Equal(2, snapshot.Candles.Count);
        Assert.Equal("BTCUSDT", snapshot.Symbol);
    }

    private sealed class StubHandler(string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "text/event-stream"),
            });
    }
}
