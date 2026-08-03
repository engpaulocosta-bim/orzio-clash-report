using OrzioClashReport.Launcher.Application;
using OrzioClashReport.Launcher.Contracts.Engine;
using OrzioClashReport.Launcher.Contracts.Jobs;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Contracts.Ports;
using OrzioClashReport.Launcher.Contracts.Settings;
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
    public void NothingClaimsToBeReady_UntilItActuallyIs()
    {
        QuickReportViewModel viewModel = Create(out _, out _);

        using (LanguageScope.Use(InterfaceLanguage.Portuguese))
        {
            Assert.Equal("Faltam dados para executar.", viewModel.ActionHint);
        }

        using (LanguageScope.Use(InterfaceLanguage.English))
        {
            Assert.Equal("Something is still missing before this can run.", viewModel.ActionHint);
        }

        viewModel.InputXmlPath = Absolute("input.xml");

        // One of the two is not both.
        using (LanguageScope.Use(InterfaceLanguage.Portuguese))
        {
            Assert.Equal("Faltam dados para executar.", viewModel.ActionHint);
        }

        viewModel.OutputHtmlPath = Absolute("report.html");

        using (LanguageScope.Use(InterfaceLanguage.Portuguese))
        {
            Assert.Equal("Pronto para executar.", viewModel.ActionHint);
        }
    }

    [Fact]
    public void AnUnusableEngineIsNeverDescribedAsReady()
    {
        QuickReportViewModel viewModel = Create(out _, out _, EngineProbeResult.Missing("no engine"));
        viewModel.InputXmlPath = Absolute("input.xml");
        viewModel.OutputHtmlPath = Absolute("report.html");

        using LanguageScope scope = LanguageScope.Use(InterfaceLanguage.Portuguese);

        Assert.False(viewModel.GenerateCommand.CanExecute(null));
        Assert.Equal("Faltam dados para executar.", viewModel.ActionHint);
    }

    [Fact]
    public async Task OnceSomethingHasRun_TheHintIsTheOutcome()
    {
        QuickReportViewModel viewModel = Create(out _, out _);
        viewModel.InputXmlPath = Absolute("input.xml");
        viewModel.OutputHtmlPath = Absolute("report.html");

        await viewModel.GenerateCommand.ExecuteAsync(null);

        using LanguageScope scope = LanguageScope.Use(InterfaceLanguage.Portuguese);

        Assert.Equal("Concluído.", viewModel.ActionHint);
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
        await OnAProgressDeliveringThread(async () =>
        {
            var jobs = new OperationJobService(
                new StreamingGateway(), new RecordingJournal(), new InMemoryRecentItemsStore(), new StubFileSystem());
            var runner = new OperationRunnerViewModel(
                jobs, new StubCollisionPrompt(OutputCollisionResolution.Cancel), new NoOpOutputRevealer());

            await runner.RunAsync(new QuickReportRequest("input.xml", Absolute("report.html")));

            Assert.Contains("12 raw clashes -> 4 groups", runner.OutputLines);
            Assert.Contains("! a diagnostic line", runner.OutputLines);
        });
    }

    [Fact]
    public async Task TheRunnerKeepsOnlyTheTailOfAVeryLoudEngine()
    {
        await OnAProgressDeliveringThread(async () =>
        {
            var jobs = new OperationJobService(
                new ChattyGateway(OperationRunnerViewModel.MaximumVisibleLines * 3),
                new RecordingJournal(),
                new InMemoryRecentItemsStore(),
                new StubFileSystem());
            var runner = new OperationRunnerViewModel(
                jobs, new StubCollisionPrompt(OutputCollisionResolution.Cancel), new NoOpOutputRevealer());

            await runner.RunAsync(new QuickReportRequest("input.xml", Absolute("report.html")));

            Assert.Equal(OperationRunnerViewModel.MaximumVisibleLines, runner.OutputLines.Count);
        });
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

        // The message comes from the error code, so it reads in whichever language is in use.
        using (LanguageScope.Use(InterfaceLanguage.Portuguese))
        {
            Assert.Equal("O motor terminou com um erro.", runner.ErrorMessage);
        }

        using (LanguageScope.Use(InterfaceLanguage.English))
        {
            Assert.Equal("The engine ended with an error.", runner.ErrorMessage);
        }

        // The engine's own words are shown as-is and are never translated.
        Assert.Contains("could not be parsed", runner.ErrorDetail!, StringComparison.Ordinal);
    }

    /// <summary>
    /// Runs the body on a thread whose synchronization context delivers a posted callback there and
    /// then, which is what the user interface thread does from the runner's point of view: by the
    /// time the operation has finished, everything the engine printed has already been shown.
    /// Without one, <see cref="Progress{T}"/> hands the callback to the thread pool and the
    /// assertions would be racing it.
    /// </summary>
    private static async Task OnAProgressDeliveringThread(Func<Task> body)
    {
        SynchronizationContext? previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(new DeliverHereAndNowContext());
        try
        {
            await body().ConfigureAwait(true);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    private sealed class DeliverHereAndNowContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback callback, object? state) => callback(state);

        public override void Send(SendOrPostCallback callback, object? state) => callback(state);

        public override SynchronizationContext CreateCopy() => this;
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
