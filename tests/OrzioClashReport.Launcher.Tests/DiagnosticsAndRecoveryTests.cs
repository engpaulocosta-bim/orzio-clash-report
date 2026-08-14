using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrzioClashReport.Launcher.Contracts.Diagnostics;
using OrzioClashReport.Launcher.Contracts.Engine;
using OrzioClashReport.Launcher.Contracts.Jobs;
using OrzioClashReport.Launcher.Contracts.Logging;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Infrastructure.Diagnostics;
using OrzioClashReport.Launcher.Infrastructure.Logging;
using OrzioClashReport.Launcher.Infrastructure.Storage;

namespace OrzioClashReport.Launcher.Tests
{
    public sealed class DiagnosticsAndRecoveryTests : IDisposable
    {
        private readonly string _root;
        private readonly LauncherStorageLocations _locations;
        private readonly FixedClock _clock = new FixedClock(DateTimeOffset.Parse("2026-08-13T10:00:00Z"));

        public DiagnosticsAndRecoveryTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "orzio-diagnostics-" + Guid.NewGuid().ToString("N"));
            _locations = new LauncherStorageLocations(_root);
            _locations.EnsureCreated();
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
        public async Task AJournalEntryExistsWhileAJobRunsAndIsGoneAfterIt()
        {
            var journal = new FileSystemJobJournal(_locations.JobsDirectory);

            var entry = new JobJournalEntry(
                "job-1", LauncherOperationKind.QuickReport, _clock.UtcNow, "report.html");

            await journal.BeginAsync(entry, CancellationToken.None);

            JobJournalEntry running = Assert.Single(await journal.ReadInterruptedAsync(CancellationToken.None));
            Assert.Equal("job-1", running.JobId);
            Assert.Equal(LauncherOperationKind.QuickReport, running.Operation);
            Assert.Equal("report.html", running.OutputFileName);

            await journal.CompleteAsync("job-1", CancellationToken.None);

            Assert.Empty(await journal.ReadInterruptedAsync(CancellationToken.None));
        }

        [Fact]
        public async Task AJournalEntryNeverRecordsTheFolderTheOutputWasIn()
        {
            var journal = new FileSystemJobJournal(_locations.JobsDirectory);

            await journal.BeginAsync(
                new JobJournalEntry("job-1", LauncherOperationKind.QuickReport, _clock.UtcNow, "report.html"),
                CancellationToken.None);

            string content = File.ReadAllText(Path.Combine(_locations.JobsDirectory, "job-1.json"));

            Assert.Contains("report.html", content);
            Assert.DoesNotContain(Path.DirectorySeparatorChar.ToString(), content.Split("\"outputFileName\": ")[1]);
        }

        [Fact]
        public async Task ReadingInterruptedEntriesDoesNotRemoveThem()
        {
            var journal = new FileSystemJobJournal(_locations.JobsDirectory);

            await journal.BeginAsync(
                new JobJournalEntry("job-1", LauncherOperationKind.Snapshot, _clock.UtcNow, null),
                CancellationToken.None);

            Assert.Single(await journal.ReadInterruptedAsync(CancellationToken.None));
            Assert.Single(await journal.ReadInterruptedAsync(CancellationToken.None));

            await journal.ClearAsync(CancellationToken.None);
            Assert.Empty(await journal.ReadInterruptedAsync(CancellationToken.None));
        }

        [Fact]
        public async Task AnUnreadableJournalEntryIsSkippedRatherThanShownHalfFormed()
        {
            File.WriteAllText(Path.Combine(_locations.JobsDirectory, "broken.json"), "{ not json");

            var journal = new FileSystemJobJournal(_locations.JobsDirectory);

            Assert.Empty(await journal.ReadInterruptedAsync(CancellationToken.None));
        }

        [Fact]
        public void LogRetentionKeepsFourteenDaysAndTwentyFiles()
        {
            Assert.Equal(14, LogRetentionPolicy.MaximumAgeInDays);
            Assert.Equal(20, LogRetentionPolicy.MaximumFileCount);

            for (int day = 0; day < 30; day++)
            {
                string path = Path.Combine(_locations.LogsDirectory, $"launcher-2026-07-{day + 1:00}.jsonl");
                File.WriteAllText(path, "{}");
                File.SetLastWriteTimeUtc(path, _clock.UtcNow.UtcDateTime.AddDays(-day));
            }

            new LogRetentionPolicy(_locations.LogsDirectory, _clock).Apply();

            string[] remaining = Directory.GetFiles(_locations.LogsDirectory, "*.jsonl");

            // Fourteen days is the tighter limit here, so it is the one that decides.
            Assert.Equal(15, remaining.Length);

            foreach (string path in remaining)
            {
                Assert.True(File.GetLastWriteTimeUtc(path) >= _clock.UtcNow.UtcDateTime.AddDays(-14));
            }
        }

        [Fact]
        public void LogRetentionAlsoCapsTheFileCountWhenEverythingIsRecent()
        {
            for (int i = 0; i < 30; i++)
            {
                string path = Path.Combine(_locations.LogsDirectory, $"launcher-recent-{i:00}.jsonl");
                File.WriteAllText(path, "{}");
                File.SetLastWriteTimeUtc(path, _clock.UtcNow.UtcDateTime.AddMinutes(-i));
            }

            new LogRetentionPolicy(_locations.LogsDirectory, _clock).Apply();

            Assert.Equal(20, Directory.GetFiles(_locations.LogsDirectory, "*.jsonl").Length);
        }

        [Fact]
        public void TheBundlePlanIsTheClosedAllowList()
        {
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
                DiagnosticBundleItem.All.Select(item => item.FileName));

            Assert.False(DiagnosticBundleItem.IsAllowed("sample-clash.xml"));
            Assert.False(DiagnosticBundleItem.IsAllowed("report.html"));
            Assert.False(DiagnosticBundleItem.IsAllowed("identity-governance.json"));
        }

        [Fact]
        public async Task TheBundleContainsExactlyTheDeclaredFilesAndNothingElse()
        {
            WriteLog();

            string bundlePath = await CreateBuilder().BuildAsync(ReadyEngine(), CancellationToken.None);

            using (ZipArchive archive = ZipFile.OpenRead(bundlePath))
            {
                Assert.Equal(
                    DiagnosticBundleItem.All.Select(item => item.FileName).OrderBy(name => name, StringComparer.Ordinal),
                    archive.Entries.Select(entry => entry.FullName).OrderBy(name => name, StringComparer.Ordinal));
            }
        }

        [Fact]
        public async Task TheBundleNeverContainsAClientFile()
        {
            // A real client export sitting right next to the launcher's own data.
            File.WriteAllText(Path.Combine(_root, "ACME-Tower-run-004.xml"), "<exchange />");
            File.WriteAllText(Path.Combine(_root, "ACME-Tower-report.html"), "<html></html>");

            WriteLog();

            string bundlePath = await CreateBuilder().BuildAsync(ReadyEngine(), CancellationToken.None);

            using (ZipArchive archive = ZipFile.OpenRead(bundlePath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    Assert.True(DiagnosticBundleItem.IsAllowed(entry.FullName));

                    using (var reader = new StreamReader(entry.Open()))
                    {
                        string content = reader.ReadToEnd();

                        Assert.DoesNotContain("ACME", content, StringComparison.OrdinalIgnoreCase);
                        Assert.DoesNotContain("<exchange", content, StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
        }

        [Fact]
        public async Task TheBundleDoesNotDiscloseWhereTheEngineIsInstalled()
        {
            WriteLog();

            string bundlePath = await CreateBuilder().BuildAsync(ReadyEngine(), CancellationToken.None);

            using (ZipArchive archive = ZipFile.OpenRead(bundlePath))
            using (var reader = new StreamReader(
                archive.GetEntry(DiagnosticBundleItem.EngineInfo.FileName)!.Open()))
            {
                string content = reader.ReadToEnd();

                Assert.Contains("0.1.0-preview.3", content);
                Assert.DoesNotContain("orzioclash.exe", content);
                Assert.DoesNotContain("Programs", content);
            }
        }

        [Fact]
        public async Task ThePreviewShowsTheSameRedactedLogTheBundleWouldCarry()
        {
            WriteLog();

            IDiagnosticsBundleBuilder builder = CreateBuilder();

            string preview = await builder.PreviewRedactedLogAsync(200, CancellationToken.None);

            Assert.Contains("job.finished", preview);
            Assert.DoesNotContain("ACME", preview);

            string bundlePath = await builder.BuildAsync(ReadyEngine(), CancellationToken.None);

            using (ZipArchive archive = ZipFile.OpenRead(bundlePath))
            using (var reader = new StreamReader(
                archive.GetEntry(DiagnosticBundleItem.RedactedLog.FileName)!.Open()))
            {
                Assert.Equal(preview, reader.ReadToEnd());
            }
        }

        [Fact]
        public async Task ABundleIsOnlyEverProducedWhenItIsAskedFor()
        {
            // Constructing the builder and asking for its plan writes nothing at all.
            IDiagnosticsBundleBuilder builder = CreateBuilder();
            builder.Plan();
            await builder.PreviewRedactedLogAsync(10, CancellationToken.None);

            Assert.Empty(Directory.GetFiles(_locations.DiagnosticsDirectory));

            await builder.BuildAsync(ReadyEngine(), CancellationToken.None);

            Assert.Single(Directory.GetFiles(_locations.DiagnosticsDirectory, "*.zip"));
        }

        private ZipDiagnosticsBundleBuilder CreateBuilder() =>
            new ZipDiagnosticsBundleBuilder(
                _locations.DiagnosticsDirectory,
                () => Path.Combine(_locations.LogsDirectory, "launcher-2026-08-13.jsonl"),
                _clock,
                "0.2.0-launcher-preview.1");

        private void WriteLog()
        {
            var log = new JsonLinesLauncherLog(_locations.LogsDirectory, _clock);
            var redactor = new Sha256PathRedactor();

            log.Write(new LauncherLogEntry(
                _clock.UtcNow, LauncherLogLevel.Information, "job.started", "Operation started.",
                new Dictionary<string, string> { ["operation"] = "QuickReport" }));

            log.Write(new LauncherLogEntry(
                    _clock.UtcNow, LauncherLogLevel.Information, "job.finished", "Operation finished.",
                    new Dictionary<string, string> { ["state"] = "Succeeded", ["exitCode"] = "0" })
                .WithPath("output", redactor.Redact(Path.Combine(_root, "ACME Tower", "report.html"))));
        }

        private static EngineInfo ReadyEngine() =>
            new EngineInfo(
                EngineStatusKind.Ready,
                "0.1.0-preview.3",
                "0.1.0-preview.3",
                new EngineLocation(
                    @"C:\Users\alguem\AppData\Local\Programs\Orzio\ClashReportLauncher\engine\win-x64\orzioclash.exe",
                    @"C:\Users\alguem\AppData\Local\Programs\Orzio\ClashReportLauncher\engine\win-x64\engine-manifest.json"),
                new EngineIntegrityResult(EngineIntegrityVerdict.Verified, "abc123", "abc123"),
                "Motor verificado.");
    }
}
