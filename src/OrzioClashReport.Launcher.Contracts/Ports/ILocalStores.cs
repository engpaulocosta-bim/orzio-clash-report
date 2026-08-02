using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OrzioClashReport.Launcher.Contracts.Diagnostics;
using OrzioClashReport.Launcher.Contracts.Jobs;
using OrzioClashReport.Launcher.Contracts.Settings;

namespace OrzioClashReport.Launcher.Contracts.Ports
{
    /// <summary>Reads and writes the launcher's own settings file.</summary>
    public interface ISettingsStore
    {
        LauncherSettings Load();

        void Save(LauncherSettings settings);
    }

    /// <summary>Reads and writes the recent-output list.</summary>
    public interface IRecentItemsStore
    {
        IReadOnlyList<RecentOutputItem> Load();

        /// <summary>Records one produced output as the most recent entry.</summary>
        void Add(RecentOutputItem item);
    }

    /// <summary>
    /// Records that a job entered <see cref="JobState.Running"/> and clears the record when the job
    /// reaches a terminal state, so a leftover record on start means the job was interrupted.
    /// An interrupted job is reported to the human and never resumed automatically.
    /// </summary>
    public interface IJobJournal
    {
        void MarkRunning(JobJournalEntry entry);

        void Clear(JobId jobId);

        /// <summary>Journal records left behind by a previous session.</summary>
        IReadOnlyList<JobJournalEntry> LoadInterrupted();
    }

    /// <summary>Writes the structured local log and prunes it on start.</summary>
    public interface ILauncherLog
    {
        void Write(LauncherLogEntry entry);

        /// <summary>Applies the retention policy. Called once on start.</summary>
        void ApplyRetention();
    }

    /// <summary>Reduces a path to the structure that is safe to log by default.</summary>
    public interface IPathRedactor
    {
        RedactedPath Redact(string path);
    }

    /// <summary>Builds the strictly enumerated diagnostic bundle, only on explicit human action.</summary>
    public interface IDiagnosticBundleBuilder
    {
        /// <summary>Describes exactly what would be written, without writing anything.</summary>
        DiagnosticBundlePreview Preview();

        /// <summary>Writes the bundle to the given absolute destination and returns that path.</summary>
        Task<string> BuildAsync(string destinationPath, CancellationToken cancellationToken);
    }

    /// <summary>Opens a produced file, or shows it in the platform's file manager.</summary>
    public interface IOutputRevealer
    {
        Task OpenAsync(string path);

        Task RevealAsync(string path);
    }
}
