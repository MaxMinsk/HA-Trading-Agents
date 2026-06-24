using Trading.Agents;
using Trading.Api;
using Xunit;

namespace Trading.Tests;

/// <summary>The settings store persists config and resolves it (store -&gt; env), masking secrets on read.</summary>
public sealed class SettingsStoreTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"trd-set-{Guid.NewGuid():N}.json");

    [Fact]
    public void Update_PersistsAndReloadsFromDisk()
    {
        var store = new SettingsStore(_path);
        store.Update(new SettingsUpdate("http://x:8080/mcp", "btok", "anthropic", "claude-sonnet-4-6", "sk-test"));

        var reloaded = new SettingsStore(_path);
        Assert.Equal("http://x:8080/mcp", reloaded.Current.McpUrl);
        Assert.Equal("sk-test", reloaded.Current.LlmApiKey);
    }

    [Fact]
    public void ResolveMcp_PrefersStoredValue()
    {
        var store = new SettingsStore(_path);
        store.Update(new SettingsUpdate("http://host:9000/mcp", null, null, null, null));

        var (url, _) = store.ResolveMcp();
        Assert.Equal("http://host:9000/mcp", url.ToString().TrimEnd('/'));
    }

    [Fact]
    public void ResolveLlm_UsesStoreWhenProviderAndKeySet()
    {
        var store = new SettingsStore(_path);
        Assert.Null(store.ResolveLlm());

        store.Update(new SettingsUpdate(null, null, "anthropic", "claude-x", "sk-test"));
        var llm = store.ResolveLlm();

        Assert.NotNull(llm);
        Assert.Equal(ChatProvider.Anthropic, llm!.Provider);
        Assert.Equal("claude-x", llm.Model);
    }

    [Fact]
    public void SecretStatus_MasksTheValue()
    {
        Assert.False(SecretStatus.Of(null).Set);
        var status = SecretStatus.Of("abcdef");
        Assert.True(status.Set);
        Assert.Equal("…cdef", status.Hint);
    }

    public void Dispose()
    {
        if (File.Exists(_path))
        {
            File.Delete(_path);
        }
    }
}
