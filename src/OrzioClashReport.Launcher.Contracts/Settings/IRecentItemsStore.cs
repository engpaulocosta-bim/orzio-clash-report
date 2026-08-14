using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrzioClashReport.Launcher.Contracts.Settings
{
    /// <summary>
    /// Keeps the most recent outputs, newest first. Implementations cap the list and de-duplicate by
    /// path; a missing or unreadable store yields an empty list rather than an exception.
    /// </summary>
    public interface IRecentItemsStore
    {
        Task<IReadOnlyList<RecentOutputItem>> LoadAsync(CancellationToken cancellationToken);

        /// <summary>Records one item and returns the resulting list, newest first.</summary>
        Task<IReadOnlyList<RecentOutputItem>> AddAsync(RecentOutputItem item, CancellationToken cancellationToken);

        Task ClearAsync(CancellationToken cancellationToken);
    }
}
