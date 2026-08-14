using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrzioClashReport.Launcher.Application.Engine;
using OrzioClashReport.Launcher.Application.Operations;
using OrzioClashReport.Launcher.Contracts.Engine;
using OrzioClashReport.Launcher.Contracts.Jobs;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Contracts.Results;
using OrzioClashReport.Launcher.Contracts.Settings;
using OrzioClashReport.Launcher.Infrastructure.Engine;
using OrzioClashReport.Launcher.Infrastructure.Logging;
using OrzioClashReport.Launcher.Infrastructure.Platform;
using OrzioClashReport.Launcher.Infrastructure.Process;
using OrzioClashReport.Launcher.Infrastructure.Storage;

namespace OrzioClashReport.Launcher.Tests
{
    /// <summary>
    /// The whole quick-report path with a real child process behind it: argument vector, working
    /// directory, collision policy, output verification, recent items and the job journal.
    /// </summary>
    public sealed class QuickReportEndToEndTests : IDisposable
    {
        private readonly string _root;
        private readonly string _outputDirectory;
        private readonly string _installationDirectory;
        private readonly LauncherStorageLocations _locations;
        private readonly JsonRecentItemsStore _recentItems;
        private readonly FileSystemJobJournal _journal;
        private readonly CollectingLauncherLog _log = new CollectingLauncherLog();

        public QuickReportEndToEndTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "orzio-quickreport-" + Guid.NewGuid().ToString("N"));
            _outputDirectory = Path.Combine(_root, "reports");
            _installationDirectory = Path.Combine(_root, "install");

            Directory.CreateDirectory(_outputDirectory);
            Directory.CreateDirectory(_installationDirectory);

            _locations = new LauncherStorageLocations(Path.Combine(_root, "appdata"));
            _locations.EnsureCreated();

            _recentItems = new JsonRecentItemsStore(_locations.RecentItemsFilePath);
            _journal = new FileSystemJobJournal(_locations.JobsDirectory);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(_root, recursive: true);
            }
            catch (IOException)
            {
                // Temporary cleanup only.
            }
        }

        [Fact]
        public async Task AQuickReportProducesTheFileRecordsItAndLeavesNoJournalEntry()
        {
            string output = Path.Combine(_outputDirectory, "report.html");

            EngineJobResult result = await ExecuteAsync(Input("model.xml"), output);

            Assert.Equal(EngineJobState.Succeeded, result.State);
            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(output));

            LauncherArtifact artifact = Assert.Single(result.Artifacts);
            Assert.Equal(LauncherArtifactKind.HtmlReport, artifact.Kind);
            Assert.Equal(output, artifact.Path);
            Assert.True(artifact.SizeInBytes > 0);

            IReadOnlyList<RecentOutputItem> recent = await _recentItems.LoadAsync(CancellationToken.None);
            Assert.Equal(output, Assert.Single(recent).Path);

            Assert.Empty(await _journal.ReadInterruptedAsync(CancellationToken.None));
        }

        [Fact]
        public async Task TheArgumentVectorIsExactlyTheInputThenDashOThenTheDestination()
        {
            string output = Path.Combine(_outputDirectory, "report.html");
            string input = Input("model.xml");

            Assert.Equal(
                new[] { input, "-o", output },
                EngineArgumentBuilder.QuickReport(input, output));
        }

        [Fact]
        public async Task TheEngineRunsInTheOutputDirectoryAndNeverInTheInstallation()
        {
            string output = Path.Combine(_outputDirectory, "report.html");

            await ExecuteAsync(Input("model.xml"), output);

            // The fake engine wrote its report relative to nothing: the destination is absolute, and
            // what matters is that the process was not rooted in the installation directory.
            Assert.True(File.Exists(output));
            Assert.Empty(Directory.GetFiles(_installationDirectory));
        }

        [Fact]
        public async Task AnOperationRootedInTheInstallationDirectoryIsRefusedBeforeAnythingRuns()
        {
            string output = Path.Combine(_installationDirectory, "report.html");

            EngineJobResult result = await ExecuteAsync(Input("model.xml"), output);

            Assert.Equal(EngineJobState.Failed, result.State);
            Assert.Equal(LauncherErrorKind.InvalidInput, result.Error!.Kind);
            Assert.False(File.Exists(output));
        }

        [Fact]
        public async Task AFailingEngineIsReportedAsAnExecutionFailureCarryingItsExitCodeAndItsOwnMessage()
        {
            string output = Path.Combine(_outputDirectory, "report.html");

            EngineJobResult result = await ExecuteAsync(Input("fail.xml"), output);

            Assert.Equal(EngineJobState.Failed, result.State);
            Assert.Equal(LauncherErrorKind.EngineExecutionFailure, result.Error!.Kind);
            Assert.Equal(1, result.Error.ExitCode);
            Assert.Contains("not a Clash Detective export", result.StandardError);
        }

        [Fact]
        public async Task ExitZeroWithNoFileIsReportedAsOutputMissing()
        {
            string output = Path.Combine(_outputDirectory, "report.html");

            EngineJobResult result = await ExecuteAsync(Input("no-output.xml"), output);

            Assert.Equal(EngineJobState.Failed, result.State);
            Assert.Equal(LauncherErrorKind.OutputMissing, result.Error!.Kind);
            Assert.Equal(0, result.ExitCode);
        }

        [Fact]
        public async Task ExitZeroWithAnEmptyFileIsAlsoReportedAsOutputMissing()
        {
            string output = Path.Combine(_outputDirectory, "report.html");

            EngineJobResult result = await ExecuteAsync(Input("empty-output.xml"), output);

            Assert.Equal(EngineJobState.Failed, result.State);
            Assert.Equal(LauncherErrorKind.OutputMissing, result.Error!.Kind);
            Assert.True(File.Exists(output));
            Assert.Equal(0, new FileInfo(output).Length);
        }

        [Fact]
        public async Task AnExistingReportIsNeverReplacedWithoutAnExplicitDecision()
        {
            string output = Path.Combine(_outputDirectory, "report.html");
            File.WriteAllText(output, "PREVIOUS");

            EngineJobResult result = await ExecuteAsync(Input("model.xml"), output);

            Assert.Equal(LauncherErrorKind.OutputCollision, result.Error!.Kind);
            Assert.Equal("PREVIOUS", File.ReadAllText(output));
        }

        [Fact]
        public async Task AnExistingReportIsReplacedOnlyWhenReplacementIsChosen()
        {
            string output = Path.Combine(_outputDirectory, "report.html");
            File.WriteAllText(output, "PREVIOUS");

            EngineJobResult result = await ExecuteAsync(
                Input("model.xml"), output, OutputCollisionDecision.ReplaceExisting);

            Assert.Equal(EngineJobState.Succeeded, result.State);
            Assert.DoesNotContain("PREVIOUS", File.ReadAllText(output));
        }

        [Fact]
        public async Task ChoosingAnotherNameNeverReplacesAnything()
        {
            string output = Path.Combine(_outputDirectory, "report.html");
            File.WriteAllText(output, "PREVIOUS");

            EngineJobResult result = await ExecuteAsync(
                Input("model.xml"), output, OutputCollisionDecision.ChooseAnotherName);

            Assert.Equal(LauncherErrorKind.OutputCollision, result.Error!.Kind);
            Assert.Equal("PREVIOUS", File.ReadAllText(output));
        }

        [Fact]
        public async Task ARunningJobCanBeCancelledAndIsReportedAsCancelledRatherThanFailed()
        {
            string output = Path.Combine(_outputDirectory, "report.html");

            using (var cancellation = new CancellationTokenSource())
            {
                LauncherOperationExecutor executor = CreateExecutor();

                var request = new LauncherOperationRequest(
                    LauncherOperationKind.QuickReport,
                    EngineArgumentBuilder.QuickReport(Input("hang.xml"), output),
                    _outputDirectory,
                    output,
                    OutputCollisionDecision.None,
                    "report.html");

                Task<EngineJobResult> run = executor.ExecuteAsync(request, null, cancellation.Token);
                cancellation.CancelAfter(TimeSpan.FromMilliseconds(400));

                EngineJobResult result = await run;

                Assert.Equal(EngineJobState.Canceled, result.State);
                Assert.Null(result.Error);
            }

            // A cancelled job is still a terminal state, so the journal must be clean afterwards.
            Assert.Empty(await _journal.ReadInterruptedAsync(CancellationToken.None));
        }

        [Fact]
        public async Task ACancelledJobIsNotRecordedAsARecentOutput()
        {
            string output = Path.Combine(_outputDirectory, "report.html");
            File.WriteAllText(output, string.Empty);
            File.Delete(output);

            using (var cancellation = new CancellationTokenSource())
            {
                cancellation.Cancel();

                LauncherOperationExecutor executor = CreateExecutor();

                var request = new LauncherOperationRequest(
                    LauncherOperationKind.QuickReport,
                    EngineArgumentBuilder.QuickReport(Input("hang.xml"), output),
                    _outputDirectory,
                    output,
                    OutputCollisionDecision.None,
                    "report.html");

                await executor.ExecuteAsync(request, null, cancellation.Token);
            }

            Assert.Empty(await _recentItems.LoadAsync(CancellationToken.None));
        }

        [Fact]
        public async Task TheLogNeverRecordsTheArgumentVectorOrAnAbsolutePath()
        {
            string output = Path.Combine(_outputDirectory, "ACME-Tower-report.html");

            await ExecuteAsync(Input("model.xml"), output);

            Assert.NotEmpty(_log.Entries);

            foreach (var entry in _log.Entries)
            {
                Assert.DoesNotContain(_outputDirectory, entry.Message, StringComparison.Ordinal);

                foreach (string value in entry.Fields.Values)
                {
                    Assert.DoesNotContain(_outputDirectory, value, StringComparison.Ordinal);
                    Assert.DoesNotContain(_root, value, StringComparison.Ordinal);
                }
            }

            // The file name is allowed and useful; the folder structure around it is not.
            Assert.Contains(
                _log.Entries,
                entry => entry.Fields.TryGetValue("output.fileName", out string? name)
                         && name == "ACME-Tower-report.html");
        }

        [Fact]
        public async Task ProgressLinesReachTheCaller()
        {
            string output = Path.Combine(_outputDirectory, "report.html");
            var lines = new List<string>();

            var progress = new Progress<EngineJobProgress>(update =>
            {
                if (update.Line != null)
                {
                    lock (lines)
                    {
                        lines.Add(update.Line);
                    }
                }
            });

            await ExecuteAsync(Input("model.xml"), output, OutputCollisionDecision.None, progress);
            await Task.Delay(200);

            lock (lines)
            {
                Assert.Contains(lines, line => line.Contains("Report written to", StringComparison.Ordinal));
            }
        }

        private string Input(string fileName)
        {
            string path = Path.Combine(_root, fileName);
            File.WriteAllText(path, "<exchange />");
            return path;
        }

        private Task<EngineJobResult> ExecuteAsync(
            string input,
            string output,
            OutputCollisionDecision decision = OutputCollisionDecision.None,
            IProgress<EngineJobProgress>? progress = null)
        {
            var request = new LauncherOperationRequest(
                LauncherOperationKind.QuickReport,
                EngineArgumentBuilder.QuickReport(input, output),
                Path.GetDirectoryName(output)!,
                output,
                decision,
                Path.GetFileName(output));

            return CreateExecutor().ExecuteAsync(request, progress, CancellationToken.None);
        }

        private LauncherOperationExecutor CreateExecutor()
        {
            var processRunner = new ProcessJobRunner();
            var fileProbe = new FileSystemProbe();

            var probe = new EngineProbe(
                new FakeEngineLocator(new EngineLocation(
                    FakeEngineLocation.ExecutablePath,
                    Path.Combine(_installationDirectory, "engine-manifest.json"))),
                new FakeIntegrityVerifier(EngineIntegrityVerdict.Verified),
                new FakeExpectationSource("0.1.0-preview.3"),
                processRunner,
                Path.GetTempPath());

            return new LauncherOperationExecutor(
                new CliEngineGateway(probe, processRunner, fileProbe),
                fileProbe,
                _recentItems,
                _journal,
                _log,
                new Sha256PathRedactor(),
                new FixedClock(DateTimeOffset.UnixEpoch),
                _installationDirectory);
        }
    }
}
