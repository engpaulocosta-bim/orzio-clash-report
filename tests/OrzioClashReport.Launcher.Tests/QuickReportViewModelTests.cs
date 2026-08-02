using OrzioClashReport.Launcher.Application;
using OrzioClashReport.Launcher.Contracts.Engine;
using OrzioClashReport.Launcher.Contracts.Jobs;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Contracts.Ports;
using OrzioClashReport.Launcher.Desktop.Platform;
using OrzioClashReport.Launcher.Desktop.ViewModels;

namespace OrzioClashReport.Launcher.Tests;

public sealed class QuickReportViewModelTests
{
    private static string Absolute(string name) => Path.Combine(Path.GetTempPath(), name);

    private static QuickReportViewModel Create(
        out StubEngineGateway gateway,
        out StubFileDialogs dialogs,
        EngineProbeResult? engineState = null)
    {
        gateway = new StubEngineGateway(OperationResult.Success(
            exitCode: 0,
            new[] { new LauncherArtifact(ArtifactKind.HtmlReport, Absolute("report.html"), 128) },
            warnings: null,
            standardOutput: "12 raw clashes -> 4 groups",
            standardError: string.Empty,
            duration: TimeSpan.FromSeconds(1)));

        dialogs = new StubFileDialogs();

        var jobs = new OperationJobService(
            gateway, new RecordingJournal(), new InMemoryRecentItemsStore(), new StubFileSystem());

        var runner = new OperationRunnerViewModel(
            jobs, new StubCollisionPrompt(OutputCollisionResolution.Cancel), new NoOpOutputRevealer());

        var engine = new EngineStatusViewModel(
            new StubEngineProbe(engineState ?? EngineProbeResult.Ready("0.1.0-preview.3")));
        engine.RefreshAsync(CancellationToken.None).GetAwaiter().GetResult();

        return new QuickReportViewModel(runner, dialogs, engine);
    }

    [Fact]
    public void GenerateIsUnavailable_UntilBothFilesAreChosen()
    {
        QuickReportViewModel viewModel = Create(out _, out _);

        Assert.False(viewModel.GenerateCommand.CanExecute(null));

        viewModel.InputXmlPath = Absolute("input.xml");
        viewModel.OutputHtmlPath = Absolute("report.html");

        Assert.True(viewModel.GenerateCommand.CanExecute(null));
    }

    [Fact]
    public void GenerateIsUnavailable_WhileTheEngineIsNotUsable()
    {
        QuickReportViewModel viewModel = Create(out _, out _, EngineProbeResult.Missing("no engine"));

        viewModel.InputXmlPath = Absolute("input.xml");
        viewModel.OutputHtmlPath = Absolute("report.html");

        Assert.False(viewModel.GenerateCommand.CanExecute(null));
        Assert.True(viewModel.IsBlocked);
        Assert.NotNull(viewModel.BlockedReason);
    }

    [Fact]
    public async Task ChoosingAnExport_ProposesADestinationBesideIt()
    {
        QuickReportViewModel viewModel = Create(out _, out StubFileDialogs dialogs);
        dialogs.OpenResult = Path.Combine(Path.GetTempPath(), "clash-export.xml");

        await viewModel.PickInputCommand.ExecuteAsync(null);

        Assert.Equal("clash-export.xml", viewModel.InputFileName);
        Assert.Equal("clash-export.report.html", viewModel.OutputFileName);
    }

    [Fact]
    public async Task AChosenDestination_IsNeverSilentlyReplacedByASuggestion()
    {
        QuickReportViewModel viewModel = Create(out _, out StubFileDialogs dialogs);
        viewModel.OutputHtmlPath = Absolute("chosen.html");

        dialogs.OpenResult = Path.Combine(Path.GetTempPath(), "clash-export.xml");
        await viewModel.PickInputCommand.ExecuteAsync(null);

        Assert.Equal("chosen.html", viewModel.OutputFileName);
    }

    [Fact]
    public void DroppingAnExport_IsTheSameAsChoosingIt()
    {
        QuickReportViewModel viewModel = Create(out _, out _);

        viewModel.AcceptDroppedInput(Path.Combine(Path.GetTempPath(), "dropped.xml"));

        Assert.Equal("dropped.xml", viewModel.InputFileName);
        Assert.Equal("dropped.report.html", viewModel.OutputFileName);
    }

    [Fact]
    public void DroppingSomethingThatIsNotAnExport_IsIgnored()
    {
        QuickReportViewModel viewModel = Create(out _, out _);

        viewModel.AcceptDroppedInput(Path.Combine(Path.GetTempPath(), "notes.txt"));

        Assert.Null(viewModel.InputFileName);
    }

    [Fact]
    public async Task Generating_SendsTheTypedRequestAndPresentsTheResult()
    {
        QuickReportViewModel viewModel = Create(out StubEngineGateway gateway, out _);
        viewModel.InputXmlPath = Absolute("input.xml");
        viewModel.OutputHtmlPath = Absolute("report.html");

        await viewModel.GenerateCommand.ExecuteAsync(null);

        var request = Assert.IsType<QuickReportRequest>(gateway.LastRequest);
        Assert.Equal(Absolute("input.xml"), request.InputXmlPath);
        Assert.Equal(Absolute("report.html"), request.OutputHtmlPath);

        Assert.True(viewModel.Runner.Succeeded);
        Assert.True(viewModel.Runner.HasProducedFile);
        Assert.Equal("report.html", viewModel.Runner.ProducedFileName);
    }

    [Fact]
    public async Task TheRunner_ShowsTheEnginesOwnLinesWithoutInterpretingThem()
    {
        var jobs = new OperationJobService(
            new StreamingGateway(), new RecordingJournal(), new InMemoryRecentItemsStore(), new StubFileSystem());
        var runner = new OperationRunnerViewModel(
            jobs, new StubCollisionPrompt(OutputCollisionResolution.Cancel), new NoOpOutputRevealer());

        await runner.RunAsync(new QuickReportRequest("input.xml", Absolute("report.html")));

        // Progress is delivered asynchronously, exactly as it is when marshalled to the user
        // interface thread, so the assertion waits for it rather than assuming it already landed.
        await WaitUntil(() => runner.OutputLines.Count >= 2);

        Assert.Contains("12 raw clashes -> 4 groups", runner.OutputLines);
        Assert.Contains("! a diagnostic line", runner.OutputLines);
    }

    [Fact]
    public async Task TheRunnerKeepsOnlyTheTailOfAVeryLoudEngine()
    {
        var jobs = new OperationJobService(
            new ChattyGateway(OperationRunnerViewModel.MaximumVisibleLines * 3),
            new RecordingJournal(),
            new InMemoryRecentItemsStore(),
            new StubFileSystem());
        var runner = new OperationRunnerViewModel(
            jobs, new StubCollisionPrompt(OutputCollisionResolution.Cancel), new NoOpOutputRevealer());

        await runner.RunAsync(new QuickReportRequest("input.xml", Absolute("report.html")));

        await WaitUntil(() => runner.OutputLines.Count >= OperationRunnerViewModel.MaximumVisibleLines);

        Assert.Equal(OperationRunnerViewModel.MaximumVisibleLines, runner.OutputLines.Count);
    }

    [Fact]
    public async Task AFailedRun_ShowsAnActionableMessageAndNoProducedFile()
    {
        var gateway = new StubEngineGateway(OperationResult.Failure(
            new LauncherError(
                LauncherErrorCode.EngineExecutionFailure,
                "O motor terminou com um erro.",
                "Failed to generate report: the export could not be parsed."),
            exitCode: 1,
            warnings: null,
            standardOutput: string.Empty,
            standardError: "boom",
            duration: TimeSpan.FromSeconds(1)));

        var jobs = new OperationJobService(
            gateway, new RecordingJournal(), new InMemoryRecentItemsStore(), new StubFileSystem());
        var runner = new OperationRunnerViewModel(
            jobs, new StubCollisionPrompt(OutputCollisionResolution.Cancel), new NoOpOutputRevealer());

        await runner.RunAsync(new QuickReportRequest("input.xml", Absolute("report.html")));

        Assert.True(runner.Failed);
        Assert.True(runner.IsCritical);
        Assert.False(runner.HasProducedFile);
        Assert.Equal("O motor terminou com um erro.", runner.ErrorMessage);
        Assert.Contains("could not be parsed", runner.ErrorDetail!, StringComparison.Ordinal);
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        for (int i = 0; i < 300 && !condition(); i++)
        {
            await Task.Delay(10);
        }

        Assert.True(condition());
    }

    private sealed class StreamingGateway : IEngineGateway
    {
        public Task<OperationResult> ExecuteAsync(
            LauncherOperationRequest request,
            IProgress<EngineOutputChunk>? progress,
            CancellationToken cancellationToken)
        {
            progress?.Report(new EngineOutputChunk(EngineOutputStream.StandardOutput, "12 raw clashes -> 4 groups"));
            progress?.Report(new EngineOutputChunk(EngineOutputStream.StandardError, "a diagnostic line"));

            return Task.FromResult(OperationResult.Success(
                exitCode: 0,
                artifacts: null,
                warnings: null,
                standardOutput: string.Empty,
                standardError: string.Empty,
                duration: TimeSpan.Zero));
        }
    }

    private sealed class ChattyGateway : IEngineGateway
    {
        private readonly int _lines;

        internal ChattyGateway(int lines)
        {
            _lines = lines;
        }

        public Task<OperationResult> ExecuteAsync(
            LauncherOperationRequest request,
            IProgress<EngineOutputChunk>? progress,
            CancellationToken cancellationToken)
        {
            for (int i = 0; i < _lines; i++)
            {
                progress?.Report(new EngineOutputChunk(EngineOutputStream.StandardOutput, "line " + i));
            }

            return Task.FromResult(OperationResult.Success(
                exitCode: 0,
                artifacts: null,
                warnings: null,
                standardOutput: string.Empty,
                standardError: string.Empty,
                duration: TimeSpan.Zero));
        }
    }
}

internal sealed class StubFileDialogs : IFileDialogs
{
    internal string? OpenResult { get; set; }

    internal string? SaveResult { get; set; }

    internal IReadOnlyList<string> OpenManyResult { get; set; } = Array.Empty<string>();

    public Task<string?> PickOpenFileAsync(string title, PickedFileKind kind) => Task.FromResult(OpenResult);

    public Task<IReadOnlyList<string>> PickOpenFilesAsync(string title, PickedFileKind kind) =>
        Task.FromResult(OpenManyResult);

    public Task<string?> PickSaveFileAsync(string title, PickedFileKind kind, string suggestedFileName) =>
        Task.FromResult(SaveResult);
}
