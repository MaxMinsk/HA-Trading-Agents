using System.Text.Json;
using Trading.Agents;

namespace Trading.Api;

/// <summary>
/// Server-side settings persisted to a JSON file (default data/settings.json, or TRADING_SETTINGS_PATH).
/// Lets the UI configure tokens/provider/model/MCP without restarts; effective config is the stored
/// value, then the environment, then a default. Secrets live only here (gitignored) and are masked
/// when read back via <see cref="SettingsDto"/>.
/// </summary>
public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions FileJson = new() { WriteIndented = true };

    private readonly string _path;
    private readonly Lock _lock = new();
    private StoredSettings _current;

    public SettingsStore(string? path = null)
    {
        _path = path
            ?? Environment.GetEnvironmentVariable("TRADING_SETTINGS_PATH")
            ?? Path.Combine("data", "settings.json");
        _current = Load(_path);
    }

    public StoredSettings Current
    {
        get
        {
            lock (_lock)
            {
                return _current;
            }
        }
    }

    /// <summary>Applies a patch (null fields unchanged) and persists.</summary>
    public StoredSettings Update(SettingsUpdate patch)
    {
        ArgumentNullException.ThrowIfNull(patch);
        lock (_lock)
        {
            _current = new StoredSettings
            {
                McpUrl = patch.McpUrl ?? _current.McpUrl,
                McpBearer = patch.McpBearer ?? _current.McpBearer,
                LlmProvider = patch.LlmProvider ?? _current.LlmProvider,
                LlmModel = patch.LlmModel ?? _current.LlmModel,
                LlmApiKey = patch.LlmApiKey ?? _current.LlmApiKey,
            };
            Save(_path, _current);
            return _current;
        }
    }

    /// <summary>Effective MCP endpoint + bearer: stored value, else environment, else default.</summary>
    public (Uri Url, string? Bearer) ResolveMcp()
    {
        var settings = Current;
        var url = Blank(settings.McpUrl)
            ?? Environment.GetEnvironmentVariable("TRADING_MCP_URL")
            ?? "http://localhost:8080/mcp";
        var bearer = Blank(settings.McpBearer) ?? Environment.GetEnvironmentVariable("TRADING_BEARER_TOKEN");
        return (new Uri(url), bearer);
    }

    /// <summary>Effective LLM options from stored settings, else the environment; null if none configured.</summary>
    public LlmOptions? ResolveLlm(string? providerOverride = null, string? modelOverride = null)
    {
        var settings = Current;
        var providerName = providerOverride ?? settings.LlmProvider;
        var model = modelOverride ?? settings.LlmModel;

        var provider = ParseProvider(providerName);
        return provider is not null && !string.IsNullOrWhiteSpace(settings.LlmApiKey)
            ? LlmOptions.Create(provider.Value, Blank(model) ?? DefaultModel(provider.Value), settings.LlmApiKey)
            : AgentEnvironment.TryReadLlmOptions(providerName, model);
    }

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static ChatProvider? ParseProvider(string? raw) => raw?.Trim().ToUpperInvariant() switch
    {
        "ANTHROPIC" or "CLAUDE" => ChatProvider.Anthropic,
        "OPENAI" or "GPT" => ChatProvider.OpenAI,
        _ => null,
    };

    private static string DefaultModel(ChatProvider provider) =>
        provider == ChatProvider.Anthropic ? "claude-sonnet-4-6" : "gpt-4.1";

    private static StoredSettings Load(string path)
    {
        try
        {
            return !File.Exists(path)
                ? new StoredSettings()
                : JsonSerializer.Deserialize<StoredSettings>(File.ReadAllText(path), FileJson) ?? new StoredSettings();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return new StoredSettings();
        }
    }

    private static void Save(string path, StoredSettings settings)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(path, JsonSerializer.Serialize(settings, FileJson));
    }
}
