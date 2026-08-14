using System;
using OrzioClashReport.Launcher.Contracts.Operations;

namespace OrzioClashReport.Launcher.Contracts.Jobs
{
    /// <summary>
    /// The crash-visible record of a job that entered <see cref="EngineJobState.Running"/>. It exists
    /// only while the job runs and is removed on any terminal state, so a leftover entry on startup
    /// means the launcher was interrupted. It is never used to resume anything automatically.
    /// </summary>
    public sealed class JobJournalEntry
    {
        public string JobId { get; }
        public LauncherOperationKind Operation { get; }
        public DateTimeOffset StartedAtUtc { get; }

        /// <summary>
        /// The output file name only, without any directory component, so a leftover journal never
        /// leaks a client's folder structure.
        /// </summary>
        public string? OutputFileName { get; }

        public JobJournalEntry(
            string jobId,
            LauncherOperationKind operation,
            DateTimeOffset startedAtUtc,
            string? outputFileName)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                throw new ArgumentException("Job id cannot be empty.", nameof(jobId));
            }

            if (!Enum.IsDefined(typeof(LauncherOperationKind), operation))
            {
                throw new ArgumentOutOfRangeException(nameof(operation), operation, "Unknown launcher operation.");
            }

            JobId = jobId;
            Operation = operation;
            StartedAtUtc = startedAtUtc;
            OutputFileName = outputFileName;
        }
    }
}
