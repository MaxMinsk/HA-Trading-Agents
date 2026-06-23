using Microsoft.Extensions.AI;
using Trading.Agents;
using Xunit;

namespace Trading.Tests;

/// <summary>
/// Constructs a real IChatClient for each provider (no network — clients are lazy). Verifies the
/// provider entrypoints and the model-pinning wrapper are wired correctly.
/// </summary>
public sealed class ChatProviderFactoryTests
{
    [Fact]
    public void Create_Anthropic_ReturnsChatClient()
    {
        var client = ChatProviderFactory.Create(
            LlmOptions.Create(ChatProvider.Anthropic, "claude-sonnet-4-6", "dummy-key"));

        Assert.NotNull(client);
        Assert.IsAssignableFrom<IChatClient>(client);
    }

    [Fact]
    public void Create_OpenAI_ReturnsChatClient()
    {
        var client = ChatProviderFactory.Create(
            LlmOptions.Create(ChatProvider.OpenAI, "gpt-4.1", "dummy-key"));

        Assert.NotNull(client);
        Assert.IsAssignableFrom<IChatClient>(client);
    }

    [Fact]
    public void LlmOptions_BlankKey_Throws() =>
        Assert.Throws<ArgumentException>(() => LlmOptions.Create(ChatProvider.Anthropic, "model", ""));
}
