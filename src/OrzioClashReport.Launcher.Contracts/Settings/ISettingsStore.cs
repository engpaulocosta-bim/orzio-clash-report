using System.Threading;
using System.Threading.Tasks;

namespace OrzioClashReport.Launcher.Contracts.Settings
{
    /// <summary>
    /// Loads and saves <see cref="LauncherSettings"/>. A missing or unreadable store yields
    /// <see cref="LauncherSettings.Default"/> rather than an exception, so a corrupt preference file
    /// never prevents the application from opening.
    /// </summary>
    public interface ISettingsStore
    {
        Task<LauncherSettings> LoadAsync(CancellationToken cancellationToken);

        Task SaveAsync(LauncherSettings settings, CancellationToken cancellationToken);
    }
}
