using System.Text;
using System.Text.Json;

namespace Trading.Agents.Mcp;

/// <summary>
/// Minimal JSON-RPC MCP client over streamable HTTP: calls a tool and returns its structured result.
/// Shared by the CLI host and the web backend so both reach the data/execution MCP the same way.
/// </summary>
public sealed class McpHttpClient
{
    private readonly HttpClient _http;
    private readonly Uri _endpoint;
    private int _id;

    /// <summary>Creates the client over an HTTP client and the MCP endpoint (e.g. http://host:8080/mcp).</summary>
    /// <param name="http">HTTP client (set its Authorization header for a bearer-protected endpoint).</param>
    /// <param name="endpoint">The /mcp endpoint URI.</param>
    public McpHttpClient(HttpClient http, Uri endpoint)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(endpoint);
        _http = http;
        _endpoint = endpoint;
    }

    /// <summary>Calls an MCP tool and returns its structured content (or the raw result if none).</summary>
    /// <param name="name">Tool name.</param>
    /// <param name="arguments">Tool arguments.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<JsonElement> CallToolAsync(
        string name,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(arguments);

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
