using OrzioClashReport.Launcher.Contracts.Jobs;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Contracts.Ports;

namespace OrzioClashReport.Launcher.Tests;

internal sealed class StubEngineGateway : IEngineGateway
{
    private readonly OperationResult _result;

    internal StubEngineGateway(OperationResult result)
    {
        _result = result;
    }

    internal int Executions { get; private set; }

    internal LauncherOperationRequest? LastRequest { get; private set; }

    internal Action? OnExecute { get; set; }

    internal Task? Gate { get; set; }

    public async Task<OperationResult> ExecuteAsync(
        LauncherOperationRequest request,
        IProgress<EngineOutputChunk>? progress,
        CancellationToken cancellationToken)
    {
        LastRequest = request;
        Executions++;
        OnExecute?.Invoke();

        if (Gate != null)
        {
            await Gate;
        }

        return _result;
    }
}

internal sealed class RecordingJournal : IJobJournal
{
    private readonly Dictionary<JobId, JobJournalEntry> _entries = new();

    internal int MarkedRunning { get; private set; }

    internal IReadOnlyCollection<JobJournalEntry> Active => _entries.Values;

    public void MarkRunning(JobJournalEntry entry)
    {
        MarkedRunning++;
        _entries[entry.JobId] = entry;
    }

    public void Clear(JobId jobId) => _entries.Remove(jobId);

    public IReadOnlyList<JobJournalEntry> LoadInterrupted() => _entries.Values.ToList();
}

internal sealed class StubFileSystem : IFileSystemProbe
{
    private readonly HashSet<string> _existing;

    internal StubFileSystem(params string[] existing)
    {
        _existing = new HashSet<string>(existing, StringComparer.Ordinal);
    }

    public bool FileExists(string path) => _existing.Contains(path);
}

internal sealed class StubCollisionPrompt : IOutputCollisionPrompt
{
    private readonly OutputCollisionResolution _resolution;

    internal StubCollisionPrompt(OutputCollisionResolution resolution)
    {
        _resolution = resolution;
    }

    internal int Asked { get; private set; }

    public Task<OutputCollisionResolution> ResolveAsync(string existingPath, CancellationToken cancellationToken)
    {
        Asked++;
        return Task.FromResult(_resolution);
    }
}
