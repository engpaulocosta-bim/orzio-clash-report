using OrzioClashReport.Launcher.Contracts.Engine;
using OrzioClashReport.Launcher.Contracts.Ports;

namespace OrzioClashReport.Launcher.Tests;

internal sealed class StubEngineProbe : IEngineProbe
{
    private readonly EngineProbeResult _result;

    internal StubEngineProbe(EngineProbeResult result)
    {
        _result = result;
    }

    internal int ProbeCount { get; private set; }

    public Task<EngineProbeResult> ProbeAsync(CancellationToken cancellationToken)
    {
        ProbeCount++;
        return Task.FromResult(_result);
    }
}

internal sealed class InMemoryRecentItemsStore : IRecentItemsStore
{
    private readonly List<Contracts.Settings.RecentOutputItem> _items = new();

    internal InMemoryRecentItemsStore(params Contracts.Settings.RecentOutputItem[] items)
    {
        _items.AddRange(items);
    }

    public IReadOnlyList<Contracts.Settings.RecentOutputItem> Load() => _items;

    public void Add(Contracts.Settings.RecentOutputItem item) => _items.Insert(0, item);
}

internal sealed class NoOpOutputRevealer : IOutputRevealer
{
    internal List<string> Opened { get; } = new();

    internal List<string> Revealed { get; } = new();

    public Task OpenAsync(string path)
    {
        Opened.Add(path);
        return Task.CompletedTask;
    }

    public Task RevealAsync(string path)
    {
        Revealed.Add(path);
        return Task.CompletedTask;
    }
}
