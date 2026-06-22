using System.Text;
using System.Text.Json;

namespace Trading.Agent;

/// <summary>
/// Minimal JSON-RPC MCP client over streamable HTTP: calls a tool and returns its structured result.
/// A thin stand-in for the agent's MCP integration until the MAF workflow (TRD-004) takes over.
/// </summary>
internal sealed class McpHttpClient(HttpClient http, string url)
{
    private readonly HttpClient _http = http;
    private readonly Uri _endpoint = new(url);
    private int _id;

    public async Task<JsonElement> CallToolAsync(
        string name,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        var rpc = new
        {
            jsonrpc = "2.0",
            id = Interlocked.Increment(ref _id),
            method = "tools/call",
            @params = new { name, arguments },
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(rpc), Encoding.UTF8, "application/json"),
        };
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.Accept.ParseAdd("text/event-stream");

        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        using var document = JsonDocument.Parse(ExtractJson(body));
        var root = document.RootElement;
        if (root.TryGetProperty("error", out var error))
        {
            throw new InvalidOperationException($"MCP error: {error.GetRawText()}");
        }

        var result = root.GetProperty("result");
        return result.TryGetProperty("structuredContent", out var structured)
            ? structured.Clone()
            : result.Clone();
    }

    // Streamable HTTP may answer with SSE ("data: {json}"); take the last data payload. Plain JSON passes through.
    private static string ExtractJson(string body)
    {
        var trimmed = body.TrimStart();
        if (trimmed.StartsWith('{') || trimmed.StartsWith('['))
        {
            return body;
        }

        string? last = null;
        foreach (var line in body.Split('\n'))
        {
            if (line.StartsWith("data:", StringComparison.Ordinal))
            {
                last = line["data:".Length..].Trim();
            }
        }

        return last ?? body;
    }
}
