using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OrzioClashReport.Launcher.Contracts.Diagnostics;
using OrzioClashReport.Launcher.Contracts.Engine;
using OrzioClashReport.Launcher.Contracts.Jobs;
using OrzioClashReport.Launcher.Contracts.Logging;
using OrzioClashReport.Launcher.Contracts.Platform;
using OrzioClashReport.Launcher.Contracts.Settings;

namespace OrzioClashReport.Launcher.Desktop.Tests
{
    /// <summary>
    /// Minimal in-memory adapters so the shell can be booted headlessly without touching the disk,
    /// the engine, or the user's real settings.
    /// </summary>
    internal sealed class StubEngineLocator : IEngineLocator
    {
        private readonly EngineLocation? _location;

        public StubEngineLocator(EngineLocation? location) => _location = location;

        public EngineLocation? Locate() => _location;
    }

    internal sealed class StubIntegrityVerifier : IEngineIntegrityVerifier
    {
        private readonly EngineIntegrityResult _result;

        public StubIntegrityVerifier(EngineIntegrityResult result) => _result = result;

        public Task<EngineIntegrityResult> VerifyAsync(EngineLocation location, CancellationToken cancellationToken) =>
            Task.FromResult(_result);
    }

    internal sealed class StubExpectationSource : IEngineExpectationSource
    {
        private readonly string? _version;

        public StubExpectationSource(string? version) => _version = version;

        public string? ReadExpectedVersion(EngineLocation location) => _version;
    }

    internal sealed class StubProcessRunner : IEngineProcessRunner
    {
        private readonly EngineProcessResult _result;

        public StubProcessRunner(EngineProcessResult result) => _result = result;

        public Task<EngineProcessResult> RunAsync(
            EngineProcessRequest request,
            IProgress<EngineJobProgress>? progress,
            CancellationToken cancellationToken) => Task.FromResult(_result);
    }

    internal sealed class InMemorySettingsStore : ISettingsStore
    {
        private LauncherSettings _settings = LauncherSettings.Default;

        public Task<LauncherSettings> LoadAsync(CancellationToken cancellationToken) =>
            Task.FromResult(_settings);

        public Task SaveAsync(LauncherSettings settings, CancellationToken cancellationToken)
        {
            _settings = settings;
            return Task.CompletedTask;
        }
    }

    internal sealed class InMemoryRecentItemsStore : IRecentItemsStore
    {
        private readonly List<RecentOutputItem> _items = new List<RecentOutputItem>();

        public Task<IReadOnlyList<RecentOutputItem>> LoadAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<RecentOutputItem>>(_items.ToArray());

        public Task<IReadOnlyList<RecentOutputItem>> AddAsync(
            RecentOutputItem item, CancellationToken cancellationToken)
        {
            _items.Insert(0, item);
            return Task.FromResult<IReadOnlyList<RecentOutputItem>>(_items.ToArray());
        }

        public Task ClearAsync(CancellationToken cancellationToken)
        {
            _items.Clear();
            return Task.CompletedTask;
        }
    }

    internal sealed class NullOutputRevealer : IOutputRevealer
    {
        public Task<bool> OpenAsync(string path, CancellationToken cancellationToken) => Task.FromResult(false);

        public Task<bool> RevealInFolderAsync(string path, CancellationToken cancellationToken) =>
            Task.FromResult(false);
    }

    internal sealed class CollectingLog : ILauncherLog
    {
        public List<LauncherLogEntry> Entries { get; } = new List<LauncherLogEntry>();

        public void Write(LauncherLogEntry entry) => Entries.Add(entry);
    }

    internal sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UnixEpoch;
    }

    internal sealed class NoOpJobJournal : IJobJournal
    {
        public List<JobJournalEntry> Interrupted { get; } = new List<JobJournalEntry>();

        public Task BeginAsync(JobJournalEntry entry, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task CompleteAsync(string jobId, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IReadOnlyList<JobJournalEntry>> ReadInterruptedAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<JobJournalEntry>>(Interrupted.ToArray());

        public Task ClearAsync(CancellationToken cancellationToken)
        {
            Interrupted.Clear();
            return Task.CompletedTask;
        }
    }

    internal sealed class StubDiagnosticsBundleBuilder : IDiagnosticsBundleBuilder
    {
        public IReadOnlyList<DiagnosticBundleItem> Plan() => DiagnosticBundleItem.All;

        public Task<string> PreviewRedactedLogAsync(int maximumLines, CancellationToken cancellationToken) =>
            Task.FromResult("{\"event\":\"job.finished\"}");

        public Task<string> BuildAsync(EngineInfo engine, CancellationToken cancellationToken) =>
            Task.FromResult("/diagnostics/bundle.zip");
    }
}
