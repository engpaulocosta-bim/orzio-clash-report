using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrzioClashReport.Launcher.Contracts.Jobs
{
    /// <summary>
    /// Records running jobs so an interrupted launcher can tell the user what was in flight. There is
    /// deliberately no resume operation: recovery is a human decision, never an automatic retry.
    /// </summary>
    public interface IJobJournal
    {
        Task BeginAsync(JobJournalEntry entry, CancellationToken cancellationToken);

        Task CompleteAsync(string jobId, CancellationToken cancellationToken);

        /// <summary>Entries left behind by a previous process. Reading them does not remove them.</summary>
        Task<IReadOnlyList<JobJournalEntry>> ReadInterruptedAsync(CancellationToken cancellationToken);

        Task ClearAsync(CancellationToken cancellationToken);
    }
}
