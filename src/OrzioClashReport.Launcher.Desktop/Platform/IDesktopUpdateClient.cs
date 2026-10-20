using System;
using System.Threading;
using System.Threading.Tasks;
using OrzioClashReport.Launcher.Infrastructure.Updates;

namespace OrzioClashReport.Launcher.Desktop.Platform
{
    /// <summary>
    /// The update operations the desktop shell needs. It exists so the update flow can be exercised
    /// headlessly with a fake, without touching the network or starting an installer.
    /// </summary>
    public interface IDesktopUpdateClient
    {
        Task<DesktopUpdateCheckResult> CheckAsync(string currentVersion, CancellationToken cancellationToken);

        Task<string> DownloadAndStartInstallerAsync(
            DesktopUpdateManifest manifest, IProgress<double>? progress, CancellationToken cancellationToken);
    }

    /// <summary>Adapts <see cref="DesktopUpdateService"/>, the only component that contacts GitHub.</summary>
    public sealed class GitHubDesktopUpdateClient : IDesktopUpdateClient
    {
        private readonly DesktopUpdateService _service;

        public GitHubDesktopUpdateClient(DesktopUpdateService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public Task<DesktopUpdateCheckResult> CheckAsync(string currentVersion, CancellationToken cancellationToken) =>
            _service.CheckAsync(currentVersion, cancellationToken);

        public Task<string> DownloadAndStartInstallerAsync(
            DesktopUpdateManifest manifest, IProgress<double>? progress, CancellationToken cancellationToken) =>
            _service.DownloadAndStartInstallerAsync(manifest, progress, cancellationToken);
    }
}
