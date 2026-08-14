using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OrzioClashReport.Launcher.Contracts.Logging;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Contracts.Results;
using OrzioClashReport.Launcher.Contracts.Settings;
using OrzioClashReport.Launcher.Infrastructure.Logging;
using OrzioClashReport.Launcher.Infrastructure.Storage;

namespace OrzioClashReport.Launcher.Tests
{
    public sealed class LocalStoreTests : IDisposable
    {
        private readonly string _root;

        public LocalStoreTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "orzio-launcher-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(_root, recursive: true);
            }
            catch (IOException)
            {
                // A leftover temporary directory is harmless; failing the test over it would not be.
            }
        }

        [Fact]
        public void AllLauncherDataLivesUnderOneLocalApplicationDataFolder()
        {
            var locations = new LauncherStorageLocations(Path.Combine(_root, "Orzio", "ClashReportLauncher"));

            Assert.Equal(Path.Combine(locations.RootDirectory, "settings.json"), locations.SettingsFilePath);
            Assert.Equal(Path.Combine(locations.RootDirectory, "recent-items.json"), locations.RecentItemsFilePath);
            Assert.Equal(Path.Combine(locations.RootDirectory, "logs"), locations.LogsDirectory);
            Assert.Equal(Path.Combine(locations.RootDirectory, "jobs"), locations.JobsDirectory);
            Assert.Equal(Path.Combine(locations.RootDirectory, "diagnostics"), locations.DiagnosticsDirectory);

            locations.EnsureCreated();

            Assert.True(Directory.Exists(locations.LogsDirectory));
            Assert.True(Directory.Exists(locations.JobsDirectory));
            Assert.True(Directory.Exists(locations.DiagnosticsDirectory));
        }

        [Fact]
        public void TheDefaultLocationIsInsideTheUsersOwnProfileAndNotTheInstallation()
        {
            LauncherStorageLocations locations = LauncherStorageLocations.CreateDefault();

            Assert.Contains(LauncherStorageLocations.VendorFolderName, locations.RootDirectory);
            Assert.Contains(LauncherStorageLocations.ApplicationFolderName, locations.RootDirectory);
            Assert.DoesNotContain(AppContext.BaseDirectory, locations.RootDirectory);
        }

        [Fact]
        public async Task SettingsRoundTrip()
        {
            var store = new JsonSettingsStore(Path.Combine(_root, "settings.json"));

            Assert.Equal(LauncherThemePreference.System, (await store.LoadAsync(CancellationToken.None)).Theme);

            await store.SaveAsync(
                new LauncherSettings(LauncherThemePreference.Dark, "/reports", false), CancellationToken.None);

            LauncherSettings loaded = await store.LoadAsync(CancellationToken.None);

            Assert.Equal(LauncherThemePreference.Dark, loaded.Theme);
            Assert.Equal("/reports", loaded.LastOutputDirectory);
            Assert.False(loaded.ShowExperimentalWarnings);
        }

        [Fact]
        public async Task ACorruptSettingsFileFallsBackToTheDefaultsInsteadOfFailingToStart()
        {
            string path = Path.Combine(_root, "settings.json");
            File.WriteAllText(path, "{ this is not json");

            LauncherSettings loaded = await new JsonSettingsStore(path).LoadAsync(CancellationToken.None);

            Assert.Equal(LauncherThemePreference.System, loaded.Theme);
        }

        [Fact]
        public async Task RecentItemsAreNewestFirstDeduplicatedAndCapped()
        {
            var store = new JsonRecentItemsStore(Path.Combine(_root, "recent-items.json"));

            for (int i = 0; i < JsonRecentItemsStore.MaximumItems + 5; i++)
            {
                await store.AddAsync(Recent(Path.Combine(_root, "report-" + i + ".html")), CancellationToken.None);
            }

            IReadOnlyList<RecentOutputItem> items = await store.LoadAsync(CancellationToken.None);

            Assert.Equal(JsonRecentItemsStore.MaximumItems, items.Count);
            Assert.Equal(Path.Combine(_root, "report-14.html"), items[0].Path);

            await store.AddAsync(Recent(Path.Combine(_root, "report-14.html")), CancellationToken.None);
            items = await store.LoadAsync(CancellationToken.None);

            Assert.Equal(JsonRecentItemsStore.MaximumItems, items.Count);
            Assert.Single(items, item => item.Path == Path.Combine(_root, "report-14.html"));
        }

        [Fact]
        public async Task ClearingRecentItemsRemovesTheListWithoutTouchingTheFiles()
        {
            string reportPath = Path.Combine(_root, "report.html");
            File.WriteAllText(reportPath, "<html></html>");

            var store = new JsonRecentItemsStore(Path.Combine(_root, "recent-items.json"));
            await store.AddAsync(Recent(reportPath), CancellationToken.None);
            await store.ClearAsync(CancellationToken.None);

            Assert.Empty(await store.LoadAsync(CancellationToken.None));
            Assert.True(File.Exists(reportPath));
        }

        [Fact]
        public void TheLogWritesOneJsonObjectPerLineWithSortedFields()
        {
            var clock = new FixedClock(DateTimeOffset.Parse("2026-08-13T10:00:00Z"));
            var log = new JsonLinesLauncherLog(Path.Combine(_root, "logs"), clock);

            log.Write(new LauncherLogEntry(
                clock.UtcNow,
                LauncherLogLevel.Information,
                "job.started",
                "Job started.",
                new Dictionary<string, string> { ["zebra"] = "1", ["alpha"] = "2" }));

            string[] lines = File.ReadAllLines(log.CurrentFilePath);

            Assert.Single(lines);
            Assert.StartsWith("{", lines[0]);
            Assert.Contains("\"event\":\"job.started\"", lines[0]);
            Assert.True(
                lines[0].IndexOf("alpha", StringComparison.Ordinal) < lines[0].IndexOf("zebra", StringComparison.Ordinal),
                "Log fields are written in ordinal key order so lines stay comparable.");
        }

        [Fact]
        public void TheLogRecordsPathsOnlyInRedactedForm()
        {
            var clock = new FixedClock(DateTimeOffset.Parse("2026-08-13T10:00:00Z"));
            var log = new JsonLinesLauncherLog(Path.Combine(_root, "logs"), clock);
            var redactor = new Sha256PathRedactor();

            string privatePath = Path.Combine(_root, "Clients", "ACME Tower", "run-004.xml");

            log.Write(new LauncherLogEntry(clock.UtcNow, LauncherLogLevel.Information, "job.input", "Input selected.")
                .WithPath("input", redactor.Redact(privatePath)));

            string content = File.ReadAllText(log.CurrentFilePath);

            Assert.Contains("run-004.xml", content);
            Assert.DoesNotContain("ACME", content);
            Assert.DoesNotContain(_root, content);
        }

        [Fact]
        public void AFailureToWriteALogLineNeverThrows()
        {
            // A path that cannot be a directory: the log must swallow the failure, not break the run.
            string blocked = Path.Combine(_root, "blocked");
            File.WriteAllText(blocked, "not a directory");

            var log = new JsonLinesLauncherLog(
                Path.Combine(blocked, "logs"), new FixedClock(DateTimeOffset.UnixEpoch));

            log.Write(new LauncherLogEntry(
                DateTimeOffset.UnixEpoch, LauncherLogLevel.Error, "test", "message"));
        }

        private static RecentOutputItem Recent(string path) =>
            new RecentOutputItem(
                path,
                Path.GetFileName(path),
                LauncherOperationKind.QuickReport,
                LauncherArtifactKind.HtmlReport,
                DateTimeOffset.UnixEpoch);
    }
}
