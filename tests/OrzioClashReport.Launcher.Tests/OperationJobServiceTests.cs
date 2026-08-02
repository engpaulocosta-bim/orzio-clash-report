using OrzioClashReport.Launcher.Application;
using OrzioClashReport.Launcher.Contracts.Jobs;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Contracts.Ports;
using OrzioClashReport.Launcher.Contracts.Settings;

namespace OrzioClashReport.Launcher.Tests;

public sealed class OperationJobServiceTests
{
    private static string Absolute(string name) => Path.Combine(Path.GetTempPath(), name);

    private static QuickReportRequest Report(string output = "report.html") =>
        new("input.xml", Absolute(output));

    [Fact]
    public async Task ASuccessfulRun_RecordsTheProducedFileAsRecent()
    {
        var recorder = new RecordingJournal();
        var recent = new InMemoryRecentItemsStore();
        var gateway = new StubEngineGateway(Success(Absolute("report.html")));
        var service = new OperationJobService(gateway, recorder, recent, new StubFileSystem());

        JobSnapshot snapshot = await service.RunAsync(
            Report(), new StubCollisionPrompt(OutputCollisionResolution.Cancel), null, CancellationToken.None);

        Assert.Equal(JobState.Succeeded, snapshot.State);
        RecentOutputItem item = Assert.Single(recent.Load());
        Assert.Equal("report.html", item.FileName);
    }

    [Fact]
    public async Task AJournalEntry_ExistsWhileRunningAndIsClearedAtTheEnd()
    {
        var recorder = new RecordingJournal();
        var gateway = new StubEngineGateway(Success(Absolute("report.html")));
        gateway.OnExecute = () => Assert.Single(recorder.Active);

        var service = new OperationJobService(
            gateway, recorder, new InMemoryRecentItemsStore(), new StubFileSystem());

        await service.RunAsync(
            Report(), new StubCollisionPrompt(OutputCollisionResolution.Cancel), null, CancellationToken.None);

        Assert.Empty(recorder.Active);
        Assert.Equal(1, recorder.MarkedRunning);
    }

    [Fact]
    public async Task AFailedRun_AlsoClearsTheJournal()
    {
        var recorder = new RecordingJournal();
        var gateway = new StubEngineGateway(Failure(LauncherErrorCode.EngineExecutionFailure));
        var service = new OperationJobService(
            gateway, recorder, new InMemoryRecentItemsStore(), new StubFileSystem());

        JobSnapshot snapshot = await service.RunAsync(
            Report(), new StubCollisionPrompt(OutputCollisionResolution.Cancel), null, CancellationToken.None);

        Assert.Equal(JobState.Failed, snapshot.State);
        Assert.Empty(recorder.Active);
    }

    [Fact]
    public async Task AThrowingGateway_StillClearsTheJournal()
    {
        var recorder = new RecordingJournal();
        var gateway = new StubEngineGateway(Success(Absolute("report.html")))
        {
            OnExecute = () => throw new InvalidOperationException("boom"),
        };
        var service = new OperationJobService(
            gateway, recorder, new InMemoryRecentItemsStore(), new StubFileSystem());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RunAsync(
            Report(), new StubCollisionPrompt(OutputCollisionResolution.Cancel), null, CancellationToken.None));

        Assert.Empty(recorder.Active);
        Assert.False(service.IsBusy);
    }

    [Fact]
    public async Task AnExistingHtmlDestination_AsksBeforeRunning()
    {
        var prompt = new StubCollisionPrompt(OutputCollisionResolution.Cancel);
        var gateway = new StubEngineGateway(Success(Absolute("report.html")));
        var service = new OperationJobService(
            gateway,
            new RecordingJournal(),
            new InMemoryRecentItemsStore(),
            new StubFileSystem(Absolute("report.html")));

        JobSnapshot snapshot = await service.RunAsync(Report(), prompt, null, CancellationToken.None);

        Assert.Equal(1, prompt.Asked);
        Assert.Equal(JobState.Canceled, snapshot.State);
        Assert.Equal(0, gateway.Executions);
    }

    [Fact]
    public async Task ReplacingIsHonouredOnlyWhenAHumanChoosesIt()
    {
        var prompt = new StubCollisionPrompt(OutputCollisionResolution.Replace);
        var gateway = new StubEngineGateway(Success(Absolute("report.html")));
        var service = new OperationJobService(
            gateway,
            new RecordingJournal(),
            new InMemoryRecentItemsStore(),
            new StubFileSystem(Absolute("report.html")));

        JobSnapshot snapshot = await service.RunAsync(Report(), prompt, null, CancellationToken.None);

        Assert.Equal(JobState.Succeeded, snapshot.State);
        Assert.Equal(Absolute("report.html"), gateway.LastRequest!.OutputPath);
    }

    [Fact]
    public async Task ChoosingAnotherName_RedirectsTheRequest()
    {
        string replacement = Absolute("report-2.html");
        var prompt = new StubCollisionPrompt(OutputCollisionResolution.UseDifferentPath(replacement));
        var gateway = new StubEngineGateway(Success(replacement));
        var service = new OperationJobService(
            gateway,
            new RecordingJournal(),
            new InMemoryRecentItemsStore(),
            new StubFileSystem(Absolute("report.html")));

        JobSnapshot snapshot = await service.RunAsync(Report(), prompt, null, CancellationToken.None);

        Assert.Equal(JobState.Succeeded, snapshot.State);
        Assert.Equal(replacement, gateway.LastRequest!.OutputPath);
    }

    [Fact]
    public async Task AnExistingSnapshotDestination_IsNeverOfferedForReplacement()
    {
        string snapshotPath = Absolute("run-001.json");
        var prompt = new StubCollisionPrompt(OutputCollisionResolution.Replace);
        var gateway = new StubEngineGateway(Failure(LauncherErrorCode.EngineExecutionFailure));
        var service = new OperationJobService(
            gateway, new RecordingJournal(), new InMemoryRecentItemsStore(), new StubFileSystem(snapshotPath));

        // The engine refuses to overwrite evidence, so the launcher does not pretend to offer it:
        // the request goes straight through and the engine's own refusal is what the human sees.
        await service.RunAsync(
            new CreateSnapshotRequest("in.xml", "manifest.json", snapshotPath),
            prompt,
            null,
            CancellationToken.None);

        Assert.Equal(0, prompt.Asked);
        Assert.Equal(1, gateway.Executions);
    }

    [Fact]
    public async Task OnlyOneJobRunsPerWindow()
    {
        var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var gateway = new StubEngineGateway(Success(Absolute("report.html"))) { Gate = gate.Task };
        var service = new OperationJobService(
            gateway, new RecordingJournal(), new InMemoryRecentItemsStore(), new StubFileSystem());

        Task<JobSnapshot> first = service.RunAsync(
            Report(), new StubCollisionPrompt(OutputCollisionResolution.Cancel), null, CancellationToken.None);

        await WaitUntil(() => service.IsBusy);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RunAsync(
            Report(), new StubCollisionPrompt(OutputCollisionResolution.Cancel), null, CancellationToken.None));

        gate.SetResult(true);
        await first;

        Assert.False(service.IsBusy);
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        for (int i = 0; i < 200 && !condition(); i++)
        {
            await Task.Delay(10);
        }

        Assert.True(condition());
    }

    private static OperationResult Success(string outputPath) => OperationResult.Success(
        exitCode: 0,
        new[] { new LauncherArtifact(ArtifactKind.HtmlReport, outputPath, 128) },
        warnings: null,
        standardOutput: "done",
        standardError: string.Empty,
        duration: TimeSpan.FromSeconds(1));

    private static OperationResult Failure(LauncherErrorCode code) => OperationResult.Failure(
        new LauncherError(code, "failed"),
        exitCode: 1,
        warnings: null,
        standardOutput: string.Empty,
        standardError: "boom",
        duration: TimeSpan.FromSeconds(1));
}
