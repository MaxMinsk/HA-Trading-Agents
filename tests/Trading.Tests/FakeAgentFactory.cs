using Trading.Agents;

namespace Trading.Tests;

/// <summary>
/// Drives the crew in tests without any network or keys: each role's reply is produced by a scripted
/// function keyed on the role name. Records which roles were created so the pipeline can be asserted.
/// </summary>
internal sealed class FakeAgentFactory(Func<string, string, string> respond) : IAgentFactory
{
    public List<string> CreatedRoles { get; } = [];

    public IChatAgent Create(string name, string instructions)
    {
        CreatedRoles.Add(name);
        return new FakeChatAgent(name, respond);
    }

    private sealed class FakeChatAgent(string name, Func<string, string, string> respond) : IChatAgent
    {
        public string Name { get; } = name;

        public Task<string> RunAsync(string input, CancellationToken cancellationToken = default) =>
            Task.FromResult(respond(Name, input));
    }
}
