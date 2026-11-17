using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrzioClashReport.Launcher.Contracts.Settings
{
    /// <summary>
    /// Keeps the most recent outputs, newest first. Implementations cap the list and de-duplicate by
    /// path. A missing store yields an empty list; unreadable or corrupt history must be reported
    /// without replacing it when a new output is recorded.
    /// </summary>
    public interface IRecentItemsStore
    {
        Task<IReadOnlyList<RecentOutputItem>> LoadAsync(CancellationToken cancellationToken);

        /// <summary>Records one item and returns the resulting list, newest first.</summary>
        Task<IReadOnlyList<RecentOutputItem>> AddAsync(RecentOutputItem item, CancellationToken cancellationToken);

        Task ClearAsync(CancellationToken cancellationToken);
    }
}
