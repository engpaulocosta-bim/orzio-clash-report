using System;
using OrzioClashReport.Launcher.Contracts.Operations;

namespace OrzioClashReport.Launcher.Contracts.Jobs
{
    /// <summary>
    /// The minimum a journal records so an interrupted job can be reported honestly on the next
    /// start. It carries no path, no argument vector, and no client data: an interrupted job is
    /// reported, never resumed, so nothing more is needed.
    /// </summary>
    public sealed class JobJournalEntry
    {
        public JobJournalEntry(JobId jobId, LauncherOperationKind operationKind, DateTimeOffset startedAtUtc)
        {
            if (jobId.IsEmpty)
            {
                throw new ArgumentException("Job id must not be empty.", nameof(jobId));
            }

            JobId = jobId;
            OperationKind = operationKind;
            StartedAtUtc = startedAtUtc;
        }

        public JobId JobId { get; }

        public LauncherOperationKind OperationKind { get; }

        public DateTimeOffset StartedAtUtc { get; }
    }
}
