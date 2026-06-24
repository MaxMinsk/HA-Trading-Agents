using System.Net.Http.Headers;
using Trading.Agents.Mcp;

namespace Trading.Api;

/// <summary>
/// Builds a <see cref="DataMcpClient"/> per request from the current settings, so a URL/bearer changed
/// in the UI takes effect without a restart. Uses the pooled HttpClient factory.
/// </summary>
public sealed class McpClientProvider(IHttpClientFactory httpFactory, SettingsStore settings)
{
    public DataMcpClient Create()
    {
        var (url, bearer) = settings.ResolveMcp();
        var http = httpFactory.CreateClient("mcp");
        http.Timeout = TimeSpan.FromMinutes(2);
        if (!string.IsNullOrWhiteSpace(bearer))
        {
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        }

        return new DataMcpClient(http, url);
    }
}
