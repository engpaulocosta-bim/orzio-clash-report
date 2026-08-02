using System.IO.Compression;
using System.Text.Json;
using OrzioClashReport.Launcher.Application;
using OrzioClashReport.Launcher.Contracts.Diagnostics;
using OrzioClashReport.Launcher.Contracts.Engine;
using OrzioClashReport.Launcher.Contracts.Jobs;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Contracts.Ports;
using OrzioClashReport.Launcher.Contracts.Settings;
using OrzioClashReport.Launcher.Desktop.ViewModels;
using OrzioClashReport.Launcher.Infrastructure.Diagnostics;
using OrzioClashReport.Launcher.Infrastructure.Storage;

namespace OrzioClashReport.Launcher.Tests;

public sealed class DiagnosticsAndRecoveryTests : IDisposable
{
    private readonly string _root;

    public DiagnosticsAndRecoveryTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "orzio-diag-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private string JobsDirectory => Path.Combine(_root, "jobs");

    private string LogsDirectory => Path.Combine(_root, "logs");

    // ---- Job journal -------------------------------------------------------

    [Fact]
    public void AJournalFile_ExistsWhileRunningAndIsNamedAfterTheJob()
    {
        var journal = new FileJobJournal(JobsDirectory);
        JobId jobId = JobId.New();

        journal.MarkRunning(new JobJournalEntry(jobId, LauncherOperationKind.QuickReport, DateTimeOffset.UnixEpoch));

        Assert.True(File.Exists(Path.Combine(JobsDirectory, jobId.Value + ".json")));

        journal.Clear(jobId);

        Assert.False(File.Exists(Path.Combine(JobsDirectory, jobId.Value + ".json")));
        Assert.Empty(journal.LoadInterrupted());
    }

    [Fact]
    public void ALeftoverJournalRecord_IsReportedAsAnInterruptedOperation()
    {
        var journal = new FileJobJournal(JobsDirectory);
        JobId jobId = JobId.New();
        journal.MarkRunning(new JobJournalEntry(
            jobId, LauncherOperationKind.CreateSnapshot, DateTimeOffset.UnixEpoch));

        IReadOnlyList<JobJournalEntry> interrupted = new FileJobJournal(JobsDirectory).LoadInterrupted();

        JobJournalEntry entry = Assert.Single(interrupted);
        Assert.Equal(jobId, entry.JobId);
        Assert.Equal(LauncherOperationKind.CreateSnapshot, entry.OperationKind);
    }

    [Fact]
    public void ADamagedJournalRecord_IsIgnoredRatherThanTurnedIntoAClaim()
    {
        Directory.CreateDirectory(JobsDirectory);
        File.WriteAllText(Path.Combine(JobsDirectory, "broken.json"), "{ not json");

        Assert.Empty(new FileJobJournal(JobsDirectory).LoadInterrupted());
    }

    [Fact]
    public void TheJournalRecord_CarriesNoPathAndNoArgumentVector()
    {
        var journal = new FileJobJournal(JobsDirectory);
        JobId jobId = JobId.New();
        journal.MarkRunning(new JobJournalEntry(jobId, LauncherOperationKind.QuickReport, DateTimeOffset.UnixEpoch));

        string content = File.ReadAllText(Path.Combine(JobsDirectory, jobId.Value + ".json"));

        Assert.DoesNotContain("-o", content, StringComparison.Ordinal);
        Assert.DoesNotContain(".xml", content, StringComparison.Ordinal);
        Assert.DoesNotContain(".html", content, StringComparison.Ordinal);
        Assert.DoesNotContain(Path.DirectorySeparatorChar.ToString(), content, StringComparison.Ordinal);
    }

    [Fact]
    public void InterruptedJobs_AreReportedAndNeverResumed()
    {
        var journal = new RecordingJournal();
        journal.MarkRunning(new JobJournalEntry(
            JobId.New(), LauncherOperationKind.CompareIndex, DateTimeOffset.UnixEpoch));

        var viewModel = new InterruptedJobsViewModel(journal);
        viewModel.Load();

        Assert.True(viewModel.HasJobs);

        using (LanguageScope.Use(InterfaceLanguage.Portuguese))
        {
            Assert.Equal("Operação interrompida", viewModel.Title);
            Assert.Contains("Nada é retomado automaticamente", viewModel.Explanation, StringComparison.Ordinal);
            Assert.Equal("Comparar índice de runs", viewModel.Jobs[0].OperationLabel);
        }

        using (LanguageScope.Use(InterfaceLanguage.English))
        {
            Assert.Equal("Operation interrupted", viewModel.Title);
            Assert.Contains("Nothing is resumed automatically", viewModel.Explanation, StringComparison.Ordinal);
            Assert.Equal("Compare run index", viewModel.Jobs[0].OperationLabel);
        }

        viewModel.DismissCommand.Execute(null);

        Assert.False(viewModel.HasJobs);
        Assert.Empty(journal.Active);
    }

    [Fact]
    public void EveryOperation_HasAnInterruptedLabel()
    {
        foreach (LauncherOperationKind kind in Enum.GetValues<LauncherOperationKind>())
        {
            Assert.False(string.IsNullOrWhiteSpace(InterruptedJobViewModel.LabelFor(kind)));
        }
    }

    // ---- Redacted log ------------------------------------------------------

    [Fact]
    public void TheLog_IsJsonLinesWithOneObjectPerLine()
    {
        var log = new JsonLinesLauncherLog(LogsDirectory);

        log.Write(new LauncherLogEntry(
            DateTimeOffset.UnixEpoch,
            LauncherLogLevel.Information,
            "job.started",
            new Dictionary<string, string> { ["operation"] = "QuickReport" }));
        log.Write(new LauncherLogEntry(
            DateTimeOffset.UnixEpoch, LauncherLogLevel.Warning, "job.finished"));

        IReadOnlyList<string> lines = log.ReadAll();

        Assert.Equal(2, lines.Count);
        foreach (string line in lines)
        {
            using JsonDocument document = JsonDocument.Parse(line);
            Assert.Equal(JsonValueKind.Object, document.RootElement.ValueKind);
            Assert.True(document.RootElement.TryGetProperty("event", out _));
        }
    }

    [Fact]
    public async Task AJobLogRecord_CarriesNoAbsolutePathAndNoArgumentVector()
    {
        var log = new JsonLinesLauncherLog(LogsDirectory);
        string output = Path.Combine(_root, "Clientes", "Torre Acme", "relatorio.html");
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);

        var service = new OperationJobService(
            new StubEngineGateway(OperationResult.Success(
                exitCode: 0,
                artifacts: null,
                warnings: null,
                standardOutput: "12 raw clashes -> 4 groups",
                standardError: string.Empty,
                duration: TimeSpan.FromSeconds(1))),
            new RecordingJournal(),
            new InMemoryRecentItemsStore(),
            new StubFileSystem(),
            log,
            new OrzioClashReport.Launcher.Infrastructure.Sha256PathRedactor());

        await service.RunAsync(
            new QuickReportRequest("export.xml", output),
            new StubCollisionPrompt(OutputCollisionResolution.Cancel),
            null,
            CancellationToken.None);

        string all = string.Join("\n", log.ReadAll());

        Assert.NotEmpty(all);
        Assert.Contains("job.started", all, StringComparison.Ordinal);
        Assert.Contains("job.finished", all, StringComparison.Ordinal);

        // Structure, never location: no directory chain, no client name, no engine output.
        Assert.DoesNotContain(output, all, StringComparison.Ordinal);
        Assert.DoesNotContain("Torre Acme", all, StringComparison.Ordinal);
        Assert.DoesNotContain("Clientes", all, StringComparison.Ordinal);
        Assert.DoesNotContain("relatorio.html", all, StringComparison.Ordinal);
        Assert.DoesNotContain("raw clashes", all, StringComparison.Ordinal);
        Assert.DoesNotContain("--", all, StringComparison.Ordinal);

        // What it does carry is enough to correlate two lines about the same destination.
        Assert.Contains("outputPathHash", all, StringComparison.Ordinal);
        Assert.Contains("outputExtension", all, StringComparison.Ordinal);
        Assert.Contains("outputRootKind", all, StringComparison.Ordinal);
    }

    [Fact]
    public void RetentionKeepsAtMostTwentyFilesAndDropsAnythingOlderThanFourteenDays()
    {
        Directory.CreateDirectory(LogsDirectory);
        var now = new DateTimeOffset(2026, 8, 2, 12, 0, 0, TimeSpan.Zero);

        for (int i = 0; i < 30; i++)
        {
            DateTimeOffset day = now.AddDays(-i);
            string path = Path.Combine(LogsDirectory, $"launcher-{day:yyyy-MM-dd}.jsonl");
            File.WriteAllText(path, "{}\n");
            File.SetLastWriteTimeUtc(path, day.UtcDateTime);
        }

        new JsonLinesLauncherLog(LogsDirectory, new FixedTime(now)).ApplyRetention();

        string[] remaining = Directory.GetFiles(LogsDirectory, "*.jsonl");

        Assert.True(remaining.Length <= JsonLinesLauncherLog.MaximumFiles);
        foreach (string file in remaining)
        {
            Assert.True(
                File.GetLastWriteTimeUtc(file) >= now.AddDays(-JsonLinesLauncherLog.RetentionDays).UtcDateTime,
                $"{Path.GetFileName(file)} should have been pruned.");
        }
    }

    [Fact]
    public void RetentionOnAnEmptyDirectoryIsHarmless()
    {
        new JsonLinesLauncherLog(LogsDirectory).ApplyRetention();

        Assert.Empty(new JsonLinesLauncherLog(LogsDirectory).ReadAll());
    }

    // ---- Diagnostic bundle -------------------------------------------------

    private DiagnosticBundleBuilder Builder(params string[] logLines) => new(
        new DiagnosticContext(
            "0.2.0-launcher-preview.1",
            () => EngineProbeResult.Ready("0.1.0-preview.3"),
            () => EngineIntegrityResult.Verified("abc123"),
            () => Array.Empty<JobSnapshot>()),
        new JsonLinesLauncherLogReader(() => logLines));

    [Fact]
    public void ThePreview_ListsExactlyTheSixAllowedFiles()
    {
        DiagnosticBundlePreview preview = Builder().Preview();

        Assert.Equal(
            new[]
            {
                "launcher-version.json",
                "engine-info.json",
                "operating-system.json",
                "job-summary.json",
                "redacted-log.jsonl",
                "integrity-check.json",
            },
            preview.Items.Select(DiagnosticBundleContents.FileNameFor).ToArray());
    }

    [Fact]
    public void ThePreview_ShowsTheExactLogThatWouldBeIncluded()
    {
        DiagnosticBundlePreview preview = Builder("{\"event\":\"job.started\"}").Preview();

        Assert.Equal(new[] { "{\"event\":\"job.started\"}" }, preview.RedactedLogPreviewLines);
    }

    [Fact]
    public async Task TheBundle_ContainsExactlyTheSixAllowedFilesAndNothingElse()
    {
        string destination = Path.Combine(_root, "diagnostics.zip");

        await Builder("{\"event\":\"job.started\"}").BuildAsync(destination, CancellationToken.None);

        using ZipArchive archive = ZipFile.OpenRead(destination);

        Assert.Equal(
            new[]
            {
                "engine-info.json",
                "integrity-check.json",
                "job-summary.json",
                "launcher-version.json",
                "operating-system.json",
                "redacted-log.jsonl",
            },
            archive.Entries.Select(entry => entry.FullName).OrderBy(name => name, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public async Task TheBundle_CarriesNoPathAndNoClientFile()
    {
        string destination = Path.Combine(_root, "diagnostics.zip");
        var builder = new DiagnosticBundleBuilder(
            new DiagnosticContext(
                "0.2.0-launcher-preview.1",
                () => EngineProbeResult.Ready("0.1.0-preview.3"),
                () => EngineIntegrityResult.Verified("abc123"),
                () => new[]
                {
                    new JobSnapshot(
                        JobId.New(),
                        LauncherOperationKind.QuickReport,
                        JobState.Succeeded,
                        DateTimeOffset.UnixEpoch,
                        DateTimeOffset.UnixEpoch.AddSeconds(3),
                        OperationResult.Success(
                            exitCode: 0,
                            new[]
                            {
                                new LauncherArtifact(
                                    ArtifactKind.HtmlReport,
                                    Path.Combine(_root, "Clientes", "Torre Acme", "relatorio.html"),
                                    128),
                            },
                            warnings: null,
                            standardOutput: "12 raw clashes -> 4 groups",
                            standardError: string.Empty,
                            duration: TimeSpan.FromSeconds(3))),
                }),
            new JsonLinesLauncherLogReader(() => new[] { "{\"event\":\"job.finished\"}" }));

        await builder.BuildAsync(destination, CancellationToken.None);

        using ZipArchive archive = ZipFile.OpenRead(destination);
        var all = new System.Text.StringBuilder();
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            using var reader = new StreamReader(entry.Open());
            all.Append(reader.ReadToEnd());
        }

        string content = all.ToString();

        Assert.DoesNotContain("Torre Acme", content, StringComparison.Ordinal);
        Assert.DoesNotContain("relatorio.html", content, StringComparison.Ordinal);
        Assert.DoesNotContain("raw clashes", content, StringComparison.Ordinal);
        Assert.DoesNotContain(_root, content, StringComparison.Ordinal);

        // The job summary still says what ran and how it ended.
        Assert.Contains("QuickReport", content, StringComparison.Ordinal);
        Assert.Contains("Succeeded", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheBundle_ReportsTheEngineByVersionAndStatusNotByLocation()
    {
        string destination = Path.Combine(_root, "diagnostics.zip");
        await Builder().BuildAsync(destination, CancellationToken.None);

        using ZipArchive archive = ZipFile.OpenRead(destination);
        ZipArchiveEntry entry = archive.GetEntry("engine-info.json")!;
        using var reader = new StreamReader(entry.Open());
        string engineInfo = reader.ReadToEnd();

        Assert.Contains("0.1.0-preview.3", engineInfo, StringComparison.Ordinal);
        Assert.Contains("Ready", engineInfo, StringComparison.Ordinal);
        Assert.DoesNotContain("orzioclash", engineInfo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Path.DirectorySeparatorChar.ToString(), engineInfo, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ABundleIsOnlyOfferedAfterTheHumanHasSeenWhatIsInIt()
    {
        var dialogs = new StubFileDialogs { SaveResult = Path.Combine(_root, "diagnostics.zip") };
        var viewModel = new DiagnosticsViewModel(
            Builder("{\"event\":\"job.started\"}"), dialogs, new NoOpOutputRevealer());

        Assert.False(viewModel.IsPreviewVisible);
        Assert.False(viewModel.CreateBundleCommand.CanExecute(null));

        viewModel.ShowPreviewCommand.Execute(null);

        Assert.True(viewModel.IsPreviewVisible);
        Assert.Equal(6, viewModel.IncludedFiles.Count);
        Assert.True(viewModel.HasLogPreview);
        Assert.True(viewModel.CreateBundleCommand.CanExecute(null));

        await viewModel.CreateBundleCommand.ExecuteAsync(null);

        Assert.Equal("diagnostics.zip", viewModel.LastBundleFileName);
        Assert.True(viewModel.HasBundle);
        Assert.False(viewModel.HasFailure);
    }

    [Fact]
    public void TheDiagnosticsPanel_SaysWhatIsNeverIncluded()
    {
        var viewModel = new DiagnosticsViewModel(
            Builder(), new StubFileDialogs(), new NoOpOutputRevealer());

        using (LanguageScope.Use(InterfaceLanguage.Portuguese))
        {
            Assert.Contains("Nunca inclui ficheiros do cliente", viewModel.Explanation, StringComparison.Ordinal);
            Assert.Contains("caminhos absolutos", viewModel.Explanation, StringComparison.Ordinal);
        }

        using (LanguageScope.Use(InterfaceLanguage.English))
        {
            Assert.Contains("never includes client files", viewModel.Explanation, StringComparison.Ordinal);
            Assert.Contains("absolute paths", viewModel.Explanation, StringComparison.Ordinal);
        }
    }

    private sealed class FixedTime : TimeProvider
    {
        private readonly DateTimeOffset _now;

        internal FixedTime(DateTimeOffset now)
        {
            _now = now;
        }

        public override DateTimeOffset GetUtcNow() => _now;
    }
}
